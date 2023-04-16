using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetaryTerrain
{
    /// <summary>
    /// Interface responsible for terrain coloring / texture selection. EvaluateTexture returns a float array of length 6, every float is the intensity value
    /// of a texture (index 0-5). The height/elevation (0-1) and normalized position (for heightmap or noise) of the to-be-textured vertex are passed.
    ///</summary>
    public interface ITextureProvider
    {
        float[] EvaluateTexture(float height, Vector3 normalized);
    }

    public class TextureProviderNone : ITextureProvider
    {
        public float[] EvaluateTexture(float height, Vector3 normalized)
        {
            return new float[6];
        }
    }


    [System.Serializable]
    public class TextureProviderGradient : ITextureProvider
    {
        const int maxNumIndicies = 6;

        public float[] heights;
        public int[] ids;

        public TextureProviderGradient(float[] heights, int[] ids)
        {
            this.heights = heights;
            this.ids = ids;
        }

        public TextureProviderGradient()
        {
            heights = new float[] { 0f, 0.01f, 0.02f, 0.75f, 1f };
            ids = new int[] { 0, 1, 2, 3, 4, 5 };
        }
        public float[] EvaluateTexture(float height, Vector3 normalized)
        {
            height = Mathf.Clamp01(height);
            int index;

            for (index = 0; index < heights.Length; index++)
                if (height < heights[index])
                    break;


            int index1 = Mathf.Clamp(index - 1, 0, heights.Length - 1);
            int index2 = Mathf.Clamp(index, 0, heights.Length - 1);

            float[] result = new float[maxNumIndicies];
            if (ids[index1] == ids[index2])
            {
                result[ids[index1]] = 1f;
                return result;
            }

            height = (height - heights[index1]) / (heights[index2] - heights[index1]);

            result[ids[index1]] = 1f - height;
            result[ids[index2]] = height;

            return result;
        }

        public Color32 EvaluateColor(float height, Color32[] sequence)
        {
            return Utils.FloatArrayToColor(EvaluateTexture(height, Vector3.zero), sequence);
        }

        /*public void AddKey(float height, int id)
        {
            int index;
            for (index = 0; index < heights.Count; index++)
            {
                if (height < heights[index])
                    break;
            }
            heights.Insert(index, height);
            ids.Insert(index, id);
        }

        public void RemoveKey(int index)
        {
            heights.RemoveAt(index);
            ids.RemoveAt(index);
        }

        public void EditKey(int index, float height)
        {
            bool decrease = heights[index] < height;

            heights[index] = height;

            if (decrease)
                while (index - 1 >= 0 && heights[index] < heights[index - 1])
                {
                    int temp = ids[index];
                    ids[index] = ids[index - 1];
                    heights[index] = heights[index - 1];

                    index--;

                    heights[index] = height;
                    ids[index] = temp;
                }
            else
                while (index + 1 < heights.Count && heights[index] > heights[index + 1])
                {
                    int temp = ids[index];
                    ids[index] = ids[index + 1];
                    heights[index] = heights[index + 1];

                    index++;

                    heights[index] = height;
                    ids[index] = temp;
                }
        }*/

        public Texture2D GetSampleTexture(Color32[] colors, int width = 256, int height = 32)
        {
            Color32[] pixels = new Color32[width * height];
            Vector3 zero = Vector3.zero;

            for (int x = 0; x < width; x++)
            {
                var value = EvaluateTexture((float)x / (float)(width - 1), zero);
                Color32 col = Utils.FloatArrayToColor(value, colors);

                for (int y = 0; y < height; y++)
                {
                    pixels[y * width + x] = col;
                }
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels32(pixels);
            texture.Apply();

            return texture;
        }
    }

    [System.Serializable]
    public class TextureProviderRange : ITextureProvider
    {
        public Vector2[] ranges;
        public int[] textures;

        public TextureProviderRange()
        {
            ranges = new Vector2[] { new Vector2(0f, 0.666667f), new Vector2(0.333333f, 1f) };
            textures = new int[] { 0, 1 };
        }

        public float[] EvaluateTexture(float height, Vector3 normalized)
        {
            float[] result = new float[] { 0, 0, 0, 0, 0, 0 };
            List<int> indicies = new List<int>();

            for (int i = 0; i < ranges.Length; i++)
            {
                if (height >= ranges[i].x && height <= ranges[i].y)
                {
                    indicies.Add(i);
                }
            }

            if (indicies.Count == 0)
                return result;

            if (indicies.Count == 1 || textures[indicies[0]] == textures[indicies[1]])
            {
                result[textures[indicies[0]]] = 1;
                return result;
            }

            float sumOfDistances = 0;
            for (int i = 0; i < indicies.Count; i++)
            {
                result[textures[indicies[i]]] = Mathf.Min(Mathf.Abs(ranges[indicies[i]].x - height), Mathf.Abs(ranges[indicies[i]].y - height)) / Mathf.Abs(ranges[indicies[i]].x - ranges[indicies[i]].y);
                sumOfDistances += result[textures[indicies[i]]];
            }

            result[0] /= sumOfDistances;
            result[1] /= sumOfDistances;
            result[2] /= sumOfDistances;
            result[3] /= sumOfDistances;
            result[4] /= sumOfDistances;
            result[5] /= sumOfDistances;

            return result;
        }

        public Texture2D GetSampleTexture(Color32[] colors, int width = 256, int height = 32)
        {
            Color32[] pixels = new Color32[width * height];
            Vector3 zero = Vector3.zero;

            for (int x = 0; x < width; x++)
            {
                var value = EvaluateTexture((float)x / (float)(width - 1), zero);
                Color32 col = Utils.FloatArrayToColor(value, colors);

                for (int y = 0; y < height; y++)
                {
                    pixels[y * width + x] = col;
                }
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels32(pixels);
            texture.Apply();

            return texture;
        }
    }


    [System.Serializable]
    public class TextureProviderSplatmap : ITextureProvider
    {
        public TextAsset[] textAssets;

        public enum DataType { Texture2d, TextAssets };
        public DataType dataType;
        public Texture2D textureA;
        public Texture2D textureB;

        public int[] heightmapColors;
        public bool useBicubicInterpolation;


        [System.NonSerialized]
        private Heightmap[] heightmaps;

        public TextureProviderSplatmap(Texture2D a, Texture2D b, bool useBicubicInterpolation)
        {
            dataType = DataType.Texture2d;

            textureA = a;
            textureB = b;

            this.useBicubicInterpolation = useBicubicInterpolation;
        }

        public TextureProviderSplatmap(TextAsset[] heightmapTextAssets, int[] heightmapColors, bool useBicubicInterpolation)
        {
            dataType = DataType.TextAssets;

            textAssets = heightmapTextAssets;
            this.heightmapColors = heightmapColors;
            this.useBicubicInterpolation = useBicubicInterpolation;
        }

        public TextureProviderSplatmap(DataType dataType, bool useBicubicInterpolation)
        {
            this.dataType = dataType;
            this.useBicubicInterpolation = useBicubicInterpolation;
        }

        public float[] EvaluateTexture(float height, Vector3 normalized)
        {
            float[] result = new float[6];

            double y = (Mathf.PI - Mathf.Acos(normalized.y));
            double x = (Mathf.Atan2(normalized.z, normalized.x) + Mathf.PI);

            x *= MathFunctions.TwoPIInv;
            y *= MathFunctions.PIInv;
            
            for (int i = 0; i < heightmaps.Length; i++)
            {
                result[heightmapColors[i]] = heightmaps[i].GetPixelInterpolated(x, y);
            }

            return result;
        }

        public void Init()
        {
            if (dataType == DataType.Texture2d)
            {
                
                int num = 0;
                if (textureA != null)
                    num += 3;
                if (textureB != null)
                    num += 3;

                heightmaps = new Heightmap[num];
                heightmapColors = new int[num];

                int index = 0;

                if (textureA != null)
                {
                    heightmapColors[0] = 0;
                    heightmapColors[1] = 1;
                    heightmapColors[2] = 2;

                    heightmaps[0] = new Heightmap(textureA, useBicubicInterpolation, 0);
                    heightmaps[1] = new Heightmap(textureA, useBicubicInterpolation, 1);
                    heightmaps[2] = new Heightmap(textureA, useBicubicInterpolation, 2);
                    index += 3;
                }

                if (textureB != null)
                {
                    heightmapColors[index] = 3;
                    heightmapColors[index + 1] = 4;
                    heightmapColors[index + 2] = 5;

                    heightmaps[index] = new Heightmap(textureB, useBicubicInterpolation, 0);
                    heightmaps[index + 1] = new Heightmap(textureB, useBicubicInterpolation, 1);
                    heightmaps[index + 2] = new Heightmap(textureB, useBicubicInterpolation, 2);
                }
            }
            else
            {
                heightmaps = new Heightmap[textAssets.Length];
                for (int i = 0; i < textAssets.Length; i++)
                {
                    heightmaps[i] = new Heightmap(textAssets[i], useBicubicInterpolation);
                }
            }
        }
    }
}
