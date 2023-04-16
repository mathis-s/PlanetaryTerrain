using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using PlanetaryTerrain;
using XNodeEditor;
using UnityEditor;

[CustomNodeEditor(typeof(PreviewNode))]
public class PreviewNodeEditor : NodeEditor
{
    public override void OnBodyGUI()
    {
        base.OnBodyGUILight();

        var node = (PreviewNode)target;

        if (UnityEngine.GUILayout.Button("Generate Preview"))
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

        GUILayout.Label(node.preview);
        node.autoUpdatePreview = EditorGUILayout.Toggle("Auto-update", node.autoUpdatePreview);
    }
}
