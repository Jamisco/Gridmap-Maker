using UnityEngine;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using System.Collections.Generic;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Gridmap_Assets.Scripts.Mapmaker;
using System;
using static Assets.Gridmap_Assets.Scripts.Mapmaker.LayeredMesh;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.GridMapMaker
{
    [Serializable]
    public class GridManager : MonoBehaviour
    {
        public Vector2Int GridSize;
        
        private List<GridChunk> gridChunks = new List<GridChunk>();

        [SerializeReference]
        List<VisualProperties> visualProperties = new List<VisualProperties>();

        [SerializeField]
        public MapVisualContainer visualContainer;

        LayeredMesh layer;

        private void CreateLayer()
        {
            GameObject obj = new GameObject();
            obj.transform.parent = transform;
            obj.name = "Layer";
            layer = obj.AddComponent<LayeredMesh>();
        }

        public void GenerateGrid(GridShape shape) 
        {
            Clear();
            CreateLayer();

            if (visualProperties == null)
            {
                visualProperties = new List<VisualProperties>();
            }

            visualProperties.Clear();

            BasicVisual data;

            Material material = visualContainer.GetRandomObject<Material>();

            layer.Initialize(shape);

            for (int x = 0; x < GridSize.x; x++)
            {
                MakeRandomData();
                visualProperties.Add(data);
                
                for (int y = 0; y < GridSize.y; y++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);
                    layer.InsertVisualData(data, gridPosition);
                }
            }

            layer.UpdateMesh();

            void MakeRandomData()
            {
                bool texture = UnityEngine.Random.Range(0, 2) == 0 ? true : false;

                if (texture)
                {
                    Texture2D T = visualContainer.GetRandomObject<Texture2D>();

                    data = new BasicVisual(material, T, Color.white);

                    visualProperties.Add(data);
                }
                else
                {
                    Color C = UnityEngine.Random.ColorHSV();
                    data = new BasicVisual(material, null, C);
                }
            }
        }
        public void GenerateGrid<T>(GridShape shape, T data) where T : VisualProperties
        {
            Clear();
            CreateLayer();

            if(visualProperties == null)
            {
                visualProperties = new List<VisualProperties>();
            }

            visualProperties.Clear();
            visualProperties.Add(data);
            
            layer.Initialize(shape);

            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);
                    layer.InsertVisualData((T)data, gridPosition);
                }
            }

            layer.UpdateMesh();
        }
        public void GenerateGrid(GridShape shape, VisualProperties data)
        {
            Clear();
            CreateLayer();

            layer.Initialize(shape);

            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);
                    layer.InsertVisualData(data, gridPosition);
                }
            }

            layer.UpdateMesh();
        }
        public void Clear()
        {
#if UNITY_EDITOR
            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
#else
            Destroy(transform.GetChild(0).gameObject);
#endif

            gridChunks.Clear();
            visualProperties.Clear();
        }

        [SerializeField]
        public string saveLoc;
        public void SerializeMap()
        {
            SavedMap savedMap = new SavedMap(GridSize, visualProperties, layer.SerializeLayer());

            savedMap.SerializeVisualProperties(visualContainer);

            string json = JsonUtility.ToJson(savedMap, true);
            
            System.IO.File.WriteAllText(saveLoc, json);

            Debug.Log("Map Saved");
        }
        public void DeserializeMap()
        {
            string json = System.IO.File.ReadAllText(saveLoc);

            SavedMap savedMap = JsonUtility.FromJson<SavedMap>(json);

            savedMap.DeserializeVisualProperties(visualContainer);
            
            LoadMap(savedMap);
            
            Debug.Log("Map Loaded");
        }
        void LoadMap(SavedMap savedMap)
        {
            Clear();
            CreateLayer();

            GridSize = savedMap.gridSize;
            visualProperties = savedMap.visualProperties;
   
            layer.Initialize(savedMap.layer, visualProperties);
        }

        [Serializable]
        public struct SavedMap
        {
            public Vector2Int gridSize;
            [SerializeReference]
            public List<VisualProperties> visualProperties;
            public SerializedLayer layer;

            public SavedMap(Vector2Int gridSize, List<VisualProperties> visualProperties, SerializedLayer layer)
            {
                this.gridSize = gridSize;
                this.visualProperties = visualProperties;
                this.layer = layer;
            }

            public void SerializeVisualProperties(MapVisualContainer visualContainer)
            {
                foreach (VisualProperties prop in visualProperties)
                {
                    prop.SerializeData(visualContainer);
                }
            }

            public void DeserializeVisualProperties(MapVisualContainer visualContainer)
            {
                foreach (VisualProperties prop in visualProperties)
                {
                    prop.DeserializeData(visualContainer);
                }
            }

        } 

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GridManager))]
    public class ClassButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

        }
    }
#endif

}