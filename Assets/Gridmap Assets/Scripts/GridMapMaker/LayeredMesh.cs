
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Gridmap_Assets.Scripts.Miscellaneous;
using Assets.Scripts.GridMapMaker;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Assets.Scripts.GridMapMaker.FusedMesh;
using static Assets.Scripts.GridMapMaker.GridManager;
using static Assets.Scripts.Miscellaneous.ExtensionMethods;
using Debug = UnityEngine.Debug;

namespace Assets.Gridmap_Assets.Scripts.Mapmaker
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    [Serializable]
    public class LayeredMesh : MonoBehaviour
    {
        /*
        5 meshes means 5 game objects to update, 5 renderer components to cull.
        1 mesh with 5 materials means 1 game object to update, 1 renderer component to cull.

        The benefit of 5 meshes is that each part can be separately culled if necessary, potentially reducing draw calls.
        The benefit of 1 mesh is less CPU overhead.
        */
        // A layered mesh is a collection of fused meshes that are then combined together to make one mesh.
        // Each fused mesh is a unique visually, meaning it may have a different texture, color, etc. However each fused mesh has thesame shape
        
        public GridShape LayerGridShape { get; private set; }

        [SerializeField]
        public List<ShapeVisualData> uniqueVisual = new List<ShapeVisualData>();

        private Dictionary<ShapeVisualData, FusedMesh> LayerFusedMeshes
                                 = new Dictionary<ShapeVisualData, FusedMesh>();

        private Dictionary<Vector2Int, int> GridVisualIds
                              = new Dictionary<Vector2Int, int>();

        private FusedMesh coloredFusedMesh;

        private Dictionary<Vector2Int, Color> GridColors
                      = new Dictionary<Vector2Int, Color>();

        public List<VisualProperties> VisualProps 
                                    = new List<VisualProperties>();

        private VisualProperties defaultVisualProp;

        GridComparer gridComparer = new GridComparer();

        private Mesh LayerMesh { get; set; }
        private Mesh shapeMesh;
        public Bounds MeshBounds
        {
            get
            {
                return meshRenderer.bounds;
            }
        }
        
        private string layerId;
        public string LayerId
        {
            get => layerId;
        }

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        private List<Material> SharedMaterials { get; set; } 
                                            = new List<Material>();
        public List<MaterialPropertyBlock> PropertyBlocks { get; private set; }
                                             = new List<MaterialPropertyBlock>();

        private SortingLayerPicker meshLayer;
        public SortingLayerPicker MeshLayer
        {
            get => meshLayer;
            set
            {
                meshLayer = value;
                SetLayerInfo();
            }
        }

        private void Reset()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
        }
        private void OnValidate()
        {
            foreach (VisualProperties visual in VisualProps)
            {
                visual.CheckVisualHashChanged();
            }
        }
        private void SetLayerInfo()
        {
            meshRenderer.sortingLayerID = meshLayer.LayerID;
            meshRenderer.sortingOrder = meshLayer.OrderInLayer;
        }
        private void SetMeshInfo()
        {
            if (meshRenderer == null || meshFilter == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshFilter = GetComponent<MeshFilter>();
            }
        }
        private void Initialize(GridShape gridShape)
        {
            Clear();
            LayerGridShape = gridShape;
            shapeMesh = gridShape.GetShapeMesh();
            SetMeshInfo();
        }     
        public void Initialize(string layerId, GridShape gridShape, VisualProperties defaultVisual)
        {
            Clear();
            
            gameObject.name = layerId;
            LayerGridShape = gridShape;
            this.layerId = layerId;
            shapeMesh = gridShape.GetShapeMesh();
            this.defaultVisualProp = defaultVisual;

            coloredFusedMesh = new FusedMesh();
            SetMeshInfo();
        }
         
        private void UpdateLayerBounds()
        {
            //// THIS Is useless, but I will keep it for now
            //List<Vector2Int> gridPositions = GridVisualIds.Keys.ToList();

            //gridPositions.Sort(gridComparer);

            //Vector2Int min = gridPositions[0];
            //Vector2Int max = gridPositions[gridPositions.Count - 1];

            //layerBounds = LayerGridShape.GetGridBounds(min, max);
        }
        void SetEvent(VisualProperties visualProp)
        {
            visualProp.VisualIdChange += VisualIdChanged;
        }

        void RemoveEvent(VisualProperties visualProp)
        {
            visualProp.VisualIdChange -= VisualIdChanged;
        }
    /// <summary>
        /// Insert visual data into the layered mesh. Will replace the visual data if it already exists.
        /// </summary>
        /// <param name="visualProp"></param>
        /// <param name="gridPosition"></param>
        public void InsertVisualData(Vector2Int gridPosition,
                                    VisualProperties visualProp)
        {
            int hash = gridPosition.GetHashCode_Unique();
            Vector3 offset = LayerGridShape.GetTesselatedPosition(gridPosition);
            ShapeVisualData visualData = visualProp.GetShapeVisualData();

            if (LayerFusedMeshes.ContainsKey(visualData))
            {
                LayerFusedMeshes[visualData].InsertMesh(shapeMesh, hash, offset);
            }
            else
            {
                FusedMesh fusedMesh = new FusedMesh();

                // if we are adding a new visualData, we must make sure that the grid we are adding it to does not already have a visualData
                
                if (GridVisualIds.ContainsKey(gridPosition))
                {
                    int visualId = GridVisualIds[gridPosition];
                    ShapeVisualData oldVisualData 
                        = VisualProps.FirstOrDefault
                                (x => x.VisualId == visualId)
                                 .GetShapeVisualData();

                    LayerFusedMeshes[oldVisualData].RemoveMesh(hash);
                }

                fusedMesh.InsertMesh(shapeMesh, hash, offset);
                LayerFusedMeshes.Add(visualData, fusedMesh);

                SetEvent(visualProp);
                VisualProps.Add(visualProp);
            }

            GridVisualIds[gridPosition] = visualProp.VisualId;
        }

        public void RemoveVisualData(Vector2Int gridPosition)
        {
            // removing a visual data is thesame as inserting a default visual data

            InsertVisualData(gridPosition, defaultVisualProp);
        }

        public void InsertShapeColor(Vector2Int gridPosition, Color color)
        {
            int hash = gridPosition.GetHashCode_Unique();
            Vector3 offset = LayerGridShape.GetTesselatedPosition(gridPosition);

            if (GridColors.ContainsKey(gridPosition))
            {
                GridColors[gridPosition] = color;
            }
            else
            {
                GridColors.Add(gridPosition, color);
            }

            Mesh newMesh = LayerGridShape.GetShapeMesh();

            newMesh.SetFullColor(color);

            coloredFusedMesh.InsertMesh(newMesh, hash, offset);
        }

        public VisualProperties GetVisualProperties(Vector2Int gridPosition)
        {
            if (GridVisualIds.ContainsKey(gridPosition))
            {
                int visualId = GridVisualIds[gridPosition];
                return VisualProps
                        .FirstOrDefault(x => x.VisualId == visualId);
            }
            
            return null;
        }

        public bool HasVisualData(Vector2Int gridPosition)
        {
            return GridVisualIds.ContainsKey(gridPosition);
        }

        //public void InsertVisualData(VisualProperties visualProp,
        //                                    Vector2Int gridPosition)
        //{ 
        //    int hash = gridPosition.GetHashCode_Unique();
        //    Vector3 offset = LayerGridShape.GetTesselatedPosition(gridPosition);
        //    ShapeVisualData visualData = visualProp.GetShapeVisualData();

        //    if (LayerFusedMeshes.ContainsKey(visualData))
        //    {
        //        LayerFusedMeshes[visualData].InsertMesh(shapeMesh, hash, offset);
        //    }
        //    else
        //    {
        //        FusedMesh fusedMesh = new FusedMesh();
        //        fusedMesh.InsertMesh(shapeMesh, hash, offset);
        //        LayerFusedMeshes.Add(visualData, fusedMesh);

        //        SetEvent(visualProp);
        //        VisualProps.Add(visualProp);
        //    }
        //}

        public void VisualIdChanged(int oldVisualId, ShapeVisualData newData)
        {
            ShapeVisualData oldData = LayerFusedMeshes.Keys.FirstOrDefault
                                    (x => x.VisualId == oldVisualId);

            // make sure the oldData exists
            // if it doesn't, it means that the visual data was never inserted in the first place or has been removed
            if (!oldData.IsNullOrEmpty())
            {
                // check to see if the new visual data is already in the dictionary
                ShapeVisualData preExistingData = LayerFusedMeshes.Keys.FirstOrDefault
                                    (x => x.VisualId == newData.VisualId);;

                // if there is a preExistingData, combine it with the old data
                if (!preExistingData.IsNullOrEmpty())
                {
                    // if there is a preExistingData, combine the fused meshes
                    FusedMesh preExistingMesh = LayerFusedMeshes[preExistingData];
                    FusedMesh changedMesh = LayerFusedMeshes[oldData];

                    // remove the old fused mesh
                    LayerFusedMeshes.Remove(oldData);

                    // we do this to make sure that the mesh with the least vertices is the one that is combined with the other for performance reasons
                    if (preExistingMesh.VertexCount > changedMesh.VertexCount)
                    {
                        preExistingMesh.CombineFusedMesh(changedMesh);
                    }
                    else
                    {
                        changedMesh.CombineFusedMesh(preExistingMesh);
                        preExistingMesh = changedMesh;
                    }

                    LayerFusedMeshes[preExistingData] = preExistingMesh;
                }
                else
                {
                    // if there is no preExistingData, it means that the visual data is still unique, so we can just update the hash
                    FusedMesh tempMesh = LayerFusedMeshes[oldData];
                    LayerFusedMeshes.Remove(oldData);
                    LayerFusedMeshes.Add(newData, tempMesh);
                }
                // if there is no preExistingData, it means that the visual data is still unique, so we can just update the hash 

#if UNITY_EDITOR
                // if we are in editor, the event was most likely raised during a serialization process, and we can't update the mesh during serialization. So we wait until the serialization process is done, then update the mesh. 
                if (UpdateOnVisualChange)
                {
                    EditorApplication.delayCall += () =>
                    {
                        UpdateMesh();
                    };
                }
#else
                UpdateMesh();
#endif
            }
            else
            {
                foreach(VisualProperties vp in VisualProps)
                {
                    if(vp.VisualId == oldVisualId)
                    {
                        RemoveEvent(vp);
                    }
                }
            }
            
        }
        public void CombineFusedMeshes()
        {
            // Performance Test
            // for 50 x 50: 0 - 0.0001 seconds
            // for 100 x 100: 0 - 0.0001 seconds

            List<FusedMesh> allMeshes = LayerFusedMeshes.Values.ToList();

            allMeshes.Add(coloredFusedMesh);

            LayerMesh = FusedMesh.CombineToSubmesh(allMeshes);
            // create new mats for each sub mesh, assign list to renderer
            SetMaterials();
        }
        public void UpdateMesh()
        {
            CombineFusedMeshes();

            if (LayerMesh != null)
            {
                meshFilter.sharedMesh = LayerMesh;
            }
        }
        public void Clear()
        {
            if (meshFilter != null)
            {
                meshFilter.mesh = null;
            }

            if (meshRenderer != null)
            {
                meshRenderer.materials = new Material[0];
            }

            foreach (FusedMesh fused in LayerFusedMeshes.Values)
            {
                fused.ClearFusedMesh();
            }

            LayerFusedMeshes?.Clear();
            SharedMaterials?.Clear();
            PropertyBlocks?.Clear();
        }
        private void SetMaterials()
        {
            SharedMaterials.Clear();
            PropertyBlocks.Clear();

            foreach (ShapeVisualData data in LayerFusedMeshes.Keys)
            {
                SharedMaterials.Add(data.SharedMaterial);

                if (data.PropertyBlock == null)
                {
                    PropertyBlocks.Add(new MaterialPropertyBlock());
                }
                else
                {
                    PropertyBlocks.Add(data.PropertyBlock);
                }
            }

            meshRenderer.sharedMaterials = SharedMaterials.ToArray();

            for (int i = 0; i < SharedMaterials.Count; i++)
            {
                if (PropertyBlocks[i].isEmpty)
                {
                    continue;
                }

                meshRenderer.SetPropertyBlock(PropertyBlocks[i], i);
            }
        }
        public SerializedLayer SerializeLayer(MapVisualContainer container)
        {
            return new SerializedLayer(this, container);
        }
        public void Deserialize(SerializedLayer sLayer,
                        MapVisualContainer container,
                        List<VisualProperties> visualProps)
        {
            layerId = sLayer.layerId;
            
            GridShape shape = container.GetGridShape(sLayer.shapeId);
            Initialize(shape);
            
            for (int i = 0; i < sLayer.gridPositions.Count; i++)
            {
                Vector2Int pos = sLayer.gridPositions[i];

                int id = sLayer.gridIds[i];
                VisualProperties data 
                            = visualProps.First(x => x.VisualId == id);

                InsertVisualData(pos, data);
            }

            UpdateMesh();
        }
        
        [Serializable]
        public struct SerializedLayer
        {
            [SerializeField]
            public string shapeId;
            public string layerId;
            // the question now is should each layer stores its visual properties
            // or should the visual properties be stored in a gridmanager class
            public List<Vector2Int> gridPositions;
            public List<int> gridIds;

            [SerializeReference]
            public List<int> visualIds;

            public SerializedLayer(LayeredMesh layer, MapVisualContainer container)
            {
                shapeId = layer.LayerGridShape.UniqueShapeId;
                layerId = layer.layerId;

                gridPositions = layer.GridVisualIds.Keys.ToList();
                gridIds = layer.GridVisualIds.Values.ToList();

                visualIds = new List<int>();

                foreach (VisualProperties prop in layer.VisualProps)
                {
                    visualIds.Add(prop.VisualId);
                }
            }
        }
        void SeriliazeDictAfter()
        {
#if UNITY_EDITOR

            EditorApplication.delayCall += () =>
            {
                // dictionaries cannot be serialize so we have to do this
                LayerFusedMeshes.Clear();

                for (int i = 0; i < visualDataList.Count; i++)
                {
                    LayerFusedMeshes.Add(visualDataList[i], fusedMeshesList[i]);
                    //visualDataList[i].VisualIdChange += VisualIdChanged;
                }

                UpdateMesh();
            };
#endif
        }

#if UNITY_EDITOR
        
        [SerializeField] private bool UpdateOnVisualChange = true;

        List<ShapeVisualData> visualDataList = new List<ShapeVisualData>();
        List<SerializedFusedMesh> fusedMeshesList = new List<SerializedFusedMesh>();
#endif
    }
}
