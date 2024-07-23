using Assets.Scripts.Procedural_Planet_Scripts;
using GridMapMaker;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace Procedural_Planet
{
    public class GridPlanet : MonoBehaviour
    {
        public GridManager gridManager;
        public NoiseGenerator noiseGenerator;
        public Biosphere biosphere;

        [Header("Gridmap Maker Settings")]

        [SerializeField]
        public MeshLayerSettings layerSettings;

        [Tooltip("Inserts the visual data in a block instead of individually. Much faster")]
        public bool GmmBlockInsert = false;

        [Header("Tilemap Settings")]
        public Tile baseTile;
        public Tilemap baseTileMap;

        public enum MapType { GridMapMaker, UnityTilemap}

        public MapType Map2Generate;
        public void Init()
        {
            gridManager = GetComponentInChildren<GridManager>();
            baseTileMap = GetComponentInChildren<Tilemap>();
            biosphere = GetComponentInChildren<Biosphere>();

            noiseGenerator = GetComponentInChildren<NoiseGenerator>();
            noiseGenerator.ComputeNoises(gridManager.GridSize);

            biosphere.ValidateWithDefault();
            biosphere.SetBiomeData(ref noiseGenerator, gridManager.GridSize);
        }

        public void GenerateSelectedMap()
        {
            Init();          

            if (Map2Generate == MapType.GridMapMaker)
            {
                GeneratePlanet_GMM();
            }
            else if(Map2Generate == MapType.UnityTilemap)
            {
                GeneratePlanet_UTM();
            }
        }
        public void GeneratePlanet_GMM()
        {
            TimeLogger.StartTimer(5234, "Gridmap Generation");

            gridManager.Initialize();
            gridManager.CreateLayer(layerSettings);

            ShapeVisualData vData;
            Vector2Int pos;

            if(GmmBlockInsert)
            {
                (List<Vector2Int>, List<ShapeVisualData>) biomeData = biosphere.GetBiomeData();

                gridManager.InsertPositionBlock(biomeData.Item1, biomeData.Item2);
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
                    }
                }
            }

            gridManager.DrawGrid();

            TimeLogger.Log(5234);
            TimeLogger.ClearTimers();
        }

        public void GeneratePlanet_UTM()
        {
            TimeLogger.StartTimer(871, "Tilemap Generation");

            BaseTile b = Instantiate(baseTile) as BaseTile;
            Color color;

            for (int x = 0; x < gridManager.GridSize.x; x++)
            {
                for (int y = 0; y < gridManager.GridSize.y; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    color = biosphere.GetBiomeColor((Vector2Int)pos);

                    baseTileMap.SetTile(pos, b);
                    baseTileMap.RefreshTile(pos);
                    baseTileMap.SetColor(pos, color);
                }
            }

            TimeLogger.Log(871);

            TimeLogger.ResetTimer(871);
        }

        public void ClearGrid()
        {
            gridManager.Clear();
            baseTileMap.ClearAllTiles();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GridPlanet))]
    public class ClassButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GridPlanet myScript = (GridPlanet)target;

            if (GUILayout.Button("Generate Selected Map"))
            {
                myScript.GenerateSelectedMap();
            }

            if (GUILayout.Button("Generate Both Maps"))
            {
                myScript.Init();
                myScript.GeneratePlanet_UTM();
                myScript.GeneratePlanet_GMM();
            }

            if (GUILayout.Button("Clear Grid"))
            {
                myScript.ClearGrid();
            }
        }
    }
#endif


}
