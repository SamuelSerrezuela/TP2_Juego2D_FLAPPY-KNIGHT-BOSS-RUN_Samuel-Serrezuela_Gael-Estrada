using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Juego
{
    // Maneja las 4 rondas de enemigos.
    // Cada ronda: sale una alerta, luego aparecen los enemigos, y cuando mueren todos empieza la siguiente.
    public class GestorRondas
    {
        public const int RondasPorNivel = 4;
        private const float Suelo = 455f;
        private const float TiempoAlerta = 1.6f;

        
        private static readonly int[][] Rondas =
        {
            new[] { 2, 0, 0 },   // ronda 1
            new[] { 2, 1, 0 },   // ronda 2
            new[] { 2, 2, 1 },   // ronda 3
            new[] { 3, 3, 2 },   // ronda 4
        };

        public int RondaActual { get; private set; }
        public bool NivelCompletado { get; private set; }
        public List<EnemigoMalvado> Enemigos { get; } = new();   // enemigos en juego
        public List<Espina> Espinas { get; } = new();

        private readonly List<EnemigoMalvado> _enEspera = new(); // ya creados, esperando que termine la alerta
        private readonly GraphicsDevice _gd;
        private readonly string _carpeta;
        private readonly Texture2D? _texAlerta;
        private readonly Random _rnd = new();
        private float _espera;
        private float _reloj;

        public GestorRondas(GraphicsDevice graphicsDevice, string carpetaNivel)
        {
            _gd = graphicsDevice;
            _carpeta = carpetaNivel;

            string ruta = Path.Combine(carpetaNivel, "alerta_peligro.png");
            if (File.Exists(ruta))
            {
                using var stream = File.OpenRead(ruta);
                _texAlerta = Texture2D.FromStream(graphicsDevice, stream);
            }
        }

        public void IniciarNivel(int nivel)
        {
            RondaActual = 0;
            NivelCompletado = false;
            Enemigos.Clear();
            Espinas.Clear();
            _enEspera.Clear();
            NuevaRonda();
        }

        // Crea los enemigos de la ronda; quedan en espera mientras se muestra la alerta.
        private void NuevaRonda()
        {
            RondaActual++;
            int[] n = Rondas[RondaActual - 1];

            for (int i = 0; i < n[0]; i++)   // flores: en el suelo, al azar
                _enEspera.Add(new FlorMalvada(Anim("flor malvada", "flores malvadas rework.png"),
                    new Vector2(_rnd.Next(80, 880), Suelo)));

            for (int i = 0; i < n[1]; i++)   // zorros: por los costados
                _enEspera.Add(new ZorroMalvado(Anim("zorro malvado", "zorro malvado caminando.png"),
                    new Vector2(i % 2 == 0 ? 40 : 920, Suelo)));

            for (int i = 0; i < n[2]; i++)   // halcones: desde arriba
                _enEspera.Add(new HalconMalvado(Anim("halcon malvado", "halcon malvado caminando.png"),
                    new Vector2(_rnd.Next(200, 880), 60)));

            _espera = TiempoAlerta;
        }

        private Animacion Anim(string carpeta, string archivo) =>
            Animacion.CargarAutomatica(_gd, Path.Combine(_carpeta, carpeta, archivo), 0.12f, true);

        public void Actualizar(float delta, Jugador jugador)
        {
            if (NivelCompletado) return;
            _reloj += delta;

            // 1) Cuando termina la alerta, los enemigos entran en juego.
            if (_enEspera.Count > 0)
            {
                _espera -= delta;
                if (_espera <= 0)
                {
                    Enemigos.AddRange(_enEspera);
                    _enEspera.Clear();
                }
            }

            // 2) Mover enemigos y espinas.
            Vector2 objetivo = jugador.Bounds.Center.ToVector2();
            foreach (var e in Enemigos) e.Actualizar(delta, objetivo, Espinas);
            foreach (var s in Espinas) s.Actualizar(delta);

            // 3) Disparos del jugador contra enemigos (5 disparos = muerte).
            foreach (var d in jugador.Proyectiles)
                foreach (var e in Enemigos)
                    if (d.Activo && e.Activo && d.Bounds.Intersects(e.Bounds))
                    {
                        e.RecibirDisparo();
                        d.Activo = false;
                    }

            // 4) Enemigos y espinas lastiman al jugador.
            foreach (var e in Enemigos)
                if (e.Bounds.Intersects(jugador.Bounds)) jugador.RecibirGolpe();
            foreach (var s in Espinas)
                if (s.Activa && s.Bounds.Intersects(jugador.Bounds)) { s.Activa = false; jugador.RecibirGolpe(); }

            // 5) Quitar lo que ya no sirve.
            Enemigos.RemoveAll(e => !e.Activo);
            Espinas.RemoveAll(s => !s.Activa);
            jugador.Proyectiles.RemoveAll(p => !p.Activo || p.FueraDePantalla);

            // 6) Si no queda nadie, siguiente ronda (o fin de las rondas).
            if (_enEspera.Count == 0 && Enemigos.Count == 0)
            {
                if (RondaActual >= RondasPorNivel) { NivelCompletado = true; Espinas.Clear(); }
                else NuevaRonda();
            }
        }

        public void Dibujar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            foreach (var e in Enemigos) e.Dibujar(spriteBatch, pixel);
            foreach (var s in Espinas) s.Dibujar(spriteBatch, pixel);

            // Alerta de peligro sobre el lugar donde va a aparecer cada enemigo.
            float opacidad = 0.55f + 0.45f * Math.Abs((float)Math.Sin(_reloj * 8f));
            foreach (var e in _enEspera)
            {
                if (_texAlerta != null)
                    spriteBatch.Draw(_texAlerta, e.Centro, null, Color.White * opacidad, 0f,
                        new Vector2(_texAlerta.Width / 2f, _texAlerta.Height / 2f),
                        44f / _texAlerta.Height, SpriteEffects.None, 0f);
                else
                    spriteBatch.Draw(pixel, new Rectangle((int)e.Centro.X - 16, (int)e.Centro.Y - 16, 32, 32),
                        Color.Red * opacidad);
            }
        }
    }
}
