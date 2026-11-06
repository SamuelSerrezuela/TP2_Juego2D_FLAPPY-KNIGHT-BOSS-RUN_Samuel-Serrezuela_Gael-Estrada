using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.IO;
using System;

namespace Juego
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch = null!;
        private Texture2D _pixel = null!;
        private Texture2D? _fondo;   // Imagen de fondo cargada
        private Jugador _jugador = null!;
        private GestorRondas _rondas = null!;   // enemigos: 4 rondas
        private JefeArdilla? _ardilla;          // jefe final del juego
        private static readonly Rectangle PlataformaJefe = new Rectangle(380, 258, 200, 14);

        // Cada plataforma es un rectángulo: (x, y de la superficie, ancho, alto).
        private readonly List<Rectangle> _plataformas = new()
        {
            // Piso bajo
            new Rectangle(50, 390, 160, 14),
            new Rectangle(380, 390, 200, 14),
            new Rectangle(750, 390, 160, 14),
            // Piso medio
            new Rectangle(215, 325, 150, 14),
            new Rectangle(595, 325, 150, 14),
            // Piso alto
            PlataformaJefe
        };

        private int _nivel = 1;   // el juego tiene un solo nivel
        private string _estado = "Jugando";
        private KeyboardState _tecladoAnterior;

        private readonly Color[] _fondos =
        {
            new Color(35, 47, 75),
            new Color(39, 65, 57),
            new Color(66, 39, 67)
        };

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 960;
            _graphics.PreferredBackBufferHeight = 540;
            Window.Title = "Aventura de tres niveles";
        }

        protected override void Initialize()
        {
            _jugador = new Jugador(new Vector2(100, 390));
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Cargar imagen de fondo desde la carpeta Sprites (fondo.jpg)
            string rutaFondo = Path.Combine(AppContext.BaseDirectory, "Sprites", "1 world", "fondo.jpg");
            if (File.Exists(rutaFondo))
            {
                using var stream = File.OpenRead(rutaFondo);
                _fondo = Texture2D.FromStream(GraphicsDevice, stream);
            }

            // Textura blanca de 1x1. Se usa para dibujar formas sencillas
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _jugador.CargarSprite(GraphicsDevice);

            // Los nombres de carpeta deben coincidir exactamente con los de tu disco.
            Proyectil.CargarTextura(GraphicsDevice,
                Path.Combine(AppContext.BaseDirectory, "Sprites", "1 wordl", "prtotagonista", "disparo.png"));

            // Enemigos: flor, zorro y halcón malvados (carpetas dentro de "1 wordl").
            _rondas = new GestorRondas(GraphicsDevice,
                Path.Combine(AppContext.BaseDirectory, "Sprites", "1 wordl"));
            IniciarNivelActual();
        }

        // Arranca las 4 rondas; el jefe aparece al terminarlas.
        private void IniciarNivelActual()
        {
            _ardilla = null;
            _rondas.IniciarNivel(_nivel);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState teclado = Keyboard.GetState();
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (teclado.IsKeyDown(Keys.Escape))
                Exit();

            if (_estado != "Jugando")
            {
                if (teclado.IsKeyDown(Keys.Enter) &&
                    _tecladoAnterior.IsKeyUp(Keys.Enter))
                {
                    if (_estado == "Perdiste")
                    {
                        ReiniciarJuego();
                    }
                    else if (_estado == "Ganaste")
                    {
                        Exit();
                    }
                }

                _tecladoAnterior = teclado;
                base.Update(gameTime);
                return;
            }

            _jugador.Actualizar(teclado, _tecladoAnterior, delta, _plataformas);

            foreach (Proyectil proyectil in _jugador.Proyectiles)
                proyectil.Actualizar(delta);

            _jugador.Proyectiles.RemoveAll(p => p.FueraDePantalla);

            // Rondas: mueve enemigos y espinas, y resuelve disparos y golpes al jugador.
            _rondas.Actualizar(delta, _jugador);
            // Al terminar la ronda 4 aparece el jefe.
            if (_rondas.NivelCompletado && _ardilla == null)
                _ardilla = new JefeArdilla(GraphicsDevice,
                    Path.Combine(AppContext.BaseDirectory, "Sprites"), PlataformaJefe);

            // La ardilla maneja sus propios ataques, bellotas y daño.
            if (_ardilla != null && _ardilla.Vivo)
                _ardilla.Actualizar(delta, _jugador, _plataformas);

            _jugador.Proyectiles.RemoveAll(p => !p.Activo);

            if (_jugador.Vida <= 0)
                _estado = "Perdiste";

            // Un solo nivel: al derrotar al jefe se completa el juego.
            if (_ardilla != null && !_ardilla.Vivo)
                _estado = "Ganaste";

            _tecladoAnterior = teclado;
            base.Update(gameTime);
        }

        private void ReiniciarJuego()
        {
            _nivel = 1;
            _estado = "Jugando";
            _jugador = new Jugador(new Vector2(100, 390));
            _jugador.CargarSprite(GraphicsDevice);
            IniciarNivelActual();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_fondos[_nivel - 1]);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            // Dibujar la imagen de fondo si se cargó correctamente
            if (_fondo != null)
            {
                _spriteBatch.Draw(_fondo, new Rectangle(0, 0, 960, 540), Color.White);
            }

            // Suelo.
            DibujarRectangulo(new Rectangle(0, 455, 960, 85), new Color(38, 35, 43));

            // Decoración sencilla de fondo.
            for (int i = 0; i < 9; i++)
                DibujarRectangulo(new Rectangle(i * 125, 430, 65, 25),
                    new Color(52, 54, 65));

            // Plataformas: cuerpo oscuro con un borde claro arriba para que se vea dónde se pisa.
            foreach (Rectangle p in _plataformas)
            {
                DibujarRectangulo(p, new Color(88, 78, 112));
                DibujarRectangulo(new Rectangle(p.X, p.Y, p.Width, 4), new Color(150, 140, 185));
            }

            _jugador.Dibujar(_spriteBatch, _pixel);

            _rondas.Dibujar(_spriteBatch, _pixel);

            foreach (Proyectil proyectil in _jugador.Proyectiles)
                proyectil.Dibujar(_spriteBatch, _pixel);

            if (_ardilla != null && _ardilla.Vivo)
                _ardilla.Dibujar(_spriteBatch, _pixel);

            // HUD hecho con barras para no depender todavía de una fuente externa.
            // Vida del jugador.
            DibujarRectangulo(new Rectangle(20, 20, 160, 18), Color.DarkRed);
            DibujarRectangulo(new Rectangle(20, 20, Math.Max(0, _jugador.Vida * 16), 18),
                Color.LimeGreen);

            // Progreso de rondas: 4 cuadritos, los completados en dorado.
            for (int i = 0; i < GestorRondas.RondasPorNivel; i++)
            {
                bool hecha = i < _rondas.RondaActual - 1 || _rondas.NivelCompletado;
                bool actual = i == _rondas.RondaActual - 1 && !_rondas.NivelCompletado;
                DibujarRectangulo(new Rectangle(20 + i * 22, 48, 18, 8),
                    hecha ? Color.Gold : (actual ? Color.SteelBlue : Color.Gray));
            }

            // Recarga del dash (cian = listo).
            DibujarRectangulo(new Rectangle(20, 62, 88, 6), Color.Black);
            DibujarRectangulo(new Rectangle(20, 62, (int)(88 * _jugador.DashProgreso), 6),
                _jugador.DashProgreso >= 1f ? Color.Cyan : Color.SteelBlue);

            // Mensajes visuales sencillos. Enter reinicia si perdiste.
            if (_estado == "Perdiste")
            {
                DibujarRectangulo(new Rectangle(260, 190, 440, 120), Color.DarkRed);
                DibujarRectangulo(new Rectangle(290, 220, 380, 12), Color.White);
            }
            else if (_estado == "Ganaste")
            {
                DibujarRectangulo(new Rectangle(260, 190, 440, 120), Color.DarkGreen);
                DibujarRectangulo(new Rectangle(340, 255, 280, 12), Color.White);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DibujarRectangulo(Rectangle rectangulo, Color color)
        {
            _spriteBatch.Draw(_pixel, rectangulo, color);
        }
    }
}