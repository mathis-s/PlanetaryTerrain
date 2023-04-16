using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain
{
    /// <summary>
    /// Like Heightmap, but the heightmap is streamed from flash storage. Useful for heightmaps to large to be stored in RAM.
    /// </summary>
    public class StreamingHeightmap
    {
        internal struct HeightmapRect
        {
            public Vector2Int lowerLeft;
            public Vector2Int upperRight;
            public double invSizeX;
            public double invSizeY;

            public float pixelDens;
            public Heightmap heightmap;

            public HeightmapRect(Heightmap heightmap, Vector2Int lowerLeft, Vector2Int size, int width, int height)
            {
                this.lowerLeft = lowerLeft;
                this.upperRight = lowerLeft + size;

                this.heightmap = heightmap;

                this.invSizeX = (double)width / size.x;
                this.invSizeY = (double)height / size.y;

                this.pixelDens = ((float)heightmap.width * heightmap.height) / ((float)size.x * size.y);
            }
        }

        public int width, height;
        public bool is16bit;
        public bool useBicubicInterpolation;
        public string path;

        internal List<HeightmapRect> heightmapRects = new List<HeightmapRect>();

        private FileStream fileStream;
        private double invWidth, invHeight;
        private double scaleX, scaleY;

        public void Init()
        {
            fileStream = new FileStream(path, FileMode.Open);

            byte[] header = new byte[9];
            fileStream.Read(header, 0, 9);

            width = BitConverter.ToInt32(header, 0);
            height = BitConverter.ToInt32(header, 4);
            is16bit = BitConverter.ToBoolean(header, 8);
        }

        public void ClearMemory()
        {
            heightmapRects.RemoveRange(1, heightmapRects.Count - 1);
        }

        public void Close()
        {
            heightmapRects = null;
            fileStream.Close();
            fileStream.Dispose();
        }

        /// <summary>
        /// Load given area on heightmap into memory. position is lower left corner, size are the extends.
        /// </summary>
        public void LoadIntoMemory(Vector2 position, Vector2 size)
        {

            int widthPixels = Mathf.RoundToInt(size.x * width);
            int heightPixels = Mathf.RoundToInt(size.y * height);

            int posXPixels = Mathf.RoundToInt(position.x * width);
            int posYPixels = Mathf.RoundToInt(position.y * height);

            Heightmap hm = new Heightmap(widthPixels, heightPixels, is16bit, useBicubicInterpolation);

            if (is16bit)
            {
                int length = widthPixels * 2;
                byte[] buffer = new byte[length];

                int maxY = posYPixels + heightPixels;
                int offset = 0;
                int fsPosition = 2 * (posYPixels * width + posXPixels) + 9;

                for (int currentY = posYPixels; currentY < maxY; currentY++)
                {
                    fileStream.Position = fsPosition;
                    fileStream.Read(buffer, 0, length);

                    for (int i = 0; i < widthPixels; i++)
                    {
                        hm.ushorts[offset + i] = (ushort)((buffer[2 * i + 1] << 8) + buffer[2 * i]);
                    }

                    offset += widthPixels;
                    fsPosition += 2 * width;
                }
            }
            else
            {
                byte[] buffer = new byte[widthPixels];

                int maxY = posYPixels + heightPixels;
                int offset = 0;
                int fsPosition = posYPixels * width + posXPixels + 9;

                for (int currentY = posYPixels; currentY < maxY; currentY++)
                {
                    fileStream.Position = fsPosition;
                    fileStream.Read(buffer, 0, widthPixels);

                    for (int i = 0; i < widthPixels; i++)
                        hm.bytes[offset + i] = buffer[i];

                    offset += widthPixels;
                    fsPosition += width;
                }
            }
            HeightmapRect hr = new HeightmapRect(hm, new Vector2Int(posXPixels, posYPixels), new Vector2Int(widthPixels, heightPixels), width, height);
            heightmapRects.Add(hr);

        }


        /// <summary>
        /// Load given area on heightmap into memory. position is the center, size are the extends. This method (unlike LoadIntoMemory) also checks if the extends reach over the heightmap and
        /// repeats the heightmap over the edges if so.
        /// </summary>

        public void LoadAreaIntoMemory(Vector2 position, Vector2 size)
        {
            Vector2[] corners = new Vector2[4];
            corners[0] = position - (0.5f * size); //lower left
            corners[1] = position + new Vector2(size.x * 0.5f, -size.y * 0.5f); //lower right
            corners[2] = position + new Vector2(-size.x * 0.5f, size.y * 0.5f); //upper left
            corners[3] = position + new Vector2(size.x * 0.5f, size.y * 0.5f); //upper right

            List<Rect> regions = new List<Rect>();

            int outOfBounds = 0;
            List<int> id = new List<int>();

            for (int i = 0; i < corners.Length; i++)
            {
                if (corners[i].x < 0 || corners[i].x > 1 || corners[i].y < 0 || corners[i].y > 1)
                {
                    outOfBounds++;
                    id.Add(i);
                }
            }

            if (outOfBounds == 0)
            {
                LoadIntoMemory(corners[0], size);
            }

            else if (outOfBounds == 2)
            {
                if (id.Contains(0) && id.Contains(2))
                {
                    Vector2 sizePart = new Vector2(-corners[0].x, size.y);
                    LoadIntoMemory(new Vector2(corners[0].x + 1, corners[0].y), sizePart);
                    LoadIntoMemory(new Vector2(0, corners[0].y), new Vector2(size.x - sizePart.x, size.y));
                }

                else if (id.Contains(1) && id.Contains(3))
                {
                    Vector2 sizePart = new Vector2(corners[1].x - 1, size.y);
                    LoadIntoMemory(new Vector2(0, corners[1].y), sizePart);
                    LoadIntoMemory(new Vector2(corners[1].x - size.x, corners[1].y), new Vector2(size.x - sizePart.x, size.y));
                }

                else if (id.Contains(0) && id.Contains(1))
                {
                    Vector2 sizePart = new Vector2(size.x, -corners[0].y);
                    LoadIntoMemory(new Vector2(corners[0].x, corners[0].y + 1), sizePart);
                    LoadIntoMemory(new Vector2(corners[0].x, 0), new Vector2(size.x, size.y - sizePart.y));
                }

                else //if (id.Contains(2) && id.Contains(3))
                {
                    Vector2 sizePart = new Vector2(size.x, corners[2].y - 1);
                    LoadIntoMemory(new Vector2(corners[2].x, 0), sizePart);
                    LoadIntoMemory(new Vector2(corners[2].x, corners[2].y - size.y), new Vector2(size.x, size.y - sizePart.y));
                }
            }

            else if (outOfBounds == 3)
            {
                Vector2 sizePart0 = Vector2.zero, sizePart1 = Vector2.zero, sizePart2 = Vector2.zero, sizePart3 = Vector2.zero;

                if (id.Contains(0) && id.Contains(1) && id.Contains(2)) // bottom left
                {
                    sizePart0 = corners[3];
                    sizePart1 = new Vector2(-corners[2].x, corners[2].y);
                    sizePart2 = new Vector2(corners[1].x, -corners[1].y);
                    sizePart3 = -corners[0];
                }
                else if (id.Contains(0) && id.Contains(1) && id.Contains(3)) // bottom right
                {
                    sizePart0 = new Vector2(corners[3].x - 1, corners[3].y);
                    sizePart1 = new Vector2(1 - corners[0].x, corners[3].y);
                    sizePart2 = new Vector2(corners[1].x - 1, -corners[1].y);
                    sizePart3 = new Vector2(1 - corners[0].x, -corners[0].y);
                }
                else if (id.Contains(0) && id.Contains(2) && id.Contains(3)) // top left
                {
                    sizePart0 = new Vector2(corners[3].x, corners[3].y - 1);
                    sizePart1 = new Vector2(-corners[2].x, corners[2].y - 1);
                    sizePart2 = new Vector2(corners[3].x, 1 - corners[0].y);
                    sizePart3 = new Vector2(-corners[0].x, 1 - corners[0].y);
                }
                else // top right
                {
                    sizePart0 = new Vector2(corners[3].x - 1, corners[3].y - 1);
                    sizePart1 = new Vector2(1 - corners[2].x, corners[2].y - 1);
                    sizePart2 = new Vector2(corners[1].x - 1, 1 - corners[1].y);
                    sizePart3 = new Vector2(1 - corners[2].x, 1 - corners[1].y);
                }

                LoadIntoMemory(new Vector2(0, 0), sizePart0); //bottom left
                LoadIntoMemory(new Vector2(1 - sizePart1.x, 0), sizePart1); //bottom right
                LoadIntoMemory(new Vector2(0, 1 - sizePart2.y), sizePart2); //top left
                LoadIntoMemory(new Vector2(1 - sizePart3.x, 1 - sizePart3.y), sizePart3); //top right
            }
        }

        /// <summary>
        /// Converts this heightmap to a Texture2D. Only loaded areas are included!
        /// </summary>
        public Texture2D GetTexture2D(int texWidth, int texHeight)
        {
            Color32[] colors = new Color32[texWidth * texHeight];

            for (int y = 0; y < texHeight; y++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    byte value = (byte)Mathf.RoundToInt(GetPosInterpolated(x / (double)(texWidth - 1), y / (double)(texHeight - 1)) * byte.MaxValue);
                    colors[y * texWidth + x] = new Color32(value, value, value, byte.MaxValue);
                }
            }

            Texture2D tex = new Texture2D(texWidth, texHeight);
            tex.SetPixels32(colors);
            tex.Apply();
            return tex;
        }


        /// <summary>
        /// Returns interpolated height at pos. Heightmap must be equirectangular for this to work.
        /// </summary>
        /// <param name="pos">position, cartesian coordinates, x, y and z, ranging from -1 to 1, relative to planet
        /// </param>
        public float GetPosInterpolated(Vector3 pos)
        {
            double y = (Math.PI - Math.Acos(pos.y));
            double x = (Math.Atan2(pos.z, pos.x) + Math.PI);

            x *= scaleX;
            y *= scaleY;

            float result = 0;

            float highestPD = float.NegativeInfinity;
            int indexHighest = -1;

            for (int i = 0; i < heightmapRects.Count; i++)
            {
                if (x >= heightmapRects[i].lowerLeft.x && y >= heightmapRects[i].lowerLeft.y && x <= heightmapRects[i].upperRight.x && y <= heightmapRects[i].upperRight.y)
                {
                    if (heightmapRects[i].pixelDens > highestPD)
                    {
                        indexHighest = i;
                        highestPD = heightmapRects[i].pixelDens;
                    }
                }

            }

            if (x * invWidth < 0.0001 || (1 - x * invWidth) < 0.0001)
                indexHighest = 0;

            if (indexHighest != -1)
            {
                x -= heightmapRects[indexHighest].lowerLeft.x;
                y -= heightmapRects[indexHighest].lowerLeft.y;

                x *= invWidth;
                y *= invHeight;

                x *= heightmapRects[indexHighest].invSizeX;
                y *= heightmapRects[indexHighest].invSizeY;

                result = heightmapRects[indexHighest].heightmap.GetPixelInterpolated(x, y, indexHighest == 0);
            }

            return result;
        }


        /// <summary>
        /// Returns interpolated height at pos. X and Y in range 0 to 1.
        /// </summary>
        public float GetPosInterpolated(double x, double y)
        {
            float result = 0;

            float highestPD = float.NegativeInfinity;
            int indexHighest = -1;

            for (int i = 0; i < heightmapRects.Count; i++)
            {
                if (x >= heightmapRects[i].lowerLeft.x && y >= heightmapRects[i].lowerLeft.y && x <= heightmapRects[i].upperRight.x && y <= heightmapRects[i].upperRight.y)
                {
                    if (heightmapRects[i].pixelDens > highestPD)
                    {
                        indexHighest = i;
                        highestPD = heightmapRects[i].pixelDens;
                    }
                }

            }

            if (x * invWidth < 0.0001 || (1 - x * invWidth) < 0.0001)
                indexHighest = 0;

            if (indexHighest != -1)
            {
                x -= heightmapRects[indexHighest].lowerLeft.x;
                y -= heightmapRects[indexHighest].lowerLeft.y;

                x *= invWidth;
                y *= invHeight;

                x *= heightmapRects[indexHighest].invSizeX;
                y *= heightmapRects[indexHighest].invSizeY;

                result = heightmapRects[indexHighest].heightmap.GetPixelInterpolated(x, y);
            }

            return result;

        }

        public StreamingHeightmap(TextAsset baseHeightmap, string path, bool useBicubicInterpolation)
        {
            this.useBicubicInterpolation = useBicubicInterpolation;
            this.path = path;

            Init();

            invHeight = 1.0 / height;
            invWidth = 1.0 / width;

            scaleX = width / (2.0 * Math.PI);
            scaleY = height / (Math.PI);

            heightmapRects.Add(new HeightmapRect(new Heightmap(baseHeightmap, useBicubicInterpolation), Vector2Int.zero, new Vector2Int(width, height), width, height));
        }

        public StreamingHeightmap(TextAsset baseHeightmap, bool useBicubicInterpolation)
        {
            this.useBicubicInterpolation = useBicubicInterpolation;
            var heightmap = new Heightmap(baseHeightmap, useBicubicInterpolation);

            width = heightmap.width;
            height = heightmap.height;
            is16bit = heightmap.is16bit;

            invHeight = 1.0 / height;
            invWidth = 1.0 / width;

            scaleX = width / (2.0 * Math.PI);
            scaleY = height / (Math.PI);

            heightmapRects.Add(new HeightmapRect(heightmap, Vector2Int.zero, new Vector2Int(width, height), width, height));
        }

    }
}
