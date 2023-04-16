using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;
using PlanetaryTerrain.Noise;
using System.Threading;
using XNodeEditor;
using UnityEditor;

[CreateAssetMenu(menuName = "Node Graph", fileName = "New Node Graph")]
public class PTNodeGraph : NodeGraph
{

    /// <summary>
    /// Recomputes preview image for all nodes affected by given node
    /// </summary>
    public void RippleUpdate(Node n)
    {
        UpdateNode(n);
        foreach (NodePort np in n.Outputs)
        {
            int connectionCount = np.ConnectionCount;

            for (int i = 0; i < connectionCount; i++)
            {
                RippleUpdate(np.GetConnection(i).node);
            }
        }
    }

    public void UpdateNode(Node n)
    {
        if (n is PTNode)
        {
            var pn = (n as PTNode);

            if (pn.autoUpdatePreview)
                pn.UpdatePreview();
        }
    }

    public void OnEnable()
    {
        XNodeEditor.NodeEditor.onUpdateNode = OnUpdateNode;
    }

    public void OnUpdateNode(XNode.Node n)
    {
        //Disconnect destroy all connections if node is OperatorNode and operator type changed
        if (n is OperatorNode)
        {
            var opNode = (OperatorNode)n;

            if (opNode.moduleType != opNode.moduleType_old)
            {
                opNode.moduleType_old = opNode.moduleType;
                foreach (XNode.NodePort port in opNode.Inputs)
                {
                    port.ClearConnections();
                }
                if (opNode.moduleType == ModuleType.Remap)
                    opNode.parameters = new float[] { 1, 1, 1, 0, 0, 0 };
            }
        }

        //Update preview images of nodes affected by given node on other thread
        System.Threading.ThreadPool.QueueUserWorkItem(delegate
         {
             RippleUpdate(n);
         }, null);

    }
}



