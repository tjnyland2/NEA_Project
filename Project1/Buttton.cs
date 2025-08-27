using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;


namespace Project1
{
    public class Button// Button Class
    {
        public Rectangle Bounds;
        public string Text;
        public SpriteFont Font;
        public Color TextColor = Color.White;
        public Color HoverColor = Color.Gray;
        public Color BackgroundColor = Color.DarkSlateGray;

        public bool IsHovered => Bounds.Contains(Mouse.GetState().Position);

        public Action OnClick;

        public void Draw(SpriteBatch spriteBatch)
        {
            var color = IsHovered ? HoverColor : BackgroundColor;
            Texture2D rect = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            rect.SetData(new[] { Color.White });

            spriteBatch.Draw(rect, Bounds, color);
            Vector2 textSize = Font.MeasureString(Text);
            Vector2 textPos = new Vector2(
                Bounds.X + (Bounds.Width - textSize.X) / 2,
                Bounds.Y + (Bounds.Height - textSize.Y) / 2
            );
            spriteBatch.DrawString(Font, Text, textPos, TextColor);
        }

        public void Update(MouseState currentMouse, MouseState previousMouse)
        {
            if (IsHovered &&
                currentMouse.LeftButton == ButtonState.Released &&
                previousMouse.LeftButton == ButtonState.Pressed)
            {
                OnClick?.Invoke();
            }
        }
    }
}
