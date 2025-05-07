using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GridMapMaker
{
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
            Bounds layerBounds = meshLayer.LayerBounds;
            Bounds camBounds = Camera.main.OrthographicBounds3D();



            GUIStyle st = new GUIStyle();

            st.fontSize = handle.fontSize;
            st.normal.textColor = handle.textColor;

            string text = "World Position: " + worldPos.ToString() +
                          "\nLocal Position: " + localPos.ToString() +
                          "\nLayer Bounds: " + layerBounds.ToString() +
                          "\nCamera Bounds: " + camBounds.ToString() +
                          "\nInsideCam: " + camBounds.Intersects(layerBounds);

            Handles.Label(worldPos + Vector3.up * 3, text, st);


            string te = "O";

            GUIStyle st2 = new GUIStyle();

            st2.fontSize = 20;
            st2.normal.textColor = handle.textColor;

            Vector3 pos = meshLayer.LayerGridShape.GetTesselatedPosition(meshLayer.gridChunk.EndPosition);

            Handles.Label(pos, te, st2);

            Handles.BeginGUI();

            if (GUILayout.Button("Reset Area", GUILayout.Width(100)))
            {
                handle.shieldArea = 5;
            }

            Handles.EndGUI();
        }
    }
}