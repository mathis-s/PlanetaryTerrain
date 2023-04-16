using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain;

[NodeWidth(548)]
public class PreviewNode : PTNode
{

    [Input] public ModuleWrapper input;
    const int width = 512, height = 256;

    public override void UpdatePreview()
    {

        Module m = GetModule();

        previewHeightmap = new Heightmap(width, height, false, false);

        double xMul = 360.0 / width;
        double yMul = 180.0 / height;

        double lat, lon;
        Vector3 xyz;
        float value;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                lat = y * yMul;
                lon = x * xMul;

                xyz = (Vector3)MathFunctions.LatLonToXyz(lat, lon, 1.0);

                value = Mathf.Clamp01((m.GetNoise(xyz.x, xyz.y, xyz.z) + 1f) * 0.5f); //Noise ranges from -1 to 1; texture needs value from 0 to 1
                previewHeightmap.SetPixel(x, y, value);
            }
        }

        previewChanged = true;

    }

    public override Module GetModule()
    {
        return GetInputValue<ModuleWrapper>("input", ModuleWrapper.Zero).m;
    }

    public PreviewNode()
    {
        autoUpdatePreview = false;
    }
}


