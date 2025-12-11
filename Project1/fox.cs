using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Project1;

namespace Project1
{
    public enum FoxState
    {
        Seeking,   //Looking for nearest rabbit
        Chasing,   //Moving toward rabbit
        Eating,    //Has caught rabbit, eating
        Idle       //Resting
    }

    public class Fox
    {
        public Vector2 Position;
        public float Speed;
        public bool Alive;
        public FoxState State;

        private Rabbit targetRabbit;
        private float eatTimer;
        // Randomness of movement
        private static readonly Random rng = new Random();
        private Texture2D texture;

        private float hungerTimer;
        private const float STARVATION_TIME = 20f; //Fox dies without food (starves)

        //How long fox eats for (seconds)
        private const float EatDuration = 3f;

        private const float DrawScale = 2f; //same scale used when drawing the fox

        // Width and Height of the hitbox is derived from fox texture size :) 
        public int Width => (int)(texture?.Width * DrawScale ?? 16);
        public int Height => (int)(texture?.Height * DrawScale ?? 16);
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        //Breeding/eating tracking (similar to Rabbit)
        public bool HasEaten { get; private set; } = false;
        private float timeSinceAte = float.MaxValue;
        private const float BREED_WINDOW = 8f;      //seconds after eating when fox can breed
        private const float BREED_COOLDOWN = 12f;   //seconds cooldown after successful breeding
        public float BreedingCooldown { get; private set; } = 0f;

        public Fox(Vector2 startPos, Texture2D tex)
        {
            Position = startPos;
            texture = tex;
            Alive = true;
            State = FoxState.Seeking;
            Speed = 120f;//
            //breeding timers initialised
            HasEaten = false;
            timeSinceAte = float.MaxValue;
            BreedingCooldown = 0f;
        }

        //Fox movement
        public void Update(GameTime gameTime, List<Rabbit> rabbits, int mapPixelWidth, int mapPixelHeight)//within the borders
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            hungerTimer += dt;
            if (hungerTimer >= STARVATION_TIME) //Fox starves to death
            {
                Alive = false;
                return;
            }

            if (!Alive)
                return;

            //Update breeding/eating timers
            if (HasEaten)
            {
                timeSinceAte += dt;
                if (timeSinceAte > BREED_WINDOW)
                {
                    HasEaten = false;
                }
            }

            if (BreedingCooldown > 0f)
            {
                BreedingCooldown -= dt;
                if (BreedingCooldown < 0f) BreedingCooldown = 0f;
            }

            switch (State)
            {
                case FoxState.Seeking:
                    //Find nearest rabbit
                    targetRabbit = FindNearestRabbit(rabbits);

                    if (targetRabbit != null)
                        State = FoxState.Chasing;
                    else
                    {
                        //small random wander when no target found
                        Vector2 wander = new Vector2((float)(rng.NextDouble() - 0.5), (float)(rng.NextDouble() - 0.5));
                        if (wander.LengthSquared() > 0.0001f) wander.Normalize();
                        Position += wander * Speed * 0.25f * dt;
                        State = FoxState.Seeking;
                    }
                    break;

                case FoxState.Chasing:
                    if (targetRabbit == null || !targetRabbit.Alive)
                    {
                        targetRabbit = null;
                        State = FoxState.Seeking;
                        break;
                    }

                    //Move toward rabbit with a little steering jitter and speed variance
                    Vector2 direction = targetRabbit.Position - Position;
                    float distance = direction.Length();

                    if (distance > 0)
                        direction.Normalize();

                    //adds a jitter when chasing (more natural)
                    float jitter = (float)(rng.NextDouble() * 0.6 - 0.3); // -0.3 .. +0.3
                    Vector2 perp = new Vector2(-direction.Y, direction.X);
                    direction += perp * jitter;

                    if (direction.LengthSquared() > 0.0001f)
                        direction.Normalize();

                    //slight per-update speed variation
                    float speedFactor = 0.9f + (float)rng.NextDouble() * 0.2f; // ~0.9 .. 1.1

                    Position += direction * Speed * speedFactor * dt;

                    //Use hitbox intersection for reliable catch (was a bit biggy when I did it based of coordinates so using hotboxes and collsion)
                    if (targetRabbit != null && Bounds.Intersects(targetRabbit.Bounds))
                    {
                        targetRabbit.Alive = false;
                        hungerTimer = 0f;      // reset hunger immediately on successful catch
                        State = FoxState.Eating;
                        eatTimer = 0f;

                        //Mark that fox has eaten for breeding window
                        HasEaten = true;
                        timeSinceAte = 0f;
                    }
                    break;

                case FoxState.Eating:
                    eatTimer += dt;
                    if (eatTimer >= EatDuration)
                    {
                        hungerTimer = 0f; //Reset hunger after eating
                        State = FoxState.Seeking;
                    }
                    break;

                case FoxState.Idle:
                    
                    eatTimer += dt;
                    //small idle drifting (so not frozen)
                    Vector2 drift = new Vector2((float)(rng.NextDouble() - 0.5), (float)(rng.NextDouble() - 0.5));
                    if (drift.LengthSquared() > 0.0001f) drift.Normalize();
                    Position += drift * Speed * 0.15f * dt;

                    if (eatTimer >= rng.Next(2, 5))
                    {
                        eatTimer = 0f;
                        State = FoxState.Seeking;
                    }
                    break;
            }

            //Clamp inside map borders so fox can't go off-screen (uses sprite size)
            Position.X = MathHelper.Clamp(Position.X, 0f, Math.Max(0, mapPixelWidth - Width));
            Position.Y = MathHelper.Clamp(Position.Y, 0f, Math.Max(0, mapPixelHeight - Height));
        }

        private Rabbit FindNearestRabbit(List<Rabbit> rabbits)
        {
            Rabbit nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var r in rabbits)
            {
                if (!r.Alive)
                    continue;

                float dist = Vector2.Distance(Position, r.Position);
                if (dist < nearestDist)
                {
                    nearest = r;
                    nearestDist = dist;
                }
            }

            return nearest;
        }

        //Called by Game1 when two foxes successfully breed
        public void MarkBred()
        {
            HasEaten = false;
            timeSinceAte = float.MaxValue;
            BreedingCooldown = BREED_COOLDOWN;
        }

        public bool CanBreed()
        {
            return HasEaten && BreedingCooldown <= 0f && Alive;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!Alive)
                return;

            spriteBatch.Draw(texture, Position, null, Color.White, 0f, Vector2.Zero, DrawScale, SpriteEffects.None, 0f);
        }
    }
}