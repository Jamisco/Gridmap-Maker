using Assets.Gridmap_Assets.Scripts.Mapmaker;
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
using UnityEngine.UIElements;

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

        [SerializeField]
        public bool DisableUnseenChunk = false;
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
            if(DisableUnseenChunk)
            {
                DisableUnseenChunks();
            }

            // on mouse click

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                MouseClick(0);
            }
            else if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                MouseClick(1);
            }
        }
        
        public string layerId = "Layer 1";
        public string layerId2 = "Layer 2";

        public (List<Vector2Int>, List<ShapeVisualData>) mapData;
            
        [SerializeField]
        bool useVe = false;

        [SerializeField]
        bool colorOnly = false;

        public void GenerateGrid()
        {
            TimeLogger.ClearTimers();
            
            TimeLogger.StartTimer(451, "Generation Time");

            gridManager.Initialize();

            MeshLayerSettings layerInfo = new MeshLayerSettings(layerId, aShape, useVe, 0);

            MeshLayerSettings layerInfo2 = new MeshLayerSettings(layerId2, aShape, useVe, 1);

            gridManager.CreateLayer(layerInfo);
           // gridManager.CreateLayer(layerInfo2);

            gridManager.SetVisualContainer(visualContainer);

            TimeLogger.StartTimer(-345, "Generate Tiles");

            TimeLogger.StopTimer(451);
         
            if (mapData.Item1 == null || mapData.Item1.Count != gridManager.GridSize.x * gridManager.GridSize.y)
            {
                mapData = GenerateRandomMap(colorOnly);
            }
            
            TimeLogger.StartTimer(451);

            TimeLogger.StopTimer(-345);

            TimeLogger.StartTimer(-1502, "Insert Block");

            gridManager.InsertPositionBlock(mapData.Item1, mapData.Item2, layerId);
            //  GridManager.InsertPosition_Block(mapData.Item1, mapData.Item2, layerId2);

            TimeLogger.StopTimer(-1502);

            TimeLogger.StartTimer(-415, "Draw Grid");

            gridManager.DrawGrid();

            TimeLogger.StopTimer(-415);

            TimeLogger.StopAllTimers();

            TimeLogger.LogAll(gridManager.GetMapDescription());
        }
        public void UpdateMap()
        {
            gridManager.DrawGrid();
        }

        public void ClearGrid()
        {
            gridManager.Clear();
        }

        [SerializeField]
        public string saveLocation;
        public void SaveMap()
        {
            string save = gridManager.GetSerializeMap();
            System.IO.File.WriteAllText(saveLocation, save);

            Debug.Log("Map Saved");
        }

        [SerializeField]
        public bool useMt;
        public void LoadMap()
        {
            TimeLogger.ClearTimers();
            TimeLogger.StartTimer(-1523, "Load Map");
            string json = System.IO.File.ReadAllText(saveLocation);
            gridManager.DeserializeMap(json, useMt);
            TimeLogger.StopTimer(-1523);

            TimeLogger.Log(-1523, "Using MT: " + useMt + "\t");
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
            
            BasicVisual data = gridManager.GetVisualData(Vector2Int.zero, layerId) as BasicVisual;

            data = data.DeepCopy<BasicVisual>();
            data.mainColor = Color.green;
            data.ValidateVisualHash();
            
            gridManager.InsertVisualData(InputHex, data, layerId);

            gridManager.DrawGrid();

            TimeLogger.StopTimer(4816);

            TimeLogger.LogAll();
        }

        [SerializeField]
        public int sizeX;

        
        [SerializeField]
        public int sizeY;
        public void SpawnSprite()
        {
            for (int i = 0; i < sizeX; i++)
            {
                for (int j = 0; j < sizeY; j++)
                {
                    gridManager.SpawnSprite(new Vector2Int(i, j), sprite);
                }
                gridManager.SpawnSprite(InputHex, sprite);
            }
        }

        public string layerName = "";
        public int order = 1;
        public void Miscellaneous()
        {
            DeleteShape();
        }
        public void ChangeOrientation()
        {
            Vector2Int gridPos = InputHex;

            gridManager.ValidateOrientation();

        }

        private void DeleteShape()
        {
            Vector2Int gridPos = InputHex;

            gridManager.DeletePosition(gridPos);
            gridManager.UpdatePosition(gridPos);

            Debug.Log("Deleted Pos: " + gridPos);
        }

        private void TestVIdChange()
        {
            Vector2Int gridPos = InputHex;

            ShapeVisualData vData = gridManager.GetVisualData(gridPos, layerId);

            Texture2D old = vData.mainTexture;

            vData.mainTexture = visualContainer.GetRandomObject<Texture2D>();

            vData.ValidateVisualHash();

            Debug.Log("Texture Changed: " + old + " -- " + vData.mainTexture);
        }

        public void MouseClick(int c)
        {
            Vector3 pos = Vector3.zero;

            if (GetMousePosition(out pos) == false)
            {
                return;
            }
            Color cc = Color.green;

            if(c > 0)
            {
                cc = Color.red;
            }

            if (gridManager.ContainsWorldPosition(pos) == false)
            {
                Debug.LogError("Mouse Position is not in grid");
                return;
            }

            Vector2Int gridPos = gridManager.WorldToGridPosition(pos);

            Vector3 worldPos = gridManager.GridToWorldPostion(gridPos);

            gridManager.InsertVisualData(gridPos, cc);
            gridManager.UpdatePosition(gridPos);

            float distance = Vector3.Distance(pos, worldPos);

            string data = "World Clicked Position:\t" + pos + "\n" +
                          "Local Clicked Position:\t" + gridManager.transform.InverseTransformPoint(pos) + "\n" +
                        "Grid Position:\t\t" + gridPos + "\n" +
                        "World Position:\t\t" + worldPos + "\n" +
                        "Distance:\t\t" + distance;

            Debug.Log(data);
        }
        public void RemoveVisualData()
        {
            Vector2Int gridPos = InputHex;

            gridManager.RemoveVisualData(gridPos);
            gridManager.UpdatePosition(gridPos);

            Debug.Log("Removed Visual: " + gridPos);
        }
        private bool GetMousePosition(out Vector3 position)
        {
            position = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            return true;

            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            //Initialise the enter variable
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                position = hit.point;
                return true;
            }

            position = Vector3.left;
            return false;
        }
        public (List<Vector2Int>, List<ShapeVisualData>) GenerateRandomMap(bool colorOnly = false)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            List<ShapeVisualData> visualData = new List<ShapeVisualData>();

            BasicVisual data;

            Material material = visualContainer.GetRandomObject<Material>();

            for (int x = 0; x < gridManager.GridSize.x; x++)
            {
                MakeRandomData();

                for (int y = 0; y < gridManager.GridSize.y; y++)
                {
                    positions.Add(new Vector2Int(x, y));
                    visualData.Add(data);
                }
            }

            TimeLogger.StopTimer(42);

            return (positions, visualData);

            void MakeRandomData()
            {
                bool texture = UnityEngine.Random.Range(0, 2) == 0 ? true : true;

                if (colorOnly)
                {
                    texture = false;
                }

                if (texture)
                {
                    Texture2D T = visualContainer.GetRandomObject<Texture2D>();

                    data = new BasicVisual(material, T, Color.white);
                }
                else
                {
                    Color C = UnityEngine.Random.ColorHSV();
                    data = new BasicVisual(material, null, C);

                    data.DataRenderMode = ShapeVisualData.RenderMode.MeshColor;
                }

                data.ValidateVisualHash();
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
                    exampleScript.SpawnSprite();
                }

                if (GUILayout.Button("Miscellaneous"))
                {
                    exampleScript.Miscellaneous();
                }

                if (GUILayout.Button("Update Map"))
                {
                    exampleScript.UpdateMap();
                }

                if (GUILayout.Button("Clear Grid"))
                {
                    exampleScript.ClearGrid();
                }

                if (GUILayout.Button("Clear array"))
                {
                    exampleScript.mapData.Item1 = null;
                    exampleScript.mapData.Item2 = null;
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
