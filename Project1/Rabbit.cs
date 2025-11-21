using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Project1
{
    public enum RabbitState //States of the rabbit 
    {
        Seeking,
        MovingToPlant,
        Eating,
        Idle
    }

    public class Rabbit // Rabbit Class
    {
        public Vector2 Position;
        public Vector2 TargetPosition;
        public RabbitState State;
        public Texture2D Texture;
        public Plant TargetPlant;
        public float EatingTimer;
        public float EatingDuration = 2f; // How long they spend eating
        public float Speed = 30f; // pixels per second
        public bool Alive = true; //If the rabbit is alive 
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, 8, 8);

        private List<Vector2> currentPath;
        private int currentPathIndex;
        private const int GRID_SIZE = 5; // map
        private const float FOX_DETECTION_RANGE = 100f; // Range to detect foxes

        public Rabbit(Vector2 startPosition, Texture2D texture)
        {
            Position = startPosition; //spawn
            Texture = texture;
            State = RabbitState.Seeking; //first state
            EatingTimer = 0f;
            currentPath = new List<Vector2>();
            currentPathIndex = 0;
        }

        public void Update(GameTime gameTime, List<Plant> plants, List<Fox> foxes, int mapWidth, int mapHeight) //Update method for the rabbits
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds; // Time (change) since last update

            Fox nearestFox = FindNearestFox(foxes);
            if (nearestFox != null && Vector2.Distance(Position, nearestFox.Position) < FOX_DETECTION_RANGE)
            {
                // Run away from fox
                FleeFromFox(nearestFox, deltaTime);
                return; // Skip other behaviors when fleeing
            }

            switch (State)
            {
                case RabbitState.Seeking: // Look for nearest plant
                    SeekNearestPlant(plants, mapWidth, mapHeight);
                    break;

                case RabbitState.MovingToPlant:// Move along path to plant
                    MoveAlongPath(deltaTime);
                    break;

                case RabbitState.Eating:// Eating the plant
                    EatingTimer += deltaTime;
                    if (EatingTimer >= EatingDuration)
                    {
                        // Finished eating, remove the plant and go back to seeking
                        if (TargetPlant != null)
                        {
                            plants.Remove(TargetPlant);
                            TargetPlant = null;
                        }
                        State = RabbitState.Seeking;
                        EatingTimer = 0f;
                    }
                    break;

                case RabbitState.Idle:// If idle 
                 
                    State = RabbitState.Seeking; //Go back to seeking
                    break;
            }
        }

        private void SeekNearestPlant(List<Plant> plants, int mapWidth, int mapHeight)
        {
            if (plants.Count == 0) return;

            // Find the nearest plant:
            Plant nearestPlant = null;
            float nearestDistance = float.MaxValue;

            foreach (var plant in plants) 
            {
                float distance = Vector2.Distance(Position, plant.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPlant = plant;
                }
            }

            if (nearestPlant != null)
            {
                TargetPlant = nearestPlant;
                // Calculate path
                currentPath = FindPathDirect(Position, nearestPlant.Position);
                if (currentPath.Count > 0)
                {
                    currentPathIndex = 0;
                    State = RabbitState.MovingToPlant;
                }
            }
        }

        private void MoveAlongPath(float deltaTime)
        {
            if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
            {
                State = RabbitState.Seeking;
                return;
            }

            Vector2 targetPoint = currentPath[currentPathIndex];
            Vector2 direction = targetPoint - Position;
            float distance = direction.Length();

            if (distance < 2f) // Close enough to current waypoint
            {
                currentPathIndex++;
                if (currentPathIndex >= currentPath.Count)
                {
                    // Reached final destination
                    if (TargetPlant != null && Vector2.Distance(Position, TargetPlant.Position) < 15f)
                    {
                        State = RabbitState.Eating;
                        EatingTimer = 0f;
                    }
                    else
                    {
                        State = RabbitState.Seeking;
                    }
                }
            }
            else
            {
                // Move towards current waypoint
                direction.Normalize();
                Position += direction * Speed * deltaTime;
            }
        }
        private Fox FindNearestFox(List<Fox> foxes)
        {
            Fox nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var fox in foxes)
            {
                if (!fox.Alive) continue;

                float dist = Vector2.Distance(Position, fox.Position);
                if (dist < nearestDist)
                {
                    nearest = fox;
                    nearestDist = dist;
                }
            }
            return nearest;
        }
        private void FleeFromFox(Fox fox, float deltaTime)// Running awway from fox
        {
            // Run in opposite direction from fox
            Vector2 fleeDirection = Position - fox.Position;
            if (fleeDirection.Length() > 0)
            {
                fleeDirection.Normalize();
                Position += fleeDirection * Speed * 1.5f * deltaTime; // 1.5x speed when fleeing as they are running
            }
        }

        private List<Vector2> FindPathDirect(Vector2 start, Vector2 end) //Pathfinding (use of waypoints) (this took well long to make so happy about it)
        {
            List<Vector2> path = new List<Vector2>();

            //add some waypoints between start and end
            Vector2 direction = end - start;
            float distance = direction.Length();

            if (distance > 20f) // Only add waypoints if distance large (bigger than 30 pixels)
            {
                direction.Normalize();

                // Add waypoints every 30 pixels
                for (float i = 30f; i < distance; i += 30f)
                {
                    Vector2 waypoint = start + direction * i;
                    path.Add(waypoint);//adds them
                }
            }

            path.Add(end); // Always add the final destination
            return path;
        }

        public void Draw(SpriteBatch spriteBatch)//Draw method for the rabbits
        {
            Color drawColor = Color.White;
            spriteBatch.Draw(Texture, Position, null, Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
        }
    }
}