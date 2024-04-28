using Assets.Gridmap_Assets.Scripts.GridMapMaker;
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

    [RequireComponent(typeof(SpriteLayer))]
    public class GridChunk : MonoBehaviour
    {
        // A grid chunk is a collection of layered meshes.

        GridManager gridManager;

        Dictionary<string, LayeredMesh> ChunkLayers = new Dictionary<string, LayeredMesh>();

        SpriteLayer spriteLayer;

        [SerializeField]
        private Vector2Int startPosition;

        [SerializeField]
        private Vector2Int endPosition;

        [SerializeField]
        private BoundsInt chunkGridBounds;

        /// <summary>
        /// Start grid position of the chunk gridBounds
        /// </summary>
        /// 
        public Vector2Int StartPosition { get { return startPosition; } }

        public Vector2Int EndPosition { get { return endPosition; } }

        public BoundsInt ChunkGridBounds { get { return chunkGridBounds; } }

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
        //public void CreateLayer(string uniqueID, GridShape shape)
        //{         
        //    if (!ChunkLayers.ContainsKey(uniqueID))
        //    {
        //        LayeredMesh newLayer = new GameObject(timerName).AddComponent<LayeredMesh>();
        //        newLayer.transform.parent = transform;
        //        newLayer.gameObject.timerName = $"Layer {ChunkLayers.Count}";
        //        newLayer.Deserialize(uniqueID, shape);
        //        ChunkLayers.Add(uniqueID, newLayer);
        //    }
        //}

        //public void InsertVisualData(string uniqueID, Vector2Int gridPosition, 
        //                                    ShapeRenderData visualProp)
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
        //                                         ShapeRenderData visualProp)
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

            spriteLayer = GetComponent<SpriteLayer>();
        }

        private static LayeredMesh CreateLayer(Transform parent = null)
        {
            LayeredMesh newLayer
                = new GameObject().AddComponent<LayeredMesh>();

            newLayer.transform.parent = parent;
            return newLayer;
        }

        public void AddLayer(string uniqueID, GridShape shape, ShapeVisualData defaultVisual, bool useVisualEquality = false)
        {
            if (!ChunkLayers.ContainsKey(uniqueID))
            {
                LayeredMesh newLayer = CreateLayer(transform);

                newLayer.Initialize(uniqueID, shape, defaultVisual, useVisualEquality);

                ChunkLayers.Add(uniqueID, newLayer);


                spriteLayer.Initialize("Sprite Layer", shape);
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

        public void InsertVisualData(string layerId, Vector2Int gridPosition, ShapeVisualData visualProp)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                ChunkLayers[layerId].InsertVisualData(gridPosition, visualProp);
            }
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
            foreach (LayeredMesh layer in ChunkLayers.Values)
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
            foreach (LayeredMesh layer in ChunkLayers.Values)
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
            foreach (LayeredMesh layer in ChunkLayers.Values)
            {
                layer.UseVisualEquality = useVisualEquality;
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
        /// Will check if the given bounds are within the chunk
        /// Used the bounds of the base layer
        /// </summary>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public bool Contains_BaseLayer(Bounds bounds)
        {
            Bounds b = GetLayerBounds(gridManager.BaseLayer);
            
            if (bounds.Intersects(b))
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// Will give the bounds the layer is confined too within the chunk
        /// </summary>
        /// <param timerName="layerId"></param>
        /// <returns></returns>
        public Bounds GetLayerBounds(string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                return ChunkLayers[layerId].LayerGridShape.GetGridBounds(startPosition, endPosition);
            }

            return new Bounds();
        }

        /// <summary>
        /// Will give the bounds the layer is confined too within the chunk
        /// </summary>
        /// <param timerName="layerId"></param>
        /// <returns></returns>
        public Bounds GetLayerMeshBounds(string layerId)
        {
            if (ChunkLayers.ContainsKey(layerId))
            {
                return ChunkLayers[layerId].MeshBounds;
            }

            return new Bounds();
        }

        /// <summary>
        /// Will Find the first layer with the given Id, and return its shape
        /// </summary>
        /// <param timerName="shape"></param>
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
        /// <param timerName="layerId"></param>
        /// <returns></returns>
        public bool TryGetLayerShape(Vector3 localPosition,
                                    string layerId, out GridShape shape)
        {
            // we would simply need to confirm that the localposition is within the bounds of this grid chunk, then find the layer.

            if(ContainsPosition(localPosition, layerId))
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
  
            if (TryGetLayerShape(gridManager.BaseLayer,
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

        public ShapeVisualData GetVisualProperties(Vector2Int gridPosition, string layerId)
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

        public void RedrawLayer(string layerId)
        {
            ChunkLayers[layerId].RedrawLayer();
        }

        public void RedrawChunk()
        {
            foreach (LayeredMesh layer in ChunkLayers.Values)
            {
                layer.RedrawLayer();
            }
        }

        public void SpawnSprite(Vector2Int position, Sprite sprite)
        {
            spriteLayer.InsertSprite(position, sprite);
        }

        public void Clear()
        {
            try
            {
                foreach (KeyValuePair<string, LayeredMesh> layer in ChunkLayers)
                {
                    layer.Value.Clear();
                }

                ChunkLayers.Clear();
                spriteLayer.Clear();
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

        public static GridChunk DeserializeData(SerializedGridChunk data, MapVisualContainer visualContainer, List<ShapeVisualData> visualProps)
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
