using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Juego
{
    // Jefe del nivel 1: la ardilla. Está parada en una plataforma y, cada 2 segundos,
    // avisa y hace caer varias bellotas del cielo (una justo sobre el jugador).
    // Además tiene daño físico: si el jugador la toca, recibe un golpe.
    public class JefeArdilla
    {
        public const int VidaMaxima = 100;       // disparos que aguanta
        private const float Suelo = 455f;       // altura (Y) del piso
        private const float AltoBoss = 190f;    // alto del jefe en pantalla
        private const int CantidadBellotas = 50; // cuántas bellotas caen en cada ataque

        private enum Estado { Esperando, Avisando, Atacando }

        public int Vida { get; private set; } = VidaMaxima;
        public bool Vivo => Vida > 0;
        public Rectangle Bounds => new Rectangle((int)_pies.X - 50, (int)_pies.Y - 130, 100, 130);

        private readonly Random _azar = new();
        private readonly Texture2D? _texBase, _texAtaque;
        private readonly Vector2 _pies;      // donde apoya los pies (centro de su plataforma)

        private Estado _estado = Estado.Esperando;
        private float _temporizador = 2f;    // segundos que faltan para cambiar de estado
        private float _flash;                // tiempo que se ve rojo al recibir un disparo

        // Posición de cada bellota que va a caer.
        private readonly List<Vector2> _bellotas = new();

        // Caja de colisión de una bellota (se usa al mover y al dibujar).
        private static Rectangle CajaBellota(Vector2 b) =>
            new Rectangle((int)b.X - 8, (int)b.Y - 8, 16, 16);

        public JefeArdilla(GraphicsDevice gd, string carpetaSprites, Rectangle plataforma)
        {
            _pies = new Vector2(plataforma.Center.X, plataforma.Top);
            _texBase = CargarSprite(gd, carpetaSprites, "ardilla base");
            _texAtaque = CargarSprite(gd, carpetaSprites, "ardilla lanzando");
        }

        // Busca "nombre.png" en la carpeta (y subcarpetas). Si no está, devuelve null.
        private static Texture2D? CargarSprite(GraphicsDevice gd, string carpeta, string nombre)
        {
            if (!Directory.Exists(carpeta)) return null;

            string? ruta = Directory.GetFiles(carpeta, nombre + ".png", SearchOption.AllDirectories)
                                    .FirstOrDefault();
            if (ruta == null) return null;

            using var stream = File.OpenRead(ruta);
            return Texture2D.FromStream(gd, stream);
        }

        // "plataformas" no se usa; queda para no romper la llamada que ya hace Game1.
        public void Actualizar(float delta, Jugador jugador, List<Rectangle>? plataformas = null)
        {
            if (!Vivo) return;

            if (_flash > 0) _flash -= delta;
            _temporizador -= delta;

            if (_estado == Estado.Esperando && _temporizador <= 0)
                PrepararBellotas(jugador);
            else if (_estado == Estado.Avisando && _temporizador <= 0)
                _estado = Estado.Atacando;
            else if (_estado == Estado.Atacando)
                MoverBellotas(delta, jugador);

            // CAMBIO: daño físico. Si el jugador toca a la ardilla, recibe un golpe.
            if (Bounds.Intersects(jugador.Bounds))
                jugador.RecibirGolpe();

            RecibirDisparos(jugador);
        }

        // Crea las bellotas. La primera cae sobre el jugador y las demás en lugares al azar.
        // Cada una arranca más arriba que la anterior (90 px), así no caen todas juntas.
        private void PrepararBellotas(Jugador jugador)
        {
            _bellotas.Clear();

            for (int i = 0; i < CantidadBellotas; i++)
            {
                float x = i == 0 ? jugador.Bounds.Center.X : _azar.Next(60, 900);
                _bellotas.Add(new Vector2(x, -20f - i * 90f));
            }

            _estado = Estado.Avisando;
            _temporizador = 0.8f;   // tiempo para que el jugador reaccione
        }

        // Mueve todas las bellotas de la lista.
        private void MoverBellotas(float delta, Jugador jugador)
        {
            
            for (int i = _bellotas.Count - 1; i >= 0; i--)
            {
                _bellotas[i] += new Vector2(0f, 500f * delta);   // velocidad * tiempo = distancia

                if (CajaBellota(_bellotas[i]).Intersects(jugador.Bounds))
                {
                    jugador.RecibirGolpe();
                    _bellotas.RemoveAt(i);
                }
                else if (_bellotas[i].Y > Suelo)
                {
                    _bellotas.RemoveAt(i);   // tocó el suelo
                }
            }

            if (_bellotas.Count == 0)   // cuando cayeron todas, termina el ataque
            {
                _estado = Estado.Esperando;
                _temporizador = 2f;
            }
        }

        // Cada disparo del jugador que toca al jefe le saca 1 de vida.
        private void RecibirDisparos(Jugador jugador)
        {
            foreach (Proyectil p in jugador.Proyectiles)
            {
                if (p.Activo && p.Bounds.Intersects(Bounds))
                {
                    Vida--;
                    _flash = 0.1f;
                    p.Activo = false;
                }
            }
        }

        public void Dibujar(SpriteBatch sb, Texture2D pixel)
        {
            if (!Vivo) return;

            // El jefe: dibujo normal o de ataque. Si falta el PNG, un bloque marrón.
            Texture2D? tex = (_estado == Estado.Esperando ? _texBase : _texAtaque) ?? _texBase;
            if (tex != null)
            {
                Color color = _flash > 0 ? Color.Red : Color.White;
                sb.Draw(tex, _pies, null, color, 0f, new Vector2(tex.Width / 2f, tex.Height),
                        AltoBoss / tex.Height, SpriteEffects.None, 0f);
            }
            else
            {
                sb.Draw(pixel, Bounds, _flash > 0 ? Color.Red : Color.SaddleBrown);
            }

            // Las bellotas cayendo.
            if (_estado == Estado.Atacando)
            {
                foreach (Vector2 b in _bellotas)
                    sb.Draw(pixel, CajaBellota(b), Color.Peru);
            }

            // Barra de vida arriba al centro.
            sb.Draw(pixel, new Rectangle(240, 14, 480, 18), Color.DarkRed);
            sb.Draw(pixel, new Rectangle(240, 14, (int)(480f * Vida / VidaMaxima), 18), Color.OrangeRed);
        }
    }
}