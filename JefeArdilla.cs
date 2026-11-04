using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Juego
{
    // Jefe del nivel 1: Chippy Chipmunker, la ardilla mecánica.
    // Vive en una plataforma en el medio del mapa. Ataques: secuaces bellota que escupen hacia arriba,
    // bellota explosiva (hay que romperla a disparos), gancho horizontal y salto con estampida.
    public class JefeArdilla
    {
        // ===== Ajustes rápidos =====
        public const int VidaMaxima = 30;                 // disparos que aguanta
        private const float PiesSuelo = 455f;
        private const int AnchoPantalla = 960;
        private const float AltoBoss = 190f;              // alto del jefe en pantalla
        private const float AltoSecuaz = 38f;             // alto de cada secuaz
        private const float Gravedad = 900f;
        private const float RadioExplosion = 110f;
        private const float TiempoVueloBomba = 1.0f;
        // "Transparente": se puede atravesar y no daña por contacto, solo con sus ataques.
        private readonly bool _danoPorContacto = false;

        // ===== Nombres de los png (sin extensión). Se buscan dentro de la carpeta Sprites =====
        private const string FBase = "ardilla base";
        private const string FLanzando = "ardilla lanzando";
        private const string FLanzando2 = "ardilla lanzando 2";
        private const string FPorLanzarDer = "ardilla por lanzar derecha";
        private const string FPorLanzar = "ardilla por lanzar";
        private const string FTras1 = "ardilla tras lanzar 1";
        private const string FTras2 = "ardilla tras lanzar 2";

        private static readonly string[] NombresBoss =
            { FBase, FLanzando, FLanzando2, FPorLanzarDer, FPorLanzar, FTras1, FTras2 };

        private static readonly string[] NombresSecuaz =
        {
            "bellota base derecha", "bellota base izquierda",
            "bellota lanzando derecha", "bellota lanzando izquierda",
            "bellota prepandose derecha", "bellota prepandose izquierda"
        };

        // ===== Estado =====
        public int Vida { get; private set; } = VidaMaxima;
        public bool Vivo => Vida > 0;
        public Rectangle Bounds => new Rectangle((int)_pies.X - 50, (int)_pies.Y - 130, 100, 130);

        private enum Estado
        {
            Idle, GanchoAviso, GanchoSale, GanchoVuelve, BombaAviso,
            SaltoAviso, SaltoAire, SaltoImpacto, SaltoVuelta, Recupera
        }
        private enum Ataque { Ninguno, Gancho, Bomba, Salto }

        private class Secuaz
        {
            public float X;
            public int Vida = 3;
            public float Cooldown, Prep, Post, Respawn, Flash;
            public int Dir = 1;
            public bool Viva => Vida > 0;
            public Rectangle Bounds => new Rectangle((int)X - 16, (int)PiesSuelo - 38, 32, 38);
        }

        private class Escupitajo
        {
            public Vector2 Pos, Vel;
            public bool Activa = true;
            public Rectangle Bounds => new Rectangle((int)Pos.X - 7, (int)Pos.Y - 7, 14, 14);
        }

        private class Bomba
        {
            public Vector2 Pos, Vel, Destino;
            public bool Activa = true, EnVuelo = true, Explotando;
            public float TVuelo, Fusible = 3.2f, TExplosion;
            public Rectangle Bounds => new Rectangle((int)Pos.X - 14, (int)Pos.Y - 14, 28, 28);
        }

        private readonly Random _rnd = new();
        private readonly Rectangle _plataforma;
        private readonly Dictionary<string, Texture2D> _frames;
        private readonly Dictionary<string, Texture2D> _framesSecuaz;
        private float _escalaBoss = 1f, _escalaSecuaz = 1f;
        private readonly Texture2D? _texAlerta;

        private readonly List<Secuaz> _secuaces = new();
        private readonly List<Escupitajo> _escupitajos = new();
        private readonly List<Bomba> _bombas = new();

        private Estado _estado = Estado.Idle;
        private Ataque _ultimo = Ataque.Ninguno;
        private Vector2 _pies;
        private float _t, _espera = 2.5f, _reloj, _flash;
        private int _dir = 1;
        private Vector2 _ganchoPos;
        private Vector2 _saltoInicio, _saltoDestino;
        private float _bombaDestino;

        public JefeArdilla(GraphicsDevice gd, string carpetaSprites, Rectangle plataforma)
        {
            _plataforma = plataforma;
            _pies = new Vector2(plataforma.Center.X, plataforma.Top);

            // Se registra todo en log_ardilla.txt (junto al .exe) para saber qué sprites se encontraron.
            var log = new System.Text.StringBuilder();
            string raiz = carpetaSprites;
            var pngs = ListarPng(raiz);
            if (pngs.Count == 0)
            {
                log.AppendLine("No hay png en la carpeta Sprites; se busca en toda la carpeta del juego.");
                raiz = AppContext.BaseDirectory;
                pngs = ListarPng(raiz);
            }
            log.AppendLine($"Buscando en: {raiz}");
            log.AppendLine($"PNG encontrados: {pngs.Count}");

            _frames = CargarRecortado(gd, pngs, NombresBoss, AltoBoss, log, out _escalaBoss);
            _framesSecuaz = CargarRecortado(gd, pngs, NombresSecuaz, AltoSecuaz, log, out _escalaSecuaz);

            string? rutaAlerta = Buscar(pngs, "alerta_peligro");
            log.AppendLine(rutaAlerta != null ? $"ok: {rutaAlerta}" : "FALTA: alerta_peligro.png");
            if (_frames.Count == 0)
            {
                log.AppendLine("Ningún frame de la ardilla cargó. Primeros png encontrados:");
                foreach (string p in pngs.Take(40)) log.AppendLine("  " + p);
            }
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "log_ardilla.txt"), log.ToString()); }
            catch { /* si no se puede escribir el log, no pasa nada */ }
            if (rutaAlerta != null)
            {
                using var fs = File.OpenRead(rutaAlerta);
                _texAlerta = Texture2D.FromStream(gd, fs);
            }

            _secuaces.Add(new Secuaz { X = 110f, Cooldown = 1.5f });
            _secuaces.Add(new Secuaz { X = 850f, Cooldown = 2.6f });
        }

        // ---------- Carga de sprites ----------
        private static List<string> ListarPng(string raiz)
        {
            if (!Directory.Exists(raiz)) return new List<string>();
            try { return Directory.EnumerateFiles(raiz, "*.png", SearchOption.AllDirectories).ToList(); }
            catch { return new List<string>(); }
        }

        // Compara ignorando mayúsculas, espacios, guiones y guiones bajos.
        private static string Norm(string s) =>
            new string(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

        private static string? Buscar(List<string> pngs, string nombre)
        {
            string objetivo = Norm(nombre);
            foreach (string p in pngs)
                if (Norm(Path.GetFileNameWithoutExtension(p)) == objetivo) return p;
            return null;
        }

        // Carga los frames de un grupo y recorta el margen transparente (igual para todos los
        // frames del grupo, así no "saltan" al cambiar de uno a otro).
        private static Dictionary<string, Texture2D> CargarRecortado(GraphicsDevice gd, List<string> pngs,
            string[] nombres, float altoDeseado, System.Text.StringBuilder log, out float escala)
        {
            var crudas = new Dictionary<string, (Texture2D tex, Color[] px)>();
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;

            foreach (string n in nombres)
            {
                string? ruta = Buscar(pngs, n);
                if (ruta == null) { log.AppendLine($"FALTA: {n}.png"); continue; }
                log.AppendLine($"ok: {ruta}");

                using var fs = File.OpenRead(ruta);
                var t = Texture2D.FromStream(gd, fs);
                var px = new Color[t.Width * t.Height];
                t.GetData(px);
                crudas[n] = (t, px);

                for (int y = 0; y < t.Height; y++)
                    for (int x = 0; x < t.Width; x++)
                        if (px[y * t.Width + x].A > 20)
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
            }

            escala = 1f;
            var resultado = new Dictionary<string, Texture2D>();
            if (maxX < 0) return resultado;

            int w = maxX - minX + 1, h = maxY - minY + 1;
            escala = altoDeseado / h;

            foreach (var kv in crudas)
            {
                var (t, px) = kv.Value;
                var rec = new Color[w * h];
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int sx = minX + x, sy = minY + y;
                        rec[y * w + x] = (sx < t.Width && sy < t.Height)
                            ? px[sy * t.Width + sx] : Color.Transparent;
                    }
                var nueva = new Texture2D(gd, w, h);
                nueva.SetData(rec);
                resultado[kv.Key] = nueva;
                t.Dispose();
            }
            return resultado;
        }

        // ---------- Lógica ----------
        public void Actualizar(float delta, Jugador jugador, List<Rectangle>? plataformas = null)
        {
            if (!Vivo) return;

            _reloj += delta;
            _t += delta;
            if (_flash > 0) _flash -= delta;

            Vector2 jc = jugador.Bounds.Center.ToVector2();
            bool furia = Vida <= VidaMaxima / 2;   // en la segunda mitad ataca más rápido

            ActualizarEstado(delta, jc, jugador, furia);
            ActualizarSecuaces(delta, jugador, jc, furia);
            ActualizarProyectiles(delta, jugador, jc);
            ResolverDisparos(jugador);

            if (_danoPorContacto && Bounds.Intersects(jugador.Bounds)) jugador.RecibirGolpe();
        }

        private void Cambiar(Estado e) { _estado = e; _t = 0f; }

        private void ActualizarEstado(float delta, Vector2 jc, Jugador jugador, bool furia)
        {
            switch (_estado)
            {
                case Estado.Idle:
                    _pies = new Vector2(_plataforma.Center.X, _plataforma.Top);
                    _espera -= delta;
                    if (_espera <= 0) ElegirAtaque(jc);
                    break;

                // --- Gancho horizontal a ras del suelo: se esquiva desde una plataforma ---
                case Estado.GanchoAviso:
                    if (_t >= (furia ? 0.65f : 0.9f))
                    {
                        _ganchoPos = new Vector2(_pies.X, PiesSuelo - 18f);
                        Cambiar(Estado.GanchoSale);
                    }
                    break;

                case Estado.GanchoSale:
                    _ganchoPos.X += _dir * 950f * delta;
                    ChocaGancho(jugador);
                    if (_ganchoPos.X < -40f || _ganchoPos.X > AnchoPantalla + 40f)
                        Cambiar(Estado.GanchoVuelve);
                    break;

                case Estado.GanchoVuelve:
                    _ganchoPos.X -= _dir * 1250f * delta;
                    ChocaGancho(jugador);
                    if ((_dir == 1 && _ganchoPos.X <= _pies.X) || (_dir == -1 && _ganchoPos.X >= _pies.X))
                        Cambiar(Estado.Recupera);
                    break;

                // --- Bellota explosiva ---
                case Estado.BombaAviso:
                    if (_t >= 0.7f)
                    {
                        LanzarBomba();
                        Cambiar(Estado.Recupera);
                    }
                    break;

                // --- Salto con estampida ---
                case Estado.SaltoAviso:
                    if (_t >= (furia ? 0.6f : 0.85f))
                    {
                        _saltoInicio = _pies;
                        Cambiar(Estado.SaltoAire);
                    }
                    break;

                case Estado.SaltoAire:
                {
                    float u = MathHelper.Clamp(_t / 0.75f, 0f, 1f);
                    _pies = SaltoPos(_saltoInicio, _saltoDestino, u, 140f);
                    if (u >= 1f)
                    {
                        _pies = _saltoDestino;
                        Impacto(jugador);
                        Cambiar(Estado.SaltoImpacto);
                    }
                    break;
                }

                case Estado.SaltoImpacto:
                    if (_t >= (furia ? 0.6f : 0.9f))
                    {
                        _saltoInicio = _pies;
                        _saltoDestino = new Vector2(_plataforma.Center.X, _plataforma.Top);
                        Cambiar(Estado.SaltoVuelta);
                    }
                    break;

                case Estado.SaltoVuelta:
                {
                    float u = MathHelper.Clamp(_t / 0.8f, 0f, 1f);
                    _pies = SaltoPos(_saltoInicio, _saltoDestino, u, 150f);
                    if (u >= 1f)
                    {
                        _pies = _saltoDestino;
                        _espera = furia ? 1.1f : 1.9f;
                        Cambiar(Estado.Idle);
                    }
                    break;
                }

                case Estado.Recupera:
                    if (_t >= 0.6f)
                    {
                        _espera = furia ? 1.1f : 1.9f;
                        Cambiar(Estado.Idle);
                    }
                    break;
            }
        }

        private void ElegirAtaque(Vector2 jc)
        {
            Ataque a;
            do { a = (Ataque)_rnd.Next(1, 4); }
            while (a == _ultimo || (a == Ataque.Bomba && _bombas.Count > 0));
            _ultimo = a;

            switch (a)
            {
                case Ataque.Gancho:
                    _dir = jc.X < _pies.X ? -1 : 1;
                    Cambiar(Estado.GanchoAviso);
                    break;
                case Ataque.Bomba:
                    _bombaDestino = MathHelper.Clamp(jc.X + _rnd.Next(-140, 141), 80f, 880f);
                    _dir = _bombaDestino < _pies.X ? -1 : 1;
                    Cambiar(Estado.BombaAviso);
                    break;
                default:
                    _saltoDestino = new Vector2(MathHelper.Clamp(jc.X, 70f, 890f), PiesSuelo);
                    Cambiar(Estado.SaltoAviso);
                    break;
            }
        }

        private static Vector2 SaltoPos(Vector2 a, Vector2 b, float u, float arco)
        {
            float x = MathHelper.Lerp(a.X, b.X, u);
            float y = MathHelper.Lerp(a.Y, b.Y, u) - 4f * arco * u * (1f - u);
            return new Vector2(x, y);
        }

        private void ChocaGancho(Jugador jugador)
        {
            var hb = new Rectangle((int)_ganchoPos.X - 22, (int)_ganchoPos.Y - 16, 44, 32);
            if (hb.Intersects(jugador.Bounds)) jugador.RecibirGolpe();
        }

        private float _ondaT = 99f;
        private float _ondaX;

        private void Impacto(Jugador jugador)
        {
            _ondaT = 0f;
            _ondaX = _pies.X;
            var onda = new Rectangle((int)_pies.X - 85, (int)PiesSuelo - 40, 170, 40);
            if (onda.Intersects(jugador.Bounds)) jugador.RecibirGolpe(2);
        }

        private void LanzarBomba()
        {
            var origen = new Vector2(_pies.X + _dir * 45f, _pies.Y - 130f);
            float y1 = PiesSuelo - 14f;
            float vx = (_bombaDestino - origen.X) / TiempoVueloBomba;
            float vy = (y1 - origen.Y - 0.5f * Gravedad * TiempoVueloBomba * TiempoVueloBomba) / TiempoVueloBomba;
            _bombas.Add(new Bomba
            {
                Pos = origen,
                Vel = new Vector2(vx, vy),
                Destino = new Vector2(_bombaDestino, y1)
            });
        }

        // ---------- Secuaces ----------
        private void ActualizarSecuaces(float delta, Jugador jugador, Vector2 jc, bool furia)
        {
            foreach (var s in _secuaces)
            {
                if (s.Flash > 0) s.Flash -= delta;

                if (!s.Viva)
                {
                    s.Respawn -= delta;
                    if (s.Respawn <= 0) { s.Vida = 3; s.Cooldown = 1.5f; s.Prep = 0; s.Post = 0; }
                    continue;
                }

                s.Dir = jc.X < s.X ? -1 : 1;

                if (s.Prep > 0)
                {
                    s.Prep -= delta;
                    if (s.Prep <= 0) { Escupir(s, jc); s.Post = 0.3f; }
                }
                else if (s.Post > 0)
                {
                    s.Post -= delta;
                }
                else
                {
                    s.Cooldown -= delta;
                    if (s.Cooldown <= 0)
                    {
                        s.Prep = 0.5f;
                        s.Cooldown = (furia ? 1.9f : 2.8f) + (float)_rnd.NextDouble();
                    }
                }

                if (s.Bounds.Intersects(jugador.Bounds)) jugador.RecibirGolpe();
            }
        }

        // Escupe una bellota hacia arriba; cae hacia donde estaba el jugador.
        private void Escupir(Secuaz s, Vector2 jc)
        {
            const float vy = -540f;
            float tCaida = 2f * -vy / Gravedad + 0.1f;
            float vx = MathHelper.Clamp((jc.X - s.X) / tCaida + _rnd.Next(-40, 41), -330f, 330f);
            _escupitajos.Add(new Escupitajo
            {
                Pos = new Vector2(s.X, PiesSuelo - 40f),
                Vel = new Vector2(vx, vy)
            });
        }

        // ---------- Proyectiles ----------
        private void ActualizarProyectiles(float delta, Jugador jugador, Vector2 jc)
        {
            foreach (var e in _escupitajos)
            {
                e.Vel.Y += Gravedad * delta;
                e.Pos += e.Vel * delta;
                if (e.Pos.Y > PiesSuelo + 10f || e.Pos.X < -40f || e.Pos.X > AnchoPantalla + 40f)
                    e.Activa = false;
                if (e.Activa && e.Bounds.Intersects(jugador.Bounds))
                {
                    e.Activa = false;
                    jugador.RecibirGolpe();
                }
            }
            _escupitajos.RemoveAll(e => !e.Activa);

            foreach (var b in _bombas)
            {
                if (!b.Activa) continue;

                if (b.Explotando)
                {
                    b.TExplosion += delta;
                    if (b.TExplosion >= 0.35f) b.Activa = false;
                    continue;
                }

                if (b.EnVuelo)
                {
                    b.TVuelo += delta;
                    b.Vel.Y += Gravedad * delta;
                    b.Pos += b.Vel * delta;
                    if (b.TVuelo >= TiempoVueloBomba)
                    {
                        b.Pos = b.Destino;
                        b.Vel = Vector2.Zero;
                        b.EnVuelo = false;
                    }
                }
                else
                {
                    b.Fusible -= delta;
                    if (b.Fusible <= 0)
                    {
                        b.Explotando = true;
                        if (Vector2.Distance(b.Pos, jc) < RadioExplosion) jugador.RecibirGolpe(2);
                    }
                }
            }
            _bombas.RemoveAll(b => !b.Activa);

            if (_ondaT < 1f) _ondaT += delta;
        }

        // Disparos del jugador: primero rompen bombas, luego secuaces, luego al jefe.
        private void ResolverDisparos(Jugador jugador)
        {
            foreach (var p in jugador.Proyectiles)
            {
                if (!p.Activo) continue;
                Rectangle pb = p.Bounds;
                bool impacto = false;

                foreach (var b in _bombas)
                {
                    if (b.Activa && !b.Explotando && pb.Intersects(b.Bounds))
                    {
                        b.Activa = false;     // bellota explosiva rota antes de explotar
                        impacto = true;
                        break;
                    }
                }
                if (impacto) { p.Activo = false; continue; }

                foreach (var s in _secuaces)
                {
                    if (s.Viva && pb.Intersects(s.Bounds))
                    {
                        s.Vida--;
                        s.Flash = 0.1f;
                        if (!s.Viva) s.Respawn = 9f;
                        impacto = true;
                        break;
                    }
                }
                if (impacto) { p.Activo = false; continue; }

                if (pb.Intersects(Bounds))
                {
                    Vida--;
                    _flash = 0.1f;
                    p.Activo = false;
                }
            }
            _bombas.RemoveAll(b => !b.Activa);
        }

        // ---------- Dibujo ----------
        public void Dibujar(SpriteBatch sb, Texture2D pixel)
        {
            if (!Vivo) return;

            DibujarAvisos(sb, pixel);
            foreach (var b in _bombas) DibujarBomba(sb, pixel, b);
            foreach (var s in _secuaces) DibujarSecuaz(sb, pixel, s);
            if (_estado == Estado.GanchoSale || _estado == Estado.GanchoVuelve) DibujarGancho(sb, pixel);
            DibujarJefe(sb, pixel);

            foreach (var e in _escupitajos)
            {
                int x = (int)e.Pos.X, y = (int)e.Pos.Y;
                sb.Draw(pixel, new Rectangle(x - 5, y - 4, 10, 11), new Color(150, 95, 40));
                sb.Draw(pixel, new Rectangle(x - 6, y - 7, 12, 5), new Color(100, 60, 25));
            }

            // Onda de la estampida.
            if (_ondaT < 0.3f)
            {
                float u = _ondaT / 0.3f;
                int ancho = (int)(170 * (0.6f + u));
                sb.Draw(pixel, new Rectangle((int)_ondaX - ancho / 2, (int)PiesSuelo - 40, ancho, 40),
                    Color.Orange * (1f - u));
            }

            // Barra de vida del jefe, arriba al centro.
            int llena = (int)(480f * Vida / VidaMaxima);
            sb.Draw(pixel, new Rectangle(238, 12, 484, 22), Color.Black);
            sb.Draw(pixel, new Rectangle(240, 14, 480, 18), Color.DarkRed);
            sb.Draw(pixel, new Rectangle(240, 14, Math.Max(0, llena), 18), Color.OrangeRed);
        }

        private void DibujarAvisos(SpriteBatch sb, Texture2D pixel)
        {
            float pulso = 0.5f + 0.5f * Math.Abs((float)Math.Sin(_reloj * 9f));

            if (_estado == Estado.GanchoAviso)
            {
                int x0 = _dir == 1 ? (int)_pies.X : 0;
                int ancho = _dir == 1 ? AnchoPantalla - x0 : (int)_pies.X;
                sb.Draw(pixel, new Rectangle(x0, (int)PiesSuelo - 34, ancho, 34),
                    Color.Red * (0.15f + 0.2f * pulso));
                DibujarAlerta(sb, pixel, new Vector2(_dir == 1 ? AnchoPantalla - 40f : 40f, PiesSuelo - 32f));
            }
            else if (_estado == Estado.SaltoAviso)
            {
                DibujarAlerta(sb, pixel, new Vector2(_saltoDestino.X, PiesSuelo - 32f));
            }
            else if (_estado == Estado.BombaAviso)
            {
                DibujarAlerta(sb, pixel, new Vector2(_bombaDestino, PiesSuelo - 32f));
            }

            foreach (var b in _bombas)
                if (b.Activa && b.EnVuelo)
                    DibujarAlerta(sb, pixel, new Vector2(b.Destino.X, PiesSuelo - 32f));
        }

        private void DibujarAlerta(SpriteBatch sb, Texture2D pixel, Vector2 centro)
        {
            float pulso = 1f + 0.12f * (float)Math.Sin(_reloj * 14f);
            float opacidad = 0.55f + 0.45f * Math.Abs((float)Math.Sin(_reloj * 8f));
            const float tam = 44f;

            if (_texAlerta != null)
            {
                sb.Draw(_texAlerta, centro, null, Color.White * opacidad, 0f,
                    new Vector2(_texAlerta.Width / 2f, _texAlerta.Height / 2f),
                    tam / _texAlerta.Height * pulso, SpriteEffects.None, 0f);
                return;
            }

            int alto = (int)(tam * pulso);
            for (int fila = 0; fila < alto; fila += 2)
            {
                int ancho = Math.Max(2, fila * 2 * 4 / 5);
                sb.Draw(pixel, new Rectangle((int)centro.X - ancho / 2, (int)centro.Y - alto / 2 + fila, ancho, 2),
                    Color.Red * opacidad);
            }
            sb.Draw(pixel, new Rectangle((int)centro.X - 2, (int)centro.Y - 6, 4, 14), Color.Black * opacidad);
            sb.Draw(pixel, new Rectangle((int)centro.X - 2, (int)centro.Y + 11, 4, 4), Color.Black * opacidad);
        }

        private (string nombre, bool flip) FrameActual()
        {
            switch (_estado)
            {
                case Estado.GanchoAviso: return (_dir == 1 ? FPorLanzarDer : FPorLanzar, false);
                case Estado.GanchoSale:
                case Estado.GanchoVuelve: return (FLanzando, _dir == -1);
                case Estado.BombaAviso: return (FLanzando2, false);
                case Estado.SaltoAviso: return (FPorLanzar, false);
                case Estado.SaltoAire:
                case Estado.SaltoVuelta: return (FTras1, false);
                case Estado.SaltoImpacto: return (FTras2, false);
                case Estado.Recupera: return (_t < 0.3f ? FTras1 : FTras2, false);
                default: return (FBase, false);
            }
        }

        private void DibujarJefe(SpriteBatch sb, Texture2D pixel)
        {
            var (nombre, flip) = FrameActual();
            Color tinte = _flash > 0 ? Color.Lerp(Color.White, Color.Red, 0.6f) : Color.White;

            if (_frames.TryGetValue(nombre, out var tex) || _frames.TryGetValue(FBase, out tex))
            {
                sb.Draw(tex, _pies, null, tinte, 0f, new Vector2(tex.Width / 2f, tex.Height),
                    _escalaBoss, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            }
            else
            {
                // Sin sprites: un bloque marrón para poder probar igual.
                sb.Draw(pixel, Bounds, _flash > 0 ? Color.Red : new Color(170, 95, 40));
            }
        }

        private void DibujarGancho(SpriteBatch sb, Texture2D pixel)
        {
            Vector2 mano = new Vector2(_pies.X + _dir * 40f, _pies.Y - 90f);
            Vector2 d = _ganchoPos - mano;
            float largo = d.Length();
            if (largo > 1f)
                sb.Draw(pixel, mano, null, new Color(70, 70, 80), (float)Math.Atan2(d.Y, d.X),
                    new Vector2(0f, 0.5f), new Vector2(largo, 4f), SpriteEffects.None, 0f);

            int x = (int)_ganchoPos.X, y = (int)_ganchoPos.Y;
            sb.Draw(pixel, new Rectangle(x - 22, y - 16, 44, 32), new Color(130, 130, 145));
            sb.Draw(pixel, new Rectangle(x + (_dir == 1 ? 8 : -22), y - 16, 14, 32), new Color(200, 200, 210));
        }

        private void DibujarBomba(SpriteBatch sb, Texture2D pixel, Bomba b)
        {
            int x = (int)b.Pos.X, y = (int)b.Pos.Y;

            if (b.Explotando)
            {
                float u = b.TExplosion / 0.35f;
                int r = (int)(RadioExplosion * u);
                sb.Draw(pixel, new Rectangle(x - r, y - r, r * 2, r * 2), Color.Orange * (0.8f - 0.6f * u));
                int r2 = r / 2;
                sb.Draw(pixel, new Rectangle(x - r2, y - r2, r2 * 2, r2 * 2), Color.Yellow * (0.9f - 0.7f * u));
                return;
            }

            // Parpadea cada vez más rápido al acercarse la explosión.
            float ritmo = 6f + (3.2f - b.Fusible) * 6f;
            bool destello = !b.EnVuelo && ((int)(_reloj * ritmo) % 2 == 0);
            Color cuerpo = destello ? Color.White : new Color(225, 40, 50);

            sb.Draw(pixel, new Rectangle(x - 11, y - 9, 22, 18), cuerpo);
            sb.Draw(pixel, new Rectangle(x - 9, y - 11, 18, 22), cuerpo);
            sb.Draw(pixel, new Rectangle(x - 7, y - 7, 5, 5), new Color(255, 170, 170));
            sb.Draw(pixel, new Rectangle(x - 8, y - 15, 16, 6), new Color(120, 75, 30));
            sb.Draw(pixel, new Rectangle(x + 1, y - 21, 3, 6), new Color(90, 90, 95));
            if (destello || b.EnVuelo)
                sb.Draw(pixel, new Rectangle(x, y - 26, 5, 5), Color.Yellow);
        }

        private void DibujarSecuaz(SpriteBatch sb, Texture2D pixel, Secuaz s)
        {
            if (!s.Viva) return;

            string estado = s.Post > 0 ? "lanzando" : (s.Prep > 0 ? "prepandose" : "base");
            Texture2D? tex = ElegirFrameSecuaz(estado, s.Dir, out bool flip);
            Color tinte = s.Flash > 0 ? Color.Lerp(Color.White, Color.Red, 0.6f) : Color.White;

            if (tex != null)
            {
                sb.Draw(tex, new Vector2(s.X, PiesSuelo), null, tinte, 0f,
                    new Vector2(tex.Width / 2f, tex.Height), _escalaSecuaz,
                    flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
            }
            else
            {
                sb.Draw(pixel, s.Bounds, s.Flash > 0 ? Color.Red : new Color(150, 95, 40));
            }
        }

        private Texture2D? ElegirFrameSecuaz(string estado, int dir, out bool flip)
        {
            string lado = dir == 1 ? "derecha" : "izquierda";
            Texture2D? t;
            if (_framesSecuaz.TryGetValue($"bellota {estado} {lado}", out t)) { flip = false; return t; }
            // Si falta la versión izquierda, se usa la derecha volteada.
            if (_framesSecuaz.TryGetValue($"bellota {estado} derecha", out t)) { flip = dir == -1; return t; }
            flip = false;
            return null;
        }
    }
}