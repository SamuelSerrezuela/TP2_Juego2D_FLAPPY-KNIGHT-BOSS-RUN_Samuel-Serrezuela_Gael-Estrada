using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Juego
{
    public abstract class EnemigoMalvado
    {
        public const int VidaMaxima = 5;            

       
        public Vector2 Posicion;
        public int Vida { get; private set; } = VidaMaxima;
        public bool Activo => Vida > 0;

        protected readonly Animacion _anim;
        protected readonly float _escala;
        protected bool _mirandoIzquierda;
      
        protected bool _spriteMiraIzquierda;
        private float _flash;
        private float _tiempo;

        protected float Ancho => _anim.Ancho * _escala;
        protected float Alto => _anim.Alto * _escala;

        public Rectangle Bounds => new Rectangle(
            (int)(Posicion.X - Ancho / 2f), (int)(Posicion.Y - Alto), (int)Ancho, (int)Alto);

        public Vector2 Centro => new Vector2(Posicion.X, Posicion.Y - Alto / 2f);

        protected EnemigoMalvado(Animacion anim, float altoEnPantalla, Vector2 pies)
        {
            _anim = anim;
            _escala = altoEnPantalla / anim.Alto;
            Posicion = pies;
        }

        public void RecibirDisparo()
        {
            Vida--;
            _flash = 0.12f;
        }

        
        public abstract void Actualizar(float delta, Vector2 jugadorCentro, List<Espina> espinas);

        protected void ActualizarComun(float delta)
        {
            _tiempo += delta;
            if (_flash > 0) _flash -= delta;
        }

        public virtual void Dibujar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            
            if (_flash > 0 && ((int)(_tiempo * 40) % 2 == 0)) return;

            var destino = new Vector2(Posicion.X - Ancho / 2f, Posicion.Y - Alto);
           
            _anim.Dibujar(spriteBatch, destino, _escala, _mirandoIzquierda != _spriteMiraIzquierda);
        }
    }

    
    public class Espina
    {
        private Vector2 _pos;
        private readonly Vector2 _vel;
        public bool Activa { get; set; } = true;

        public Rectangle Bounds => new Rectangle((int)_pos.X - 6, (int)_pos.Y - 3, 12, 6);

        public Espina(Vector2 pos, Vector2 vel) { _pos = pos; _vel = vel; }

        public void Actualizar(float delta)
        {
            _pos += _vel * delta;
            if (_pos.X < -100 || _pos.X > 1060 || _pos.Y < -100 || _pos.Y > 700) Activa = false;
        }

        public void Dibujar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            float rot = (float)Math.Atan2(_vel.Y, _vel.X);
           
            spriteBatch.Draw(pixel, _pos, null, Color.LimeGreen, rot,
                new Vector2(0.5f, 0.5f), new Vector2(14, 4), SpriteEffects.None, 0f);
        }
    }
}
