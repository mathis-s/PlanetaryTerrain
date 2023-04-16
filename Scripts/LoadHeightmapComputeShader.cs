using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadHeightmapComputeShader : MonoBehaviour
{
    public ComputeShader computeShader;
    public Texture2D[] heightmaps;
    public string[] variableNames;
    
    public void Awake()
    {
        if(heightmaps.Length != variableNames.Length)
            throw new System.IndexOutOfRangeException("Heightmaps and variableNames must have the same length!");

        int kernelID = computeShader.FindKernel("ComputePositions");
        
        for (int i = 0; i < heightmaps.Length; i++)
        {
            heightmaps[i].wrapMode = TextureWrapMode.Repeat;
            computeShader.SetTexture(kernelID, variableNames[i], heightmaps[i]);
        }
    }
}
