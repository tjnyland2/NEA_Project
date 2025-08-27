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

    public class Rabbit
    {
        public Vector2 Position;
        public Vector2 TargetPosition;
        public RabbitState State;
        public Texture2D Texture;
        public Plant TargetPlant;
        public float EatingTimer;
        public float EatingDuration = 2f; // How long they spend eating
        public float Speed = 30f; // pixels per second
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, 8, 8);

        private List<Vector2> currentPath;
        private int currentPathIndex;
        private const int GRID_SIZE = 5; // map

        public Rabbit(Vector2 startPosition, Texture2D texture)
        {
            Position = startPosition; //spawn
            Texture = texture;
            State = RabbitState.Seeking; //first state
            EatingTimer = 0f;
            currentPath = new List<Vector2>();
            currentPathIndex = 0;
        }

        public void Update(GameTime gameTime, List<Plant> plants, int mapWidth, int mapHeight)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (State)
            {
                case RabbitState.Seeking:
                    SeekNearestPlant(plants, mapWidth, mapHeight);
                    break;

                case RabbitState.MovingToPlant:
                    MoveAlongPath(deltaTime);
                    break;

                case RabbitState.Eating:
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

                case RabbitState.Idle:
                 
                    State = RabbitState.Seeking;
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

        public void Draw(SpriteBatch spriteBatch)
        {
            Color drawColor = Color.White;

            // Change color based on state for visual feedback
            switch (State)
            {
                case RabbitState.Eating:
                    drawColor = Color.Yellow;
                    break;
                case RabbitState.MovingToPlant:
                    drawColor = Color.LightBlue;
                    break;
                case RabbitState.Seeking:
                    drawColor = Color.White;
                    break;
            }

            spriteBatch.Draw(Texture, Bounds, drawColor);
        }
    }
}