using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain.DoubleMath;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;


namespace PlanetaryTerrain
{

    public static class Utils
    {


        /// <summary>
        /// Gradient with float array instead of color
        /// </summary>
        public static float[] EvaluateTexture(float time, float[] textureHeights, int[] textureIds)
        {
            time = Mathf.Clamp01(time);
            int index;

            for (index = 0; index < textureHeights.Length; index++)
                if (time < textureHeights[index])
                    break;


            int index1 = Mathf.Clamp(index - 1, 0, textureHeights.Length - 1);
            int index2 = Mathf.Clamp(index, 0, textureHeights.Length - 1);

            float[] result = new float[6];
            if (textureIds[index1] == textureIds[index2])
            {
                result[textureIds[index1]] = 1f;
                return result;
            }

            time = (time - textureHeights[index1]) / (textureHeights[index2] - textureHeights[index1]);

            result[textureIds[index1]] = 1f - time;
            result[textureIds[index2]] = time;
            return result;
        }
        /// <summary>
        /// Converts float array used for biomes to color array used for scaled space texture
        /// </summary>
        public static Color32 FloatArrayToColor(float[] floats, Color32[] colors)
        {

            Color32 result = new Color32(0, 0, 0, 0);
            for (int i = 0; i < colors.Length; i++)
            {
                result += floats[i] * (Color)colors[i];
            }
            return result;
        }

        /// <summary>
        /// Tests if bounds are in viewPlanes
        /// </summary>
        public static bool TestPlanesAABB(Plane[] planes, Vector3 boundsMin, Vector3 boundsMax, bool testIntersection = true, float extraRange = 0f)
        {
            if (planes == null)
                return false;

            Vector3 vmin, vmax;
            int testResult = 2;

            for (int planeIndex = 0; planeIndex < planes.Length; planeIndex++)
            {
                var normal = planes[planeIndex].normal;
                var planeDistance = planes[planeIndex].distance;

                // X axis
                if (normal.x < 0)
                {
                    vmin.x = boundsMin.x;
                    vmax.x = boundsMax.x;
                }
                else
                {
                    vmin.x = boundsMax.x;
                    vmax.x = boundsMin.x;
                }

                // Y axis
                if (normal.y < 0)
                {
                    vmin.y = boundsMin.y;
                    vmax.y = boundsMax.y;
                }
                else
                {
                    vmin.y = boundsMax.y;
                    vmax.y = boundsMin.y;
                }

                // Z axis
                if (normal.z < 0)
                {
                    vmin.z = boundsMin.z;
                    vmax.z = boundsMax.z;
                }
                else
                {
                    vmin.z = boundsMax.z;
                    vmax.z = boundsMin.z;
                }

                var dot1 = normal.x * vmin.x + normal.y * vmin.y + normal.z * vmin.z;
                if (dot1 + planeDistance < 0 - extraRange)
                    return false;

                if (testIntersection)
                {
                    var dot2 = normal.x * vmax.x + normal.y * vmax.y + normal.z * vmax.z;
                    if (dot2 + planeDistance <= 0 + extraRange)
                        testResult = 1;
                }
            }
            return testResult > 0;
        }

        /// <summary>
        /// Generates grayscale preview of noise module
        /// </summary>
        public static Texture2D GeneratePreview(Module module, int resX = 256, int resY = 256)
        {
            Texture2D preview = new Texture2D(resX, resY);

            for (int x = 0; x < resX; x++)
            {
                for (int y = 0; y < resY; y++)
                {
                    float v = (module.GetNoise(x / (float)resX, y / (float)resY, 0f) + 1f) / 2f;
                    preview.SetPixel(x, y, new Color(v, v, v));
                }
            }
            preview.Apply();
            return preview;
        }

        /// <summary>
        /// Generates grayscale preview of noise module
        /// </summary>
        public static Heightmap GeneratePreviewHeightmap(Module module, int resX = 256, int resY = 256)
        {
            Heightmap preview = new Heightmap(resX, resY, false, false);

            float scaleX = 1f / (2 * resX);
            float scaleY = 1f / (2 * resY);

            for (int x = 0; x < resX; x++)
            {
                for (int y = 0; y < resY; y++)
                {
                    float v = (module.GetNoise(x * scaleX - 0.25f, y * scaleY - 0.25f, 0.75f) + 1f) / 2f;
                    v = Mathf.Clamp01(v);
                    preview.SetPixel(x, y, v);
                }
            }

            return preview;
        }

        /// <summary>
        /// Deserializes noise module to module tree
        /// </summary>
        public static Module DeserializeModule(Stream fs)
        {
            BinaryFormatter bf = new BinaryFormatter();
            return (Module)bf.Deserialize(fs);
        }

        public static Module DeserializeTextAsset(TextAsset ta)
        {
            MemoryStream stream = new MemoryStream(ta.bytes);
            
            BinaryFormatter bf = new BinaryFormatter();
            var module = (Module)bf.Deserialize(stream);
            stream.Dispose();

            InitializeModuleTree(module);
            return module;
        }

        public static Module DeserializeFile(string fileName)
        {
            FileStream fs = new FileStream(Application.persistentDataPath + "/" + fileName, FileMode.Open);
            BinaryFormatter bf = new BinaryFormatter();
            var module = (Module)bf.Deserialize(fs);
            fs.Dispose();

            InitializeModuleTree(module);
            return module;
        }


        /// <summary>
        /// Runs an Init() method on the main thread for all nodes that have it. Currently this is only the heightmap node.
        /// </summary>
        public static void InitializeModuleTree(Module m)
        {
            if (m is HeightmapModule)
            {
                (m as HeightmapModule).Init();

                if(!Application.isEditor && Application.isPlaying)
                    (m as HeightmapModule).textAssetBytes = null; 
            }

            if (m.inputs != null)
                for (int i = 0; i < m.inputs.Length; i++)
                {
                    InitializeModuleTree(m.inputs[i]);
                }
        }

        /// <summary>
        /// Randomly sets the seeds of a noise module
        /// </summary>
        public static void RandomizeNoise(ref Module m)
        {
            if (m.opType == ModuleType.Noise)
            {
                ((FastNoise)m).SetSeed(Random.Range(int.MinValue, int.MaxValue));

                //Randomize Frequency:
                //var frequency = ((FastNoise)m).m_frequency;
                //frequency += Random.Range(frequency/-100f, frequency/100f);
                //((FastNoise)m).SetFrequency(frequency); 
            }

            if (m.inputs != null)
                for (int i = 0; i < m.inputs.Length; i++)
                    RandomizeNoise(ref m.inputs[i]);
        }


        /// <summary>
        /// Saves a Vector3 array as a binary file.
        /// </summary>
        public static void SaveAsBinary(Vector3[] array)
        {

            float[] floats = new float[array.Length * 3];

            for (int i = 0; i < array.Length; i++)
            {
                int index = i * 3;

                floats[index] = array[i].x;
                floats[index + 1] = array[i].y;
                floats[index + 2] = array[i].z;
            }

            var bf = new BinaryFormatter();
            bf.Serialize(new FileStream(Application.dataPath + "/" + "filename" + ".txt", FileMode.Create), floats);
            Debug.Break();
        }

        /// <summary>
        /// Saves a Vector3 array as a text file.
        /// </summary>
        public static void SaveAsText(Vector3[] array)
        {
            StringBuilder sb = new StringBuilder("{");
            sb.AppendLine();

            for (int i = 0; i < array.Length; i++)
            {
                sb.Append("new Vector3(");
                sb.Append(array[i].x);
                sb.Append("f, ");
                sb.Append(array[i].y);
                sb.Append("f, ");
                sb.Append(array[i].z);
                sb.Append("f), ");

                if (i % 33 == 0 && i != 0)
                {
                    sb.AppendLine();
                }

            }

            File.WriteAllText(Application.dataPath + "/" + "filename" + ".txt", sb.ToString());
            Debug.Break();
        }

        public static string ArrayToString(float[] array)
        {
            StringBuilder sb = new StringBuilder("{");

            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i].ToString());

                if (i != array.Length - 1)
                    sb.Append(", ");
            }
            sb.Append("}");

            return sb.ToString();

        }

        /// <summary>
        /// Saves an int array as a text file.
        /// </summary>
        public static void SaveAsText(int[] array)
        {
            StringBuilder sb = new StringBuilder("{");
            sb.AppendLine();

            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i]);
                sb.Append(", ");

                if (i % 66 == 0 && i != 0)
                {
                    sb.AppendLine();
                }

            }

            File.WriteAllText(Application.dataPath + "/" + "filename" + ".txt", sb.ToString());
            Debug.Break();
        }

    }

    public struct int2
    {
        public int x;
        public int y;

        public int2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int this[int index]
        {
            get
            {
                if (index == 0)
                    return x;

                if (index > 1)
                    throw new System.IndexOutOfRangeException("int2 index out of range");

                return y;
            }
            set
            {

                if (index == 0)
                {
                    x = value;
                    return;
                }
                if (index > 1)
                    throw new System.IndexOutOfRangeException("int2 index out of range");

                y = value;

            }
        }


    }

}

