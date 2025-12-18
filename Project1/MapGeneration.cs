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
        // Generate coherent noise in range [0,1]. 'x' and 'y' are integer tile coordinates.
        // 'scale' controls feature size (larger scale -> larger features = smoother terrain),
        // 'seed' is a random seed, 'octaves' increases detail when >1.
        public static float Generate(int x, int y, float scale, int seed, int octaves = 1)
        {
            float fx = x * scale;
            float fy = y * scale;
            return FBM(fx, fy, seed, octaves);
        }

        // fractal Brownian motion: combine octaves of smooth value noise
        private static float FBM(float x, float y, int seed, int octaves)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float persistence = 0.5f;
            float max = 0f;
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
        private Texture2D grassTexture, waterTexture, plantTexture;
        private GraphicsDevice graphicsDevice;

        // Optional art per biome loaded via Content.Load
        private readonly Dictionary<int, (Texture2D grassArt, Texture2D plantArt)> biomeArt = new();

        public int TileSize { get; private set; } = 10;
        public int MapTilesWidth => width;
        public int MapTilesHeight => height;
        public int PixelWidth => width * TileSize;
        public int PixelHeight => height * TileSize;

        private int currentBiomeId = -1;
        private int roughnessLevel = 5;

        public MapGenerator(int width, int height, GraphicsDevice graphicsDevice)
        {
            this.width = width;
            this.height = height;
            this.graphicsDevice = graphicsDevice;
            noiseMap = new float[width, height];
            GenerateNoiseMap();
        }

        private void GenerateNoiseMap()
        {
            float baseScale = 0.08f;
            float scale = baseScale / Math.Max(1, roughnessLevel);

            int seed = new Random().Next(0, 10000);
            int octaves = Math.Min(6, 1 + roughnessLevel / 2);

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    noiseMap[x, y] = Noise.Generate(x, y, scale, seed, octaves);
        }

        public void SetRoughness(int level)
        {
            int clamped = Math.Clamp(level, 1, 10);
            if (clamped == roughnessLevel) return;
            roughnessLevel = clamped;
            GenerateNoiseMap();
        }

        // Legacy fallback: create 1x1 color textures
        public void LoadContent()
        {
            CreateTextures(Color.ForestGreen, Color.DarkGreen, Color.DarkBlue);
            currentBiomeId = -1;
        }

        // New: attempt to load art assets for biomes; call this with Game1.Content
        public void LoadContent(ContentManager content)
        {
            // try load art for biomes 1..3 (adjust names if your assets differ)
            for (int b = 1; b <= 3; b++)
            {
                Texture2D grassArt = null;
                Texture2D plantArt = null;
                try { grassArt = content.Load<Texture2D>($"Biome{b}GrassTrans"); } catch { grassArt = null; }
                try { plantArt = content.Load<Texture2D>($"ThornsTexture{b}"); } catch { plantArt = null; }
                biomeArt[b] = (grassArt, plantArt);
            }

            // create fallback 1x1 textures; later SetBiome will override with art if available
            CreateTextures(Color.ForestGreen, Color.DarkGreen, Color.DarkBlue);
        }

        public void SetBiome(int biomeId)
        {
            if (biomeId == currentBiomeId) return;
            currentBiomeId = biomeId;

            switch (biomeId)
            {
                case 1:
                    CreateTextures(new Color(80, 160, 60), new Color(34, 139, 34), new Color(28, 58, 148));
                    break;
                case 2:
                    CreateTextures(new Color(120, 180, 120), new Color(170, 200, 170), new Color(35, 75, 120));
                    break;
                case 3:
                    CreateTextures(new Color(210, 180, 140), new Color(200, 160, 80), new Color(210, 180, 140));
                    break;
                default:
                    CreateTextures(Color.ForestGreen, Color.DarkGreen, Color.DarkBlue);
                    break;
            }

            // If art was loaded for this biome, use it to override the 1x1 textures
            if (biomeArt.TryGetValue(biomeId, out var art))
            {
                if (art.grassArt != null) grassTexture = art.grassArt;
                if (art.plantArt != null) plantTexture = art.plantArt;
            }
        }

        private void CreateTextures(Color grass, Color plant, Color water)
        {
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

        // Return the texture for a given plant type (visual only)
        public Texture2D GetPlantTexture(string type)
        {
            if (string.Equals(type, "Grass", StringComparison.OrdinalIgnoreCase))
                return grassTexture;
            if (string.Equals(type, "Thorns", StringComparison.OrdinalIgnoreCase))
                return plantTexture;
            return grassTexture;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            int tileSize = TileSize;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Texture2D texture = noiseMap[x, y] < 0.5f ? plantTexture : grassTexture;
                    spriteBatch.Draw(texture, new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize), Color.White);
                }
            }
        }
    }
}
