using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static GridMapMaker.Tutorial.Ecosphere;

namespace GridMapMaker.Tutorial
{
    /// <summary>
    /// A class that used to generated a procedurally generated worldmap using nothing but shaders. 
    /// </summary>
    [SerializeField]
    public class Worldmap : MonoBehaviour
    {
        public GridManager gridManager;
        public Ecosphere biosphere;
        public NoiseGenerator noiseGenerator;

        public bool blockInsert = false;

        [SerializeField]
        public MeshLayerSettings baseLayer;

        [SerializeField]
        public MeshLayerSettings snowLayer;

        public bool instantUpdate = false;
        public void Init()
        {
            gridManager = GetComponent<GridManager>();
            biosphere = GetComponent<Ecosphere>();
            noiseGenerator = GetComponent<NoiseGenerator>();

            noiseGenerator.ComputeNoises(gridManager.GridSize);
        }

        public void ComputeNoise()
        {
            noiseGenerator.ComputeNoises(gridManager.GridSize);
            biosphere.SetBiomeData(noiseGenerator, gridManager.GridSize);
        }

        public void Update()
        {
            if (noiseGenerator.NoiseModified && instantUpdate)
            {
                GenerateGrid();
            }
        }

        public bool printTime = false;
        public bool generating = false;
        public void GenerateGrid()
        {
            if (generating)
            {
                return;
            }

            generating = true;

            TimeLogger.StartTimer(25, "Compute Noise");

            ComputeNoise();

            TimeLogger.StopTimer(25);

            TimeLogger.StartTimer(23, "Generate Time");

            gridManager.Initialize();
            gridManager.CreateLayer(baseLayer, true);
            gridManager.CreateLayer(snowLayer, false);

            Vector2Int pos;
            ShapeVisualData vData;
            ShapeVisualData snowData;

            if (blockInsert)
            {
                BiomeBlockValues data = biosphere.GetBiomeData();

                gridManager.InsertPositionBlock(data.landPos, data.landData);
                gridManager.InsertPositionBlock(data.snowPos, data.snowData, snowLayer.LayerId);
            }
            else
            {
                for (int x = 0; x < gridManager.GridSize.x; x++)
                {
                    for (int y = 0; y < gridManager.GridSize.y; y++)
                    {
                        pos = new Vector2Int(x, y);
                        vData = biosphere.GetBiomeVData(pos);
                        gridManager.InsertVisualData(pos, vData);

                        snowData = biosphere.GetSnowBiomeVData(pos);
                        gridManager.InsertVisualData(pos, snowData, snowLayer.LayerId);
                    }
                }
            }

            gridManager.DrawGrid();

            TimeLogger.StopAllTimers();

            if (printTime)
            {
                TimeLogger.LogAll();
                Debug.Log("Number of Unique Visuals: " + gridManager.GetUniqueVisualHashes().Count);
            }

            TimeLogger.ClearTimers();

            generating = false;
        }

        public string saveLocation = "Assets/Worldmap/WorldmapSave.txt";

        public (List<Vector2Int>, List<ShapeVisualData>) GetPositions()
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            List<ShapeVisualData> visualDatas = new List<ShapeVisualData>();

            for (int i = 0; i <= gridManager.GridSize.x; i++)
            {
                for (int j = 0; j <= gridManager.GridSize.y; j++)
                {
                    Vector2Int pos = new Vector2Int(i, j);
                    ShapeVisualData vData = biosphere.GetBiomeVData(pos);

                    positions.Add(pos);
                    visualDatas.Add(vData);
                }
            }

            return (positions, visualDatas);
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(Worldmap))]
    public class WorldmapEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            Worldmap exampleScript = (Worldmap)target;

            if (GUILayout.Button("Generate Grid"))
            {
                exampleScript.Init();
                exampleScript.GenerateGrid();
            }

            if (GUILayout.Button("Save Grid"))
            {
                string s = exampleScript.gridManager.GetSerializeMap();
                File.WriteAllText(exampleScript.saveLocation, s);
                Debug.Log("Map Saved");
            }

            if (GUILayout.Button("Load Grid"))
            {
                string s = File.ReadAllText(exampleScript.saveLocation);
                exampleScript.gridManager.DeserializeMap(s);
                Debug.Log("Map Loaded");
            }

            if (GUILayout.Button("Clear Grid"))
            {
                exampleScript.gridManager.Clear();
                exampleScript.generating = false;
            }
        }
    }
#endif
}