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
        public const int MaxAssigned = 1; // maximum rabbits that may target this plant concurrently (I set this to 1 after during some testing 100 rabbits went to the same plant and then all started breeding, it was choas)

        public Vector2 Position;
        public string Type; // "Grass" or "Thorns"
        public float SpawnTime;
        public Texture2D Texture;
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, 10, 10); // 10x10 plant size

        // number of rabbits that currently have this plant as their TargetPlant
        private int assignedRabbits;
        public int AssignedRabbits => assignedRabbits;

        public Plant(Vector2 pos, string type, float spawnTime, Texture2D texture)
        {
            Position = pos;
            Type = type;
            SpawnTime = spawnTime;
            Texture = texture;
            assignedRabbits = 0;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Texture, Bounds, Color.White);
        }

        // Try to reserve this plant for a rabbit. Returns true if reservation succeeded.
        public bool TryAssign()
        {
            if (assignedRabbits < MaxAssigned)
            {
                assignedRabbits++;
                return true;
            }
            return false;
        }

        // Release a previously reserved slot
        public void ReleaseAssigned()
        {
            if (assignedRabbits > 0) assignedRabbits--;
        }
    }
}
