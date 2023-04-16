using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace PlanetaryTerrain
{
    public class WaterFlow : MonoBehaviour
    {
        public Material water;
        public float flowSpeed;
        Vector2 flowvector = Vector2.zero;

        void Update()
        {
            flowvector.x += (flowSpeed / 100f);
            flowvector.y -= (flowSpeed / 100f);

            water.SetTextureOffset("_Normals", -flowvector);
            water.SetTextureOffset("_ReflectTex", flowvector);
            water.SetTextureOffset("_WaveMap", flowvector);
        }
    }
}
