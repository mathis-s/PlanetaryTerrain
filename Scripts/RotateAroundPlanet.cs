using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PlanetaryTerrain
{	

    public class RotateAroundPlanet : MonoBehaviour
    {

		public Vector3 rotationSpeed = new Vector3(0f, 0.000005f, 0f);
		public Transform parentPlanet;
		Quaternion rotSpeed;

        void FixedUpdate()
        {
            rotSpeed = Quaternion.Euler(rotationSpeed);
			transform.position = MathFunctions.RotateAroundPoint(transform.position, parentPlanet.position, rotSpeed);
        }
    }
}
