using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameOfLifeFaster
{
    class TextBox
    {
        private string text;

        private readonly SpriteFont font;

        private readonly Texture2D pixel;

        private readonly int border_Size;

        private Color border_Color;

        public Color Background_Color { get; set; }
        public Vector2 Position { get; set; }
        public Vector2 String_Size { get; }

        private Rectangle rectangle = new Rectangle(0,0,0,0);

        public TextBox(string _text, SpriteFont _font, Texture2D _pixel, int _borderSize, Color _border, Color _background)
        {
            text = _text;
            font = _font;
            pixel = _pixel;
            border_Size = _borderSize;
            border_Color = _border;
            Background_Color = _background;

            String_Size = MeasureString();
        }

        public TextBox(string _text, SpriteFont _font, Vector2 vector2, Texture2D _pixel, int _borderSize, Color _border, Color _background)
        {
            text = _text;
            font = _font;
            Position = vector2;
            pixel = _pixel;
            border_Size = _borderSize;
            border_Color = _border;
            Background_Color = _background;

            String_Size = MeasureString();
            rectangle = GetRectangle();
        }

        public void Update(string text_To_Update)
        {
            text = text_To_Update;
        }

        public void Draw(SpriteBatch sprite)
        {
            sprite.Draw(pixel, new Rectangle(rectangle.X - border_Size, rectangle.Y - border_Size, rectangle.Width + border_Size * 2, rectangle.Height + border_Size * 2), border_Color);
            sprite.Draw(pixel, rectangle, Background_Color);
            sprite.DrawString(font, text, Position, Color.White);
        }

        public void SetPosition(Vector2 _position)
        {
            Position = _position;
            rectangle = GetRectangle();
        }

        private Vector2 MeasureString()
        {
            return font.MeasureString(text);
        }
        private Rectangle GetRectangle()
        {
            return new Rectangle((int)Position.X, (int)Position.Y, (int)String_Size.X, (int)String_Size.Y);
        }
    }
}
