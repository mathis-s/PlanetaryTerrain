using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace PlanetaryTerrain.EditorUtils
{
    public class OldToNewHeightmap : EditorWindow
    {

        TextAsset oldHeightmap;
        string filenameNewHeightmap;



        int width = 8192, height = 4096;
        bool is16Bit = false;

        [MenuItem("Planetary Terrain/Utils/Old To New Heightmap")]
        static void Init()
        {
#pragma warning disable 0219
            OldToNewHeightmap window = (OldToNewHeightmap)EditorWindow.GetWindow(typeof(OldToNewHeightmap));
#pragma warning restore 0219
        }

        void OnGUI()
        {
            GUILayout.Label("Old To New Heightmap", EditorStyles.boldLabel);
            filenameNewHeightmap = EditorGUILayout.TextField("Filename for new Heightmap", filenameNewHeightmap);
            oldHeightmap = (TextAsset)EditorGUILayout.ObjectField("Heightmap", oldHeightmap, typeof(TextAsset), true);

            EditorGUILayout.BeginHorizontal();
            width = EditorGUILayout.IntField("Width", width);
            height = EditorGUILayout.IntField("Height", height);
            EditorGUILayout.EndHorizontal();
            is16Bit = EditorGUILayout.Toggle("16bit", is16Bit);


            if (GUILayout.Button("Convert"))
            {
                byte[] oldHeightmapBytes = oldHeightmap.bytes;
                int oldHeightmapBytesLen = oldHeightmapBytes.Length;
                TestHeightmapResolutionOld(oldHeightmapBytesLen, width, height, is16Bit);

                byte[] newHeightmapHeader = new byte[9];

                BitConverter.GetBytes(width).CopyTo(newHeightmapHeader, 0);
                BitConverter.GetBytes(height).CopyTo(newHeightmapHeader, 4);
                BitConverter.GetBytes(is16Bit).CopyTo(newHeightmapHeader, 8);

                byte[] newHeightmap = new byte[oldHeightmapBytesLen + 9];

                newHeightmapHeader.CopyTo(newHeightmap, 0);

                if (is16Bit)
                {
                    int oldHeightmapBytesLenH = oldHeightmapBytesLen / 2;

                    for (int i = 0; i < oldHeightmapBytesLenH; i++)
                    {
                        newHeightmap[2 * i + 9] = oldHeightmapBytes[i];
                        newHeightmap[2 * i + 10] = oldHeightmapBytes[i + oldHeightmapBytesLenH];
                    }
                }
                else
                {
                    for (int i = 0; i < oldHeightmapBytesLen; i++)
                    {
                        newHeightmap[i + 9] = oldHeightmapBytes[i];
                    }
                }

                System.IO.File.WriteAllBytes(Application.dataPath + "/" + filenameNewHeightmap + ".bytes", newHeightmap);
                AssetDatabase.Refresh();
            }
        }

        static void TestHeightmapResolutionOld(int length, int width, int height, bool is16bit)
        {
            if ((is16bit ? length / 2 : length) == (height * width))
                return;

            if ((is16bit ? (length - 9) / 2 : length - 9) == height * width)
                throw new System.ArgumentException("heightmap", "Heightmap was already converted to new format! No need to convert it again.");

            throw new System.ArgumentOutOfRangeException("width, height", "Heightmap resolution incorrect! Cannot read heightmap!");
        }
    }
}
