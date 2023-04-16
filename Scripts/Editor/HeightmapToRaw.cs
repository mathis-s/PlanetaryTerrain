using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
namespace PlanetaryTerrain.EditorUtils
{
    public class HeightmapToRaw : EditorWindow
    {

        Texture2D heightmap;
        enum Channel { R = 0, G = 1, B = 2, A = 3, Gray = -1 };
        Channel channel = Channel.R;
        string filename = "heightmap";

        [MenuItem("Planetary Terrain/Utils/Texture Heightmap to RAW")]
        static void Init()
        {
        #pragma warning disable 0219
            HeightmapToRaw window = (HeightmapToRaw)EditorWindow.GetWindow(typeof(HeightmapToRaw));
        #pragma warning restore 0219
        }

        void OnGUI()
        {
            heightmap = (Texture2D)EditorGUILayout.ObjectField("Heightmap", heightmap, typeof(Texture2D), false);
            filename = EditorGUILayout.TextField("Filename", filename);
            channel = (Channel) EditorGUILayout.EnumPopup("Source Channel", channel);
            if (GUILayout.Button("Convert"))
            {
                Heightmap heightmapRaw = new Heightmap(heightmap, false, (int) channel);

                Debug.Log("Width: " + heightmap.width + ", Height: " + heightmap.height);

                File.WriteAllBytes(Application.dataPath + "/" + filename + ".bytes", heightmapRaw.GetFileBytes());
                AssetDatabase.Refresh();
            }
        }
    }
}

