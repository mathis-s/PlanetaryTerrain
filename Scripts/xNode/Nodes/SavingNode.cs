using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain;
using System.IO;

[NodeWidth(292)]
public class SavingNode : Node
{

    [Input] public ModuleWrapper input;
    public string filename = "noiseModule";

    [System.NonSerialized]
    public Texture2D preview;

    public void Serialize()
    {
        GetInputValue<ModuleWrapper>("input", null).m.Serialize(new FileStream(Application.dataPath + "/" + filename + ".bytes", FileMode.Create));
    }

    public void SerializeComputeShader()
    {
        File.WriteAllText(Application.dataPath + "/" + filename + ".compute", ComputeShaderGenerator.GenerateComputeShader(GetInputValue<ModuleWrapper>("input", null).m));
    }

    public void GeneratePreview()
    {
        preview = Utils.GeneratePreview(GetInputValue<ModuleWrapper>("input", null).m, 256, 256);
    }

}





