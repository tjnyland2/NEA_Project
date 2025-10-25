using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Project1;

namespace Project1
{
    public enum FoxState
    {
        Seeking,   // Looking for nearest rabbit
        Chasing,   // Moving toward rabbit
        Eating,    // Has caught rabbit, eating
        Idle       // Resting
    }

    public class Fox
    {
        public Vector2 Position;
        public float Speed;
        public bool Alive;
        public FoxState State;

        private Rabbit targetRabbit;
        private float eatTimer;
        private Random rand;
        private Texture2D texture;

        // How long fox eats for (seconds)
        private const float EatDuration = 3f;

        public Fox(Vector2 startPos, Texture2D tex)
        {
            Position = startPos;
            texture = tex;
            Alive = true;
            State = FoxState.Seeking;
            Speed = 40f;
            rand = new Random();
        }

        public void Update(GameTime gameTime, List<Rabbit> rabbits)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (!Alive)
                return;

            switch (State)
            {
                case FoxState.Seeking:
                    // Find nearest rabbit
                    targetRabbit = FindNearestRabbit(rabbits);

                    if (targetRabbit != null)
                        State = FoxState.Chasing;
                    else
                        State = FoxState.Idle;
                    break;

                case FoxState.Chasing:
                    if (targetRabbit == null || !targetRabbit.Alive)
                    {
                        targetRabbit = null;
                        State = FoxState.Seeking;
                        break;
                    }

                    // Move toward rabbit
                    Vector2 direction = targetRabbit.Position - Position;
                    float distance = direction.Length();

                    if (distance > 0)
                        direction.Normalize();

                    Position += direction * Speed * dt;

                    // Check if close enough to catch
                    if (distance < 10f)
                    {
                        // Eat the rabbit
                        targetRabbit.Alive = false;
                        State = FoxState.Eating;
                        eatTimer = 0f;
                    }
                    break;

                case FoxState.Eating:
                    eatTimer += dt;
                    if (eatTimer >= EatDuration)
                    {
                        // Done eating, look for next target
                        State = FoxState.Seeking;
                    }
                    break;

                case FoxState.Idle:
                    // Idle for a short random time, then start seeking again
                    eatTimer += dt;
                    if (eatTimer >= rand.Next(2, 5))
                    {
                        eatTimer = 0f;
                        State = FoxState.Seeking;
                    }
                    break;
            }
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

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!Alive)
                return;

            Color tint = Color.OrangeRed;
            spriteBatch.Draw(texture, Position, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
        }
    }
}
