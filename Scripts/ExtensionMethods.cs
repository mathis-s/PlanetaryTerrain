using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetaryTerrain.DoubleMath
{
    public static class ExtensionMethods
    {
        public static Vector3d ToVector3d(this Vector3 v3)
        {
            return new Vector3d(v3.x, v3.y, v3.z);
        }

        public static QuaternionD ToQuaterniond(this Quaternion q)
        {
            return new QuaternionD(q.x, q.y, q.z, q.w);
        }

        public static Vector2Int RoundToInt(this Vector2 v)
        {
            return new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
        }

        public static string ValuesToString(this float[] array)
        {

            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            for (int i = 0; i < array.Length; i++)
            {
                sb.Append(array[i].ToString("F6"));
                if (i < array.Length - 1)
                    sb.Append(", ");
            }
            sb.Append("}");
            return sb.ToString();
        }
    }
}
