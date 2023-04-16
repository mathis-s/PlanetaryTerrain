using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using PlanetaryTerrain;
using XNodeEditor;
using UnityEditor;

[CustomNodeEditor(typeof(FromSavedNode))]
public class FromSavedNodeEditor : NodeEditor
{
    public TextAsset moduleTA;
    public override void OnBodyGUI()
    {
        base.OnBodyGUILight();

        var node = (FromSavedNode)target;

        EditorGUI.BeginChangeCheck();
        moduleTA = (TextAsset)EditorGUILayout.ObjectField("Noise", moduleTA, typeof(TextAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            try {
            node.serialized = Utils.DeserializeTextAsset(moduleTA);
            } catch {
                Debug.LogError("Cannot deserialize. Invalid Noise Module!");
            }
        }

        if (node.previewChanged)
        {
            if (node.previewHeightmap == null) return;
            node.preview = node.previewHeightmap.GetTexture2D();
            node.previewChanged = false;
            NodeEditorWindow.current.Repaint();
        }

        if (node.preview == null)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                node.UpdatePreview();
            }, null);
        }

        var centered = new GUIStyle();
        centered.alignment = TextAnchor.UpperCenter;

        GUILayout.Label(node.preview, centered);
    }
}