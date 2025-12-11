using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static System.Net.Mime.MediaTypeNames;

namespace Project1
{
    public static class Noise // value / Perlin-like noise (coherent interpolated noise + fBm)
    {


        //I understand this more now, Will update and describe in own words, tricky one to explain

        // Generate coherent noise in range [0,1]. 'x' and 'y' are integer tile coordinates.
        // 'scale' controls feature size (larger scale -> larger features = smoother terrain),
        // 'seed' is a random seed, 'octaves' increases detail when >1.
        public static float Generate(int x, int y, float scale, int seed, int octaves = 1)
        {
            float fx = x * scale;
            float fy = y * scale;
            return FBM(fx, fy, seed, octaves);
            //lactice coordinates
        }

        // fractal Brownian motion: combine octaves of smooth value noise
        private static float FBM(float x, float y, int seed, int octaves)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float persistence = 0.5f;
            float max = 0f;
            //
            for (int i = 0; i < Math.Max(1, octaves); i++)
            {
                total += amplitude * InterpolatedNoise(x * frequency, y * frequency, seed + i * 1337);
                max += amplitude;
                amplitude *= persistence;
                frequency *= 2f;
            }

            float result = total / Math.Max(0.0001f, max); // normalize to approximately -1..1
            // InterpolatedNoise returns in [-1,1] so map to [0,1]
            return Math.Clamp(result * 0.5f + 0.5f, 0f, 1f);
        }

        // Smooth value noise via bilinear interpolation with a smoothstep curve
        private static float InterpolatedNoise(float x, float y, int seed)
        {
            int xi = (int)Math.Floor(x);
            int yi = (int)Math.Floor(y);

            float xf = x - xi;
            float yf = y - yi;

            float v00 = ValueNoise(xi, yi, seed);
            float v10 = ValueNoise(xi + 1, yi, seed);
            float v01 = ValueNoise(xi, yi + 1, seed);
            float v11 = ValueNoise(xi + 1, yi + 1, seed);

            float sx = SmoothStep(xf);
            float sy = SmoothStep(yf);

            float ix0 = Lerp(v00, v10, sx);
            float ix1 = Lerp(v01, v11, sx);
            float value = Lerp(ix0, ix1, sy);

            return value;
        }

        // deterministic pseudo-random value per integer lattice point in [-1,1]
        private static float ValueNoise(int xi, int yi, int seed)
        {
            unchecked
            {
                int n = xi;
                n = n * 374761393 + yi * 668265263 + seed * 2147483647;
                n = (n ^ (n >> 13)) * 1274126177;
                n = n ^ (n >> 16);
                // map to [-1,1]
                return 1f - ((n & 0x7fffffff) / 1073741824f);
            }
        }

        private static float SmoothStep(float t) => t * t * (3f - 2f * t);
        private static float Lerp(float a, float b, float t) => a + (b - a) * t;
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

        // Roughness: 1..10. Higher -> more contours / finer detail.
        private int roughnessLevel = 5;

        public MapGenerator(int width, int height, GraphicsDevice graphicsDevice)
        {
            this.width = width;
            this.height = height;
            this.graphicsDevice = graphicsDevice;
            noiseMap = new float[width, height];
            GenerateNoiseMap();
        }

        private void GenerateNoiseMap()//Use of Perlin-like fBm noise
        {
            // Map roughness (1..10) to a base scale and octaves:
            // - Higher roughness -> smaller scale (finer features) and more octaves (more contours).
            // Chosen mapping: scale = baseScale / roughnessLevel, baseScale tuned for tile grid.
            float baseScale = 0.08f; // tuned experimentally for map sizes used in project
            float scale = baseScale / Math.Max(1, roughnessLevel);

            int seed = new Random().Next(0, 10000);
            int octaves = Math.Min(6, 1 + roughnessLevel / 2); // more octaves for greater detail

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float noiseValue = Noise.Generate(x, y, scale, seed, octaves);
                    noiseMap[x, y] = noiseValue;
                }
            }
        }

        // Public setter so callers (Game1) can change roughness and immediately regenerate the map
        public void SetRoughness(int level)
        {
            int clamped = Math.Clamp(level, 1, 10);
            if (clamped == roughnessLevel)
                return;
            roughnessLevel = clamped;
            GenerateNoiseMap();
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
                case 3: // desert
                    CreateTextures(
                        grass: new Color(210, 180, 140), // sand
                        plant: new Color(200, 160, 80),  // dry plants 
                        water: new Color(210, 180, 140));//made this same as sand as no water in desert
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
                    // thresholding noise to pick a texture; noiseMap in [0,1].
                    Texture2D texture = noiseMap[x, y] < 0.5f ? plantTexture : grassTexture; //was using waterTexture
                    spriteBatch.Draw(texture, new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize), Color.White);//draws map
                }
            }
        }
    }

}
