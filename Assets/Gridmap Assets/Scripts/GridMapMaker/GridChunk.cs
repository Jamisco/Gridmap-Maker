using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using Assets.Gridmap_Assets.Scripts.Mapmaker;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using static Assets.Gridmap_Assets.Scripts.Mapmaker.LayeredMesh;

namespace Assets.Scripts.GridMapMaker
{
    public class GridChunk : MonoBehaviour
    {
        // A grid chunk is a collection of layered meshes.

        Dictionary<string, LayeredMesh> ChunkLayers = new Dictionary<string, LayeredMesh>();

        [ShowOnlyField]
        public Vector2Int smallestPosition;
        [ShowOnlyField]
        public Vector2Int largestPosition;

        private void OnValidate()
        {
            ValidatePosition();
        }

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
        //        newLayer.Initialize(uniqueID, shape);
        //        ChunkLayers.Add(uniqueID, newLayer);
        //    }
        //}

        //public void InsertShape(string uniqueID, Vector2Int gridPosition, 
        //                                    ShapeVisualData visualData)
        //{
        //    if (ChunkLayers.ContainsKey(uniqueID))
        //    {
        //        ChunkLayers[uniqueID].InsertVisualData(visualData, gridPosition);
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
        //                                         ShapeVisualData visualData)
        //{
        //    //if (ChunkLayers.ContainsKey(uniqueID))
        //    //{
        //    //    ChunkLayers[uniqueID].RemoveVisualData(visualData, gridPosition);
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

        private void ValidatePosition()
        {
            //Vector2Int smallest = new Vector2Int(int.MaxValue, int.MaxValue);
            //Vector2Int largest = new Vector2Int(int.MinValue, int.MinValue);

            //foreach (LayeredMesh layer in ChunkLayers.Values)
            //{
            //    if(layer.SmallestPosition.IsLessThan(smallest))
            //    {
            //        smallest = layer.SmallestPosition;
            //    }

            //    if(layer.LargestPosition.IsGreaterThan(largest))
            //    {
            //        largest = layer.LargestPosition;
            //    }
            //}
        }

        public void DrawLayers()
        {
            foreach (LayeredMesh layer in ChunkLayers.Values)
            {
                layer.UpdateMesh();
            }
        }

        public void Clear()
        {
            foreach (KeyValuePair<string, LayeredMesh> layer in ChunkLayers)
            {
                Destroy(layer.Value.gameObject);
            }

            ChunkLayers.Clear();
        }
    }

}
