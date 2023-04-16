using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace PlanetaryTerrain.EditorUtils
{
    public class TextureAverageColor : EditorWindow
    {

        Color32 averageColor;
        Texture2D texture;

        UInt32 r;
        UInt32 b;
        UInt32 g;


        [MenuItem("Planetary Terrain/Utils/Texture Average Color")]
        static void Init()
        {
            #pragma warning disable 0219
            TextureAverageColor window = (TextureAverageColor)EditorWindow.GetWindow(typeof(TextureAverageColor));
            #pragma warning restore 0219
        }

        void OnGUI()
        {
            GUILayout.Label("Average Texture Color", EditorStyles.boldLabel);
            texture = (Texture2D)EditorGUILayout.ObjectField("Texture", texture, typeof(Texture2D), false);
            averageColor = EditorGUILayout.ColorField("Average Color", averageColor);

            if (GUILayout.Button("Calculate"))
            {
                averageColor = Color.black;
                r = g = b = 0;
                for (int x = 0; x < texture.width; x++)
                {
                    for (int y = 0; y < texture.height; y++)
                    {
                        Color32 c = texture.GetPixel(x, y);
                        r += c.r;
                        g += c.g;
                        b += c.b;
                    }
                }
                int pixels = texture.width * texture.height;
                averageColor = new Color32((byte)(r / pixels), (byte)(g / pixels), (byte)(b / pixels), 255);
            }
        }
    }
}
