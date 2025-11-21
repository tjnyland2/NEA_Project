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

        public void LoadContent()//Map textures
        {
            grassTexture = new Texture2D(graphicsDevice, 1, 1);
            grassTexture.SetData(new[] { Color.ForestGreen });
            // grassTexture2 = Content.Load<Texture2D>(@"grassTextureImage");

            waterTexture = new Texture2D(graphicsDevice, 1, 1);
            waterTexture.SetData(new[] { Color.DarkBlue });

            plantTexture = new Texture2D(graphicsDevice, 1, 1);
            plantTexture.SetData(new[] { Color.DarkGreen });

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
