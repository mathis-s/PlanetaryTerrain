using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain.DoubleMath;


namespace PlanetaryTerrain
{
    [RequireComponent(typeof(Planet))]
    public class FloatingOrigin : MonoBehaviour
    {
        public float threshold = 6000f;
        public Transform player;
        public Transform[] objects;


        [HideInInspector]
        public Vector3d distanceFromOriginalOrigin = Vector3d.zero;
        public List<Planet> planets = new List<Planet>();

        void Start()
        {
            if (!planets.Contains(GetComponent<Planet>()))
                planets.Add(GetComponent<Planet>());
        }
        void Update()
        {
            //Shifts origin if cameras distance from it is larger than the threshold.
            if ((player.position.magnitude > threshold || Input.GetKey(KeyCode.F)) && (planets[0].initialized || planets[0].inScaledSpace))
            {
                //print("moving origin");
                distanceFromOriginalOrigin += player.position.ToVector3d();
                transform.position -= player.position;

                for (int i = 0; i < objects.Length; i++)
                    objects[i].position -= player.position;

                int len = planets.Count;
                for (int i = 0; i < len; i++)
                {
                    int lenj = planets[i].quads.Count;

                    for (int j = 0; j < lenj; j++)
                        if (planets[i].quads[j].renderedQuad)
                            planets[i].quads[j].renderedQuad.transform.position -= player.position;
                }

                player.position = Vector3.zero;

            }

            /*if (Input.GetKeyDown(KeyCode.H))
            {
                int len = planets.Count;
                for (int i = 0; i < len; i++)
                {
                    planets[i].UpdatePosition();
                }
            }*/

        }
        public Vector3 WorldSpaceToScaledSpace(Vector3 worldPos, float scaleFactor)
        {
            return (Vector3)((worldPos.ToVector3d() + distanceFromOriginalOrigin) / (double)scaleFactor);
        }

        public Vector3d WorldSpaceToScaledSpace(Vector3d worldPos, float scaleFactor)
        {
            return ((worldPos + distanceFromOriginalOrigin) / (double)scaleFactor);
        }
    }
}
