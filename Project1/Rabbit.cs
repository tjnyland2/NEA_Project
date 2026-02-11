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

    public class Rabbit : Animal //Rabbit Class inherits Animal c;ass
    {
        public Vector2 TargetPosition;
        public RabbitState State;
        public Texture2D Texture;

        public Plant TargetPlant { get; private set; }
        public float EatingTimer;
        public float EatingDuration = 2f; //How long they spend eating

        public bool IsMutated { get; private set; } = false;//if the rabbit is mutated

        private const float DrawScale = 0.7f; //scale used when drawing the rabbit

        //Hitbox height and width size based on texture size and draw scale
        public override int Width => (int)(Texture?.Width * DrawScale ?? 8);
        public override int Height => (int)(Texture?.Height * DrawScale ?? 8);

        // Breeding/eating values (specific to rabbits)
        private const float RABBIT_BREED_WINDOW = 8f;
        private const float RABBIT_BREED_COOLDOWN = 10f;

        private List<Vector2> currentPath;
        private int currentPathIndex;
        private const int GRID_SIZE = 5; // map
        private const float FOX_DETECTION_RANGE = 100f; //Range to detect foxes

        //Starvation
        private const float RABBIT_STARVATION_TIME = 20f; //seconds until rabbit dies without food

        //RNG for natural variation
        private static readonly Random rng = new Random();

        // Base speeds
        private const float BASE_SPEED = 70f;
        private const float MUTATED_SPEED_BOOST = 25f; //mutated rabbits are faster

        public Rabbit(Vector2 startPosition, Texture2D texture, bool isMutated = false)
        {
            Position = startPosition;
            Texture = texture;
            State = RabbitState.Seeking;
            EatingTimer = 0f;
            currentPath = new List<Vector2>();
            currentPathIndex = 0;
            TargetPlant = null;
            IsMutated = isMutated;

            // configure inherited fields
            Speed = BASE_SPEED + (IsMutated ? MUTATED_SPEED_BOOST : 0f);
            starvationTime = RABBIT_STARVATION_TIME;
            breedWindow = RABBIT_BREED_WINDOW;
            breedCooldownTime = RABBIT_BREED_COOLDOWN;
            HasEaten = false;
            timeSinceAte = float.MaxValue;
            BreedingCooldown = 0f;
            hungerTimer = 0f;
        }

        //mapPixelWidth/mapPixelHeight are in pixels (not tiles)
        public void Update(GameTime gameTime, List<Plant> plants, List<Fox> foxes, List<Rabbit> rabbits, int mapPixelWidth, int mapPixelHeight)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            //update common timers (hunger/breeding)
            UpdateCommonTimers(deltaTime);

            //If already dead, skip and clear targets
            if (!Alive)
            {
                ClearTarget();
                return;
            }

            // If a nearby fox is detected, flee immediately (the survival instinct)
            Fox nearestFox = FindNearestFox(foxes);
            if (nearestFox != null && Vector2.Distance(Position, nearestFox.Position) < FOX_DETECTION_RANGE)
            {
                //Run away from fox (avoid other rabbits while fleeing)
                FleeFromFox(nearestFox, deltaTime, rabbits, foxes, mapPixelWidth, mapPixelHeight);
                //Keep inside of the map
                ClampToMap(mapPixelWidth, mapPixelHeight);
                return; //Skip other behaviors when fleeing
            }

            switch (State)
            {
                case RabbitState.Seeking: //Look for nearest plant
                    SeekNearestPlant(plants, mapPixelWidth, mapPixelHeight);
                    break;

                case RabbitState.MovingToPlant://Move along path to plant
                    MoveAlongPath(deltaTime, rabbits, foxes, mapPixelWidth, mapPixelHeight);
                    break;

                case RabbitState.Eating://Eating the plant
                    EatingTimer += deltaTime;
                    if (EatingTimer >= EatingDuration)
                    {
                        //Finished eating, remove the plant and go back to seeking
                        if (TargetPlant != null)
                        {
                            //Release assigned slot before removing the plant
                            TargetPlant.ReleaseAssigned();
                            plants.Remove(TargetPlant);
                            TargetPlant = null;
                        }

                        //Mark as having eaten for the breeding window
                        HasEaten = true;
                        timeSinceAte = 0f;

                        //Reset hunger on successful eating
                        ResetHunger();

                        State = RabbitState.Seeking;
                        EatingTimer = 0f;
                    }
                    break;

                case RabbitState.Idle://If idle 
                    //wander around a bit while idle so rabbits don't stand perfectly still
                    Vector2 randDir = new Vector2((float)(rng.NextDouble() - 0.5), (float)(rng.NextDouble() - 0.5));
                    if (randDir.LengthSquared() > 0.0001f) randDir.Normalize();
                    Vector2 proposed = Position + randDir * Speed * 0.35f * deltaTime;
                    TryMove(proposed, rabbits, foxes, mapPixelWidth, mapPixelHeight);
                    State = RabbitState.Seeking; //periodically resume seeking
                    break;
            }

            //Clamp inside map borders so rabbits don't go off-screen (uses sprite size)
            ClampToMap(mapPixelWidth, mapPixelHeight);
        }

        private void SeekNearestPlant(List<Plant> plants, int mapPixelWidth, int mapPixelHeight)
        {
            if (plants.Count == 0)
            {
                State = RabbitState.Idle;
                return;
            }

            //Find the nearest plant that still has assignment capacity
            Plant nearestPlant = null;
            float nearestDistance = float.MaxValue;

            foreach (var plant in plants)
            {
                if (plant == null) continue;
                if (plant.AssignedRabbits >= Plant.MaxAssigned) continue; //skip "full" plants

                //Only mutatued rabbits can eat thorns
                if (!IsMutated && plant.Type == "Thorns") continue;

                float distance = Vector2.Distance(Position, plant.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestPlant = plant;
                }
            }

            if (nearestPlant != null)
            {
                //Try to reserve the plant before committing to it
                if (nearestPlant.TryAssign())
                {
                    //release any previous target (shouldn't normally have one here)
                    if (TargetPlant != null)
                        TargetPlant.ReleaseAssigned();

                    TargetPlant = nearestPlant;
                    //Calculate path
                    currentPath = FindPathDirect(Position, nearestPlant.Position);
                    if (currentPath.Count > 0)
                    {
                        currentPathIndex = 0;
                        State = RabbitState.MovingToPlant;
                    }
                }
                else
                {
                    TargetPlant = null;
                    State = RabbitState.Seeking;
                }
            }
            else
            {
                //no available plant = idle wander
                State = RabbitState.Idle;
            }
        }

        //Tries to move to a potision (avoiding collsions with foxes)
        private void TryMove(Vector2 proposedPosition, List<Rabbit> rabbits, List<Fox> foxes, int mapPixelWidth, int mapPixelHeight)
        {
            //Clamp proposed inside map first
            float clampedX = MathHelper.Clamp(proposedPosition.X, 0f, Math.Max(0, mapPixelWidth - Width));
            float clampedY = MathHelper.Clamp(proposedPosition.Y, 0f, Math.Max(0, mapPixelHeight - Height));
            var newBounds = new Rectangle((int)clampedX, (int)clampedY, Width, Height);

            // Check collision with foxes
            foreach (var f in foxes)
            {
                if (f == null || !f.Alive) continue;
                if (newBounds.Intersects(f.Bounds))
                {
                    //collision with fox = cancel movement
                    return;
                }
            }

            //no collision, apply
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

            if (distance < 2f) //Close enough to current waypoint
            {
                currentPathIndex++;
                if (currentPathIndex >= currentPath.Count)
                {
                    //Reached final destination
                    if (TargetPlant != null && Vector2.Distance(Position, TargetPlant.Position) < 15f)
                    {
                        State = RabbitState.Eating;
                        EatingTimer = 0f;
                    }
                    else
                    {
                        //If target plant is gone or out of reach, release target and seek again
                        if (TargetPlant != null && Vector2.Distance(Position, TargetPlant.Position) >= 15f)
                        {
                            //keep target until confirmed lost
                        }
                        else
                        {
                            ClearTarget();
                            State = RabbitState.Seeking;
                        }
                    }
                }
            }
            else
            {
                //Move towards current waypoint but avoid moving into other rabbits or foxes
                direction.Normalize();

                //Adds a jitter to make things seem more natural
                float jitter = (float)(rng.NextDouble() * 0.5 - 0.25); // -0.25 .. +0.25
                Vector2 perp = new Vector2(-direction.Y, direction.X);
                direction += perp * jitter;
                if (direction.LengthSquared() > 0.0001f) direction.Normalize();

                //little changes in speed (makes things seem more natural)
                float speedFactor = 0.9f + (float)rng.NextDouble() * 0.2f; // 0.9 .. 1.1

                Vector2 proposed = Position + direction * Speed * speedFactor * deltaTime;
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
        private void FleeFromFox(Fox fox, float deltaTime, List<Rabbit> rabbits, List<Fox> foxes, int mapPixelWidth, int mapPixelHeight)//Running awway from fox
        {
            //Run in opposite direction from fox but avoid stepping into other rabbits or fox hitboxes
            Vector2 fleeDirection = Position - fox.Position;
            if (fleeDirection.Length() > 0)
            {
                fleeDirection.Normalize();

                //making sure fleeing has little jitters (make it seems more natural so not in straight line)
                float perpJitter = (float)(rng.NextDouble() * 0.6 - 0.3);
                Vector2 perp = new Vector2(-fleeDirection.Y, fleeDirection.X);
                fleeDirection += perp * perpJitter;
                if (fleeDirection.LengthSquared() > 0.0001f) fleeDirection.Normalize();

                Vector2 proposed = Position + fleeDirection * Speed * 1.5f * deltaTime; //1.5x speed when fleeing as they are running
                TryMove(proposed, rabbits, foxes, mapPixelWidth, mapPixelHeight);
            }
        }

        private List<Vector2> FindPathDirect(Vector2 start, Vector2 end) //Pathfinding (use of waypoints)
        {
            List<Vector2> path = new List<Vector2>();

            //add some waypoints between start and end
            Vector2 direction = end - start;
            float distance = direction.Length();

            if (distance > 20f) //Only add waypoints if distance large
            {
                direction.Normalize();

                // Add waypoints every 30 pixels
                for (float i = 30f; i < distance; i += 30f)
                {
                    Vector2 waypoint = start + direction * i;
                    path.Add(waypoint);//adds them
                }
            }

            path.Add(end); //Always add the final destination
            return path;
        }

        //release current target assignment (called when plant removed, rabbit dies, or rabbit abandons target)
        public void ClearTarget()
        {
            if (TargetPlant != null)
            {
                TargetPlant.ReleaseAssigned();
                TargetPlant = null;
            }
        }

        // Called by Game1 when two rabbits successfully breed
        public void MarkBred()
        {
            MarkBredCommon(RABBIT_BREED_COOLDOWN);
        }

        public bool CanBreed()
        {
            return CanBreedCommon();
        }

        public void Draw(SpriteBatch spriteBatch)//Draw method for the rabbits
        {
            if (!Alive) return;

            //mutated rabbits are light brown
            Color drawColor = IsMutated ? Color.White : new Color(150, 120, 90);

            //draw a thin dark outline to improve readability on varied backgrounds
            float outlineScale = DrawScale;
            int outlinePixels = 1;
            Color outlineColor = Color.Black * 0.85f;
            for (int ox = -outlinePixels; ox <= outlinePixels; ox++)
            {
                for (int oy = -outlinePixels; oy <= outlinePixels; oy++)
                {
                    if (ox == 0 && oy == 0) continue;
                    spriteBatch.Draw(Texture, Position + new Vector2(ox, oy), null, outlineColor, 0f, Vector2.Zero, outlineScale, SpriteEffects.None, 0f);
                }
            }

            //main sprite
            spriteBatch.Draw(Texture, Position, null, drawColor, 0f, Vector2.Zero, DrawScale, SpriteEffects.None, 0f);
        }
    }
}