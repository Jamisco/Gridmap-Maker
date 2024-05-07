using Assets.Gridmap_Assets.Scripts.Miscellaneous;
using Assets.Scripts.GridMapMaker;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData
{
    [Serializable]
    public class GridTester : MonoBehaviour
    {
        [SerializeField]
        GridManager gridManager;

        [SerializeReference]
        private MapVisualContainer visualContainer;

        [SerializeField]
        GridShape aShape;

        [SerializeField]
        BasicVisual basicVisual;
        
        private void OnValidate()
        {
           // basicVisual.CheckVisualDataChanged();
        }

        private void Update()
        {
            DisableUnseenChunks();
        }

        string layerId = "Base Layer";

        [SerializeField]
        bool useVe = false;
        public void GenerateGrid()
        {
            TimeLogger.ClearTimers();
            
            TimeLogger.StartTimer(451, "Generation Time");

            DefaultVisual def
                    = DefaultVisual.CreateDefaultVisual(Color.blue);

            TimeLogger.StartTimer(145, "Initing Grid");
            gridManager.Initialize();
            TimeLogger.StopTimer(145);

            TimeLogger.StartTimer(233, "Creating Layer");
            gridManager.CreateLayer(layerId, aShape, def, useVisualEquality: useVe);
            TimeLogger.StopTimer(233);

            gridManager.SetVisualContainer(visualContainer);

            gridManager.FillGridChunks_TestMethod();           

            TimeLogger.StopAllTimers();

            TimeLogger.LogAll();
        }
        public void UpdateMap()
        {
            gridManager.SetVisualEquality(useVe, layerId);
            gridManager.RedrawLayer(layerId);
            gridManager.UpdateGrid();
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

        
        public bool status;
        public bool invert;
        public void DisableUnseenChunks()
        {
            Bounds bounds = Camera.main.OrthographicBounds3D();

            gridManager.SetStatusIfChunkIsInBounds(bounds, status, invert);
        }

        [SerializeField]
        public Vector2Int InputHex;

        [SerializeField]
        public Sprite sprite;

        public void HighlightShape()
        {
            

        }

        public void SetSprite()
        {
            gridManager.SpawnSprite(InputHex, sprite);
        }

        public void RemoveVisualData()
        {
            Vector3 pos = GetMousePosition();

            Debug.Log("Clicked Position: " + pos);

            Vector2Int gridPos = InputHex;

            gridManager.RemoveVisualData(gridPos);
            gridManager.UpdatePosition(gridPos);

            Debug.Log("Removed Visual: " + gridPos);
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

                if (GUILayout.Button("Spawn Sprite"))
                {
                    exampleScript.SetSprite();
                }

                if (GUILayout.Button("Update Map"))
                {
                    exampleScript.UpdateMap();
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
