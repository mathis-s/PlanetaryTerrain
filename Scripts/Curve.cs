using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlanetaryTerrain.DoubleMath;

namespace PlanetaryTerrain.Noise
{
    [System.Serializable]


    /// <summary>
    /// Very simple implementation of a cubic-interpolated curve. Needed because Unitys built in Animation Curve is neither thread-save nor very fast.
    /// </summary>
    public class FloatCurve
    {
        public float[] times = new float[] { 0f, 0.25f, 0.75f, 1f };
        public float[] values = new float[] { 0f, 0.0625f, 0.5625f, 1f };

        public float Evaluate(float time)
        {
            if (times.Length == values.Length && times.Length > 0)
            {

                time = (time + 1f) / 2f; //Scaling to 0-1 scale
                int index;
                for (index = 0; index < times.Length; index++)
                {
                    if (time < times[index])
                        break;

                }
                int length = times.Length - 1;

                int index0 = Mathf.Clamp(index - 2, 0, length);
                int index1 = Mathf.Clamp(index - 1, 0, length);
                int index2 = Mathf.Clamp(index, 0, length);
                int index3 = Mathf.Clamp(index + 1, 0, length);

                if (index1 == index2)
                    return values[index1];

                float alpha = (time - times[index1]) / (times[index2] - times[index1]);

                return (MathFunctions.CubicInterpolation(values[index0], values[index1], values[index2], values[index3], alpha) * 2f) - 1f; //Scaling back to -1 to 1.
            }
            else
            {
                return 0f;
            }
        }

        public FloatCurve(AnimationCurve animCurve, int accuracy = 25)
        {
            times = new float[accuracy];
            values = new float[accuracy];
            for (int i = 0; i < accuracy; i++)
            {
                times[i] = (i / (accuracy - 1f));
                values[i] = (Mathf.Clamp01(animCurve.Evaluate(i / (accuracy - 1f))));
            }
        }
        public FloatCurve() { }
    }
}
