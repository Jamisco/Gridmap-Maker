using System;
using System.Collections.Generic;
using UnityEngine;
using static GridMapMaker.GridManager;

namespace GridMapMaker
{
    /// <summary>
    /// A grid chunk is a collection of mesh layers. Each grid chunk its like its own grid manager, but with a limited grid bounds. Grid chunks operate with little help from the gridManager
    /// </summary>
    public class GridChunk : MonoBehaviour
    {
        public GridManager GridManager { get; private set; }

        private Dictionary<string, MeshLayer> ChunkLayers = new Dictionary<string, MeshLayer>();

        [SerializeField]
        private Vector2Int startGridPosition;

        [SerializeField]
        private Vector2Int endGridPosition;

        [SerializeField]
        private BoundsInt chunkGridBounds;

        [SerializeField]
        private Bounds chunkLocalBounds;

        /// <summary>
        /// Start grid position of the chunk localBounds
        /// </summary>
        /// 
        public Vector2Int StartPosition { get { return startGridPosition; } }

        public Vector2Int EndPosition { get { return endGridPosition; } }

        public BoundsInt ChunkGridBounds { get { return chunkGridBounds; } }
               
        public Bounds ChunkLocalBounds
        {
            get
            {
                return chunkLocalBounds;
            }
        }

        /// <summary>
        /// Note that colliders only take into account the bounds of the chunk regardless of whether the tiles are empty or not.
        /// </summary>
        /// 

        [SerializeField]
        private ColliderType chunkColliderType = ColliderType.None;
        public ColliderType ChunkColliderType
        {
            get
            {
                return chunkColliderType;
            }
            set
            {
                chunkColliderType = value;
                ValidateCollider();
            }
        }

        /// <summary>
        /// If orientation is in 3D, the collider will be a mesh collider. If orientation is in 2D, the collider will be a 2d collider.
        /// </summary>
        public Collider2D ChunkCollider_XY
        {
            get
            {
                return GetComponent<Collider2D>();
            }
        }

        /// <summary>
        /// If orientation is in 3D, the collider will be a mesh collider. If orientation is in 2D, the collider will be a 2d collider.
        /// </summary>
        public Collider ChunkCollider_XZ
        {
            get
            {
                return GetComponent<MeshCollider>();
            }
        }

        public void Initialize(GridManager grid, BoundsInt gridBounds, ColliderType col)
        {
            GridManager = grid;

            startGridPosition = (Vector2Int)gridBounds.min;
            endGridPosition = (Vector2Int)gridBounds.max - Vector2Int.one;

            chunkGridBounds = gridBounds;

            chunkGridBounds.zMin = 0;
            chunkGridBounds.zMax = 1;

            chunkColliderType = col;

            // a chunk local position is simply the position of the first cell in the chunk
            // thus, the chunk position can only be known after a layer has been added
            // see updatelocalposition method
        }

        private MeshLayer CreateLayer()
        {
            MeshLayer newLayer
                = new GameObject().AddComponent<MeshLayer>();

            newLayer.transform.SetParent(transform, false);

            return newLayer;
        }


        /// <summary>
        /// Initialize the layer with the given layer setting. If the layer already exists, it will be re-initialized with the new layer settings.
        /// </summary>
        /// <param name="layerSettings"></param>
        public void InitializeLayer(MeshLayerSettings layerSettings)
        {
            if (!ChunkLayers.ContainsKey(layerSettings.LayerId))
            {
                MeshLayer newLayer = CreateLayer();

                newLayer.Initialize(layerSettings, this);
                ChunkLayers.Add(layerSettings.LayerId, newLayer);
            }
            else
            {
                MeshLayer newLayer = ChunkLayers[layerSettings.LayerId];
                newLayer.Initialize(layerSettings, this);
            }
        }

        public bool HasLayer(string layerId)
        {
            return ChunkLayers.ContainsKey(layerId);
        }
        public void RemoveLayer(string uniqueID)
        {
            if (ChunkLayers.ContainsKey(uniqueID))
            {
                ChunkLayers[uniqueID].Clear();
                ChunkLayers.Remove(uniqueID);
                Destroy(ChunkLayers[uniqueID].gameObject);
            }
        }
        public MeshLayer GetMeshLayer(string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                return ChunkLayers[layerId];
            }

            return null;
        }
        public void InsertVisualData(Vector2Int gridPosition, ShapeVisualData visualProp, string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId) && ContainsGridPosition(gridPosition))
            {
                ChunkLayers[layerId].InsertVisualData(gridPosition, visualProp);
            }
        }
        public bool TryInsertVisualData(Vector2Int gridPosition, ShapeVisualData visualProp, string layerId)
        {
            MeshLayer ml = null;
            ChunkLayers.TryGetValue(layerId, out ml);

            if (ml != null && ContainsGridPosition(gridPosition))
            {
                ml.InsertVisualData(gridPosition, visualProp);
                return true;
            }

            return false;
        }

        public void QuickInsertVisualData(Vector2Int gridPosition, ShapeVisualData visualProp, string layerId)
        {
            ChunkLayers[layerId].InsertVisualData(gridPosition, visualProp);
        }
        public bool CanInsert(Vector2Int gridPosition, string layerId)
        {
            MeshLayer ml = null;
            ChunkLayers.TryGetValue(layerId, out ml);

            if (ml != null && ContainsGridPosition(gridPosition))
            {
                return true;
            }

            return false;
        }

        public void RemoveVisualData(Vector2Int gridPosition, string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].RemoveVisualData(gridPosition);
            }
        }


        /// <summary>
        /// Removes all visual data from all layers at the specified grid position
        /// </summary>
        /// <param timerName="gridPosition"></param>
        public void RemoveVisualData(Vector2Int gridPosition)
        {
            foreach (MeshLayer layer in ChunkLayers.Values)
            {
                layer.RemoveVisualData(gridPosition);
            }
        }

        public void RemoveAllVisualData(string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].RemoveAllVisualData();
            }
        }

        public void SetVisualEquality(string layerId, bool useVisualEquality)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].UseVisualEquality = useVisualEquality;
            }
        }
        public void SetVisualEquality(bool useVisualEquality)
        {
            foreach (MeshLayer layer in ChunkLayers.Values)
            {
                layer.UseVisualEquality = useVisualEquality;
            }
        }

        public void SetGridShape(string layerId, GridShape shape)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].LayerGridShape = shape;
            }
        }
        public void SetGridShape(GridShape shape)
        {
            foreach (MeshLayer layer in ChunkLayers.Values)
            {
                layer.LayerGridShape = shape;
            }
        }
        public bool ContainsGridPosition(Vector2Int gridPosition)
        {
            Vector3Int boundsPosition = (Vector3Int)gridPosition;

            // the reason we check this way is because bounds are not inclusive on the max side, so we need to check if the grid position is within the bounds of the chunk.
            return
                boundsPosition.x >= chunkGridBounds.xMin &&
                boundsPosition.x <= chunkGridBounds.xMax &&
                boundsPosition.y >= chunkGridBounds.yMin &&
                boundsPosition.y <= chunkGridBounds.yMax &&
                boundsPosition.z >= chunkGridBounds.zMin &&
                boundsPosition.z <= chunkGridBounds.zMax;
        }
        public bool ContainsLocalPosition(Vector3 localPosition, string layerId)
        {
            if (GetLayerBounds(layerId, out Bounds bounds))
            {
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;

                // the reason we check this way is because bounds are not inclusive on the max side, so we need to check if the local position is within the bounds of the layer.
                return
                    localPosition.x >= min.x && localPosition.x <= max.x &&
                    localPosition.y >= min.y && localPosition.y <= max.y &&
                    localPosition.z >= min.z && localPosition.z <= max.z;
            }

            return false;
        }

        /// <summary>
        /// Will return the bounds of a given layer. If layer doesnt exist, it will return an empty bounds.
        /// </summary>
        /// <param timerName="LayerId"></param>
        /// <returns></returns>
        public bool GetLayerBounds(string layerId, out Bounds bounds)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                GridShape shape = ChunkLayers[layerId].LayerGridShape;

                bounds = shape.GetGridBounds(startGridPosition, endGridPosition);

                return true;
            }

            bounds = new Bounds();
            return false;
        }

        /// <summary>
        /// Will Find the first layer with the given Id, and return its Shape
        /// </summary>
        /// <param timerName="Shape"></param>
        /// <param timerName="layerId"></param>
        /// <returns></returns>
        public bool TryGetLayerShape(string layerId, out GridShape shape)
        {
            shape = null;

            if (ChunkLayers.ContainsKey(layerId))
            {
                shape = ChunkLayers[layerId].LayerGridShape;

                return true;
            }

            return false;
        }
        /// <summary>
        /// Will verify that the position is within the chunk first, and then find the layer with the given id
        /// </summary>
        /// <param timerName="localPosition"></param>
        /// <param timerName="LayerId"></param>
        /// <returns></returns>
        public bool TryGetLayerShape(Vector3 localPosition,
                                    string layerId, out GridShape shape)
        {
            // we would simply need to confirm that the localposition is within the bounds of this grid chunk, then find the layer.

            if (ContainsLocalPosition(localPosition, layerId))
            {
                return TryGetLayerShape(layerId, out shape);
            }

            shape = null;
            return false;
        }

        public bool TryGetGridPosition(Vector3 localPosition,
                                       out Vector2Int gridPosition)
        {
            GridShape shape;

            if (TryGetLayerShape(GridManager.BaseLayer,
                                                out shape))
            {
                gridPosition = shape.GetGridCoordinate(localPosition);
                return true;
            }

            gridPosition = Vector2Int.left;
            return false;
        }

        public bool ContainsVisualData(Vector2Int gridPosition, string layerId)
        {
            if (!ContainsGridPosition(gridPosition))
            {
                return false;
            }

            if (ChunkLayers.ContainsKey(layerId))
            {
                return ChunkLayers[layerId].HasVisualData(gridPosition);
            }

            return false;
        }

        public ShapeVisualData GetVisualData(Vector2Int gridPosition, string layerId)
        {
            if (!ContainsGridPosition(gridPosition))
            {
                return null;
            }

            if (ChunkLayers.ContainsKey(layerId))
            {
                return ChunkLayers[layerId].GetVisualData(gridPosition);
            }

            return null;
        }

        /// <summary>
        /// Sorts the layer by the given axis, and offset. The offset is used to move the layer in the direction of the axis. Used to draw layers on top of each other.
        /// </summary>
        /// <param name="layerId"></param>
        /// <param name="axis"></param>
        /// <param name="offset"></param>
        public void SortLayer(string layerId, SortAxis axis, float offset)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].SortLayer(axis, offset);
            }
        }
        /// <summary>
        /// Will swap the Y and Z axis of the chunk, and all its layers. There is no check for the validity of the swap, so be sure to only call this when needed.
        /// </summary>
        public void ValidateOrientation()
        {
            gameObject.transform.localPosition =
                SwapYZ(gameObject.transform.localPosition);

            foreach (MeshLayer layer in ChunkLayers.Values)
            {
                layer.ValidateOrientation();
            }

            Vector3 SwapYZ(Vector3 vector)
            {
                return new Vector3(vector.x, vector.z, vector.y);
            }
        }
        private void ValidateCollider()
        {
            if (ChunkLayers.Count == 0)
            {
                // no layers, no collider
                return;
            }

            Collider2D collider = GetComponent<Collider2D>();
            MeshCollider meshC = GetComponent<MeshCollider>();

            switch (chunkColliderType)
            {
                case ColliderType.None:

                    if (collider != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(collider);
#else
                        Destroy(collider);
#endif
                    }

                    if (meshC != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(meshC);
#else
                        Destroy(meshC);
#endif
                    }


                    break;
                case ColliderType.BoxCollider2D:

                    if (meshC != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(meshC);
#else
                        Destroy(meshC);
#endif
                    }

                    if (collider == null)
                    {
                        collider = gameObject.AddComponent<BoxCollider2D>();
                    }

                    BoxCollider2D b = collider as BoxCollider2D;

                    Bounds temp = ChunkLayers[GridManager.BaseLayer].LayerBounds;

                    b.size = temp.size;
                    b.offset = temp.center;
                    break;

                case ColliderType.MeshCollider:
                case ColliderType.MeshCollider_Convex:

                    if (collider != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(collider);
#else
                        Destroy(collider);
#endif
                    }

                    if (meshC == null)
                    {
                        meshC = gameObject.AddComponent<MeshCollider>();
                    }

                    Mesh fullMesh = ChunkLayers[GridManager.BaseLayer].FullMesh;

                    meshC.sharedMesh = fullMesh;

                    if (chunkColliderType == ColliderType.MeshCollider_Convex)
                    {
                        meshC.convex = true;
                    }

                    break;
                default:
                    break;
            }
        }

        public void ValidateLocalPosition()
        {
            // you need this because chunk position are by default 0,0,
            // chunks local position is the position of the first cell in the chunk
            GridShape shape = ChunkLayers[GridManager.BaseLayer].LayerGridShape;

            chunkLocalBounds = shape.GetGridBounds(startGridPosition, endGridPosition);

            Vector3 pos = shape.GetTesselatedPosition(startGridPosition);

            gameObject.transform.localPosition = pos;
        }

        public void FusedMeshGroups()
        {
            foreach (MeshLayer layer in ChunkLayers.Values)
            {
                layer.FusedMeshGroups();
            }
        }
        public void DrawFusedMesh()
        {
            foreach (MeshLayer layer in ChunkLayers.Values)
            {
                layer.DrawFusedMesh();
            }

            ValidateCollider();

        }
        public void DrawLayer(string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].DrawLayer();
            }

            if (layerId == GridManager.BaseLayer)
            {
                ValidateCollider();
            }
        }
        public void DrawChunk()
        {
            foreach (MeshLayer layer in ChunkLayers.Values)
            {
                layer.DrawLayer();
            }

            ValidateCollider();

        }

        public void Clear()
        {
            try
            {
                foreach (KeyValuePair<string, MeshLayer> layer in ChunkLayers)
                {
                    layer.Value.Clear();
                }

                ChunkLayers.Clear();
            }
            catch (Exception)
            {

            }

#if UNITY_EDITOR

            DestroyImmediate(transform.gameObject);
#else
            Destroy(transform.gameObject);
#endif
        }
    }
}
