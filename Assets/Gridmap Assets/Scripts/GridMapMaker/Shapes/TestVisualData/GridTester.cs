using Assets.Scripts.GridMapMaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData
{
    [Serializable]
    public class GridTester : MonoBehaviour
    {
        [SerializeField]
        GridManager gridManager;

        [SerializeField]
        GridShape aShape;

        [SerializeField]
        BasicVisual basicVisual;
        
        private void OnValidate()
        {
            basicVisual.CheckVisualHashChanged();
        }

        private void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HighlightShape();
            }
        }

        public void GenerateGrid()
        {
            string layerId = "Base Layer";

            gridManager.GenerateGrid(aShape, layerId);
        }
        public void ClearGrid()
        {
            gridManager.Clear();
        }

        public void SaveMap()
        {
            gridManager.SerializeMap();
        }

        public void LoadMap()
        {
            gridManager.DeserializeMap();
        }

        [SerializeField]
        public Vector2Int InputHex;

        public void HighlightShape()
        {
            //Vector3 pos = GetMousePosition();

            //Debug.Log("Clicked Position: " + pos);

            Vector2Int gridPos = InputHex;

            BasicVisual basic = gridManager
                .GetVisualProperties_Clone<BasicVisual>(gridPos);

            basic.mainColor = Color.red;
            basic.CheckVisualHashChanged();
            
            gridManager.InsertVisualData(gridPos, basic);
            gridManager.UpdateChunkLayer(gridPos);

            Debug.Log("Highligted: " + gridPos);

        }

        public void RemoveVisualData()
        {
            //Vector3 pos = GetMousePosition();

            //Debug.Log("Clicked Position: " + pos);

            Vector2Int gridPos = InputHex;

            
            gridManager.RemoveVisualData(gridPos);
            gridManager.UpdateChunkLayer(gridPos);
            
            Debug.Log("Remove Visual: " + gridPos);

        }


        private Vector3 GetMousePosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // The ray hit something, return the point in world space
                return hit.point;
            }
            else
            {
                // The ray didn't hit anything, you might return a default position or handle it as needed
                return Vector3.negativeInfinity;
            }
        }


#if UNITY_EDITOR
        [CustomEditor(typeof(GridTester))]
        public class ClassButtonEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                GridTester exampleScript = (GridTester)target;

                if (GUILayout.Button("Generate Grid"))
                {
                    exampleScript.GenerateGrid();
                }

                if (GUILayout.Button("Highlight Hex"))
                {
                    exampleScript.HighlightShape();
                }

                if (GUILayout.Button("Remove Visual Hex"))
                {
                    exampleScript.RemoveVisualData();
                }


                    if (GUILayout.Button("Clear Grid"))
                {
                    exampleScript.ClearGrid();
                }

                if (GUILayout.Button("Save Map"))
                {
                    exampleScript.SaveMap();
                }

                if (GUILayout.Button("Load Map"))
                {
                    exampleScript.LoadMap();
                }
            }
        }
#endif
    }
}
