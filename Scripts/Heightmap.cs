using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain
{
    /// <summary>
    /// Class used to store an uncompressed 8 or 16 bit heightmap.
    /// </summary>
    public class Heightmap
    {

        public byte[] bytes; //only bytes or ushorts is used, not both simultaneously
        public ushort[] ushorts;
        public int height;
        public int width;
        public bool is16bit;
        public bool disableInterpolation;
        public bool useBicubicInterpolation;

        int mIndexX, mIndexY;


        public Heightmap(TextAsset textAsset, bool useBicubicInterpolation)
        {
            this.useBicubicInterpolation = useBicubicInterpolation;

            ReadFileBytes(textAsset.bytes);

            this.mIndexX = width - 1;
            this.mIndexY = height - 1;
        }

        public Heightmap(byte[] fileBytes, bool useBicubicInterpolation)
        {
            this.useBicubicInterpolation = useBicubicInterpolation;

            ReadFileBytes(fileBytes);

            this.mIndexX = width - 1;
            this.mIndexY = height - 1;
        }

        public Heightmap(string path, bool useBicubicInterpolation)
        {
            this.useBicubicInterpolation = useBicubicInterpolation;

            var fs = new System.IO.FileStream(path, System.IO.FileMode.Open);

            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, (int)fs.Length);
            ReadFileBytes(buffer);

            this.mIndexX = width - 1;
            this.mIndexY = height - 1;
        }

        public Heightmap(int width, int height, bool is16bit, bool useBicubicInterpolation)
        {

            this.width = width;
            this.height = height;
            this.mIndexX = width - 1;
            this.mIndexY = height - 1;
            this.is16bit = is16bit;
            this.useBicubicInterpolation = useBicubicInterpolation;

            if (is16bit)
                ushorts = new ushort[width * height];
            else
                bytes = new byte[width * height];
        }

        public Heightmap(Texture2D texture, bool useBicubicInterpolation, int channel = 0)
        {
            this.useBicubicInterpolation = useBicubicInterpolation;

            ReadTexture2D(texture, channel);

            this.mIndexX = width - 1;
            this.mIndexY = height - 1;
        }


        public void SetPixelAtPos(Vector3 pos, float value)
        {
            float lat = (Mathf.PI - Mathf.Acos(pos.y));
            float lon = (Mathf.Atan2(pos.z, pos.x) + Mathf.PI);

            lat *= MathFunctions.PIInvF;
            lon *= MathFunctions.TwoPIInvF;

            if (!(lat < 1)) lat = 0.9999999f;
            if (!(lon < 1)) lon = 0.9999999f;

            lat *= height;
            lon *= width;

            int x = Mathf.RoundToInt(lon);
            int y = Mathf.RoundToInt(lat);

            if (x == width) x = 0;
            if (y == height) y = 0;

            if (is16bit)
                ushorts[y * width + x] = (ushort)(value * ushort.MaxValue);
            else
                bytes[y * width + x] = (byte)(value * byte.MaxValue);

        }


        /// <summary>
        /// Returns interpolated height at pos. Heightmap must be equirectangular for this to work.
        /// </summary>
        /// <param name="pos">position, cartesian coordinates, x, y and z, ranging from -1 to 1, relative to planet.
        /// </param>
        public float GetPosInterpolated(Vector3 pos)
        {
            float lat = (Mathf.PI - Mathf.Acos(pos.y));
            float lon = (Mathf.Atan2(pos.z, pos.x) + Mathf.PI);

            lat *= MathFunctions.PIInvF;
            lon *= MathFunctions.TwoPIInvF;

            if (!(lat < 1)) lat = 0.9999999f;
            if (!(lon < 1)) lon = 0.9999999f;

            lat *= height;
            lon *= width;

            float result = 0f;

            if (useBicubicInterpolation && !disableInterpolation)
            {

                int x2 = (int)lon; //floor
                int x1 = x2 - 1;
                if (x1 < 0) x1 += width;

                int x3 = x2 + 1;
                if (x3 > mIndexX) x3 -= width;

                int x4 = x3 + 1;
                if (x4 > mIndexX) x4 -= width;

                int y2 = (int)lat;
                int y1 = y2 - 1;
                if (y1 < 0) y1 += height;

                int y3 = y2 + 1;
                if (y3 > mIndexY) y3 -= height;

                int y4 = y3 + 1;
                if (y4 > mIndexY) y4 -= height;


                float[] pixels = new float[16];

                if (!is16bit)
                    pixels = new float[] { bytes[y1 * width + x1], bytes[y1 * width + x2], bytes[y1 * width + x3], bytes[y1 * width + x4],
                                           bytes[y2 * width + x1], bytes[y2 * width + x2], bytes[y2 * width + x3], bytes[y2 * width + x4],
                                           bytes[y3 * width + x1], bytes[y3 * width + x2], bytes[y3 * width + x3], bytes[y3 * width + x4],
                                           bytes[y4 * width + x1], bytes[y4 * width + x2], bytes[y4 * width + x3], bytes[y4 * width + x4] }; //get sixteen pixels around point
                else
                    pixels = new float[] { ushorts[y1 * width + x1], ushorts[y1 * width + x2], ushorts[y1 * width + x3], ushorts[y1 * width + x4],
                                           ushorts[y2 * width + x1], ushorts[y2 * width + x2], ushorts[y2 * width + x3], ushorts[y2 * width + x4],
                                           ushorts[y3 * width + x1], ushorts[y3 * width + x2], ushorts[y3 * width + x3], ushorts[y3 * width + x4],
                                           ushorts[y4 * width + x1], ushorts[y4 * width + x2], ushorts[y4 * width + x3], ushorts[y4 * width + x4] }; //get sixteen pixels around point

                float xpos = (lon - x2);

                float val1 = MathFunctions.CubicInterpolation(pixels[0], pixels[1], pixels[2], pixels[3], xpos); //cubic interpolation between lines
                float val2 = MathFunctions.CubicInterpolation(pixels[4], pixels[5], pixels[6], pixels[7], xpos);
                float val3 = MathFunctions.CubicInterpolation(pixels[8], pixels[9], pixels[10], pixels[11], xpos);
                float val4 = MathFunctions.CubicInterpolation(pixels[12], pixels[13], pixels[14], pixels[15], xpos);

                result = MathFunctions.CubicInterpolation(val1, val2, val3, val4, lat - y2); //interpolating between line values
            }
            else if (!disableInterpolation)
            {
                int x1 = (int)lon;
                int x2 = x1 + 1;
                if (x2 > mIndexX) x2 -= width;

                int y1 = (int)lat;
                int y2 = y1 + 1;
                if (y2 > mIndexY) y2 -= height;

                float[] pixels = new float[4];

                if (!is16bit)
                    pixels = new float[] { bytes[y1 * width + x1], bytes[y1 * width + x2], bytes[y2 * width + x1], bytes[y2 * width + x2] }; //get four pixels closest to point
                else
                    pixels = new float[] { ushorts[y1 * width + x1], ushorts[y1 * width + x2], ushorts[y2 * width + x1], ushorts[y2 * width + x2] }; //get four pixels closest to point

                double xpos = lon - x1;

                double val1 = pixels[0] + (pixels[1] - pixels[0]) * xpos;
                double val2 = pixels[2] + (pixels[3] - pixels[2]) * xpos;

                result = (float)(val1 + (val2 - val1) * (lat - y1));
            }
            else
            {
                int x = Mathf.RoundToInt(lon);
                int y = Mathf.RoundToInt(lat);

                if (x == width) x = 0;
                if (y == height) y = 0;

                if (is16bit)
                    result = ushorts[y * width + x];
                else
                    result = bytes[y * width + x];
            }

            if (is16bit)
                result /= ushort.MaxValue;
            else
                result /= byte.MaxValue;

            return Mathf.Clamp01(result);

        }

        /// <summary>
        /// Returns interpolated height at pos. Heightmap must be equirectangular for this to work.
        /// </summary>
        /// <param name="pos">position, cartesian coordinates, x, y and z, ranging from -1 to 1, relative to planet.
        /// </param>
        public double GetPosInterpolated(Vector3d pos)
        {
            double lat = (Math.PI - Math.Acos(pos.y));
            double lon = (Math.Atan2(pos.z, pos.x) + Math.PI);

            lat *= MathFunctions.PIInv;
            lon *= MathFunctions.TwoPIInv;

            if (!(lat < 1)) lat = 0.999999999999999;
            if (!(lon < 1)) lon = 0.999999999999999;


            lat *= height;
            lon *= width;



            double result = 0f;

            if (useBicubicInterpolation && !disableInterpolation)
            {

                int x2 = (int)lon; //floor
                int x1 = x2 - 1;
                if (x1 < 0) x1 += width;

                int x3 = x2 + 1;
                if (x3 > mIndexX) x3 -= width;

                int x4 = x3 + 1;
                if (x4 > mIndexX) x4 -= width;
                
                int y2 = (int)lat;
                int y1 = y2 - 1;
                if (y1 < 0) y1 = 0;

                int y3 = y2 + 1;
                if (y3 > mIndexY) y3 = mIndexY;

                int y4 = y3 + 1;
                if (y4 > mIndexY) y4 = mIndexY;


                double[] pixels = new double[16];

                if (!is16bit)
                    pixels = new double[] { bytes[y1 * width + x1], bytes[y1 * width + x2], bytes[y1 * width + x3], bytes[y1 * width + x4],
                                           bytes[y2 * width + x1], bytes[y2 * width + x2], bytes[y2 * width + x3], bytes[y2 * width + x4],
                                           bytes[y3 * width + x1], bytes[y3 * width + x2], bytes[y3 * width + x3], bytes[y3 * width + x4],
                                           bytes[y4 * width + x1], bytes[y4 * width + x2], bytes[y4 * width + x3], bytes[y4 * width + x4] }; //get sixteen pixels around point
                else
                    pixels = new double[] { ushorts[y1 * width + x1], ushorts[y1 * width + x2], ushorts[y1 * width + x3], ushorts[y1 * width + x4],
                                           ushorts[y2 * width + x1], ushorts[y2 * width + x2], ushorts[y2 * width + x3], ushorts[y2 * width + x4],
                                           ushorts[y3 * width + x1], ushorts[y3 * width + x2], ushorts[y3 * width + x3], ushorts[y3 * width + x4],
                                           ushorts[y4 * width + x1], ushorts[y4 * width + x2], ushorts[y4 * width + x3], ushorts[y4 * width + x4] }; //get sixteen pixels around point

                double xpos = (lon - x2);

                double val1 = MathFunctions.CubicInterpolation(pixels[0], pixels[1], pixels[2], pixels[3], xpos); //cubic interpolation between lines
                double val2 = MathFunctions.CubicInterpolation(pixels[4], pixels[5], pixels[6], pixels[7], xpos);
                double val3 = MathFunctions.CubicInterpolation(pixels[8], pixels[9], pixels[10], pixels[11], xpos);
                double val4 = MathFunctions.CubicInterpolation(pixels[12], pixels[13], pixels[14], pixels[15], xpos);

                result = MathFunctions.CubicInterpolation(val1, val2, val3, val4, lat - y2); //interpolating between line values
            }
            else if (!disableInterpolation)
            {
                int x1 = (int)lon;
                int x2 = x1 + 1;
                if (x2 > mIndexX) x2 -= width;

                int y1 = (int)lat;
                int y2 = y1 + 1;
                if (y2 > mIndexY) y2 -= height;

                double[] pixels = new double[4];

                if (!is16bit)
                    pixels = new double[] { bytes[y1 * width + x1], bytes[y1 * width + x2], bytes[y2 * width + x1], bytes[y2 * width + x2] }; //get four pixels closest to point
                else
                    pixels = new double[] { ushorts[y1 * width + x1], ushorts[y1 * width + x2], ushorts[y2 * width + x1], ushorts[y2 * width + x2] }; //get four pixels closest to point

                double xpos = lon - x1;

                double val1 = pixels[0] + (pixels[1] - pixels[0]) * xpos;
                double val2 = pixels[2] + (pixels[3] - pixels[2]) * xpos;

                result = (val1 + (val2 - val1) * (lat - y1));
            }
            else
            {
                int x = (int)Math.Round(lon);
                int y = (int)Math.Round(lat);

                if (x == width) x = 0;
                if (y == height) y = 0;

                if (is16bit)
                    result = ushorts[y * width + x];
                else
                    result = bytes[y * width + x];
            }

            if (is16bit)
                result /= ushort.MaxValue;
            else
                result /= byte.MaxValue;

            return result;//(double)Mathf.Clamp01((float)result);

        }

        /// <summary>
        /// Returns interpolated height at pos.
        /// </summary>
        /// <param name="pos">Get interpolated pixel on heightmap. x and y are in range 0 to 1.
        /// </param>
        public float GetPixelInterpolated(double x, double y, bool wrap = true)
        {
            if (!(x < 1)) x = 0.999999999999999;
            if (!(y < 1)) y = 0.999999999999999;

            x *= width;
            y *= height;

            float result = 0f;

            if (useBicubicInterpolation && !disableInterpolation)
            {

                int x2 = (int)x;
                int x1 = x2 - 1;
                if (x1 < 0)
                {
                    if (wrap)
                        x1 += width;
                    else
                        x1 = 0;
                }

                int x3 = x2 + 1;
                if (x3 > mIndexX)
                {
                    if (wrap)
                        x3 -= width;
                    else
                        x3 = mIndexX;
                }

                int x4 = x3 + 1;
                if (x4 > mIndexX)
                {
                    if (wrap)
                        x4 -= width;
                    else
                        x4 = mIndexX;
                }



                int y2 = (int)y;
                int y1 = y2 - 1;
                if (y1 < 0)
                {
                    y1 = 0;
                }

                int y3 = y2 + 1;
                if (y3 > mIndexY)
                {
                    y3 = mIndexY;

                }

                int y4 = y3 + 1;
                if (y4 > mIndexY)
                {
                    y4 = mIndexY;
                }

                float[] pixels = new float[16];

                if (!is16bit)
                    pixels = new float[] { bytes[y1 * width + x1], bytes[y1 * width + x2], bytes[y1 * width + x3], bytes[y1 * width + x4],
                                           bytes[y2 * width + x1], bytes[y2 * width + x2], bytes[y2 * width + x3], bytes[y2 * width + x4],
                                           bytes[y3 * width + x1], bytes[y3 * width + x2], bytes[y3 * width + x3], bytes[y3 * width + x4],
                                           bytes[y4 * width + x1], bytes[y4 * width + x2], bytes[y4 * width + x3], bytes[y4 * width + x4] }; //get sixteen pixels around point
                else
                    pixels = new float[] { ushorts[y1 * width + x1], ushorts[y1 * width + x2], ushorts[y1 * width + x3], ushorts[y1 * width + x4],
                                           ushorts[y2 * width + x1], ushorts[y2 * width + x2], ushorts[y2 * width + x3], ushorts[y2 * width + x4],
                                           ushorts[y3 * width + x1], ushorts[y3 * width + x2], ushorts[y3 * width + x3], ushorts[y3 * width + x4],
                                           ushorts[y4 * width + x1], ushorts[y4 * width + x2], ushorts[y4 * width + x3], ushorts[y4 * width + x4] }; //get sixteen pixels around point

                float xpos = (float)(x - x2);

                float val1 = MathFunctions.CubicInterpolation(pixels[0], pixels[1], pixels[2], pixels[3], xpos); //cubic interpolation between lines
                float val2 = MathFunctions.CubicInterpolation(pixels[4], pixels[5], pixels[6], pixels[7], xpos);
                float val3 = MathFunctions.CubicInterpolation(pixels[8], pixels[9], pixels[10], pixels[11], xpos);
                float val4 = MathFunctions.CubicInterpolation(pixels[12], pixels[13], pixels[14], pixels[15], xpos);

                result = MathFunctions.CubicInterpolation(val1, val2, val3, val4, (float)(y - y2)); //interpolating between line values

            }
            else if (!disableInterpolation)
            {
                int x1 = (int)x;
                int x2 = x1 + 1;
                if (x2 > mIndexX) x2 -= width;

                int y1 = (int)y;
                int y2 = y1 + 1;
                if (y2 > mIndexY) y2 -= height;

                float[] pixels = new float[4];

                if (!is16bit)
                    pixels = new float[] { bytes[y1 * width + x1], bytes[y1 * width + x2], bytes[y2 * width + x1], bytes[y2 * width + x2] }; //get four pixels closest to point
                else
                    pixels = new float[] { ushorts[y1 * width + x1], ushorts[y1 * width + x2], ushorts[y2 * width + x1], ushorts[y2 * width + x2] }; //get four pixels closest to point

                double xpos = x - x1;

                double val1 = pixels[0] + (pixels[1] - pixels[0]) * xpos;
                double val2 = pixels[2] + (pixels[3] - pixels[2]) * xpos;

                result = (float)(val1 + (val2 - val1) * (y - y1));
            }
            else
            {
                int _x = Mathf.RoundToInt((float)x);
                int _y = Mathf.RoundToInt((float)y);

                if (_x == width) _x = 0;
                if (_y == height) _y = 0;

                if (is16bit)
                    result = ushorts[_y * width + _x];
                else
                    result = bytes[_y * width + _x];

            }

            if (is16bit)
                result /= ushort.MaxValue;
            else
                result /= byte.MaxValue;

            return Mathf.Clamp01(result);
        }


        /// <summary>
        /// Gets heightmap value at pixel (x, y), (0, 0) is lower left. Always returns float in range [0, 1]
        /// </summary>
        public float GetPixel(int x, int y)
        {
            if (is16bit)
                return ushorts[y * width + x] / (float)ushort.MaxValue;
            return bytes[y * width + x] / (float)byte.MaxValue;
        }

        /// <summary>
        /// Sets heightmap value at pixel (x, y), (0, 0) is lower left.
        /// </summary>
        /// <param name="value">height/intensity in range [0, 1]
        /// </param>
        public void SetPixel(int x, int y, float value)
        {
            if (is16bit)
                ushorts[y * width + x] = (ushort)Mathf.RoundToInt(value * ushort.MaxValue);
            else
                bytes[y * width + x] = (byte)Mathf.RoundToInt(value * byte.MaxValue);
        }

        public void ReadTexture2D(Texture2D tex, int channel = 0)
        {
            width = tex.width;
            height = tex.height;

            Color32[] pixels = tex.GetPixels32();
            ushorts = null;
            bytes = new byte[pixels.Length];

            if (channel == 0)
                for (int i = 0; i < pixels.Length; i++)
                {
                    bytes[i] = pixels[i].r;
                }
            else if (channel == 1)
                for (int i = 0; i < pixels.Length; i++)
                {
                    bytes[i] = pixels[i].g;
                }
            else if (channel == 2)
                for (int i = 0; i < pixels.Length; i++)
                {
                    bytes[i] = pixels[i].b;
                }
            else if (channel == 3)
                for (int i = 0; i < pixels.Length; i++)
                {
                    bytes[i] = pixels[i].a;
                }
            else if (channel == -1) //grayscale
                for (int i = 0; i < pixels.Length; i++)
                {
                    bytes[i] = (byte)((pixels[i].r + pixels[i].g + pixels[i].b) / 3);
                }
        }

        /// <summary>
        /// Converts this heightmap to a Texture2D.
        /// </summary>
        public Texture2D GetTexture2D()
        {
            Color32[] colors = new Color32[width * height];

            for (int i = 0; i < colors.Length; i++)
            {
                byte grayscale = 0;

                if (is16bit)
                    grayscale = (byte)(ushorts[i] / 257);
                else
                    grayscale = bytes[i];

                colors[i] = new Color32(grayscale, grayscale, grayscale, byte.MaxValue);
            }

            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels32(colors);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Reads heightmap, resolution and is16bit from file/byte array. Byte array can be generated with GetFileBytes().
        /// </summary>
        void ReadFileBytes(byte[] fileBytes)
        {
            //Header is 9 bytes long and contains width, height (each 4 bytes) and is16bit (1 byte)
            width = BitConverter.ToInt32(fileBytes, 0);
            height = BitConverter.ToInt32(fileBytes, 4);
            is16bit = BitConverter.ToBoolean(fileBytes, 8);

            TestHeightmapResolution(fileBytes.Length, width, height, is16bit);
            int length = width * height;

            if (is16bit)
            {
                bytes = null;
                ushorts = new ushort[length];

                for (int i = 0; i < length; i++)
                    ushorts[i] = (ushort)((fileBytes[(2 * i) + 10] << 8) + fileBytes[(2 * i) + 9]);
            }
            else
            {
                ushorts = null;
                bytes = new byte[length];

                for (int i = 0; i < length; i++)
                    bytes[i] = fileBytes[i + 9];
            }
        }

        /// <summary>
        /// Converts this heightmap to a saveable byte array with header that encodes width, height and is16bit. Data can be read with ReadFileBytes()
        /// </summary>
        public byte[] GetFileBytes()
        {
            byte[] newHeightmapHeader = new byte[9]; //Header is 9 bytes long

            //Saving width, height and is16bit to header
            BitConverter.GetBytes(width).CopyTo(newHeightmapHeader, 0);
            BitConverter.GetBytes(height).CopyTo(newHeightmapHeader, 4);
            BitConverter.GetBytes(is16bit).CopyTo(newHeightmapHeader, 8);

            byte[] fileBytes;

            if (is16bit)
            {
                int length = width * height;
                fileBytes = new byte[2 * width * height + 9];

                for (int i = 0; i < length; i++)
                {
                    fileBytes[2 * i + 9] = (byte)(ushorts[i] & 0xFF); //Little endian
                    fileBytes[2 * i + 10] = (byte)(ushorts[i] >> 8);
                }
            }
            else
            {
                fileBytes = new byte[width * height + 9];
                bytes.CopyTo(fileBytes, 9);
            }

            newHeightmapHeader.CopyTo(fileBytes, 0);
            return fileBytes;
        }

        public static void TestHeightmapResolution(int length, int width, int height, bool is16bit)
        {
            if ((is16bit ? (length - 9) / 2 : length - 9) == height * width)
                return;

            throw new System.ArgumentOutOfRangeException("width, height", "Heightmap resolution incorrect! If heightmap is in old format, please use PlanetaryTerrain -> Utils -> Old To New Heightmap to convert it.");
        }

    }
}
