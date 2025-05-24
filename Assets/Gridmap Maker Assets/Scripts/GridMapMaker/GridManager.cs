using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Debug = UnityEngine.Debug;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using static GridMapMaker.ShapeVisualData;
using static GridMapMaker.MeshLayer;
using static GridMapMaker.GridChunk;


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
        /// The bounds of the grid in local position. The dimensions are determined by the shape of the default layer
        /// </summary>
        public Bounds LocalBounds
        {
            get
            {
                Bounds b = new Bounds();

                foreach (GridChunk chunk in sortedChunks.Values)
                {
                    b.Encapsulate(chunk.ChunkLocalBounds);
                }

                return b;
            }
        }

        private BoundsInt gridBounds;

        /// <summary>
        /// The bounds of the grid size. So if your grid size is (10, 10), then the bounds will be (0, 0, 0) to (10, 10, 0)
        /// </summary>
        public BoundsInt GridBounds => gridBounds;

        /// <summary>
        /// Checks if the given grid position is within the bounds of the grid. This is useful for checking if a grid position is valid or not.
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <returns></returns>
        public bool WithinGridBounds(Vector2Int gridPosition)
        {
            return gridBounds.Contains((Vector3Int)gridPosition);
        }

        public enum ColliderType
        {
            None,
            BoxCollider2D,
            MeshCollider,
            MeshCollider_Convex

            // TODO : Figure out how to convert mesh to polygon collider
        }



        private string baseLayerId;

        /// <summary>
        /// This is the default layer in which modifications will be made if no layer is specified.
        /// Additionally, this layer is used to determine the bounds of the map
        /// </summary>
        public string BaseLayer { get { return baseLayerId; } }

        /// <summary>
        /// The gap between cells
        /// </summary>
        public Vector2 cellGap;


        private static ShapeVisualData _colorVisualData;

        /// <summary>
        /// The ColorVisualData is what is used When inserting/displaying Colors. That is, when you use ShapeVisualData.RenderMode.MeshColors.
        /// </summary>
        public static ShapeVisualData DefaultColorVisualData
        {
            get
            {
                if (_colorVisualData == null)
                {
                    _colorVisualData = new ColorVisualData(Color.white);
                }
                return _colorVisualData;
            }
            set
            {
                if (value != null)
                {
                    _colorVisualData = value;
                }
            }
        }



        /// <summary>
        /// The map can be displayed in different orientations. The default orientation is XY.
        /// If you changed the orientation, be sure to call the ValidateOrientation method for your changes to take effect
        /// </summary>
        [SerializeField]
        public GridShape.Orientation MapOrientation = GridShape.Orientation.XY;

        /// <summary>
        /// Note Colliders only work for the base layer. 
        /// Colliders take into account the entire grid regardless of whether or not chunk or tiles have been placed or not.
        /// Also note that the Grid itself does not have nor does it need a collider. 
        /// All Colliders are added to GridChunks only.
        /// Note that if you display the map on the XZ plane, then you cannot use 2D colliders.
        /// Note that certain shapes, such as hexes will have gaps between them. Thus, the colliders will not be accurate. You can use a mesh collider to get around this, but note mesh colliders are more expensive.
        /// Use MeshCollider_Convex if you want complete accuracy with better performance. This will create a convex mesh collider that is more expensive than a box collider but less expensive than a mesh collider.
        /// </summary>
        /// 
        [SerializeField]
        private ColliderType gridChunkCollider = ColliderType.None;

        /// <summary>
        /// Note that the collider is the collider of the DRAWN mesh. Thus, if you have a situation where no tiles have been placed on a grid chunk, the collider will be empty. Subsequently, box colliders might not be accurate depending on whether certain shapes are used.
        /// </summary>
        public ColliderType GridChunkCollider
        {
            get => gridChunkCollider;
            set
            {
                gridChunkCollider = value;
                foreach (GridChunk chunk in sortedChunks.Values)
                {
                    chunk.ChunkColliderType = gridChunkCollider;
                }
            }
        }

        private HashSet<ShapeVisualData> visualDatas = new HashSet<ShapeVisualData>();

        [SerializeField]
        private HashSet<GridShape> gridShapes = new HashSet<GridShape>();

        /// <summary>
        /// This is redundant, since meshlayerSettings are not 2 way meaning modifications to the meshlayerSettings will not be reflected in the gridmanager. 
        /// 
        /// For serialization purposes, we should reconstruct the meshlayerSettings from the MeshLayer directly instead.
        /// 
        /// Only store the layer ids and delete this.
        /// </summary>
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

        // we use these variables because, the transform of the map can only take effect AFTER the meshes have been drawn. Thus we have to save the initial rotation in which we want the map to be rotated to after it is drawn
        private Vector3 T_EulerAngles { get; set; }
        private Vector3 T_Position { get; set; }
        private Vector3 T_Scale { get; set; }


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
        private GridChunk CreateGridChunk(Vector2Int gridPosition, GridChunk prefab)
        {
            GridChunk chunk;

            Vector3Int start = (Vector3Int)GetChunkStartPosition(gridPosition);

            BoundsInt chunkBounds = new BoundsInt(start, (Vector3Int)ChunkSize);

            chunk = Instantiate(prefab, transform, false);

            // the transform is always relative to the parent such that if the parent is moved, the child moves with it

            // this is required. The reason I have this is to initialize the property.
            // We need to this init the property because if the default value is not set, the property will try and find the sprites/default shader. However, Shader.Find can only work from the mainthread. So if you use a multithreaded approach, it will cause an error.
            _colorVisualData = null;
            ShapeVisualData d = DefaultColorVisualData;

            chunk.Initialize(this, chunkBounds, gridChunkCollider);

            return chunk;
        }
        private GridChunk GetGridChunk(Vector2Int gridPosition)
        {
            Vector2Int startPosition = GetChunkStartPosition(gridPosition);

            GridChunk chunk = null;

            sortedChunks.TryGetValue(startPosition.GetHashCode_Unique(), out chunk);

            return chunk;
        }
        private GridChunk GetGridChunk(Vector3 localPosition, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);
            // see if a chunk contains a gridposition at that local position
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                if (chunk.ContainsLocalPosition(localPosition, layerId))
                {
                    return chunk;
                }
            }

            return null;
        }
        private bool ValidateChunkSize()
        {
            // If the chunk size is less than or equal to 0, then the chunk size is the same as the chunk size, else if the chunk size is greater than the chunk size, then the chunk size is the chunk size

            if (gridSize.x > 0 && gridSize.y > 0)
            {
                // Mathf.Clamp(value, min, max

                int x = (chunkSize.x <= 0) ? gridSize.x : (chunkSize.x > gridSize.x) ? gridSize.x : chunkSize.x;
                int y = (chunkSize.y <= 0) ? gridSize.y : (chunkSize.y > gridSize.y) ? gridSize.y : chunkSize.y;

                chunkSize = new Vector2Int(x, y);

                // we subtract 1 from the grid size because the grid size is inclusive. So if the grid size is (10, 10), then the bounds will be (0, 0, 0) to (9, 9, 0)
                gridBounds = new BoundsInt(Vector3Int.zero, (Vector3Int)gridSize - new Vector3Int(1, 1, 0));

                gridBounds.zMin = 0;
                gridBounds.zMax = 1;

                return true;
            }

            return false;

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

                    GridChunk chunk = CreateGridChunk(start, prefab);

                    chunk.name = "Chunk " + count;

                    chunk.ChunkColliderType = gridChunkCollider;

                    count++;

                    sortedChunks.Add(chunk.StartPosition.GetHashCode_Unique(), chunk);
                }
            }

            DestroyImmediate(prefab.gameObject);
        }

        #endregion

        #region Grid Manipulation

        /// <summary>
        /// This is used to simply store the color shader for serialization and deserialization purposes
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private const string spriteDefault = "Sprites/Default";
        /// <summary>
        /// Initializes the grid with its current settings. This method assumes the settings have been set from the editor. Be advised, most settings of the grid manager are used to determine how the grid is displayed. Thus, if you change any of these settings, after the grid has been initialized, it will have no effect or cause errors. For example, setting the chunkSize, or gridSize or cellGap will have no effect on a map that is already created. However, it will cause various errors if you were to do bounds checking or grid positioning etc.
        /// </summary>
        public void Initialize()
        {
            if(ValidateChunkSize() == false)
            {
                Debug.Log("Could not initialize Grid. Make sure your Grid size is greater than Zero");
            }

            // the map has to start of a rotation of zero, then after it is drawn,
            // we can rotate it back to its original rotation.
            // No idea why but it is what is is
            T_EulerAngles = transform.localEulerAngles;
            T_Position = transform.localPosition;
            T_Scale = transform.localScale;

            //transform.localEulerAngles = Vector3.zero;
            //transform.localPosition = Vector3.zero;
            //transform.localScale = Vector3.one;

            CreateGridChunks();
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

                if (layerInfo.ShapeSize == Vector2.zero)
                {
                    // your welcome
                    layerInfo.ShapeSize = Vector2.one;
                }

                gridShape.scale = layerInfo.ShapeSize;

                gridShape.UpdateShape();

                layerInfo.Shape = gridShape;

                gridShapes.Add(gridShape);
            }

            meshLayerInfos.Add(layerInfo.LayerId, layerInfo);

            // add layer to all chunk chunks
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.InitializeLayer(layerInfo);
            }

            if (setBaselayer || meshLayerInfos.Count == 1)
            {
                baseLayerId = layerInfo.LayerId;
            }

            return true;
        }
        public MeshLayerSettings GetLayerInfo(string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            return meshLayerInfos[layerId];

        }

        // we call this after the map has been drawn so that is tranforms(if set in editor) can take effect
        private void VaidateMapTransforms()
        {
            transform.localEulerAngles = T_EulerAngles;

            //transform.localEulerAngles = Vector3.zero;
            //transform.localPosition = Vector3.zero;
            //transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Will sort the layers based on the orderInLayer and the grid layerSortAxis. The layer with the lowest orderInLayer will be at the back, while the layer with the highest orderInLayer will be at the front
        /// </summary>
        public void SortMeshLayers()
        {
            VaidateMapTransforms();

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

            int dir = 0;

            switch (layerSortAxis)
            {
                case SortAxis.X:
                    baseLocation += gridPos.x;
                    dir = (transform.right.x > 0 ? 1 : -1);
                    break;
                case SortAxis.Y:
                    baseLocation += gridPos.y;
                    dir = (transform.up.y > 0 ? 1 : -1);
                    break;
                case SortAxis.Z:
                    baseLocation += gridPos.z;
                    dir = (transform.forward.z > 0 ? -1 : 1);
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
                    offset *= dir;
                }

                foreach (GridChunk chunk in sortedChunks.Values)
                {
                    chunk.SortLayer(layer.LayerId, layerSortAxis, offset);
                    chunk.ValidateLocalPosition();
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

            if(data == null)
            {
                //Debug.LogWarning("Visual Data cannot be null");
                return;
            }

            GridChunk chunk = GetGridChunk(gridPosition);

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

            GridChunk chunk = GetGridChunk(gridPosition);

            if (chunk != null)
            {
                ColorVisualData data = new ColorVisualData(color);

                chunk.InsertVisualData(gridPosition, data, layerId);
                visualDatas.Add(data);
            }
        }
        /// <summary>
        /// The multithreading version of the InsertPostionBlock method. This method is faster than the non-multithreading version. However, may or may not work (most likely will) on some computers. Inorder to maximize performance, there is very little error checking. Make sure your data is valid before calling this method
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="datas"></param>
        /// <param name="layerId"></param>
        private void InsertPositionBlock_Fast(List<Vector2Int> positions, List<ShapeVisualData> datas, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            if (positions.Count != datas.Count)
            {
                Debug.LogWarning("When Inserting as Block, The number of positions and datas must be the same");
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
                GridChunk chunk = GetGridChunk(positions[i]);

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
                    if (datas[i] != null)
                    {
                        chunk.QuickInsertVisualData(positions[i], datas[i], layerId);
                        vDatas.TryAdd(datas[i], b);
                    }
                }
            });

            visualDatas.UnionWith(vDatas.Keys);
        }

        /// <summary>
        /// Will insert a block of visual data at the given positions. If the number of positions and datas are not the same, the method will abort. Inorder to maximize performance, there is very little error checking. Make sure your data is valid before calling this method
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
                GridChunk chunk = GetGridChunk(positions[i]);

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
                    if(datas[i] != null)
                    {
                        chunk.QuickInsertVisualData(positions[i], datas[i], layerId);
                        visualDatas.Add(datas[i]);
                    }
                }
            }
        }


        /// <summary>
        /// Removes the visual data at the given chunk position from all layers. There will be nothing displayed at that position after this operation.
        /// </summary>
        /// <param timerName="gridPosition"></param>
        /// <param timerName="LayerId"></param>
        public void RemoveVisualData(Vector2Int gridPosition, string layerId = USE_DEFAULT_LAYER)
        {
            GridChunk chunk = GetGridChunk(gridPosition);

            if (chunk != null)
            {
                chunk.RemoveVisualData(gridPosition, layerId);
            }
        }

        /// <summary>
        /// Removes the visual data at the given chunk position from all layers. There will be nothing displayed at that position after this operation.
        /// </summary>
        /// <param timerName="gridPosition"></param>
        public void RemoveVisualData(Vector2Int gridPosition)
        {
            GridChunk chunk = GetGridChunk(gridPosition);

            if (chunk != null)
            {
                chunk.RemoveVisualData(gridPosition);
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
                MeshLayerSettings ms = meshLayerInfos[layerId];

                ms.UseVisualEquality = useEquality;

                meshLayerInfos[layerId] = ms;
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

            foreach (MeshLayerSettings item in meshLayerInfos.Values)
            {
                MeshLayerSettings ms = item;
                ms.UseVisualEquality = useEquality;
                meshLayerInfos[item.LayerId] = ms;
            }
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
                Bounds chunkBounds = chunk.ChunkLocalBounds;

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
                Bounds chunkBounds = chunk.ChunkLocalBounds;

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
            GridChunk chunk = GetGridChunk(gridPosition);

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
            GridChunk chunk = GetGridChunk(gridPosition);

            if (chunk != null)
            {
                chunk.DrawLayer(layerId);
            }
        }

        /// <summary>
        /// Will redraw the layer with the new settings. Note that a redraw will take place.
        /// </summary>
        /// <param name="modifiedSettings"></param>
        public void ModifyLayerSettings(MeshLayerSettings modifiedSettings)
        {
            string layerId = modifiedSettings.LayerId;

            if (meshLayerInfos.ContainsKey(layerId))
            {
                meshLayerInfos[layerId] = modifiedSettings;
            }

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.InitializeLayer(modifiedSettings);
            }

            DrawLayer(layerId);
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
        /// Simply draws a specific layer. Call this when you have made changes to a layer and want to see those changes take effect
        /// </summary>
        /// <param name="layerId"></param>
        public void DrawLayer(string layerId = USE_DEFAULT_LAYER)
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
            foreach (GridChunk chunk in sortedChunks.Values)
            {
               chunk.Clear();
            }

            sortedChunks.Clear();
            visualDatas.Clear();
            meshLayerInfos.Clear();
            gridShapes.Clear();

            // loop throught children and remove any object with gridchunk componenet

            // loop in reverse

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.GetComponent<GridChunk>() != null)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
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
                layerId = BaseLayer;
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

            GridChunk chunk = GetGridChunk(gridPosition);

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

            GridChunk chunk = GetGridChunk(localPosition, layerId);

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

            return LocalBounds.Contains(localPosition);
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

            GridChunk chunk = GetGridChunk(localPosition, layerId);

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

            GridChunk chunk = GetGridChunk(gridPosition);

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

            GridChunk chunk = GetGridChunk(gridPosition);

            if (chunk != null)
            {
                return chunk.GetVisualData(gridPosition, layerId);
            }

            return null;
        }

        public List<ShapeVisualData> GetUniqueVisualIds()
        {
            return visualDatas.ToList();
        }

        /// <summary>
        /// Will get all the unique visual Data being used in the map. 
        /// Uniqueness is determined by the visualIDHash not visualHash
        /// </summary>
        /// <returns></returns>
        public List<ShapeVisualData> GetUniqueVisualHashes(string layer = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layer);

            bool useVisualEqual = meshLayerInfos[layer].UseVisualEquality;

            VisualDataComparer com = new VisualDataComparer();
            com.UseVisualHash = useVisualEqual;
            HashSet<ShapeVisualData> uniqueVisuals = new HashSet<ShapeVisualData>(visualDatas, com);

            return uniqueVisuals.ToList();
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
        public static readonly float SortStep = 0.01f;

        /// <summary>
        /// Once this is set, it can never be changed. This is the unique identifier for a layer
        /// </summary>
        [SerializeField]
        private string layerId;

        [SerializeField]
        private int orderInLayer;

        [SerializeField]
        private bool useVisualEquality;

        [SerializeField]
        private GridShape shape;

        [SerializeField]
        private Vector2 shapeScale;

        [SerializeField]
        private bool includeMeshCollider;

        public GridShape Shape
        {
            get
            {
                return shape;
            }
            set
            {
                shape = value;
            }
        }
        public Vector2 ShapeSize
        {
            get
            {
                return shapeScale;
            }
            set { shapeScale = value; }
        }
        public string LayerId { get => layerId; }
        public string ShapeId { get => shape.UniqueShapeName; }
        public int OrderInLayer { get => orderInLayer; set => orderInLayer = value; }
        public bool UseVisualEquality { get => useVisualEquality; set => useVisualEquality = value; }
        public bool IncludeMeshCollider { get => includeMeshCollider; set => includeMeshCollider = value; }

        public MeshLayerSettings(string layerId, int orderInLayer, bool useVisualEquality,
                                GridShape shape, Vector2 shapeSize, bool includeMeshCollider)
        {
            this.layerId = layerId;
            this.orderInLayer = orderInLayer;
            this.useVisualEquality = useVisualEquality;
            this.shape = shape;
            this.shapeScale = shapeSize;
            this.includeMeshCollider = includeMeshCollider;
        }

        public MeshLayerSettings(string layerId)
        {
            this.layerId = layerId;
            orderInLayer = 0;
            useVisualEquality = false;
            shape = null;
            shapeScale = Vector2.zero;
            includeMeshCollider = false;
        }

        public override int GetHashCode()
        {
            return LayerId.GetHashCode();
        }

        /// <summary>
        /// Two layers are equal if they have the same layerId
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
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