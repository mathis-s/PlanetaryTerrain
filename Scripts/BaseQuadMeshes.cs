#define ENABLE_DOUBLE_QUAD_FANS //Enables double quad fans that allow neighboring quads to have a level difference of two (not just one) without cracks.
//The only drawback is that quadSize can only be 5, 9, 13, 17... If you want to use a quadSize of e.g. 15 (largest where convex colliders are supported),
//you can undefine this and override the quadSize slider (disable "if (planet.quadSize % 2 == 0 || ((planet.quadSize - 1) / 2) % 2 != 0)") in PlanetEditor.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetaryTerrain
{

    internal class BaseQuadMeshes
    {
        public int sideLength;

        public Vector3[] plane;

        public Vector3[] extendedPlane;
        public int[] trisExtendedPlane;

        public int[] tris0000;

        public int[] tris0001;
        public int[] tris0010;
        public int[] tris0100;
        public int[] tris1000;

        public int[] tris0101;
        public int[] tris0110;
        public int[] tris1001;
        public int[] tris1010;

#if ENABLE_DOUBLE_QUAD_FANS
        public int[] tris0002;
        public int[] tris0020;
        public int[] tris0200;
        public int[] tris2000;

        public int[] tris0202;
        public int[] tris0220;
        public int[] tris2002;
        public int[] tris2020;
#endif

        public Unity.Collections.NativeArray<int> trisNative;

        public BaseQuadMeshes(int sideLength)
        {
            this.sideLength = sideLength;

            GeneratePlane(sideLength, out plane, out tris0000);

            extendedPlane = ExtendedPlaneVerts(sideLength, plane);
            trisExtendedPlane = ExtendedPlaneTris(sideLength, tris0000);

            tris0001 = Remove_000n(sideLength, tris0000, 2, 1);
            tris0010 = Remove_00n0(sideLength, tris0000, 2, 1);
            tris0100 = Remove_0n00(sideLength, tris0000, 2, 1);
            tris1000 = Remove_n000(sideLength, tris0000, 2, 1);

            tris0101 = Remove_0n0n(sideLength, tris0000, 2, 1);
            tris0110 = Remove_0nn0(sideLength, tris0000, 2, 1);
            tris1001 = Remove_n00n(sideLength, tris0000, 2, 1);
            tris1010 = Remove_n0n0(sideLength, tris0000, 2, 1);

#if ENABLE_DOUBLE_QUAD_FANS
            tris0002 = Remove_000n(sideLength, tris0000, 4, 2);
            tris0020 = Remove_00n0(sideLength, tris0000, 4, 2);
            tris0200 = Remove_0n00(sideLength, tris0000, 4, 2);
            tris2000 = Remove_n000(sideLength, tris0000, 4, 2);

            tris0202 = Remove_0n0n(sideLength, tris0000, 4, 2);
            tris0220 = Remove_0nn0(sideLength, tris0000, 4, 2);
            tris2002 = Remove_n00n(sideLength, tris0000, 4, 2);
            tris2020 = Remove_n0n0(sideLength, tris0000, 4, 2);
#endif

            trisNative = new Unity.Collections.NativeArray<int>(trisExtendedPlane.Length, Unity.Collections.Allocator.Persistent);
            trisNative.CopyFrom(trisExtendedPlane);
        }

        public int[] GetTriangles(EdgeConfiguration conf)
        {
            switch ((int)conf)
            {
                default:
                    return tris0000;
                case 0b00000000_00000000_00000000_00000001:
                    return tris0001;
                case 0b00000000_00000000_00000001_00000000:
                    return tris0010;
                case 0b00000000_00000001_00000000_00000000:
                    return tris0100;
                case 0b00000000_00000001_00000000_00000001:
                    return tris0101;

                case 0b00000000_00000001_00000001_00000000:
                    return tris0110;
                case 0b00000001_00000000_00000000_00000000:
                    return tris1000;
                case 0b00000001_00000000_00000000_00000001:
                    return tris1001;
                case 0b00000001_00000000_00000001_00000000:
                    return tris1010;

#if ENABLE_DOUBLE_QUAD_FANS
                case 0b00000000_00000000_00000000_00000010:
                    return tris0002;
                case 0b00000000_00000000_00000010_00000000:
                    return tris0020;
                case 0b00000000_00000010_00000000_00000000:
                    return tris0200;
                case 0b00000010_00000000_00000000_00000000:
                    return tris2000;

                case 0b00000000_00000010_00000000_00000010:
                    return tris0202;
                case 0b00000000_00000010_00000010_00000000:
                    return tris0220;
                case 0b00000010_00000000_00000000_00000010:
                    return tris2002;
                case 0b00000010_00000000_00000010_00000000:
                    return tris2020;
#endif
            }
        }

        public Vector3[] GetExtendedPlane()
        {
            Vector3[] ret = new Vector3[extendedPlane.Length];
            extendedPlane.CopyTo(ret, 0);
            return ret;
        }

        public Vector3[] GetPlane()
        {
            Vector3[] ret = new Vector3[plane.Length];
            plane.CopyTo(ret, 0);
            return ret;
        }


        internal static void GeneratePlane(int sideL, out Vector3[] vertices, out int[] triangles)
        {
            int length = sideL * sideL;
            vertices = new Vector3[sideL * sideL];

            int triangleIndex = 0;
            triangles = new int[(sideL - 1) * (sideL - 1) * 2 * 3];

            float vertDistance = 1f / (float)(sideL - 1);

            for (int y = 0; y < sideL; y++)
            {
                for (int x = 0; x < sideL; x++)
                {
                    int index = y * sideL + x;
                    vertices[index] = new Vector3(x * vertDistance * 2 - 1, 0, y * vertDistance * 2 - 1); //, Mathf.PerlinNoise(x * vertDistance * 100f, y * vertDistance * 100f) * 0.05f

                    if (y < (sideL - 1) && x < (sideL - 1))
                    {
                        triangles[triangleIndex] = index;
                        triangles[triangleIndex + 1] = (y + 1) * sideL + x;
                        triangles[triangleIndex + 2] = (y + 1) * sideL + (x + 1);
                        triangleIndex += 3;

                        triangles[triangleIndex] = index;
                        triangles[triangleIndex + 1] = (y + 1) * sideL + (x + 1);
                        triangles[triangleIndex + 2] = y * sideL + (x + 1);
                        triangleIndex += 3;
                    }
                }
            }
        }


        internal static Vector3[] ExtendedPlaneVerts(int sideL, Vector3[] verts)
        {
            List<Vector3> vertsOut = new List<Vector3>(verts);

            float vDis = 2f / (float)(sideL - 1);

            int length = sideL + 1;
            for (int i = 0; i < length; i++)
            {
                vertsOut.Add(new Vector3(-1 + vDis * i, 0, -1 - vDis));
            }


            for (int i = 0; i < length; i++)
            {
                vertsOut.Add(new Vector3(1 + vDis, 0, -1 + vDis * i));
            }


            for (int i = 0; i < length; i++)
            {
                vertsOut.Add(new Vector3(1 - vDis * i, 0, 1 + vDis));
            }


            for (int i = 0; i < length; i++)
            {
                vertsOut.Add(new Vector3(-1 - vDis, 0, 1 - vDis * i));
            }

            return vertsOut.ToArray();

        }

        internal static int[] ExtendedPlaneTris(int sideL, int[] tris)
        {
            List<int> trisOut = new List<int>(tris);

            int n = sideL * sideL;
            int index = n;
            int length = index + sideL + 1;

            for (int i = index; i < length - 2; i++)
            {
                trisOut.Add(i);
                trisOut.Add(i - n);
                trisOut.Add(i - n + 1);

                trisOut.Add(i);
                trisOut.Add(i - n + 1);
                trisOut.Add(i + 1);
            }

            trisOut.Add(index + sideL - 1);
            trisOut.Add((index + sideL - 1) - n);
            trisOut.Add(index + sideL + 1);

            trisOut.Add(index + sideL - 1);
            trisOut.Add(index + sideL + 1);
            trisOut.Add(index + sideL);


            index += sideL + 1;
            length += sideL + 1;

            for (int i = index; i < length - 2; i++)
            {
                trisOut.Add(i);
                trisOut.Add((i % sideL) * sideL - 1);
                trisOut.Add(i + 1);

                trisOut.Add((i % sideL) * sideL - 1);
                trisOut.Add((i % sideL) * sideL - 1 + sideL);
                trisOut.Add(i + 1);
            }

            trisOut.Add(n + sideL * 2);
            trisOut.Add(n - 1);
            trisOut.Add(n + sideL * 2 + 1);

            trisOut.Add(n + sideL * 2 + 1);
            trisOut.Add(n - 1);
            trisOut.Add(n + sideL * 2 + 2);


            int corner = length - 1;
            index += sideL + 1;
            length += sideL + 1;

            for (int i = index; i < length - 2; i++)
            {
                trisOut.Add(i);
                trisOut.Add(n - (i - corner));
                trisOut.Add(n - (i - corner + 1));

                trisOut.Add(i);
                trisOut.Add(n - (i - corner + 1));
                trisOut.Add(i + 1);
            }

            corner = sideL * (sideL - 1);

            trisOut.Add(length - 2);
            trisOut.Add(corner);
            trisOut.Add(length);

            trisOut.Add(length - 2);
            trisOut.Add(length);
            trisOut.Add(length - 1);


            index += sideL + 1;
            length += sideL + 1;

            for (int i = index; i < length - 2; i++)
            {
                trisOut.Add(i);
                trisOut.Add(corner - sideL * (i - index));
                trisOut.Add(i + 1);

                trisOut.Add(i + 1);
                trisOut.Add(corner - sideL * (i - index));
                trisOut.Add(corner - sideL * (i - index + 1));
            }

            trisOut.Add(n);
            trisOut.Add(n + 4 * sideL + 3);
            trisOut.Add(0);

            trisOut.Add(n + 4 * sideL + 2);
            trisOut.Add(0);
            trisOut.Add(n + 4 * sideL + 3);

            return trisOut.ToArray();
        }


        internal static int[] Remove_00n0(int sideL, int[] triangles, int step, int num)
        {
            List<int> outTris = new List<int>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool keep = true;

                for (int j = 0; j < num; j++)
                    if ((triangles[i] / sideL == j) || (triangles[i + 1] / sideL == j) || (triangles[i + 2] / sideL == j))
                    {
                        keep = false;
                        break;
                    }

                if (keep)
                {
                    outTris.Add(triangles[i]);
                    outTris.Add(triangles[i + 1]);
                    outTris.Add(triangles[i + 2]);
                }
            }

            for (int yOffset = 0; yOffset < num; yOffset++)
            {
                int length = sideL * (yOffset + 1);
                int start = sideL * yOffset;

                for (int x = start; x < length; x += step)
                {
                    int nHalf = step / 2;
                    if (x < length - step)
                    {
                        outTris.Add(x);
                        outTris.Add(sideL + x + nHalf);
                        outTris.Add(x + step);

                        outTris.Add(x);
                        outTris.Add(sideL + x);
                        outTris.Add(sideL + x + nHalf);
                    }

                    if (x > start)
                    {
                        outTris.Add(x);
                        outTris.Add(sideL + x - nHalf);
                        outTris.Add(sideL + x);
                    }
                }

                step /= 2;
            }
            return outTris.ToArray();
        }


        internal static int[] Remove_000n(int sideL, int[] triangles, int step, int num)
        {
            List<int> outTris = new List<int>();

            int sideL_1 = sideL - 1;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool keep = true;

                for (int j = 0; j < num; j++)
                    if ((triangles[i] / sideL == sideL_1 - j) || (triangles[i + 1] / sideL == sideL_1 - j) || (triangles[i + 2] / sideL == sideL_1 - j))
                    {
                        keep = false;
                        break;
                    }

                if (keep)
                {
                    outTris.Add(triangles[i]);
                    outTris.Add(triangles[i + 1]);
                    outTris.Add(triangles[i + 2]);
                }
            }

            for (int yOffset = 0; yOffset < num; yOffset++)
            {
                int length = sideL * sideL - (yOffset * sideL);
                int b = sideL * (sideL - 1 - yOffset);

                for (int x = sideL * (sideL_1 - yOffset); x < length; x += step)
                {
                    int nHalf = step / 2;
                    if (x < length - 2)
                    {
                        outTris.Add(x);
                        outTris.Add(x + step);
                        outTris.Add(x - sideL + nHalf);

                        outTris.Add(x);
                        outTris.Add(x - sideL + nHalf);
                        outTris.Add(x - sideL);
                    }

                    if (x > b)
                    {
                        outTris.Add(x);
                        outTris.Add(x - sideL);
                        outTris.Add(x - nHalf - sideL);
                    }
                }

                step /= 2;
            }
            return outTris.ToArray();
        }


        internal static int[] Remove_0n00(int sideL, int[] triangles, int step, int num)
        {
            List<int> outTris = new List<int>();

            int sideL_1 = sideL - 1;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool keep = true;

                for (int j = 0; j < num; j++)
                    if ((triangles[i] % sideL == j) || (triangles[i + 1] % sideL == j) || (triangles[i + 2] % sideL == j))
                    {
                        keep = false;
                        break;
                    }

                if (keep)
                {
                    outTris.Add(triangles[i]);
                    outTris.Add(triangles[i + 1]);
                    outTris.Add(triangles[i + 2]);
                }

            }

            for (int xOffset = 0; xOffset < num; xOffset++)
            {
                int length = xOffset + (sideL * sideL_1 + 1);
                for (int y = xOffset; y < length; y += sideL * step)
                {
                    int nHalf = step / 2;
                    if (y < length - 1)
                    {
                        outTris.Add(y);
                        outTris.Add(y + sideL * step);
                        outTris.Add(y + sideL * nHalf + 1);

                        outTris.Add(y);
                        outTris.Add(y + sideL * nHalf + 1);
                        outTris.Add(y + 1);
                    }

                    if (y > xOffset)
                    {
                        outTris.Add(y);
                        outTris.Add(y + 1);
                        outTris.Add(y - sideL * nHalf + 1);
                    }
                }

                step /= 2;
            }
            return outTris.ToArray();
        }


        internal static int[] Remove_n000(int sideL, int[] triangles, int step, int num)
        {
            List<int> outTris = new List<int>();

            int sideL_1 = sideL - 1;


            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool keep = true;

                for (int j = 0; j < num; j++)
                    if ((triangles[i] % sideL == sideL_1 - j) || (triangles[i + 1] % sideL == sideL_1 - j) || (triangles[i + 2] % sideL == sideL_1 - j))
                    {
                        keep = false;
                        break;
                    }

                if (keep)
                {
                    outTris.Add(triangles[i]);
                    outTris.Add(triangles[i + 1]);
                    outTris.Add(triangles[i + 2]);
                }

            }

            for (int xOffset = 0; xOffset < num; xOffset++)
            {
                int length = sideL * sideL - xOffset;
                int b = sideL - xOffset;

                for (int y = sideL_1 - xOffset; y < length; y += sideL * step)
                {
                    int nHalf = (step / 2);
                    if (y < length - 1)
                    {
                        outTris.Add(y);
                        outTris.Add(y + sideL * nHalf - 1);
                        outTris.Add(y + sideL * step);

                        outTris.Add(y);
                        outTris.Add(y - 1);
                        outTris.Add(y + sideL * nHalf - 1);
                    }

                    if (y > b)
                    {
                        outTris.Add(y);
                        outTris.Add(y - sideL * nHalf - 1);
                        outTris.Add(y - 1);
                    }
                }

                step /= 2;
            }
            return outTris.ToArray();
        }


        internal static int[] Remove_n0n0(int sideL, int[] triangles, int step, int num)
        {
            List<int> outTris = new List<int>();

            int sideL_1 = sideL - 1;


            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool keep = true;

                for (int j = 0; j < num; j++)
                    if ((triangles[i] % sideL == sideL_1 - j) || (triangles[i + 1] % sideL == sideL_1 - j) || (triangles[i + 2] % sideL == sideL_1 - j) ||
                        (triangles[i] / sideL == j) || (triangles[i + 1] / sideL == j) || (triangles[i + 2] / sideL == j))
                    {
                        keep = false;
                        break;
                    }

                if (keep)
                {
                    outTris.Add(triangles[i]);
                    outTris.Add(triangles[i + 1]);
                    outTris.Add(triangles[i + 2]);
                }
            }

            int stepR = step;
            for (int xOffset = 0; xOffset < num; xOffset++)
            {
                int length = sideL * sideL - xOffset;
                for (int x = sideL_1 - xOffset; x < length; x += sideL * stepR)
                {
                    int nRHalf = stepR / 2;
                    if (x < length - 1)
                    {
                        if (xOffset == 0 || x > sideL - xOffset)
                        {
                            outTris.Add(x);
                            outTris.Add(x + sideL * nRHalf - 1);
                            outTris.Add(x + sideL * stepR);
                        }

                        if (x > sideL - xOffset)
                        {
                            outTris.Add(x);
                            outTris.Add(x - 1);
                            outTris.Add(x + sideL * nRHalf - 1);
                        }
                    }

                    if (x > sideL - xOffset)
                    {
                        outTris.Add(x);
                        outTris.Add(x - sideL * nRHalf - 1);
                        outTris.Add(x - 1);
                    }
                }
                stepR /= 2;
            }

            stepR = step;
            for (int yOffset = 0; yOffset < num; yOffset++)
            {

                int length = sideL * (yOffset + 1);
                int start = sideL * yOffset;
                for (int x = start; x < length; x += stepR)
                {
                    int nRHalf = stepR / 2;
                    if (x < length - stepR)
                    {
                        if (yOffset == 0 || x < length - 2 * stepR)
                        {
                            outTris.Add(x);
                            outTris.Add(sideL + x + nRHalf);
                            outTris.Add(x + stepR);
                        }

                        outTris.Add(x);
                        outTris.Add(sideL + x);
                        outTris.Add(sideL + x + nRHalf);

                        if (x > start)
                        {
                            outTris.Add(x);
                            outTris.Add(sideL + x - nRHalf);
                            outTris.Add(sideL + x);
                        }
                    }
                }

                stepR /= 2;
            }

            if (num == 2)
            {
                outTris.Add(sideL_1);
                outTris.Add(sideL_1 - 2 + sideL);
                outTris.Add(sideL_1 - 1 + 2 * sideL);
            }
            return outTris.ToArray();
        }


        internal static int[] Remove_0nn0(int sideL, int[] triangles, int step, int num)
        {
            List<int> outTris = new List<int>();

            int sideL_1 = sideL - 1;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool keep = true;

                for (int j = 0; j < num; j++)
                    if ((triangles[i] % sideL == j) || (triangles[i + 1] % sideL == j) || (triangles[i + 2] % sideL == j) ||
                        (triangles[i] / sideL == j) || (triangles[i + 1] / sideL == j) || (triangles[i + 2] / sideL == j))
                    {
                        keep = false;
                        break;
                    }

                if (keep)
                {
                    outTris.Add(triangles[i]);
                    outTris.Add(triangles[i + 1]);
                    outTris.Add(triangles[i + 2]);
                }

            }

            int stepR = step;
            for (int xOffset = 0; xOffset < num; xOffset++)
            {

                int length = xOffset + (sideL * sideL_1 + 1);
                for (int x = xOffset; x < length; x += sideL * stepR)
                {
                    int nRHalf = stepR / 2;
                    if (x < length - 1)
                    {
                        if (xOffset == 0 || x > xOffset)
                        {
                            outTris.Add(x);
                            outTris.Add(x + sideL * stepR);
                            outTris.Add(x + sideL * nRHalf + 1);
                        }

                        if (x > xOffset)
                        {
                            outTris.Add(x);
                            outTris.Add(x + sideL * nRHalf + 1);
                            outTris.Add(x + 1);
                        }
                    }

                    if (x > xOffset)
                    {
                        outTris.Add(x);
                        outTris.Add(x + 1);
                        outTris.Add(x - sideL * nRHalf + 1);
                    }
                }

                stepR /= 2;
            }

            stepR = step;
            for (int yOffset = 0; yOffset < num; yOffset++)
            {

                int length = sideL * (yOffset + 1);
                int start = sideL * yOffset;

                for (int x = start; x < length; x += stepR)
                {
                    int nRHalf = stepR / 2;
                    if (x < length - stepR)
                    {
                        if (yOffset == 0 || x > start)
                        {
                            outTris.Add(x);
                            outTris.Add(sideL + x + nRHalf);
                            outTris.Add(x + stepR);
                        }
                        if (x > start)
                        {
                            outTris.Add(x);
                            outTris.Add(sideL + x);
                            outTris.Add(sideL + x + nRHalf);
                        }
                    }

                    if (x > start)
                    {
                        outTris.Add(x);
                        outTris.Add(sideL + x - nRHalf);
                        outTris.Add(sideL + x);
                    }
                }

                stepR /= 2;
            }

            if (num == 2)
            {
                outTris.Add(0);
                outTris.Add(1 + 2 * sideL);
                outTris.Add(2 + sideL);
            }
            return outTris.ToArray();
        }



        internal static int[] Remove_n00n(int sideL, int[] triangles, int step, int num)
        {
            List<int> outTris = new List<int>();

            int sideL_1 = sideL - 1;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool keep = true;

                for (int j = 0; j < num; j++)
                    if ((triangles[i] % sideL == sideL_1 - j) || (triangles[i + 1] % sideL == sideL_1 - j) || (triangles[i + 2] % sideL == sideL_1 - j) ||
                        (triangles[i] / sideL == sideL_1 - j) || (triangles[i + 1] / sideL == sideL_1 - j) || (triangles[i + 2] / sideL == sideL_1 - j))
                    {
                        keep = false;
                        break;
                    }

                if (keep)
                {
                    outTris.Add(triangles[i]);
                    outTris.Add(triangles[i + 1]);
                    outTris.Add(triangles[i + 2]);
                }

            }

            int stepR = step;
            for (int xOffset = 0; xOffset < num; xOffset++)
            {

                int length = sideL * sideL - xOffset;
                int b = sideL - xOffset;

                for (int x = sideL_1 - xOffset; x < length; x += sideL * stepR)
                {
                    int nRHalf = stepR / 2;
                    if (x < length - 1)
                    {
                        if (xOffset == 0 || x < length - 1 - 2 * sideL)
                        {
                            outTris.Add(x);
                            outTris.Add(x + sideL * nRHalf - 1);
                            outTris.Add(x + sideL * stepR);
                        }

                        outTris.Add(x);
                        outTris.Add(x - 1);
                        outTris.Add(x + sideL * nRHalf - 1);

                        if (x > b)
                        {
                            outTris.Add(x);
                            outTris.Add(x - sideL * nRHalf - 1);
                            outTris.Add(x - 1);
                        }
                    }
                }

                stepR /= 2;
            }

            stepR = step;
            for (int yOffset = 0; yOffset < num; yOffset++)
            {
                int length = sideL * sideL - (yOffset * sideL);
                int b = sideL * (sideL - 1 - yOffset);

                for (int x = sideL * (sideL_1 - yOffset); x < length; x += stepR)
                {
                    int nRHalf = stepR / 2;
                    if (x < length - 2)
                    {
                        if (yOffset == 0 || x < length - 4)
                        {
                            outTris.Add(x);
                            outTris.Add(x + stepR);
                            outTris.Add(x - sideL + nRHalf);
                        }

                        outTris.Add(x);
                        outTris.Add(x - sideL + nRHalf);
                        outTris.Add(x - sideL);

                        if (x > b)
                        {
                            outTris.Add(x);
                            outTris.Add(x - sideL);
                            outTris.Add(x - nRHalf - sideL);
                        }
                    }
                }

                stepR /= 2;
            }

            if (num == 2)
            {
                int corner = sideL * sideL - 1;
                outTris.Add(corner);
                outTris.Add(corner - 1 - 2 * sideL);
                outTris.Add(corner - 2 - sideL);
            }

            return outTris.ToArray();
        }

        internal static int[] Remove_0n0n(int sideL, int[] triangles, int step, int num)
        {
            List<int> outTris = new List<int>();

            int sideL_1 = sideL - 1;


            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool keep = true;

                for (int j = 0; j < num; j++)
                    if ((triangles[i] % sideL == j) || (triangles[i + 1] % sideL == j) || (triangles[i + 2] % sideL == j) ||
                        (triangles[i] / sideL == sideL_1 - j) || (triangles[i + 1] / sideL == sideL_1 - j) || (triangles[i + 2] / sideL == sideL_1 - j))
                    {
                        keep = false;
                        break;
                    }

                if (keep)
                {
                    outTris.Add(triangles[i]);
                    outTris.Add(triangles[i + 1]);
                    outTris.Add(triangles[i + 2]);
                }
            }

            int stepR = step;
            for (int xOffset = 0; xOffset < num; xOffset++)
            {
                int length = xOffset + sideL * sideL_1 + 1;
                for (int x = xOffset; x < length; x += sideL * stepR)
                {
                    int nRHalf = stepR / 2;
                    if (x < length - 1)
                    {
                        if (xOffset == 0 || x < length - 1 - sideL * 2)
                        {
                            outTris.Add(x);
                            outTris.Add(x + sideL * stepR);
                            outTris.Add(x + sideL * nRHalf + 1);
                        }

                        outTris.Add(x);
                        outTris.Add(x + sideL * nRHalf + 1);
                        outTris.Add(x + 1);

                        if (x > xOffset)
                        {
                            outTris.Add(x);
                            outTris.Add(x + 1);
                            outTris.Add(x - sideL * nRHalf + 1);
                        }
                    }
                }

                stepR /= 2;
            }

            stepR = step;
            for (int yOffset = 0; yOffset < num; yOffset++)
            {

                int length = sideL * sideL - (yOffset * sideL);
                int start = sideL * (sideL_1 - yOffset);

                for (int x = start; x < length; x += stepR)
                {
                    int nRHalf = stepR / 2;
                    if (x < length - 2)
                    {
                        if (yOffset == 0 || x > sideL * (sideL - 1 - yOffset))
                        {
                            outTris.Add(x);
                            outTris.Add(x + stepR);
                            outTris.Add(x - sideL + nRHalf);
                        }

                        if (x > sideL * (sideL - 1 - yOffset))
                        {
                            outTris.Add(x);
                            outTris.Add(x - sideL + nRHalf);
                            outTris.Add(x - sideL);
                        }
                    }

                    if (x > start)
                    {
                        outTris.Add(x);
                        outTris.Add(x - sideL);
                        outTris.Add(x - nRHalf - sideL);
                    }
                }

                stepR /= 2;
            }

            if (num == 2)
            {
                int corner = sideL_1 * sideL;
                outTris.Add(corner);
                outTris.Add(corner + 2 - sideL);
                outTris.Add(corner + 1 - 2 * sideL);
            }
            return outTris.ToArray();
        }
    }
}
