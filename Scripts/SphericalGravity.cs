using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;

[RequireComponent(typeof(Planet))]
public class SphericalGravity : MonoBehaviour {
    public Transform[] objects;
    public float acceleration = -9.81f;
    float radius;

	void Start () {
        radius = GetComponent<Planet>().radius;
    }
	
	void FixedUpdate () {
        for (int i = 0; i < objects.Length; i++)
        {

            objects[i].GetComponent<Rigidbody>().AddForce((transform.position - objects[i].position).normalized * -acceleration * objects[i].GetComponent<Rigidbody>().mass * radius * radius / (transform.position - objects[i].position).sqrMagnitude);
        }
	}
}
