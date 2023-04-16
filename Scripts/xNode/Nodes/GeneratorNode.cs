using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain;

public class GeneratorNode : PTNode
{

    [Output] public ModuleWrapper output;


    public NoiseType noiseType = NoiseType.SimplexFractal;
    public FractalType fractalType = FractalType.Billow;
    public int seed = 42;
    public int octaves = 20;
    public float frequency = 1f;
    public float lacunarity = 2f;

    public override object GetValue(XNode.NodePort port)
    {

        // Get new a and b values from input connections. Fallback to field values if input is not connected
        if (port.fieldName != "output")
            return null;

        //preview = Utils.GeneratePreview(noise, 128, 128);

        return new ModuleWrapper(GetModule());
    }

    public override Module GetModule()
    {
        FastNoise noise = new FastNoise(seed);
        noise.SetNoiseType(noiseType);
        noise.SetFractalType(fractalType);
        noise.SetFractalOctaves(octaves);
        noise.SetFrequency(frequency);
        noise.SetFractalLacunarity(lacunarity);

        return noise;
    }

}


