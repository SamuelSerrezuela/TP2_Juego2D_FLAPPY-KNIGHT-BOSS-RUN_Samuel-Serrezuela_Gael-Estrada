using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Juego
{
    public class Animacion
    {
        private readonly Texture2D _hoja;
        private readonly Rectangle[] _cuadros;
        private readonly float _duracionFrame;
        private readonly bool _repetir;
        private float _tiempo;

        public int Alto { get; }
        public int Ancho => _cuadros[FrameActual].Width;
        public bool Terminada { get; private set; }

        // Hoja con frames del mismo tamaño.
        public Animacion(Texture2D hoja, int frames, float duracionFrame, bool repetir)
        {
            _hoja = hoja;
            _duracionFrame = duracionFrame;
            _repetir = repetir;
            Alto = hoja.Height;

            int anchoFrame = hoja.Width / frames;
            _cuadros = new Rectangle[frames];
            for (int i = 0; i < frames; i++)
                _cuadros[i] = new Rectangle(i * anchoFrame, 0, anchoFrame, hoja.Height);
        }

        // Hoja con recortes ya calculados.
        private Animacion(Texture2D hoja, Rectangle[] cuadros, float duracionFrame, bool repetir)
        {
            _hoja = hoja;
            _cuadros = cuadros;
            _duracionFrame = duracionFrame;
            _repetir = repetir;
            Alto = hoja.Height;
        }

        public static Animacion Cargar(GraphicsDevice gd, string ruta, int frames,
                                       float duracionFrame, bool repetir)
        {
            using var stream = File.OpenRead(ruta);
            var textura = Texture2D.FromStream(gd, stream);
            return new Animacion(textura, frames, duracionFrame, repetir);
        }

        // Detecta solos los dibujos buscando columnas vacías (transparentes) entre ellos.
        // Si no encuentra separaciones, divide la hoja en "framesDeRespaldo" partes iguales.
        public static Animacion CargarAutomatica(GraphicsDevice gd, string ruta,
                                                 float duracionFrame, bool repetir,
                                                 int framesDeRespaldo = 4)
        {
            using var stream = File.OpenRead(ruta);
            var textura = Texture2D.FromStream(gd, stream);

            var pixeles = new Color[textura.Width * textura.Height];
            textura.GetData(pixeles);

            var cuadros = new List<Rectangle>();
            int inicio = -1;

            for (int x = 0; x <= textura.Width; x++)
            {
                bool ocupada = false;
                if (x < textura.Width)
                {
                    for (int y = 0; y < textura.Height && !ocupada; y++)
                        ocupada = pixeles[y * textura.Width + x].A > 10;
                }

                if (ocupada && inicio < 0)
                {
                    inicio = x;
                }
                else if (!ocupada && inicio >= 0)
                {
                    // Ignora rayitas sueltas de menos de 6 píxeles de ancho.
                    if (x - inicio >= 6)
                        cuadros.Add(new Rectangle(inicio, 0, x - inicio, textura.Height));
                    inicio = -1;
                }
            }

            if (cuadros.Count <= 1 && framesDeRespaldo > 1)
                return new Animacion(textura, framesDeRespaldo, duracionFrame, repetir);

            return new Animacion(textura, cuadros.ToArray(), duracionFrame, repetir);
        }

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
            var origen = _cuadros[FrameActual];
            var efecto = voltear ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(_hoja, posicion, origen, Color.White, 0f, Vector2.Zero, escala, efecto, 0f);
        }
    }
}