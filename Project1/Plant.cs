
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
        public const int MaxAssigned = 2; // maximum rabbits that may target this plant concurrently

        public Vector2 Position;
        public string Type; // "Grass" or "Thorns"
        public float SpawnTime;
        public Texture2D Texture;

        // Size (in pixels) used for logic and drawing. Default preserved for backward compatibility.
        public int Size { get; private set; }
        
        // Bounds now derived from Size so collision/assignment and drawing match
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Size, Size);

        // number of rabbits that currently have this plant as their TargetPlant
        private int assignedRabbits;
        public int AssignedRabbits => assignedRabbits;

        public Plant(Vector2 pos, string type, float spawnTime, Texture2D texture, int size = 1)
        {
            Position = pos;
            Type = type;
            SpawnTime = spawnTime;
            Texture = texture;
            Size = Math.Max(0, size);
            assignedRabbits = 0;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw texture stretched to the Size rectangle. Keeps logic and visuals in sync.
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