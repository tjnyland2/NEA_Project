using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;

namespace Project1
{
    //Menus DONE ==> Settings, Terrain, Main Menu and quit
    //PLANTs work

    
    /// To do:
    /// Textures
    /// Foxes
    /// Mutations (Allowed only mutated rabbits to have certain plants)
    /// Biomes (make it work) + Terrain Roughness
    /// Tutorial menu

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

    public class Game1 : Game//The Game (Simulation)
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
        int terrainRoughness = 5; // Value range: e.g., 1–10
        int selectedBiome = 1;

        List<Button> menuButtons = new List<Button>(); //Buttons (menu)
        MouseState currentMouse, previousMouse;

        List<Button> settingsButtons = new List<Button>();//Buttons (settings)
        Button rabbitMinus, rabbitPlus, foxMinus, foxPlus, mutationMinus, mutationPlus;

        List<Button> terrainButtons = new List<Button>();//Buttons (Terrain)
        Button roughnessMinus, roughnessPlus;

        List<Plant> activePlants = new List<Plant>();//Plants 
        List<Rabbit> activeRabbits = new List<Rabbit>();//Rabbits
        Texture2D grassTex, thornsTex, rabbitTex;//Plant and Rabbit Textures
        Random rng = new Random();//randomness
        float plantSpawnTimer = 0f;
        float plantSpawnInterval = 2f; // every 2 seconds

        bool rabbitsSpawned = false; // Flag to ensure rabbits are only spawned once

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

            //Plant Textures
            grassTex = new Texture2D(GraphicsDevice, 1, 1);
            grassTex.SetData(new[] { Color.Green });

            thornsTex = new Texture2D(GraphicsDevice, 1, 1);
            thornsTex.SetData(new[] { Color.DarkRed });

            //Rabbit Texture
            rabbitTex = new Texture2D(GraphicsDevice, 1, 1);
            rabbitTex.SetData(new[] { Color.Brown });
        }

        private void SpawnRabbits()
        {
            activeRabbits.Clear(); // Clear any existing rabbits

            for (int i = 0; i < rabbitCount; i++)
            {
                // Spawn rabbits at random positions on the map
                Vector2 spawnPosition = new Vector2(
                    rng.Next(50, 750), // Keep away from edges
                    rng.Next(50, 550)
                );

                activeRabbits.Add(new Rabbit(spawnPosition, rabbitTex));
            }

            rabbitsSpawned = true;
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
                    SpawnRabbits();
                }

                float time = (float)gameTime.TotalGameTime.TotalSeconds;

                // Plants spawning 
                plantSpawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (plantSpawnTimer >= plantSpawnInterval)
                {
                    plantSpawnTimer = 0f;

                    Vector2 position = new Vector2(rng.Next(50, 750), rng.Next(50, 550));
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

                // Update all rabbits
                foreach (var rabbit in activeRabbits)
                {
                    rabbit.Update(gameTime, activePlants, 80, 60); // Pass map dimensions
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

            previousMouse = currentMouse;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);// Black background :)
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

                // Draw UI information
                _spriteBatch.DrawString(font, $"Plants: {activePlants.Count}", new Vector2(10, 10), Color.White);
                _spriteBatch.DrawString(font, $"Rabbits: {activeRabbits.Count}", new Vector2(10, 30), Color.White);
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

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}