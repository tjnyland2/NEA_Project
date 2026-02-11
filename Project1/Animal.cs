using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Project1
{
    
    public abstract class Animal
    {
        public Vector2 Position;
        public float Speed;
        public bool Alive = true;

        // Breeding/eating tracking
        public bool HasEaten { get; protected set; } = false;
        protected float timeSinceAte = float.MaxValue;
        protected float breedWindow = 8f;       // seconds after eating when can breed
        protected float breedCooldownTime = 10f; // default cooldown
        public float BreedingCooldown { get; protected set; } = 0f;

        //Starvation
        protected float hungerTimer = 0f;
        protected float starvationTime = 20f;

        //Bounds
        public abstract int Width { get; }
        public abstract int Height { get; }
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        //Update
        protected void UpdateCommonTimers(float deltaTime)
        {
            //hunger
            hungerTimer += deltaTime;
            if (hungerTimer >= starvationTime)
            {
                Alive = false;
            }

            //breeding window
            if (HasEaten)
            {
                timeSinceAte += deltaTime;
                if (timeSinceAte > breedWindow)
                    HasEaten = false;
            }

            //breeding cooldown
            if (BreedingCooldown > 0f)
            {
                BreedingCooldown -= deltaTime;
                if (BreedingCooldown < 0f) BreedingCooldown = 0f;
            }
        }

        //reset hunger
        protected void ResetHunger()
        {
            hungerTimer = 0f;
        }

        //Marked as bred
        protected void MarkBredCommon(float cooldown)
        {
            HasEaten = false;
            timeSinceAte = float.MaxValue;
            BreedingCooldown = cooldown;
        }

        //Check if animal can breed (if animal as eaten, is off cooldown and is ovbousally alive)
        protected bool CanBreedCommon()
        {
            return HasEaten && BreedingCooldown <= 0f && Alive;
        }

        // Ensures Position stays inside the provided pixel bounds (0..mapPixelWidth, 0..mapPixelHeight),
        // taking into account the sprite's Width/Height. Use this instead of duplicating clamp math.
        public void ClampToMap(int mapPixelWidth, int mapPixelHeight)
        {
            float maxX = Math.Max(0f, mapPixelWidth - Width);
            float maxY = Math.Max(0f, mapPixelHeight - Height);
            Position.X = MathHelper.Clamp(Position.X, 0f, maxX);
            Position.Y = MathHelper.Clamp(Position.Y, 0f, maxY);
        }
    }
}