using UnityEngine;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using System.Collections.Generic;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Gridmap_Assets.Scripts.Mapmaker;
using System;
using static Assets.Gridmap_Assets.Scripts.Mapmaker.MeshLayer;
using System.Linq;
using static Assets.Scripts.GridMapMaker.GridChunk;
using Assets.Scripts.Miscellaneous;
using Unity.IO.LowLevel.Unsafe;
using static Assets.Scripts.Miscellaneous.HexFunctions;
using UnityEngine.TextCore.Text;
using static Assets.Scripts.Miscellaneous.ExtensionMethods;
using static UnityEditor.Experimental.GraphView.GraphView;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Assets.Gridmap_Assets.Scripts.Miscellaneous;
using static UnityEngine.Rendering.VolumeComponent;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Assets.Scripts.GridMapMaker
{
    [Serializable]
    public class GridManager : MonoBehaviour
    {
        /// <summary>
        /// String is merely used as a place holder
        /// </summary>
        public const string USE_DEFAULT_LAYER = "12345USE_DEFAULT_LAYER12345";

        [SerializeField]
        private Vector2Int gridSize;

        [SerializeField]
        private Vector2Int chunkSize;

        private string defaultLayerId;
        public string DefaultLayer { get { return defaultLayerId; } }

        public Vector2 CellGap;

        [SerializeReference]
        private MapVisualContainer visualContainer;

        private HashSet<ShapeVisualData> visualProps = new HashSet<ShapeVisualData>();

        [SerializeField]
        private HashSet<GridShape> gridShapes = new HashSet<GridShape>();

        [SerializeField]
        private HashSet<string> layerIds = new HashSet<string>();

        public Vector3 WorldPosition
        {
            get
            {
                return transform.position;
            }
        }

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

        public Vector2Int GridSize { get => gridSize; set => gridSize = value; }
        public Vector2Int ChunkSize { get => chunkSize; set => chunkSize = value; }

        private GridComparer chunkComparer = new GridComparer();
        /// <summary>
        /// Sorts chunks based on their start position.
        /// Bottom Left to Top Right, Horizontally
        /// </summary>
        private SortedDictionary<Vector2Int, GridChunk> sortedChunks;
        /// <summary>
        /// Given a grid gridPosition, gets the start gridPosition of the chunk said grid will be in
        /// </summary>
        /// <param timerName="gridPosition"></param>
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

            // the transform is always relative to the parent such that if the parent is moved, the child moves with it

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
        private GridChunk GetHexChunk(Vector3 localPosition, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);
            // see if a chunk contains a gridposition at that local position
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                if (chunk.ContainsPosition(localPosition, layerId))
                {
                    return chunk;
                }
            }

            return null;
        }
      
        private void ValidateChunkSize()
        {
            // If the chunk size is less than or equal to 0, then the chunk size is the same as the grid size, else if the chunk size is greater than the grid size, then the chunk size is the grid size
            int x = (chunkSize.x <= 0) ? gridSize.x : (chunkSize.x > gridSize.x) ? gridSize.x : chunkSize.x;
            int y = (chunkSize.y <= 0) ? gridSize.y : (chunkSize.y > gridSize.y) ? gridSize.y : chunkSize.y;

            chunkSize = new Vector2Int(x, y);
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
        private void AddLayerToAllGridChunks(string layerId, GridShape shape,
                                ShapeVisualData defaultVisual, bool useVisualEquality = false)
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.AddLayer(layerId, shape, defaultVisual, useVisualEquality);
            }
        }
        public void FillGridChunks_TestMethod(string layerId = USE_DEFAULT_LAYER)
        {
            TimeLogger.StartTimer(42, "Filling Grid Chunks");
            
            ValidateLayerId(ref layerId);
            
            BasicVisual data;

            Material material = visualContainer.GetRandomObject<Material>();

            MakeRandomData();

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                int startX = chunk.StartPosition.x;
                int startY = chunk.StartPosition.y;
                
                int xCount = startX + Mathf.Min(ChunkSize.x, GridSize.x - chunk.StartPosition.x);
                int yCount =  startY + Mathf.Min(ChunkSize.y, GridSize.y - chunk.StartPosition.y);

                Vector2Int gridPosition;

                for (int x = startX; x < xCount; x++)
                {              
                    visualProps.Add(data);

                    for (int y = startY; y < yCount; y++)
                    {
                        MakeRandomData();
                                         
                        gridPosition = new Vector2Int(x, y);

                        chunk.InsertVisualData(layerId, gridPosition, data);                      
                    } 
                }   
            }

            TimeLogger.StopTimer(42);

            UpdateGrid();

            void MakeRandomData()
            {
                bool texture = UnityEngine.Random.Range(0, 2) == 0 ? true : true;
                
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
        
        #region Grid Manipulation
        public void SetVisualContainer(MapVisualContainer container)
        {
            visualContainer = container;
        }
        public void Initialize()
        {
            ValidateChunkSize();
            CreateGridChunks();
        }
        public bool CreateLayer(LayerCreator data)
        {
            return CreateLayer(data.layerId, data.shape, data.defaultVData, data.setBaselayer, data.useVisualEquality);
        }
        
        public bool CreateLayer(string layerId, GridShape shape, ShapeVisualData defaultVData, bool setBaselayer = false, bool useVisualEquality = false)
        {
            if (layerIds.Contains(layerId))
            {
                return false;
            }

            gridShapes.TryGetValue(shape, out GridShape gridShape);

            if (gridShape == null)
            {
                gridShape = shape.Init(shape, CellGap);

                gridShapes.Add(gridShape);
            }

            layerIds.Add(layerId);

            AddLayerToAllGridChunks(layerId, gridShape, defaultVData, useVisualEquality);

            if (setBaselayer || layerIds.Count == 1)
            {
                defaultLayerId = layerId;

                UpdateChunkLocalPosition();
            }

            return true;
        }
        
        /// <summary>
        /// Insert a visual data at a given position. If said position already has a visual data, it is replaced with the given data
        /// </summary>
        /// <param timerName="gridPosition"></param>
        /// <param timerName="data"></param>
        /// <param timerName="layerId"></param>
        public void InsertVisualData(Vector2Int gridPosition, ShapeVisualData data, 
                            string layerId = USE_DEFAULT_LAYER)
        {

            ValidateLayerId(ref layerId);
            
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.InsertVisualData(layerId, gridPosition, data);
                visualProps.Add(data);
            }
        }

        public void InsertVisualData_Block(List<Vector2Int> positions, List<ShapeVisualData> data, string layerId = USE_DEFAULT_LAYER)
        {
            for(int i = 0; i < positions.Count; i++)
            {
                InsertVisualData(positions[i], data[i], layerId);
            }
        }

        /// <summary>
        /// Removes the visual data at the given grid position from the given layer
        /// </summary>
        /// <param timerName="gridPosition"></param>
        /// <param timerName="layerId"></param>
        public void RemoveVisualData(Vector2Int gridPosition, string layerId = USE_DEFAULT_LAYER)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.RemoveVisualData(gridPosition, layerId);
            }
        }
        
    /// <summary>
        /// Removes the visual data at the given grid position from all layers
        /// </summary>
        /// <param timerName="gridPosition"></param>
        public void RemoveVisualData(Vector2Int gridPosition)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.RemoveVisualData(gridPosition);
            }
        }

        public void DeletePosition(Vector2Int gridPosition)
        {
            ValidateLayerId(ref defaultLayerId);

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.DeletePosition(gridPosition);
            }
        }

        public void DeletePosition(Vector2Int gridPosition, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.DeletePosition(gridPosition, layerId);
            }
        }

        /// <summary>
        /// Set whether to use visual equality at the given layer. Note, that you will have to reinsert the visual data so that the changes take effect. It is not enough to call UpdateGrid()
        /// </summary>
        /// <param name="useEquality"></param>
        public void SetVisualEquality(bool useEquality, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            foreach (GridChunk c in sortedChunks.Values)
            {
                c.SetVisualEquality(layerId, useEquality);
            }
        }

        /// <summary>
        /// Set whether to use visual equality at the given layer
        /// </summary>
        /// <param name="useEquality"></param>
        public void SetVisualEquality(bool useEquality)
        {
            GridChunk c = sortedChunks.Values.First();

            c.SetVisualEquality(useEquality);
        }

        /// <summary>
        /// Enable or disable a chunk if the chunk is contained within the given bounds. If invert is true, the status of chunks not in the bounds will be set to the opposite of the given status
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="status"></param>
        /// <param name="invert"></param>
        public void SetStatusIfChunkIsInBounds(Bounds bounds, bool status, bool invert = false)
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                Bounds chunkBounds = chunk.GetDefaultLayerBounds();
                
                if (bounds.Intersects(chunkBounds))
                {
                    chunk.gameObject.SetActive(status);
                }
                else
                {
                    if (invert)
                    {
                        chunk.gameObject.SetActive(!status);
                    }
                }
            }
        }

        /// <summary>
        /// Enable or disable a chunk if the chunk is not contained within the given bounds. If invert is true, the status of chunks that are in the bounds will be set to the opposite of the given status
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="status"></param>
        /// <param name="invert"></param>
        public void SetStatusIfChunkIsNotInBounds(Bounds bounds, bool status,
                                                  bool invert = false)
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                Bounds chunkBounds = chunk.GetDefaultLayerBounds();

                if (!bounds.Intersects(chunkBounds))
                {
                    chunk.gameObject.SetActive(status);
                }
                else
                {
                    if (invert)
                    {
                        chunk.gameObject.SetActive(!status);
                    }
                }
            }
        }

        /// <summary>
        /// Update all layers in a given position
        /// </summary>
        /// <param timerName="gridPosition"></param>
        public void UpdatePosition(Vector2Int gridPosition)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.UpdateLayers();
            }
        }

        // Update a specific layer at a given position
        public void UpdatePosition(Vector2Int gridPosition, string layerId = USE_DEFAULT_LAYER)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.UpdateLayer(layerId);
            }
        }

        /// <summary>
        /// Update the local position of all chunks. Call this when you have set or changed the defaultLayer
        /// </summary>
        private void UpdateChunkLocalPosition()
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.UpdateLocalPosition();
            }
        }

        public void UpdateGrid()
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.UpdateLayers();
            }
        }

        public void RedrawLayer(string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.RedrawLayer(layerId);
            }
        }

        public void RedrawGrid()
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.RedrawChunk();
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
            layerIds.Clear();
            gridShapes.Clear();
        }
        #endregion

        #region Grid Positioning

        /// <summary>
        /// If the layerId is the Base layer id, we simply set the layerID to the actual base layer id
        /// </summary>
        /// <param timerName="layerId"></param>
        private void ValidateLayerId(ref string layerId)
        {
            if (layerId.Equals(USE_DEFAULT_LAYER))
            {
                layerId = DefaultLayer;
            }
        }
        /// <summary>
        /// Loops through all the chunks and returns the shape of the first layer found with the given ID.
        /// </summary>
        /// <param timerName="layerId"></param>
        /// <returns></returns>
        public GridShape GetShape(string layerId = USE_DEFAULT_LAYER)
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
        /// <param timerName="layerId"></param>
        /// <param timerName="gridPosition"></param>
        /// <returns></returns>
        public GridShape GetShape(Vector2Int gridPosition, 
                                        string layerId = USE_DEFAULT_LAYER)
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
        public GridShape GetShape(Vector3 localPosition, 
                            string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(localPosition, layerId);

            GridShape shape = null;

            if (chunk != null)
            {
                chunk.TryGetLayerShape(layerId, out shape);
            }
            
            return shape;
        }

        // use base layer for these positions
        public Vector2Int LocalToGridPosition(Vector3 localPosition, string layerId = USE_DEFAULT_LAYER)
        {
            GridChunk chunk = GetHexChunk(localPosition, layerId);

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
            GridShape shape = GetShape(gridPosition);

            return shape.GetTesselatedPosition(gridPosition);
        }

        /// <summary>
        /// Will get the visualdata at the given layer and gridPosition and return a CLONE of it.
        /// This is to allow you to modify the visual data without modifying other shapes with thesame visual data
        /// </summary>
        /// <typeparam timerName="T"></typeparam>
        /// <param timerName="gridPosition"></param>
        /// <param timerName="layerId"></param>
        /// <returns></returns>
        public T GetVisualProperties<T>(Vector2Int gridPosition,
                                    string layerId = USE_DEFAULT_LAYER) where T : ShapeVisualData
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                return chunk.GetVisualProperties(gridPosition, layerId) as T;
            }

            return null;
        }

        /// <summary>
        /// Will get the visualdata at the given layer and gridPosition.
        /// Modifying and Updating said visualData will consequently modify all shapes that used said visualData...use with care
        /// </summary>
        /// <param timerName="gridPosition"></param>
        /// <param timerName="layerId"></param>
        /// <returns></returns>
        public ShapeVisualData GetVisualProperties(Vector2Int gridPosition,
                                                    string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);
            
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                return chunk.GetVisualProperties(gridPosition, layerId);
            }

            return null;
        }
        public void VisualIdChanged()
        {
            foreach (ShapeVisualData vp in visualProps)
            {
                vp.VisualIdChanged();
            }
        }

        #endregion

        #region Sprite Spawning

        public void SpawnSprite(Vector2Int gridPosition, Sprite sprite)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.SpawnSprite(gridPosition, sprite);
            }
        }
        #endregion

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
        public List<ShapeVisualData> GetUniqueVisuals()
        {
            return visualProps.ToList();
        }

            [Serializable]
        public struct SavedMap
        {
            public Vector2Int gridSize;

            [SerializeField]
            public List<SerializedGridChunk> serializedChunk;

            [SerializeReference]
            public List<ShapeVisualData> visualProps;

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
                foreach (ShapeVisualData visual in visualProps)
                {
                    visual.SetSerializeData(container);
                }
            }

            public void DeserializeVisualProps(MapVisualContainer container)
            {
                foreach (ShapeVisualData visual in visualProps)
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