using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;

namespace PlanetaryTerrain.Extra
{
    public class Coordinates : MonoBehaviour
    {

        public Vector2 latlon = new Vector2(90f, 0f);
        public Planet planet;
        public bool teleport;
        public float height = 1.0006f;

        void Start()
        {
            if (teleport)
            {
                planet.transform.position = -MathFunctions.LatLonToXyz(latlon, planet.radius) * height;
            }
        }
        void Update()
        {
            Vector3 relativePlanet = (transform.position - planet.transform.position);
            latlon = MathFunctions.XyzToLatLon(Quaternion.Inverse(planet.transform.rotation) * relativePlanet);
        }
    }
}
