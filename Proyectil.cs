using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Juego
{
    public class Proyectil
    {
        private static Texture2D? _textura;
        private static Texture2D? _texturaRoja;   // versión roja para el disparo mortal (tecla P)
        private const float Velocidad = 520f;
        private const float AltoDibujado = 24f; // tamaño de la bola en pantalla

        private static float Escala => _textura == null ? 1f : AltoDibujado / _textura.Height;

        private Vector2 _centro;
        private readonly int _direccion;

        public bool Activo { get; set; } = true;
        // Disparo de pruebas: mata de un solo impacto a cualquier enemigo o jefe.
        public bool Mortal { get; }
        public bool FueraDePantalla => _centro.X < -100 || _centro.X > 1060;

        public Rectangle Bounds
        {
            get
            {
                if (_textura == null)
                    return new Rectangle((int)_centro.X - 10, (int)_centro.Y - 5, 20, 10);

                // Hitbox un poco más chica que el dibujo, para que se sienta justo.
                float ancho = _textura.Width * Escala * 0.7f;
                float alto = AltoDibujado * 0.7f;
                return new Rectangle((int)(_centro.X - ancho / 2), (int)(_centro.Y - alto / 2),
                                     (int)ancho, (int)alto);
            }
        }

        // "centro" es el punto desde donde sale la bola (la boca del jugador).
        public Proyectil(Vector2 centro, int direccion, bool mortal = false)
        {
            _centro = centro;
            _direccion = direccion;
            Mortal = mortal;
        }

        public static void CargarTextura(GraphicsDevice graphicsDevice, string ruta)
        {
            using var stream = File.OpenRead(ruta);
            _textura = Texture2D.FromStream(graphicsDevice, stream);
            _texturaRoja = CrearVersionRoja(graphicsDevice, _textura);
        }

        // Misma imagen pero en rojo: cambia el tono y conserva brillo, transparencia y destellos blancos.
        private static Texture2D CrearVersionRoja(GraphicsDevice graphicsDevice, Texture2D origen)
        {
            var px = new Color[origen.Width * origen.Height];
            origen.GetData(px);

            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i];
                if (c.A == 0) continue;

                float max = Math.Max(c.R, Math.Max(c.G, c.B)) / 255f;
                float min = Math.Min(c.R, Math.Min(c.G, c.B)) / 255f;
                float saturacion = max <= 0f ? 0f : (max - min) / max;
                float s = Math.Min(1f, saturacion * 1.2f);

                px[i] = new Color(
                    (int)(max * 255f),
                    (int)(max * (1f - s) * 255f),
                    (int)(max * (1f - s) * 255f),
                    (int)c.A);
            }

            var roja = new Texture2D(graphicsDevice, origen.Width, origen.Height);
            roja.SetData(px);
            return roja;
        }

        public void Actualizar(float delta)
        {
            _centro.X += _direccion * Velocidad * delta;
        }

        public void Dibujar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            Texture2D? tex = Mortal ? (_texturaRoja ?? _textura) : _textura;

            if (tex == null)
            {
                spriteBatch.Draw(pixel, Bounds, Mortal ? Color.Red : Color.Cyan);
                return;
            }

            var origen = new Vector2(tex.Width / 2f, tex.Height / 2f);
            var efecto = _direccion == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(tex, _centro, null, Color.White, 0f, origen, Escala, efecto, 0f);
        }
    }
}