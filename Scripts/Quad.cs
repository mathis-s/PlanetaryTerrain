using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain.Foliage;
using System.Text;
using UnityEngine.Rendering;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain
{
    public enum QuadPlane { XPlane, YPlane, ZPlane };
    public enum Position { Back, Front };

    [Flags] //Configuration of quad edge fans in order right, left, down, up
    public enum EdgeConfiguration //int is split up into 4 unsigned 8-bit numbers that encode the level difference to the four neighbors
    {
        None = 0,
        Right = 0xFF,
        Left = 0xFF_00,
        Down = 0xFF_00_00,
        Up = ~0x00_FF_FF_FF,
        All = ~0
    }

    [Flags]
    public enum Biome
    {
        None = 0,
        Zero = 0b0000_0001,
        One = 0b0000_0010,
        Two = 0b0000_0100,
        Three = 0b0000_1000,
        Four = 0b0001_0000,
        Five = 0b0010_0000,
        All = 0b0011_1111
    }

    public class Quad
    {
        public Planet planet;
        public Quad parent;
        public Quad[] children;
        public Quad[] neighbors;
        public EdgeConfiguration configuration;
        public int level = 0;
        public ulong index;
        public QuadPlane plane = 0;
        public Position position = 0;
        public bool hasSplit = false;
        public bool isSplitting;
        public bool inSplitQueue;
        public bool initialized;
        public Mesh mesh;
        public Vector3 trPosition;
        public Quaternion rotation;
        public QuaternionD rotationD;
        public GameObject renderedQuad;
        public Vector3 meshOffset;
        public Coroutine coroutine;
        public float distance;

        internal Biome biome;

        internal float scale = 1f;
        internal float msd; //mean squared deviation
        internal MeshGenerator meshGenerator;


        private ulong[] neighborIds;
        private FoliageRenderer foliageRenderer;
        private bool visibleToCamera;



        static Dictionary<int2, int[]> orderOfChildren = new Dictionary<int2, int[]>() {

                {new int2(0, 1), new int[] {2, 0, 1, 3}}, //{int2(plane, position), int[]{order of children}}
                {new int2(0, 0), new int[] {3, 1, 0, 2}},

                {new int2(1, 1), new int[] {1, 0, 2, 3}},
                {new int2(1, 0), new int[] {3, 2, 0, 1}},

                {new int2(2, 1), new int[] {3, 2, 0, 1}},
                {new int2(2, 0), new int[] {2, 3, 1, 0}},
            };

        /// <summary>
        /// Resets all variables. Used for pooling.
        /// </summary>
        internal void Reset()
        {
            biome = Biome.None;
            parent = null;
            children = null;
            neighbors = null;
            configuration = EdgeConfiguration.None;
            hasSplit = false;
            isSplitting = false;
            initialized = false;
            neighborIds = null;
            meshOffset = Vector3.zero;
            foliageRenderer = null;
            distance = Mathf.Infinity;
            coroutine = null;
            level = 1;
            msd = 0f;
            meshGenerator = null;
        }


        #region LOD

        /// <summary>
        /// Recalculates quad's distance to the camera. Can then split or combine based on new distance
        /// </summary>
        internal void UpdateDistances()
        {
            if (initialized)
            {
                if (mesh != null)
                {
                    distance = mesh.bounds.SqrDistance(planet.worldToMeshVector - meshOffset); //Converting cameraPosition to local mesh position to use bounds.
                    visibleToCamera = VisibleToCamera();
                }
                //LOD: Checking distances, deciding to split or combine
                if (level < planet.detailDistancesSqr.Length)
                {
                    if (distance < planet.detailDistancesSqr[level] && visibleToCamera && (!planet.calculateMsds || msd >= planet.detailMsds[level]) && !hasSplit && !isSplitting)
                        planet.quadSplitQueue.Add(this);

                    if ((distance > planet.detailDistancesSqr[level] || !visibleToCamera)) //Combine as often as possible if invisible to camera
                    {
                        if (inSplitQueue)
                            planet.quadSplitQueue.Remove(this);
                        if (hasSplit)
                            Combine();
                    }
                }

                if (!renderedQuad && visibleToCamera && !hasSplit && !isSplitting && !planet.inScaledSpace) //If it will be visible, create GameObject in scene
                {
                    renderedQuad = planet.quadGameObjectPool.GetGameObject(this);
                }
                else if (renderedQuad && (!visibleToCamera || planet.inScaledSpace || hasSplit)) //Remove if the generated renderQuad is not visible
                    planet.quadGameObjectPool.RemoveGameObject(this);

                if (renderedQuad && !renderedQuad.activeSelf && (level == 0 || parent.hasSplit))
                {
                    renderedQuad.SetActive(true);
                    if (!planet.quadIndicies.ContainsKey(index))
                        planet.quadIndicies.Add(index, this);
                    UpdateNeighbors();
                }

                //Foliage Stuff:
                if (planet.generateDetails && ((biome & planet.foliageBiomes) != Biome.None || (planet.foliageBiomes & Biome.All) == Biome.All)) //Generating details if enabled and right biome.
                {
                    if (level >= planet.grassLevel && foliageRenderer == null && renderedQuad != null && distance < planet.detailDistanceSqr)
                    {
                        foliageRenderer = renderedQuad.AddComponent<FoliageRenderer>();
                        foliageRenderer.planet = planet;
                        foliageRenderer.quad = this;
                    }
                    else if (foliageRenderer != null && distance > planet.detailDistanceSqr)
                    {
                        MonoBehaviour.Destroy(foliageRenderer);
                        foliageRenderer = null;
                    }
                }
            }
        }


        /// <summary>
        /// Checks if mesh generation on other thread or on the GPU is finished. If so, it is applied to the mesh.
        /// </summary>
        internal void Update()
        {
            if (meshGenerator != null && meshGenerator.isRunning && meshGenerator.isCompleted)
            {
                if (!mesh)
                    mesh = new Mesh();
                else
                    mesh.Clear();

                meshGenerator.ApplyToMesh(mesh);
                mesh.triangles = planet.quadArrays.GetTriangles(configuration);
                mesh.RecalculateBounds();

                if (renderedQuad)
                    renderedQuad.GetComponent<MeshFilter>().mesh = mesh;

                meshGenerator.Dispose();
                meshGenerator = null;

                initialized = true;
            }
        }

        /// <summary>
        /// Finds this Quad's neighbors, then checks if edge fans are needed. If so, they are applied.
        /// </summary>
        internal void GetNeighbors()
        {
            if (neighborIds == null) //Finding the IDs of all neigbors. This is only done once.
            {
                neighborIds = new ulong[4];
                for (int i = 0; i < 4; i++)
                {
                    neighborIds[i] = QuadNeighbor.GetNeighbor(index, i);
                }
            }
            neighbors = new Quad[4];

            for (int i = 0; i < 4; i++) //Trying to find neighbors by id. If not there, neighbor has a lower subdivision level, last char of id is removed.
            {
                int j = 0;

                ulong idTemp = neighborIds[i];
                while (neighbors[i] == null && j < 3)
                {
                    planet.quadIndicies.TryGetValue(idTemp, out neighbors[i]);
                    idTemp = QuadNeighbor.Slice(idTemp);
                    j++;
                }
            }

            EdgeConfiguration configurationOld = configuration;
            configuration = EdgeConfiguration.None;

            for (int i = 0; i < neighbors.Length; i++) //Creating configuration based on neighbor levels.
            {
                if (neighbors[i] != null)
                {
                    if (neighbors[i].renderedQuad == null)
                        continue;

                    int delta = level - neighbors[i].level;

                    if (delta > 0)
                    {
                        delta = delta << 24;
                        configuration = (EdgeConfiguration)((int)configuration | (delta >> 8 * i));
                    }
                }
            }

            if (configuration != configurationOld && initialized && mesh.vertices.Length > 0)
                mesh.triangles = planet.quadArrays.GetTriangles(configuration); //If mesh was already generated we only need new triangles that skip every other edge vertex
        }

        internal void StartMeshGeneration()
        {
            switch (planet.serializedInherited.heightProviderType)
            {
                case HeightProviderType.ComputeShader:
                    meshGenerator = new GPUMeshGenerator(planet, this);
                    break;

                //case HeightProviderType.Burst:
                //    meshGenerator = new MeshGenerationBurst(planet, this);
                //    break;

                default:
                    meshGenerator = new CPUMeshGenerator(planet, this);
                    break;
            }
            meshGenerator.StartGeneration();
        }


        /// <summary>
        /// Splits the Quad, creates four smaller ones 
        /// </summary>
        internal IEnumerator Split()
        {

            if (!hasSplit)
            {
                isSplitting = true;
                children = new Quad[4];

                int[] order = orderOfChildren[new int2((int)plane, (int)position)];

                //Creating children
                switch (plane)
                {
                    case QuadPlane.XPlane:

                        children[order[0]] = planet.quadPool.GetQuad(new Vector3(trPosition.x, trPosition.y - 1f / 2f * scale, trPosition.z - 1f / 2f * scale), rotation);
                        children[order[1]] = planet.quadPool.GetQuad(new Vector3(trPosition.x, trPosition.y + 1f / 2f * scale, trPosition.z - 1f / 2f * scale), rotation);
                        children[order[2]] = planet.quadPool.GetQuad(new Vector3(trPosition.x, trPosition.y + 1f / 2f * scale, trPosition.z + 1f / 2f * scale), rotation);
                        children[order[3]] = planet.quadPool.GetQuad(new Vector3(trPosition.x, trPosition.y - 1f / 2f * scale, trPosition.z + 1f / 2f * scale), rotation);
                        break;

                    case QuadPlane.YPlane:

                        children[order[0]] = planet.quadPool.GetQuad(new Vector3(trPosition.x - 1f / 2f * scale, trPosition.y, trPosition.z - 1f / 2f * scale), rotation);
                        children[order[1]] = planet.quadPool.GetQuad(new Vector3(trPosition.x + 1f / 2f * scale, trPosition.y, trPosition.z - 1f / 2f * scale), rotation);
                        children[order[2]] = planet.quadPool.GetQuad(new Vector3(trPosition.x + 1f / 2f * scale, trPosition.y, trPosition.z + 1f / 2f * scale), rotation);
                        children[order[3]] = planet.quadPool.GetQuad(new Vector3(trPosition.x - 1f / 2f * scale, trPosition.y, trPosition.z + 1f / 2f * scale), rotation);
                        break;

                    case QuadPlane.ZPlane:

                        children[order[0]] = planet.quadPool.GetQuad(new Vector3(trPosition.x - 1f / 2f * scale, trPosition.y - 1f / 2f * scale, trPosition.z), rotation);
                        children[order[1]] = planet.quadPool.GetQuad(new Vector3(trPosition.x + 1f / 2f * scale, trPosition.y - 1f / 2f * scale, trPosition.z), rotation);
                        children[order[2]] = planet.quadPool.GetQuad(new Vector3(trPosition.x + 1f / 2f * scale, trPosition.y + 1f / 2f * scale, trPosition.z), rotation);
                        children[order[3]] = planet.quadPool.GetQuad(new Vector3(trPosition.x - 1f / 2f * scale, trPosition.y + 1f / 2f * scale, trPosition.z), rotation);
                        break;
                }
                int i;
                for (i = 0; i < 4; i++)
                {
                    children[i].scale = scale / 2;
                    children[i].level = level + 1;
                    children[i].plane = plane;
                    children[i].parent = this;
                    children[i].planet = planet;
                    children[i].index = QuadNeighbor.Append(index, i);
                    children[i].position = position;
                    planet.quads.Add(children[i]);

                    if (planet.serializedInherited.heightProviderType != HeightProviderType.ComputeShader)
                        children[i].StartMeshGeneration();
                }

                for (i = 0; i < 4; i++)
                {
                    if (planet.serializedInherited.heightProviderType == HeightProviderType.ComputeShader)
                        children[i].StartMeshGeneration();

                    while (!children[i].initialized) //Waiting until Quad is initialized
                    {
                        children[i].Update();
                        yield return null;
                    }
                    children[i].UpdateDistances();
                }


                for (i = 0; i < 4; i++)
                {
                    if (children[i].renderedQuad)
                    {
                        children[i].renderedQuad.SetActive(true);
                        planet.quadIndicies.Add(children[i].index, children[i]);
                    }
                }

                for (i = 0; i < 4; i++)
                    children[i].GetNeighbors();

                if (renderedQuad)
                    planet.quadGameObjectPool.RemoveGameObject(this);

                UpdateNeighbors();

                isSplitting = false;
                hasSplit = true;
                coroutine = null;
            }
        }

        /// <summary>
        /// Update neighbors in all neighbors and all their children
        /// </summary>
        private void UpdateNeighbors()
        {
            if (neighbors != null)
                for (int i = 0; i < neighbors.Length; i++)
                {
                    if (neighbors[i] != null)
                        neighbors[i].GetNeighborsAll();
                }
        }
        /// <summary>
        /// Update neighbors in this Quad and all children
        /// </summary>
        private void GetNeighborsAll()
        {
            if (initialized)
            {
                if (children != null)
                    for (int i = 0; i < children.Length; i++)
                    {
                        children[i].GetNeighborsAll();
                    }

                GetNeighbors();
            }
        }

        /// <summary>
        /// Removes all of this quad's children and reenables rendering
        /// </summary>
        private void Combine()
        {
            if (hasSplit && !isSplitting)
            {
                hasSplit = false;
                for (int i = 0; i < 4; i++)
                {
                    if (children[i].hasSplit)
                        children[i].Combine();

                    planet.quadPool.RemoveQuad(children[i]);
                }

                children = null;
            }
        }
        /// <summary>
        /// Is this quad visible to the camera? Called when over Recompute Quad Threshold
        /// </summary>
        private bool VisibleToCamera()
        {
            if (distance <= planet.radiusVisSphere)
                if (planet.lodModeBehindCam == LODModeBehindCam.ComputeRender || Utils.TestPlanesAABB(planet.viewPlanes, planet.transform.TransformPoint(mesh.bounds.min + meshOffset), planet.transform.TransformPoint(mesh.bounds.max + meshOffset), true, planet.behindCameraExtraRange))
                    return true;
            return false;
        }
        #endregion

        /// <summary>
        /// Create a new Quad with trPosition and rotation
        /// </summary>
        public Quad(Vector3 position, Quaternion rotation)
        {
            this.trPosition = position;
            this.rotation = rotation;
        }

        private void print(object message)
        {
            Debug.Log(message);
        }
    }
}





