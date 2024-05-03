using UnityEngine;
using System.Collections;
using UnityEditor;
using static Assets.Scripts.Miscellaneous.HexFunctions;
using Assets.Gridmap_Assets.Scripts.Miscellaneous;
using Assets.Scripts.GridMapMaker;
using Assets.Scripts.Miscellaneous;
using Assets.Gridmap_Assets.Scripts.Mapmaker;

// Create a 180 degrees wire arc with a ScaleValueHandle attached to the disc
// lets you visualize some info of the transform

[CustomEditor(typeof(LayerHandle))]
class LabelHandle : Editor
{
    void OnSceneGUI()
    {
        LayerHandle handle = (LayerHandle)target;
        MeshLayer meshLayer = handle.GetComponent<MeshLayer>();
        GridChunk chunk = handle.GetComponentInParent<GridChunk>();

        if (handle == null)
        {
            return;
        }

        Handles.color = Color.red;

        Vector3 worldPos = meshLayer.gameObject.transform.position;
        Vector3 localPos = meshLayer.gameObject.transform.localPosition;
        Bounds chunkBounds = chunk.GetLayerBounds(meshLayer.LayerId);
        Bounds camBounds = Camera.main.OrthographicBounds3D();



        GUIStyle st = new GUIStyle();

        st.fontSize = handle.fontSize;
        st.normal.textColor = handle.textColor;

        string text = "World Position: " + worldPos.ToString() +
                      "\nLocal Position: " + localPos.ToString() +
                      "\nLayer Bounds: " + chunkBounds.ToString() +
                      "\nCam Bounds: " + camBounds.ToString() +
                      "\nInsideCam: " + camBounds.Intersects(chunkBounds);

        Handles.Label(worldPos + Vector3.up * 3, text, st);

        Handles.BeginGUI();
        
        if (GUILayout.Button("Reset Area", GUILayout.Width(100)))
        {
            handle.shieldArea = 5;
        }
        
        Handles.EndGUI();


        //Handles.DrawWireArc(meshLayer.Position,
        //    handle.transform.up,
        //    -handle.transform.right,
        //    180,
        //    handle.shieldArea);
        //handle.shieldArea =
        //    Handles.ScaleValueHandle(handle.shieldArea,
        //        handle.transform.position + handle.transform.forward * handle.shieldArea,
        //        handle.transform.rotation,
        //        1,
        //        Handles.ConeHandleCap,
        //        1);
    }
}