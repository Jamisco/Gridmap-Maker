using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Debug = UnityEngine.Debug;
using System.Collections.Concurrent;
using System.Threading.Tasks;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GridMapMaker
{
    public enum SortAxis { X, Y, Z }

    /// <summary>
    /// The gridmanager contains all the fields, methods and data that is used to generate the map.
    /// It is the main class that is used to create and manipulate the grid.
    /// </summary>
    [Serializable]
    public class GridManager : MonoBehaviour
    {
        /// <summary>
        /// String is merely used as a place holder
        /// </summary>
        public const string USE_DEFAULT_LAYER = "12345USE_DEFAULT_LAYER12345";

        /// <summary>
        /// Size of the grid
        /// </summary>
        [SerializeField]
        private Vector2Int gridSize;

        /// <summary>
        /// Size of the chunks in a grid.
        /// Set to (0, 0) if you dont want chunks.
        /// If the chunk size is greater than the grid size, then the chunk size is the grid size
        /// </summary>
        [SerializeField]
        private Vector2Int chunkSize;

        /// <summary>
        /// The scale of the shape. Use this to make increase or reduce the size of your shapes
        /// </summary>
        [SerializeField]
        private Vector2 shapeScale = Vector2.one;

        private Bounds localBounds;

        /// <summary>
        /// The bounds of the grid in local position. The dimensions are determined by the shape of the default layer
        /// </summary>
        public Bounds LocalBounds => localBounds;

        private BoundsInt gridBounds;

        /// <summary>
        /// The bounds of the grid size. So if your grid size is (10, 10), then the bounds will be (0, 0, 0) to (10, 10, 0)
        /// </summary>
        public BoundsInt GridBounds => gridBounds;

        private string defaultLayerId;

        /// <summary>
        /// This is the default layer in which modifications will be made if no layer is specified.
        /// Additionally, this layer is used to determine the bounds of the map
        /// </summary>
        public string DefaultLayer { get { return defaultLayerId; } }

        /// <summary>
        /// The gap between cells
        /// </summary>
        public Vector2 cellGap;


        /// <summary>
        /// This is a shader that will instruct a shape to be drawn with a color only.
        /// This shader is required and must be set. You are provided a shader called "MeshColorShader", if you dont have a color shader. You can also use Unity's Sprites default shader. This shader will also serve as the default shader for all shapes if you do not provide a default visual data.
        /// </summary>
        [SerializeField]
        private Shader colorShader;
        /// <summary>
        /// Every gridmanager must have a color shader. A color shader will be used to render shapes with a specified color when the shape has no visualData You are provided a color shader called "MeshColorShader", I recommend you use that.
        /// </summary>
        public Shader ColorShader { get => colorShader; set => colorShader = value; }

        [SerializeField]
        [HideInInspector]
        private ShapeVisualData defaultVisualData;

        /// <summary>
        /// The default visual data to use when no visual data is specified,
        /// If no default visual data is set, then a color visual data with the default shader and color white is used
        /// </summary>
        public ShapeVisualData DefaultVisualData
        {
            get
            {
                if (defaultVisualData == null)
                {
                    // you can change the color to whatever else you want
                    defaultVisualData = new ColorVisualData(ColorShader, Color.white);
                }

                return defaultVisualData;
            }
            set
            {
                defaultVisualData = value;
            }
        }

        /// <summary>
        /// The map can be displayed in different orientations. The default orientation is XY.
        /// If you changed the orientation, be sure to call the ValidateOrientation method for your changes to take effect
        /// </summary>
        [SerializeField]
        public GridShape.Orientation MapOrientation = GridShape.Orientation.XY;

        [SerializeReference]
        private MapVisualContainer visualContainer;

        private HashSet<ShapeVisualData> visualDatas = new HashSet<ShapeVisualData>();

        [SerializeField]
        private HashSet<GridShape> gridShapes = new HashSet<GridShape>();

        [SerializeField]
        private Dictionary<string, MeshLayerSettings> meshLayerInfos
                            = new Dictionary<string, MeshLayerSettings>();

        private SortAxis layerSortAxis;

        /// <summary>
        /// The sort axis indicates which axis to sort by. For example, if the map is displayed along the XY axis, then the sort axis will be Z because we can control which layer is in front or behind by moving the layers along the Z axis
        /// </summary>
        public SortAxis LayerSortAxis => layerSortAxis;
        public Vector3 WorldPosition
        {
            get
            {
                return transform.position;
            }
        }
        /// <summary>
        /// When the gridmanager is doing various operations such as fusing and drawing the meshes, we can use multithreading to speed up the process. This may be stable on a case by case basis. However So as long as you are not using Unity objects outside of main thread, you should be fine.
        /// </summary>
        public bool UseMultithreading = true;

        /// <summary>
        /// When the visual hash of any visual data changes, the chunk will redraw all layers in which said visual data is in. This is useful if you want the changes in visual data to immediately be reflected in the chunk.
        /// For example, say I have a ColorVisualData with the color set to white, the moment I change the color to blue, all cells using said visualData should auto change
        /// </summary>
        public bool RedrawOnVisualHashChanged = true;
        private void OnValidate()
        {
            if (sortedChunks == null)
            {
                sortedChunks = new Dictionary<int, GridChunk>();
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
        /// <summary>
        /// Sorts chunks based on their start position.
        /// Bottom Left to Top Right, Horizontally
        /// </summary>
        private Dictionary<int, GridChunk> sortedChunks = new Dictionary<int, GridChunk>();
        /// <summary>
        /// Given a chunk gridPosition, gets the start gridPosition of the chunk said chunk will be in
        /// </summary>
        /// <param timerName="gridPosition"></param>
        /// <returns></returns>
        private Vector2Int GetChunkStartPosition(Vector2Int gridPosition)
        {
            int x = Mathf.FloorToInt((float)gridPosition.x / chunkSize.x);
            int y = Mathf.FloorToInt((float)gridPosition.y / chunkSize.y);

            Vector2Int start = new Vector2Int();

            start.x = ChunkSize.x * x;
            start.y = ChunkSize.y * y;

            return start;
        }
        private GridChunk CreateHexChunk(Vector2Int gridPosition, GridChunk prefab)
        {
            GridChunk chunk;

            Vector3Int start = (Vector3Int)GetChunkStartPosition(gridPosition);

            BoundsInt chunkBounds = new BoundsInt(start, (Vector3Int)ChunkSize);

            chunk = Instantiate(prefab, transform, true);

            // the transform is always relative to the parent such that if the parent is moved, the child moves with it

            chunk.Initialize(this, chunkBounds);

            return chunk;
        }
        private GridChunk GetHexChunk(Vector2Int gridPosition)
        {
            Vector2Int startPosition = GetChunkStartPosition(gridPosition);

            GridChunk chunk = null;

            sortedChunks.TryGetValue(startPosition.GetHashCode_Unique(), out chunk);

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
            // If the chunk size is less than or equal to 0, then the chunk size is the same as the chunk size, else if the chunk size is greater than the chunk size, then the chunk size is the chunk size
            int x = (chunkSize.x <= 0) ? gridSize.x : (chunkSize.x > gridSize.x) ? gridSize.x : chunkSize.x;
            int y = (chunkSize.y <= 0) ? gridSize.y : (chunkSize.y > gridSize.y) ? gridSize.y : chunkSize.y;

            chunkSize = new Vector2Int(x, y);

            gridBounds = new BoundsInt(Vector3Int.zero, (Vector3Int)gridSize);

            gridBounds.zMin = 0;
            gridBounds.zMax = 1;
        }
        private void CreateGridChunks()
        {
            Clear();

            int xCount = Mathf.CeilToInt((float)GridSize.x / ChunkSize.x);
            int yCount = Mathf.CeilToInt((float)GridSize.y / ChunkSize.y);

            GridChunk prefab = new GameObject().AddComponent<GridChunk>();

            int count = 0;

            for (int y = 0; y < yCount; y++)
            {
                for (int x = 0; x < xCount; x++)
                {
                    Vector2Int start = new Vector2Int();

                    start.x = x * chunkSize.x;
                    start.y = y * chunkSize.y;

                    GridChunk chunk = CreateHexChunk(start, prefab);

                    chunk.name = "Chunk " + count;

                    count++;

                    sortedChunks.Add(chunk.StartPosition.GetHashCode_Unique(), chunk);
                }
            }

            DestroyImmediate(prefab.gameObject);
        }

        #endregion

        #region Grid Manipulation
        public void SetVisualContainer(MapVisualContainer container)
        {
            visualContainer = container;
        }

        /// <summary>
        /// This is used to simply store the color shader for serialization and deserialization purposes
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private string colorShaderName;
        private const string spriteDefault = "Sprites/Default";
        private const string gmmColorShader = "GridMapMaker/ColorShader";
        /// <summary>
        /// Initializes the grid with its current settings. This method assumes the settings have been set from the editor. Be advised, most settings of the grid manager are used to determine how the grid is displayed. Thus, if you change any of these settings, after the grid has been initialized, it will have no effect or cause errors. For example, setting the chunkSize, or gridSize or cellGap will have no effect on a map that is already created. However, it will cause various errors if you were to do bounds checking or grid positioning etc.
        /// </summary>
        public void Initialize()
        {
            ValidateChunkSize();
            CreateGridChunks();

            if(colorShader == null)
            {
                colorShader = Shader.Find(gmmColorShader);

                if(colorShader == null)
                {
                    colorShader = Shader.Find(spriteDefault);
                }
               
                if(colorShader == null)
                {
                    colorShaderName = "";

                    Debug.LogError("No color shader found. Please provide a color shader. Make sure you add your color shader to the build list in your editor settings so they can be used via build");
                    return;
                }

                colorShaderName = colorShader.name;

            }
        }

        /// <summary>
        /// Initializes the grid with the minimum settings required to create a grid
        /// </summary>
        /// <param name="gridSize"></param>
        /// <param name="chunkSize"></param>
        /// <param name="colorShader"></param>
        public void Initialize(Vector2Int gridSize, Vector2Int chunkSize)
        {
            GridSize = gridSize;
            ChunkSize = chunkSize;

            Initialize();
        }

        /// <summary>
        /// Creates and adds a new layer to the grid. If the layer already exists, it will return false.
        /// The shape provided in the MeshLayerSettings will be cloned. If said shape already exists in the grid, the existing shape will be used. If this is the first layer to be added, then it will be set as the default layer
        /// </summary>
        /// <param name="layerInfo"></param>
        /// <param name="setBaselayer"></param>
        /// <returns></returns>
        public bool CreateLayer(MeshLayerSettings layerInfo, bool setBaselayer = false)
        {
            if (meshLayerInfos.ContainsKey(layerInfo.LayerId))
            {
                return false;
            }

            gridShapes.TryGetValue(layerInfo.Shape, out GridShape gridShape);

            if (gridShape == null)
            {
                gridShape = Instantiate(layerInfo.Shape);
                gridShape.CellGap = cellGap;
                gridShape.ShapeOrientation = MapOrientation;
                gridShape.Scale = shapeScale;

                gridShape.UpdateShape();

                layerInfo.Shape = gridShape;

                gridShapes.Add(gridShape);
            }

            meshLayerInfos.Add(layerInfo.LayerId, layerInfo);

            // add layer to all chunk chunks
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.AddLayer(layerInfo);
            }

            if (setBaselayer || meshLayerInfos.Count == 1)
            {
                defaultLayerId = layerInfo.LayerId;

                ValidateChunkPositions();
                ValidateGridBounds();
            }

            return true;
        }
        public MeshLayerSettings GetLayerInfo(string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            return meshLayerInfos[layerId];

        }

        /// <summary>
        /// Will sort the layers based on the orderInLayer and the grid layerSortAxis. The layer with the lowest orderInLayer will be at the back, while the layer with the highest orderInLayer will be at the front
        /// </summary>
        public void SortMeshLayers()
        {
            if (meshLayerInfos.Count == 0)
            {
                return;
            }

            List<MeshLayerSettings> orderedLayers = meshLayerInfos.Values.OrderBy(x => x.OrderInLayer).ToList();

            int i = 0;

            int previousOrder = int.MinValue;
            float offset = 0;

            // the reason we have this is because, consecutive calls to sort the layers will result in the offset position progressively getting higher. Thus, we assume that the position of the parent gridObject is the absolute lowest position all mesh layers will start from.
            float baseLocation = 0;
            Vector3 gridPos = transform.position;

            // the orientation of the map determines which axis we must sort againsts.
            // For example, if the chunk is displayed along the XY axis, then we determine sorting by moving the layers forward or backward on the Z axis
            if (MapOrientation == GridShape.Orientation.XY)
            {
                layerSortAxis = SortAxis.Z;
            }
            else
            {
                layerSortAxis = SortAxis.Y;
            }

            switch (layerSortAxis)
            {
                case SortAxis.X:
                    baseLocation += gridPos.x;
                    break;
                case SortAxis.Y:
                    baseLocation += gridPos.y;
                    break;
                case SortAxis.Z:
                    baseLocation += gridPos.z;
                    break;
                default:
                    break;
            }

            foreach (MeshLayerSettings layer in orderedLayers)
            {
                int order = layer.OrderInLayer;

                if (order > previousOrder)
                {
                    offset = MeshLayerSettings.SortStep * i++;
                }

                foreach (GridChunk chunk in sortedChunks.Values)
                {
                    chunk.SortLayer(layer.LayerId, layerSortAxis, offset);
                }

                previousOrder = order;
            }
        }

        /// <summary>
        /// Will return a dictionary, (layerId, WorldPosition) for each layer in the grid. This is useful if you want to know the position of the layers such that you can know how/where to place other objects such as sprites
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Vector3> GetMeshLayerPositions()
        {
            GridChunk chunks = sortedChunks.Values.First();
            Dictionary<string, Vector3> mlPos = new Dictionary<string, Vector3>();

            foreach (MeshLayerSettings info in meshLayerInfos.Values)
            {
                MeshLayer ml = chunks.GetMeshLayer(info.LayerId);
                mlPos.Add(info.LayerId, ml.gameObject.transform.position);
            }

            return mlPos;
        }

        /// <summary>
        /// Insert a visual data at a given position. If said position already has a visual data, it is replaced with the given data
        /// </summary>
        /// <param timerName="gridPosition"></param>
        /// <param timerName="data"></param>
        /// <param timerName="LayerId"></param>
        public void InsertVisualData(Vector2Int gridPosition, ShapeVisualData data,
                            string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.InsertVisualData(gridPosition, data, layerId);
                visualDatas.Add(data);
            }
        }

        /// <summary>
        /// Insert a colorVisualData with the specified color at the given grid position
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="color"></param>
        /// <param name="layerId"></param>
        public void InsertVisualData(Vector2Int gridPosition, Color color, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                ColorVisualData data = new ColorVisualData(ColorShader, color);

                chunk.InsertVisualData(gridPosition, data, layerId);
                visualDatas.Add(data);
            }
        }
        /// <summary>
        /// The multithreading version of the InsertPostionBlock method. This method is faster than the non-multithreading version. However, may or may not work (most likely will) on some computers.
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="datas"></param>
        /// <param name="layerId"></param>
        private void InsertPositionBlock_Fast(List<Vector2Int> positions, List<ShapeVisualData> datas, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            if (positions.Count != datas.Count)
            {
                Debug.LogError("When Inserting as Block, The number of positions and datas must be the same");
                return;
            }

            Dictionary<GridChunk, ConcurrentBag<int>> chunkIndex = new Dictionary<GridChunk, ConcurrentBag<int>>();

            ConcurrentDictionary<ShapeVisualData, byte> vDatas = new ConcurrentDictionary<ShapeVisualData, byte>();

            foreach (GridChunk item in sortedChunks.Values)
            {
                if (item.HasLayer(layerId))
                {
                    chunkIndex.Add(item, new ConcurrentBag<int>());
                }
            }

            Parallel.For(0, positions.Count, i =>
            {
                GridChunk chunk = GetHexChunk(positions[i]);

                if (chunk != null)
                {
                    if (chunkIndex.TryGetValue(chunk, out ConcurrentBag<int> indexList))
                    {
                        indexList.Add(i);
                    }
                }
            });

            // remove empty values from chunkINdex
            chunkIndex = chunkIndex.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);
            byte b = 2;
            Parallel.ForEach(chunkIndex, item =>
            {
                GridChunk chunk = item.Key;
                ConcurrentBag<int> indexList = item.Value;

                foreach (int i in indexList)
                {
                    chunk.QuickInsertVisualData(positions[i], datas[i], layerId);
                    vDatas.TryAdd(datas[i], b);
                }
            });

            visualDatas.UnionWith(vDatas.Keys);
        }

        /// <summary>
        /// Will insert a block of visual data at the given positions. If the number of positions and datas are not the same, the method will abort
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="datas"></param>
        /// <param name="layerId"></param>
        public void InsertPositionBlock(List<Vector2Int> positions, List<ShapeVisualData> datas, string layerId = USE_DEFAULT_LAYER)
        {
            if (UseMultithreading)
            {
                InsertPositionBlock_Fast(positions, datas, layerId);

                return;
            }

            ValidateLayerId(ref layerId);

            if (positions.Count != datas.Count)
            {
                Debug.LogError("When Inserting as Block, The number of positions and datas must be the same");
                return;
            }

            Dictionary<GridChunk, List<int>> chunkIndex = new Dictionary<GridChunk, List<int>>();

            foreach (GridChunk item in sortedChunks.Values)
            {
                if (item.HasLayer(layerId))
                {
                    chunkIndex.Add(item, new List<int>());
                }
            }

            for (int i = 0; i < positions.Count; i++)
            {
                GridChunk chunk = GetHexChunk(positions[i]);

                if (chunk != null)
                {
                    if (chunkIndex.TryGetValue(chunk, out List<int> indexList))
                    {
                        indexList.Add(i);
                    }
                }
            }

            // remove empty values from chunkIndex
            chunkIndex = chunkIndex.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);

            foreach (var item in chunkIndex)
            {
                GridChunk chunk = item.Key;
                List<int> indexList = item.Value;

                foreach (int i in indexList)
                {
                    chunk.QuickInsertVisualData(positions[i], datas[i], layerId);
                    visualDatas.Add(datas[i]);
                }
            }
        }


        /// <summary>
        /// Removes the visual data at the given chunk position from the given layer.
        /// Note removing a visualData will cause the cell at that position to be drawn with the defaultVisualData. If you wish for the entire cell to be empty, use the DeletePosition method
        /// </summary>
        /// <param timerName="gridPosition"></param>
        /// <param timerName="LayerId"></param>
        public void RemoveVisualData(Vector2Int gridPosition, string layerId = USE_DEFAULT_LAYER)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.RemoveVisualData(gridPosition, layerId);
            }
        }

        /// <summary>
        /// Removes the visual data at the given chunk position from all layers.Note removing a visualData will cause the cell at that position to be drawn with the defaultVisualData. If you wish for the entire cell to be empty, use the DeletePosition method
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

        /// <summary>
        /// Will delete the position from the grid. This means there will be no mesh at that cells position
        /// </summary>
        /// <param name="gridPosition"></param>
        public void DeletePosition(Vector2Int gridPosition)
        {
            ValidateLayerId(ref defaultLayerId);

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.DeletePosition(gridPosition);
            }
        }

        /// <summary>
        /// Delete the position at that specific layer. This means there will be no mesh at that cells position
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="layerId"></param>
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
        /// Set whether to use visual equality at the given layer. Note, This is an expensive operation as it requires that all positions be regrouped and redrawn.
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
        /// Set whether to use visual equality at the given layer. Note, This is an expensive operation as it requires that all positions be regrouped and redrawn.
        /// </summary>
        /// <param name="useEquality"></param>
        public void SetVisualEquality(bool useEquality)
        {
            GridChunk c = sortedChunks.Values.First();

            c.SetVisualEquality(useEquality);
        }
        /// <summary>
        /// Changes the gridShape of the layer. The entire layer will have to be redrawn
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="layerId"></param>
        public void SetGridShape(GridShape shape, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            foreach (GridChunk c in sortedChunks.Values)
            {
                c.SetGridShape(layerId, shape);
            }
        }

        /// <summary>
        /// Enable or disable a chunk if the chunk is contained within the given bounds. If invert is true, the status of chunks not in the bounds will be set to the opposite of the given status
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="status">The status to set chunks which are in the bounds too</param>
        /// <param name="invert">For chunks not it the bounds, should their status be said to the opposite of the passed in status array</param>
        public void SetStatusIfChunkIsInBounds(Bounds bounds, bool status, bool invert = false)
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                Bounds chunkBounds = chunk.GetDefaultLayerBounds();

                chunkBounds.center += transform.position;

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

        public void SetStatusAllChunk(bool status)
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.gameObject.SetActive(status);
            }
        }

        /// <summary>
        /// Will update all layers in which the given chunk is in.
        /// Note, all add,remove,delete operations will not take effect unless the position has been updated or the map has been redrawn
        /// </summary>
        /// <param timerName="gridPosition"></param>
        public void UpdatePosition(Vector2Int gridPosition)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.DrawChunk();
            }
        }

        /// <summary>
        /// Update a specific layer of a specific chunk at the given position.
        /// Note, all add,remove,delete operations will not take effect unless the position has been updated or the map has been redrawn
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="layerId"></param>
        public void UpdatePosition(Vector2Int gridPosition, string layerId = USE_DEFAULT_LAYER)
        {
            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.DrawLayer(layerId);
            }
        }

        /// <summary>
        /// Update the local position of all chunks. Call this when you have set or changed the defaultLayer
        /// </summary>
        private void ValidateChunkPositions()
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.ValidateLocalPosition();
            }
        }

        /// <summary>
        /// Makes sure the current grid orientation is the same as the orientation as stated by the 'MapOrientation' field. Call this method after you modify the map orientation
        /// </summary>
        public void ValidateOrientation()
        {
            // at all points in time all shapes should have thesame orientation, thus we can check the shapes at any index
            if (gridShapes.First().ShapeOrientation != MapOrientation)
            {
                foreach (GridShape shape in gridShapes)
                {
                    shape.ShapeOrientation = MapOrientation;
                }

                foreach (GridChunk chunk in sortedChunks.Values)
                {
                    chunk.ValidateOrientation();
                }
            }

            SortMeshLayers();
        }
        private void ValidateGridBounds()
        {
            Vector2Int min = Vector2Int.zero;
            Vector2Int max = gridSize;

            GridShape shape = null;

            sortedChunks.Values.First().TryGetLayerShape(defaultLayerId, out shape);

            localBounds = shape.GetGridBounds(min, max);
        }

        /// <summary>
        /// Draws the grid. Call this whenever you have Inserted a new position/visualData into the grid and want to see those changes take effect
        /// </summary>
        public void DrawGrid()
        {
            if (UseMultithreading)
            {
                Parallel.ForEach(sortedChunks.Values, chunk =>
                {
                    chunk.FusedMeshGroups();
                });

                foreach (GridChunk chunk in sortedChunks.Values)
                {
                    chunk.DrawFusedMesh();
                }
            }
            else
            {
                foreach (GridChunk chunk in sortedChunks.Values)
                {
                    chunk.DrawChunk();
                }
            }

            SortMeshLayers();
        }

        /// <summary>
        /// Simply redraws a layer. Call this when you have made changes to a layer and want to see those changes take effect
        /// </summary>
        /// <param name="layerId"></param>
        public void RedrawLayer(string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.DrawLayer(layerId);
            }

            SortMeshLayers();
        }

        /// <summary>
        /// Clears the entire map, and frees up resources.
        /// </summary>
        public void Clear()
        {

#if UNITY_EDITOR

            while (transform.childCount > 0)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
#else
            foreach (Transform child in transform.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
#endif

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.Clear();
            }

            sortedChunks.Clear();
            visualDatas.Clear();
            meshLayerInfos.Clear();
            gridShapes.Clear();
        }
        #endregion

        #region Grid Positioning

        /// <summary>
        /// If the LayerId is the Base layer id, we simply set the layerID to the actual base layer id
        /// </summary>
        /// <param timerName="LayerId"></param>
        private void ValidateLayerId(ref string layerId)
        {
            if (layerId.Equals(USE_DEFAULT_LAYER))
            {
                layerId = DefaultLayer;
            }
        }
        /// <summary>
        /// Loops through all the chunks and returns the Shape of the first layer found with the given ID.
        /// </summary>
        /// <param timerName="LayerId"></param>
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
        /// Returns the Shape of the layer at the given chunk position.
        /// </summary>
        /// <param timerName="LayerId"></param>
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

        /// <summary>
        /// Uses the localBounds field to determine where a given location position is in the map
        /// </summary>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        public bool ContainsLocalPosition(Vector3 localPosition)
        {
            if (MapOrientation == GridShape.Orientation.XY)
            {
                localPosition = new Vector3(localPosition.x, localPosition.y, 0);
            }
            else
            {
                localPosition = new Vector3(localPosition.x, 0, localPosition.z);
            }

            return localBounds.Contains(localPosition);
        }

        public bool ContainsWorldPosition(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            return ContainsLocalPosition(localPosition);
        }

        /// <summary>
        /// Used the gridBounds field to see if a given gridPosition is in the grid
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <returns></returns>
        public bool ContainsGridPosition(Vector2Int gridPosition)
        {
            return gridBounds.Contains((Vector3Int)gridPosition);
        }

        /// <summary>
        /// Gets the gridPosition at the given location position. If the location position is not in the grid, the method will return Vector2Int.left
        /// </summary>
        /// <param name="localPosition"></param>
        /// <param name="layerId"></param>
        /// <returns></returns>
        public Vector2Int LocalToGridPosition(Vector3 localPosition, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(localPosition, layerId);

            Vector2Int gridPosition = Vector2Int.left;

            if (chunk != null)
            {
                chunk.TryGetGridPosition(localPosition, out gridPosition);
            }

            return gridPosition;
        }

        /// <summary>
        /// Gets the gridPosition at the given world position. If the world position is not in the grid, the method will return Vector2Int.left
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            Vector3 localPosition = transform.InverseTransformPoint(worldPosition);

            if (MapOrientation == GridShape.Orientation.XY)
            {
                localPosition.z = 0;
            }
            else
            {
                localPosition.y = 0;
            }

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
        public Vector3 GridToLocalPosition(Vector2Int gridPosition)
        {
            GridShape shape = GetShape(gridPosition);

            return shape.GetTesselatedPosition(gridPosition);
        }
        public Vector3 GridToWorldPostion(Vector2Int gridPosition)
        {
            GridShape shape = GetShape(gridPosition);

            Vector3 local = shape.GetTesselatedPosition(gridPosition);

            return transform.TransformPoint(local);
        }

        /// <summary>
        /// Will get the visualdata at the given layer and gridPosition and return a CLONE of it.
        /// This is to allow you to modify the visual data without modifying other shapes with thesame visual data
        /// </summary>
        /// <typeparam timerName="T"></typeparam>
        /// <param timerName="gridPosition"></param>
        /// <param timerName="LayerId"></param>
        /// <returns></returns>
        public T GetVisualData<T>(Vector2Int gridPosition,
                                    string layerId = USE_DEFAULT_LAYER) where T : ShapeVisualData
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                return chunk.GetVisualData(gridPosition, layerId) as T;
            }

            return null;
        }

        /// <summary>
        /// Will get the visualdata at the given layer and gridPosition.
        /// Modifying and Updating said visualData will consequently modify all shapes that used said visualData...use with care
        /// </summary>
        /// <param timerName="gridPosition"></param>
        /// <param timerName="LayerId"></param>
        /// <returns></returns>
        public ShapeVisualData GetVisualData(Vector2Int gridPosition,
                                                    string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                return chunk.GetVisualData(gridPosition, layerId);
            }

            return null;
        }

        /// <summary>
        /// Will update the visualHash of all the hashes in the map.
        /// Do not call unless you need to. Make sure to disable updateOnVisual change in the meshlayer class
        /// </summary>
        public void ValidateAllVisualHashes()
        {
            foreach (ShapeVisualData vp in visualDatas)
            {
                vp.ValidateVisualHash();
            }
        }

        #endregion

        #region Saving and Loading Map

        /// <summary>
        /// Will return a string that represents the current state of the map. This string can be saved and loaded at a later time. This used the current visualContainer to serialize the map. If you wish to load the map, you must have the same visualContainer that was used to serialize the map. Any modifications to the visualContainer will cause the map to not load correctly.
        /// </summary>
        /// <returns></returns>
        public string GetSerializeMap()
        {
            SavedMap savedMap = new SavedMap(this);

            string json = JsonUtility.ToJson(savedMap, true);

            return json;
        }

        /// <summary>
        /// Given a string which contains a serialized map, This method will deserialize the map and load it into the grid. In order for this to work, this gridManager must have the appropriate visualContainer which was used to serialize the map
        /// </summary>
        /// <param name="json"></param>
        public void DeserializeMap(string json)
        {
            SavedMap savedMap = JsonUtility.FromJson<SavedMap>(json);

            savedMap.DeserializeVisualProps(visualContainer);

            visualDatas = savedMap.visualDatas.ToHashSet();

            LoadMap(savedMap);

            Debug.Log("Map Loaded");
        }
        private void LoadMap(SavedMap savedMap)
        {
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.Clear();
            }

            gridSize = savedMap.gridSize;
            chunkSize = savedMap.chunkSize;
            cellGap = savedMap.cellGap;
            shapeScale = savedMap.shapeScale;

            MapOrientation = savedMap.mapOrientation;
            layerSortAxis = savedMap.layerSortAxis;

            UseMultithreading = savedMap.useMultiThreading;
            RedrawOnVisualHashChanged = savedMap.redrawOnV;

            defaultLayerId = savedMap.baseLayerId;
            colorShaderName = savedMap.colorShaderName;

            defaultVisualData = savedMap.defaultVisualData;
            colorShader = Shader.Find(colorShaderName);
            Initialize();

            if (sortedChunks.Count > 0)
            {
                for (int i = 0; i < savedMap.layerSettings.Count; i++)
                {
                    MeshLayerSettings item = savedMap.layerSettings[i];
                    item.Shape = visualContainer.GetGridShape(item.ShapeId);

                    CreateLayer(item, item.LayerId == savedMap.baseLayerId);
                }

                Dictionary<Guid, ShapeVisualData> savedDatas = new Dictionary<Guid, ShapeVisualData>();

                foreach (ShapeVisualData vData in savedMap.visualDatas)
                {
                    savedDatas.TryAdd(vData.VisualId, vData);
                }

                if (savedMap.useMultiThreading)
                {
                    InsertChunk_Fast();
                }
                else
                {
                    InsertChunk();
                }

                DrawGrid();

                void InsertChunk()
                {
                    foreach (SerializedGridChunk item in savedMap.serializedChunks)
                    {
                        GridChunk chunk = null;
                        ColorVisualData defaultVData = new ColorVisualData(ColorShader, Color.white);

                        if (sortedChunks.TryGetValue(item.startPosition.GetHashCode_Unique(), out chunk))
                        {
                            foreach (SerializedMeshLayer sml in item.serializedLayers)
                            {
                                List<ShapeVisualData> data = new List<ShapeVisualData>();

                                for (int i = 0; i < sml.visualDatas.Count; i++)
                                {
                                    Vector2Int pos = sml.gridPositions[i];
                                    Guid id = Guid.Parse(sml.visualDatas[i]);

                                    if (!savedDatas.TryGetValue(id, out ShapeVisualData v))
                                    {
                                        v = defaultVData;
                                    }

                                    chunk.QuickInsertVisualData(pos, v, sml.layerId);
                                    visualDatas.Add(v);
                                }
                            }
                        }
                    }
                }

                void InsertChunk_Fast()
                {
                    ColorVisualData defaultVData = new ColorVisualData(ColorShader, Color.white);

                    Parallel.ForEach(savedMap.serializedChunks, item =>
                    {
                        if (sortedChunks.TryGetValue(item.startPosition.GetHashCode_Unique(), out GridChunk chunk))
                        {
                            foreach (SerializedMeshLayer sml in item.serializedLayers)
                            {
                                List<ShapeVisualData> data = new List<ShapeVisualData>();

                                for (int i = 0; i < sml.visualDatas.Count; i++)
                                {
                                    Vector2Int pos = sml.gridPositions[i];
                                    Guid id = Guid.Parse(sml.visualDatas[i]);

                                    if (!savedDatas.TryGetValue(id, out ShapeVisualData v))
                                    {
                                        v = defaultVData;
                                    }

                                    chunk.QuickInsertVisualData(pos, v, sml.layerId);

                                    visualDatas.Add(v); // ConcurrentBag handles thread-safety
                                }
                            }
                        }
                    });
                }
            }
        }
        /// <summary>
        /// Will get all the unique visual Data being used in the map. 
        /// Uniqueness is determined by the visualIdHash
        /// </summary>
        /// <returns></returns>
        public List<ShapeVisualData> GetUniqueVisuals()
        {
            return visualDatas.ToList();
        }

        /// <summary>
        /// A saved map is a serialized struct that holds the state of the grid. This struct can be serialized and deserialized to save and load the grid
        /// </summary>

        [Serializable]
        public struct SavedMap
        {
            public Vector2Int gridSize;
            public Vector2Int chunkSize;
            public Vector2 shapeScale;
            public Vector2 cellGap;

            public GridShape.Orientation mapOrientation;
            public SortAxis layerSortAxis;

            public bool useMultiThreading;
            public bool redrawOnV;

            public string baseLayerId;
            public string colorShaderName;

            [SerializeReference]
            public ShapeVisualData defaultVisualData;

            [SerializeReference]
            public List<ShapeVisualData> visualDatas;

            [SerializeField]
            public List<MeshLayerSettings> layerSettings;

            [SerializeField]
            public List<SerializedGridChunk> serializedChunks;

            public SavedMap(GridManager gridManager)
            {
                gridSize = gridManager.GridSize;

                chunkSize = gridManager.ChunkSize;
                shapeScale = gridManager.shapeScale;
                cellGap = gridManager.cellGap;
                mapOrientation = gridManager.MapOrientation;
                layerSortAxis = gridManager.LayerSortAxis;
                useMultiThreading = gridManager.UseMultithreading;
                redrawOnV = gridManager.RedrawOnVisualHashChanged;
                baseLayerId = gridManager.DefaultLayer;
                colorShaderName = gridManager.colorShaderName;

                layerSettings = gridManager.meshLayerInfos.Values.ToList();

                visualDatas = gridManager.visualDatas.ToList();

                defaultVisualData = gridManager.DefaultVisualData;

                serializedChunks = new List<SerializedGridChunk>();

                SerializeVisualProps(gridManager.visualContainer);

                foreach (GridChunk item in gridManager.sortedChunks.Values)
                {
                    serializedChunks.Add(item.GetSerializedChunk());
                }
            }

            public void SerializeVisualProps(MapVisualContainer container)
            {
                defaultVisualData.SerializeVisualData(container);

                foreach (ShapeVisualData visual in visualDatas)
                {
                    visual.SerializeVisualData(container);
                }
            }

            public void DeserializeVisualProps(MapVisualContainer container)
            {
                defaultVisualData.DeserializeVisualData(container);

                List<ShapeVisualData> deserializedVData = new List<ShapeVisualData>();

                foreach (ShapeVisualData visual in visualDatas)
                {
                    deserializedVData.Add(visual.DeserializeVisualData(container));
                }

                visualDatas = deserializedVData;
            }
        }
        #endregion
        public string GetMapDescription()
        {
            string s1 = "Map size(Chunk Size): " + gridSize.x + " X " + gridSize.y;
            string s2 = "(" + chunkSize.x + " X " + chunkSize.y + ")";
            string s3 = "\nUses Multithreaded Chunks: " + UseMultithreading;

            return s1 + s2 + s3;
        }
    }

    /// <summary>
    /// A struct used to hold the settings of a meshLayer
    /// </summary>
    [Serializable]
    public struct MeshLayerSettings
    {
        /// <summary>
        /// The distance between each layer. This is used to determine the order in which the layers are drawn. So layers are drawn this value away or closer to each other.
        /// </summary>
        public static float SortStep = 0.0001f;

        [SerializeField]
        private string layerId;

        [SerializeField]
        private int orderInLayer;

        [SerializeField]
        private bool useVisualEquality;

        [SerializeField]
        private GridShape shape;

        [SerializeField]
        [HideInInspector]
        private string shapeId;
        public GridShape Shape
        {
            get
            {
                return shape;
            }
            set
            {
                shape = value;
                shapeId = shape.UniqueShapeName;
            }
        }

        public string ShapeId
        {
            get
            {
                return shapeId;
            }
        }
        public string LayerId { get => layerId; private set => layerId = value; }
        public int OrderInLayer { get => orderInLayer; set => orderInLayer = value; }
        public bool UseVisualEquality { get => useVisualEquality; set => useVisualEquality = value; }
        public MeshLayerSettings(string layerId, GridShape shape, bool useVisualEquality = false, int orderInLayer = 0)
        {
            this.layerId = layerId;

            this.shape = shape;
            shapeId = shape.UniqueShapeName;

            this.orderInLayer = orderInLayer;
            this.useVisualEquality = useVisualEquality;
        }

        /// <summary>
        /// Makes sure the shapeId is set.
        /// This will need to be done if the shapeId is set via the inspector
        /// </summary>
        public void Validate()
        {
            shapeId = shape.UniqueShapeName;
        }

        public override int GetHashCode()
        {
            return LayerId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(MeshLayerSettings))
            {
                MeshLayerSettings mli = (MeshLayerSettings)obj;
                return LayerId.Equals(mli.LayerId);
            }
            else
            {
                return false;
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