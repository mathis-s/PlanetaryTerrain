using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using PlanetaryTerrain.Noise;
using PlanetaryTerrain;


/// <summary>
/// Class some PlanetaryTerrain Nodes derive from. Handles preview images.
/// </summary>
public abstract class PTNode : Node
{
	public bool previewChanged;
	public Heightmap previewHeightmap;
    public Texture2D preview;
    public bool autoUpdatePreview = true;
	public abstract Module GetModule();

	public virtual void UpdatePreview()
    {
        Module m = GetModule();
        if (m != null)
        {
            previewHeightmap = Utils.GeneratePreviewHeightmap(m, 128, 128);
            previewChanged = true;
        }
    }
}

[System.Serializable]
public class ModuleWrapper
{
    public Module m;

    public static ModuleWrapper Zero
    {
        get
        {
            return new ModuleWrapper(new Const(0f));
        }
    }
    public ModuleWrapper(Module m)
    {
        this.m = m;
    }
}
