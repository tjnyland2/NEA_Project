using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;

namespace Project1
{
    
    //Version 22/11/25 (1)

    /// To do:

    ///Mutations (Allowed only mutated rabbits to have certain plants)
    /// Biomes (make it work) + Terrain Roughness
    /// Tutorial menu
    /// Game Over screen + Graph (of populations over time)
    /// Better graphics (animation of sprites) + plant textures

    public enum GameState // The states of the game (did this off my FSM I made in my analysis)
    {
        MainMenu,
        Playing,
        Terrain,
        Settings,
        Tutorial,
        GameOver,
        Quit
    }

    public class Game1 : Game//The Simulation
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        GameState currentGameState = GameState.MainMenu;//The first menu is the main menu
        SpriteFont font;//Menu font I set in Content Pipeline

        private MapGenerator mapGenerator;

        //Default Values for my simulation
        int rabbitCount = 10;
        int foxCount = 5;
        int mutationChance = 10; // percent
        int terrainRoughness = 5; // terrain roughness level
        int selectedBiome = 1;

        List<Button> menuButtons = new List<Button>(); //Buttons (menu)
        MouseState currentMouse, previousMouse;

        List<Button> settingsButtons = new List<Button>();//Buttons (settings)
        Button rabbitMinus, rabbitPlus, foxMinus, foxPlus, mutationMinus, mutationPlus;

        List<Button> terrainButtons = new List<Button>();//Buttons (Terrain)
        Button roughnessMinus, roughnessPlus;

        List<Plant> activePlants = new List<Plant>();//Plants 
        List<Rabbit> activeRabbits = new List<Rabbit>();//Rabbits
        List<Fox> activeFoxes = new List<Fox>();//Foxes
        
        Texture2D grassTex, thornsTex, rabbitTex, foxTexture;//Plant,Rabbit and Fox Textures
        Texture2D pixelTexture; // used to draw borders and hitbox outlines
        Random rng = new Random();//randomness
        float plantSpawnTimer = 0f;
        float plantSpawnInterval = 2f; // every 2 seconds

        bool rabbitsSpawned = false; // make sure rabbits only spawn once

        // Population history for GameOver graph
        List<int> rabbitHistory = new List<int>();
        List<int> foxHistory = new List<int>();
        float historySampleTimer = 0f;
        float historySampleInterval = 1f; // sample population every 1 second

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("File");
            menuButtons.Add(new Button// PLay button
            {
                Bounds = new Rectangle(300, 100, 200, 50),
                Text = "Play",
                Font = font,
                OnClick = () => currentGameState = GameState.Playing
            });
            menuButtons.Add(new Button// Tutorial button
            {
                Bounds = new Rectangle(300, 160, 200, 50),
                Text = "Tutorial",
                Font = font,
                OnClick = () => currentGameState = GameState.Tutorial
            });
            menuButtons.Add(new Button// Terrain button
            {
                Bounds = new Rectangle(300, 220, 200, 50),
                Text = "Terrain",
                Font = font,
                OnClick = () => currentGameState = GameState.Terrain
            });
            menuButtons.Add(new Button// Settings button
            {
                Bounds = new Rectangle(300, 280, 200, 50),
                Text = "Settings",
                Font = font,
                OnClick = () => currentGameState = GameState.Settings
            });
            menuButtons.Add(new Button// Quit button
            {
                Bounds = new Rectangle(300, 340, 200, 50),
                Text = "Quit",
                Font = font,
                OnClick = () => Exit()
            });

            // Rabbit + and -
            rabbitMinus = new Button
            {
                Bounds = new Rectangle(250, 100, 40, 40),
                Text = "-",
                Font = font,
                OnClick = () => { if (rabbitCount > 0) rabbitCount--; }
            };
            rabbitPlus = new Button
            {
                Bounds = new Rectangle(400, 100, 40, 40),
                Text = "+",
                Font = font,
                OnClick = () => { rabbitCount++; }
            };

            // Fox + and -
            foxMinus = new Button
            {
                Bounds = new Rectangle(250, 235, 40, 40),
                Text = "-",
                Font = font,
                OnClick = () => { if (foxCount > 0) foxCount--; }
            };
            foxPlus = new Button
            {
                Bounds = new Rectangle(400, 235, 40, 40),
                Text = "+",
                Font = font,
                OnClick = () => { foxCount++; }
            };

            // Mutation + and -
            mutationMinus = new Button
            {
                Bounds = new Rectangle(250, 345, 40, 40),
                Text = "-",
                Font = font,
                OnClick = () => { if (mutationChance > 0) mutationChance--; }
            };
            mutationPlus = new Button
            {
                Bounds = new Rectangle(400, 345, 40, 40),
                Text = "+",
                Font = font,
                OnClick = () => { if (mutationChance < 100) mutationChance++; }
            };
            

            settingsButtons.Add(new Button//Reset Values (settings)
            {
                Bounds = new Rectangle(550, 150, 200, 50),
                Text = "Reset Values",
                Font = font,
                BackgroundColor = Color.Red,
                OnClick = () =>
                {
                    rabbitCount = 10;
                    foxCount = 5;
                    mutationChance = 10;
                }
            });
            settingsButtons.Add(new Button //Exit button (settings)
            {
                Bounds = new Rectangle(550, 220, 200, 50),
                Text = "Exit",
                Font = font,
                OnClick = () => currentGameState = GameState.MainMenu
            });

            // Terrain Roughness + and -
            roughnessMinus = new Button
            {
                Bounds = new Rectangle(560, 120, 40, 40),
                Text = "-",
                Font = font,
                OnClick = () => { if (terrainRoughness > 1) terrainRoughness--; }
            };
            roughnessPlus = new Button
            {
                Bounds = new Rectangle(660, 120, 40, 40),
                Text = "+",
                Font = font,
                OnClick = () => { if (terrainRoughness < 10) terrainRoughness++; }
            };

            // Biome selection buttons
            terrainButtons.Add(new Button
            {
                Bounds = new Rectangle(20, 100, 200, 40),
                Text = "Choose Biome 1",
                Font = font,
                OnClick = () => selectedBiome = 1
            });
            terrainButtons.Add(new Button
            {
                Bounds = new Rectangle(20, 200, 200, 40),
                Text = "Choose Biome 2",
                Font = font,
                OnClick = () => selectedBiome = 2
            });
            terrainButtons.Add(new Button
            {
                Bounds = new Rectangle(20, 300, 200, 40),
                Text = "Choose Biome 3",
                Font = font,
                OnClick = () => selectedBiome = 3
            });


            terrainButtons.Add(new Button// Exit button (terrain)
            {
                Bounds = new Rectangle(550, 300, 200, 50),
                Text = "Exit",
                Font = font,
                OnClick = () => currentGameState = GameState.MainMenu
            });

            //Plant Textures (Temp)
            grassTex = new Texture2D(GraphicsDevice, 1, 1);
            grassTex.SetData(new[] { Color.DarkRed });

            thornsTex = new Texture2D(GraphicsDevice, 1, 1);
            thornsTex.SetData(new[] { Color.DarkRed });

            //Rabbit Texture
            rabbitTex = Content.Load<Texture2D>("rabbitrun");

            //Fox Texture
            foxTexture = Content.Load<Texture2D>("foxrun8");

            // Pixel used to draw borders and outlines (just for testing)
            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });
        }

        private void SpawnRabbits(int mapPixelWidth, int mapPixelHeight)
        {
            activeRabbits.Clear(); // Clear any existing rabbits

            int margin = 10;
            for (int i = 0; i < rabbitCount; i++)
            {
                // Spawn rabbits at random positions on the map (respect map pixel bounds)
                Vector2 spawnPosition = new Vector2( 
                    rng.Next(margin, Math.Max(margin+1, mapPixelWidth - margin)), //this is the random x potison
                    rng.Next(margin, Math.Max(margin+1, mapPixelHeight - margin))// the random y potison
                );

                activeRabbits.Add(new Rabbit(spawnPosition, rabbitTex));
            }

            rabbitsSpawned = true;
        }
        private void SpawnFoxes(int mapPixelWidth, int mapPixelHeight)
        {
            activeFoxes.Clear(); // Clear any existing foxes

            int margin = 10;
            for (int i = 0; i < foxCount; i++)
            {
                // Spawn foxes at random positions on the map
                Vector2 spawnPosition = new Vector2(
                    rng.Next(margin, Math.Max(margin+1, mapPixelWidth - margin)), // x position
                    rng.Next(margin, Math.Max(margin+1, mapPixelHeight - margin)) // y position
                );

                activeFoxes.Add(new Fox(spawnPosition, foxTexture));
            }
        }


        protected override void Update(GameTime gameTime)
        {
            currentMouse = Mouse.GetState();

            if (currentGameState == GameState.MainMenu)// Main menu
            {
                foreach (var button in menuButtons)
                    button.Update(currentMouse, previousMouse);

                // Reset spawn flag when returning to menu
                rabbitsSpawned = false;
            }
            if (currentGameState == GameState.Playing)//Playing
            {
                // Initialize map if needed
                if (mapGenerator == null)
                {
                    mapGenerator = new MapGenerator(80, 60, GraphicsDevice);
                    mapGenerator.LoadContent();
                }

                // Spawn rabbits once when entering play mode
                if (!rabbitsSpawned)
                {
                    SpawnRabbits(mapGenerator.PixelWidth, mapGenerator.PixelHeight);
                    SpawnFoxes(mapGenerator.PixelWidth, mapGenerator.PixelHeight); //Also decided to spawn the foxes here
                    // clear any old history when starting a new simulation
                    rabbitHistory.Clear();
                    foxHistory.Clear();
                    historySampleTimer = 0f;
                }

                float time = (float)gameTime.TotalGameTime.TotalSeconds;

                // Plants spawning 
                plantSpawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (plantSpawnTimer >= plantSpawnInterval)
                {
                    plantSpawnTimer = 0f;

                    Vector2 position = new Vector2(rng.Next(10, Math.Max(11, mapGenerator.PixelWidth - 10)), rng.Next(10, Math.Max(11, mapGenerator.PixelHeight - 10)));
                    string type = rng.NextDouble() < 0.5 ? "Grass" : "Thorns";
                    Texture2D tex = type == "Grass" ? grassTex : thornsTex;

                    activePlants.Add(new Plant(position, type, time, tex));
                }

                // Despawn plants after 15s (only those not being eaten)
                for (int i = activePlants.Count - 1; i >= 0; i--)
                {
                    if (time - activePlants[i].SpawnTime > 15f)
                    {
                        // Check if any rabbit is currently eating this plant
                        bool beingEaten = false;
                        foreach (var rabbit in activeRabbits)
                        {
                            if (rabbit.TargetPlant == activePlants[i] && rabbit.State == RabbitState.Eating)
                            {
                                beingEaten = true;
                                break;
                            }
                        }

                        if (!beingEaten)
                        {
                            activePlants.RemoveAt(i);
                        }
                    }
                }

                foreach (var fox in activeFoxes)//Foxes update
                {
                    fox.Update(gameTime, activeRabbits, mapGenerator.PixelWidth, mapGenerator.PixelHeight);
                }

                // Update all rabbits
                // Update all rabbits (pass full rabbit list so each rabbit can avoid others)
                foreach (var rabbit in activeRabbits)
                {
                    rabbit.Update(gameTime, activePlants, activeFoxes, activeRabbits, mapGenerator.PixelWidth, mapGenerator.PixelHeight);
                }
                activeRabbits.RemoveAll(r => !r.Alive); //removes the rabbits that are not alive

                // Breeding: if two rabbits have eaten recently and meet, create a new rabbit
                float breedDistance = 20f;
                var newBabies = new List<Rabbit>();
                for (int i = 0; i < activeRabbits.Count; i++)
                {
                    var r1 = activeRabbits[i];
                    if (!r1.Alive) continue;
                    for (int j = i + 1; j < activeRabbits.Count; j++)
                    {
                        var r2 = activeRabbits[j];
                        if (!r2.Alive) continue;

                        if (r1.CanBreed() && r2.CanBreed())
                        {
                            if (Vector2.Distance(r1.Position, r2.Position) <= breedDistance)
                            {
                                // spawn baby at midpoint, clamp to map
                                Vector2 spawnPos = (r1.Position + r2.Position) / 2f;
                                spawnPos.X = MathHelper.Clamp(spawnPos.X, 0f, Math.Max(0, mapGenerator.PixelWidth - 8));
                                spawnPos.Y = MathHelper.Clamp(spawnPos.Y, 0f, Math.Max(0, mapGenerator.PixelHeight - 8));

                                newBabies.Add(new Rabbit(spawnPos, rabbitTex));
                                r1.MarkBred();
                                r2.MarkBred();

                                // prevent same rabbits breeding again this tick
                                break;
                            }
                        }
                    }
                }

                if (newBabies.Count > 0)
                    activeRabbits.AddRange(newBabies);

                // Sample population history at fixed interval
                historySampleTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (historySampleTimer >= historySampleInterval)
                {
                    historySampleTimer -= historySampleInterval;
                    rabbitHistory.Add(activeRabbits.Count);
                    foxHistory.Add(activeFoxes.Count);
                }

                if (activeRabbits.Count == 0 || activeFoxes.Count == 0)// If one of the species is dead
                {
                    currentGameState = GameState.GameOver; //Game over
                }

                // Exit to menu with Escape key
                if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                {
                    currentGameState = GameState.MainMenu;
                    // Clear game objects
                    activePlants.Clear();
                    activeRabbits.Clear();
                    mapGenerator = null;
                    rabbitsSpawned = false;
                }
            }
            if (currentGameState == GameState.Settings)//Settings
            {
                //Buttons
                rabbitMinus.Update(currentMouse, previousMouse);
                rabbitPlus.Update(currentMouse, previousMouse);
                foxMinus.Update(currentMouse, previousMouse);
                foxPlus.Update(currentMouse, previousMouse);
                mutationMinus.Update(currentMouse, previousMouse);
                mutationPlus.Update(currentMouse, previousMouse);

                foreach (var button in settingsButtons)
                    button.Update(currentMouse, previousMouse);
            }
            if (currentGameState == GameState.Terrain)//Terrain
            {
                roughnessMinus.Update(currentMouse, previousMouse);
                roughnessPlus.Update(currentMouse, previousMouse);

                foreach (var button in terrainButtons)
                    button.Update(currentMouse, previousMouse);
            }

            if (currentGameState == GameState.GameOver)
            {
                // Allow returning to main menu and clear simulation state/history
                if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                {
                    currentGameState = GameState.MainMenu;
                    activePlants.Clear();
                    activeRabbits.Clear();
                    activeFoxes.Clear();
                    mapGenerator = null;
                    rabbitsSpawned = false;
                    rabbitHistory.Clear();
                    foxHistory.Clear();
                    historySampleTimer = 0f;
                }
            }

            previousMouse = currentMouse;
            base.Update(gameTime);
        }

        private void DrawRectangleOutline(SpriteBatch sb, Rectangle rect, int thickness, Color color)
        {
            //JUST FOR TESTING, SO I COULD SEE THE MAP BOUNDARIES AND HITBOXES

            // top
            // sb.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // bottom
            // sb.Draw(pixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
            // left
            // sb.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // right
            // sb.Draw(pixelTexture, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawPopulationGraph(SpriteBatch sb, Rectangle area)
        {
            // background
            sb.Draw(pixelTexture, area, Color.Black * 0.8f);

            int padding = 10;
            var plotRect = new Rectangle(area.X + padding, area.Y + padding, area.Width - padding * 2, area.Height - padding * 2);

            // axes
            // y axis
            sb.Draw(pixelTexture, new Rectangle(plotRect.X, plotRect.Y, 2, plotRect.Height), Color.White);
            // x axis
            sb.Draw(pixelTexture, new Rectangle(plotRect.X, plotRect.Y + plotRect.Height - 2, plotRect.Width, 2), Color.White);

            int points = Math.Max(rabbitHistory.Count, foxHistory.Count);
            if (points < 2)
            {
                sb.DrawString(font, "Not enough data to plot.", new Vector2(area.X + 20, area.Y + 20), Color.White);
                return;
            }

            int maxY = 1;
            if (rabbitHistory.Count > 0) maxY = Math.Max(maxY, rabbitHistory.Max());
            if (foxHistory.Count > 0) maxY = Math.Max(maxY, foxHistory.Max());

            // draw y ticks and labels (3 ticks)
            for (int t = 0; t <= 3; t++)
            {
                float frac = t / 3f;
                int y = plotRect.Y + (int)((1 - frac) * plotRect.Height);
                sb.Draw(pixelTexture, new Rectangle(plotRect.X - 5, y, plotRect.Width + 5, 1), Color.Gray * 0.6f);
                int label = (int)Math.Round(frac * maxY);
                sb.DrawString(font, label.ToString(), new Vector2(plotRect.X - 40, y - 8), Color.White);
            }

            // Helper to map sample index/value to screen coords
            float xStep = (float)plotRect.Width / (points - 1);
            float yScale = (float)plotRect.Height / Math.Max(1, maxY);

            // draw rabbit polyline (yellow)
            Color rabbitColor = Color.Yellow;
            for (int i = 1; i < rabbitHistory.Count; i++)
            {
                float x1 = plotRect.X + (i - 1) * xStep;
                float x2 = plotRect.X + i * xStep;
                float y1 = plotRect.Y + plotRect.Height - rabbitHistory[i - 1] * yScale;
                float y2 = plotRect.Y + plotRect.Height - rabbitHistory[i] * yScale;
                DrawLine(sb, new Vector2(x1, y1), new Vector2(x2, y2), rabbitColor, 2);
            }

            // draw fox polyline (red)
            Color foxColor = Color.Red;
            for (int i = 1; i < foxHistory.Count; i++)
            {
                float x1 = plotRect.X + (i - 1) * xStep;
                float x2 = plotRect.X + i * xStep;
                float y1 = plotRect.Y + plotRect.Height - foxHistory[i - 1] * yScale;
                float y2 = plotRect.Y + plotRect.Height - foxHistory[i] * yScale;
                DrawLine(sb, new Vector2(x1, y1), new Vector2(x2, y2), foxColor, 2);
            }

            // legend
            int legendX = plotRect.X + 10;
            int legendY = plotRect.Y + 10;
            sb.Draw(pixelTexture, new Rectangle(legendX, legendY, 10, 10), rabbitColor);
            sb.DrawString(font, $" Rabbits (final: {rabbitHistory.LastOrDefault()})", new Vector2(legendX + 14, legendY - 3), Color.White);
            legendY += 18;
            sb.Draw(pixelTexture, new Rectangle(legendX, legendY, 10, 10), foxColor);
            sb.DrawString(font, $" Foxes (final: {foxHistory.LastOrDefault()})", new Vector2(legendX + 14, legendY - 3), Color.White);
        }

        private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            // draw line using pixelTexture
            Vector2 edge = end - start;
            float angle = (float)Math.Atan2(edge.Y, edge.X);
            float length = edge.Length();
            sb.Draw(pixelTexture, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        private Color GetBiomeBackgroundColor(int biomeId)
        {
            switch (biomeId)
            {
                case 1: return new Color(110, 170, 100); // lush green tint
                case 2: return new Color(150, 190, 150); // pale moss tint
                case 3: return new Color(235, 210, 170); // sandy tint
                default: return Color.Black;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clear to biome background when playing; otherwise keep black
            Color clearColor = Color.Black;
            if (currentGameState == GameState.Playing)
                clearColor = GetBiomeBackgroundColor(selectedBiome);

            GraphicsDevice.Clear(clearColor);
            _spriteBatch.Begin();

            if (currentGameState == GameState.MainMenu)//Main menu
            {
                _spriteBatch.DrawString(font, "Predator/Prey Simulator", new Vector2(290, 30), Color.White);//title
                foreach (var button in menuButtons)
                    button.Draw(_spriteBatch); //Draw each button
            }
            else if (currentGameState == GameState.Playing)//Playing
            {
                mapGenerator?.Draw(_spriteBatch);//Generation of map

                foreach (var plant in activePlants)//Plants 
                    plant.Draw(_spriteBatch);

                foreach (var rabbit in activeRabbits)//Rabbits
                    rabbit.Draw(_spriteBatch);
                foreach (var fox in activeFoxes)//Foxes
                    fox.Draw(_spriteBatch);
                
                // Draw map border (based on map pixel size)
                if (mapGenerator != null)
                {
                    int borderThickness = 3;
                    var mapRect = new Rectangle(0, 0, mapGenerator.PixelWidth, mapGenerator.PixelHeight);
                    DrawRectangleOutline(_spriteBatch, mapRect, borderThickness, Color.White);
                }

                // Draw hitbox outlines for debugging/visibility
                foreach (var plant in activePlants)
                {
                    DrawRectangleOutline(_spriteBatch, plant.Bounds, 1, Color.Lime * 0.8f);
                }
                foreach (var rabbit in activeRabbits)
                {
                    DrawRectangleOutline(_spriteBatch, rabbit.Bounds, 1, Color.Yellow * 0.8f);
                }
               

                // Draw UI information
                _spriteBatch.DrawString(font, $"Plants: {activePlants.Count}", new Vector2(10, 10), Color.White);
                _spriteBatch.DrawString(font, $"Rabbits: {activeRabbits.Count}", new Vector2(10, 30), Color.White);
                _spriteBatch.DrawString(font, $"Foxes: {activeFoxes.Count}", new Vector2(10, 50), Color.White);
                _spriteBatch.DrawString(font, "Press ESC to return to menu", new Vector2(10, 570), Color.White);
            }
            else if (currentGameState == GameState.Settings)//Settings
            {
                _spriteBatch.DrawString(font, "Main Menu > Settings", new Vector2(330, 30), Color.White);
                // Labels and values
                _spriteBatch.DrawString(font, "Rabbits:", new Vector2(320, 85), Color.White);
                _spriteBatch.DrawString(font, rabbitCount.ToString(), new Vector2(350, 115), Color.White);

                _spriteBatch.DrawString(font, "Foxes:", new Vector2(320, 205), Color.White);
                _spriteBatch.DrawString(font, foxCount.ToString(), new Vector2(350, 235), Color.White);

                _spriteBatch.DrawString(font, "Mutation Chance:", new Vector2(280, 315), Color.White);
                _spriteBatch.DrawString(font, mutationChance + "%", new Vector2(340, 345), Color.White);

                // Draw all buttons
                rabbitMinus.Draw(_spriteBatch);
                rabbitPlus.Draw(_spriteBatch);
                foxMinus.Draw(_spriteBatch);
                foxPlus.Draw(_spriteBatch);
                mutationMinus.Draw(_spriteBatch);
                mutationPlus.Draw(_spriteBatch);

                foreach (var button in settingsButtons)
                    button.Draw(_spriteBatch);
            }
            else if (currentGameState == GameState.Terrain)//Terrain
            {
                _spriteBatch.DrawString(font, "Terrain Editor", new Vector2(330, 30), Color.White);

                // Biome descriptions
                _spriteBatch.DrawString(font, "- Grass\n- Thorns\n- Brown Rabbits Camouflage Best", new Vector2(220, 100), Color.White);
                _spriteBatch.DrawString(font, "- Moss Clumps\n- Thorns\n- White Rabbits Camouflage Best", new Vector2(220, 200), Color.White);
                _spriteBatch.DrawString(font, "- Grass\n- Cacti\n- Brown Rabbits Camouflage Best", new Vector2(220, 300), Color.White);

                // Roughness controls
                _spriteBatch.DrawString(font, "Terrain Roughness:", new Vector2(540, 80), Color.White);
                _spriteBatch.DrawString(font, terrainRoughness.ToString(), new Vector2(630, 125), Color.White);
                roughnessMinus.Draw(_spriteBatch);
                roughnessPlus.Draw(_spriteBatch);

                // Draw buttons
                foreach (var button in terrainButtons)
                    button.Draw(_spriteBatch);
            }
            else if (currentGameState == GameState.GameOver)
            {
                _spriteBatch.DrawString(font, "Game Over", new Vector2(330, 20), Color.White);

                // Graph area
                var graphArea = new Rectangle(100, 60, 600, 400);
                DrawPopulationGraph(_spriteBatch, graphArea);

                // summary text (was 720x changed to 600x)
                _spriteBatch.DrawString(font, $"Final Rabbits: {rabbitHistory.LastOrDefault()}", new Vector2(600, 80), Color.Yellow);
                _spriteBatch.DrawString(font, $"Final Foxes: {foxHistory.LastOrDefault()}", new Vector2(600, 110), Color.Red);
                _spriteBatch.DrawString(font, "Press ESC to return to main menu", new Vector2(330, -110), Color.White);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}