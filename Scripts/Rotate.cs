using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain
{
    public class Rotate : MonoBehaviour
    {
        public Vector3d rotationSpeed = new Vector3d(0f, 0.0001, 0f);
        QuaternionD rSpeedQ;
        List<Planet> planets = new List<Planet>();

        void Start()
        {

            planets.Add(GetComponent<Planet>());

            foreach (Transform t in transform)
                if (t.GetComponent<Planet>() != null)
                    planets.Add(t.GetComponent<Planet>());
                
            
            rSpeedQ = QuaternionD.Euler(rotationSpeed);
        }

        //Using FixedUpdate() is better than Update() and Time.deltaTime because the math only has to be done 30 times per second.
        void FixedUpdate()
        {
            rSpeedQ = QuaternionD.Euler(rotationSpeed);
            foreach (Planet p in planets)
            {
                p.rotation *= rSpeedQ;
            }
        }
    }
}
