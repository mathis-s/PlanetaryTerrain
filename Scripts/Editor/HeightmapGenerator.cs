using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using PlanetaryTerrain.Noise;
using System.Text;

namespace PlanetaryTerrain.EditorUtils
{
    public class HeightmapGenerator : EditorWindow
    {
        public enum DataSource { Noise, ComputeShader, RawHeightmap }
        public enum ColorDataSource { Planet, Gradient }
        string filename = "heightmapGenerated";

        int resolutionX = 8192;
        int resolutionY = 4096;

        public float[] textureHeights = { 0f, 0.01f, 0.4f, 0.8f, 1f };
        public Color32[] colors = { new Color32(166, 130, 90, 255), new Color32(72, 80, 28, 255), new Color32(60, 53, 37, 255), new Color32(81, 81, 81, 255), new Color32(255, 255, 255, 255) };
        public int[] textureIds = new int[] { 0, 1, 2, 3, 4, 5 };
        public Planet planet;

        bool ocean;
        float oceanLevel = 0f;
        Color32 oceanColor = new Color32(48, 57, 56, 255);
        Texture2D heightmapTex;
        Texture2D texture;
        Texture2D gradient;
        DataSource dataSource = DataSource.Noise;
        ColorDataSource colorDataSource = ColorDataSource.Planet;


        int width, height;
        bool heightmap16bit;

        Heightmap heightmap;

        float progress;
        IAsyncResult cookie;
        bool preview;
        Module module;
        byte[] lastBytes;

        TextAsset textAsset;
        ComputeShader computeShader;

        bool generateHeightmapTex = true, generateTexture = true, generateHeightmap = true;
        bool debugLayout;



        [MenuItem("Planetary Terrain/Heightmap Generator")]

        static void Init()
        {
#pragma warning disable 0219
            HeightmapGenerator window = (HeightmapGenerator)EditorWindow.GetWindow(typeof(HeightmapGenerator));
#pragma warning restore 0219
        }

        void OnGUI()
        {
            DrawGUI();

            if (GUILayout.Button("Generate Gradient"))
            {
                GenerateGradient();
            }

            GUILayout.Label(gradient);
            GUILayout.Space(15);

            EditorGUI.BeginDisabledGroup(dataSource == DataSource.RawHeightmap);
            if (GUILayout.Button("Generate Preview") && cookie == null)
            {
                progress = 0f;
                preview = true;
                width = 512;
                height = 256;

                if (dataSource == DataSource.Noise)
                {
                    LoadSerializedNoise();
                    Action method = GenerateBytes;
                    cookie = method.BeginInvoke(null, null);
                }
                else if (dataSource == DataSource.ComputeShader)
                {
                    GenerateBytesGPU();
                    GenerateTextures();
                }
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Generate") && cookie == null)
            {
                progress = 0f;
                preview = false;
                width = resolutionX;
                height = resolutionY;

                if (dataSource == DataSource.Noise)
                {
                    LoadSerializedNoise();

                    Action method = GenerateBytes;
                    cookie = method.BeginInvoke(null, null);
                }
                else if (dataSource == DataSource.ComputeShader)
                {
                    GenerateBytesGPU();
                    GenerateTextures();
                    SaveAssets();
                }
                else if (dataSource == DataSource.RawHeightmap)
                {
                    ReadHeightmap();
                    GenerateTextures();
                    SaveAssets();

                }
            }

            if (cookie != null)
            {
                EditorUtility.DisplayProgressBar("Progress", "Generating heightmap from noise...", progress);
                if (cookie.IsCompleted)
                {
                    EditorUtility.ClearProgressBar();
                    GenerateTextures();
                    SaveAssets();
                    cookie = null;

                }
            }
            GUILayout.Label(heightmapTex);
            GUILayout.Label(texture);

            GUILayout.FlexibleSpace();
            debugLayout = GUILayout.Toggle(debugLayout, new GUIContent("Safe Layout", "Toggle this if arrays are invisible. Circumvents a bug with Horizontal Layouts in recent versions of Unity by disabling them."));
        }

        void OnInspectorUpdate()
        {
            if (cookie != null)
                Repaint();
        }

        void SaveAssets()
        {
            if (!preview)
            {
                if (generateTexture)
                    File.WriteAllBytes(Application.dataPath + "/" + filename + "_texture.png", texture.EncodeToPNG());
                if (generateHeightmapTex)
                    File.WriteAllBytes(Application.dataPath + "/" + filename + "_heightmap.png", heightmapTex.EncodeToPNG());
                if (generateHeightmap)
                    File.WriteAllBytes(Application.dataPath + "/" + filename + ".bytes", heightmap.GetFileBytes());
            }
            AssetDatabase.Refresh();
        }
        void ReadHeightmap()
        {
            heightmap = new Heightmap(textAsset, false);

            width = heightmap.width;
            height = heightmap.height;
            heightmap16bit = heightmap.is16bit;
        }

        void LoadSerializedNoise()
        {
            if (textAsset != null && textAsset.bytes.Length != 0 && textAsset.bytes != lastBytes)
            {
                try
                {
                    lastBytes = textAsset.bytes;
                    MemoryStream stream = new MemoryStream(textAsset.bytes);
                    module = Utils.DeserializeModule(stream);

                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    module = null;
                }
            }
        }

        void GenerateBytes()
        {
            System.DateTime startTime = System.DateTime.UtcNow;

            int length = width * height;

            heightmap = new Heightmap(width, height, heightmap16bit, false);

            double xMul = 360.0 / width;
            double yMul = 180.0 / height;

            double lat, lon;
            Vector3 xyz;
            float value;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    lat = y * yMul;
                    lon = x * xMul;

                    xyz = (Vector3)MathFunctions.LatLonToXyz(lat, lon, 1.0);

                    value = Mathf.Clamp01((module.GetNoise(xyz.x, xyz.y, xyz.z) + 1f) / 2f); //Noise ranges from -1 to 1; texture needs value from 0 to 1

                    if (!heightmap16bit)
                        heightmap.bytes[y * width + x] = (byte)(value * 255f);
                    else
                    {
                        heightmap.ushorts[y * width + x] = (ushort)(value * ushort.MaxValue);
                    }
                }
                progress = (float)x / width;
            }
            Debug.Log("Generation Time: " + (System.DateTime.UtcNow - startTime).TotalMilliseconds);
        }

        void GenerateBytesGPU()
        {
            System.DateTime startTime = System.DateTime.UtcNow;

            int length = width * height;

            heightmap = new Heightmap(width, height, heightmap16bit, false);

            double xMul = 360.0 / width;
            double yMul = 180.0 / height;

            double lat, lon;
            Vector3[] xyz = new Vector3[length];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    lat = y * yMul;
                    lon = x * xMul;

                    xyz[y * width + x] = (Vector3)MathFunctions.LatLonToXyz(lat, lon, 1.0);
                }
            }

            int kernelIndex = computeShader.FindKernel("ComputeHeightmap");

            //xyz is a Vector3[] containing all points you want to generate the height of; the more you can do in one batch, the better
            ComputeBuffer computeBuffer = new ComputeBuffer(length, 12);
            //copying xyz to the GPU
            computeBuffer.SetData(xyz);

            //Starting Calculation; length is xyz.length
            computeShader.SetBuffer(kernelIndex, "dataBuffer", computeBuffer);
            computeShader.Dispatch(kernelIndex, Mathf.CeilToInt(length / 256f), 1, 1);

            //Finishes calculation and copies data back to CPU (don't to this at runtime! Either wait a few frames after Dispatch() or use an AsyncGPUReadbackRequest)
            computeBuffer.GetData(xyz);
            computeBuffer.Dispose();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float value = Mathf.Clamp01(xyz[y * width + x].x);

                    if (!heightmap16bit)
                        heightmap.bytes[y * width + x] = (byte)(value * byte.MaxValue);
                    else
                    {
                        heightmap.ushorts[y * width + x] = (ushort)(value * ushort.MaxValue);
                    }
                }
            }
            Debug.Log("Generation Time: " + (System.DateTime.UtcNow - startTime).TotalMilliseconds);
        }

        void GenerateTextures()
        {
            if (planet != null && colorDataSource == ColorDataSource.Planet)
                planet.Initialize();

            if (generateHeightmapTex)
                heightmapTex = new Texture2D(width, height);
            if (generateTexture)
                texture = new Texture2D(width, height);


            float value = 0f;
            int length = width * height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    value = heightmap.GetPixel(x, y);

                    if (generateHeightmapTex)
                        heightmapTex.SetPixel(x, y, new Color(value, value, value));

                    if (generateTexture)
                    {
                        Color32 col = Color.clear;

                        if (colorDataSource == ColorDataSource.Gradient)
                        {
                            col = Utils.FloatArrayToColor(Utils.EvaluateTexture(value, textureHeights, textureIds), colors);
                        }
                        else
                        {
                            if (planet != null)
                                col = Utils.FloatArrayToColor(planet.textureProvider.EvaluateTexture(value, Vector3.zero), colors);
                        }


                        if (!ocean)
                            texture.SetPixel(x, y, col);
                        else
                            texture.SetPixel(x, y, value >= oceanLevel ? col : oceanColor);
                    }
                }
            }
            if (generateHeightmapTex)
                heightmapTex.Apply();
            if (generateTexture)
                texture.Apply();

            if (planet != null && colorDataSource == ColorDataSource.Planet)
                planet.Reset();
        }

        void GenerateGradient()
        {
            if (planet != null && colorDataSource == ColorDataSource.Planet)
                planet.Initialize();

            gradient = new Texture2D(512, 32);
            gradient.alphaIsTransparency = false;

            Color32[] pixels = new Color32[512 * 32];

            var colorsOpaque = new Color32[colors.Length];

            for (int i = 0; i < colorsOpaque.Length; i++)
            {
                colorsOpaque[i] = colors[i];
                colorsOpaque[i].a = 255;
            }


            for (int x = 0; x < 512; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    Color32 col = Color.clear;

                    if (colorDataSource == ColorDataSource.Gradient)
                    {
                        col = Utils.FloatArrayToColor(Utils.EvaluateTexture(x / 511f, textureHeights, textureIds), colorsOpaque);
                    }
                    else if (colorDataSource == ColorDataSource.Planet)
                    {
                        col = Utils.FloatArrayToColor(planet.textureProvider.EvaluateTexture(x / 511f, Vector3.zero), colorsOpaque);
                    }

                    pixels[y * 512 + x] = col;
                }
            }
            gradient.SetPixels32(pixels);
            gradient.Apply();

            if (planet != null && colorDataSource == ColorDataSource.Planet)
                planet.Reset();
        }

        void DrawGUI()
        {
            GUILayout.Label("Heightmap/Texture Generator", EditorStyles.boldLabel);
            filename = EditorGUILayout.TextField("Filename", filename);

            EditorGUI.BeginDisabledGroup(dataSource == DataSource.RawHeightmap);
            GUILayout.Space(15);
            GUILayout.Label("Textures size: ", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            resolutionX = EditorGUILayout.IntField("Width", resolutionX);
            resolutionY = EditorGUILayout.IntField("Height", resolutionY);
            EditorGUILayout.EndHorizontal();
            heightmap16bit = EditorGUILayout.Toggle("16bit Mode", heightmap16bit);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(15);
            GUILayout.Label("Data source: ", EditorStyles.boldLabel);
            dataSource = (DataSource)EditorGUILayout.EnumPopup(new GUIContent("Data source", "Source for heightmap/texture generation. You can also use heightmaps imported with Texture Heightmap to RAW. Don't forget setting the bit depth."), dataSource);


            switch (dataSource)
            {
                case DataSource.Noise:
                    textAsset = (TextAsset)EditorGUILayout.ObjectField("Noise Module", textAsset, typeof(TextAsset), true);
                    break;
                case DataSource.ComputeShader:
                    computeShader = (ComputeShader)EditorGUILayout.ObjectField("Compute Shader", computeShader, typeof(ComputeShader), false);
                    break;
                case DataSource.RawHeightmap:
                    textAsset = (TextAsset)EditorGUILayout.ObjectField("Heightmap", textAsset, typeof(TextAsset), true);
                    break;
            }
            GUILayout.Space(15);

            GUILayout.Label("Color gradient", EditorStyles.boldLabel);

            ocean = EditorGUILayout.Toggle("Generate Ocean", ocean);
            if (ocean)
            {
                oceanLevel = EditorGUILayout.Slider("Water Level", oceanLevel, 0f, 1f);
                oceanColor = EditorGUILayout.ColorField("Ocean Color", oceanColor);
            }

            colorDataSource = (ColorDataSource)EditorGUILayout.EnumPopup("Color Data Source", colorDataSource);

            SerializedObject serialObj = new SerializedObject(this);
            SerializedProperty colorsArray = serialObj.FindProperty("colors");

            if (colorDataSource == ColorDataSource.Gradient)
            {

                SerializedProperty textureHeightsArray = serialObj.FindProperty("textureHeights");
                SerializedProperty textureIdsArray = serialObj.FindProperty("textureIds");

                if (!debugLayout)
                    EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(textureHeightsArray, true);
                EditorGUILayout.PropertyField(textureIdsArray, true);
                if (debugLayout)
                    EditorGUILayout.PropertyField(colorsArray, true);

                if (!debugLayout)
                {
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.PropertyField(colorsArray, true);
                }
                if (GUILayout.Button("Set Alpha(Smoothness) to 0"))
                    for (int i = 0; i < colors.Length; i++)
                        colors[i].a = 0;
                if (GUILayout.Button("Set Alpha(Smoothness) to 255"))
                    for (int i = 0; i < colors.Length; i++)
                        colors[i].a = 255;
                if (!debugLayout)
                    EditorGUILayout.EndVertical();

                if (!debugLayout)
                    EditorGUILayout.EndHorizontal();

            }
            else if (colorDataSource == ColorDataSource.Planet)
            {
                if (!debugLayout)
                    EditorGUILayout.BeginHorizontal();

                planet = (Planet)EditorGUILayout.ObjectField("Planet", planet, typeof(Planet), true);

                if (!debugLayout)
                    EditorGUILayout.BeginVertical();

                EditorGUILayout.PropertyField(colorsArray, true);
                if (GUILayout.Button("Set Alpha(Smoothness) to 0"))
                    for (int i = 0; i < colors.Length; i++)
                        colors[i].a = 0;
                if (GUILayout.Button("Set Alpha(Smoothness) to 255"))
                    for (int i = 0; i < colors.Length; i++)
                        colors[i].a = 255;

                if (!debugLayout)
                {
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
            }

            serialObj.ApplyModifiedProperties();


            GUILayout.Space(15);

            GUILayout.Label("Files to generate: ", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            generateHeightmapTex = EditorGUILayout.Toggle("Heightmap Texture", generateHeightmapTex);
            generateTexture = EditorGUILayout.Toggle("Texture", generateTexture);
            generateHeightmap = EditorGUILayout.Toggle("Heightmap", generateHeightmap);
            EditorGUILayout.EndHorizontal();

        }
    }
}




