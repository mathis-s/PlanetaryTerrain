using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain.Foliage
{
    /// <summary>
    /// Generates foliage when added to a quad and then renders it.
    /// </summary>
    public class FoliageRenderer : MonoBehaviour
    {
        public List<DetailMesh> detailObjects = new List<DetailMesh>();
        internal List<GameObject> spawnedPrefabs = new List<GameObject>();
        public Matrix4x4[][] matrices;
        internal Planet planet;
        internal Quad quad;
        Vector3 oldPosition, position;
        Quaternion oldRotation, rotation;
        bool initialized;
        bool generating;


        public void Initialize()
        {
            position = transform.position;
            rotation = transform.rotation;

            matrices = new Matrix4x4[detailObjects.Count][];
            RecalculateMatrices();

            oldPosition = position;
            oldRotation = rotation;

            initialized = true;
        }

        void Update()
        {
            if (initialized)
            {
                position = transform.position;
                rotation = transform.rotation;

                if (position != oldPosition || (rotation.x != oldRotation.x || rotation.y != oldRotation.y || rotation.z != oldRotation.z || rotation.w != oldRotation.w))
                {
                    RecalculateMatrices();

                    oldPosition = position;
                    oldRotation = rotation;
                }

                for (int i = 0; i < matrices.Length; i++)
                {
                    if (!detailObjects[i].useGPUInstancing)
                        for (int j = 0; j < matrices[i].Length; j++)
                        {
                            Graphics.DrawMesh(detailObjects[i].mesh, matrices[i][j], detailObjects[i].material, 0);
                        }
                    else Graphics.DrawMeshInstanced(detailObjects[i].mesh, 0, detailObjects[i].material, matrices[i]);
                }
            }
            else if (!generating && planet.detailObjectsGenerating < planet.detailObjectsGeneratingSimultaneously)
            {
                generating = true;
                StartCoroutine(GenerateDetails());
                planet.detailObjectsGenerating++;
            }

        }

        /// <summary>
        /// Matrices are needed to render meshes or grass with DrawMesh()/DrawMeshInstanced(). They need to be recomputed when the quad has rotated or moved.
        /// </summary>
        void RecalculateMatrices()
        {
            for (int i = 0; i < matrices.Length; i++)
            {
                matrices[i] = ToMatrix4x4Array(detailObjects[i].posRots, detailObjects[i].meshScale);
            }
        }

        Matrix4x4[] ToMatrix4x4Array(PosRot[] posRots, Vector3 meshScale)
        {

            var matrices = new Matrix4x4[posRots.Length];

            if (rotation != Quaternion.identity)
            {
                for (int i = 0; i < matrices.Length; i++)
                {
                    matrices[i].SetTRS(MathFunctions.RotateAroundPoint((posRots[i].position + position), position, rotation), posRots[i].rotation * rotation, meshScale);
                }

            } // Computation is simpler when quad is not rotated.
            else for (int i = 0; i < matrices.Length; i++)
                {
                    matrices[i].SetTRS(posRots[i].position + transform.position, posRots[i].rotation, meshScale);
                }

            return matrices;
        }

        public void OnDestroy()
        {
            if (generating)
                planet.detailObjectsGenerating--;

            while (spawnedPrefabs.Count != 0)
            {
                Destroy(spawnedPrefabs[0]);
                spawnedPrefabs.RemoveAt(0);
            }
        }

        public IEnumerator GenerateDetails()
        {

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            Vector3 down = planet.Vector3Down(meshRenderer.bounds.center);
            FoliageGenerator fm = new FoliageGenerator(quad, quad.mesh, QuaternionD.Inverse(planet.rotation), down, planet.foliageBiomes & Biome.All, (int)(quad.index / 0x200000004));

            int numberOfPoints = planet.grassPerQuad;

            for (int i = 0; i < planet.detailMeshes.Count; i++)
                numberOfPoints += planet.detailMeshes[i].number;

            for (int i = 0; i < planet.detailPrefabs.Count; i++)
                numberOfPoints += planet.detailPrefabs[i].number;

            int currentPoints = numberOfPoints;

            const int pointsPerFrame = 5000;

            int length = (currentPoints / pointsPerFrame);

            if (length <= 1)
            {
                fm.PointCloud(currentPoints); //generate if points per quad is less than points per frame
                length = 0;
            }

            for (int i = 0; i < length; i++)
            {
                if (currentPoints - pointsPerFrame > 0)
                {
                    currentPoints -= pointsPerFrame;
                    fm.PointCloud(pointsPerFrame);
                    yield return null;
                }
                else fm.PointCloud(currentPoints);
            }

            float frac = (float) fm.positions.Count / numberOfPoints;

            List<DetailPrefab> detailPrefabs = new List<DetailPrefab>();

            if (!planet.expMeshes)
                for (int i = 0; i < planet.detailMeshes.Count; i++)
                {
                    if (planet.detailMeshes[i].number != 0)
                    {
                        DetailMesh dO;
                        dO = new DetailMesh(planet.detailMeshes[i]);
                        detailObjects.Add(dO);
                        dO.posRots = fm.Positions(Mathf.RoundToInt(planet.detailMeshes[i].number * frac), planet.detailMeshes[i]);
                    }
                }

            if (!planet.expPrefabs)
                for (int i = 0; i < planet.detailPrefabs.Count; i++)
                {
                    if (planet.detailPrefabs[i].number != 0)
                    {
                        DetailPrefab dO;
                        dO = new DetailPrefab(planet.detailPrefabs[i]);
                        detailPrefabs.Add(dO);
                        dO.posRots = fm.Positions(Mathf.RoundToInt(planet.detailPrefabs[i].number * frac), planet.detailPrefabs[i]);
                    }
                }

            if (!planet.expGrass && planet.generateGrass)
            {
                DetailMesh grass = new DetailMesh(fm.CreateMesh(), planet.grassMaterial);
                grass.posRots = new PosRot[] { new PosRot(Vector3.zero, Quaternion.identity) };
                grass.number = 0;
                grass.isGrass = true;
                detailObjects.Add(grass);
            }

            for (int i = 0; i < detailPrefabs.Count; i++)
            {
                for (int j = 0; j < detailPrefabs[i].posRots.Length; j++)
                {
                    spawnedPrefabs.Add(Instantiate(detailPrefabs[i].prefab, transform.TransformPoint(detailPrefabs[i].posRots[j].position), detailPrefabs[i].posRots[j].rotation, transform));
                }
            }

            planet.detailObjectsGenerating--;
            generating = false;
            Initialize();
        }
    }
}
