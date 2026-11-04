using Microsoft.Xna.Framework;

namespace Juego
{
    // Plantada en el suelo, sin movimiento, escupe espinas hacia el jugador.
    public class FlorMalvada : EnemigoMalvado
    {
        private const float IntervaloDisparo = 2f;
        private const float VelEspina = 260f;
        private float _cooldown = 1.2f;

        public FlorMalvada(Animacion anim, Vector2 pies) : base(anim, 52f, pies) { }

        public override void Actualizar(float delta, Vector2 jugadorCentro, List<Espina> espinas)
        {
            ActualizarComun(delta);
            _anim.Actualizar(delta);
            _mirandoIzquierda = jugadorCentro.X < Posicion.X;

            _cooldown -= delta;
            if (_cooldown <= 0)
            {
                _cooldown = IntervaloDisparo;
                Vector2 boca = new Vector2(Posicion.X + (_mirandoIzquierda ? -1 : 1) * Ancho * 0.3f,
                                           Posicion.Y - Alto * 0.65f);
                Vector2 dir = jugadorCentro - boca;
                if (dir != Vector2.Zero) dir.Normalize();
                espinas.Add(new Espina(boca, dir * VelEspina));
            }
        }
    }
}
