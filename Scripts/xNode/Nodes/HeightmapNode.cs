using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain;

public class HeightmapNode : PTNode
{

    [Output] public ModuleWrapper output;

    public TextAsset heightmapTextAsset;
    
    [System.NonSerialized]
    public byte[] textAssetBytes = null;

    public string computeShaderName;
    public bool useBicubicInterpolation;
    

    public override object GetValue(XNode.NodePort port)
    {
        // Get new a and b values from input connections. Fallback to field values if input is not connected
        if (port.fieldName != "output")
            return null;

        return new ModuleWrapper(GetModule());
    }

    public override Module GetModule()
    {
        if(textAssetBytes == null && computeShaderName == null)
            return new Const(-1);

        HeightmapModule heightmapNode = new HeightmapModule(textAssetBytes, computeShaderName, useBicubicInterpolation);
        heightmapNode.Init();
        return heightmapNode;
    }

}


