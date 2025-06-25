using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GridMapMaker.Tutorial
{
    /// <summary>
    /// A script used to test the GridManager and its subsequent classes and functions in editor. 
    /// </summary>
    public class GridTester : MonoBehaviour
    {
        [SerializeField]
        public GridManager gridManager;

        [SerializeField]
        public bool DisableUnseenChunk = false;
        private void OnValidate()
        {
           // basicVisual.CheckVisualDataChanged();
        }
        private void Start()
        {
            //GenerateGrid();
        }

        bool prevUnseen = false;
        private void Update()
        {
            if (DisableUnseenChunk)
            {
                DisableUnseenChunks();
                prevUnseen = true;
            }
            else
            {
                if (prevUnseen)
                {
                    ActiveAll();
                    prevUnseen = false;
                }
            }

            // on mouse click

            if (Input.GetMouseButton(0) == true)
            {
                MouseClick(0);
            }
            else if (Input.GetMouseButton(1) == true)
            {
                MouseClick(1);
            }
        }

        public (List<Vector2Int>, List<ShapeVisualData>) mapData;
            
        [SerializeField]
        bool generateOnlyColors = false;

        [SerializeField]
        public MeshLayerSettings baseLayer;

        [SerializeField]
        public MeshLayerSettings layer2Add;

        public bool status;
        public bool invert;

        [SerializeField]
        public Vector2Int InputHex;


        [SerializeField]
        public string savePath;

        public void ActiveAll()
        {
            gridManager.SetStatusAllChunk(true);
        }
        public void AddCurrentLayer()
        {
            if(gridManager != null)
            {
                gridManager.CreateLayer(layer2Add);

                (List<Vector2Int>, List<ShapeVisualData>) data = GenerateRandomMap(generateOnlyColors);

                gridManager.InsertPositionBlock(data.Item1, data.Item2, layer2Add.LayerId);

                gridManager.DrawLayer(layer2Add.LayerId);
            }       
        }

        public void GenerateGrid(int gs, int cs, bool mt, bool ve)
        {
            gridManager.GridSize = new Vector2Int(gs, gs);
            gridManager.ChunkSize = new Vector2Int(cs, cs);
            gridManager.UseMultithreading = mt;

            baseLayer.UseVisualEquality = ve;

            GenerateGrid();
        }
        public void GenerateGrid()
        {
            TimeLogger.StartTimer(8491, "Generate Grid");

            gridManager.Initialize();

            gridManager.CreateLayer(baseLayer);

            TimeLogger.StopTimer(8491, "Generate Grid");
            // we stop the timer here because it has nothing to do with the gridmap maker
            mapData = GenerateRandomMap(generateOnlyColors);

            TimeLogger.StartTimer(8491, "Generate Grid");


            gridManager.InsertPositionBlock(mapData.Item1, mapData.Item2, baseLayer.LayerId);

            TimeLogger.Log(8491, "Insert Time");

            gridManager.DrawGrid();

            TimeLogger.StopTimer(8491);

            TimeLogger.Log(8491);
            TimeLogger.ClearTimers();

            AddPolyCollider();

        }


        private PolygonCollider2D polygonCollider;
        public void AddPolyCollider()
        {
            Bounds mapBounds = gridManager.LocalBounds;

            polygonCollider = gameObject.GetComponent<PolygonCollider2D>();

            if (polygonCollider == null)
            {
                polygonCollider = gameObject.AddComponent<PolygonCollider2D>();
            }

            Vector2 min = mapBounds.min;
            Vector2 max = mapBounds.max;

            // Create the four corners in clockwise or counter-clockwise order
            Vector2[] points = new Vector2[]
            {
                new Vector2(min.x, min.y), // Bottom Left
                new Vector2(max.x, min.y), // Bottom Right
                new Vector2(max.x, max.y), // Top Right
                new Vector2(min.x, max.y)  // Top Left
            };

            // Assign the path to the collider
            polygonCollider.SetPath(0, points);
        }

        public void UpdateMap()
        {
            gridManager.DrawGrid();
        }

        public void ClearGrid()
        {
            gridManager.Clear();
        }
        
        public void DisableUnseenChunks()
        {
            Bounds bounds = Camera.main.OrthographicBounds3D();

            gridManager.SetStatusIfChunkIsInBounds(bounds, status, invert);
        }

        public void HighlightShape()
        {
            BasicVisual data = gridManager.GetVisualData(Vector2Int.zero, baseLayer.LayerId) as BasicVisual;

            //data = data.DeepCopy() as BasicVisual;
            //data.mainColor = Color.green;
            data.ValidateVisualHash();
            
            gridManager.InsertVisualData(InputHex, data, baseLayer.LayerId);

            gridManager.DrawGrid();

        }

        //public void SpawnSprite()
        //{
        //    gridManager.SpawnSprite(InputHex, sprite2Spawn);
        //}
        public void Miscellaneous()
        {
            BasicVisual data = (BasicVisual) gridManager.GetVisualData(InputHex, baseLayer.LayerId);

            data.mainTexture = textures[Random.Range(0, textures.Count)];

            data.ValidateVisualHash();
        }
        public void ChangeOrientation()
        {
            Vector2Int gridPos = InputHex;

            gridManager.ValidateOrientation();
        }

        private void DeletePosition()
        {
            Vector2Int gridPos = InputHex;

            gridManager.RemoveVisualData(gridPos);
            gridManager.UpdatePosition(gridPos);

            Debug.Log("Deleted Pos: " + gridPos);
        }

        private void TestVIdChange()
        {
            Vector2Int gridPos = InputHex;

            ShapeVisualData vData = gridManager.GetVisualData(gridPos, baseLayer.LayerId);

            //Texture2D old = vData.mainTexture;

            //vData.mainTexture = visualContainer.GetRandomObject<Texture2D>();

            vData.ValidateVisualHash();

            //Debug.Log("Texture Changed: " + old + " -- " + vData.mainTexture);
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
               // Debug.LogError("Mouse Position is not in grid");
                return;
            }

            Vector2Int gridPos = gridManager.WorldToGridPosition(pos);

            Vector3 worldPos = gridManager.GridToWorldPostion(gridPos);

            bool hasGrid = gridManager.ContainsGridPosition(gridPos);

            Debug.Log("Has Pos: " + hasGrid);

            gridManager.InsertVisualData(gridPos, cc);
            gridManager.UpdatePosition(gridPos);

            float distance = Vector3.Distance(pos, worldPos);

            string data = "World Clicked Position:\t" + pos + "\n" +
                          "Local Clicked Position:\t" + gridManager.transform.InverseTransformPoint(pos) + "\n" +
                        "Grid Position:\t\t" + gridPos + "\n" +
                        "World Position:\t\t" + worldPos + "\n" +
                        "Distance:\t\t" + distance;

            //Debug.Log(data);
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
            position = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            return true;
        }

        /// <summary>
        /// The default shader MUST BE PRESENT in the VISUAL CONTRAINER you are using
        /// </summary>
        public Material defaultMat;
        public List<Texture2D> textures;
        public (List<Vector2Int>, List<ShapeVisualData>) GenerateRandomMap(bool colorOnly = false)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            List<ShapeVisualData> visualData = new List<ShapeVisualData>();

            BasicVisual data;

            for (int x = 0; x < gridManager.GridSize.x; x++)
            {
                MakeRandomData();

                for (int y = 0; y < gridManager.GridSize.y; y++)
                {
                    positions.Add(new Vector2Int(x, y));
                    visualData.Add(data);
                }
            }

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
                    Texture2D T = textures[UnityEngine.Random.Range(0, textures.Count)];
                    data = new BasicVisual(defaultMat, T, Color.white);
                }
                else
                {
                    Color C = UnityEngine.Random.ColorHSV();
                    data = new BasicVisual(defaultMat, null, C);

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

                if (GUILayout.Button("Add Layer"))
                {
                    exampleScript.AddCurrentLayer();
                }

                if (GUILayout.Button("Highlight Hex"))
                {
                    exampleScript.HighlightShape();
                }

                if (GUILayout.Button("Remove Visual Hex"))
                {
                    exampleScript.RemoveVisualData();
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
            }
        }
#endif
    }
}
