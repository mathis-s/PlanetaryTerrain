using UnityEditor;
using XNode;
using XNodeEditor;
using XNodeEditor.Internal;
using UnityEngine;
using PlanetaryTerrain.Noise;


[CustomNodeEditor(typeof(HeightmapNode))]
public class HeightmapNodeEditor : NodeEditor
{

    public override void OnBodyGUI()
    {
        var node = (HeightmapNode)target;
        base.OnBodyGUILight();

        EditorGUI.BeginChangeCheck();
        node.heightmapTextAsset = (TextAsset)EditorGUILayout.ObjectField("Heightmap", node.heightmapTextAsset, typeof(TextAsset), false);
        if (node.heightmapTextAsset != null && (EditorGUI.EndChangeCheck() || node.textAssetBytes == null)) node.textAssetBytes = node.heightmapTextAsset.bytes;

        node.computeShaderName = EditorGUILayout.TextField("Name", node.computeShaderName);

        node.useBicubicInterpolation = EditorGUILayout.Toggle("Use Bicubic Interpolation", node.useBicubicInterpolation);

        if (node.preview == null)
        {
            node.UpdatePreview();
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