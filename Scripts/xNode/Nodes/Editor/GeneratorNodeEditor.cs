using UnityEditor;
using XNode;
using XNodeEditor;
using XNodeEditor.Internal;
using UnityEngine;
using PlanetaryTerrain.Noise;


[CustomNodeEditor(typeof(GeneratorNode))]
public class GeneratorNodeEditor : NodeEditor
{
    /// <summary> Called whenever the xNode editor window is updated </summary>
    public override void OnBodyGUI()
    {
        var node = (GeneratorNode)target;
        base.OnBodyGUILight();

        node.seed = EditorGUILayout.IntField("Seed", node.seed);
        node.frequency = EditorGUILayout.FloatField("Frequency", node.frequency);

        bool usingFractalNoise = node.noiseType == NoiseType.CubicFractal || node.noiseType == NoiseType.PerlinFractal || node.noiseType == NoiseType.SimplexFractal || node.noiseType == NoiseType.ValueFractal;
        if (usingFractalNoise)
        {
            node.octaves = EditorGUILayout.IntField("Octaves", node.octaves);
            node.lacunarity = EditorGUILayout.FloatField("Lacunarity", node.lacunarity);
        }
        node.noiseType = (NoiseType)EditorGUILayout.EnumPopup("Noise Type", node.noiseType);
        if (usingFractalNoise)
            node.fractalType = (FractalType)EditorGUILayout.EnumPopup("Fractal Type", node.fractalType);


        if (node.preview == null)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
             node.UpdatePreview();
            }, null);
        }
        
        if (node.previewChanged)
        {
            if (node.previewHeightmap == null) return;
            node.preview = node.previewHeightmap.GetTexture2D();
            node.previewChanged = false;
            NodeEditorWindow.current.Repaint();
        }

        var centered = new GUIStyle();
        centered.alignment = TextAnchor.UpperCenter;

        GUILayout.Label(node.preview, centered);
    }
}