using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;


namespace PlanetaryTerrain.EditorUtils
{
    public class HeightmapImporter : EditorWindow
    {

        string path = "";
        string filename = "heightmapPhotoshop";
        int width = 8192, height = 4096;
        bool is16bit;
        bool reverseByteOrder = false;
        bool cutTiffHeader = false;


        Texture2D preview;

        [MenuItem("Planetary Terrain/Utils/Heightmap Importer")]
        static void Init()
        {
#pragma warning disable 414
#pragma warning disable 0219
            HeightmapImporter window = (HeightmapImporter)EditorWindow.GetWindow(typeof(HeightmapImporter));
#pragma warning restore 0219
#pragma warning restore 414
        }


        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField("Path", path);
            if (GUILayout.Button("Select"))
                path = EditorUtility.OpenFilePanel("RAW File", "", "raw");
            EditorGUILayout.EndHorizontal();
            filename = EditorGUILayout.TextField("Filename", filename);
            EditorGUILayout.BeginHorizontal();
            width = EditorGUILayout.IntField("Width", width);
            height = EditorGUILayout.IntField("Height", height);
            EditorGUILayout.EndHorizontal();
            is16bit = EditorGUILayout.Toggle("16-bit", is16bit);

            if (is16bit)
                reverseByteOrder = EditorGUILayout.Toggle("Reverse byte order", reverseByteOrder);

            var temp = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 300f;
            cutTiffHeader = EditorGUILayout.Toggle("Cut header (allows importing uncompressed tiffs)", cutTiffHeader);
            EditorGUIUtility.labelWidth = temp;

            if (GUILayout.Button("Convert"))
                ConvertToHeightmap();

            EditorStyles.label.wordWrap = true;

            EditorGUILayout.LabelField("Converts grayscale Photoshop or GIMP .raw files to heightmaps. Photoshop: Export with Macintosh byte order and 0 header. GIMP: Export with Standard (R, G, B). You can also import uncompressed tiffs if you enable Cut Header.");
            GUILayout.Label(preview);
        }

        void ConvertToHeightmap()
        {
            var fs = File.OpenRead(path);

            if (cutTiffHeader)
            {
                fs.Position = 8;
                
            }
            Heightmap heightmap;

            if (is16bit)
            {
                int halfLength = width * height;
                int hh = height - 1;

                heightmap = new Heightmap(width, height, true, false);

                int i = 0;
                int _index = 0;

                if (fs.Length != heightmap.ushorts.Length * 2 && !cutTiffHeader)
                {
                    Debug.LogError("Failed to convert to heightmap. Incorrect resolution or incompatible file format.");
                    return;
                }

                try
                {
                    while (i < halfLength)
                    {
                        int index = (hh - (int)(i / (double)width)) * width + (i % width);

                        _index = index;
                        var us = new byte[2];
                        fs.Read(us, 0, 2);

                        if (reverseByteOrder)
                            heightmap.ushorts[index] = (ushort)((us[1] << 8) + us[0]);
                        else
                            heightmap.ushorts[index] = (ushort)((us[0] << 8) + us[1]);

                        i++;
                    }
                }
                catch (System.IndexOutOfRangeException e)
                {
                    Debug.LogError("Failed to convert to heightmap. Incorrect resolution or incompatible file format.");
                    Debug.LogError(e);
                    Debug.Log("index: " + _index.ToString());
                    Debug.Log("i " + i.ToString());
                    return;
                }
            }
            else
            {
                int length = width * height;
                int hh = height - 1;

                heightmap = new Heightmap(width, height, false, false);

                if (fs.Length != heightmap.bytes.Length && !cutTiffHeader)
                {
                    Debug.LogError("Failed to convert to heightmap. Incorrect resolution or incompatible file format.");
                    return;
                }

                int i = 0;
                int index = 0;
                try
                {
                    while (i < length)
                    {
                        index = (hh - (int)(i / (double)width)) * width + (i % width);
                        heightmap.bytes[index] = (byte)fs.ReadByte();
                        i++;
                    }
                }
                catch (System.IndexOutOfRangeException e)
                {
                    Debug.LogError("Failed to convert to heightmap. Incorrect resolution or incompatible file format.");
                    Debug.LogError(e);
                    Debug.Log("i: " + i + ", index: " + index);

                    return;
                }
            }


            Debug.Log("Successfully converted!");
            File.WriteAllBytes(Application.dataPath + "/" + filename + ".bytes", heightmap.GetFileBytes());
            AssetDatabase.Refresh();
            preview = heightmap.GetTexture2D();
        }
    }
}
