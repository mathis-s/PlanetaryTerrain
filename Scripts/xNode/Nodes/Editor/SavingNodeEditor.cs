using UnityEditor;
using XNode;
using XNodeEditor;
using XNodeEditor.Internal;
using UnityEngine;



[CustomNodeEditor(typeof(SavingNode))]
public class SavingNodeEditor : NodeEditor
{
    /// <summary> Called whenever the xNode editor window is updated </summary>
    public override void OnBodyGUI()
    {
        var node = (SavingNode)target;
        base.OnBodyGUILight();

        node.filename = EditorGUILayout.TextField("Filename", node.filename);

        if (UnityEngine.GUILayout.Button("Serialize"))
        {
            node.Serialize();
            AssetDatabase.Refresh();
        }

        if (UnityEngine.GUILayout.Button("Serialize Compute Shader"))
        {
            node.SerializeComputeShader();
            AssetDatabase.Refresh();
        }

        if (UnityEngine.GUILayout.Button("Generate Preview"))
        {
            node.GeneratePreview();
        }
        GUILayout.Label(node.preview);
    }
}