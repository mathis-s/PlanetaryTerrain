using UnityEngine;
using System.Collections;
using PlanetaryTerrain;

namespace PlanetaryTerrain.Atmosphere
{
    [RequireComponent(typeof(Planet))]
    public class Atmosphere : MonoBehaviour
    {
        public GameObject m_sun;

        Material m_skyFromSpace;
        Material m_skyFromAtmosphere;

        public float m_hdrExposure = 0.8f;
        public Vector3 m_waveLength = new Vector3(0.65f, 0.57f, 0.475f); // Wave length of sun light
        public float m_ESun = 8.0f;            // Sun brightness constant
        public float m_kr = 0.0025f;            // Rayleigh scattering constant
        public float m_km = 0.0010f;            // Mie scattering constant
        public float m_g = -0.990f;             // The Mie phase asymmetry factor, must be between 0.999 to -0.999

        public int atmosphereLayer = 8;

        //Don't change these
        float m_outerScaleFactor = 1.025f; // Difference between inner and ounter radius. Must be 2.5%
        float m_innerRadius;            // Radius of the ground sphere
        float m_outerRadius;            // Radius of the sky sphere
        float m_scaleDepth = 0.25f;     // The scale depth (i.e. the altitude at which the atmosphere's average density is found)
        GameObject atmosphereSpace;
        GameObject atmosphereGround;
        Planet planet;
        Vector3 position;
        private bool scaledSpaceCopy;

        void Start()
        {
            planet = GetComponent<Planet>();
            scaledSpaceCopy = planet.scaledSpaceCopy != null;

            float radius;

            if (scaledSpaceCopy)
            {
                position = planet.scaledSpaceCopy.transform.position;
                radius = planet.radius / planet.scaledSpaceFactor;
            }
            else
                radius = planet.radius;

            m_innerRadius = radius;
            //The outer sphere must be 2.5% larger that the inner sphere
            m_outerRadius = m_outerScaleFactor * radius;

            atmosphereSpace = (GameObject)Instantiate(Resources.Load("Atmosphere"), scaledSpaceCopy ? position : transform.position, transform.rotation, transform);
            atmosphereSpace.transform.localScale = Vector3.one * m_outerRadius;

            atmosphereGround = (GameObject)Instantiate(Resources.Load("AtmosphereGround"), scaledSpaceCopy ? position : transform.position, transform.rotation, transform);
            atmosphereGround.transform.localScale = Vector3.one * m_outerRadius;
            atmosphereGround.SetActive(false);

            m_skyFromSpace = atmosphereSpace.GetComponent<Renderer>().sharedMaterial;
            m_skyFromAtmosphere = atmosphereGround.GetComponent<Renderer>().sharedMaterial;

            if (scaledSpaceCopy)
            {
                atmosphereGround.layer = atmosphereLayer;
                atmosphereSpace.layer = atmosphereLayer;
            }
            InitMaterial(m_skyFromSpace);
            InitMaterial(m_skyFromAtmosphere);
        }
        void Update()
        {
            if (Vector3.Distance(transform.position, Camera.main.transform.position) < (scaledSpaceCopy ? m_outerRadius * planet.scaledSpaceFactor : m_outerRadius))
            {
                atmosphereGround.SetActive(true);
                atmosphereSpace.SetActive(false);
            }
            else
            {
                atmosphereGround.SetActive(false);
                atmosphereSpace.SetActive(true);
            }

            if (scaledSpaceCopy)
            {
                atmosphereGround.transform.parent = planet.scaledSpaceCopy.transform;
                atmosphereSpace.transform.parent = planet.scaledSpaceCopy.transform;

                atmosphereGround.transform.localPosition = Vector3.zero;
                atmosphereSpace.transform.localPosition = Vector3.zero;
            }

            InitMaterial(m_skyFromSpace);
            InitMaterial(m_skyFromAtmosphere);
        }

        void InitMaterial(Material mat)
        {
            Vector3 invWaveLength4 = new Vector3(1.0f / Mathf.Pow(m_waveLength.x, 4.0f), 1.0f / Mathf.Pow(m_waveLength.y, 4.0f), 1.0f / Mathf.Pow(m_waveLength.z, 4.0f));
            float scale = 1.0f / (m_outerRadius - m_innerRadius);

            mat.SetVector("v3LightPos", m_sun.transform.forward * -1.0f);
            mat.SetVector("v3InvWavelength", invWaveLength4);
            mat.SetFloat("fOuterRadius", m_outerRadius);
            mat.SetFloat("fOuterRadius2", m_outerRadius * m_outerRadius);
            mat.SetFloat("fInnerRadius", m_innerRadius);
            mat.SetFloat("fInnerRadius2", m_innerRadius * m_innerRadius);
            mat.SetFloat("fKrESun", m_kr * m_ESun);
            mat.SetFloat("fKmESun", m_km * m_ESun);
            mat.SetFloat("fKr4PI", m_kr * 4.0f * Mathf.PI);
            mat.SetFloat("fKm4PI", m_km * 4.0f * Mathf.PI);
            mat.SetFloat("fScale", scale);
            mat.SetFloat("fScaleDepth", m_scaleDepth);
            mat.SetFloat("fScaleOverScaleDepth", scale / m_scaleDepth);
            mat.SetFloat("fHdrExposure", m_hdrExposure);
            mat.SetFloat("g", m_g);
            mat.SetFloat("g2", m_g * m_g);
            mat.SetVector("v3LightPos", m_sun.transform.forward * -1.0f);
            mat.SetVector("v3Translate", scaledSpaceCopy ? planet.scaledSpaceCopy.transform.position : transform.localPosition);
        }
    }
}





