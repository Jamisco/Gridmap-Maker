using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Gridmap_Assets.Scripts.Mapmaker;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;
using static Assets.Gridmap_Assets.Scripts.Mapmaker.LayeredMesh;

namespace Assets.Scripts.GridMapMaker
{
    public class GridChunk : MonoBehaviour
    {
        // A grid chunk is a collection of layered meshes.

        GridManager gridManager;

        Dictionary<string, LayeredMesh> ChunkLayers = new Dictionary<string, LayeredMesh>();

        [SerializeField]
        private Vector2Int startPosition;

        [SerializeField]
        private Vector2Int endPosition;

        [SerializeField]
        private BoundsInt chunkGridBounds;

        [SerializeField]
        private Bounds chunkBounds;

        /// <summary>
        /// Start grid position of the chunk gridBounds
        /// </summary>
        /// 
        public Vector2Int StartPosition { get { return startPosition; } }

        public Vector2Int EndPosition { get { return endPosition; } }

        public BoundsInt ChunkGridBounds { get { return chunkGridBounds; } }

        public Bounds ChunkBounds { get { return chunkBounds; } }

        //public void ValidateChunkLayers()
        //{
        //    // get all the child objects that have a layered mesh component and make sure they are in the dictionary.
        //    foreach (Transform child in transform)
        //    {
        //        LayeredMesh layeredMesh = child.GetComponent<LayeredMesh>();

        //        if (layeredMesh != null)
        //        {
        //            if (!ChunkLayers.ContainsKey(layeredMesh.UniqueId))
        //            {
        //                ChunkLayers.Add(layeredMesh.UniqueId, layeredMesh);
        //            }
        //        }
        //    }
        //}
        //public void AddLayer(string uniqueID, GridShape shape)
        //{         
        //    if (!ChunkLayers.ContainsKey(uniqueID))
        //    {
        //        LayeredMesh newLayer = new GameObject(name).AddComponent<LayeredMesh>();
        //        newLayer.transform.parent = transform;
        //        newLayer.gameObject.name = $"Layer {ChunkLayers.Count}";
        //        newLayer.Deserialize(uniqueID, shape);
        //        ChunkLayers.Add(uniqueID, newLayer);
        //    }
        //}

        //public void InsertVisualData(string uniqueID, Vector2Int gridPosition, 
        //                                    ShapeVisualData visualProp)
        //{
        //    if (ChunkLayers.ContainsKey(uniqueID))
        //    {
        //        ChunkLayers[uniqueID].InsertVisualData(visualProp, gridPosition);
        //        ValidatePosition(uniqueID);
        //    }


        //}

        //public bool ShapeExist(string uniqueID, Vector2Int gridPosition)
        //{
        //    //if (ChunkLayers.ContainsKey(uniqueID))
        //    //{
        //    //    return ChunkLayers[uniqueID].HasShape(gridPosition);
        //    //}

        //    return false;
        //}

        //public void RemoveShape(string uniqueID, Vector2Int gridPosition,
        //                                         ShapeVisualData visualProp)
        //{
        //    //if (ChunkLayers.ContainsKey(uniqueID))
        //    //{
        //    //    ChunkLayers[uniqueID].RemoveVisualData(visualProp, gridPosition);
        //    //    ValidatePosition(uniqueID);
        //    //}
        //}

        //public void RemoveLayer(string uniqueID)
        //{
        //    if (ChunkLayers.ContainsKey(uniqueID))
        //    {
        //        ChunkLayers[uniqueID].Clear();
        //        ChunkLayers.Remove(uniqueID);
        //        Destroy(ChunkLayers[uniqueID].gameObject);
        //    }
        //}

        //private void ValidatePosition(string uniqueID)
        //{
        //    LayeredMesh layer = ChunkLayers[uniqueID];

        //    if (layer.SmallestPosition.IsLessThan(smallestPosition))
        //    {
        //        smallestPosition = layer.SmallestPosition;
        //    }

        //    if (layer.LargestPosition.IsGreaterThan(largestPosition))
        //    {
        //        largestPosition = layer.LargestPosition;
        //    }
        //}

        public void Initialize(GridManager grid, BoundsInt gridBounds)
        {
            gridManager = grid;
            
            startPosition = gridBounds.min.ToGridPos();
            endPosition = gridBounds.max.ToGridPos();

            chunkGridBounds = gridBounds;

            chunkGridBounds.yMin = 0;
            chunkGridBounds.yMax = 1;

            chunkBounds = gridManager.BaseShape.GetGridBounds(startPosition, EndPosition);
        }

        private static LayeredMesh CreateLayer(Transform parent = null)
        {
            LayeredMesh newLayer
                = new GameObject().AddComponent<LayeredMesh>();

            newLayer.transform.parent = parent;
            return newLayer;
        }

        public void AddLayer(string uniqueID, GridShape shape, VisualProperties defaultVisual)
        {
            // all shapes added to the next MUST be within the bounds of the base shape
            if(!gridManager.BaseShape.WithinShapeBounds(shape))
            {
                return;
            }

            if (!ChunkLayers.ContainsKey(uniqueID))
            {
                LayeredMesh newLayer = CreateLayer(transform);

                newLayer.Initialize(uniqueID, shape, defaultVisual);

                ChunkLayers.Add(uniqueID, newLayer);
            }
        }

        private LayeredMesh AddLayer(LayeredMesh layer)
        {
            if (!ChunkLayers.ContainsKey(layer.LayerId))
            {
                ChunkLayers.Add(layer.LayerId, layer);
                layer.transform.SetParent(transform);
                return layer;
            }

            return null;
        }

        public void InsertVisualData(string layerId, Vector2Int gridPosition, VisualProperties visualProp)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].InsertVisualData(gridPosition, visualProp);
            }
        }

        public void RemoveVisualData(string layerId, Vector2Int gridPosition)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].RemoveVisualData(gridPosition);
            }
        }


        /// <summary>
        /// Removes all visual data from all layers at the specified grid position
        /// </summary>
        /// <param name="gridPosition"></param>
        public void RemoveVisualData_AL(Vector2Int gridPosition)
        {
            foreach (LayeredMesh layer in ChunkLayers.Values)
            {
                layer.RemoveVisualData(gridPosition);
            }
        }

        public bool ContainsPosition(Vector2Int gridPosition)
        {
            Vector3Int boundsPosition = gridPosition.ToBoundsPos();

            if (ChunkGridBounds.Contains(boundsPosition))
            {
                return true;
            }

            return false;
        }
        public bool ContainsPosition(Vector3 localPosition)
        {
            if(chunkBounds.Contains(localPosition))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Will Find the first layer with the given Id, and return its shape
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="layerId"></param>
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
        /// <param name="localPosition"></param>
        /// <param name="layerId"></param>
        /// <returns></returns>
        public bool TryGetLayerShape(Vector3 localPosition,
                                    string layerId, out GridShape shape)
        {
            // we would simply need to confirm that the localposition is within the bounds of this grid chunk, then find the layer.

            if(ContainsPosition(localPosition))
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
  
            if (TryGetLayerShape(localPosition, gridManager.BaseLayer,
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
            if(!ContainsPosition(gridPosition))
            {
                return false;
            }

            if (ChunkLayers.ContainsKey(layerId))
            {
                return ChunkLayers[layerId].HasVisualData(gridPosition);
            }

            return false;
        }


        public VisualProperties GetVisualProperties(Vector2Int gridPosition, string layerId)
        {
            if (!ContainsPosition(gridPosition))
            {
                return null;
            }

            if (ChunkLayers.ContainsKey(layerId))
            {
                return ChunkLayers[layerId].GetVisualProperties(gridPosition);
            }

            return null;
        }

        public void UpdateLayers()
        {
            foreach (LayeredMesh layer in ChunkLayers.Values)
            {
                layer.UpdateMesh();
            }
        }

        public void UpdateLayer(string layerId)
        {
            ChunkLayers[layerId].UpdateMesh();
        }

        public void Clear()
        {
            try
            {
                foreach (KeyValuePair<string, LayeredMesh> layer in ChunkLayers)
                {
                    layer.Value.Clear();
                    DestroyImmediate(layer.Value.gameObject);
                }

                ChunkLayers.Clear();
            }
            catch (Exception)
            {

            }
        }

        public SerializedGridChunk SerializeChunk(MapVisualContainer container)
        {
            SerializedGridChunk serializedChunk = new SerializedGridChunk(this, container);
            return serializedChunk;
        }

        public static GridChunk DeserializeData(SerializedGridChunk data, MapVisualContainer visualContainer, List<VisualProperties> visualProps)
        {
            GridChunk chunk = new GameObject(data.name).AddComponent<GridChunk>();

            chunk.startPosition = data.startPosition;

            foreach (SerializedLayer item in data.serializedLayers)
            {
                LayeredMesh newLayer = CreateLayer(chunk.transform);

                newLayer.Deserialize(item, visualContainer, visualProps);
                chunk.AddLayer(newLayer);
            }

            return chunk;
        }

        [Serializable]
        public struct SerializedGridChunk
        {
            [SerializeField]
            public string name;

            [SerializeField]
            public Vector2Int startPosition;

            [SerializeField]
            public List<SerializedLayer> serializedLayers;

            public SerializedGridChunk(GridChunk chunk, MapVisualContainer container)
            {
                name = chunk.gameObject.name;
                startPosition = chunk.startPosition;

                serializedLayers = new List<SerializedLayer>();

                foreach (LayeredMesh item in chunk.ChunkLayers.Values)
                {
                    serializedLayers.Add(item.SerializeLayer(container));
                }
            }
        }
    }

}
