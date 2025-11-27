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

        private const float DrawScale = 0.7f; // scale used when drawing the rabbit

        // Hitbox height and width size based on texture size and draw scale
        public int Width => (int)(Texture?.Width * DrawScale ?? 8);//was 8
        public int Height => (int)(Texture?.Height * DrawScale ?? 8);//was 8
        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

        // Breeding/eating tracking
        public bool HasEaten { get; private set; } = false;
        private float timeSinceAte = float.MaxValue;
        //CHANGED VALUES
        private const float BREED_WINDOW = 8f;      // seconds after eating when rabbit can breed
        private const float BREED_COOLDOWN = 10f;   // seconds cooldown after successful breeding
        public float BreedingCooldown { get; private set; } = 0f;

        private List<Vector2> currentPath;
        private int currentPathIndex;
        private const int GRID_SIZE = 5; // map
        private const float FOX_DETECTION_RANGE = 100f; // Range to detect foxes

        // Starvation (similar approach to Fox)
        private float hungerTimer = 0f;
        private const float STARVATION_TIME = 20f; // seconds until rabbit dies without food

        public Rabbit(Vector2 startPosition, Texture2D texture)
        {
            Position = startPosition; //spawn
            Texture = texture;
            State = RabbitState.Seeking; //first state
            EatingTimer = 0f;
            currentPath = new List<Vector2>();
            currentPathIndex = 0;
            HasEaten = false;
            timeSinceAte = float.MaxValue;
            BreedingCooldown = 0f;
            hungerTimer = 0f;
        }

        // mapPixelWidth/mapPixelHeight are in pixels (not tiles)
        // updated signature: receives list of rabbits so it can avoid same-species collisions
        public void Update(GameTime gameTime, List<Plant> plants, List<Fox> foxes, List<Rabbit> rabbits, int mapPixelWidth, int mapPixelHeight)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds; // Time (change) since last update

            // If already dead, skip
            if (!Alive)
                return;

            // Hunger updates: starve if exceed starvation time (same approach as Fox)
            hungerTimer += deltaTime;
            if (hungerTimer >= STARVATION_TIME)
            {
                Alive = false;
                return;
            }

            // Update timers related to breeding/eating
            if (HasEaten)
            {
                timeSinceAte += deltaTime;
                if (timeSinceAte > BREED_WINDOW)
                {
                    HasEaten = false;
                }
            }

            if (BreedingCooldown > 0f)
            {
                BreedingCooldown -= deltaTime;
                if (BreedingCooldown < 0f) BreedingCooldown = 0f;
            }

            Fox nearestFox = FindNearestFox(foxes);
            if (nearestFox != null && Vector2.Distance(Position, nearestFox.Position) < FOX_DETECTION_RANGE)
            {
                // Run away from fox (avoid other rabbits while fleeing)
                FleeFromFox(nearestFox, deltaTime, rabbits, foxes, mapPixelWidth, mapPixelHeight);
                // ensure inside bounds and skip other behaviours when fleeing
                Position.X = MathHelper.Clamp(Position.X, 0f, Math.Max(0, mapPixelWidth - Width));
                Position.Y = MathHelper.Clamp(Position.Y, 0f, Math.Max(0, mapPixelHeight - Height));
                return; // Skip other behaviors when fleeing (as thats the survival priority)
            }

            switch (State)
            {
                case RabbitState.Seeking: // Look for nearest plant
                    SeekNearestPlant(plants, mapPixelWidth, mapPixelHeight);
                    break;

                case RabbitState.MovingToPlant:// Move along path to plant
                    MoveAlongPath(deltaTime, rabbits, foxes, mapPixelWidth, mapPixelHeight);
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

                        // Mark as having eaten for the breeding window
                        HasEaten = true;
                        timeSinceAte = 0f;

                        // Reset hunger on successful eating
                        hungerTimer = 0f;

                        State = RabbitState.Seeking;
                        EatingTimer = 0f;
                    }
                    break;

                case RabbitState.Idle:// If idle 
                 
                    State = RabbitState.Seeking; //Go back to seeking
                    break;
            }

            // Clamp inside map borders so rabbits don't go off-screen (uses sprite size)
            //Clamp ensures a value stays between min and max values set (figured this one out from reading an acticle from codecademy)
            Position.X = MathHelper.Clamp(Position.X, 0f, Math.Max(0, mapPixelWidth - Width));
            Position.Y = MathHelper.Clamp(Position.Y, 0f, Math.Max(0, mapPixelHeight - Height));
        }

        private void SeekNearestPlant(List<Plant> plants, int mapPixelWidth, int mapPixelHeight)
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

        // Attempt to move to proposed position, avoiding collisions with other rabbits and foxes
        private void TryMove(Vector2 proposedPosition, List<Rabbit> rabbits, List<Fox> foxes, int mapPixelWidth, int mapPixelHeight)
        {
            // Clamp proposed inside map first
            float clampedX = MathHelper.Clamp(proposedPosition.X, 0f, Math.Max(0, mapPixelWidth - Width));
            float clampedY = MathHelper.Clamp(proposedPosition.Y, 0f, Math.Max(0, mapPixelHeight - Height));
            var newBounds = new Rectangle((int)clampedX, (int)clampedY, Width, Height);

            //REMOVED Collsion (For now) with other rabbits, as had issues with them getting stuck

            // Check collision with other rabbits
            //foreach (var other in rabbits)
            //{
            //  if (other == null || other == this || !other.Alive) continue;
            //if (newBounds.Intersects(other.Bounds))
            //{
            // collision with another rabbit -> cancel movement
            //  return;
            //}
            //}

            // Check collision with foxes
            foreach (var f in foxes)
            {
                if (f == null || !f.Alive) continue;
                if (newBounds.Intersects(f.Bounds))
                {
                    // collision with fox -> cancel movement
                    return;
                }
            }

            // no collision, apply
            Position.X = clampedX;
            Position.Y = clampedY;
        }

        private void MoveAlongPath(float deltaTime, List<Rabbit> rabbits, List<Fox> foxes, int mapPixelWidth, int mapPixelHeight)
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
                // Move towards current waypoint but avoid moving into other rabbits or foxes
                direction.Normalize();
                Vector2 proposed = Position + direction * Speed * deltaTime;
                TryMove(proposed, rabbits, foxes, mapPixelWidth, mapPixelHeight);
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
        private void FleeFromFox(Fox fox, float deltaTime, List<Rabbit> rabbits, List<Fox> foxes, int mapPixelWidth, int mapPixelHeight)// Running awway from fox
        {
            // Run in opposite direction from fox but avoid stepping into other rabbits or fox hitboxes
            Vector2 fleeDirection = Position - fox.Position;
            if (fleeDirection.Length() > 0)
            {
                fleeDirection.Normalize();
                Vector2 proposed = Position + fleeDirection * Speed * 1.5f * deltaTime; // 1.5x speed when fleeing as they are running
                TryMove(proposed, rabbits, foxes, mapPixelWidth, mapPixelHeight);
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

        // Called by Game1 when two rabbits successfully breed
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

        public void Draw(SpriteBatch spriteBatch)//Draw method for the rabbits
        {
            if (!Alive) return;
            Color drawColor = Color.White;
            spriteBatch.Draw(Texture, Position, null, Color.White, 0f, Vector2.Zero, DrawScale, SpriteEffects.None, 0f);
        }
    }
}