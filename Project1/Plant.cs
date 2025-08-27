using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;

namespace Project1
{
    public class Plant
    {
        public Vector2 Position;
        public string Type; // "Grass" or "Thorns"
        public float SpawnTime;
        public Texture2D Texture;
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, 10, 10); // 10x10 plant size

        public Plant(Vector2 pos, string type, float spawnTime, Texture2D texture)
        {
            Position = pos;
            Type = type;
            SpawnTime = spawnTime;
            Texture = texture;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Bounds, Color.White);
        }
    }
}
