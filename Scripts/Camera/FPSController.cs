using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;

namespace PlanetaryTerrain.Extra
{
    public class FPSController : MonoBehaviour
    {
        public Planet planet;
        public float speed = 5f;
        public float shiftMult = 2f;
        public float jumpForce = 2f;
        public float sensitivity = 0.25f;
        public Transform m_camera;

        Vector3 targetVelocity;
        Vector3 curVelocity;
        Vector3 deltaV;
        Rigidbody m_rigidbody;
        

        bool standing;
        void Start()
        {
            m_rigidbody = GetComponent<Rigidbody>();
        }
        void FixedUpdate()
        {


            targetVelocity.x = Input.GetAxis("Horizontal") * speed;
            targetVelocity.z = Input.GetAxis("Vertical") * speed;
            targetVelocity.y = transform.InverseTransformDirection(m_rigidbody.velocity).y;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                targetVelocity.x *= shiftMult;
                targetVelocity.z *= shiftMult;
            }

            curVelocity = transform.InverseTransformDirection(m_rigidbody.velocity);
            deltaV = transform.TransformDirection(targetVelocity - curVelocity);

            m_rigidbody.velocity += deltaV;

            float rotation = Input.GetAxis("Mouse X") * sensitivity;

            m_camera.Rotate(-Input.GetAxis("Mouse Y") * sensitivity, 0f, 0f);

            transform.rotation = Quaternion.FromToRotation(transform.up, -planet.Vector3Down(transform.position)) * transform.rotation;
            
            transform.Rotate(Vector3.up, rotation, Space.Self);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) && standing)
                m_rigidbody.AddForce((planet.transform.position - transform.position).normalized * -jumpForce, ForceMode.Impulse);
        }

        void OnCollisionEnter(Collision c)
        {
            standing = true;
        }

        void OnCollisionExit(Collision c)
        {
            standing = false;
        }
    }
}
