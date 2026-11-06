
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Juego
{
    public enum OpcionMenu
    {
        Ninguna,
        Jugar,
        Salir
    }

    public class Menu
    {
        private int _opcionSeleccionada = 0;

        private const int AnchoPantalla = 960;
        private const int AltoPantalla = 540;

        public OpcionMenu Actualizar(
            KeyboardState teclado,
            KeyboardState tecladoAnterior)
        {
            // Cambiar la opción hacia arriba.
            if (teclado.IsKeyDown(Keys.Up) &&
                tecladoAnterior.IsKeyUp(Keys.Up))
            {
                _opcionSeleccionada--;

                if (_opcionSeleccionada < 0)
                    _opcionSeleccionada = 1;
            }

            // Cambiar la opción hacia abajo.
            if (teclado.IsKeyDown(Keys.Down) &&
                tecladoAnterior.IsKeyUp(Keys.Down))
            {
                _opcionSeleccionada++;

                if (_opcionSeleccionada > 1)
                    _opcionSeleccionada = 0;
            }

            // Confirmar la opción seleccionada.
            if (teclado.IsKeyDown(Keys.Enter) &&
                tecladoAnterior.IsKeyUp(Keys.Enter))
            {
                if (_opcionSeleccionada == 0)
                    return OpcionMenu.Jugar;

                return OpcionMenu.Salir;
            }

            return OpcionMenu.Ninguna;
        }

        public void Dibujar(SpriteBatch spriteBatch, Texture2D pixel)
        {
            // Fondo del menú.
            spriteBatch.Draw(
                pixel,
                new Rectangle(0, 0, AnchoPantalla, AltoPantalla),
                new Color(17, 22, 39));

            // Panel central.
            spriteBatch.Draw(
                pixel,
                new Rectangle(210, 90, 540, 360),
                new Color(31, 40, 65));

            // Bordes del panel.
            spriteBatch.Draw(
                pixel,
                new Rectangle(210, 90, 540, 5),
                new Color(115, 151, 205));

            spriteBatch.Draw(
                pixel,
                new Rectangle(210, 445, 540, 5),
                new Color(115, 151, 205));

            // Título.
            DibujarTextoCentrado(
                spriteBatch, pixel, "FLAPPY KNIGHT",
                170, 5, Color.White);

            // Primera opción: JUGAR.
            Color colorJugar = _opcionSeleccionada == 0
                ? Color.Gold
                : Color.LightGray;

            spriteBatch.Draw(
                pixel,
                new Rectangle(340, 270, 280, 55),
                _opcionSeleccionada == 0
                    ? new Color(65, 79, 112)
                    : new Color(42, 52, 78));

            DibujarTextoCentrado(
                spriteBatch, pixel, "JUGAR",
                285, 4, colorJugar);

            // Segunda opción: SALIR.
            Color colorSalir = _opcionSeleccionada == 1
                ? Color.Gold
                : Color.LightGray;

            spriteBatch.Draw(
                pixel,
                new Rectangle(340, 345, 280, 55),
                _opcionSeleccionada == 1
                    ? new Color(65, 79, 112)
                    : new Color(42, 52, 78));

            DibujarTextoCentrado(
                spriteBatch, pixel, "SALIR",
                360, 4, colorSalir);

            // Indicaciones de controles.
            DibujarTextoCentrado(
                spriteBatch, pixel, "FLECHAS Y ENTER",
                420, 2, Color.LightGray);
        }

        private void DibujarTextoCentrado(
            SpriteBatch spriteBatch,
            Texture2D pixel,
            string texto,
            int y,
            int escala,
            Color color)
        {
            int anchoTexto = texto.Length * 6 * escala;
            int x = (AnchoPantalla - anchoTexto) / 2;

            DibujarTexto(
                spriteBatch, pixel, texto,
                x, y, escala, color);
        }

        private void DibujarTexto(
            SpriteBatch spriteBatch,
            Texture2D pixel,
            string texto,
            int x,
            int y,
            int escala,
            Color color)
        {
            for (int i = 0; i < texto.Length; i++)
            {
                string[] patron = ObtenerPatron(texto[i]);

                for (int fila = 0; fila < patron.Length; fila++)
                {
                    for (int columna = 0; columna < patron[fila].Length; columna++)
                    {
                        if (patron[fila][columna] == '1')
                        {
                            spriteBatch.Draw(
                                pixel,
                                new Rectangle(
                                    x + (i * 6 + columna) * escala,
                                    y + fila * escala,
                                    escala,
                                    escala),
                                color);
                        }
                    }
                }
            }
        }

        private string[] ObtenerPatron(char letra)
        {
            switch (letra)
            {
                case 'A':
                    return new[] { "01110", "10001", "10001", "11111", "10001", "10001", "10001" };
                case 'C':
                    return new[] { "01111", "10000", "10000", "10000", "10000", "10000", "01111" };
                case 'E':
                    return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "11111" };
                case 'F':
                    return new[] { "11111", "10000", "10000", "11110", "10000", "10000", "10000" };
                case 'G':
                    return new[] { "01111", "10000", "10000", "10111", "10001", "10001", "01111" };
                case 'H':
                    return new[] { "10001", "10001", "10001", "11111", "10001", "10001", "10001" };
                case 'I':
                    return new[] { "11111", "00100", "00100", "00100", "00100", "00100", "11111" };
                case 'J':
                    return new[] { "00111", "00010", "00010", "00010", "10010", "10010", "01100" };
                case 'K':
                    return new[] { "10001", "10010", "10100", "11000", "10100", "10010", "10001" };
                case 'L':
                    return new[] { "10000", "10000", "10000", "10000", "10000", "10000", "11111" };
                case 'N':
                    return new[] { "10001", "11001", "11001", "10101", "10011", "10011", "10001" };
                case 'P':
                    return new[] { "11110", "10001", "10001", "11110", "10000", "10000", "10000" };
                case 'R':
                    return new[] { "11110", "10001", "10001", "11110", "10100", "10010", "10001" };
                case 'S':
                    return new[] { "01111", "10000", "10000", "01110", "00001", "00001", "11110" };
                case 'T':
                    return new[] { "11111", "00100", "00100", "00100", "00100", "00100", "00100" };
                case 'U':
                    return new[] { "10001", "10001", "10001", "10001", "10001", "10001", "01110" };
                case 'Y':
                    return new[] { "10001", "10001", "01010", "00100", "00100", "00100", "00100" };
                default:
                    return new[] { "00000", "00000", "00000", "00000", "00000", "00000", "00000" };
            }
        }
    }
}
