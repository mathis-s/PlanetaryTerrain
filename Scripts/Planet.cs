using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain.Foliage;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain
{

    public enum LODModeBehindCam { NotComputed, ComputeRender } //How are quads behind the camera handled?
    public enum HeightProviderType { Heightmap, Noise, Hybrid, Const, ComputeShader, StreamingHeightmap, DetailHeightmaps/*Burst*/ }
    public enum UVType { Cube, Quad, Legacy, LegacyContinuous }
    public enum TextureProviderType { Gradient, Range, Splatmap, None }
    public enum SlopeTextureType { Fade, Threshold, None }

    public class Planet : MonoBehaviour
    {
        //General
        public float radius = 10000;
        public QuaternionD rotation;
        public float[] detailDistances = { 50000, 25000, 12500, 6250, 3125 };
        public bool calculateMsds;
        public float[] detailMsds = { 0f, 0f, 0f, 0f, 0f };
        public LODModeBehindCam lodModeBehindCam = LODModeBehindCam.ComputeRender;
        public float behindCameraExtraRange;
        public Material planetMaterial;
        public UVType uvType = UVType.Cube;
        public float uvScale = 1f;
        public bool[] generateColliders = { false, false, false, false, false, true };
        public float visSphereRadiusMod = 1f;
        public bool updateAllQuads = false;
        public int maxQuadsToUpdate = 250;
        public float recomputeQuadDistancesThreshold = 10f;
        public int quadsSplittingSimultaneously = 2;
        public int quadSize = 33;

        //Scaled Space
        public bool useScaledSpace;
        public float scaledSpaceDistance = 1500f;
        public float scaledSpaceFactor = 100000f;
        public Material scaledSpaceMaterial;
        public GameObject scaledSpaceCopy;

        //Biomes
        public ITextureProvider textureProvider;
        public SlopeTextureType slopeTextureType;
        public float slopeFadeInAngle = 10;
        public float slopeAngle = 60;
        public int slopeTexture = 5;

        //Terrain generation
        public IHeightProvider heightProvider;
        public ComputeShader computeShader;
        public float heightScale = 0.02f;

        //Detail/Grass generation
        public bool generateDetails;
        public bool generateGrass;
        public Material grassMaterial;
        public int grassPerQuad = 10000;
        public int grassLevel = 5;
        public float detailDistance;
        public bool expGrass, expMeshes, expPrefabs;
        public List<DetailMesh> detailMeshes = new List<DetailMesh>();
        public List<DetailPrefab> detailPrefabs = new List<DetailPrefab>();
        public Biome foliageBiomes;
        public int detailObjectsGeneratingSimultaneously = 3;

        //Misc
        public FloatingOrigin floatingOrigin;
        public bool hideQuads = true;
#if UNITY_EDITOR
        public int numQuads;
        //only used for editor preview gradient
        public Color32[] textureColorSequence = new Color32[] { new Color32(0x1f, 0x77, 0xb4, 0xff), new Color32(0xff, 0x7f, 0x0e, 0xff), new Color32(0x2c, 0xa0, 0x2c, 0xff), new Color32(0xd6, 0x27, 0x28, 0xff), new Color32(0x94, 0x67, 0xbd, 0xff), new Color32(0x8c, 0x56, 0x4b, 0xff) };
#endif



        internal QuadSplitQueue quadSplitQueue;
        internal QuadPool quadPool;
        internal QuadGameObjectPool quadGameObjectPool;
        internal BaseQuadMeshes quadArrays;
        internal List<Quad> quads = new List<Quad>();
        internal Dictionary<ulong, Quad> quadIndicies = new Dictionary<ulong, Quad>();
        internal UnityEngine.Plane[] viewPlanes;
        internal bool initialized;
        internal bool inScaledSpace;
        internal bool usingLegacyUVType;
        internal float detailDistanceSqr;
        internal float heightInv;
        internal float radiusVisSphere;
        internal float[] detailDistancesSqr;
        internal int detailObjectsGenerating;
        internal Vector3 worldToMeshVector;
        internal GameObject quadGO;


        private Camera mainCamera;
        private Transform mainCameraTr;

        private float radiusSqr;
        private float radiusMaxSqr;
        private float scaledSpaceDisSqr;
        private float recomputeQuadDistancesThresholdSqr;

        private Quaternion oldCamRotation;
        private Vector3 oldCamPosition;
        private Vector3 oldPlanetPosition;
        private QuaternionD oldPlanetRotation;
        private Coroutine quadUpdateCV;

        private int framesSinceSplit = 0;

        public delegate void ScaledSpaceStateChanged(bool inScaledSpace);
        public ScaledSpaceStateChanged scaledSpaceStateChanged;
        public UnityEngine.Events.UnityEvent enteredScaledSpace;
        public UnityEngine.Events.UnityEvent leftScaledSpace;

        public delegate void FinishedGeneration();
        public FinishedGeneration finishedGeneration;
        public UnityEngine.Events.UnityEvent eventFinishedGeneration;


        [Serializable] //Unity can't serialize derived classes in variables of type typeof(base class/interface), so all derived classes are stored in this container. The selected derived class is loaded into the corresponding base-class-variable in Initialize().
        public class SerializedInheritedClasses
        {
            public HeightProviderType heightProviderType = HeightProviderType.Const;
            public HeightmapHeightProvider heightmapHeightProvider;
            public NoiseHeightProvider noiseHeightProvider;
            public HybridHeightProvider hybridHeightProvider;
            public ConstHeightProvider constHeightProvider;
            public StreamingHeightmapHeightProvider streamingHeightmapHeightProvider;
            public DetailHeightmapHeightProvider detailHeightmapHeightProvider;

            public TextureProviderType textureProviderType = TextureProviderType.None;
            public TextureProviderGradient textureProviderGradient;
            public TextureProviderRange textureProviderRange;
            public TextureProviderSplatmap textureProviderSplatmap;

        }

        [SerializeField]
        public SerializedInheritedClasses serializedInherited = new SerializedInheritedClasses();
        public void Awake()
        {

            if (detailDistances.Length > generateColliders.Length - 1)
            {
                Debug.LogWarning("Generate Colliders needs to be one longer than Detail Distances! Length was increased!");
                generateColliders = new bool[detailDistances.Length + 1];
            }
            //throw new ArgumentOutOfRangeException("detailDistances, generateColliders", "Generate Colliders needs to be one longer than Detail Distances!");

            if (calculateMsds && detailDistances.Length != detailMsds.Length)
                throw new ArgumentOutOfRangeException("detailDistances, detailMsds", "Detail Distances and Detail Msds need to be the same size!");

            Initialize();

            StartCoroutine(InstantiateBaseQuads());
        }

        public void Initialize()
        {

            switch (serializedInherited.heightProviderType)
            {
                default:
                    heightProvider = serializedInherited.constHeightProvider;
                    break;
                case HeightProviderType.Heightmap:
                    heightProvider = serializedInherited.heightmapHeightProvider;
                    break;
                case HeightProviderType.Noise:
                    heightProvider = serializedInherited.noiseHeightProvider;
                    break;
                case HeightProviderType.Hybrid:
                    heightProvider = serializedInherited.hybridHeightProvider;
                    break;
                case HeightProviderType.StreamingHeightmap:
                    heightProvider = serializedInherited.streamingHeightmapHeightProvider;
                    break;
                case HeightProviderType.DetailHeightmaps:
                    heightProvider = serializedInherited.detailHeightmapHeightProvider;
                    break;
            }
            heightProvider.Init();


            switch (serializedInherited.textureProviderType)
            {
                default:
                    textureProvider = new TextureProviderNone();
                    break;
                case TextureProviderType.Gradient:
                    textureProvider = serializedInherited.textureProviderGradient;
                    break;
                case TextureProviderType.Range:
                    textureProvider = serializedInherited.textureProviderRange;
                    break;
                case TextureProviderType.Splatmap:
                    textureProvider = serializedInherited.textureProviderSplatmap;
                    (textureProvider as TextureProviderSplatmap).Init();
                    break;

            }

            usingLegacyUVType = uvType == UVType.Legacy || uvType == UVType.LegacyContinuous;
            mainCamera = Camera.main;
            mainCameraTr = mainCamera.transform;

            if (generateDetails)
                generateDetails = (!expGrass && generateGrass) || (!expMeshes && detailMeshes.Count > 0) || (!expPrefabs && detailPrefabs.Count > 0);

            quadArrays = new BaseQuadMeshes(quadSize);

            if (!floatingOrigin)
            {
                floatingOrigin = GetComponent<FloatingOrigin>();
                if (floatingOrigin)
                {
                    Debug.LogError("Floating Origin found but not defined on planet! Make sure to define floating origin on each planet.");
                }
            }
            heightInv = 1f / heightScale;
            rotation = transform.rotation.ToQuaterniond();
            oldPlanetPosition = transform.position;
            oldPlanetRotation = rotation;

            quadGO = ((GameObject)Resources.Load("plane")); //Original renderQuad

            if (generateDetails && grassMaterial)
                ((GameObject)Resources.Load("Grass")).GetComponent<MeshRenderer>().material = grassMaterial;

            radiusMaxSqr = radius * (heightInv + 1) / heightInv; //Squared values so sqrt is not required
            radiusMaxSqr *= radiusMaxSqr;
            radiusSqr = radius * radius;
            recomputeQuadDistancesThresholdSqr = recomputeQuadDistancesThreshold * recomputeQuadDistancesThreshold;
            scaledSpaceDisSqr = scaledSpaceDistance * scaledSpaceDistance;
            detailDistanceSqr = detailDistance * detailDistance;

            detailDistancesSqr = new float[detailDistances.Length];
            for (int i = 0; i < detailDistances.Length; i++)
                detailDistancesSqr[i] = detailDistances[i] * detailDistances[i];


            float camHeight = (mainCamera.transform.position - transform.position).sqrMagnitude;

            quadSplitQueue = new QuadSplitQueue(this);
            quadPool = new QuadPool(this);
            quadGameObjectPool = new QuadGameObjectPool(this);

            if (useScaledSpace && Application.isPlaying)
            {
                if (camHeight > scaledSpaceDisSqr)
                {
                    inScaledSpace = true;
                    if (scaledSpaceStateChanged != null)
                        scaledSpaceStateChanged(inScaledSpace);
                    enteredScaledSpace.Invoke();
                }
                else if (camHeight < scaledSpaceDisSqr)
                {
                    inScaledSpace = false;
                    if (scaledSpaceStateChanged != null)
                        scaledSpaceStateChanged(inScaledSpace);
                    leftScaledSpace.Invoke();
                }
            }
        }

        public void Reset()
        {
            heightProvider = null;
            textureProvider = null;
        }

        public void OnDisable()
        {
            if (quads != null)
                for (int i = 0; i < quads.Count; i++)
                {
                    if (quads[i].meshGenerator != null)
                    {
                        quads[i].meshGenerator.Dispose();
                        quads[i].meshGenerator = null;
                    }
                }

            if (heightProvider is StreamingHeightmapHeightProvider)
            {
                (heightProvider as StreamingHeightmapHeightProvider).sHeightmap.Close();
            }

            quadArrays.trisNative.Dispose();
        }

        /// <summary>
        /// Creates the six base quads (one for each side of the spherified cube).
        /// </summary>
        private IEnumerator InstantiateBaseQuads() //Instantiate quads and assign values
        {

            quads.Add(new Quad(Vector3.up, Quaternion.Euler(0, 180, 0)));
            quads[0].plane = QuadPlane.YPlane;
            quads[0].position = Position.Front;
            quads[0].index = QuadNeighbor.Encode(new int[] { 0, 1 });//Check QuadNeighbor.cs for an explaination of indices.

            quads.Add(new Quad(Vector3.down, Quaternion.Euler(180, 180, 0)));
            quads[1].plane = QuadPlane.YPlane;
            quads[1].position = Position.Back;
            quads[1].index = QuadNeighbor.Encode(new int[] { 2, 1 });

            quads.Add(new Quad(Vector3.forward, Quaternion.Euler(270, 270, 270)));
            quads[2].plane = QuadPlane.ZPlane;
            quads[2].position = Position.Front;
            quads[2].index = QuadNeighbor.Encode(new int[] { 0, 3 });

            quads.Add(new Quad(Vector3.back, Quaternion.Euler(270, 0, 0)));
            quads[3].plane = QuadPlane.ZPlane;
            quads[3].position = Position.Back;
            quads[3].index = QuadNeighbor.Encode(new int[] { 1, 3 });

            quads.Add(new Quad(Vector3.right, Quaternion.Euler(270, 0, 270)));
            quads[4].plane = QuadPlane.XPlane;
            quads[4].position = Position.Front;
            quads[4].index = QuadNeighbor.Encode(new int[] { 0, 2 });

            quads.Add(new Quad(Vector3.left, Quaternion.Euler(270, 0, 90)));
            quads[5].plane = QuadPlane.XPlane;
            quads[5].position = Position.Back;
            quads[5].index = QuadNeighbor.Encode(new int[] { 1, 2 });

            Quad[] l0Quads = new Quad[6];

            for (int i = 0; i < quads.Count; i++)
            {
                l0Quads[i] = quads[i];
                quads[i].planet = this;
                quadIndicies.Add(quads[i].index, quads[i]);
            }

            for (int i = 0; i < 6; i++)
            {
                l0Quads[i].StartMeshGeneration();
                while (!l0Quads[i].initialized) //Waiting until Quad is initialized
                {
                    l0Quads[i].Update();
                    yield return null;
                }

                l0Quads[i].UpdateDistances();
            }

            initialized = true;
        }



        private void Update()
        {
            transform.rotation = (Quaternion)rotation;

            Vector3 camPos = mainCameraTr.position;
            Quaternion camRot = mainCameraTr.rotation;

            Vector3 trPos = transform.position;
            Quaternion trRot = transform.rotation;

            Vector3 relCamPos = camPos - trPos;

            if (heightProvider is StreamingHeightmapHeightProvider)
            {
                var p = (heightProvider as StreamingHeightmapHeightProvider);

                Vector3 pos = Quaternion.Inverse(trRot) * relCamPos.normalized;
                p.Update(quadSplitQueue, pos);
            }

            if (!quadSplitQueue.Update())
            {
                if (framesSinceSplit > 30)
                {
                    if (finishedGeneration != null)
                        finishedGeneration();
                    eventFinishedGeneration.Invoke();

                    framesSinceSplit = -1;
                }
                else if (framesSinceSplit != -1)
                    framesSinceSplit++;
            }
            else framesSinceSplit = 0;

            //Vector used by Quads when computing distance
            worldToMeshVector = Quaternion.Inverse(trRot) * (mainCameraTr.position - trPos);

#if UNITY_EDITOR
            numQuads = quads.Count;
#endif

            //radiusVisSphere is used by Quads to check if they are visible
            float camHeight = (camPos - trPos).sqrMagnitude;
            radiusVisSphere = (camHeight + (radiusMaxSqr - 2 * radiusSqr)) * visSphereRadiusMod;

            //Recompute quad positions if the planet rotation has changed
            bool overRotationThreshold = !QuaternionD.Equals(rotation, oldPlanetRotation);
            if (overRotationThreshold)
            {
                oldPlanetRotation = rotation;
                UpdatePosition();
            }

            //Quad positions are also recalculated when the camera has moved farther than the threshold
            bool changedViewport = (relCamPos - oldCamPosition).sqrMagnitude > recomputeQuadDistancesThresholdSqr || overRotationThreshold || (camRot != oldCamRotation && lodModeBehindCam == LODModeBehindCam.NotComputed);

            if (changedViewport)
            {
                oldCamPosition = relCamPos;
                oldCamRotation = camRot;
            }

            if (scaledSpaceCopy)
            {
                scaledSpaceCopy.transform.rotation = trRot; //Set scaledSpaceCopy rotation to planet's rotation
                if (floatingOrigin == null)
                    scaledSpaceCopy.transform.position = transform.position / scaledSpaceFactor;
                else
                    scaledSpaceCopy.transform.position = floatingOrigin.WorldSpaceToScaledSpace(transform.position, scaledSpaceFactor);
            }

            if (changedViewport && (camHeight < scaledSpaceDisSqr * 1.5f || !useScaledSpace)) //Update all quads when close to planet or not using scaled space
            {
                UpdateQuads(); //Quads recalculate their distance to the camera and check if they are close enough to split or far enough to combine.
            }

            //If cameraViewPlanes are needed to check quad visibilty they are computed here.
            if (lodModeBehindCam == LODModeBehindCam.NotComputed)
                viewPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

            //Move all quads if planet center has moved.
            if (trPos != oldPlanetPosition)
            {
                UpdatePosition();
                oldPlanetPosition = trPos;
            }

            if (useScaledSpace && changedViewport)
            {
                if (camHeight > scaledSpaceDisSqr && !inScaledSpace)
                {
                    inScaledSpace = true;
                    if (scaledSpaceStateChanged != null)
                        scaledSpaceStateChanged(inScaledSpace);
                    enteredScaledSpace.Invoke();
                }
                else if (camHeight < scaledSpaceDisSqr && inScaledSpace)
                {
                    inScaledSpace = false;
                    if (scaledSpaceStateChanged != null)
                        scaledSpaceStateChanged(inScaledSpace);
                    leftScaledSpace.Invoke();
                }

            }
        }

        /// <summary>
        /// Recalculates the positions of all rendered quads. Is called when the planet has been moved or rotated.
        /// </summary>
        internal void UpdatePosition()
        {
            Vector3d plPos = transform.position.ToVector3d();

            int len = quads.Count;
            for (int i = 0; i < len; i++)
            {
                //We need to do the math with doubles for increased accuracy. The final result is converted back to floats.
                if (quads[i].renderedQuad)
                {
                    quads[i].renderedQuad.transform.position = (Vector3)((rotation * quads[i].trPosition.ToVector3d() + rotation * quads[i].meshOffset.ToVector3d()) + plPos);
                    quads[i].renderedQuad.transform.rotation = rotation;
                }
            }
        }

        internal Vector3 GetRenderedQuadPosition(Quad quad)
        {
            return (Vector3)((rotation * quad.meshOffset.ToVector3d()) + transform.position.ToVector3d());
        }

        /// <summary>
        /// Updates all quads. When updateAllQuads == false, the Update process is done over multiple frames via a coroutine. The quads recompute their distances to the camera and can then split or combine.
        /// </summary>
        private void UpdateQuads()
        {
            if (quadUpdateCV == null && !updateAllQuads)
            {
                //Coroutine for Updating viewport over multiple frames
                quadUpdateCV = StartCoroutine(UpdateChangedViewport());
            }
            else
            {
                //Fallback if UpdateChangedViewport Coroutine is too slow. This should optimally never be run. Increase maxQuadsToUpdate or recomputeQuadDistancesThreshold if this is run.
                if (quadUpdateCV != null)
                    StopCoroutine(quadUpdateCV);

                quadUpdateCV = null;

                Quad[] quadArray = quads.ToArray();
                int len = quadArray.Length;

                for (int i = 0; i < len; i++)
                {
                    Quad q = quadArray[i];

                    if (q.initialized)
                        q.UpdateDistances();
                }
            }
        }

        /// <summary>
        /// Coroutine for Updating viewport over multiple frames.
        /// </summary>
        private IEnumerator UpdateChangedViewport()
        {
            var qa = quads.ToArray();
            int len = qa.Length;

            int i = 0;
            while (i < len)
            {
                Quad q = qa[i];

                if (q.initialized)
                    q.UpdateDistances();

                if (++i % maxQuadsToUpdate == 0)
                    yield return null;
            }
            quadUpdateCV = null;
        }

        /// <summary>
        /// Finds quad by index
        /// </summary>
        public Quad FindQuad(ulong index)
        {
            Quad q;
            if (quadIndicies.TryGetValue(index, out q))
                return q;
            return null;
        }

        /// <summary>
        /// Instantiates object on this planet.
        /// </summary>
        /// <param name="objToInst">the object to instantiate
        /// </param>
        /// <param name="LatLon">position
        /// </param>
        public GameObject InstantiateOnPlanet(GameObject objToInst, Vector2 LatLon, float offsetUp = 0f)
        {
            Vector3 xyz = transform.rotation * MathFunctions.LatLonToXyz(LatLon, radius);
            return Instantiate(objToInst, xyz * ((heightInv + heightProvider.HeightAtXYZ(Quaternion.Inverse(transform.rotation) * (xyz / radius))) / heightInv) + (Vector3Down(xyz, true) * -offsetUp) + transform.position, RotationAtPosition(xyz, true));
        }

        /// <summary>
        /// Instantiates object on this planet.
        /// </summary>
        /// <param name="objToInst">the object to instantiate
        /// </param>
        /// <param name="pos">position, cartesian coordinates, x, y and z, ranging from -1 to 1, relative to planet
        /// </param>
        public GameObject InstantiateOnPlanet(GameObject objToInst, Vector3 pos, float offsetUp = 0f)
        {
            pos = pos * radius;
            return Instantiate(objToInst, pos * ((heightInv + heightProvider.HeightAtXYZ(Quaternion.Inverse(transform.rotation) * (pos / radius))) / heightInv) + (Vector3Down(pos, true) * -offsetUp) + transform.position, RotationAtPosition(pos, true));
        }

        /// <summary>
        /// Returns rotation to stand up straight at specified position
        /// </summary>
        /// <param name="pos">position
        /// </param>
        public Quaternion RotationAtPosition(Vector3 pos, bool posRelativeToPlanet = false)
        {
            return Quaternion.LookRotation(-Vector3Down(pos, posRelativeToPlanet)) * Quaternion.Euler(90f, 0f, 0f);
        }
        /// <summary>
        /// Returns up vector at specific position on the planet. 
        /// </summary>
        /// <param name="LatLon">position
        /// </param>
        public Quaternion RotationAtPosition(Vector2 LatLon)
        {
            Vector3 pos = MathFunctions.LatLonToXyz(LatLon, radius);

            return Quaternion.LookRotation(Vector3Down(pos, true)) * Quaternion.Euler(90f, 0f, 0f);
        }
        /// <summary>
        /// Returns a normalized Vector towards the planet center at position
        /// </summary>
        public Vector3 Vector3Down(Vector3 position, bool posRelativeToPlanet = false)
        {
            return posRelativeToPlanet ? -position.normalized : (transform.position - position).normalized;
        }


        /// <summary>
        /// Creates a copy of this planet in scaled space
        /// </summary>
        public void CreateScaledSpaceCopy()
        {
            if (!scaledSpaceCopy)
            {
                GameObject sphere = (GameObject)Resources.Load("scaledSpacePlanet");

                sphere = Instantiate(sphere, transform.position / scaledSpaceFactor, transform.rotation);

                Mesh mesh;
                if (Application.isPlaying)
                    mesh = sphere.GetComponent<MeshFilter>().mesh;
                else
                    mesh = Instantiate(sphere.GetComponent<MeshFilter>().sharedMesh);


                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                Vector2[] uvs = mesh.uv;

                float sphereRadius = radius / scaledSpaceFactor;

                for (int i = 0; i < vertices.Length; i++)
                {
                    float height = heightProvider.HeightAtXYZ(vertices[i]);
                    normals[i] = vertices[i].normalized;
                    uvs[i] = new Vector2(uvs[i].x - 0.5f, uvs[i].y);

                    vertices[i] *= (heightInv + height) / heightInv;
                    vertices[i] *= sphereRadius;

                }

                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                if (!Application.isPlaying)
                    sphere.GetComponent<MeshFilter>().mesh = mesh;
                sphere.GetComponent<Renderer>().material = scaledSpaceMaterial;
                sphere.name = transform.name + "_ScaledSpace";

                scaledSpaceCopy = sphere;
            }
        }
    }
}
