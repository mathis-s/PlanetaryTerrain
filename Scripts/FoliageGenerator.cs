using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain;

namespace PlanetaryTerrain.Foliage
{
    /// <summary>
    /// Randomly generates points on the surface of a quad. The points are used for foliage.
    /// </summary>
    public class FoliageGenerator
    {
        
        public List<Vector3> positions;
        public List<Vector3> normals;
        public List<int> indicies;
        public Vector3 position;
        public Mesh mesh;
        public Vector3 down;
        public Quaternion rotation;
        public Biome foliageBiomes;

        System.Random random;

        int[] meshTris;
        Vector3[] meshVertices;
        Vector3[] meshNormals;

        Color32[] meshColors;
        Vector2[] meshUv4;


        public void PointCloud(int number)
        {
            int numTris = meshTris.Length / 3;
            for (int i = 0; i < number; i++)
            {
                int randomTriangle = random.Next(0, numTris - 1);
                randomTriangle *= 3;

                if (foliageBiomes != Biome.All)
                {
                    Color32 col = meshColors[meshTris[randomTriangle]];
                    Vector2 uv = meshUv4[meshTris[randomTriangle]];

                    if(((int) foliageBiomes & 1) != 0 && col.r > 127) goto gen;
                    if(((int) foliageBiomes & 2) != 0 && col.g > 127) goto gen;
                    if(((int) foliageBiomes & 4) != 0 && col.b > 127) goto gen;
                    if(((int) foliageBiomes & 8) != 0 && col.a > 127) goto gen;

                    if(((int) foliageBiomes & 16) != 0 && uv.x > 0.5f) goto gen;
                    if(((int) foliageBiomes & 32) != 0 && uv.y > 0.5f) goto gen;

                    continue;
                }
gen:

                Vector3 a = meshVertices[meshTris[randomTriangle]];
                Vector3 b = meshVertices[meshTris[randomTriangle + 1]];
                Vector3 c = meshVertices[meshTris[randomTriangle + 2]];

                float x = random.Next() / 2147483646f; //returned number is _less_ than int.max, therefore division by int.max - 1
                float y = random.Next() / 2147483646f;

                if (x + y >= 1)
                {
                    x = 1 - x;
                    y = 1 - y;
                }

                Vector3 pointOnMesh = a + x * (b - a) + y * (c - a);

                indicies.Add(indicies.Count);
                positions.Add(pointOnMesh);
                normals.Add(meshNormals[meshTris[randomTriangle]]);
            }
        }

        public FoliageGenerator(Quad quad, Mesh mesh, Quaternion rotation, Vector3 down, Biome foliageBiomes, int seed)
        {
            this.rotation = rotation;
            this.down = down;
            this.mesh = mesh;
            this.foliageBiomes = foliageBiomes;

            positions = new List<Vector3>();
            indicies = new List<int>();
            normals = new List<Vector3>();

            meshTris = quad.planet.quadArrays.tris0000;
            meshVertices = mesh.vertices;
            meshNormals = mesh.normals;
            meshColors = mesh.colors32;
            meshUv4 = mesh.uv4;

            random = new System.Random(seed);
        }

        public PosRot[] Positions(int number, DetailObject d)
        {
            if (number > positions.Count)
                number = positions.Count;

            int i = positions.Count - number;
            var posrots = new PosRot[(positions.Count - i)];

            Vector3 rdown = rotation * down;
            Quaternion rot = Quaternion.LookRotation(down) * Quaternion.Euler(-90f, 0f, 0f);

            Vector3 pos = (rdown * -d.meshOffsetUp);
            int j = 0;
            for (; i < positions.Count; i++)
            {
                posrots[j] = new PosRot(positions[i] + pos, rot);
                j++;
            }

            int x = positions.Count - number;
            positions.RemoveRange(x, number);
            indicies.RemoveRange(x, number);
            normals.RemoveRange(x, number);
            return posrots;
        }

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(positions);
            mesh.SetIndices(indicies.ToArray(), MeshTopology.Points, 0);
            mesh.SetNormals(normals);

            positions.Clear();
            normals.Clear();
            indicies.Clear();

            return mesh;
        }

    }
    [System.Serializable]
    public class DetailObject
    {
        public int number;
        public float meshOffsetUp;
        public PosRot[] posRots;

    }
    [System.Serializable]
    public class DetailMesh : DetailObject
    {
        public Vector3 meshScale;
        public Mesh mesh;
        public Material material;
        public bool useGPUInstancing;
        public bool isGrass;

        public DetailMesh(Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;

            number = 100;
            meshOffsetUp = 2.5f;
            meshScale = Vector3.one;
            useGPUInstancing = false;
            isGrass = false;
            posRots = new PosRot[0];
        }
        public DetailMesh(DetailMesh d)
        {
            mesh = d.mesh;
            material = d.material;
            number = d.number;
            meshOffsetUp = d.meshOffsetUp;
            meshScale = d.meshScale;
            useGPUInstancing = d.useGPUInstancing;
            isGrass = d.isGrass;
            posRots = new PosRot[0];
        }
        public DetailMesh()
        {
            mesh = null;
            material = null;
            number = 100;
            meshOffsetUp = 2.5f;
            meshScale = Vector3.one;
            useGPUInstancing = false;
            isGrass = false;
            posRots = new PosRot[0];
        }
    }
    [System.Serializable]
    public class DetailPrefab : DetailObject
    {
        public GameObject prefab;

        public GameObject[] InstantiateObjects()
        {
            GameObject[] objects = new GameObject[posRots.Length];

            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = MonoBehaviour.Instantiate(prefab, posRots[i].position, posRots[i].rotation);
            }

            return objects;
        }

        public DetailPrefab(GameObject prefab)
        {
            this.prefab = prefab;
        }


        public DetailPrefab(DetailPrefab d)
        {
            this.number = d.number;
            this.meshOffsetUp = d.meshOffsetUp;
            this.prefab = d.prefab;
        }

        public DetailPrefab() { }

    }


    [System.Serializable]
    public struct PosRot
    {
        public Vector3 position;
        public Quaternion rotation;


        public PosRot(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }


}
