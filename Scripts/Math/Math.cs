using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain.DoubleMath;
using System;

namespace PlanetaryTerrain
{

    public static class MathFunctions
    {
        public const double DegToRad = System.Math.PI / 180.0;
        public const double Rad2Deg = 180.0 / System.Math.PI;
        public const double HalfPI = System.Math.PI / 2.0;
        public const double PIInv = 1.0 / System.Math.PI;
        public const double TwoPIInv = 1.0 / (2.0 * System.Math.PI);

        public const float PIInvF = (float)(1.0 / System.Math.PI);
        public const float TwoPIInvF = (float)(1.0 / (2.0 * System.Math.PI));
        public const float HalfPIf = Mathf.PI / 2.0f;
        public const float TwoPIf = Mathf.PI * 2.0f;

        /// <summary>
        /// Converts from spherical to cartesian coordinates 
        /// </summary>
        /// <param name="lat">latitude in degrees
        /// </param>
        /// <param name="lon">longitude in degrees
        /// </param>
        /// <param name="radius">radius
        /// </param>
        public static Vector3d LatLonToXyz(double lat, double lon, double radius)
        {
            lat *= DegToRad;
            lon *= DegToRad;
            Vector3d xyz;
            xyz.x = (-radius * System.Math.Sin(lat) * System.Math.Cos(lon));
            xyz.y = (-radius * System.Math.Cos(lat));
            xyz.z = (-radius * System.Math.Sin(lat) * System.Math.Sin(lon));
            return xyz;
        }


        /// <summary>
        /// Converts from spherical to cartesian coordinates 
        /// </summary>
        /// <param name="latlon">spherical coordinates in degrees
        /// </param>
        public static Vector3 LatLonToXyz(Vector2 latlon, float radius)
        {
            latlon *= Mathf.Deg2Rad;
            Vector3 xyz;
            xyz.x = (-radius * Mathf.Sin(latlon.x) * Mathf.Cos(latlon.y));
            xyz.y = (-radius * Mathf.Cos(latlon.x));
            xyz.z = (-radius * Mathf.Sin(latlon.x) * Mathf.Sin(latlon.y));
            return xyz;
        }

        /// <summary>
        /// Converts from cartesian to spherical coordinates, returns in degrees. Y is longitude, in range 0 to 360. X is latitude in range 0 (south pole) to 180 (north pole).
        /// </summary>
        /// <param name="pos">Cartesian coordinates relative to sphere center.
        /// </param>
        /// <param name="radius">Radius, i.e. distance to sphere center, of point.
        /// </param>
        public static Vector2 XyzToLatLon(Vector3 pos, float radius)
        {
            float lat = Mathf.PI - Mathf.Acos(pos.y / radius);
            float lon = (Mathf.Atan2(pos.z, pos.x) + Mathf.PI);

            lat *= Mathf.Rad2Deg;
            lon *= Mathf.Rad2Deg;

            Vector2 ll = new Vector2(lat, lon);

            return ll;
        }

        /// <summary>
        /// Converts from cartesian to spherical coordinates, returns in degrees. Y is longitude, in range 0 to 360. X is latitude in range 0 (south pole) to 180 (north pole).
        /// </summary>
        /// <param name="pos">Cartesian coordinates relative to sphere center.
        /// </param>
        /// <param name="radius">Radius, i.e. distance to sphere center, of point.
        /// </param>
        public static Vector2 XyzToLatLon(Vector3d pos, double radius)
        {
            double lat = Math.PI - Math.Acos(pos.y / radius);
            double lon = (Math.Atan2(pos.z, pos.x) + Mathf.PI);

            lat *= Rad2Deg;
            lon *= Rad2Deg;

            Vector2 ll = new Vector2((float)lat, (float)lon);
            return ll;
        }


        /// <summary>
        /// Converts from cartesian to spherical coordinates, returns in degrees. Y is longitude, in range 0 to 360. X is latitude in range 0 (south pole) to 180 (north pole).
        /// </summary>
        /// <param name="pos">Cartesian coordinates relative to sphere center. Radius is not relevant for the calculation.
        /// </param>
        public static Vector2 XyzToLatLon(Vector3 pos)
        {
            float radius = pos.magnitude;
            float lat = Mathf.PI - Mathf.Acos(pos.y / radius);
            float lon = (Mathf.Atan2(pos.z, pos.x) + Mathf.PI);

            lat *= Mathf.Rad2Deg;
            lon *= Mathf.Rad2Deg;

            Vector2 ll = new Vector2(lat, lon);

            return ll;
        }

        /// <summary>
        /// Converts from cartesian to spherical coordinates, returns in degrees. Y is longitude, in range 0 to 360. X is latitude in range 0 (south pole) to 180 (north pole).
        /// </summary>
        /// <param name="pos">Cartesian coordinates relative to sphere center. Radius is not relevant for the calculation.
        /// </param>
        public static Vector2 XyzToLatLon(Vector3d pos)
        {
            double radius = pos.magnitude;
            double lat = Math.PI - Math.Acos(pos.y / radius);
            double lon = (Math.Atan2(pos.z, pos.x) + Math.PI);

            lat *= Rad2Deg;
            lon *= Rad2Deg;

            Vector2 ll = new Vector2((float)lat, (float)lon);

            return ll;
        }


        /// <summary>
        /// Converts from cartesian to spherical coordinates, returns in range 0 to 1. X is longitude, Y is latitude.
        /// </summary>
        /// <param name="pos">Cartesian coordinates relative to sphere center. Radius is not relevant for the calculation.
        /// </param>
        public static Vector2 XyzToUV(Vector3 pos)
        {
            float radius = pos.magnitude;
            float lat = Mathf.PI - Mathf.Acos(pos.y / radius);
            float lon = (Mathf.Atan2(pos.z, pos.x) + Mathf.PI);

            lat *= (float)MathFunctions.PIInv;
            lon *= (float)MathFunctions.TwoPIInv;

            return new Vector2(lon, lat);
        }


        /// <summary>
        /// Interpolates between n0 and n4 with time a
        /// </summary>
        public static float CubicInterpolation(float n0, float n1, float n2, float n3, float a)
        {
            return n1 + 0.5f * a * (n2 - n0 + a * (2f * n0 - 5f * n1 + 4f * n2 - n3 + a * (3f * (n1 - n2) + n3 - n0)));
        }

        /// <summary>
        /// Interpolates between n0 and n4 with time a
        /// </summary>
        public static double CubicInterpolation(double n0, double n1, double n2, double n3, double a)
        {
            return n1 + 0.5 * a * (n2 - n0 + a * (2 * n0 - 5 * n1 + 4 * n2 - n3 + a * (3 * (n1 - n2) + n3 - n0)));
        }

        /// <summary>
        /// Rotates a point around a pivot
        /// </summary>
        public static Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            var dir = point - pivot;
            dir = rotation * dir;
            point = dir + pivot;
            return point;
        }

        /// <summary>
        /// Rotates a point around a pivot
        /// </summary>
        public static Vector3d RotateAroundPoint(Vector3d point, Vector3d pivot, QuaternionD rotation)
        {
            var dir = point - pivot;
            dir = rotation * dir;
            point = dir + pivot;
            return point;
        }
    }
}
