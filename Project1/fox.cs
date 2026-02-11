using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Project1
{
    public enum FoxState
    {
        Seeking,   //Looking for nearest rabbit
        Chasing,   //Moving toward rabbit
        Eating,    //Has caught rabbit, eating
        Idle       //Resting
    }

    public class Fox : Animal//Inhereance from Animal Class
    {
        public FoxState State;

        private Rabbit targetRabbit;
        private float eatTimer;
        //Randomness of movement to make it seems more natural
        private static readonly Random rng = new Random();
        private Texture2D texture;

        //How long fox eats for
        private const float EatDuration = 3f;

        private const float DrawScale = 2f; //same scale used when drawing the fox

        //Width and Height of the hitbox is derived from fox texture size 
        public override int Width => (int)(texture?.Width * DrawScale ?? 16);
        public override int Height => (int)(texture?.Height * DrawScale ?? 16);

        //Fox specfic timers
        private const float FOX_BREED_WINDOW = 8f;
        private const float FOX_BREED_COOLDOWN = 12f;
        private const float FOX_STARVATION_TIME = 20f;

        public Fox(Vector2 startPos, Texture2D tex)
        {
            Position = startPos;
            texture = tex;
            Alive = true;
            State = FoxState.Seeking;

            //specific speeds and timers
            Speed = 120f;
            starvationTime = FOX_STARVATION_TIME;
            breedWindow = FOX_BREED_WINDOW;
            breedCooldownTime = FOX_BREED_COOLDOWN;
            HasEaten = false;
            timeSinceAte = float.MaxValue;
            BreedingCooldown = 0f;
            hungerTimer = 0f;
        }

        //Fox movement
        public void Update(GameTime gameTime, List<Rabbit> rabbits, int mapPixelWidth, int mapPixelHeight)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //update common timers
            UpdateCommonTimers(dt);

            if (!Alive)
                return;

            switch (State)
            {
                case FoxState.Seeking:
                    //Find nearest rabbit
                    targetRabbit = FindNearestRabbit(rabbits);

                    if (targetRabbit != null)
                        State = FoxState.Chasing;
                    else
                    {
                        //small random wander when no target found (more natural movement)
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

                    //Move toward rabbit with a little steering jitter and speed variance (for more natural movement)
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

                    //Hitbox Intersection (catching rabbit)
                    if (targetRabbit != null && Bounds.Intersects(targetRabbit.Bounds))
                    {
                        targetRabbit.Alive = false;
                        ResetHunger();      //reset hunger on successful catch
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
                        ResetHunger(); //Reset hunger after eating
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
            ClampToMap(mapPixelWidth, mapPixelHeight);
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
            MarkBredCommon(FOX_BREED_COOLDOWN);
        }

        public bool CanBreed()
        {
            return CanBreedCommon();
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!Alive)
                return;

            spriteBatch.Draw(texture, Position, null, Color.White, 0f, Vector2.Zero, DrawScale, SpriteEffects.None, 0f);
        }
    }
}