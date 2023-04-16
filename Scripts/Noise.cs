using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

namespace PlanetaryTerrain.Noise
{
    public enum ModuleType { Heightmap = -2, Noise = -1, Select, Curve, Blend, Remap, Add, Subtract, Multiply, Min, Max, Scale, ScaleBias, Abs, Invert, Clamp, Const, Terrace, Turbulence }


    [System.Serializable]
    public class Select : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            float cv = inputs[2].GetNoise(x, y, z);

            if (parameters[0] > 0f)
            {
                float a;
                if (cv < (parameters[1] - parameters[0]))
                {
                    return inputs[0].GetNoise(x, y, z);
                }

                if (cv < (parameters[1] + parameters[0]))
                {
                    float lc = (parameters[1] - parameters[0]);
                    float uc = (parameters[1] + parameters[0]);
                    a = MapCubicSCurve((cv - lc) / (uc - lc));
                    return Mathf.Lerp(inputs[0].GetNoise(x, y, z), inputs[1].GetNoise(x, y, z), a);
                }

                if (cv < (parameters[2] - parameters[0]))
                {
                    return inputs[1].GetNoise(x, y, z);
                }

                if (cv < (parameters[2] + parameters[0]))
                {
                    float lc = (parameters[2] - parameters[0]);
                    float uc = (parameters[2] + parameters[0]);
                    a = MapCubicSCurve((cv - lc) / (uc - lc));
                    return Mathf.Lerp(inputs[1].GetNoise(x, y, z), inputs[0].GetNoise(x, y, z), a);
                }
                return inputs[0].GetNoise(x, y, z);
            }

            if (cv < parameters[1] || cv > parameters[2])
            {
                return inputs[0].GetNoise(x, y, z);
            }
            return inputs[1].GetNoise(x, y, z);
        }
        static float MapCubicSCurve(float value)
        {
            return (value * value * (3f - 2f * value));
        }

        public Select(Module terrainType, Module noise1, Module noise2, float fallOff = 0.175f, float min = -1f, float max = 0f)
        {
            opType = ModuleType.Select;

            inputs = new Module[3];
            parameters = new float[3];

            inputs[0] = noise1;
            inputs[1] = noise2;
            inputs[2] = terrainType;

            parameters[0] = fallOff;
            parameters[1] = min;
            parameters[2] = max;
        }
    }
    [System.Serializable]
    public class Const : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return parameters[0];
        }
        public Const(float constant)
        {
            opType = ModuleType.Const;

            parameters = new float[1];
            this.parameters[0] = constant;
        }
    }
    [System.Serializable]
    public class Add : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return inputs[0].GetNoise(x, y, z) + inputs[1].GetNoise(x, y, z);
        }
        public Add(Module module1, Module module2)
        {
            opType = ModuleType.Add;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;
        }
    }
    [System.Serializable]
    public class Multiply : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return inputs[0].GetNoise(x, y, z) * inputs[1].GetNoise(x, y, z);
        }
        public Multiply(Module module1, Module module2)
        {
            opType = ModuleType.Multiply;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;
        }
    }
    [System.Serializable]
    public class Scale : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return inputs[0].GetNoise(x, y, z) * parameters[0];
        }
        public Scale(Module module1, float scale)
        {
            opType = ModuleType.Scale;

            inputs = new Module[1];
            parameters = new float[1];

            inputs[0] = module1;
            parameters[0] = scale;

        }
    }
    [System.Serializable]
    public class ScaleBias : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return inputs[0].GetNoise(x, y, z) * parameters[0] + parameters[1];
        }
        public ScaleBias(Module module1, float scale, float bias)
        {
            opType = ModuleType.ScaleBias;

            inputs = new Module[1];
            parameters = new float[2];

            inputs[0] = module1;
            parameters[0] = scale;
            parameters[1] = bias;

        }
    }
    [System.Serializable]
    public class Abs : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return Mathf.Abs(inputs[0].GetNoise(x, y, z));
        }
        public Abs(Module module1)
        {
            opType = ModuleType.Abs;

            inputs = new Module[1];

            inputs[0] = module1;

        }
    }
    [System.Serializable]
    public class Clamp : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return Mathf.Clamp(inputs[0].GetNoise(x, y, z), parameters[0], parameters[1]);
        }
        public Clamp(Module module1, float min, float max)
        {
            opType = ModuleType.Clamp;

            inputs = new Module[1];
            parameters = new float[2];

            inputs[0] = module1;
            parameters[0] = min;
            parameters[1] = max;

        }
    }
    [System.Serializable]
    public class Curve : Module
    {
        public FloatCurve curve;
        public override float GetNoise(float x, float y, float z)
        {
            return curve.Evaluate(inputs[0].GetNoise(x, y, z));
        }
        public Curve(Module module1, AnimationCurve curve)
        {
            opType = ModuleType.Curve;

            inputs = new Module[1];

            inputs[0] = module1;
            this.curve = new FloatCurve(curve);

        }
    }

    [System.Serializable]
    public class Subtract : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return inputs[0].GetNoise(x, y, z) - inputs[1].GetNoise(x, y, z);
        }
        public Subtract(Module module1, Module module2)
        {
            opType = ModuleType.Subtract;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;
        }
    }

    [System.Serializable]
    public class Blend : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            float a = inputs[0].GetNoise(x, y, z);
            float b = inputs[1].GetNoise(x, y, z);

            return a + parameters[0] * (b - a);
        }
        public Blend(Module module1, Module module2, float bias = 0.5f)
        {
            opType = ModuleType.Blend;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;

            parameters = new float[1];
            this.parameters[0] = bias;
        }
    }

    [System.Serializable]
    public class Remap : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            //Scale and offset coordiantes
            return inputs[0].GetNoise(
                (x * parameters[0]) + parameters[3],
                (y * parameters[1]) + parameters[4],
                (z * parameters[2]) + parameters[5]
                  );

        }
        public Remap(Module module1, float scaleX, float scaleY, float scaleZ, float offsetX, float offsetY, float offsetZ)
        {
            opType = ModuleType.Remap;

            inputs = new Module[1];

            inputs[0] = module1;

            parameters = new float[6];

            this.parameters[0] = scaleX;
            this.parameters[1] = scaleY;
            this.parameters[2] = scaleZ;

            this.parameters[3] = offsetX;
            this.parameters[4] = offsetY;
            this.parameters[5] = offsetZ;

        }

        public Remap(Module module1, float[] parameters)
        {
            opType = ModuleType.Remap;

            inputs = new Module[1];

            inputs[0] = module1;

            if (parameters.Length != 6)
                throw new System.ArgumentOutOfRangeException("parameters[]", "Size of parameters for Remap needs to be 6.");
            this.parameters = parameters;
        }

    }

    [System.Serializable]
    public class Min : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            float a = inputs[0].GetNoise(x, y, z);
            float b = inputs[1].GetNoise(x, y, z);

            if (b < a)
                return b;
            return a;

        }
        public Min(Module module1, Module module2)
        {
            opType = ModuleType.Min;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;
        }
    }


    [System.Serializable]
    public class Max : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            float a = inputs[0].GetNoise(x, y, z);
            float b = inputs[1].GetNoise(x, y, z);

            if (b > a)
                return b;
            return a;

        }
        public Max(Module module1, Module module2)
        {
            opType = ModuleType.Max;

            inputs = new Module[2];

            inputs[0] = module1;
            inputs[1] = module2;
        }
    }

    [System.Serializable]
    public class Invert : Module
    {
        public override float GetNoise(float x, float y, float z)
        {
            return -inputs[0].GetNoise(x, y, z);
        }
        public Invert(Module module1)
        {
            opType = ModuleType.Invert;

            inputs = new Module[1];

            inputs[0] = module1;
        }
    }

    [System.Serializable]
    public class Terrace : Module
    {
        public override float GetNoise(float x, float y, float z)
        {

            // Get the output value from the source module.
            float sourceModuleValue = inputs[0].GetNoise(x, y, z);
            int controlPointCount = parameters.Length;

            // Find the first element in the control point array that has a value
            // larger than the output value from the source module.
            int indexPos;

            for (indexPos = 0; indexPos < controlPointCount; indexPos++)
                if (sourceModuleValue < parameters[indexPos])
                    break;


            // Find the two nearest control points so that we can map their values
            // onto a quadratic curve.
            int index0 = Mathf.Clamp(indexPos - 1, 0, controlPointCount - 1);
            int index1 = Mathf.Clamp(indexPos, 0, controlPointCount - 1);

            // If some control points are missing (which occurs if the output value from
            // the source module is greater than the largest value or less than the
            // smallest value of the control point array), get the value of the nearest
            // control point and exit now.
            if (index0 == index1)
                return parameters[index1];


            // Compute the alpha value used for linear interpolation.
            float value0 = parameters[index0];
            float value1 = parameters[index1];
            float alpha = (sourceModuleValue - value0) / (value1 - value0);

            // Squaring the alpha produces the terrace effect.
            alpha *= alpha;

            // Now perform the linear interpolation given the alpha value.
            return Mathf.Lerp(value0, value1, alpha);
        }

        public Terrace(Module module1, bool inverted, float[] controlPoints)
        {
            opType = ModuleType.Terrace;

            if (controlPoints.Length < 2)
                throw new System.ArgumentOutOfRangeException("Two or more control points must be specified.");

            parameters = controlPoints;
            inputs = new Module[1];
            inputs[0] = module1;
        }
    }

    [System.Serializable]
    public class HeightmapModule : Module
    {
        [SerializeField]
        public byte[] textAssetBytes;
        public string computeShaderName;
        public bool useBicubicInterpolation;


        [System.NonSerialized]
        public Heightmap heightmap;

        public override float GetNoise(float x, float y, float z)
        {
            if(heightmap == null) return 0;
            return heightmap.GetPosInterpolated(new Vector3(x, y, z)) * 2 - 1;
        }

        public void Init()
        {
            heightmap = new Heightmap(textAssetBytes, useBicubicInterpolation);
        }

        public HeightmapModule(byte[] textAssetBytes, string computeShaderName, bool useBicubicInterpolation)
        {
            opType = ModuleType.Heightmap;
            this.textAssetBytes = textAssetBytes;
            this.computeShaderName = computeShaderName;
            this.useBicubicInterpolation = useBicubicInterpolation;
        }
    }


    [System.Serializable]
    public abstract class Module
    {
        public Module[] inputs;
        public float[] parameters;
        public ModuleType opType;

        public abstract float GetNoise(float x, float y, float z);

        public void Serialize(FileStream fs)
        {
            BinaryFormatter bf = new BinaryFormatter();

            try
            {
                bf.Serialize(fs, this);
            }
            catch (SerializationException e)
            {
                Debug.Log("Failed to serialize. Reason: " + e.Message);
                throw;
            }
            finally
            {
                fs.Close();
            }
        }

        // Required to make sure nodes where parameters are only "approximately" equal are also marked as equal.
        public override bool Equals(object obj)
        {
            if (!(obj is Module)) return false;

            var m = obj as Module;

            if (obj == null || GetType() != obj.GetType())
                return false;

            if (!(m is FastNoise))
            {
                if (this is FastNoise)
                    return false;

                if (m.opType != opType)
                    return false;

                if (m.parameters != parameters)
                    for (int i = 0; i < parameters.Length; i++)
                        if (!Mathf.Approximately(parameters[i], m.parameters[i]))
                            return false;

                if (m.inputs != null && inputs != null)
                {
                    if (m.inputs.Length != inputs.Length) return false;

                    for (int i = 0; i < inputs.Length; i++)
                        if (!inputs[i].Equals(m.inputs[i])) return false;
                }
                else if (!(inputs == null && m.inputs == null)) return false;
            }
            else
            {
                if (!(this is FastNoise))
                    return false;

                var f1 = this as FastNoise;
                var f2 = m as FastNoise;

                if (f1.m_fractalType != f2.m_fractalType || f1.m_frequency != f2.m_frequency || f1.m_lacunarity != f2.m_lacunarity || f1.m_noiseType != f2.m_noiseType || f1.m_octaves != f2.m_octaves)
                    return false;
            }
            return true;
        }


        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    
    [System.Serializable]
    public class Turbulence : Module
    {
        private FastNoise xDistortModule, yDistortModule, zDistortModule; 
        public override float GetNoise(float x, float y, float z)
        {
            // Copied from turbulence.cpp in libNoise!
            // turbulence.cpp
            //
            // Copyright (C) 2003, 2004 Jason Bevins
            //
            // This library is free software; you can redistribute it and/or modify it
            // under the terms of the GNU Lesser General Public License as published by
            // the Free Software Foundation; either version 2.1 of the License, or (at
            // your option) any later version.
            //
            // This library is distributed in the hope that it will be useful, but WITHOUT
            // ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
            // FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
            // License (COPYING.txt) for more details.
            //
            // You should have received a copy of the GNU Lesser General Public License
            // along with this library; if not, write to the Free Software Foundation,
            // Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
            //
            // The developer's email is jlbezigvins@gmzigail.com (for great email, take
            // off every 'zig'.)
            //
            float x0, y0, z0;
            float x1, y1, z1;
            float x2, y2, z2;
            x0 = x + (12414.0f / 65536.0f);
            y0 = y + (65124.0f / 65536.0f);
            z0 = z + (31337.0f / 65536.0f);
            x1 = x + (26519.0f / 65536.0f);
            y1 = y + (18128.0f / 65536.0f);
            z1 = z + (60493.0f / 65536.0f);
            x2 = x + (53820.0f / 65536.0f);
            y2 = y + (11213.0f / 65536.0f);
            z2 = z + (44845.0f / 65536.0f);
            float xDistort = x + (xDistortModule.GetValue (x0, y0, z0)
            * parameters[0]);
            float yDistort = y + (yDistortModule.GetValue (x1, y1, z1)
            * parameters[0]);
            float zDistort = z + (zDistortModule.GetValue (x2, y2, z2)
            * parameters[0]);

            return inputs[0].GetNoise(xDistort, yDistort, zDistort);
        }
        public Turbulence(Module module, float power, int seed, float frequency, int octaves)
        {
            opType = ModuleType.Turbulence;

            inputs = new Module[1];

            inputs[0] = module;

            parameters = new float[] {power};

            xDistortModule = new FastNoise(seed);
            yDistortModule = new FastNoise(seed + 1);
            zDistortModule = new FastNoise(seed + 2);

            xDistortModule.SetNoiseType(NoiseType.Perlin);
            yDistortModule.SetNoiseType(NoiseType.Perlin);
            zDistortModule.SetNoiseType(NoiseType.Perlin);

            xDistortModule.SetFrequency(frequency);
            yDistortModule.SetFrequency(frequency);
            zDistortModule.SetFrequency(frequency);

            xDistortModule.SetFractalOctaves(octaves);
            yDistortModule.SetFractalOctaves(octaves);
            zDistortModule.SetFractalOctaves(octaves);
        }
    }

}
