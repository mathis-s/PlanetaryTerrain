using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain
{

    /// <summary>
    /// Just a thread-safe container class that stores the data of a mesh (vertices, normals, ...).
    /// </summary>
    internal class MeshData
    {
        public Vector3[] vertices;
        public Color32[] colors;
        public Vector3[] normals;
        public Vector2[] uv;
        public Vector2[] uv2;

        public MeshData(Vector3[] vertices, Color32[] colors, Vector3[] normals, Vector2[] uv, Vector2[] uv2)
        {
            this.vertices = vertices;
            this.colors = colors;
            this.normals = normals;
            this.uv = uv;
            this.uv2 = uv2;
        }

        public MeshData(Vector3[] vertices, Vector3[] normals, Vector2[] uv, int colorsLength)
        {
            this.vertices = vertices;
            this.normals = normals;
            this.uv = uv;
            this.colors = new Color32[colorsLength];
            this.uv2 = new Vector2[colorsLength];

        }

        public MeshData(Vector3[] vertices, int length)
        {
            this.vertices = vertices;
            this.normals = new Vector3[length];
            this.uv = uv = new Vector2[length];
            this.colors = new Color32[length];
            this.uv2 = new Vector2[length];
        }
    }

    internal static class MeshGeneration
    {

        /// <summary>
        /// Creates a MeshData that is later applied to this Quad's mesh.
        /// </summary>
        internal static MeshData GenerateMesh(Quad quad, MeshData md)
        {

            Planet planet = quad.planet;
            int sideLength = planet.quadSize;
            int sideLength_1 = sideLength - 1;
            int numVertices = sideLength * sideLength;

            Vector3[] finalVerts = new Vector3[numVertices];
            float height = 0f;
            float[] texture;
            Vector3 down = Vector3.zero;
            Vector3 normalized = Vector3.zero;

            float averageHeight = 0f;
            float[] heights = null;

            if (planet.calculateMsds)
                heights = new float[numVertices];

            double offsetX = 0, offsetY = 0;
            int levelConstant = 0;

            CalculateUVConstants(quad, ref levelConstant, ref offsetX, ref offsetY);

            md.uv = new Vector2[numVertices];
            for (int i = 0; i < numVertices; i++)
            {
                md.vertices[i] = GetPosition(quad, md.vertices[i], out height, out normalized, out md.uv[i], i);

                if (planet.calculateMsds)
                {
                    averageHeight += height;
                    heights[i] = height;
                }

                if (i == 0)
                {
                    quad.meshOffset = md.vertices[0]; //Using first calculated vertex as mesh origin
                    down = (Vector3.zero - quad.meshOffset).normalized; //First vertex to calculate down vector
                }

                md.vertices[i] -= quad.meshOffset;

                finalVerts[i] = md.vertices[i];

                // Non-Legacy UV calculations
                if (!planet.usingLegacyUVType)
                    if (planet.uvType == UVType.Cube)
                        CalculateUVCube(quad, ref md.uv[i], i, sideLength, sideLength_1, planet.uvScale, levelConstant, offsetX, offsetY);
                    else
                        CalculateUVQuad(quad, ref md.uv[i], i, sideLength, sideLength_1, levelConstant);

                texture = planet.textureProvider.EvaluateTexture(height, normalized);

                md.colors[i] = new Color(texture[0], texture[1], texture[2], texture[3]); //Using color and uv4 channels to encode biome/texture data
                md.uv2[i] = new Vector2(texture[4], texture[5]);

                for (int j = 0; j < texture.Length; j++)
                {
                    if (texture[j] > 0.5f) quad.biome = (Biome)(((int)quad.biome) | (1 << j));
                }

            }

            //Calculating position of edge vertices. They are only used for normal generation and discarded afterwards, so we can leave out everything (UV, Texture, ...) except for the actual position calculation.
            for (int i = numVertices; i < md.vertices.Length; i++)
            {
                md.vertices[i] = GetPosition(quad, md.vertices[i]);
                md.vertices[i] -= quad.meshOffset;
            }


            CalculateNormals(ref md.normals, ref md.vertices, planet.quadArrays.trisExtendedPlane, numVertices);
            md.vertices = finalVerts; //Discarding edge vertices

            SlopeTexture(planet, ref md, numVertices, down);

            if (planet.calculateMsds)
            {
                averageHeight /= numVertices;

                for (int i = 0; i < numVertices; i++)
                {
                    float deviation = (averageHeight - heights[i]);
                    quad.msd += deviation * deviation;
                }
            }
            return md;
        }

        /// <summary>
        /// Creates a MeshData that is later applied to this Quad's mesh.
        /// </summary>
        internal static MeshData GenerateMeshD(Quad quad, MeshData md)
        {

            Planet planet = quad.planet;
            int sideLength = planet.quadSize;
            int sideLength_1 = sideLength - 1;
            int numVertices = sideLength * sideLength;

            Vector3[] finalVerts = new Vector3[numVertices];
            float height = 0f;
            float[] texture;
            Vector3 down = Vector3.zero;
            Vector3d meshOffset = Vector3d.zero;

            float averageHeight = 0f;
            float[] heights = null;

            if (planet.calculateMsds)
                heights = new float[numVertices];

            double offsetX = 0, offsetY = 0;
            int levelConstant = 0;

            CalculateUVConstants(quad, ref levelConstant, ref offsetX, ref offsetY);

            md.uv = new Vector2[numVertices];

            for (int i = 0; i < numVertices; i++)
            {
                Vector3d normalized;
                Vector3d vert = GetPositionD(quad, new Vector3d(md.vertices[i]), out height, out normalized, out md.uv[i], i);

                if (planet.calculateMsds)
                {
                    averageHeight += height;
                    heights[i] = height;
                }

                if (i == 0)
                {
                    // Use zeroth vertex rounded to float and converted back to int as meshOffset
                    quad.meshOffset = (Vector3)vert;
                    meshOffset = new Vector3d(quad.meshOffset);
                    down = (Vector3)(Vector3d.zero - meshOffset).normalized;
                }
                // After this subtraction the magnitude is a lot smaller,
                // we can cast to float now.
                vert -= meshOffset;

                finalVerts[i] = (Vector3)vert;
                md.vertices[i] = (Vector3)vert;

                // Non-Legacy UV calculations
                if (!planet.usingLegacyUVType)
                    if (planet.uvType == UVType.Cube)
                        CalculateUVCube(quad, ref md.uv[i], i, sideLength, sideLength_1, planet.uvScale, levelConstant, offsetX, offsetY);
                    else
                        CalculateUVQuad(quad, ref md.uv[i], i, sideLength, sideLength_1, levelConstant);

                texture = planet.textureProvider.EvaluateTexture(height, (Vector3)normalized);

                md.colors[i] = new Color(texture[0], texture[1], texture[2], texture[3]); //Using color and uv4 channels to encode biome/texture data
                md.uv2[i] = new Vector2(texture[4], texture[5]);

                for (int j = 0; j < texture.Length; j++)
                {
                    if (texture[j] > 0.5f) quad.biome = (Biome)(((int)quad.biome) | (1 << j));
                }
            }

            //Calculating position of edge vertices. They are only used for normal generation and discarded afterwards, so we can leave out everything (UV, Texture, ...) except for the actual position calculation.
            for (int i = numVertices; i < md.vertices.Length; i++)
            {
                md.vertices[i] = (Vector3)(GetPositionD(quad, new Vector3d(md.vertices[i])) - meshOffset);
            }


            CalculateNormals(ref md.normals, ref md.vertices, planet.quadArrays.trisExtendedPlane, numVertices);
            md.vertices = finalVerts; //Discarding edge vertices

            SlopeTexture(planet, ref md, numVertices, down);

            if (planet.calculateMsds)
            {
                averageHeight /= numVertices;

                for (int i = 0; i < numVertices; i++)
                {
                    float deviation = (averageHeight - heights[i]);
                    quad.msd += deviation * deviation;
                }
            }
            return md;
        }

        internal static MeshData GenerateMeshGPU(Quad quad, MeshData md)
        {
            Planet planet = quad.planet;
            int sideLength = planet.quadSize;
            int sideLength_1 = sideLength - 1;
            int numVertices = sideLength * sideLength;

            Vector3[] finalVerts = new Vector3[numVertices];
            float height = 0f;
            float[] texture;
            Vector3 down = Vector3.zero;
            Vector3 normalized = Vector3.zero;

            float averageHeight = 0f;
            float[] heights = null;

            if (planet.calculateMsds)
                heights = new float[numVertices];

            double offsetX = 0, offsetY = 0;
            int levelConstant = 0;

            CalculateUVConstants(quad, ref levelConstant, ref offsetX, ref offsetY);

            quad.meshOffset = md.vertices[0]; //Using first calculated vertex as mesh origin
            down = (Vector3.zero - quad.meshOffset).normalized; //First vertex to calculate down vector

            md.uv = new Vector2[numVertices];
            for (int i = 0; i < numVertices; i++)
            {
                height = (md.vertices[i] + quad.trPosition).magnitude / planet.radius;
                height -= 1f;
                height *= planet.heightInv;

                normalized = md.vertices[i];
                normalized /= (planet.heightInv + height) / planet.heightInv;
                normalized += quad.trPosition;
                normalized /= planet.radius;
                normalized /= 1.005f;

                if (planet.calculateMsds)
                {
                    averageHeight += height;
                    heights[i] = height;
                }

                md.vertices[i] -= quad.meshOffset;
                finalVerts[i] = md.vertices[i];

                // Non-Legacy UV calculations
                if (!planet.usingLegacyUVType)
                {
                    if (planet.uvType == UVType.Cube)
                        CalculateUVCube(quad, ref md.uv[i], i, sideLength, sideLength_1, planet.uvScale, levelConstant, offsetX, offsetY);
                    else
                        CalculateUVQuad(quad, ref md.uv[i], i, sideLength, sideLength_1, levelConstant);
                }

                texture = planet.textureProvider.EvaluateTexture(height, normalized);

                md.colors[i] = new Color(texture[0], texture[1], texture[2], texture[3]); //Using color and uv4 channels to encode biome/texture data
                md.uv2[i] = new Vector2(texture[4], texture[5]);

                for (int j = 0; j < texture.Length; j++)
                {
                    if (texture[j] > 0.5f) quad.biome = (Biome)(((int)quad.biome) | (1 << j));
                }
            }

            for (int i = numVertices; i < md.vertices.Length; i++)
            {
                md.vertices[i] -= quad.meshOffset;
            }

            CalculateNormals(ref md.normals, ref md.vertices, planet.quadArrays.trisExtendedPlane, numVertices);
            md.vertices = finalVerts;

            SlopeTexture(planet, ref md, numVertices, down);

            if (planet.calculateMsds)
            {
                averageHeight /= numVertices;

                for (int i = 0; i < numVertices; i++)
                {
                    float deviation = (averageHeight - heights[i]);
                    quad.msd += deviation * deviation;
                }
            }

            return md;
        }

        /// <summary>
        /// Calculates normals for all vertices whose index is lower than numVertices. This is used to avoid
        /// </summary>

        internal static void CalculateNormals(ref Vector3[] normals, ref Vector3[] vertices, int[] tris, int numVertices)
        {
            int len = normals.Length;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 p1 = vertices[tris[i]];
                Vector3 p2 = vertices[tris[i + 1]];
                Vector3 p3 = vertices[tris[i + 2]];

                Vector3 l1 = p2 - p1;
                Vector3 l2 = p3 - p1;

                Vector3 normal = Vector3.Cross(l1, l2);

                int n = tris[i];
                if (n < len)
                    normals[n] += normal;

                n = tris[i + 1];
                if (n < len)
                    normals[n] += normal;

                n = tris[i + 2];
                if (n < len)
                    normals[n] += normal;
            }

            for (int i = 0; i < len; i++)
            {
                float length = (float)System.Math.Sqrt((double)normals[i].x * normals[i].x + (double)normals[i].y * normals[i].y + (double)normals[i].z * normals[i].z);
                normals[i] /= length;
            }
        }

        /// <summary>
        /// Position on unit cube to position on planet
        /// </summary>
        private static Vector3 GetPosition(Quad quad, Vector3 vertex, out float height, out Vector3 normalized, out Vector2 uv, int index)
        {
            Planet planet = quad.planet;

            vertex = vertex * quad.scale; //Scaling down to subdivision level
            vertex = quad.rotation * vertex; //Rotating so the vertices are on the unit cube. Planes that are fed into this function all face up.

            if (planet.usingLegacyUVType)
            {
                if (planet.uvType == UVType.LegacyContinuous)
                    vertex += quad.trPosition; //Offsetting the plane. Now all vertices form a cube

                switch (quad.plane)
                {
                    case QuadPlane.ZPlane:
                        uv = new Vector2(vertex.x, vertex.y);
                        break;
                    case QuadPlane.YPlane:
                        uv = new Vector2(vertex.x, vertex.z);
                        break;
                    case QuadPlane.XPlane:
                        uv = new Vector2(vertex.z, vertex.y);
                        break;
                    default:
                        uv = Vector2.zero;
                        break;
                }
                if (planet.uvType != UVType.LegacyContinuous)
                    vertex += quad.trPosition;
            }
            else
            {
                vertex += quad.trPosition;
                uv = Vector2.zero;
            }

            vertex.Normalize();//Normalizing the vertices. The cube now is a sphere.

            normalized = vertex;
            height = planet.heightProvider.HeightAtXYZ(vertex); //Getting height at vertex position
            vertex *= planet.radius; //Scaling up the sphere
            vertex *= (planet.heightInv + height) / planet.heightInv; //Offsetting vertex from center based on height and inverse heightScale
            return vertex;
        }

        /// <summary>
        /// Position on unit cube to position on planet
        /// </summary>
        private static Vector3d GetPositionD(Quad quad, Vector3d vertex, out float height, out Vector3d normalized, out Vector2 uv, int index)
        {
            Planet planet = quad.planet;

            vertex = vertex * (double)quad.scale; //Scaling down to subdivision level
            vertex = ((QuaternionD)quad.rotation) * vertex; //Rotating so the vertices are on the unit cube. Planes that are fed into this function all face up.

            if (planet.usingLegacyUVType)
            {
                if (planet.uvType == UVType.LegacyContinuous)
                    vertex += new Vector3d(quad.trPosition); //Offsetting the plane. Now all vertices form a cube

                switch (quad.plane)
                {
                    case QuadPlane.ZPlane:
                        uv = new Vector2((float)vertex.x, (float)vertex.y);
                        break;
                    case QuadPlane.YPlane:
                        uv = new Vector2((float)vertex.x, (float)vertex.z);
                        break;
                    case QuadPlane.XPlane:
                        uv = new Vector2((float)vertex.z, (float)vertex.y);
                        break;
                    default:
                        uv = Vector2.zero;
                        break;
                }
                if (planet.uvType != UVType.LegacyContinuous)
                    vertex += new Vector3d(quad.trPosition);
            }
            else
            {
                vertex += new Vector3d(quad.trPosition);
                uv = Vector2.zero;
            }

            vertex.Normalize();//Normalizing the vertices. The cube now is a sphere.

            normalized = vertex;
            height = planet.heightProvider.HeightAtXYZ(((Vector3)vertex)); //Getting height at vertex position
            vertex *= (double)planet.radius; //Scaling up the sphere
            vertex *= ((double)planet.heightInv + (double)height) / (double)planet.heightInv; //Offsetting vertex from center based on height and inverse heightScale
            return vertex;
        }


        /// <summary>
        /// Position on unit cube to position on planet
        /// </summary>
        private static Vector3 GetPosition(Quad quad, Vector3 vertex)
        {
            Planet planet = quad.planet;

            vertex = vertex * quad.scale;
            vertex = quad.rotation * vertex + quad.trPosition;
            vertex.Normalize();
            float height = planet.heightProvider.HeightAtXYZ(vertex);
            vertex *= planet.radius;
            vertex *= (planet.heightInv + height) / planet.heightInv;
            return vertex;
        }

        /// <summary>
        /// Position on unit cube to position on planet
        /// </summary>
        private static Vector3d GetPositionD(Quad quad, Vector3d vertex)
        {
            Planet planet = quad.planet;

            vertex = vertex * (double)quad.scale;
            vertex = ((QuaternionD)quad.rotation) * vertex + new Vector3d(quad.trPosition);
            vertex.Normalize();
            float height = planet.heightProvider.HeightAtXYZ((Vector3)vertex);
            vertex *= (double)planet.radius;
            vertex *= ((double)planet.heightInv + (double)height) / (double)planet.heightInv;
            return vertex;
        }

        /// <summary>
        /// Pre-calculates quad-wide constants (that are used in the following UV calculations) to avoid computing them once per vertex.
        /// </summary>
        internal static void CalculateUVConstants(Quad quad, ref int levelConstant, ref double offsetX, ref double offsetY)
        {
            Planet planet = quad.planet;

            if (planet.uvType == UVType.Quad)
            {
                levelConstant = 1 << (planet.detailDistances.Length - quad.level);//Utils.Pow(2, planet.detailDistances.Length - quad.level);
            }
            else if (planet.uvType == UVType.Cube)
            {
                levelConstant = 1 << quad.level;//Utils.Pow(2, quad.level);

                int[] indicies = QuadNeighbor.Decode(quad.index);
                double p = 0.5 * planet.uvScale;
                for (int j = 2; j < indicies.Length; j++)
                {
                    switch (indicies[j])
                    {
                        case 3:
                            offsetY += p;
                            break;
                        case 1:
                            offsetX += p;
                            offsetY += p;
                            break;
                        case 2:
                            break;
                        case 0:
                            offsetX += p;
                            break;
                    }
                    p *= 0.5;
                }

                offsetX = offsetX % 1;
                offsetY = offsetY % 1;
            }
        }

        /// <summary>
        /// Calculates the uv of a vertex with index i.
        /// </summary>
        private static void CalculateUVCube(Quad quad, ref Vector2 uv, int i, int sideLength, int sideLength_1, float uvScale, int levelConstant, double offsetX, double offsetY)
        {
            double x = (i / sideLength) / (double)sideLength_1;
            double y = (i % sideLength) / (double)sideLength_1;

            double scale = (double)uvScale / levelConstant;

            x *= scale;
            y *= scale;

            x += offsetX;
            y += offsetY;

            y *= -1;

            RotateUV(quad, ref x, ref y);

            uv = new Vector2((float)x, (float)y);
        }

        /// <summary>
        /// Calculates the uv of a vertex with index i.
        /// </summary>
        private static void CalculateUVQuad(Quad quad, ref Vector2 uv, int i, int sideLength, int sideLength_1, int levelConstant)
        {
            double x = (i / sideLength) / (double)sideLength_1;
            double y = (i % sideLength) / (double)sideLength_1;

            x *= levelConstant;
            y *= levelConstant;

            y = -y;

            RotateUV(quad, ref x, ref y);


            uv.x = (float)x;
            uv.y = (float)y;
        }


        private static void RotateUV(Quad q, ref double x, ref double y)
        {
            if (q.position == Position.Front)
                switch (q.plane)
                {
                    case QuadPlane.YPlane:
                        return;
                    case QuadPlane.ZPlane:
                        return;
                    case QuadPlane.XPlane:
                        double temp = x;
                        x = y;
                        y = -temp;
                        return;
                }
            else
                switch (q.plane)
                {
                    case QuadPlane.YPlane:
                        x = -x;
                        y = -y;
                        return;
                    case QuadPlane.ZPlane:
                        x = -x;
                        y = -y;
                        return;
                    case QuadPlane.XPlane:
                        double temp = x;
                        x = -y;
                        y = temp;
                        return;
                }
        }

        /// <summary>
        /// Applies slope texturing to a quad or more specifically its MeshData.
        /// </summary>
        private static void SlopeTexture(Planet planet, ref MeshData md, int numVertices, Vector3 down)
        {

            if (planet.slopeTextureType == SlopeTextureType.Fade)
            {
                for (int i = 0; i < numVertices; i++)
                {
                    float slope = Mathf.Acos(Vector3.Dot(-down, md.normals[i])) * Mathf.Rad2Deg;

                    if (slope > planet.slopeAngle - planet.slopeFadeInAngle) //Using slope texture if slope is high enough
                    {
                        float fade = Mathf.Clamp01((slope - planet.slopeFadeInAngle) / (planet.slopeAngle - planet.slopeFadeInAngle)); //fading in in range[slopeAngle - fadeInAngel; slopeAngle]

                        var texture = new float[] { 0, 0, 0, 0, 0, 0 };
                        texture[planet.slopeTexture] = 1f;

                        md.colors[i] = Color32.Lerp(md.colors[i], new Color(texture[0], texture[1], texture[2], texture[3]), fade);
                        md.uv2[i] = Vector2.Lerp(md.uv2[i], new Vector2(texture[4], texture[5]), fade);
                    }
                }
            }


            else if (planet.slopeTextureType == SlopeTextureType.Threshold)
            {
                float slopeAngle = planet.slopeAngle * Mathf.Deg2Rad;

                for (int i = 0; i < numVertices; i++)
                {
                    if (Mathf.Acos(Vector3.Dot(-down, md.normals[i])) > slopeAngle) //Using slope texture if slope is high enough
                    {
                        var texture = new float[] { 0, 0, 0, 0, 0, 0 };
                        texture[planet.slopeTexture] = 1f;

                        md.colors[i] = new Color(texture[0], texture[1], texture[2], texture[3]);
                        md.uv2[i] = new Vector2(texture[4], texture[5]);
                    }
                }
            }
        }
    }
}
