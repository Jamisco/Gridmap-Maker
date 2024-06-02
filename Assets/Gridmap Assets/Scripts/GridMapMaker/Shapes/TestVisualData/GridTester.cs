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

        private void Start()
        {
            GenerateGrid();
        }

        private void Update()
        {
            DisableUnseenChunks();
        }

        public string layerId = "Layer 1";
        public string layerId2 = "Layer 2";
        
        [SerializeField]
        bool useVe = false;
        public void GenerateGrid()
        {
            TimeLogger.ClearTimers();
            
            TimeLogger.StartTimer(451, "Generation Time");

            DefaultVisual def
                    = DefaultVisual.CreateDefaultVisual(Color.blue);
            
            gridManager.Initialize();

            MeshLayerInfo layerInfo = new MeshLayerInfo(layerId, aShape, def, useVe, 0);

            MeshLayerInfo layerInfo2 = new MeshLayerInfo(layerId2, aShape, def, useVe, 1);

            gridManager.CreateLayer(layerInfo);
            gridManager.CreateLayer(layerInfo2);

            gridManager.SetVisualContainer(visualContainer);

            (List<Vector2Int>, List<ShapeVisualData>) mapData = gridManager.GenerateRandomMap();

            gridManager.InsertPosition_Block(mapData.Item1, mapData.Item2, layerId);
            gridManager.InsertPosition_Block(mapData.Item1, mapData.Item2, layerId2);

            gridManager.UpdateGrid();

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
            TimeLogger.ClearTimers();
            
            TimeLogger.StartTimer(4816, "Highlight Shape");
            
            BasicVisual data = gridManager.GetVisualProperties(Vector2Int.zero, layerId) as BasicVisual;

            data = data.DeepCopy<BasicVisual>();
            data.mainColor = Color.green;
            data.ValidateVisualHash();
            
            gridManager.InsertVisualData(InputHex, data, layerId);

            gridManager.UpdateGrid();

            TimeLogger.StopTimer(4816);

            TimeLogger.LogAll();
        }

        public string layerName = "";
        public int order = 1;
        public void SetSprite()
        {
            // gridManager.SpawnSprite(InputHex, sprite);

            SortingLayerInfo sl = new SortingLayerInfo(layerName, order);

            gridManager.SortMeshLayers(GridManager.SortAxis.Y);
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
