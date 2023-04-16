using UnityEngine;
using XNode;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain;
using System.IO;

public class FromSavedNode : PTNode
{

    [Output] public ModuleWrapper output;

    public string path;
    public Module serialized;
    public override object GetValue(XNode.NodePort port)
    {
        return new ModuleWrapper(GetModule());
    }

    public override Module GetModule()
    {
        if(serialized == null)
            return new Const(0);

        return serialized;
    }

}


