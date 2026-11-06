using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Juego
{
    // Reproduce una animación: una imagen con varios dibujos en fila, mostrados uno tras otro.
    public class Animacion
    {
        private readonly Texture2D _hoja;
        private readonly Rectangle[] _cuadros;   // un rectángulo por cada dibujo de la hoja
        private readonly float _duracionFrame;   // segundos que dura cada dibujo
        private readonly bool _repetir;          // true = vuelve a empezar, false = se queda en el último
        private float _tiempo;

        public int Alto => _hoja.Height;
        public int Ancho => _cuadros[FrameActual].Width;
        public bool Terminada { get; private set; }

        private Animacion(Texture2D hoja, Rectangle[] cuadros, float duracionFrame, bool repetir)
        {
            _hoja = hoja;
            _cuadros = cuadros;
            _duracionFrame = duracionFrame;
            _repetir = repetir;
        }

        

        // Tú dices cuántos frames tiene la hoja (todos del mismo ancho).
        public static Animacion Cargar(GraphicsDevice gd, string ruta, int frames,
                                       float duracionFrame, bool repetir)
        {
            Texture2D hoja = CargarTextura(gd, ruta);
            return new Animacion(hoja, DividirEnPartes(hoja, frames), duracionFrame, repetir);
        }

        // Detecta solos los dibujos (separados por columnas transparentes).
        // Si no logra separarlos, divide la hoja en 4 partes iguales.
        public static Animacion CargarAutomatica(GraphicsDevice gd, string ruta,
                                                 float duracionFrame, bool repetir)
        {
            Texture2D hoja = CargarTextura(gd, ruta);
            Rectangle[] cuadros = BuscarDibujos(hoja);
            if (cuadros.Length <= 1) cuadros = DividirEnPartes(hoja, 4);
            return new Animacion(hoja, cuadros, duracionFrame, repetir);
        }

        private static Texture2D CargarTextura(GraphicsDevice gd, string ruta)
        {
            using var stream = File.OpenRead(ruta);
            return Texture2D.FromStream(gd, stream);
        }

        private static Rectangle[] DividirEnPartes(Texture2D hoja, int partes)
        {
            int ancho = hoja.Width / partes;
            var cuadros = new Rectangle[partes];
            for (int i = 0; i < partes; i++)
                cuadros[i] = new Rectangle(i * ancho, 0, ancho, hoja.Height);
            return cuadros;
        }

        // Recorre la imagen de izquierda a derecha: una columna con algo dibujado empieza un dibujo,
        // y una columna vacía lo termina.
        private static Rectangle[] BuscarDibujos(Texture2D hoja)
        {
            var pixeles = new Color[hoja.Width * hoja.Height];
            hoja.GetData(pixeles);

            var cuadros = new List<Rectangle>();
            int inicio = -1;

            for (int x = 0; x <= hoja.Width; x++)
            {
                bool hayDibujo = x < hoja.Width && ColumnaTieneDibujo(pixeles, hoja, x);

                if (hayDibujo && inicio < 0)
                {
                    inicio = x;
                }
                else if (!hayDibujo && inicio >= 0)
                {
                    if (x - inicio >= 6)   // ignora rayitas sueltas
                        cuadros.Add(new Rectangle(inicio, 0, x - inicio, hoja.Height));
                    inicio = -1;
                }
            }
            return cuadros.ToArray();
        }

        private static bool ColumnaTieneDibujo(Color[] pixeles, Texture2D hoja, int x)
        {
            for (int y = 0; y < hoja.Height; y++)
                if (pixeles[y * hoja.Width + x].A > 10) return true;
            return false;
        }

        // ---------- Uso en el juego ----------

        // Número del dibujo que toca mostrar ahora.
        public int FrameActual
        {
            get
            {
                int f = (int)(_tiempo / _duracionFrame);
                return _repetir ? f % _cuadros.Length : Math.Min(f, _cuadros.Length - 1);
            }
        }

        public void Reiniciar()
        {
            _tiempo = 0;
            Terminada = false;
        }

        public void Actualizar(float delta)
        {
            if (Terminada) return;
            _tiempo += delta;
            if (!_repetir && _tiempo >= _cuadros.Length * _duracionFrame)
                Terminada = true;
        }

        public void Dibujar(SpriteBatch spriteBatch, Vector2 posicion, float escala, bool voltear)
        {
            var efecto = voltear ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(_hoja, posicion, _cuadros[FrameActual], Color.White, 0f,
                Vector2.Zero, escala, efecto, 0f);
        }
    }
}