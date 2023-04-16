using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlanetaryTerrain;

namespace PlanetaryTerrain.EditorUtils
{
    public class DetailDistancesCalc : EditorWindow
    {

        float startValue = 20000;
        const string planetName = "Planet";
        Planet planet;

        [MenuItem("Planetary Terrain/Utils/Detail Distances Calculator")]
        static void Init()
        {
        #pragma warning disable 414
        #pragma warning disable 0219
            DetailDistancesCalc window = (DetailDistancesCalc)EditorWindow.GetWindow(typeof(DetailDistancesCalc));
        #pragma warning restore 0219
        #pragma warning restore 414
        }
        void OnGUI()
        {
            try
            {
                if (!planet)
                    planet = GameObject.Find(planetName).GetComponent<Planet>();
            }
            catch { }
            GUILayout.Label("Detail Distances Calculator", EditorStyles.boldLabel);
            planet = (Planet)EditorGUILayout.ObjectField("Planet", planet, typeof(Planet), true);
            startValue = EditorGUILayout.FloatField("start value", startValue);

            if (GUILayout.Button("Calculate"))
            {
                float[] detailDistances = planet.detailDistances;

                for (int i = 0; i < detailDistances.Length; i++)
                {
                    if (i == 0)
                        detailDistances[i] = startValue;
                    else
                        detailDistances[i] = (detailDistances[i - 1] / 2f);

                }
            }
        }
    }
}
