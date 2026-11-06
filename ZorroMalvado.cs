using Microsoft.Xna.Framework;

namespace Juego
{
    // Salta hacia el jugador cada 3,5 segundos.
    public class ZorroMalvado : EnemigoMalvado
    {
        private const float IntervaloSalto = 3.5f;
        private const float Gravedad = 1000f;      // igual que el jugador
        private const float FuerzaSalto = 430f;
        private const float VelMaxHorizontal = 260f;

        private readonly float _suelo;
        private float _temporizador = IntervaloSalto;
        private Vector2 _velocidad;
        private bool _enAire;

        public ZorroMalvado(Animacion anim, Vector2 pies) : base(anim, 40f, pies)
        {
            _suelo = pies.Y;
            _spriteMiraIzquierda = true;   // el sprite del zorro mira a la izquierda
        }

        public override void Actualizar(float delta, Vector2 jugadorCentro, List<Espina> espinas)
        {
            ActualizarComun(delta);

            if (!_enAire)
            {
                _anim.Actualizar(delta);
                _mirandoIzquierda = jugadorCentro.X < Posicion.X;
                _temporizador -= delta;
                if (_temporizador <= 0)
                {
                    _temporizador = IntervaloSalto;
                    _enAire = true;
                    float dx = jugadorCentro.X - Posicion.X;
                    
                    float tVuelo = 2f * FuerzaSalto / Gravedad;
                    float vx = MathHelper.Clamp(dx / tVuelo, -VelMaxHorizontal, VelMaxHorizontal);
                    _velocidad = new Vector2(vx, -FuerzaSalto);
                }
            }
            else
            {
                _velocidad.Y += Gravedad * delta;
                Posicion += _velocidad * delta;
                Posicion.X = MathHelper.Clamp(Posicion.X, 20, 940);
                if (Posicion.Y >= _suelo)
                {
                    Posicion.Y = _suelo;
                    _velocidad = Vector2.Zero;
                    _enAire = false;
                }
            }
        }
    }
}