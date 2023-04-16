using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;
using PlanetaryTerrain.Foliage;
using PlanetaryTerrain.DoubleMath;


namespace PlanetaryTerrain.Foliage
{
    public class FoliageExperimental : MonoBehaviour
    {
        public Planet planet;

        public int maxPointsPerFrame = 1000;
        public float size = 1500f;
        public bool useRaycast;

        private float radiusSqr;

        private SurfacePoints grass, detailMeshes, detailPrefabs;
        private Mesh pointCloud;
        private Matrix4x4[] matrices;
        private Transform[] transforms;

        private Vector3 lastPosition;
        private Vector3 lastPlanetPosition;
        private QuaternionD lastPlanetRotation;
        private Vector3 avgPos;

        private bool generateGrass, generateMeshes, generatePrefabs;

        private bool initialized = false;

        private int quadSize;
        private int quadSize_1;

        public struct SurfacePoints
        {
            public Vector3[] points;
            public Vector3[] normals;
            public int currentlyUsedPoints;
            public int numberOfPoints;

            public SurfacePoints(int numberOfPoints)
            {
                this.numberOfPoints = numberOfPoints;
                currentlyUsedPoints = 0;

                points = new Vector3[numberOfPoints];
                normals = new Vector3[numberOfPoints];
            }
        }

        void Awake()
        {
            radiusSqr = 2 * size * size;
        }

        void Initialize()
        {
            generateGrass = planet.expGrass;
            generateMeshes = planet.expMeshes;
            generatePrefabs = planet.expPrefabs;

            quadSize = planet.quadSize;
            quadSize_1 = quadSize - 1;

            if (generateGrass && planet.grassPerQuad > 0)
            {
                //generateGrass = true;
                grass = new SurfacePoints(planet.grassPerQuad);

                pointCloud = new Mesh();
                int[] indicies = new int[planet.grassPerQuad];

                for (int i = 0; i < indicies.Length; i++)
                    indicies[i] = i;

                pointCloud.vertices = grass.points;
                pointCloud.SetIndices(indicies, MeshTopology.Points, 0);
                pointCloud.normals = grass.normals;
            } else generateGrass = false;


            int numberDetailMeshes = 0;
            for (int i = 0; i < planet.detailMeshes.Count; i++)
            {
                numberDetailMeshes += planet.detailMeshes[i].number;
            }
            if (generateMeshes && numberDetailMeshes > 0)
            {
                //generateMeshes = true;
                detailMeshes = new SurfacePoints(numberDetailMeshes);
                matrices = new Matrix4x4[numberDetailMeshes];
            } else generateMeshes = false;

            int numberDetailPrefabs = 0;
            for (int i = 0; i < planet.detailPrefabs.Count; i++)
            {
                numberDetailPrefabs += planet.detailPrefabs[i].number;
            }
            if (generatePrefabs && numberDetailPrefabs > 0)
            {
                detailPrefabs = new SurfacePoints(numberDetailPrefabs);
                transforms = new Transform[numberDetailPrefabs];
            } else generatePrefabs = false;

            int offset = 0;
            for (int i = 0; i < planet.detailPrefabs.Count; i++)
            {
                for (int j = 0; j < planet.detailPrefabs[i].number; j++)
                {
                    transforms[j + offset] = Instantiate(planet.detailPrefabs[i].prefab, Vector3.zero, Quaternion.identity).transform;
                }

                offset += planet.detailPrefabs[i].number;
            }
            initialized = true;

        }

        void Reset()
        {
            grass.points = null;
            grass.normals = null;

            detailMeshes.points = null;
            detailMeshes.normals = null;

            detailPrefabs.points = null;
            detailPrefabs.normals = null;

            matrices = null;

            if (transforms != null)
                for (int i = 0; i < transforms.Length; i++)
                {
                    Destroy(transforms[i].gameObject);
                }

            transforms = null;

            initialized = false;
        }

        void UpdateMeshes()
        {
            int offset = 0;
            Quaternion rotation = Quaternion.LookRotation((planet.transform.position - detailMeshes.points[0]).normalized) * Quaternion.Euler(90f, 0, 0);
            for (int i = 0; i < planet.detailMeshes.Count; i++)
            {
                for (int j = 0; j < planet.detailMeshes[i].number; j++)
                {
                    if (detailMeshes.points[j + offset] != Vector3.zero)
                        matrices[j + offset] = Matrix4x4.TRS(detailMeshes.points[j + offset] + planet.detailMeshes[i].meshOffsetUp * detailMeshes.normals[i], rotation, planet.detailMeshes[i].meshScale);
                }

                offset += planet.detailMeshes[i].number;
            }
        }

        void RenderMeshes()
        {
            int offset = 0;

            for (int i = 0; i < planet.detailMeshes.Count; i++)
            {
                if (planet.detailMeshes[i].useGPUInstancing)
                    for (int j = 0; j < planet.detailMeshes[i].number; j++)
                    {
                        if (detailMeshes.points[j + offset] != Vector3.zero)
                            Graphics.DrawMesh(planet.detailMeshes[i].mesh, matrices[j + offset], planet.detailMeshes[i].material, 0);
                    }
                else
                {
                    List<Matrix4x4> toRender = new List<Matrix4x4>();
                    for (int j = 0; j < planet.detailMeshes[i].number; j++)
                        if (detailMeshes.points[j + offset] != Vector3.zero)
                            toRender.Add(matrices[j + offset]);

                    Graphics.DrawMeshInstanced(planet.detailMeshes[i].mesh, 0, planet.detailMeshes[i].material, toRender.ToArray());
                }
                offset += planet.detailMeshes[i].number;
            }
        }

        void UpdateTransforms()
        {
            int offset = 0;

            Quaternion rotation = Quaternion.LookRotation((planet.transform.position - detailPrefabs.points[0]).normalized) * Quaternion.Euler(90f, 0, 0);
            for (int i = 0; i < planet.detailPrefabs.Count; i++)
            {
                for (int j = 0; j < planet.detailPrefabs[i].number; j++)
                {
                    if (detailPrefabs.points[j + offset] != Vector3.zero)
                    {
                        transforms[j + offset].position = detailPrefabs.points[j + offset] + planet.detailPrefabs[i].meshOffsetUp * detailPrefabs.normals[i];
                        transforms[j + offset].rotation = rotation;
                        transforms[j + offset].gameObject.SetActive(true);
                    }
                    else transforms[j + offset].gameObject.SetActive(false);
                }

                offset += planet.detailPrefabs[i].number;
            }
        }


        void Update()
        {
            Vector3 trPosition = transform.position;
            Vector3 down = (planet.transform.position - trPosition).normalized;
            RaycastHit hit;
            if (!Physics.Raycast(trPosition - 100 * down, down, out hit, 3000f))
            {
                if (initialized)
                    Reset();
                return;
            }
            else if (hit.transform.tag == "Quad")
            {
                Biome biome = planet.FindQuad(ulong.Parse(hit.transform.name.Substring(5))).biome;
                if ((biome & planet.foliageBiomes) == Biome.None && (planet.foliageBiomes & Biome.All) != Biome.All) return;

                if (!initialized)
                    Initialize();
            }

            if (lastPlanetPosition != planet.transform.position)
            {
                Vector3 deltaPos = planet.transform.position - lastPlanetPosition;
                lastPosition -= deltaPos;

                if (generateGrass)
                {
                    for (int i = 0; i < grass.points.Length; i++)
                    {
                        if (grass.points[i] != Vector3.zero)
                            grass.points[i] += deltaPos;
                    }
                    pointCloud.vertices = grass.points;
                    pointCloud.RecalculateBounds();
                }

                if (generateMeshes)
                {
                    for (int i = 0; i < detailMeshes.points.Length; i++)
                    {
                        if (detailMeshes.points[i] != Vector3.zero)
                            detailMeshes.points[i] += deltaPos;
                    }
                    UpdateMeshes();
                }

                if (generatePrefabs)
                {
                    for (int i = 0; i < detailPrefabs.points.Length; i++)
                    {
                        if (detailPrefabs.points[i] != Vector3.zero)
                            detailPrefabs.points[i] += deltaPos;
                    }
                    UpdateTransforms();
                }

                lastPlanetPosition = planet.transform.position;

            }

            if (lastPlanetRotation.x != planet.rotation.x || lastPlanetRotation.y != planet.rotation.y || lastPlanetRotation.z != planet.rotation.z || lastPlanetRotation.w != planet.rotation.w)
            {
                QuaternionD rotate = planet.rotation * QuaternionD.Inverse(lastPlanetRotation);

                if (generateGrass)
                {
                    for (int i = 0; i < grass.points.Length; i++)
                    {
                        if (grass.points[i] != Vector3.zero)
                            grass.points[i] = (Vector3)MathFunctions.RotateAroundPoint(grass.points[i].ToVector3d(), planet.transform.position.ToVector3d(), rotate);
                    }
                    pointCloud.vertices = grass.points;
                    pointCloud.RecalculateBounds();
                }

                if (generateMeshes)
                {
                    for (int i = 0; i < detailMeshes.points.Length; i++)
                    {
                        if (detailMeshes.points[i] != Vector3.zero)
                            detailMeshes.points[i] = (Vector3)MathFunctions.RotateAroundPoint(detailMeshes.points[i].ToVector3d(), planet.transform.position.ToVector3d(), rotate);
                    }
                    UpdateMeshes();
                }

                if (generatePrefabs)
                {
                    for (int i = 0; i < detailPrefabs.points.Length; i++)
                    {
                        if (detailPrefabs.points[i] != Vector3.zero)
                            detailPrefabs.points[i] = (Vector3)MathFunctions.RotateAroundPoint(detailPrefabs.points[i].ToVector3d(), planet.transform.position.ToVector3d(), rotate);
                    }
                    UpdateTransforms();
                }

                lastPlanetRotation = planet.rotation;
            }


            if ((trPosition - lastPosition).sqrMagnitude > 2500 && (!generateGrass || grass.currentlyUsedPoints >= grass.numberOfPoints) && (!generateMeshes || detailMeshes.currentlyUsedPoints >= detailMeshes.numberOfPoints) && (!generatePrefabs || detailPrefabs.currentlyUsedPoints >= detailPrefabs.numberOfPoints))
            {
                lastPosition = trPosition;

                if (generateGrass)
                    for (int i = 0; i < grass.points.Length; i++)
                    {
                        if ((trPosition - grass.points[i]).sqrMagnitude > radiusSqr)
                        {
                            grass.points[i] = Vector3.zero;
                            grass.currentlyUsedPoints--;
                        }
                    }


                if (generateMeshes)
                    for (int i = 0; i < detailMeshes.points.Length; i++)
                    {
                        if ((trPosition - detailMeshes.points[i]).sqrMagnitude > radiusSqr)
                        {
                            detailMeshes.points[i] = Vector3.zero;
                            detailMeshes.currentlyUsedPoints--;
                        }
                    }

                if (generatePrefabs)
                    for (int i = 0; i < detailPrefabs.points.Length; i++)
                    {
                        if ((trPosition - detailPrefabs.points[i]).sqrMagnitude > radiusSqr)
                        {
                            detailPrefabs.points[i] = Vector3.zero;
                            detailPrefabs.currentlyUsedPoints--;
                        }
                    }

                avgPos = Vector3.zero;
                int length = 0;

                if (generateGrass)
                {
                    length = Mathf.Min(1000, grass.numberOfPoints);
                    for (int i = 0; i < length; i++)
                    {
                        avgPos += grass.points[i] - trPosition;
                    }
                }
                else if (generateMeshes)
                {
                    length = Mathf.Min(1000, detailMeshes.numberOfPoints);
                    for (int i = 0; i < length; i++)
                    {
                        avgPos += detailMeshes.points[i] - trPosition;
                    }
                }
                else if (generatePrefabs)
                {
                    length = Mathf.Min(1000, detailPrefabs.numberOfPoints);
                    for (int i = 0; i < length; i++)
                    {
                        avgPos += detailPrefabs.points[i] - trPosition;
                    }
                }

                avgPos /= (float)length;
            }

            if ((generateGrass && grass.currentlyUsedPoints < grass.numberOfPoints) || (generateMeshes && detailMeshes.currentlyUsedPoints < detailMeshes.numberOfPoints) || (generatePrefabs && detailPrefabs.currentlyUsedPoints < detailPrefabs.numberOfPoints))
            {
                if (useRaycast)
                    GenerateFoliageRaycast(avgPos);
                else
                    GenerateFoliage(avgPos);

                if (generateGrass)
                {
                    pointCloud.vertices = grass.points;
                    pointCloud.normals = grass.normals;
                    pointCloud.RecalculateBounds();
                }

                if (generateMeshes)
                    UpdateMeshes();

                if (generatePrefabs)
                    UpdateTransforms();
            }

            if (generateGrass)
                Graphics.DrawMesh(pointCloud, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), planet.grassMaterial, 0);

            if (generateMeshes)
                RenderMeshes();
        }


        void GenerateFoliage(Vector3 avgPos)
        {
            Vector3 trPosition = transform.position;
            Vector3 down = (planet.transform.position - trPosition).normalized;

            float largest = Mathf.Abs(down.x);
            int index = 0;

            if (Mathf.Abs(down.y) > largest)
            {
                index = 1;
                largest = down.y;
            }

            if (Mathf.Abs(down.z) > largest)
            {
                index = 2;
                largest = down.y;
            }
            Vector3 a, b;
            switch (index)
            {
                default: //x
                    a = new Vector3(-down.y / down.x, 1, 0).normalized;
                    b = Vector3.Cross(a, down);
                    break;
                case 1: //y
                    a = new Vector3(1, -down.x / down.y, 0).normalized;
                    b = Vector3.Cross(a, down);
                    break;
                case 2: //z
                    if (down.z > 0)
                    {
                        b = new Vector3(1, 0, -down.x / down.z).normalized;
                        a = -Vector3.Cross(b, down);
                    }
                    else
                    {
                        b = -new Vector3(1, 0, -down.x / down.z).normalized;
                        a = Vector3.Cross(-b, down);
                    }
                    break;
            }

            //Transformation matrix to local space on planet surface where x is left/right, y is up/planet normal and z is forward. The y component can be dropped later to reduce the problem to 2d
            Matrix4x4 mat = new Matrix4x4(
                new Vector4(a.x, down.x, b.x, 0),
                new Vector4(a.y, down.y, b.y, 0),
                new Vector4(a.z, down.z, b.z, 0),
                new Vector4(0, 0, 0, 1));

            avgPos = mat.MultiplyPoint3x4(avgPos);
            avgPos.y = 0;
            avgPos = -mat.inverse.MultiplyPoint3x4(avgPos);

            float mag = avgPos.magnitude;

            avgPos /= mag;

            mag /= size;

            Vector3 ray0 = a * -size + b * -size;
            Vector3 ray1 = a * -size + b * size;
            Vector3 ray2 = a * size + b * size;
            Vector3 ray3 = a * size + b * -size;

            float hsize = size / 2f;

            if (mag > 0.2)
            {
                Vector3 perpendicular = Vector3.Cross(avgPos, down);

                ray0 = hsize * avgPos + size * perpendicular;
                ray1 = size * avgPos + size * perpendicular;
                ray2 = size * avgPos - size * perpendicular;
                ray3 = hsize * avgPos - size * perpendicular;
            }

            RaycastHit hit0, hit1, hit2, hit3;
            List<Quad> hitQuads = new List<Quad>();

            if (Physics.Raycast(trPosition + ray0 + down * -500, down, out hit0) &&
                Physics.Raycast(trPosition + ray1 + down * -500, down, out hit1) &&
                Physics.Raycast(trPosition + ray2 + down * -500, down, out hit2) &&
                Physics.Raycast(trPosition + ray3 + down * -500, down, out hit3))
            {
                if (hit0.transform.tag == "Quad")
                {
                    hitQuads.Add(planet.FindQuad(ulong.Parse(hit0.transform.name.Substring(5))));
                }

                Quad q = planet.FindQuad(ulong.Parse(hit1.transform.name.Substring(5)));
                if (hit1.transform.tag == "Quad" && !hitQuads.Contains(q))
                {
                    hitQuads.Add(q);
                }

                q = planet.FindQuad(ulong.Parse(hit2.transform.name.Substring(5)));
                if (hit2.transform.tag == "Quad" && !hitQuads.Contains(q))
                {
                    hitQuads.Add(q);
                }

                q = planet.FindQuad(ulong.Parse(hit3.transform.name.Substring(5)));
                if (hit3.transform.tag == "Quad" && !hitQuads.Contains(q))
                {
                    hitQuads.Add(q);
                }

                Vector3 hitPoint0 = mat.MultiplyPoint3x4(hit0.point);
                Vector3 hitPoint1 = mat.MultiplyPoint3x4(hit1.point);
                Vector3 hitPoint2 = mat.MultiplyPoint3x4(hit2.point);
                Vector3 hitPoint3 = mat.MultiplyPoint3x4(hit3.point);

                Vector2Int[] lowerLimits = new Vector2Int[hitQuads.Count];
                Vector2Int[] upperLimits = new Vector2Int[hitQuads.Count];

                List<int>[] potTriangles = new List<int>[hitQuads.Count];

                for (int i = 0; i < hitQuads.Count; i++)
                {
                    potTriangles[i] = new List<int>();

                    Vector3 quadA = mat.MultiplyPoint3x4(hitQuads[i].renderedQuad.transform.TransformPoint(hitQuads[i].mesh.vertices[0]));
                    Vector3 quadB = mat.MultiplyPoint3x4(hitQuads[i].renderedQuad.transform.TransformPoint(hitQuads[i].mesh.vertices[quadSize * quadSize_1]));
                    Vector3 quadD = mat.MultiplyPoint3x4(hitQuads[i].renderedQuad.transform.TransformPoint(hitQuads[i].mesh.vertices[quadSize_1]));

                    Vector2 quadOrigin = new Vector2(quadA.x, quadA.z);
                    Vector2 quadI = new Vector2(quadB.x - quadA.x, quadB.z - quadA.z);
                    Vector2 quadJ = new Vector2(quadD.x - quadA.x, quadD.z - quadA.z);
                    Matrix2x2 toQuad = new Matrix2x2(quadI, quadJ).inverse;

                    Vector2 qHit0 = new Vector2(hitPoint0.x, hitPoint0.z) - quadOrigin;
                    Vector2 qHit1 = new Vector2(hitPoint1.x, hitPoint1.z) - quadOrigin;
                    Vector2 qHit2 = new Vector2(hitPoint2.x, hitPoint2.z) - quadOrigin;
                    Vector2 qHit3 = new Vector2(hitPoint3.x, hitPoint3.z) - quadOrigin;

                    qHit0 = toQuad * qHit0;
                    qHit1 = toQuad * qHit1;
                    qHit2 = toQuad * qHit2;
                    qHit3 = toQuad * qHit3;

                    Vector2 dirLeastConc = (toQuad * new Vector2(-avgPos.x, -avgPos.z)).normalized;

                    Vector2[] aaRect;
                    if (Mathf.Abs(qHit0.x - qHit2.x) * Mathf.Abs(qHit3.y - qHit1.y) > Mathf.Abs(qHit0.y - qHit2.y) * Mathf.Abs(qHit3.x - qHit1.x))
                    {
                        //axis-aligned with quad basis axes
                        aaRect = new Vector2[] { new Vector2(qHit0.x, qHit3.y),
                                                        new Vector2(qHit2.x, qHit3.y),
                                                        new Vector2(qHit2.x, qHit1.y),
                                                        new Vector2(qHit0.x, qHit1.y) };
                    }
                    else
                    {

                        //axis-aligned with quad basis axes
                        aaRect = new Vector2[] { new Vector2(qHit3.x, qHit0.y),
                                                        new Vector2(qHit1.x, qHit0.y),
                                                        new Vector2(qHit1.x, qHit2.y),
                                                        new Vector2(qHit3.x, qHit2.y) };
                    }

                    float compare = float.PositiveInfinity;
                    int indexLowest = 0;

                    //Finding point with smallest and largest distance from quad-coordinates origin
                    for (int j = 0; j < aaRect.Length; j++)
                    {
                        float temp = aaRect[j].sqrMagnitude;
                        if (temp < compare)
                        {
                            compare = temp;
                            indexLowest = j;
                        }
                    }

                    compare = float.NegativeInfinity;
                    int indexLargest = 0;

                    for (int j = 0; j < aaRect.Length; j++)
                    {
                        float temp = aaRect[j].sqrMagnitude;
                        if (temp > compare)
                        {
                            compare = temp;
                            indexLargest = j;
                        }
                    }

                    lowerLimits[i].x = Mathf.CeilToInt(aaRect[indexLowest].x * quadSize);
                    lowerLimits[i].y = Mathf.CeilToInt(aaRect[indexLowest].y * quadSize);

                    upperLimits[i].x = Mathf.FloorToInt(aaRect[indexLargest].x * quadSize);
                    upperLimits[i].y = Mathf.FloorToInt(aaRect[indexLargest].y * quadSize);

                    int[] triangles = hitQuads[i].mesh.triangles;



                    /* Debug.DrawLine(Vector3.zero, qHit0, Color.red);
                    Debug.DrawLine(new Vector2(0, 0), new Vector2(1, 0), Color.white);
                    Debug.DrawLine(new Vector2(1, 0), new Vector2(1, 1), Color.white);
                    Debug.DrawLine(new Vector2(1, 1), new Vector2(0, 1), Color.white);
                    Debug.DrawLine(new Vector2(0, 1), new Vector2(0, 0), Color.white);

                    Debug.DrawLine(qHit3, qHit0, Color.red);
                    Debug.DrawLine(qHit0, qHit1, Color.red);
                    Debug.DrawLine(qHit1, qHit2, Color.red);
                    Debug.DrawLine(qHit2, qHit3, Color.red);

                    Debug.DrawLine(aaRect[3], aaRect[0], Color.green);
                    Debug.DrawLine(aaRect[0], aaRect[1], Color.green);
                    Debug.DrawLine(aaRect[1], aaRect[2], Color.green);
                    Debug.DrawLine(aaRect[2], aaRect[3], Color.green);*/

                    //print("Rect: (" + lowerLimits[i] + ", " + lowerLimits[i].y + "), (" + upperLimits[i] + ", " + upperLimits[i] + ")");


                    for (int j = 0; j < triangles.Length; j += 3)
                    {
                        int f = triangles[j];
                        int g = triangles[j + 1];
                        int h = triangles[j + 2];

                        Vector2Int f_xy = new Vector2Int(f / quadSize, f % quadSize);
                        Vector2Int g_xy = new Vector2Int(g / quadSize, g % quadSize);
                        Vector2Int h_xy = new Vector2Int(h / quadSize, h % quadSize);


                        if (f_xy.x >= lowerLimits[i].x && f_xy.y >= lowerLimits[i].y && f_xy.x <= upperLimits[i].x && f_xy.y <= upperLimits[i].y &&
                            g_xy.x >= lowerLimits[i].x && g_xy.y >= lowerLimits[i].y && g_xy.x <= upperLimits[i].x && g_xy.y <= upperLimits[i].y &&
                            h_xy.x >= lowerLimits[i].x && h_xy.y >= lowerLimits[i].y && h_xy.x <= upperLimits[i].x && h_xy.y <= upperLimits[i].y)
                        {
                            //Debug.DrawLine((Vector2)f_xy / quadSize_1, (Vector2)g_xy / quadSize_1, Color.blue);
                            //Debug.DrawLine((Vector2)g_xy / quadSize_1, (Vector2)h_xy / quadSize_1, Color.blue);
                            //Debug.DrawLine((Vector2)h_xy / quadSize_1, (Vector2)f_xy / quadSize_1, Color.blue);

                            potTriangles[i].Add(f);
                            potTriangles[i].Add(g);
                            potTriangles[i].Add(h);
                        }

                    }

                }

                bool ret = true;
                for (int i = 0; i < potTriangles.Length; i++)
                    if (potTriangles[i].Count != 0)
                    {
                        ret = false;
                        break;
                    }
                if (ret) return;

                int length = lowerLimits.Length;
                int currentFramePoints = 0;
                int counter = 0;
                int counterReset = length - 1;

                Vector3[][] vertices = new Vector3[length][];
                for (int i = 0; i < length; i++)
                    vertices[i] = hitQuads[i].mesh.vertices;

                Vector3[][] normals = new Vector3[length][];
                for (int i = 0; i < length; i++)
                    normals[i] = hitQuads[i].mesh.normals;

                while (potTriangles[counter].Count == 0)
                {
                    counter++;
                    if (counter > counterReset) counter = 0;
                }

                if (generateGrass)
                {
                    for (int i = 0; i < grass.points.Length; i++)
                    {
                        if (grass.points[i] == Vector3.zero)
                        {
                            index = Random.Range(0, potTriangles[counter].Count);
                            index -= index % 3;

                            Vector3 triA = vertices[counter][potTriangles[counter][index]];
                            Vector3 triB = vertices[counter][potTriangles[counter][index + 1]];
                            Vector3 triC = vertices[counter][potTriangles[counter][index + 2]];

                            float x = Random.value;
                            float y = Random.value;

                            if (x + y >= 1)
                            {
                                x = 1 - x;
                                y = 1 - y;
                            }

                            grass.points[i] = hitQuads[counter].renderedQuad.transform.TransformPoint(triA + x * (triB - triA) + y * (triC - triA));
                            grass.normals[i] = hitQuads[counter].renderedQuad.transform.TransformDirection(normals[counter][potTriangles[counter][index]]);

                            do
                            {
                                counter++;
                                if (counter > counterReset) counter = 0;

                            } while (potTriangles[counter].Count == 0);

                            currentFramePoints++;
                            if (currentFramePoints > maxPointsPerFrame)
                                break;
                        }
                    }
                    grass.currentlyUsedPoints += currentFramePoints;
                    currentFramePoints = 0;
                }


                if (generateMeshes)
                {
                    for (int i = 0; i < detailMeshes.points.Length; i++)
                    {
                        if (detailMeshes.points[i] == Vector3.zero)
                        {
                            index = Random.Range(0, potTriangles[counter].Count);
                            index -= index % 3;

                            Vector3 triA = vertices[counter][potTriangles[counter][index]];
                            Vector3 triB = vertices[counter][potTriangles[counter][index + 1]];
                            Vector3 triC = vertices[counter][potTriangles[counter][index + 2]];

                            float x = Random.value;
                            float y = Random.value;

                            if (x + y >= 1)
                            {
                                x = 1 - x;
                                y = 1 - y;
                            }

                            detailMeshes.points[i] = hitQuads[counter].renderedQuad.transform.TransformPoint(triA + x * (triB - triA) + y * (triC - triA));
                            detailMeshes.normals[i] = hitQuads[counter].renderedQuad.transform.TransformDirection(normals[counter][potTriangles[counter][index]]);

                            do
                            {
                                counter++;
                                if (counter > counterReset) counter = 0;

                            } while (potTriangles[counter].Count == 0);

                            currentFramePoints++;
                            if (currentFramePoints > maxPointsPerFrame)
                                break;
                        }
                    }
                    detailMeshes.currentlyUsedPoints += currentFramePoints;
                    currentFramePoints = 0;
                }


                if (generatePrefabs)
                {
                    for (int i = 0; i < detailPrefabs.points.Length; i++)
                    {
                        if (detailPrefabs.points[i] == Vector3.zero)
                        {
                            index = Random.Range(0, potTriangles[counter].Count);
                            index -= index % 3;

                            Vector3 triA = vertices[counter][potTriangles[counter][index]];
                            Vector3 triB = vertices[counter][potTriangles[counter][index + 1]];
                            Vector3 triC = vertices[counter][potTriangles[counter][index + 2]];

                            float x = Random.value;
                            float y = Random.value;

                            if (x + y >= 1)
                            {
                                x = 1 - x;
                                y = 1 - y;
                            }

                            detailPrefabs.points[i] = hitQuads[counter].renderedQuad.transform.TransformPoint(triA + x * (triB - triA) + y * (triC - triA));
                            detailPrefabs.normals[i] = hitQuads[counter].renderedQuad.transform.TransformDirection(normals[counter][potTriangles[counter][index]]);

                            do
                            {
                                counter++;
                                if (counter > counterReset) counter = 0;

                            } while (potTriangles[counter].Count == 0);

                            currentFramePoints++;
                            if (currentFramePoints > maxPointsPerFrame)
                                break;
                        }
                    }
                    detailPrefabs.currentlyUsedPoints += currentFramePoints;
                    currentFramePoints = 0;
                }

            }
        }

        void GenerateFoliageRaycast(Vector3 avgPos)
        {

            Vector3 trPosition = transform.position;
            Vector3 down = (planet.transform.position - trPosition).normalized;

            float largest = Mathf.Abs(down.x);
            int index = 0;

            if (Mathf.Abs(down.y) > largest)
            {
                index = 1;
                largest = down.y;
            }

            if (Mathf.Abs(down.z) > largest)
            {
                index = 2;
                largest = down.y;
            }
            Vector3 a, b;
            switch (index)
            {
                default: //x
                    a = new Vector3(-down.y / down.x, 1, 0).normalized;
                    b = Vector3.Cross(a, down);
                    break;
                case 1: //y
                    a = new Vector3(1, -down.x / down.y, 0).normalized;
                    b = Vector3.Cross(a, down);
                    break;
                case 2: //z
                    if (down.z > 0)
                    {
                        b = new Vector3(1, 0, -down.x / down.z).normalized;
                        a = -Vector3.Cross(b, down);
                    }
                    else
                    {
                        b = -new Vector3(1, 0, -down.x / down.z).normalized;
                        a = Vector3.Cross(-b, down);
                    }
                    break;
            }


            Matrix4x4 mat = new Matrix4x4(
                new Vector4(a.x, down.x, b.x, 0),
                new Vector4(a.y, down.y, b.y, 0),
                new Vector4(a.z, down.z, b.z, 0),
                new Vector4(0, 0, 0, 1));

            avgPos = mat.MultiplyPoint3x4(avgPos);
            avgPos.y = 0;
            avgPos = -mat.inverse.MultiplyPoint3x4(avgPos);

            float mag = avgPos.magnitude;
            avgPos /= mag;
            mag /= size;

            Vector3 perpendicular = Vector3.Cross(down, avgPos);

            int currentFramePoints = 0;

            if (generateGrass)
            {
                for (int i = 0; i < grass.points.Length; i++)
                {
                    if (grass.points[i] == Vector3.zero)
                    {
                        if (currentFramePoints > maxPointsPerFrame)
                            break;

                        Vector3 rayOrigin;

                        if (mag > 0.2f)
                            rayOrigin = transform.position + avgPos * Random.Range(size / 2, size) + perpendicular * Random.Range(-size, size) - 500 * down;
                        else
                            rayOrigin = transform.position + a * Random.Range(-size, size) + b * Random.Range(-size, size) - 500 * down;

                        RaycastHit _hit;
                        if (Physics.Raycast(rayOrigin, down, out _hit))
                        {
                            if (_hit.transform.tag == "Quad")
                            {
                                grass.points[i] = _hit.point;
                                grass.normals[i] = _hit.normal;
                                currentFramePoints++;
                            }
                        }
                    }
                }
                grass.currentlyUsedPoints += currentFramePoints;
                currentFramePoints = 0;
            }

            if (generateMeshes)
            {
                for (int i = 0; i < detailMeshes.points.Length; i++)
                {
                    if (detailMeshes.points[i] == Vector3.zero)
                    {
                        if (currentFramePoints > maxPointsPerFrame)
                            break;

                        Vector3 rayOrigin;

                        if (mag > 0.2f)
                            rayOrigin = transform.position + avgPos * Random.Range(size / 2, size) + perpendicular * Random.Range(-size, size) - 500 * down;
                        else
                            rayOrigin = transform.position + a * Random.Range(-size, size) + b * Random.Range(-size, size) - 500 * down;

                        RaycastHit _hit;
                        if (Physics.Raycast(rayOrigin, down, out _hit))
                        {
                            if (_hit.transform.tag == "Quad")
                            {
                                detailMeshes.points[i] = _hit.point;
                                detailMeshes.normals[i] = _hit.normal;
                                currentFramePoints++;
                            }
                        }
                    }
                }
                detailMeshes.currentlyUsedPoints += currentFramePoints;
                currentFramePoints = 0;
            }

            if (generatePrefabs)
            {
                for (int i = 0; i < detailPrefabs.points.Length; i++)
                {
                    if (detailPrefabs.points[i] == Vector3.zero)
                    {
                        if (currentFramePoints > maxPointsPerFrame)
                            break;

                        Vector3 rayOrigin;

                        if (mag > 0.2f)
                            rayOrigin = transform.position + avgPos * Random.Range(size / 2, size) + perpendicular * Random.Range(-size, size) - 500 * down;
                        else
                            rayOrigin = transform.position + a * Random.Range(-size, size) + b * Random.Range(-size, size) - 500 * down;

                        RaycastHit _hit;
                        if (Physics.Raycast(rayOrigin, down, out _hit))
                        {
                            if (_hit.transform.tag == "Quad")
                            {
                                detailPrefabs.points[i] = _hit.point;
                                detailPrefabs.normals[i] = _hit.normal;
                                currentFramePoints++;
                            }
                        }
                    }
                }
                detailPrefabs.currentlyUsedPoints += currentFramePoints;
            }
        }
    }
}
