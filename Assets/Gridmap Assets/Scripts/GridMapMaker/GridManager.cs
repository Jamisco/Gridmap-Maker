using UnityEngine;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using System.Collections.Generic;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Gridmap_Assets.Scripts.Mapmaker;
using System;
using static Assets.Gridmap_Assets.Scripts.Mapmaker.LayeredMesh;
using System.Linq;
using static Assets.Scripts.GridMapMaker.GridChunk;
using Assets.Scripts.Miscellaneous;
using Unity.IO.LowLevel.Unsafe;
using static Assets.Scripts.Miscellaneous.HexFunctions;
using UnityEngine.TextCore.Text;
using static Assets.Scripts.Miscellaneous.ExtensionMethods;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.GridMapMaker
{
    [Serializable]
    public class GridManager : MonoBehaviour
    {
        public const string USE_BASE_LAYER = "Base Layer";

        public Vector2Int GridSize;
        public Vector2Int ChunkSize;

        private GridShape baseShape;
        public GridShape BaseShape { get { return baseShape; } }

        private string baseLayer;
        public string BaseLayer { get { return baseLayer; } }

        [SerializeReference]
        private MapVisualContainer visualContainer;

        public HashSet<VisualProperties> visualProps = new HashSet<VisualProperties>();

        private void OnValidate()
        {
            if (sortedChunks == null)
            {
                sortedChunks = new SortedDictionary<Vector2Int, GridChunk>(chunkComparer);
            }
        }

        #region Chunk Related Code

        /// <summary>
        /// The total number of grids in a chunk
        /// </summary>
        public int ChunkGridCount
        {
            get
            {
                return ChunkSize.x * ChunkSize.y;
            }
        }

        private GridComparer chunkComparer = new GridComparer();
        /// <summary>
        /// Sorts chunks based on their start position.
        /// Bottom Left to Top Right, Horizontally
        /// </summary>
        private SortedDictionary<Vector2Int, GridChunk> sortedChunks;
        /// <summary>
        /// Given a grid gridPosition, gets the start gridPosition of the chunk said grid will be in
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <returns></returns>
        private Vector2Int GetChunkStartPosition(Vector2Int gridPosition)
        {
            int x = Mathf.FloorToInt((float)gridPosition.x / ChunkGridCount);
            int y = Mathf.FloorToInt((float)gridPosition.y / ChunkGridCount);

            Vector2Int start = new Vector2Int();

            start.x = ChunkSize.x * x;
            start.y = ChunkSize.y * y;

            return start;
        }
        private BoundsInt GetChunkBounds(Vector2Int gridPosition)
        {
            Vector3Int start = GetChunkStartPosition(gridPosition).ToBoundsPos();

            return new BoundsInt(start, ChunkSize.ToBoundsPos());
        }
        private GridChunk CreateHexChunk(Vector2Int gridPosition, GridChunk prefab)
        {
            GridChunk chunk;

            BoundsInt chunkBounds = GetChunkBounds(gridPosition);
            
            chunk = Instantiate(prefab, transform);
            chunk.Initialize(this, chunkBounds);

            return chunk;
        }
        private GridChunk GetHexChunk(Vector2Int gridPosition)
        {
            Vector2Int startPosition = GetChunkStartPosition(gridPosition);

            GridChunk chunk = null;

            sortedChunks.TryGetValue(startPosition, out chunk);

            return chunk;
        }

        private GridChunk GetHexChunk(Vector3 localPosition)
        {
            // see if a chunk contains a gridposition at that local position
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                if (chunk.ContainsPosition(localPosition))
                {
                    return chunk;
                }
            }

            return null;
        }

        private void CreateGridChunks()
        {
            Clear();
            
            int xCount = Mathf.CeilToInt(GridSize.x / ChunkSize.x);
            int yCount = Mathf.CeilToInt(GridSize.y / ChunkSize.y);

            GridChunk prefab = new GameObject().AddComponent<GridChunk>();

            int count = 0;
            
            for (int y = 0; y < yCount; y++)
            {
                for (int x = 0; x < xCount; x++)
                {
                    Vector2Int start = new Vector2Int();

                    start.x = x * ChunkGridCount;
                    start.y = y * ChunkGridCount;

                    GridChunk chunk = CreateHexChunk(start, prefab);

                    chunk.name = "Chunk " + count;

                    count++;

                    sortedChunks.Add(chunk.StartPosition, chunk);
                }
            }

            DestroyImmediate(prefab.gameObject);

            Debug.Log("Chunks Created: " + count);
        }
        private void AddLayerToAllGridChunks(string layerId, GridShape shape, VisualProperties defaultVisual)
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.AddLayer(layerId, shape, defaultVisual);
            }
        } 
        private void FillGridChunks(string layerId)
        {
            BasicVisual data;

            Material material = visualContainer.GetRandomObject<Material>();

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                int startX = chunk.StartPosition.x;
                int startY = chunk.StartPosition.y;
                
                int xCount = startX + Mathf.Min(ChunkSize.x, GridSize.x - chunk.StartPosition.x);
                int yCount =  startY + Mathf.Min(ChunkSize.y, GridSize.y - chunk.StartPosition.y);

                Vector2Int gridPosition;

                for (int x = startX; x < xCount; x++)
                {
                    MakeRandomData();
                    visualProps.Add(data);

                    for (int y = startY; y < yCount; y++)
                    {
                        gridPosition = new Vector2Int(x, y);

                        chunk.InsertVisualData(layerId, gridPosition, data);                      
                    }
                }

                chunk.UpdateLayers();
            }

            Debug.Log("Total Unique Visuals: " + visualProps.Count);

            void MakeRandomData()
            {
                bool texture = UnityEngine.Random.Range(0, 2) == 0 ? true : false;
                texture = true;
                
                if (texture)
                {
                    Texture2D T = visualContainer.GetRandomObject<Texture2D>();

                    data = new BasicVisual(material, T, Color.white);
                }
                else
                {
                    Color C = UnityEngine.Random.ColorHSV();
                    data = new BasicVisual(material, null, C);
                }
            }
        }


        #endregion

        #region Hex Data

        /// <summary>
        /// If the layerId is the Base layer id, we simply set the layerID to the actual base layer id
        /// </summary>
        /// <param name="layerId"></param>
        private void ValidateLayerId(ref string layerId)
        {
            if (layerId.Equals(USE_BASE_LAYER))
            {
                layerId = BaseLayer;
            }
        }
        /// <summary>
        /// Loops through all the chunks and returns the shape of the first layer found with the given ID.
        /// </summary>
        /// <param name="layerId"></param>
        /// <returns></returns>
        public GridShape GetLayerShape(string layerId = USE_BASE_LAYER)
        {
            ValidateLayerId(ref layerId);

            GridShape shape = null;

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.TryGetLayerShape(layerId, out shape);
            }

            return shape;
        }

        /// <summary>
        /// Returns the shape of the layer at the given grid position.
        /// </summary>
        /// <param name="layerId"></param>
        /// <param name="gridPosition"></param>
        /// <returns></returns>
        public GridShape GetLayerShape(Vector2Int gridPosition, 
                                        string layerId = USE_BASE_LAYER)
        {
            ValidateLayerId(ref layerId);
            
            GridShape shape = null;

            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.TryGetLayerShape(layerId, out shape);
            }

            return shape;
        }

        public GridShape GetLayerShape(Vector3 localPosition, 
                            string layerId = USE_BASE_LAYER)
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(localPosition);

            GridShape shape = null;

            if (chunk != null)
            {
                chunk.TryGetLayerShape(layerId, out shape);
            }
            
            return shape;
        }

        public Vector2Int LocalToGridPosition(Vector3 localPosition)
        {
            GridChunk chunk = GetHexChunk(localPosition);

            Vector2Int gridPosition = Vector2Int.left;

            if (chunk != null)
            {
               chunk.TryGetGridPosition(localPosition, out gridPosition);
            }

            return gridPosition;
        }
        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            return LocalToGridPosition(localPosition);
        }

        public Vector3 WorldToLocalPosition(Vector3 worldPosition)
        {
            return transform.InverseTransformPoint(worldPosition);
        }

        public Vector3 LocalToWorldPosition(Vector3 localPosition)
        {
            return transform.TransformPoint(localPosition);
        }

        public Vector3 GetLocalPosition(Vector2Int gridPosition)
        {
            GridShape shape = GetLayerShape(gridPosition);

            return shape.GetTesselatedPosition(gridPosition);
        }

        /// <summary>
        /// Will get the visualdata at the given layer and gridPosition and return a CLONE of it.
        /// This is to allow you to modify the visual data without modifying other shapes with thesame visual data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gridPosition"></param>
        /// <param name="layerId"></param>
        /// <returns></returns>
        public T GetVisualProperties_Clone<T>(Vector2Int gridPosition,
                                    string layerId = USE_BASE_LAYER) where T : VisualProperties
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                return chunk.GetVisualProperties(gridPosition, layerId).ShallowCopy<T>();
            }

            return null;
        }

        /// <summary>
        /// Will get the visualdata at the given layer and gridPosition.
        /// Modifying and Updating said visualData will consequently modify all shapes that used said visualData...use with care
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="layerId"></param>
        /// <returns></returns>
        public VisualProperties GetVisualProperties(Vector2Int gridPosition,
                                                    string layerId = USE_BASE_LAYER)
        {
            ValidateLayerId(ref layerId);
            
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                return chunk.GetVisualProperties(gridPosition, layerId);
            }

            return null;
        }

        #endregion

        public void SetBaseData(GridShape baseShape, string baseLayer)
        {

        }
        public void GenerateGrid(GridShape shape, string layerId) 
        {
            baseShape = shape;
            baseLayer = layerId;

            Material mat = visualContainer.GetRandomObject<Material>();

            DefaultVisual def = new DefaultVisual(mat, Color.green);

            CreateGridChunks();
            AddLayerToAllGridChunks(layerId, shape, def);
            FillGridChunks(layerId);
        }
        public void InsertVisualData(Vector2Int gridPosition, BasicVisual data, 
                            string layerId = USE_BASE_LAYER)
        {

            ValidateLayerId(ref layerId);
            
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.InsertVisualData(layerId, gridPosition, data);
                visualProps.Add(data);
            }
        }

        /// <summary>
        /// Removes the visual data at the given grid position from the given layer
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="layerId"></param>
        public void RemoveVisualData(Vector2Int gridPosition, string layerId = USE_BASE_LAYER)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.RemoveVisualData(layerId, gridPosition);
            }
        }
        
        /// <summary>
        /// Removes the visual data at the given grid position from all layers
        /// </summary>
        /// <param name="gridPosition"></param>
        public void RemoveVisualData_AL(Vector2Int gridPosition)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.RemoveVisualData_AL(gridPosition);
            }
        }
        public void UpdateWholeChunk(Vector2Int gridPosition)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.UpdateLayers();
            }
        }
        public void UpdateChunkLayer(Vector2Int gridPosition, string layerId = USE_BASE_LAYER)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.UpdateLayer(layerId);
            }
        }
        public void UpdateWholeGrid()
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.UpdateLayers();
            }
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

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.Clear();
            }

            sortedChunks.Clear();
            visualProps.Clear();
        }


        #region Saving and Loading Map

        [SerializeField]
        public string saveLoc;
        public void SerializeMap()
        {
            SavedMap savedMap = new SavedMap(this);
            
            string json = JsonUtility.ToJson(savedMap, true);
            
            System.IO.File.WriteAllText(saveLoc, json);

            Debug.Log("Map Saved");
        }
        public void DeserializeMap()
        {
            string json = System.IO.File.ReadAllText(saveLoc);

            SavedMap savedMap = JsonUtility.FromJson<SavedMap>(json);

            savedMap.DeserializeVisualProps(visualContainer);

            visualProps = savedMap.visualProps.ToHashSet();

            LoadMap(savedMap);
            
            Debug.Log("Map Loaded");
        }
        public void LoadMap(SavedMap savedMap)
        {
            Clear();

            GridSize = savedMap.gridSize;

            foreach (SerializedGridChunk item in savedMap.serializedChunk)
            {
                GridSize = savedMap.gridSize;
                GridChunk grid 
                    = GridChunk.DeserializeData(item, visualContainer,savedMap.visualProps);
                
                grid.transform.parent = transform;
                sortedChunks.Add(grid.StartPosition, grid);
            }
        }

        public void CheckVisualHashChanged()
        {
            foreach (VisualProperties vp in visualProps)
            {
                vp.CheckVisualHashChanged();
            }
        }

            [Serializable]
        public struct SavedMap
        {
            public Vector2Int gridSize;

            [SerializeField]
            public List<SerializedGridChunk> serializedChunk;

            [SerializeReference]
            public List<VisualProperties> visualProps;

            public SavedMap(GridManager gridManager)
            {
                this.gridSize = gridManager.GridSize; ;
                
                visualProps = gridManager.visualProps.ToList();

                serializedChunk = new List<SerializedGridChunk>();

                foreach (GridChunk item in gridManager.sortedChunks.Values)
                {
                    serializedChunk.Add(item.SerializeChunk(gridManager.visualContainer));
                }
                
                SerializeVisualProps(gridManager.visualContainer);
            }

            public void SerializeVisualProps(MapVisualContainer container)
            {
                foreach (VisualProperties visual in visualProps)
                {
                    visual.SetSerializeData(container);
                }
            }

            public void DeserializeVisualProps(MapVisualContainer container)
            {
                foreach (VisualProperties visual in visualProps)
                {
                    visual.DeserializeData(container);
                }
            }
        }
        
        #endregion

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