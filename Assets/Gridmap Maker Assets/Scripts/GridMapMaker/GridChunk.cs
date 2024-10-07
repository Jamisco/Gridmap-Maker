using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridMapMaker
{
    /// <summary>
    /// A grid chunk is a collection of mesh layers. Each grid chunk its like its own grid manager, but with a limited grid bounds. Grid chunks operate with little help from the gridManager
    /// </summary>
    public class GridChunk : MonoBehaviour
    {
        public GridManager GridManager { get; private set; }

        public Dictionary<string, MeshLayer> ChunkLayers = new Dictionary<string, MeshLayer>();

        [SerializeField]
        private Vector2Int startPosition;

        [SerializeField]
        private Vector2Int endPosition;

        [SerializeField]
        private BoundsInt chunkGridBounds;

        /// <summary>
        /// Start grid position of the chunk localBounds
        /// </summary>
        /// 
        public Vector2Int StartPosition { get { return startPosition; } }

        public Vector2Int EndPosition { get { return endPosition; } }

        public BoundsInt ChunkGridBounds { get { return chunkGridBounds; } }

        public void Initialize(GridManager grid, BoundsInt gridBounds)
        {
            GridManager = grid;

            startPosition = (Vector2Int)gridBounds.min;
            endPosition = (Vector2Int)gridBounds.max;

            chunkGridBounds = gridBounds;

            chunkGridBounds.zMin = 0;
            chunkGridBounds.zMax = 1;

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
        public void AddLayer(MeshLayerSettings layerInfo)
        {
            if (!ChunkLayers.ContainsKey(layerInfo.LayerId))
            {
                MeshLayer newLayer = CreateLayer();

                newLayer.Initialize(layerInfo, this);
                ChunkLayers.Add(layerInfo.LayerId, newLayer);
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

        public void ValidateLocalPosition()
        {
            string layer = GridManager.BaseLayer;

            if (string.IsNullOrEmpty(layer))
            {
                return;
            }

            // a chunk local position is simply the position of the first cell in the chunk
            Vector3 pos = ChunkLayers[layer].LayerGridShape.GetTesselatedPosition(startPosition);

            gameObject.transform.localPosition = pos;
        }
        public void InsertVisualData(Vector2Int gridPosition, ShapeVisualData visualProp, string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId) && ContainsPosition(gridPosition))
            {
                ChunkLayers[layerId].InsertVisualData(gridPosition, visualProp);
            }
        }
        public bool TryInsertVisualData(Vector2Int gridPosition, ShapeVisualData visualProp, string layerId)
        {
            MeshLayer ml = null;
            ChunkLayers.TryGetValue(layerId, out ml);

            if (ml != null && ContainsPosition(gridPosition))
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

            if (ml != null && ContainsPosition(gridPosition))
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
        /// Delete the cell at the given grid position from all layers
        /// </summary>
        /// <param name="gridPosition"></param>
        public void DeletePosition(Vector2Int gridPosition)
        {
            foreach (MeshLayer layer in ChunkLayers.Values)
            {
                layer.DeleteShape(gridPosition);
            }
        }

        /// <summary>
        /// Deletes the cell at the given grid position from the given layer 
        /// </summary>
        /// <param name="gridPosition"></param>
        /// <param name="layerId"></param>
        public void DeletePosition(Vector2Int gridPosition, string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].DeleteShape(gridPosition);
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

        public bool ContainsPosition(Vector2Int gridPosition)
        {
            Vector3Int boundsPosition = (Vector3Int)gridPosition;

            if (ChunkGridBounds.Contains(boundsPosition))
            {
                return true;
            }

            return false;
        }
        public bool ContainsPosition(Vector3 localPosition, string layerId)
        {
            Bounds bounds = GetLayerBounds(layerId);

            if (bounds.Contains(localPosition))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Will give the bounds the layer is confined too within the chunk
        /// </summary>
        /// <param timerName="LayerId"></param>
        /// <returns></returns>
        public Bounds GetLayerBounds(string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                return ChunkLayers[layerId].LayerBounds;
            }

            return new Bounds();
        }

        /// <summary>
        /// Will give the bounds the layer is confined too within the chunk
        /// </summary>
        /// <param timerName="LayerId"></param>
        /// <returns></returns>
        public Bounds GetDefaultLayerBounds()
        {
            string layerId = GridManager.BaseLayer;

            return GetLayerBounds(layerId);
        }

        ///// <summary>
        ///// Will give the bounds the layer is confined too within the chunk
        ///// </summary>
        ///// <param timerName="LayerId"></param>
        ///// <returns></returns>
        //public Bounds GetLayerMeshBounds(string LayerId)
        //{
        //    if (ChunkLayers.ContainsKey(LayerId))
        //    {
        //        return ChunkLayers[LayerId].MeshBounds;
        //    }

        //    return new Bounds();
        //}

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

            if (ContainsPosition(localPosition, layerId))
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
            if (!ContainsPosition(gridPosition))
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
            if (!ContainsPosition(gridPosition))
            {
                return null;
            }

            if (ChunkLayers.ContainsKey(layerId))
            {
                return ChunkLayers[layerId].GetVisualData(gridPosition);
            }

            return null;
        }

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
        }
        public void DrawLayer(string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].DrawLayer();
            }
        }
        public void DrawChunk()
        {
            foreach (MeshLayer layer in ChunkLayers.Values)
            {
                layer.DrawLayer();
            }
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
        public SerializedGridChunk GetSerializedChunk()
        {
            SerializedGridChunk serializedChunk = new SerializedGridChunk(this);
            return serializedChunk;
        }
    }

    /// <summary>
    /// A serialized version of the GridChunk class. Used to save and load grid chunks
    /// </summary>
    [Serializable]
    public struct SerializedGridChunk
    {
        [SerializeField]
        public Vector2Int startPosition;

        [SerializeField]
        public List<SerializedMeshLayer> serializedLayers;
        public SerializedGridChunk(GridChunk chunk)
        {
            startPosition = chunk.StartPosition;
            serializedLayers = new List<SerializedMeshLayer>();

            foreach (MeshLayer item in chunk.ChunkLayers.Values)
            {
                serializedLayers.Add(item.GetSerializedLayer());
            }
        }
    }

}
