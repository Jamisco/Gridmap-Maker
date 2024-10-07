using Procedural_Planet;
using System;
using UnityEditor;
using UnityEngine;

namespace GridMapMaker.Tutorial
{
    [Serializable]
    public class MapSpawner : MonoBehaviour
    {
        public GridManager gridManager;
        public ComparisonBiosphere comparisonBiosphere;
        public NoiseGenerator noiseGenerator;

        public MeshLayerSettings meshLayerSettings;

        public bool blockInsert = false;
        void Init()
        {
            gridManager = GetComponent<GridManager>();  
            comparisonBiosphere = GetComponent<ComparisonBiosphere>();
            noiseGenerator = GetComponent<NoiseGenerator>();

            noiseGenerator.ComputeNoises(gridManager.GridSize);
            comparisonBiosphere.SetBiomeData(noiseGenerator, gridManager.GridSize);
        }

        public void GenerateMap()
        {
            Init();

            TimeLogger.StartTimer(23, "Generate Time");

            gridManager.Initialize();
            gridManager.CreateLayer(meshLayerSettings, true);

            Vector2Int pos;
            ShapeVisualData vData;

            if(blockInsert)
            {
                var data = comparisonBiosphere.GetBiomeData();

                gridManager.InsertPositionBlock(data.Item1, data.Item2);
            }
            else
            {
                for (int x = 0; x < gridManager.GridSize.x; x++)
                {
                    for (int y = 0; y < gridManager.GridSize.y; y++)
                    {
                        pos = new Vector2Int(x, y);
                        vData = comparisonBiosphere.GetBiomeVData(pos);

                        gridManager.InsertVisualData(pos, vData);
                    }
                }
            }

            gridManager.DrawGrid();

            TimeLogger.StopTimer(23);

            TimeLogger.Log(23);

            TimeLogger.ClearTimers();
        }

        public void Clear()
        {
            gridManager.Clear();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MapSpawner))]
    public class ClassButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapSpawner myScript = (MapSpawner)target;

            if (GUILayout.Button("Generate Map"))
            {
                myScript.GenerateMap();
            }


            if (GUILayout.Button("Clear Map"))
            {
                myScript.Clear();
            }
        }
    }
#endif
}

