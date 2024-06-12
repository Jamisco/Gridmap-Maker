using UnityEngine;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using System.Collections.Generic;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Gridmap_Assets.Scripts.Mapmaker;
using System;
using System.Linq;
using static Assets.Scripts.GridMapMaker.GridChunk;
using Assets.Scripts.Miscellaneous;
using static Assets.Scripts.Miscellaneous.ExtensionMethods;
using Debug = UnityEngine.Debug;
using Assets.Gridmap_Assets.Scripts.Miscellaneous;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using static UnityEditor.PlayerSettings;
using Plane = UnityEngine.Plane;
using static UnityEditor.Experimental.GraphView.GraphView;

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

        [SerializeField]
        private Vector2 shapeScale = Vector2.one;

        private Bounds localBounds;
        public Bounds LocalBounds => localBounds;

        private BoundsInt gridBounds;
        public BoundsInt GridBounds => gridBounds;

        private string baseLayerId;
        public string DefaultLayer { get { return baseLayerId; } }

        public Vector2 CellGap;
        
        [SerializeField]
        public GridShape.Orientation MapOrientation = GridShape.Orientation.XY;

        [SerializeReference]
        private MapVisualContainer visualContainer;

        private HashSet<ShapeVisualData> visualProps = new HashSet<ShapeVisualData>();

        [SerializeField]
        private HashSet<GridShape> gridShapes = new HashSet<GridShape>();

        [SerializeField]
        private Dictionary<string, MeshLayerInfo> meshLayerInfos 
                            = new Dictionary<string, MeshLayerInfo>();

        private SortAxis layerSortAxis;
        public SortAxis LayerSortAxis => layerSortAxis;
        public Vector3 WorldPosition
        {
            get
            {
                return transform.position;
            }
        } 
        /// <summary>
        /// When the grid is doing various operations such as fusing and drawing the meshes, we can use multithreading to speed up the process. This may be stable on a case by case basis. So as long as you are not using Unity objects outside of main thread, you should be fine.
        /// </summary>
        public bool UseMultithreading = true;
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
        private Dictionary<int, GridChunk> sortedChunks;
        /// <summary>
        /// Given a grid gridPosition, gets the start gridPosition of the chunk said grid will be in
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

            Vector3Int start = (Vector3Int) GetChunkStartPosition(gridPosition);

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
            // If the chunk size is less than or equal to 0, then the chunk size is the same as the grid size, else if the chunk size is greater than the grid size, then the chunk size is the grid size
            int x = (chunkSize.x <= 0) ? gridSize.x : (chunkSize.x > gridSize.x) ? gridSize.x : chunkSize.x;
            int y = (chunkSize.y <= 0) ? gridSize.y : (chunkSize.y > gridSize.y) ? gridSize.y : chunkSize.y;

            chunkSize = new Vector2Int(x, y);

            gridBounds = new BoundsInt(Vector3Int.zero, (Vector3Int) gridSize);

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
        public void Initialize()
        {
            ValidateChunkSize();
            CreateGridChunks();
            
            ShapeVisualData.CreateDefaultVisual(Color.white);
        }
        public void Initialize(Vector2Int gridSize, Vector2Int chunkSize)
        {
            GridSize = gridSize;
            ChunkSize = chunkSize;

            Initialize();
        }

        public bool CreateLayer(MeshLayerInfo layerInfo, bool setBaselayer = false)
        {
            if (meshLayerInfos.TryAdd(layerInfo.LayerId, layerInfo) == false)
            {
                return false;
            }

            gridShapes.TryGetValue(layerInfo.Shape, out GridShape gridShape);

            if (gridShape == null)
            {
                gridShape = Instantiate(layerInfo.Shape);
                gridShape.CellGap = CellGap;
                gridShape.ShapeOrientation = MapOrientation;
                gridShape.Scale = shapeScale;
                
                gridShape.UpdateShape();
                
                layerInfo.Shape = gridShape;
                
                gridShapes.Add(gridShape);
            }

            // add layer to all grid chunks
            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.AddLayer(layerInfo);
            }

            if (setBaselayer || meshLayerInfos.Count == 1)
            {
                baseLayerId = layerInfo.LayerId;

                ValidateChunkPositions();
                ValidateGridBounds();
            }

            return true;
        }
        public MeshLayerInfo GetLayerInfo(string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            return meshLayerInfos[layerId];
            
        }
        public enum SortAxis { X, Y, Z }
        public void SortMeshLayers()
        {
            if (meshLayerInfos.Count == 0)
            {
                return;
            }

            List<MeshLayerInfo> orderedLayers = meshLayerInfos.Values.OrderBy(x => x.OrderInLayer).ToList();

            int i = 0;

            int previousOrder = int.MinValue;
            float offset = 0;

            // the reason we have this is because, consecutive calls to sort the layers will result in the offset position progressively getting higher. Thus, we assume that the position of the parent gridObject is the absolute lowest position all mesh layers will start from.
            float baseLocation = 0;
            Vector3 gridPos = transform.position;

            // the orientation of the map determines which axis we must sort againsts.
            // For example, if the grid is displayed along the XY axis, then we determine sorting by moving the layers forward or backward on the Z axis
            if(MapOrientation ==  GridShape.Orientation.XY)
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

            foreach (MeshLayerInfo layer in orderedLayers)
            {
                int order = layer.OrderInLayer;

                if (order > previousOrder)
                {
                    offset = MeshLayerInfo.SortStep * i++;
                }
            
                foreach (GridChunk chunk in sortedChunks.Values)
                {
                    chunk.SortLayer(layer.LayerId, layerSortAxis, offset);
                }

                previousOrder = order;
            }
        }
        
        /// <summary>
        /// Will return a dictionary, where for every LayerId, give the current position of its corresponding layerObject. This is useful if you want to know the position of the layers such that you can know how/where to place other objects such as sprites
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, Vector3> GetMeshLayerPositions()
        {
            GridChunk start = sortedChunks.Values.First();
            Dictionary<string, Vector3> mlPos = new Dictionary<string, Vector3>();

            foreach(MeshLayerInfo info in meshLayerInfos.Values)
            {
                MeshLayer ml = start.GetMeshLayer(info.LayerId);
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
                visualProps.Add(data);
            }
        }

        /// <summary>
        /// Will create a visual data which will render the shape at the given position the given color
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
                chunk.SetColor(gridPosition, color, layerId);
            }
        }
        private void InsertPositionBlock_Fast(List<Vector2Int> positions, List<ShapeVisualData> datas, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            if (positions.Count != datas.Count)
            {
                Debug.LogError("When Inserting as Block, The number of positions and datas must be the same");
                return;
            }

            Dictionary<GridChunk, ConcurrentBag<int>> chunkIndex = new Dictionary<GridChunk, ConcurrentBag<int>>();

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

            Parallel.ForEach(chunkIndex, item =>
            {
                GridChunk chunk = item.Key;
                ConcurrentBag<int> indexList = item.Value;

                foreach (int i in indexList)
                {
                    chunk.QuickInsertVisualData(positions[i], datas[i], layerId);
                }
            });
        }

        public void InsertPositionBlock(List<Vector2Int> positions, List<ShapeVisualData> datas, string layerId = USE_DEFAULT_LAYER)
        {
            if(UseMultithreading)
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
                }
            }
        }


        /// <summary>
        /// Removes the visual data at the given grid position from the given layer
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
            ValidateLayerId(ref baseLayerId);

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
        /// Set whether to use visual equality at the given layer. Note, that you will have to reinsert the visual data so that the changes take effect. It is not enough to call DrawGrid()
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
                chunk.DrawChunk();
            }
        }

        /// <summary>
        /// Update a specific layer of a specific chunk at the given position
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
        public void ValidateOrientation()
        {
            TimeLogger.ClearTimers();
            TimeLogger.StartTimer(67234, "Validate Orientation");
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
            
            TimeLogger.StopTimer(67234);

            TimeLogger.LogAll();
        }
        private void ValidateGridBounds()
        {
            Vector2Int min = Vector2Int.zero;
            Vector2Int max = gridSize;

            GridShape shape = null;

            sortedChunks.Values.First().TryGetLayerShape(baseLayerId, out shape);

            localBounds = shape.GetGridBounds(min, max);
        }
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
        public void RedrawLayer(string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            foreach (GridChunk chunk in sortedChunks.Values)
            {
                chunk.DrawLayer(layerId);
            }

            SortMeshLayers();
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
        /// Returns the Shape of the layer at the given grid position.
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

        // use base layer for these positions

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

        public bool ContainsGridPosition(Vector2Int gridPosition)
        {
            return gridBounds.Contains((Vector3Int) gridPosition);
        }

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

        public Plane plane = new Plane(Vector3.up, Vector3.zero);
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
        /// <param timerName="LayerId"></param>
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
       
        /// <summary>
        /// Will update the visualHash of all the hashes in the map.
        /// Do not call unless you need to. Make sure to disable updateOnVisual change in the meshlayer class
        /// </summary>
        public void ValidateAllVisualHashes()
        {
            foreach (ShapeVisualData vp in visualProps)
            {
                vp.ValidateVisualHash();
            }
        }

        #endregion

        #region Sprite Spawning

        public void SpawnSprite(Vector2Int gridPosition, Sprite sprite, string layerId = USE_DEFAULT_LAYER)
        {
            ValidateLayerId(ref layerId);

            GridChunk chunk = GetHexChunk(gridPosition);

            if (chunk != null)
            {
                chunk.SpawnSprite(gridPosition, sprite, layerId);
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
                sortedChunks.Add(grid.StartPosition.GetHashCode_Unique(), grid);
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
                gridSize = gridManager.GridSize; ;
                
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
        public string GetMapDescription()
        {
            string s1 = "Map size(Chunk Size): " + gridSize.x + " X " + gridSize.y;
            string s2 = "(" + chunkSize.x + " X " + chunkSize.y + ")";
            string s3 = "\nUses Multithreaded Chunks: " + UseMultithreading;

            return s1 + s2 + s3;
        }
    }

    public struct MeshLayerInfo
    {
        public static float SortStep = 0.0001f;

        public string LayerId { get; private set; }
        public GridShape Shape { get; set; }
        public int OrderInLayer { get; set; }

        public bool UseVisualEquality { get; set; }
        public MeshLayerInfo(string layerId, GridShape shape, bool useVisualEquality = false, int orderInLayer = 0)
        {
            LayerId = layerId;
            Shape = shape;
            OrderInLayer = orderInLayer;
            UseVisualEquality = useVisualEquality;
        }

        public override int GetHashCode()
        {
            return LayerId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(MeshLayerInfo))
            {
                MeshLayerInfo mli = (MeshLayerInfo)obj;
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