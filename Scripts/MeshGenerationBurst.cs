/*
Burst-Generation is not finished yet, leaving this in as it might be useful to someone.
You can import Burst and un-comment this script


#define USE_UNSAFE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace PlanetaryTerrain
{
    public class MeshGenerationBurst : MeshGenerator
    {
        public MeshGenerationBurst(Planet planet, Quad quad)
        {
            this.planet = planet;
            this.quad = quad;
        }

        public override bool isCompleted
        {
            get
            {
                return handle.IsCompleted;
            }
        }

        private NativeArray<float3> vertices, normals, meshOffset;
        private NativeArray<float2> uv;

        private NativeArray<float4> colors;
        private NativeArray<float2> uv4;

        public JobHandle handle;
        private MeshGenerationJob job;


        public override void StartGeneration()
        {
            int length = planet.quadArrays.extendedPlane.Length;
            vertices = new NativeArray<float3>(length, Allocator.Persistent);

            for (int i = 0; i < length; i++)
                vertices[i] = planet.quadArrays.extendedPlane[i];
            

            normals = new NativeArray<float3>(length, Allocator.Persistent);
            meshOffset = new NativeArray<float3>(1, Allocator.Persistent);
            uv = new NativeArray<float2>(planet.quadSize * planet.quadSize, Allocator.Persistent);
            colors = new NativeArray<float4>(length, Allocator.Persistent);
            uv4 = new NativeArray<float2>(length, Allocator.Persistent);

            int levelConstant = 0;
            double offsetX = 0, offsetY = 0;
            MeshGeneration.CalculateUVConstants(quad, ref levelConstant, ref offsetX, ref offsetY);

            job = new MeshGenerationBurst.MeshGenerationJob
            {
                vertices = this.vertices,
                normals = this.normals,
                meshOffset = this.meshOffset,
                uv4 = this.uv4,
                colors = this.colors,
                triangles = planet.quadArrays.trisNative,
                uv = this.uv,
                sideLength = planet.quadSize,
                scale = quad.scale,
                rotation = quad.rotation,
                trPosition = quad.trPosition,
                radius = planet.radius,
                heightInv = planet.heightInv,
                levelConstant = levelConstant,
                offsetX = offsetX,
                offsetY = offsetY,
                uvScale = planet.uvScale,
            };

            handle = job.Schedule();
            isRunning = true;
        }

        public unsafe override void ApplyToMesh(Mesh mesh)
        {
            handle.Complete();

            int length = planet.quadSize * planet.quadSize;
            var verticesV = new Vector3[length];
            var normalsV = new Vector3[length];
            var uvV = new Vector2[length];
            var uv4V = new Vector2[length];
            var colorsV = new Color[length];

#if USE_UNSAFE
            unsafe
            {
                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref verticesV[0]), vertices.GetUnsafePtr(), sizeof(Vector3) * length);
                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref normalsV[0]), normals.GetUnsafePtr(), sizeof(Vector3) * length);
                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref uvV[0]), uv.GetUnsafePtr(), sizeof(Vector2) * length);
                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref uv4V[0]), uv4.GetUnsafePtr(), sizeof(Vector2) * length);
                UnsafeUtility.MemCpy(UnsafeUtility.AddressOf(ref colorsV[0]), colors.GetUnsafePtr(), sizeof(Color) * length);
            }
#else
            for (int i = 0; i < length; i++)
            {
                verticesV[i] = vertices[i];
                normalsV[i] = normals[i];
                uvV[i] = uv[i];
                uv4V[i] = uv4[i];
                colorV[i] = color[i];
            }
#endif

            quad.meshOffset = meshOffset[0];

            mesh.vertices = verticesV;
            mesh.normals = normalsV;
            mesh.uv = uvV;
            mesh.uv4 = uv4V;
            mesh.colors = colorsV;
        }

        public override void Dispose()
        {
            handle.Complete();

            vertices.Dispose();
            normals.Dispose();
            meshOffset.Dispose();
            uv.Dispose();

            uv4.Dispose();
            colors.Dispose();
        }

        [BurstCompile]
        internal struct MeshGenerationJob : IJob
        {
            public NativeArray<float3> vertices;
            public NativeArray<float3> normals;
            public NativeArray<float3> meshOffset;

            public NativeArray<float4> colors;
            public NativeArray<float2> uv4;

            [ReadOnly] public NativeArray<int> triangles;

            [WriteOnly] public NativeArray<float2> uv;


            public int sideLength;

            public float scale;
            public quaternion rotation;
            public float3 trPosition;
            public float radius;
            public float heightInv;

            public int levelConstant;
            public double offsetX, offsetY;
            public float uvScale;

            public void Execute()
            {
                int sideLength_1 = sideLength - 1;
                int numVertices = sideLength * sideLength;

                float height = 0f;

                float3 normalized;
                float3 down;

                for (int i = 0; i < numVertices; i++)
                {
                    vertices[i] = GetPosition(vertices[i], scale, rotation, trPosition, radius, heightInv, out height, out normalized);

                    if (i == 0)
                    {
                        meshOffset[0] = vertices[0]; //Using first calculated vertex as mesh origin
                        down = normalize(-meshOffset[0]); //First vertex to calculate down vector
                    }

                    vertices[i] -= meshOffset[0];

                    CalculateUVCube(ref uv, i, sideLength, sideLength_1, uvScale, levelConstant, offsetX, offsetY);
                    CalculateUVQuad(ref uv, i, sideLength, sideLength_1, levelConstant);

                    colors[i] = new float4(0, 1, 0, 0);
                    uv4[i] = new float2(0, 0);
                }
                for (int i = numVertices; i < vertices.Length; i++)
                {
                    vertices[i] = GetPosition(vertices[i], scale, rotation, trPosition, radius, heightInv);
                    vertices[i] -= meshOffset[0];
                }


                // Normals
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    float3 p1 = vertices[triangles[i]];
                    float3 p2 = vertices[triangles[i + 1]];
                    float3 p3 = vertices[triangles[i + 2]];

                    float3 l1 = p2 - p1;
                    float3 l2 = p3 - p1;

                    float3 normal = cross(l1, l2);

                    int n = triangles[i];
                    if (n < numVertices)
                        normals[n] += normal;

                    n = triangles[i + 1];
                    if (n < numVertices)
                        normals[n] += normal;

                    n = triangles[i + 2];
                    if (n < numVertices)
                        normals[n] += normal;
                }

                for (int i = 0; i < numVertices; i++)
                {
                    normals[i] = normalize(normals[i]);
                }

            }

            private static float3 GetPosition(float3 vertex, float scale, quaternion rotation, float3 trPosition, float radius, float heightInv, out float height, out float3 normalized)
            {
                vertex = vertex * scale; //Scaling down to subdivision level
                vertex = rotate(rotation, vertex);//rotation * vertex; //Rotating so the vertices are on the unit cube. Planes that are fed into this function all face up.

                vertex += trPosition;

                vertex = normalize(vertex);//Normalizing the vertices. The cube now is a sphere.

                normalized = vertex;
                height = HeightAtXYZ(vertex); //Getting height at vertex position
                vertex *= radius; //Scaling up the sphere
                vertex -= trPosition; //Subtracting trPosition, center is now (0, 0, 0)
                vertex *= (heightInv + height) / heightInv; //Offsetting vertex from center based on height and inverse heightScale

                return vertex;
            }

            private static float3 GetPosition(float3 vertex, float scale, quaternion rotation, float3 trPosition, float radius, float heightInv)
            {
                vertex = vertex * scale; //Scaling down to subdivision level
                vertex = rotate(rotation, vertex);//rotation * vertex; //Rotating so the vertices are on the unit cube. Planes that are fed into this function all face up.

                vertex += trPosition;

                vertex = normalize(vertex); //Normalizing the vertices. The cube now is a sphere.
                float height = HeightAtXYZ(vertex);
                vertex *= radius; //Scaling up the sphere
                vertex -= trPosition; //Subtracting trPosition, center is now (0, 0, 0)
                vertex *= (heightInv + height) / heightInv; //Offsetting vertex from center based on height and inverse heightScale

                return vertex;
            }

            private static float HeightAtXYZ(float3 pos)
            {
                const float lacunarity = 2f;
                const float gain = 0.5f;

                float sum = 0;
                float ampl = 1;

                for (int i = 0; i < 20; i++)
                {
                    sum += snoise(pos) * ampl;

                    pos *= lacunarity;
                    ampl *= gain;

                }

                return sum;
            }

            private static float3 mod289(float3 x)
            {
                return x - floor(x / 289.0f) * 289.0f;
            }

            private static float4 mod289(float4 x)
            {
                return x - floor(x / 289.0f) * 289.0f;
            }

            private static float4 permute(float4 x)
            {
                return mod289((x * 34.0f + 1.0f) * x);
            }

            private static float4 taylorInvSqrt(float4 r)
            {
                return 1.79284291400159f - r * 0.85373472095314f;
            }

            private static float snoise(float3 v)
            {
                float2 C = float2(1.0f / 6.0f, 1.0f / 3.0f);

                // First corner
                float3 i = floor(v + dot(v, C.yyy));
                float3 x0 = v - i + dot(i, C.xxx);

                // Other corners
                float3 g = step(x0.yzx, x0.xyz);
                float3 l = 1.0f - g;
                float3 i1 = min(g.xyz, l.zxy);
                float3 i2 = max(g.xyz, l.zxy);

                // x1 = x0 - i1  + 1.0 * C.xxx;
                // x2 = x0 - i2  + 2.0 * C.xxx;
                // x3 = x0 - 1.0 + 3.0 * C.xxx;
                float3 x1 = x0 - i1 + C.xxx;
                float3 x2 = x0 - i2 + C.yyy;
                float3 x3 = x0 - 0.5f;

                // Permutations
                i = mod289(i); // Avoid truncation effects in permutation
                float4 p =
                  permute(permute(permute(i.z + float4(0.0f, i1.z, i2.z, 1.0f))
                                        + i.y + float4(0.0f, i1.y, i2.y, 1.0f))
                                        + i.x + float4(0.0f, i1.x, i2.x, 1.0f));

                // Gradients: 7x7 points over a square, mapped onto an octahedron.
                // The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
                float4 j = p - 49.0f * floor(p / 49.0f);  // mod(p,7*7)

                float4 x_ = floor(j / 7.0f);
                float4 y_ = floor(j - 7.0f * x_);  // mod(j,N)

                float4 x = (x_ * 2.0f + 0.5f) / 7.0f - 1.0f;
                float4 y = (y_ * 2.0f + 0.5f) / 7.0f - 1.0f;

                float4 h = 1.0f - abs(x) - abs(y);

                float4 b0 = float4(x.xy, y.xy);
                float4 b1 = float4(x.zw, y.zw);

                //float4 s0 = float4(lessThan(b0, 0.0)) * 2.0 - 1.0;
                //float4 s1 = float4(lessThan(b1, 0.0)) * 2.0 - 1.0;
                float4 s0 = floor(b0) * 2.0f + 1.0f;
                float4 s1 = floor(b1) * 2.0f + 1.0f;
                float4 sh = -step(h, 0.0f);

                float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
                float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

                float3 g0 = float3(a0.xy, h.x);
                float3 g1 = float3(a0.zw, h.y);
                float3 g2 = float3(a1.xy, h.z);
                float3 g3 = float3(a1.zw, h.w);

                // Normalise gradients
                float4 norm = taylorInvSqrt(float4(dot(g0, g0), dot(g1, g1), dot(g2, g2), dot(g3, g3)));
                g0 *= norm.x;
                g1 *= norm.y;
                g2 *= norm.z;
                g3 *= norm.w;

                // Mix final noise value
                float4 m = max(0.6f - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0f);
                m = m * m;
                m = m * m;

                float4 px = float4(dot(x0, g0), dot(x1, g1), dot(x2, g2), dot(x3, g3));
                return 42.0f * dot(m, px);
            }

            /// <summary>
            /// Calculates the uv of a vertex with index i.
            /// </summary>
            private static void CalculateUVCube(ref NativeArray<float2> uv, int i, int sideLength, int sideLength_1, float uvScale, int levelConstant, double offsetX, double offsetY)
            {
                double x = (i / sideLength) / (double)sideLength_1;
                double y = (i % sideLength) / (double)sideLength_1;

                double scale = (double)uvScale / levelConstant;

                x *= scale;
                y *= scale;

                x += offsetX;
                y += offsetY;

                y *= -1;

                //RotateUV(quad, ref x, ref y);
                uv[i] = float2((float)x, (float)y);
            }

            /// <summary>
            /// Calculates the uv of a vertex with index i.
            /// </summary>
            private static void CalculateUVQuad(ref NativeArray<float2> uv, int i, int sideLength, int sideLength_1, int levelConstant)
            {
                double x = (i / sideLength) / (float)sideLength_1;
                double y = (i % sideLength) / (float)sideLength_1;

                x *= levelConstant;
                y *= levelConstant;

                y = -y;

                //RotateUV(quad, ref x, ref y);
                uv[i] = float2((float)x, (float)y);
            }
        }
    }
}
//*/
