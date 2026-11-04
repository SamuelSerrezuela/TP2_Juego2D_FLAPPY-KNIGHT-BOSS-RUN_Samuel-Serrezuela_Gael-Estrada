using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Juego
{
    // 4 rondas por nivel. Antes de cada ronda aparecen alertas de peligro donde saldrán los enemigos.
    public class GestorRondas
    {
        public const int RondasPorNivel = 4;
        private const float PiesSuelo = 455f;       // SueloY (407) + alto del jugador (48)
        private const int AnchoPantalla = 960;

        private const float DuracionAlerta = 1.6f;   // segundos de aviso antes de cada ronda
        private const float PausaTrasRonda = 0.8f;   // respiro al limpiar una ronda
        private const float ProbabilidadVida = 0.15f; // 15% de que un enemigo suelte vida
        private const int CuracionPorItem = 1;
        private const float TamanoAlerta = 44f;      // alto del icono en pantalla

        public int Nivel { get; private set; } = 1;
        public int RondaActual { get; private set; }
        public bool NivelCompletado { get; private set; }
        public List<EnemigoMalvado> Enemigos { get; } = new();
        public List<Espina> Espinas { get; } = new();
        public List<ObjetoVida> Items { get; } = new();
        public bool Alertando => _pendientes.Count > 0;
        public event Action<int>? NivelTerminado;

        private enum Tipo { Flor, Zorro, Halcon }

        private class Spawn
        {
            public Tipo Tipo;
            public Vector2 Pies;           // dónde aparecerá el enemigo
            public Vector2 IconoCentro;    // dónde se dibuja la alerta
        }

        private readonly List<Spawn> _pendientes = new();
        private readonly GraphicsDevice _gd;
        private readonly string _carpeta;
        private readonly Texture2D? _texAlerta;
        private readonly Random _rnd = new();
        private float _pausa;
        private float _alerta;
        private float _reloj;
        private float _jugadorX = 100f;

        // carpetaNivel = ...\Sprites\1 wordl  (con las subcarpetas de enemigos y alerta_peligro.png)
        public GestorRondas(GraphicsDevice graphicsDevice, string carpetaNivel)
        {
            _gd = graphicsDevice;
            _carpeta = carpetaNivel;

            string rutaAlerta = Path.Combine(carpetaNivel, "alerta_peligro.png");
            if (File.Exists(rutaAlerta))
            {
                using var stream = File.OpenRead(rutaAlerta);
                _texAlerta = Texture2D.FromStream(graphicsDevice, stream);
            }
        }

        private Animacion Cargar(string subcarpeta, string archivo) =>
            Animacion.CargarAutomatica(_gd, Path.Combine(_carpeta, subcarpeta, archivo), 0.12f, true);

        public void IniciarNivel(int nivel)
        {
            Nivel = nivel; RondaActual = 0; NivelCompletado = false; _pausa = 0;
            Enemigos.Clear(); Espinas.Clear(); Items.Clear(); _pendientes.Clear();
            PrepararRonda();
        }

        // Decide qué enemigos vienen y dónde, y arranca la cuenta de la alerta.
        private void PrepararRonda()
        {
            RondaActual++;
            int extra = (Nivel - 1) / 2;
            int flores = RondaActual == 1 ? 2 + extra : 1 + RondaActual / 2 + extra;
            int zorros = RondaActual >= 2 ? RondaActual - 1 + extra : 0;
            int halcones = RondaActual >= 3 ? RondaActual - 2 + extra : 0;

            for (int i = 0; i < flores; i++)
            {
                // Evita que una flor aparezca encima del jugador.
                float x = 0;
                for (int intento = 0; intento < 10; intento++)
                {
                    x = _rnd.Next(80, AnchoPantalla - 80);
                    if (Math.Abs(x - _jugadorX) > 110f) break;
                }
                _pendientes.Add(new Spawn
                {
                    Tipo = Tipo.Flor,
                    Pies = new Vector2(x, PiesSuelo),
                    IconoCentro = new Vector2(x, PiesSuelo - 32f)
                });
            }

            for (int i = 0; i < zorros; i++)
            {
                float x = i % 2 == 0 ? 40f : AnchoPantalla - 40f;
                _pendientes.Add(new Spawn
                {
                    Tipo = Tipo.Zorro,
                    Pies = new Vector2(x, PiesSuelo),
                    IconoCentro = new Vector2(MathHelper.Clamp(x, 30f, AnchoPantalla - 30f), PiesSuelo - 32f)
                });
            }

            for (int i = 0; i < halcones; i++)
            {
                // x >= 200 para no tapar la barra de vida del HUD.
                float x = _rnd.Next(200, AnchoPantalla - 80);
                _pendientes.Add(new Spawn
                {
                    Tipo = Tipo.Halcon,
                    Pies = new Vector2(x, 60f),
                    IconoCentro = new Vector2(x, 50f)
                });
            }

            _alerta = DuracionAlerta;
        }

        // Termina la alerta: aparecen los enemigos en los puntos avisados.
        private void Aparecer()
        {
            foreach (var s in _pendientes)
            {
                switch (s.Tipo)
                {
                    case Tipo.Flor:
                        Enemigos.Add(new FlorMalvada(
                            Cargar("flor malvada", "flores malvadas rework.png"), s.Pies));
                        break;
                    case Tipo.Zorro:
                        Enemigos.Add(new ZorroMalvado(
                            Cargar("zorro malvado", "zorro malvado caminando.png"), s.Pies));
                        break;
                    case Tipo.Halcon:
                        Enemigos.Add(new HalconMalvado(
                            Cargar("halcon malvado", "halcon malvado caminando.png"), s.Pies));
                        break;
                }
            }
            _pendientes.Clear();
        }

        public void Actualizar(float delta, Jugador jugador, List<Rectangle>? plataformas = null)
        {
            if (NivelCompletado) return;

            _reloj += delta;
            _jugadorX = jugador.Bounds.Center.X;
            Vector2 objetivo = jugador.Bounds.Center.ToVector2();

            foreach (var e in Enemigos) e.Actualizar(delta, objetivo, Espinas);
            foreach (var s in Espinas) s.Actualizar(delta);
            foreach (var it in Items) it.Actualizar(delta, plataformas);

            // Disparos del jugador -> enemigos (5 disparos = muerte).
            foreach (var p in jugador.Proyectiles)
            {
                if (!p.Activo) continue;
                foreach (var e in Enemigos)
                {
                    if (e.Activo && p.Bounds.Intersects(e.Bounds))
                    {
                        e.RecibirDisparo();
                        p.Activo = false;
                        break;
                    }
                }
            }

            // Enemigos y espinas -> jugador (RecibirGolpe ya tiene invulnerabilidad).
            foreach (var e in Enemigos)
                if (e.Activo && e.Bounds.Intersects(jugador.Bounds)) jugador.RecibirGolpe();
            foreach (var s in Espinas)
                if (s.Activa && s.Bounds.Intersects(jugador.Bounds)) { s.Activa = false; jugador.RecibirGolpe(); }

            // De vez en cuando, un enemigo derrotado suelta un corazón.
            foreach (var e in Enemigos)
                if (!e.Activo && _rnd.NextDouble() < ProbabilidadVida)
                    Items.Add(new ObjetoVida(e.Centro));

            // El jugador recoge la vida (solo si le falta).
            foreach (var it in Items)
            {
                if (it.Activa && jugador.Vida < Jugador.VidaMaxima && it.Bounds.Intersects(jugador.Bounds))
                {
                    jugador.Curar(CuracionPorItem);
                    it.Activa = false;
                }
            }

            Enemigos.RemoveAll(e => !e.Activo);
            Espinas.RemoveAll(s => !s.Activa);
            Items.RemoveAll(i => !i.Activa);
            jugador.Proyectiles.RemoveAll(p => !p.Activo || p.FueraDePantalla);

            if (_pendientes.Count > 0)
            {
                _alerta -= delta;
                if (_alerta <= 0) Aparecer();
            }
            else if (Enemigos.Count == 0)
            {
                _pausa += delta;
                if (_pausa >= PausaTrasRonda)
                {
                    _pausa = 0;
                    if (RondaActual >= RondasPorNivel)
                    {
                        NivelCompletado = true;
                        Espinas.Clear();
                        Items.Clear();
                        NivelTerminado?.Invoke(Nivel);
                    }
                    else PrepararRonda();
                }
            }
        }

        public void Dibujar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            foreach (var it in Items) it.Dibujar(spriteBatch, pixel);
            foreach (var e in Enemigos) e.Dibujar(spriteBatch, pixel);
            foreach (var s in Espinas) s.Dibujar(spriteBatch, pixel);
            foreach (var s in _pendientes) DibujarAlerta(spriteBatch, pixel, s.IconoCentro);
        }

        private void DibujarAlerta(SpriteBatch spriteBatch, Texture2D pixel, Vector2 centro)
        {
            // Parpadeo + pulso para que se note.
            float pulso = 1f + 0.12f * (float)Math.Sin(_reloj * 14f);
            float opacidad = 0.55f + 0.45f * Math.Abs((float)Math.Sin(_reloj * 8f));

            if (_texAlerta != null)
            {
                float escala = TamanoAlerta / _texAlerta.Height * pulso;
                spriteBatch.Draw(_texAlerta, centro, null, Color.White * opacidad, 0f,
                    new Vector2(_texAlerta.Width / 2f, _texAlerta.Height / 2f), escala,
                    SpriteEffects.None, 0f);
                return;
            }

            // Si no se encuentra la imagen: triángulo rojo hecho con rectángulos.
            int alto = (int)(TamanoAlerta * pulso);
            for (int fila = 0; fila < alto; fila += 2)
            {
                int ancho = Math.Max(2, fila * 2 * 4 / 5);
                spriteBatch.Draw(pixel, new Rectangle((int)centro.X - ancho / 2,
                    (int)centro.Y - alto / 2 + fila, ancho, 2), Color.Red * opacidad);
            }
            spriteBatch.Draw(pixel, new Rectangle((int)centro.X - 2, (int)centro.Y - 6, 4, 14),
                Color.Black * opacidad);
            spriteBatch.Draw(pixel, new Rectangle((int)centro.X - 2, (int)centro.Y + 11, 4, 4),
                Color.Black * opacidad);
        }
    }

    // Corazón que suelta un enemigo: salta, cae al suelo o a una plataforma y desaparece a los 9 s.
    public class ObjetoVida
    {
        private static readonly string[] Forma =
        {
            "0110110",
            "1111111",
            "1111111",
            "0111110",
            "0011100",
            "0001000"
        };
        private const int Celda = 3;
        private const int Ancho = 7 * Celda;
        private const int Alto = 6 * Celda;
        private const float PiesSuelo = 455f;

        private Vector2 _pos;
        private float _vy = -180f;     // saltito al aparecer
        private float _restante = 9f;
        private float _tiempo;

        public bool Activa { get; set; } = true;
        public Rectangle Bounds => new Rectangle((int)_pos.X, (int)_pos.Y, Ancho, Alto);

        public ObjetoVida(Vector2 centro)
        {
            _pos = new Vector2(centro.X - Ancho / 2f, centro.Y - Alto / 2f);
        }

        public void Actualizar(float delta, List<Rectangle>? plataformas)
        {
            _tiempo += delta;
            _restante -= delta;
            if (_restante <= 0) { Activa = false; return; }

            float piesAntes = _pos.Y + Alto;
            _vy += 900f * delta;
            _pos.Y += _vy * delta;
            float piesAhora = _pos.Y + Alto;

            if (_vy >= 0)
            {
                if (plataformas != null)
                {
                    foreach (Rectangle p in plataformas)
                    {
                        bool encima = _pos.X + Ancho > p.Left && _pos.X < p.Right;
                        if (encima && piesAntes <= p.Top + 2f && piesAhora >= p.Top)
                        {
                            _pos.Y = p.Top - Alto;
                            _vy = 0;
                            break;
                        }
                    }
                }
                if (_pos.Y + Alto >= PiesSuelo)
                {
                    _pos.Y = PiesSuelo - Alto;
                    _vy = 0;
                }
            }

            _pos.X = MathHelper.Clamp(_pos.X, 0, 960 - Ancho);
        }

        public void Dibujar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // Parpadea en los últimos segundos para avisar que va a desaparecer.
            if (_restante < 2.5f && ((int)(_tiempo * 10) % 2 == 0)) return;

            for (int fila = 0; fila < Forma.Length; fila++)
                for (int col = 0; col < Forma[fila].Length; col++)
                    if (Forma[fila][col] == '1')
                    {
                        bool brillo = fila == 1 && col == 1;
                        spriteBatch.Draw(pixel,
                            new Rectangle((int)_pos.X + col * Celda, (int)_pos.Y + fila * Celda, Celda, Celda),
                            brillo ? new Color(255, 170, 180) : new Color(230, 50, 70));
                    }
        }
    }
}