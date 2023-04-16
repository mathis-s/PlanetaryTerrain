using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlanetaryTerrain;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain.DoubleMath;

public interface IHeightProvider
{
    /// <summary>
    /// Returns a height value for the point pos. Pos is a (normalized) point on the unit sphere.
    /// </summary>
    float HeightAtXYZ(Vector3 pos);

    void Init();
}


[System.Serializable]
public class HeightmapHeightProvider : IHeightProvider
{
    [System.NonSerialized]
    public Heightmap heightmap;
    public TextAsset heightmapTextAsset;
    public bool useBicubicInterpolation;

    public float HeightAtXYZ(Vector3 pos)
    {
        return heightmap.GetPosInterpolated(pos);
    }
    public void Init()
    {
        heightmap = new Heightmap(heightmapTextAsset, useBicubicInterpolation);
    }
}


[System.Serializable]
public class NoiseHeightProvider : IHeightProvider
{
    [System.NonSerialized]
    public Module noise;
    public TextAsset noiseSerialized;

    public float HeightAtXYZ(Vector3 pos)
    {
        return ((noise.GetNoise(pos.x, pos.y, pos.z) + 1f) * 0.5f);
    }

    public void Init()
    {
        noise = Utils.DeserializeTextAsset(noiseSerialized);
        Utils.RandomizeNoise(ref noise);
    }
}


[System.Serializable]
public class HybridHeightProvider : IHeightProvider
{
    [System.NonSerialized]
    public Heightmap heightmap;
    public TextAsset heightmapTextAsset;
    [System.NonSerialized]
    public Module noise;
    public TextAsset noiseSerialized;
    public bool useBicubicInterpolation;
    public float hybridModeNoiseDiv;

    public float HeightAtXYZ(Vector3 pos)
    {
        return heightmap.GetPosInterpolated(pos) * (hybridModeNoiseDiv - ((noise.GetNoise(pos.x, pos.y, pos.z) + 1f) / 2f)) / hybridModeNoiseDiv;
    }

    public void Init()
    {
        noise = Utils.DeserializeTextAsset(noiseSerialized);

        heightmap = new Heightmap(heightmapTextAsset, useBicubicInterpolation);
    }
}


[System.Serializable]
public class StreamingHeightmapHeightProvider : IHeightProvider
{
    [System.NonSerialized]
    public StreamingHeightmap sHeightmap;

    public string heightmapPath;
    public TextAsset baseHeightmapTextAsset;
    public bool useBicubicInterpolation;
    public Vector2 loadSize;
    public float reloadThreshold;

    [System.NonSerialized]
    private Vector3 lastPosition = Vector3.one * float.PositiveInfinity;
    [System.NonSerialized]
    private bool currentlyReloading;

    public float HeightAtXYZ(Vector3 pos)
    {
        return sHeightmap.GetPosInterpolated(pos);
    }


    public void Init()
    {
        sHeightmap = new StreamingHeightmap(baseHeightmapTextAsset, heightmapPath, useBicubicInterpolation);
    }

    public void Update(QuadSplitQueue queue, Vector3 position)
    {
        // The quad split queue needs to be empty or stopped, then a new area can be loaded on another thread.
        if ((position - lastPosition).magnitude > reloadThreshold && !currentlyReloading)
        {
            if (!queue.isAnyCurrentlySplitting)
            {
                queue.stop = true;
                currentlyReloading = true;
                System.Threading.ThreadPool.QueueUserWorkItem(delegate
                {
                    sHeightmap.ClearMemory();
                    sHeightmap.LoadAreaIntoMemory(MathFunctions.XyzToUV(position), loadSize);
                    currentlyReloading = false;
                    queue.stop = false;
                }, null);
                
                lastPosition = position;
            }
            else
                queue.stop = true;

        }
    }
}

[System.Serializable]
public class DetailHeightmapHeightProvider : IHeightProvider
{
    [System.NonSerialized]
    public StreamingHeightmap sHeightmap;
    public TextAsset baseHeightmapTextAsset;
    public bool useBicubicInterpolation;

    [System.Serializable]
    public struct DetailHeightmap 
    {
        public TextAsset heightmapTextAsset;
        [System.NonSerialized]
        public Heightmap heightmap;
        public Vector2Int lowerLeftInBaseHeightmap;
        public Vector2Int sizeInBaseHeightmap;
    }

    public DetailHeightmap[] detailHeightmaps = new DetailHeightmap[0];

    public float HeightAtXYZ(Vector3 pos)
    {
        return sHeightmap.GetPosInterpolated(pos);
    }


    public void Init()
    {
        sHeightmap = new StreamingHeightmap(baseHeightmapTextAsset, useBicubicInterpolation);
        for (int i = 0; i < detailHeightmaps.Length; i++)
        {
            detailHeightmaps[i].heightmap = new Heightmap(detailHeightmaps[i].heightmapTextAsset, useBicubicInterpolation);
            sHeightmap.heightmapRects.Add(new StreamingHeightmap.HeightmapRect(detailHeightmaps[i].heightmap, detailHeightmaps[i].lowerLeftInBaseHeightmap, detailHeightmaps[i].sizeInBaseHeightmap, sHeightmap.width, sHeightmap.height));
        }
        
    }
}

[System.Serializable]
public class ConstHeightProvider : IHeightProvider
{
    public float constant;

    public float HeightAtXYZ(Vector3 pos)
    {
        return constant;
    }

    public void Init() { }
}

