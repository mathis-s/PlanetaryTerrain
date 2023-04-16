using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class QuadNeighbor
{
    /*
    This is a hybrid approach to finding quad neighbors in quadtree planets.
    The (not yet spherified) planet is unfolded, the unfolded cube is projected onto a 2x2 quadtree that has been subdivided once.

    |00| "01" |10| |11| 
    "02" "03" "12" "13"
    |20| "21" |30| |31|
    |22| |23| |30| |31|

    The quads in quotes are used, the ones with vbars are empty. You should be able to see the familiar unfolded cube shape.
    Where the quads are directly next to a neighbor, the neighbor is found with the algorithm described in this paper: https://web.archive.org/web/20120907211934/http://ww1.ucmss.com/books/LFS/CSREA2006/MSV4517.pdf

    Because the cube is unfolded, there are edge neighbors, which aren't next to each other in the quadtree. Some of these, for example neighbor of 13 in direction 0 (right), quad 02, can be found using the same algorithm.
    For others, like neighbor of quad 21 in direction 2 (down), the algorithm would return unused or wrong quads. Those neighbors are found by replacing or inverting certain numbers in the location string according to the Dictonary quadEdgeNeighbors.

    */
    
    
    // One ulong (64-bit) is a quad index. 6 bits are used to encode how many of the following indicies are used. Otherwise an unused
    // index would be indistinguishable from a 0 index.
    // 0b0000000000000000000000000000000000000000000000000000000000_000000
    //                           indicies                          | length
    //                          29 * 2-bit                         | 6-bit

    const ulong lengthMask = 0b111111;

    static ulong Pow(ulong basen, ulong exponent)
    {
        ulong result = 1;
        for (ulong i = 0; i < exponent; i++)
        {
            result *= basen;
        }

        return result;
    }

    static string ToString(int[] array)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < array.Length; i++)
        {
            sb.Append(array[i]);
        }
        return sb.ToString();
    }

    internal static ulong Encode(int[] array)
    {
        ulong index = 0;

        ulong mul = 1;
        for (int i = 0; i < array.Length; i++)
        {
            index += (ulong)array[i] * mul;

            mul *= 4;
        }
        ulong size = (ulong)array.Length;
        index = (index << 6) | size;

        return index;
    }


    internal static int[] Decode(ulong index)
    {
        int size = (int)(index & lengthMask);
        int[] indexOut = new int[size];
        index = (index) >> 6;

        ulong div = 1;

        for (int i = 0; i < size; i++)
        {
            indexOut[i] = (int)((index / div) % 4);
            div *= 4;
        }

        return indexOut;
    }

    internal static ulong Append(ulong index, int num)
    {
        int size = (int)(index & lengthMask);
        index = (index) >> 6;

        ulong pow = 1;
        for (int i = 0; i < size; i++)
        {
            pow *= 4;
        }
        ulong usize = (ulong)(size + 1);

        index = index + pow * (ulong)num;
        index = (index << 6) | usize;

        return index;
    }

    internal static ulong Slice(ulong index)
    {
        int size = (int)(index & lengthMask) - 1;
        index = (index) >> 6;

        ulong pow = 1;
        for (int i = 0; i < size; i++)
            pow *= 4;

        index -= (pow * ((index / pow) % 4));

        ulong usize = (ulong)size;
        index = (index << 6) | usize;
        return index;
    }

    static readonly Dictionary<int2, int2> dict = new Dictionary<int2, int2>()
    {
        {new int2(0, 0), new int2(1, -1)}, // {"direction/quadID", "newQuadId/operation"}
        {new int2(0, 1), new int2(0, 0)},
        {new int2(0, 2), new int2(3, -1)},
        {new int2(0, 3), new int2(2, 0)},

        {new int2(1, 0), new int2(1, 1)},
        {new int2(1, 1), new int2(0, -1)},
        {new int2(1, 2), new int2(3, 1)},
        {new int2(1, 3), new int2(2, -1)},

        {new int2(2, 0), new int2(2, -1)},
        {new int2(2, 1), new int2(3, -1)},
        {new int2(2, 2), new int2(0, 2)},
        {new int2(2, 3), new int2(1, 2)},

        {new int2(3, 0), new int2(2, 3)},
        {new int2(3, 1), new int2(3, 3)},
        {new int2(3, 2), new int2(0, -1)},
        {new int2(3, 3), new int2(1, -1)},

    };

    static readonly Dictionary<int3, int4> quadEdgeNeighbors = new Dictionary<int3, int4>() //Right: 0; Left: 1; Down: 2; Up: 3;
    {
        {new int3(3, 1, 2), new int4(0, 1, 0, 3)}, //{int3(direction, startQuadId[0], startQuadId[0]), int4(newId[0], newId[1], to replace, replace with)}
        {new int3(0, 0, 1), new int4(1, 2, 3, 0)},

        {new int3(3, 0, 2), new int4(0, 1, 1, 2)},
        {new int3(1, 0, 1), new int4(0, 2, 2, 1)},

        {new int3(3, 1, 3), new int4(0, 1, 0, 1)},
        {new int3(3, 0, 1), new int4(1, 3, 1, 0)},


        {new int3(2, 0, 2), new int4(2, 1, 3, 0)},
        {new int3(1, 2, 1), new int4(0, 2, 0, 3)},

        {new int3(2, 1, 2), new int4(2, 1, 2, 1)},
        {new int3(0, 2, 1), new int4(1, 2, 1, 2)},

        {new int3(2, 1, 3), new int4(2, 1, 0, 0)},
        {new int3(2, 2, 1), new int4(1, 3, 0, 0)},

    };
    internal static ulong GetNeighbor(ulong quadId, int dir)
    {
        int[] quadIdA = Decode(quadId);

        if (quadEdgeNeighbors.ContainsKey(new int3(dir, quadIdA[0], quadIdA[1])))
        {
            if (AtQuadEdge(quadIdA, dir))
            {
                return Encode(QuadEdges(quadIdA, dir));
            }
        }

        int quad = quadIdA[quadIdA.Length - 1];
        for (int i = quadIdA.Length - 1; i >= 0; i--)
        {
            int2 result = dict[new int2(dir, quad)];
            quadIdA[i] = result.x;
            dir = result.y;

            if (dir == -1 || i - 1 < 0) break;

            quad = quadIdA[i - 1];
        }

        return Encode(quadIdA);
    }
    static bool AtQuadEdge(int[] quadId, int dir)
    {
        switch (dir)
        {
            case 0:
                return !Contains(quadId, 0, 2);
            case 1:
                return !Contains(quadId, 1, 3);
            case 2:
                return !Contains(quadId, 0, 1);
            case 3:
                return !Contains(quadId, 2, 3);
        }
        return false;
    }

    static bool Contains(int[] quadId, int a, int b)
    {
        for (int i = 2; i < quadId.Length; i++)
        {
            if (quadId[i] == a || quadId[i] == b)
                return true;
        }
        return false;
    }


    public static int[] QuadEdges(int[] quadIdA, int dir)
    {
        int4 result = quadEdgeNeighbors[new int3(dir, quadIdA[0], quadIdA[1])];

        bool invert = (quadIdA[0] == 1 && quadIdA[1] == 3) || (quadIdA[0] == 0 && quadIdA[1] == 1) && dir == 3;

        quadIdA[0] = result.x;
        quadIdA[1] = result.y;

        for (int i = 2; i < quadIdA.Length; i++)
        {
            if (invert)
            {
                if (quadIdA[i] == result.z)
                    quadIdA[i] = result.w;
                else if (quadIdA[i] == result.w)
                    quadIdA[i] = result.z;
            }
            else
            {
                if (quadIdA[i] == result.z)
                    quadIdA[i] = result.w;
            }
        }
        return quadIdA;
    }


    private struct int4
    {
        public int x, y, z, w;
        public int4(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    private struct int3
    {
        public int x, y, z;
        public int3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    private struct int2
    {
        public int x, y;
        public int2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

}
