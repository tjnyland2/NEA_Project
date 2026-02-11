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
    
    //Version 

    /// To do:
    ///Clean up code
    

    public enum GameState //The states of the game (did this off my FSM I made in my analysis)
    {
        MainMenu,
        Playing,
        Paused,
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
        int rabbitCount = 50;
        int foxCount = 2;
        int mutationChance = 15; //percent
        int terrainRoughness = 5; //terrain roughness level
        int selectedBiome = 1;

        List<Button> menuButtons = new List<Button>(); //Buttons (menu)`
        MouseState currentMouse, previousMouse;

        List<Button> settingsButtons = new List<Button>();//Buttons (settings)
        Button rabbitMinus, rabbitPlus, foxMinus, foxPlus, mutationMinus, mutationPlus;

        List<Button> terrainButtons = new List<Button>();//Buttons (Terrain)
        Button roughnessMinus, roughnessPlus;

        // Pause UI
        List<Button> pauseButtons = new List<Button>();
        Button pauseContinueButton, pauseEndButton;

        // Tutorial UI
        List<Texture2D> tutorialSlides = new List<Texture2D>();
        int tutorialIndex = 0;
        Button tutorialBackButton, tutorialNextButton, tutorialExitButton;
        Button tutorialRestartButton, tutorialStartButton;
        const int TutorialSlideCount = 5;
        string[] tutorialTexts;

        List<Plant> activePlants = new List<Plant>();//Plants 
        List<Rabbit> activeRabbits = new List<Rabbit>();//Rabbits
        List<Fox> activeFoxes = new List<Fox>();//Foxes
        
        Texture2D grassTex, thornsTex, rabbitTex, foxTexture;//Plant,Rabbit and Fox Textures //also add mutatedRabbitTex once I have made it
        Texture2D pixelTexture; //used to draw borders
        Random rng = new Random();//randomness
        float plantSpawnTimer = 0f;
        float plantSpawnInterval = 0.25f; //seconds inbetween plants spawning

        bool rabbitsSpawned = false; //make sure rabbits only spawn once

        // Population history for GameOver graph
        List<int> rabbitHistory = new List<int>();
        List<int> foxHistory = new List<int>();
        List<int> plantHistory = new List<int>(); 
        float historySampleTimer = 0f;
        float historySampleInterval = 1f; // sample population every 1 second

        // Simulation timer
        float simulationTimer = 0f; //how many seconds a simulation has been
        float lastSimulationDuration = 0f; //stored when simulation ends (for GameOver display)

        //Input
        KeyboardState previousKeyboard;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            
            base.Initialize();
        }

        protected override void LoadContent() //Load all the content
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("File");
            menuButtons.Add(new Button//Play button
            {
                Bounds = new Rectangle(300, 100, 200, 50),
                Text = "Play",
                Font = font,
                OnClick = () => currentGameState = GameState.Playing
            });
            menuButtons.Add(new Button//Tutorial button
            {
                Bounds = new Rectangle(300, 160, 200, 50),
                Text = "Tutorial",
                Font = font,
                OnClick = () => currentGameState = GameState.Tutorial
            });
            menuButtons.Add(new Button//Terrain button
            {
                Bounds = new Rectangle(300, 220, 200, 50),
                Text = "Terrain",
                Font = font,
                OnClick = () => currentGameState = GameState.Terrain
            });
            menuButtons.Add(new Button//Settings button
            {
                Bounds = new Rectangle(300, 280, 200, 50),
                Text = "Settings",
                Font = font,
                OnClick = () => currentGameState = GameState.Settings
            });
            menuButtons.Add(new Button//Quit button
            {
                Bounds = new Rectangle(300, 340, 200, 50),
                Text = "Quit",
                Font = font,
                OnClick = () => Exit()
            });

            //Rabbit + and -
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

            //Fox + and -
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

            //Mutation + and -
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
                    rabbitCount = 50;
                    foxCount = 2;
                    mutationChance = 15;
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
                OnClick = () => { if (terrainRoughness > 1) { terrainRoughness--; mapGenerator?.SetRoughness(terrainRoughness); } }
            };
            roughnessPlus = new Button
            {
                Bounds = new Rectangle(660, 120, 40, 40),
                Text = "+",
                Font = font,
                OnClick = () => { if (terrainRoughness < 10) { terrainRoughness++; mapGenerator?.SetRoughness(terrainRoughness); } }
            };

            // Reset roughness to default (terrain) button
            terrainButtons.Add(new Button
            {
                Bounds = new Rectangle(550, 180, 200, 50),
                Text = "Reset Roughness",
                Font = font,
                BackgroundColor = Color.Red,
                OnClick = () =>
                {
                    terrainRoughness = 5; // default value
                    mapGenerator?.SetRoughness(terrainRoughness);
                }
            });

            //Biome selection buttons 
            terrainButtons.Add(new Button
            {
                Bounds = new Rectangle(20, 100, 200, 40),
                Text = "Choose Biome 1",
                Font = font,
                OnClick = () =>
                {
                    selectedBiome = 1;
                    mapGenerator?.SetBiome(1);//Sets biome
                }
            });
            terrainButtons.Add(new Button
            {
                Bounds = new Rectangle(20, 200, 200, 40),
                Text = "Choose Biome 2",
                Font = font,
                OnClick = () =>
                {
                    selectedBiome = 2;
                    mapGenerator?.SetBiome(2);//Sets biome
                }
            });
            terrainButtons.Add(new Button
            {
                Bounds = new Rectangle(20, 300, 200, 40),
                Text = "Choose Biome 3",
                Font = font,
                OnClick = () =>
                {
                    selectedBiome = 3;
                    mapGenerator?.SetBiome(3);//Sets biome
                }
            });


            terrainButtons.Add(new Button//Exit button (terrain)
            {
                Bounds = new Rectangle(550, 300, 200, 50),
                Text = "Exit",
                Font = font,
                OnClick = () => currentGameState = GameState.MainMenu
            });

            //Pause UI buttons
            pauseContinueButton = new Button
            {
                Bounds = new Rectangle(300, 220, 200, 50),
                Text = "Continue",
                Font = font,
                OnClick = () => currentGameState = GameState.Playing//Returns to playing state 
            };
            pauseEndButton = new Button
            {
                Bounds = new Rectangle(300, 280, 200, 50),
                Text = "End Simulation",
                BackgroundColor = Color.Red,
                Font = font,
                OnClick = () =>
                {
                    //Store final time and a last population sample, then go to GameOver(for our graph)
                    lastSimulationDuration = simulationTimer;
                    rabbitHistory.Add(activeRabbits.Count);
                    foxHistory.Add(activeFoxes.Count);
                    plantHistory.Add(activePlants.Count); 
                    currentGameState = GameState.GameOver;
                }
            };
            pauseButtons.Add(pauseContinueButton);
            pauseButtons.Add(pauseEndButton);

            //Plant Textures
            grassTex = Content.Load<Texture2D>("Biome1GrassTrans");//was DesertGrass
            thornsTex = Content.Load<Texture2D>("ThornsTexture1");//was cactus

            //Rabbit Texture
            rabbitTex = Content.Load<Texture2D>("rabbitrun");//was rabbitrun
 

            //Fox Texture
            foxTexture = Content.Load<Texture2D>("foxrun8");//was foxrun8

            // Pixel used to draw borders and outlines (just for testing)
            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

         
            // Try to load tutorial images named "tutorial1" .. "tutorialN". If missing, keep null so draw will show a placeholder.
            tutorialSlides.Clear();
            for (int i = 1; i <= TutorialSlideCount; i++)
            {
                try
                {
                    var tex = Content.Load<Texture2D>($"tutorial{i}");
                    tutorialSlides.Add(tex);
                }
                catch
                {
                    tutorialSlides.Add(null);
                }
            }

            // Simple explanatory text for each slide (keeps UI self-contained)
            tutorialTexts = new string[ TutorialSlideCount ]
            {
                "Main Menu.",
                "Terrain",
                "Settings",
                "Play",
                "Graph"
            };

            // Tutorial buttons (keeps layout similar to settings)
            tutorialBackButton = new Button
            {
                Bounds = new Rectangle(160, 430, 120, 40),
                Text = "< Back",
                Font = font,
                OnClick = () => { if (tutorialIndex > 0) tutorialIndex--; }
            };
            tutorialNextButton = new Button
            {
                Bounds = new Rectangle(480, 430, 120, 40),
                Text = "Next >",
                Font = font,
                OnClick = () => { if (tutorialIndex < TutorialSlideCount - 1) tutorialIndex++; }
            };
            tutorialExitButton = new Button
            {
                Bounds = new Rectangle(650, 430, 140, 40),
                Text = "Exit to Menu",
                Font = font,
                BackgroundColor = Color.Red,
                OnClick = () => currentGameState = GameState.MainMenu
            };
            tutorialRestartButton = new Button
            {
                Bounds = new Rectangle(320, 430, 150, 40),
                Text = "Restart Slides",
                Font = font,
                OnClick = () => tutorialIndex = 0
            };
            tutorialStartButton = new Button
            {
                Bounds = new Rectangle(320, 480, 200, 40),
                Text = "Start Simulation",
                Font = font,
                BackgroundColor = Color.Green,
                OnClick = () =>
                {
                    // Transition to playing; ensure simulation re-initializes
                    rabbitsSpawned = false;
                    activePlants.Clear();
                    activeRabbits.Clear();
                    activeFoxes.Clear();
                    rabbitHistory.Clear();
                    foxHistory.Clear();
                    plantHistory.Clear();
                    historySampleTimer = 0f;
                    simulationTimer = 0f;
                    currentGameState = GameState.Playing;
                }
            };
        }

        private void SpawnRabbits(int mapPixelWidth, int mapPixelHeight)
        {
            activeRabbits.Clear(); // Clear any existing rabbits

            int margin = 10;
            //match Rabbit draw scale
            int rabbitWidth = (int)(rabbitTex?.Width * 0.7f ?? 8);//70% of texture's width, if null then it defualts to 8
            int rabbitHeight = (int)(rabbitTex?.Height * 0.7f ?? 8);//70% of texture's height, if null then it defualts to 8

            int minX = margin;
            int minY = margin;
            int maxXExclusive = Math.Max(minX + 1, mapPixelWidth - margin - rabbitWidth + 1);
            int maxYExclusive = Math.Max(minY + 1, mapPixelHeight - margin - rabbitHeight + 1);

            for (int i = 0; i < rabbitCount; i++)
            {
                //Spawn rabbits at random positions on the map
                Vector2 spawnPosition = new Vector2(
                    rng.Next(minX, maxXExclusive),//randomness
                    rng.Next(minY, maxYExclusive)//randomness
                );

                activeRabbits.Add(new Rabbit(spawnPosition, rabbitTex));
            }

            rabbitsSpawned = true;
        }
        private void SpawnFoxes(int mapPixelWidth, int mapPixelHeight)
        {
            activeFoxes.Clear(); //Clear any existing foxes

            int margin = 10;
            //Fox sprite scale
            int foxWidth = (int)(foxTexture?.Width * 2f ?? 16);//scales width by 2 if null then it sets it to 16
            int foxHeight = (int)(foxTexture?.Height * 2f ?? 16);//scales height by 2 if null then it sets it to 16

            int minX = margin;
            int minY = margin;
            int maxXExclusive = Math.Max(minX + 1, mapPixelWidth - margin - foxWidth + 1);
            int maxYExclusive = Math.Max(minY + 1, mapPixelHeight - margin - foxHeight + 1);

            for (int i = 0; i < foxCount; i++)
            {
                //Spawn foxes at random positions on the map
                Vector2 spawnPosition = new Vector2(
                    rng.Next(minX, maxXExclusive),
                    rng.Next(minY, maxYExclusive)
                );

                activeFoxes.Add(new Fox(spawnPosition, foxTexture));
            }
        }

        private (int width, int height) GetEffectiveMapSize()
        {
            if (mapGenerator == null)
                return (GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            int w = Math.Min(mapGenerator.PixelWidth, GraphicsDevice.Viewport.Width);
            int h = Math.Min(mapGenerator.PixelHeight, GraphicsDevice.Viewport.Height);
            return (w, h);
        }

        protected override void Update(GameTime gameTime)
        {
            currentMouse = Mouse.GetState();
            var currentKeyboard = Keyboard.GetState();

            if (currentGameState == GameState.MainMenu)//Main menu
            {
                foreach (var button in menuButtons)
                    button.Update(currentMouse, previousMouse);

                // Reset spawn flag when returning to menu
                rabbitsSpawned = false;
            }
            if (currentGameState == GameState.Playing) //Playing
            {
                //toggle pause on P key (edge detect)
                if (currentKeyboard.IsKeyDown(Keys.P) && !previousKeyboard.IsKeyDown(Keys.P))
                {
                    currentGameState = GameState.Paused;//was paused
                }

                //Initialize map if needed
                if (mapGenerator == null) //if maps not working 
                {
                    mapGenerator = new MapGenerator(80, 60, GraphicsDevice);
                    mapGenerator.LoadContent();//generate a new one
                }

                //Ensure map generator uses currently selected biome
                mapGenerator?.SetBiome(selectedBiome);

                // compute effective map size clipped to viewport so animals never go past the visible bottom/right
                var (mapW, mapH) = GetEffectiveMapSize();

                //Spawn rabbits once when entering play mode
                if (!rabbitsSpawned)
                {
                    SpawnRabbits(mapW, mapH);
                    SpawnFoxes(mapW, mapH); //Also decided to spawn the foxes here
                                            //clear any old history when starting a new simulation
                    rabbitHistory.Clear();
                    foxHistory.Clear();
                    plantHistory.Clear();
                    historySampleTimer = 0f;

                    // reset simulation timer
                    simulationTimer = 0f;
                    lastSimulationDuration = 0f;
                }

                //continue simulation time
                simulationTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

                float time = (float)gameTime.TotalGameTime.TotalSeconds;

                //Plants spawning 
                plantSpawnTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (plantSpawnTimer >= plantSpawnInterval)
                {
                    plantSpawnTimer = 0f;

                    int plantSize = 50;// was 10
                    int margin = 10;
                    int minX = margin;
                    int minY = margin;
                    int maxXExclusive = Math.Max(minX + 1, mapW - margin - plantSize + 1);
                    int maxYExclusive = Math.Max(minY + 1, mapH - margin - plantSize + 1);

                    Vector2 position = new Vector2(rng.Next(minX, maxXExclusive), rng.Next(minY, maxYExclusive));
                    string type = rng.NextDouble() < 0.8 ? "Grass" : "Thorns";
                    Texture2D tex = type == "Grass" ? grassTex : thornsTex;

                    activePlants.Add(new Plant(position, type, time, tex, plantSize));
                }

                //Despawn plants
                for (int i = activePlants.Count - 1; i >= 0; i--)
                {
                    if (time - activePlants[i].SpawnTime > 15f)
                    {
                        //Check if any rabbit is currently eating this plant
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
                            //If any rabbits were targeting this plant, clear their target (and release assigned slot)
                            foreach (var rabbit in activeRabbits)
                            {
                                if (rabbit.TargetPlant == activePlants[i])
                                    rabbit.ClearTarget();
                            }

                            activePlants.RemoveAt(i);
                        }
                    }
                }

                foreach (var fox in activeFoxes)//Foxes update
                {
                    fox.Update(gameTime, activeRabbits, mapW, mapH);
                }
                activeFoxes.RemoveAll(r => !r.Alive);//removes the dead foxes

                //Update all rabbits
                foreach (var rabbit in activeRabbits)
                {
                    rabbit.Update(gameTime, activePlants, activeFoxes, activeRabbits, mapW, mapH);
                }

                //Release targets for any rabbits that died during update before removing them
                foreach (var dead in activeRabbits.Where(r => !r.Alive).ToList())
                {
                    dead.ClearTarget();
                }

                activeRabbits.RemoveAll(r => !r.Alive); //removes the dead rabbits 

                //Breeding of rabbits 
                float breedDistance = 20f;
                List<Rabbit> newBabies = new List<Rabbit>();
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
                                // Spawns rabbit in the middle and make sure it is within map bounds
                                Vector2 spawnPos = (r1.Position + r2.Position) / 2f;
                                spawnPos.X = MathHelper.Clamp(spawnPos.X, 0f, Math.Max(0, mapW - 8));
                                spawnPos.Y = MathHelper.Clamp(spawnPos.Y, 0f, Math.Max(0, mapH - 8));

                                //Determine mutation for offspring...
                                bool offspringMutated = false;
                                if (r1.IsMutated && r2.IsMutated)
                                {
                                    offspringMutated = true;
                                }
                                else if (r1.IsMutated ^ r2.IsMutated)
                                {
                                    double chance = 0.5 + (mutationChance / 100.0);
                                    if (chance > 1.0) chance = 1.0;
                                    offspringMutated = rng.NextDouble() < chance;
                                }
                                else
                                {
                                    offspringMutated = rng.NextDouble() < (mutationChance / 100.0);
                                }

                                newBabies.Add(new Rabbit(spawnPos, rabbitTex, offspringMutated));
                                r1.MarkBred();
                                r2.MarkBred();

                                break;
                            }
                        }
                    }
                }

                if (newBabies.Count > 0)
                    activeRabbits.AddRange(newBabies);

                // Fox breeding (use mapW/mapH similarly)
                float foxBreedDistance = 20f;
                var newFoxes = new List<Fox>();
                for (int i = 0; i < activeFoxes.Count; i++)
                {
                    var f1 = activeFoxes[i];
                    if (!f1.Alive) continue;
                    for (int j = i + 1; j < activeFoxes.Count; j++)
                    {
                        var f2 = activeFoxes[j];
                        if (!f2.Alive) continue;

                        if (f1.CanBreed() && f2.CanBreed())
                        {
                            if (Vector2.Distance(f1.Position, f2.Position) <= foxBreedDistance)
                            {
                                Vector2 spawnPos = (f1.Position + f2.Position) / 2f;
                                spawnPos.X = MathHelper.Clamp(spawnPos.X, 0f, Math.Max(0, mapW - 8));
                                spawnPos.Y = MathHelper.Clamp(spawnPos.Y, 0f, Math.Max(0, mapH - 8));

                                newFoxes.Add(new Fox(spawnPos, foxTexture));
                                f1.MarkBred();
                                f2.MarkBred();

                                break;
                            }
                        }
                    }
                }

                if (newFoxes.Count > 0)
                    activeFoxes.AddRange(newFoxes);

                if (newFoxes.Count > 0)
                    activeFoxes.AddRange(newFoxes);

                // Sample population history at fixed interval (For the graph) 
                historySampleTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (historySampleTimer >= historySampleInterval)
                {
                    historySampleTimer -= historySampleInterval;
                    rabbitHistory.Add(activeRabbits.Count);
                    foxHistory.Add(activeFoxes.Count);
                    plantHistory.Add(activePlants.Count);
                }

                if (activeRabbits.Count == 0 || activeFoxes.Count == 0)// If one of the species is dead
                {
                    // store final simulation time for GameOver screen
                    lastSimulationDuration = simulationTimer;

                    // add a final sample for the graph
                    rabbitHistory.Add(activeRabbits.Count);
                    foxHistory.Add(activeFoxes.Count);
                    plantHistory.Add(activePlants.Count);

                    currentGameState = GameState.GameOver; //Game over
                }

                // Exit to menu with Escape key
                if (currentKeyboard.IsKeyDown(Keys.Escape))
                {
                    currentGameState = GameState.MainMenu;
                    // Clear game objects
                    activePlants.Clear();
                    activeRabbits.Clear();
                    mapGenerator = null;
                    rabbitsSpawned = false;
                    rabbitHistory.Clear();
                    foxHistory.Clear();
                    plantHistory.Clear();
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
                // make sure map exists
                if (mapGenerator == null)
                {
                    mapGenerator = new MapGenerator(80, 60, GraphicsDevice);
                    mapGenerator.LoadContent();
                    // ensure mapGenerator reflects current terrainRoughness
                    mapGenerator.SetRoughness(terrainRoughness);
                    mapGenerator.SetBiome(selectedBiome);
                }

                // Apply selected biome
                mapGenerator.SetBiome(selectedBiome);

                roughnessMinus.Update(currentMouse, previousMouse);
                roughnessPlus.Update(currentMouse, previousMouse);

                foreach (var button in terrainButtons)
                    button.Update(currentMouse, previousMouse);
            }

            if (currentGameState == GameState.Playing) //Doesn't work
            {
                // Pause toggle 
                if (currentKeyboard.IsKeyDown(Keys.P) && !previousKeyboard.IsKeyDown(Keys.P))
                {
                    currentGameState = GameState.Playing;
                }

                // Update pause menu buttons
                foreach (var b in pauseButtons)
                    b.Update(currentMouse, previousMouse);

                // Allow Escape to return to main menu (will clear simulation)
                if (currentKeyboard.IsKeyDown(Keys.Escape))
                {
                    currentGameState = GameState.MainMenu;
                    activePlants.Clear();
                    activeRabbits.Clear();
                    activeFoxes.Clear();
                    mapGenerator = null;
                    rabbitsSpawned = false;
                    rabbitHistory.Clear();
                    foxHistory.Clear();
                    plantHistory.Clear();
                    historySampleTimer = 0f;
                    simulationTimer = 0f;
                }
            }

            if (currentGameState == GameState.Tutorial)
            {
                // Mouse-driven buttons
                tutorialBackButton.Update(currentMouse, previousMouse);
                tutorialNextButton.Update(currentMouse, previousMouse);
                tutorialExitButton.Update(currentMouse, previousMouse);

                // Show restart / start options on last slide
                if (tutorialIndex == TutorialSlideCount - 1)
                {
                    tutorialRestartButton.Update(currentMouse, previousMouse);
                    tutorialStartButton.Update(currentMouse, previousMouse);
                }

                // Keyboard navigation (edge detect)
                if (currentKeyboard.IsKeyDown(Keys.Right) && !previousKeyboard.IsKeyDown(Keys.Right))
                {
                    if (tutorialIndex < TutorialSlideCount - 1)
                        tutorialIndex++;
                }
                if (currentKeyboard.IsKeyDown(Keys.Left) && !previousKeyboard.IsKeyDown(Keys.Left))
                {
                    if (tutorialIndex > 0)
                        tutorialIndex--;
                }
                // Escape exits to menu
                if (currentKeyboard.IsKeyDown(Keys.Escape))
                {
                    currentGameState = GameState.MainMenu;
                }
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
                    plantHistory.Clear();
                    historySampleTimer = 0f;
                    simulationTimer = 0f;
                    lastSimulationDuration = 0f;
                }
            }

            previousMouse = currentMouse;
            previousKeyboard = currentKeyboard;
            base.Update(gameTime);
        }
        private void DrawTextOutlined(SpriteBatch sb, string text, Vector2 position, Color fill, Color outline, float scale = 1f, int outlinePixels = 1)
        {
            // draw outline by drawing the text offset around the main position
            for (int ox = -outlinePixels; ox <= outlinePixels; ox++)
            {
                for (int oy = -outlinePixels; oy <= outlinePixels; oy++)
                {
                    if (ox == 0 && oy == 0) continue;
                    sb.DrawString(font, text, new Vector2(position.X + ox, position.Y + oy), outline, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
            sb.DrawString(font, text, position, fill, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
      

        private void DrawPopulationGraph(SpriteBatch sb, Rectangle area)
        {
            // background
            sb.Draw(pixelTexture, area, Color.Black * 0.9f);

            int padding = 18;
            var plotRect = new Rectangle(area.X + padding, area.Y + padding, area.Width - padding * 2, area.Height - padding * 2);

            // thicker axes
            int axisThickness = 3;
            sb.Draw(pixelTexture, new Rectangle(plotRect.X, plotRect.Y, axisThickness, plotRect.Height), Color.LightGray); // y axis
            sb.Draw(pixelTexture, new Rectangle(plotRect.X, plotRect.Y + plotRect.Height - axisThickness, plotRect.Width, axisThickness), Color.LightGray); // x axis

            int points = Math.Max(Math.Max(rabbitHistory.Count, foxHistory.Count), plantHistory.Count);
            if (points < 2)
            {
                DrawTextOutlined(sb, "Not enough data to plot.", new Vector2(area.X + 20, area.Y + 20), Color.White, Color.Black, 1f, 2);
                return;
            }

            int maxY = 1;
            if (rabbitHistory.Count > 0) maxY = Math.Max(maxY, rabbitHistory.Max());
            if (foxHistory.Count > 0) maxY = Math.Max(maxY, foxHistory.Max());
            if (plantHistory.Count > 0) maxY = Math.Max(maxY, plantHistory.Max());

            // draw horizontal grid lines and labels (4 ticks)
            int ticks = 4;
            float labelScale = 0.9f;
            for (int t = 0; t <= ticks; t++)
            {
                float frac = t / (float)ticks;
                int y = plotRect.Y + (int)((1 - frac) * plotRect.Height);
                sb.Draw(pixelTexture, new Rectangle(plotRect.X, y, plotRect.Width, 1), Color.Gray * 0.35f);
                int label = (int)Math.Round(frac * maxY);
                DrawTextOutlined(sb, label.ToString(), new Vector2(plotRect.X - 42, y - 10), Color.White, Color.Black, labelScale, 2);
            }

            // Helper to map sample index/value to screen coords
            float xStep = (float)plotRect.Width / (points - 1);
            float yScale = (float)plotRect.Height / Math.Max(1, maxY);

            // draw polylines with thicker strokes
            Color rabbitColor = Color.Yellow;
            for (int i = 1; i < rabbitHistory.Count; i++)
            {
                float x1 = plotRect.X + (i - 1) * xStep;
                float x2 = plotRect.X + i * xStep;
                float y1 = plotRect.Y + plotRect.Height - rabbitHistory[i - 1] * yScale;
                float y2 = plotRect.Y + plotRect.Height - rabbitHistory[i] * yScale;
                DrawLine(sb, new Vector2(x1, y1), new Vector2(x2, y2), rabbitColor, 3);
            }

            Color foxColor = Color.Red;
            for (int i = 1; i < foxHistory.Count; i++)
            {
                float x1 = plotRect.X + (i - 1) * xStep;
                float x2 = plotRect.X + i * xStep;
                float y1 = plotRect.Y + plotRect.Height - foxHistory[i - 1] * yScale;
                float y2 = plotRect.Y + plotRect.Height - foxHistory[i] * yScale;
                DrawLine(sb, new Vector2(x1, y1), new Vector2(x2, y2), foxColor, 2);
            }

            Color plantColor = Color.Lime;
            for (int i = 1; i < plantHistory.Count; i++)
            {
                float x1 = plotRect.X + (i - 1) * xStep;
                float x2 = plotRect.X + i * xStep;
                float y1 = plotRect.Y + plotRect.Height - plantHistory[i - 1] * yScale;
                float y2 = plotRect.Y + plotRect.Height - plantHistory[i] * yScale;
                DrawLine(sb, new Vector2(x1, y1), new Vector2(x2, y2), plantColor, 3);
            }

            //legend box
            int legendW = 260;
            int legendH = 80;
            int legendX = plotRect.X + 8;
            int legendY = plotRect.Y + 8;
            sb.Draw(pixelTexture, new Rectangle(legendX - 6, legendY - 6, legendW + 12, legendH + 12), Color.Black * 0.6f);
            sb.Draw(pixelTexture, new Rectangle(legendX - 6, legendY - 6, legendW + 12, 2), Color.Gray * 0.5f);

            //legend entries with small colored squares
            int entryX = legendX;
            int entryY = legendY;
            int sw = 10;
            sb.Draw(pixelTexture, new Rectangle(entryX, entryY, sw, sw), rabbitColor);
            DrawTextOutlined(sb, $" Rabbits (final: {rabbitHistory.LastOrDefault()})", new Vector2(entryX + sw + 8, entryY - 2), Color.White, Color.Black, 0.95f, 2);
            entryY += 20;
            sb.Draw(pixelTexture, new Rectangle(entryX, entryY, sw, sw), foxColor);
            DrawTextOutlined(sb, $" Foxes (final: {foxHistory.LastOrDefault()})", new Vector2(entryX + sw + 8, entryY - 2), Color.White, Color.Black, 0.95f, 2);
            entryY += 20;
            sb.Draw(pixelTexture, new Rectangle(entryX, entryY, sw, sw), plantColor);
            DrawTextOutlined(sb, $" Plants (final: {plantHistory.LastOrDefault()})", new Vector2(entryX + sw + 8, entryY - 2), Color.White, Color.Black, 0.95f, 2);

            // small axis labels
            DrawTextOutlined(sb, "Time ->", new Vector2(plotRect.X + plotRect.Width - 48, plotRect.Y + plotRect.Height + 6), Color.White, Color.Black, 0.85f, 2);
            DrawTextOutlined(sb, "Population", new Vector2(plotRect.X - 58, plotRect.Y - 28), Color.White, Color.Black, 0.85f, 2);
        }
        private void DrawLine(SpriteBatch sb, Vector2 start, Vector2 end, Color color, int thickness = 1)
        {
            //draw line using pixelTexture
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

        private static string FormatTime(float seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return $"{(int)ts.TotalMinutes:D2}:{ts.Seconds:D2}";
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clear to biome background when playing or previewing Terrain; otherwise keep black
            Color clearColor = Color.Black;
            if (currentGameState == GameState.Playing || currentGameState == GameState.Terrain)
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

                    var mapRect = new Rectangle(0, 0, mapGenerator.PixelWidth, mapGenerator.PixelHeight);

                }

                // Draw UI information
                _spriteBatch.DrawString(font, $"Plants: {activePlants.Count}", new Vector2(10, 10), Color.White);
                _spriteBatch.DrawString(font, $"Rabbits: {activeRabbits.Count}", new Vector2(10, 30), Color.White);
                _spriteBatch.DrawString(font, $"Foxes: {activeFoxes.Count}", new Vector2(10, 50), Color.White);
                _spriteBatch.DrawString(font, $"Time: {FormatTime(simulationTimer)}", new Vector2(10, 70), Color.White);
                _spriteBatch.DrawString(font, "Press P to pause, ESC to return to menu", new Vector2(10, 570), Color.White);
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
                // draw map preview
                mapGenerator?.Draw(_spriteBatch);

                _spriteBatch.DrawString(font, "Terrain Editor", new Vector2(330, 30), Color.White);

                // Biome descriptions
                _spriteBatch.DrawString(font, "-Grasslands", new Vector2(220, 100), Color.White);
                _spriteBatch.DrawString(font, "-Tundra", new Vector2(220, 200), Color.White);
                _spriteBatch.DrawString(font, "-Desert ", new Vector2(220, 300), Color.White);

                // Roughness controls
                _spriteBatch.DrawString(font, "Terrain Roughness:", new Vector2(540, 80), Color.White);
                _spriteBatch.DrawString(font, terrainRoughness.ToString(), new Vector2(630, 125), Color.White);
                roughnessMinus.Draw(_spriteBatch);
                roughnessPlus.Draw(_spriteBatch);

                // Draw buttons
                foreach (var button in terrainButtons)
                    button.Draw(_spriteBatch);
            }
            else if (currentGameState == GameState.Paused) //If paused
            {
                // dim background
                _spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * 0.6f);

                _spriteBatch.DrawString(font, "PAUSED", new Vector2(340, 60), Color.White);
                _spriteBatch.DrawString(font, $"Time: {FormatTime(simulationTimer)}", new Vector2(320, 110), Color.White);
                _spriteBatch.DrawString(font, $"Plants: {activePlants.Count}", new Vector2(320, 140), Color.White);
                _spriteBatch.DrawString(font, $"Rabbits: {activeRabbits.Count}", new Vector2(320, 170), Color.White);
                _spriteBatch.DrawString(font, $"Foxes: {activeFoxes.Count}", new Vector2(320, 200), Color.White);
                _spriteBatch.DrawString(font, "Press P to resume", new Vector2(320, 240), Color.White);

                // Draw pause menu buttons
                foreach (var b in pauseButtons)
                    b.Draw(_spriteBatch);
            }

            else if (currentGameState == GameState.Tutorial) //Tutorial
            {
                
                _spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.Black * 0.6f);
               // _spriteBatch.DrawString(font, "Tutorial", new Vector2(340, 10), Color.White);

                // Slide area
                var slideRect = new Rectangle(-325, -10, 1450, 450);//120, 60, 600, 360

                // Draw slide texture when available, otherwise draw placeholder
                var tex = tutorialSlides.ElementAtOrDefault(tutorialIndex);
                if (tex != null)
                {
                    // Fit texture into slideRect preserving aspect ratio
                    float texAspect = (float)tex.Width / tex.Height;
                    float rectAspect = (float)slideRect.Width / slideRect.Height;
                    Rectangle dest = slideRect;
                    if (texAspect > rectAspect)
                    {
                        // texture is wider -> fit width
                        int h = (int)(slideRect.Width / texAspect);
                        dest = new Rectangle(slideRect.X, slideRect.Y + (slideRect.Height - h) / 2, slideRect.Width, h);
                    }
                    else
                    {
                        // texture is taller -> fit height
                        int w = (int)(slideRect.Height * texAspect);
                        dest = new Rectangle(slideRect.X + (slideRect.Width - w) / 2, slideRect.Y, w, slideRect.Height);
                    }
                    _spriteBatch.Draw(tex, dest, Color.White);
                }
                else
                {
                    // placeholder rectangle
                    _spriteBatch.Draw(pixelTexture, slideRect, Color.DarkGray * 0.95f);
                    DrawTextOutlined(_spriteBatch, $"Slide {tutorialIndex + 1} (image not found)", new Vector2(slideRect.X + 20, slideRect.Y + 20), Color.White, Color.Black, 1f, 2);
                }

               
               
                // Draw navigation buttons (left, right, exit)
                tutorialBackButton.Draw(_spriteBatch);
                tutorialNextButton.Draw(_spriteBatch);
                tutorialExitButton.Draw(_spriteBatch);

                // If last slide, show restart and start simulation options
                if (tutorialIndex == TutorialSlideCount - 1)
                {
                    tutorialRestartButton.Draw(_spriteBatch);
                    tutorialStartButton.Draw(_spriteBatch);
                }

                // small hint
                DrawTextOutlined(_spriteBatch, "Use Left/Right arrows or click the buttons to navigate. ESC = Exit to menu", new Vector2(110, slideRect.Y + slideRect.Height + 76), Color.LightGray, Color.Black, 0.85f, 2);
            }
            else if (currentGameState == GameState.GameOver)
            {
                _spriteBatch.DrawString(font, "Game Over", new Vector2(330, 20), Color.White);

                // Graph area
                var graphArea = new Rectangle(100, 60, 600, 400);
                DrawPopulationGraph(_spriteBatch, graphArea);

                //summary text
               // _spriteBatch.DrawString(font, $"Final Rabbits: {rabbitHistory.LastOrDefault()}", new Vector2(550, 10), Color.Yellow);
               // _spriteBatch.DrawString(font, $"Final Foxes: {foxHistory.LastOrDefault()}", new Vector2(550, 50), Color.Red);
               // _spriteBatch.DrawString(font, $"Final Plants: {plantHistory.LastOrDefault()}", new Vector2(550, 90), Color.Lime); 
                _spriteBatch.DrawString(font, $"Simulation Time: {FormatTime(lastSimulationDuration)}", new Vector2(550, 10), Color.White);
                _spriteBatch.DrawString(font, "Press ESC to return to main menu", new Vector2(10, 30), Color.White);
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}