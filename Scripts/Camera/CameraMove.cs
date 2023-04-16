using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PlanetaryTerrain.Extra
{
    public class CameraMove : MonoBehaviour
    {

        public float speed = 10f;
        public float fastSpeed = 1000f;
        public float rotSpeed = 0.25f;
        private Vector3 lastMousePos;

        void FixedUpdate()
        {

            transform.Translate(0, 0, Input.GetAxis("Vertical") * (speed + (Input.GetAxis("Fire3") * fastSpeed)), Space.Self);
            Vector3 delta = lastMousePos - Input.mousePosition;

            if (Input.GetMouseButton(0))
            {
                transform.Rotate(new Vector3(-delta.y * rotSpeed, -delta.x * rotSpeed, 0));
            }
            transform.Rotate(0, 0, -Input.GetAxis("Horizontal"));

            lastMousePos = Input.mousePosition;
        }
    }
}
