using Microsoft.Xna.Framework;

namespace Juego
{
    // Vuela directo al jugador. Cada 3 s se desliza (embestida) en línea recta.
    public class HalconMalvado : EnemigoMalvado
    {
        private enum Estado { Persiguiendo, Deslizando }

        private const float TiempoAtaque = 3f;
        private const float DuracionDeslizar = 0.7f;
        private const float VelPersecucion = 110f;
        private const float VelDeslizar = 480f;

        private Estado _estado = Estado.Persiguiendo;
        private float _temporizador = TiempoAtaque;
        private float _tiempoDeslizando;
        private Vector2 _dirAtaque;

        public HalconMalvado(Animacion anim, Vector2 pies) : base(anim, 36f, pies) { }

        public override void Actualizar(float delta, Vector2 jugadorCentro, List<Espina> espinas)
        {
            ActualizarComun(delta);
            _anim.Actualizar(delta);

            Vector2 dir = jugadorCentro - Centro;
            if (dir != Vector2.Zero) dir.Normalize();

            if (_estado == Estado.Persiguiendo)
            {
                _mirandoIzquierda = dir.X < 0;
                Posicion += dir * VelPersecucion * delta;
                _temporizador -= delta;
                if (_temporizador <= 0)
                {
                    _estado = Estado.Deslizando;
                    _tiempoDeslizando = DuracionDeslizar;
                    _dirAtaque = dir;           // dirección fijada al iniciar el ataque
                }
            }
            else
            {
                Posicion += _dirAtaque * VelDeslizar * delta;
                _tiempoDeslizando -= delta;
                if (_tiempoDeslizando <= 0)
                {
                    _estado = Estado.Persiguiendo;
                    _temporizador = TiempoAtaque;
                }
            }

            
            Posicion.X = MathHelper.Clamp(Posicion.X, 20, 940);
            Posicion.Y = MathHelper.Clamp(Posicion.Y, Alto, 455f);
        }
    }
}
