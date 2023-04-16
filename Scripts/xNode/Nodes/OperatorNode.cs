using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain;

public class OperatorNode : PTNode
{

    [Output] public ModuleWrapper output;

    [Input] public ModuleWrapper input0;
    [Input] public ModuleWrapper input1;
    [Input] public ModuleWrapper input2;


    public ModuleType moduleType = ModuleType.Const;
    public float[] parameters;
    public AnimationCurve curve;
    public ModuleType moduleType_old = ModuleType.Const;


    public override Module GetModule()
    {

        Module a = GetInputValue<ModuleWrapper>("input0", ModuleWrapper.Zero).m;
        Module b = GetInputValue<ModuleWrapper>("input1", ModuleWrapper.Zero).m;
        Module c = GetInputValue<ModuleWrapper>("input2", ModuleWrapper.Zero).m;

        Module m;

        if (parameters == null)
            parameters = new float[6];
        if(curve == null)
            curve = new AnimationCurve();

        switch (moduleType)
        {
            case ModuleType.Select:
                if (a == null || b == null || c == null) return null;
                m = new Select(a, b, c, parameters[0], parameters[1], parameters[2]);
                break;

            case ModuleType.Curve:
                if (a == null) return null;
                m = new Curve(a, curve);
                break;

            case ModuleType.Blend:
                if (a == null || b == null) return null;
                m = new Blend(a, b, parameters[0]);
                break;

            case ModuleType.Remap:
                if (parameters.Length != 6)
                    parameters = new float[6];
                if (a == null) return null;
                m = new Remap(a, parameters);
                break;

            case ModuleType.Add:
                if (a == null || b == null) return null;
                m = new Add(a, b);
                break;

            case ModuleType.Subtract:
                if (a == null || b == null) return null;
                m = new Subtract(a, b);
                break;

            case ModuleType.Multiply:
                if (a == null || b == null) return null;
                m = new Multiply(a, b);
                break;

            case ModuleType.Min:
                if (a == null || b == null) return null;
                m = new Min(a, b);
                break;

            case ModuleType.Max:
                if (a == null || b == null) return null;
                m = new Max(a, b);
                break;

            case ModuleType.Scale:
                if (a == null) return null;
                m = new Scale(a, parameters[0]);
                break;

            case ModuleType.ScaleBias:
                if (a == null) return null;
                m = new ScaleBias(a, parameters[0], parameters[1]);
                break;

            case ModuleType.Abs:
                if (a == null) return null;
                m = new Abs(a);
                break;

            case ModuleType.Invert:
                if (a == null) return null;
                m = new Invert(a);
                break;

            case ModuleType.Clamp:
                if (a == null) return null;
                m = new Clamp(a, parameters[0], parameters[1]);
                break;
            case ModuleType.Terrace:
                if (a == null) return null;
                m = new Terrace(a, false, parameters);
                break;
            case ModuleType.Turbulence:
                if (a == null) return null;
                m = new Turbulence(a, parameters[0], (int)parameters[1], parameters[2], (int)parameters[3]);
                break;
            default:
                m = new Const(parameters[0]);
                break;
        }

        return m;
    }

    public override object GetValue(XNode.NodePort port)
    {
        Module m = GetModule();
        if (m != null)
            return new ModuleWrapper(m);
        return null;
    }


}

