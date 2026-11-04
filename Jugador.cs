using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Juego
{
    public class Jugador
    {
        // --- Animaciones ---
        private Animacion _caminar = null!;
        private Animacion _disparo = null!;
        private Animacion _salto = null!;
        private float _escala = 1f;
        private bool _caminando;
        private bool _disparando;
        private bool _disparoPendiente;

        // Disparo: en qué frame sale la bola y cuánto dura cada frame (más chico = más rápido).
        private const int FrameSalidaDisparo = 3;
        private const float DuracionFrameDisparo = 0.04f;

        // Posición de la boca dentro del sprite de disparo (0 a 1).
        private const float BocaX = 0.85f; // 0 = borde izquierdo, 1 = borde derecho
        private const float BocaY = 0.5f;  // 0 = arriba, 1 = abajo

        // Altura donde se apoya el jugador en el suelo (posición Y de su parte de arriba).
        private const float SueloY = 407f;

        // --- Dash (tecla K) ---
        private const float DashVelocidad = 650f;   // velocidad durante el dash
        private const float DashDuracion = 0.16f;   // cuánto dura (segundos)
        private const float DashRecarga = 0.7f;     // espera antes de poder repetirlo
        private float _dashRestante;
        private float _dashCooldown;
        private int _dashDireccion = 1;

        public void CargarSprite(GraphicsDevice graphicsDevice)
        {
            // Los nombres de carpeta deben coincidir exactamente con los de tu disco.
            string carpeta = Path.Combine(AppContext.BaseDirectory, "Sprites", "1 wordl", "prtotagonista");

            _caminar = Animacion.Cargar(graphicsDevice, Path.Combine(carpeta, "caminata_azul.png"), 3, 0.12f, true);
            _disparo = Animacion.Cargar(graphicsDevice, Path.Combine(carpeta, "disparo_azul.png"), 6, DuracionFrameDisparo, false);
            _salto = Animacion.CargarAutomatica(graphicsDevice, Path.Combine(carpeta, "salto_azul.png"), 0.2f, false);

            _escala = 48f / _caminar.Alto; // misma altura que la hitbox
        }

        // --- Estado del jugador ---
        public Vector2 Posicion;
        public const int VidaMaxima = 10;
        public int Vida { get; private set; } = VidaMaxima;
        public Rectangle Bounds => new Rectangle((int)Posicion.X, (int)Posicion.Y, 34, 48);
        public bool EnSuelo => _enSuelo;
        public List<Proyectil> Proyectiles { get; } = new();

        // 0 = recién usado, 1 = dash listo (para la barra del HUD).
        public float DashProgreso => _dashRestante > 0
            ? 0f
            : 1f - MathHelper.Clamp(_dashCooldown / DashRecarga, 0f, 1f);

        private float _velocidad = 230f;
        private float _velocidadVertical;
        private bool _enSuelo = true;
        private float _tiempoSinPlataformas; // mientras es > 0, atraviesa las plataformas hacia abajo
        private float _tiempoAnimacion;
        private int _direccion = 1;
        private float _tiempoInvulnerable;
        private int _contadorGolpes;

        public Jugador(Vector2 posicion)
        {
            Posicion = posicion;
        }

        public void Actualizar(KeyboardState teclado, KeyboardState anterior, float delta,
                               List<Rectangle> plataformas)
        {
            _tiempoAnimacion += delta;
            _tiempoInvulnerable -= delta;
            if (_tiempoInvulnerable < 0) _tiempoInvulnerable = 0;
            if (_tiempoSinPlataformas > 0) _tiempoSinPlataformas -= delta;
            if (_dashCooldown > 0) _dashCooldown -= delta;

            bool estabaEnSuelo = _enSuelo;
            _caminando = false;

            // Dash: K. Sirve en el suelo y en el aire; durante el dash eres invulnerable.
            bool dashNuevo = teclado.IsKeyDown(Keys.K) && anterior.IsKeyUp(Keys.K);
            if (dashNuevo && _dashCooldown <= 0 && _dashRestante <= 0)
            {
                _dashRestante = DashDuracion;
                _dashCooldown = DashRecarga;
                _dashDireccion = _direccion;
            }

            bool haciendoDash = _dashRestante > 0;

            if (haciendoDash)
            {
                Posicion.X += _dashDireccion * DashVelocidad * delta;
                _velocidadVertical = 0; // el dash es horizontal: sin caída mientras dura
                _dashRestante -= delta;
                _caminando = true;
            }
            else
            {
                if (teclado.IsKeyDown(Keys.Left) || teclado.IsKeyDown(Keys.A))
                {
                    Posicion.X -= _velocidad * delta;
                    _direccion = -1;
                    _caminando = true;
                }

                if (teclado.IsKeyDown(Keys.Right) || teclado.IsKeyDown(Keys.D))
                {
                    Posicion.X += _velocidad * delta;
                    _direccion = 1;
                    _caminando = true;
                }

                // Saltar: Espacio, W o flecha arriba.
                if ((teclado.IsKeyDown(Keys.Space) || teclado.IsKeyDown(Keys.Up) ||
                     teclado.IsKeyDown(Keys.W)) && _enSuelo)
                {
                    _velocidadVertical = -430;
                    _enSuelo = false;
                    _salto.Reiniciar(); // arranca la animación de salto
                }

                // Bajar de una plataforma: S o flecha abajo estando arriba de una.
                bool abajo = teclado.IsKeyDown(Keys.Down) || teclado.IsKeyDown(Keys.S);
                if (abajo && _enSuelo && Posicion.Y < SueloY - 1f)
                {
                    _tiempoSinPlataformas = 0.25f;
                    _enSuelo = false;
                    Posicion.Y += 2f;
                }

                // Gravedad.
                _velocidadVertical += 1000 * delta;
            }

            float piesAntes = Posicion.Y + 48f;
            Posicion.Y += _velocidadVertical * delta;
            float piesAhora = Posicion.Y + 48f;

            bool aterrizo = false;

            // Plataformas de un solo sentido: solo aterriza si viene cayendo
            // y sus pies cruzan la superficie de la plataforma en este frame.
            if (_velocidadVertical >= 0 && _tiempoSinPlataformas <= 0)
            {
                foreach (Rectangle p in plataformas)
                {
                    bool encima = Posicion.X + 34 > p.Left && Posicion.X < p.Right;
                    if (encima && piesAntes <= p.Top + 2f && piesAhora >= p.Top)
                    {
                        Posicion.Y = p.Top - 48f;
                        _velocidadVertical = 0;
                        aterrizo = true;
                        break;
                    }
                }
            }

            // Suelo.
            if (Posicion.Y >= SueloY)
            {
                Posicion.Y = SueloY;
                _velocidadVertical = 0;
                aterrizo = true;
            }

            _enSuelo = aterrizo;

            // Si se cayó de una plataforma sin saltar, la animación de salto arranca de nuevo.
            if (estabaEnSuelo && !_enSuelo && _velocidadVertical >= 0 && !haciendoDash)
                _salto.Reiniciar();

            Posicion.X = MathHelper.Clamp(Posicion.X, 0, 926);

            // Disparo: J empieza la animación, pero se ignora si hay una bola todavía por salir.
            bool disparoNuevo = teclado.IsKeyDown(Keys.J) && anterior.IsKeyUp(Keys.J);
            if (disparoNuevo && !_disparoPendiente)
            {
                _disparando = true;
                _disparoPendiente = true;
                _disparo.Reiniciar();
            }

            // Animación de salto: avanza mientras está en el aire.
            if (!_enSuelo) _salto.Actualizar(delta);

            // Animación de caminar: solo en el suelo; si está quieto, vuelve al primer frame.
            if (_caminando && _enSuelo) _caminar.Actualizar(delta);
            else _caminar.Reiniciar();

            // Animación de disparo.
            if (_disparando)
            {
                _disparo.Actualizar(delta);

                // La bola sale en el frame indicado.
                if (_disparoPendiente && _disparo.FrameActual >= FrameSalidaDisparo)
                {
                    float ancho = _disparo.Ancho * _escala;
                    float alto = _disparo.Alto * _escala;
                    float izquierda = Posicion.X + (34 - ancho) / 2f;
                    float arriba = Posicion.Y + 48 - alto; // alineado por los pies

                    float bocaX = _direccion == 1
                        ? izquierda + ancho * BocaX
                        : izquierda + ancho * (1f - BocaX);
                    float bocaY = arriba + alto * BocaY;

                    Proyectiles.Add(new Proyectil(new Vector2(bocaX, bocaY), _direccion));
                    _disparoPendiente = false;
                }

                if (_disparo.Terminada)
                    _disparando = false;
            }
        }

        public void RecibirGolpe(int dano = 1)
        {
            // Durante el dash no te pueden golpear (sirve para esquivar).
            if (_tiempoInvulnerable > 0 || _dashRestante > 0) return;

            Vida -= dano;
            _tiempoInvulnerable = 0.8f;
            _contadorGolpes++;
        }

        public void Curar(int cantidad = 3)
        {
            Vida = Math.Min(VidaMaxima, Vida + cantidad);
        }

        public void Dibujar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            bool parpadea = _tiempoInvulnerable > 0 && ((int)(_tiempoAnimacion * 12) % 2 == 0);
            if (parpadea) return;

            // Estelas del dash: rayitas blancas detrás del jugador.
            if (_dashRestante > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    int largo = 28 - i * 5;
                    int x = _dashDireccion == 1
                        ? (int)Posicion.X - largo - i * 6
                        : (int)Posicion.X + 34 + i * 6;
                    spriteBatch.Draw(pixel, new Rectangle(x, (int)Posicion.Y + 8 + i * 10, largo, 3),
                        Color.White * 0.6f);
                }
            }

            // Prioridad: disparo > salto (en el aire) > caminar.
            Animacion actual = _disparando ? _disparo : (!_enSuelo ? _salto : _caminar);

            // Centrado en la hitbox y alineado por los pies.
            float anchoDibujado = actual.Ancho * _escala;
            float altoDibujado = actual.Alto * _escala;
            var destino = new Vector2(
                Posicion.X + (34 - anchoDibujado) / 2f,
                Posicion.Y + 48 - altoDibujado);

            actual.Dibujar(spriteBatch, destino, _escala, _direccion != 1);
        }
    }
}