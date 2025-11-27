using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;

namespace Project1
{
    public static class Noise//Perlin's Noise
    {
        public static float Generate(int x, int y, float scale, int seed)
        {
            Random rand = new Random(x * 49632 + y * 325176 + seed);
            return (float)rand.NextDouble();
        }
    }

    public class MapGenerator
    {
        private int width, height;
        private float[,] noiseMap;
        private Texture2D grassTexture, waterTexture, plantTexture, grassTexture2;
        private GraphicsDevice graphicsDevice;

        // Tile size in pixels (exposed so callers can compute pixel dimensions)
        public int TileSize { get; private set; } = 10;

        public int MapTilesWidth => width;
        public int MapTilesHeight => height;
        public int PixelWidth => width * TileSize;
        public int PixelHeight => height * TileSize;

        private int currentBiomeId = -1; // cached biome id -1 means none

        public MapGenerator(int width, int height, GraphicsDevice graphicsDevice)
        {
            this.width = width;
            this.height = height;
            this.graphicsDevice = graphicsDevice;
            noiseMap = new float[width, height];
            GenerateNoiseMap();
        }

        private void GenerateNoiseMap()//Use of Perlin's Noise
        {
            float scale = 0.1f;
            int seed = new Random().Next(0, 10000);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float noiseValue = Noise.Generate(x, y, scale, seed);
                    noiseMap[x, y] = noiseValue;
                }
            }
        }

        public void LoadContent()//Map textures (create defaults)
        {
            // default biome
            CreateTextures(Color.ForestGreen, Color.DarkGreen, Color.DarkBlue);
            currentBiomeId = -1; // ensure first SetBiome will create proper textures
        }

        // Call this to change the biome; will recreate 1x1 textures only when biome changes
        public void SetBiome(int biomeId)
        {
            if (biomeId == currentBiomeId)
                return;

            currentBiomeId = biomeId;

            switch (biomeId)
            {
                case 1: // Grassland style
                    CreateTextures(
                        grass: new Color(80, 160, 60),    // grass
                        plant: new Color(34, 139, 34),    // plants
                        water: new Color(28, 58, 148));   // water
                    break;
                case 2: // Mossy / pale
                    CreateTextures(
                        grass: new Color(120, 180, 120),
                        plant: new Color(170, 200, 170),
                        water: new Color(35, 75, 120));
                    break;
                case 3: // Arid / desert-like
                    CreateTextures(
                        grass: new Color(210, 180, 140), // sand
                        plant: new Color(200, 160, 80),  // dry plants 
                        water: new Color(40, 70, 110));
                    break;
                default: // fallback
                    CreateTextures(Color.ForestGreen, Color.DarkGreen, Color.DarkBlue);
                    break;
            }
        }

        private void CreateTextures(Color grass, Color plant, Color water)
        {
            // Dispose old textures safely (optional but good)
            grassTexture?.Dispose();
            plantTexture?.Dispose();
            waterTexture?.Dispose();

            grassTexture = new Texture2D(graphicsDevice, 1, 1);
            grassTexture.SetData(new[] { grass });

            plantTexture = new Texture2D(graphicsDevice, 1, 1);
            plantTexture.SetData(new[] { plant });

            waterTexture = new Texture2D(graphicsDevice, 1, 1);
            waterTexture.SetData(new[] { water });
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            int tileSize = TileSize; // use exposed tile size

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)//Makes a "grid" with x and y
                {
                    Texture2D texture = noiseMap[x, y] < 0.5f ? plantTexture : grassTexture; //was using waterTexture
                    spriteBatch.Draw(texture, new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize), Color.White);//draws map
                }
            }
        }
    }

}
