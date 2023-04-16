using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using UnityEngine.Rendering;

namespace PlanetaryTerrain
{
    public abstract class MeshGenerator
    {
        public abstract void StartGeneration();
        public abstract void ApplyToMesh(Mesh mesh);
        public virtual void Dispose() { }

        public abstract bool isCompleted
        {
            get;
        }

        public bool isRunning;

        protected Planet planet;
        protected Quad quad;
    }


    public class CPUMeshGenerator : MeshGenerator
    {
        private Func<Quad, MeshData, MeshData> method;
        private IAsyncResult cookie;

        public override bool isCompleted { get { return cookie != null && cookie.IsCompleted; } }

        public override void StartGeneration()
        {
            MeshData md = new MeshData(planet.quadArrays.GetExtendedPlane(), planet.quadSize * planet.quadSize);

            method = MeshGeneration.GenerateMesh;
            cookie = method.BeginInvoke(quad, md, null, null);

            isRunning = true;
        }

        public override void ApplyToMesh(Mesh mesh)
        {
            MeshData result = method.EndInvoke(cookie);
            isRunning = false;

            mesh.vertices = result.vertices;
            mesh.colors32 = result.colors;
            mesh.uv = result.uv;

            //if (planet.serializedInherited.textureProviderType != TextureProviderType.None)
            mesh.uv4 = result.uv2;

            mesh.normals = result.normals;

            cookie = null;
            method = null;
        }

        public CPUMeshGenerator(Planet planet, Quad quad)
        {
            this.planet = planet;
            this.quad = quad;
        }
    }

    public class GPUMeshGenerator : MeshGenerator
    {
        private Func<Quad, MeshData, MeshData> method;
        private IAsyncResult cookie;
        private AsyncGPUReadbackRequest gpuReadbackReq;
        private ComputeBuffer computeBuffer;
        private bool isRunningOnGPU;
        public override bool isCompleted
        {
            get
            {
                if (isRunningOnGPU && gpuReadbackReq.done)
                {
                    if (gpuReadbackReq.hasError)
                    {
                        computeBuffer.Dispose();
                        computeBuffer = null;
                        StartGeneration();
                    }
                    else
                    {
                        var a = gpuReadbackReq.GetData<Vector3>().ToArray();
                        MeshData md = new MeshData(a, planet.quadSize * planet.quadSize);

                        method = MeshGeneration.GenerateMeshGPU;
                        cookie = method.BeginInvoke(quad, md, null, null);
                        computeBuffer.Dispose();
                        computeBuffer = null;
                        isRunningOnGPU = false;
                    }
                }

                return cookie != null && cookie.IsCompleted;
            }
        }

        public override void StartGeneration()
        {
            MeshData md = new MeshData(planet.quadArrays.GetExtendedPlane(), planet.quadSize * planet.quadSize);
            
            int kernelIndex = planet.computeShader.FindKernel("ComputePositions");

            computeBuffer = new ComputeBuffer(md.vertices.Length, 12);
            computeBuffer.SetData(planet.quadArrays.GetExtendedPlane());

            planet.computeShader.SetFloat("scale", quad.scale);
            planet.computeShader.SetFloats("trPosition", new float[] { quad.trPosition.x, quad.trPosition.y, quad.trPosition.z });
            planet.computeShader.SetFloat("radius", planet.radius);
            planet.computeShader.SetFloats("rotation", new float[] { quad.rotation.x, quad.rotation.y, quad.rotation.z, quad.rotation.w });
            planet.computeShader.SetFloat("noiseDiv", 1f / planet.heightScale);
            planet.computeShader.SetBuffer(kernelIndex, "dataBuffer", computeBuffer);

            planet.computeShader.Dispatch(kernelIndex, Mathf.CeilToInt(md.vertices.Length / 256f), 1, 1);

            gpuReadbackReq = AsyncGPUReadback.Request(computeBuffer);
            isRunning = true;
            isRunningOnGPU = true;
        }

        public override void ApplyToMesh(Mesh mesh)
        {
            MeshData result = method.EndInvoke(cookie);
            isRunning = false;

            mesh.vertices = result.vertices;
            mesh.colors32 = result.colors;
            mesh.uv = result.uv;
            mesh.uv4 = result.uv2;
            mesh.normals = result.normals;

            cookie = null;
            method = null;
        }

        public override void Dispose()
        {
            if (computeBuffer != null)
                computeBuffer.Dispose();
        }

        public GPUMeshGenerator(Planet planet, Quad quad)
        {
            this.planet = planet;
            this.quad = quad;
        }
    }
}