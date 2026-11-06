
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Juego
{
    public enum ResultadoMenu
    {
        Ninguno,
        Jugar,
        Salir
    }

    public class Menu
    {
        private int opcion = 0;

        public ResultadoMenu Actualizar(
            KeyboardState teclado,
            KeyboardState anterior)
        {
            if (teclado.IsKeyDown(Keys.Up) &&
                anterior.IsKeyUp(Keys.Up))
            {
                opcion = 0;
            }

            if (teclado.IsKeyDown(Keys.Down) &&
                anterior.IsKeyUp(Keys.Down))
            {
                opcion = 1;
            }

            if (teclado.IsKeyDown(Keys.Enter) &&
                anterior.IsKeyUp(Keys.Enter))
            {
                if (opcion == 0)
                    return ResultadoMenu.Jugar;
                else
                    return ResultadoMenu.Salir;
            }

            return ResultadoMenu.Ninguno;
        }

        public void Dibujar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            spriteBatch.Draw(pixel,
                new Rectangle(0, 0, 960, 540),
                new Color(25, 30, 50));

            // Título y botones.
            spriteBatch.Draw(pixel,
                new Rectangle(250, 100, 460, 340),
                new Color(45, 55, 80));

            spriteBatch.Draw(pixel,
                new Rectangle(340, 265, 280, 55),
                opcion == 0 ? Color.DarkSlateBlue : Color.Gray);

            spriteBatch.Draw(pixel,
                new Rectangle(340, 345, 280, 55),
                opcion == 1 ? Color.DarkSlateBlue : Color.Gray);

            // Indicador de la opción elegida.
            if (opcion == 0)
                spriteBatch.Draw(pixel,
                    new Rectangle(350, 280, 8, 25), Color.Gold);
            else
                spriteBatch.Draw(pixel,
                    new Rectangle(350, 360, 8, 25), Color.Gold);
        }
    }
}
