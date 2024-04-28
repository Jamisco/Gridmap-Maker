
using Assets.Gridmap_Assets.Scripts.GridMapMaker;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Gridmap_Assets.Scripts.Miscellaneous;
using Assets.Scripts.GridMapMaker;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using static Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData.ShapeVisualData;
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

        [SerializeField]
        private bool useVisualEquality;

        public bool UseVisualEquality
        {
            get => useVisualEquality;
            set
            {
                useVisualEquality = value;

                visualDataComparer.UseVisualEquality = value;
            }
        }
        
        public GridShape LayerGridShape { get; private set; }

        VisualDataComparer visualDataComparer = new VisualDataComparer();

        private ShapeVisualData defaultVisualProp;

        /// <summary>
        // The grouping of visual datas that are used to make a mesh.
        // So cells that have "equal" visualData will be group here and drawn as one
        /// </summary>
        private Dictionary<ShapeVisualData, FusedMesh> LayerFusedMeshes;
        /// <summary>
        /// The original visualData used for each cell. We store this so we can redraw the mesh if need be
        /// </summary>
        private Dictionary<Vector2Int, ShapeVisualData> CellVisualDatas
                          = new Dictionary<Vector2Int, ShapeVisualData>();
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

        [SerializeField]
        private bool UpdateOnVisualChange;

        private void Reset()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
        }
        private void OnValidate()
        {

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
        public void Initialize(string layerId, GridShape gridShape, ShapeVisualData defaultVisual, bool useVisualEquality = false)
        {
            Initialize(gridShape);
            
            this.layerId = layerId;
            this.defaultVisualProp = defaultVisual;
            this.useVisualEquality = useVisualEquality;
            visualDataComparer.UseVisualEquality = useVisualEquality;

            LayerFusedMeshes = new Dictionary<ShapeVisualData, FusedMesh>(visualDataComparer);

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
        void SetEvent(ShapeVisualData visualProp)
        {
            visualProp.VisualIdChange += VisualIdChanged;
        }
        void RemoveEvent(ShapeVisualData visualProp)
        {
            visualProp.VisualIdChange -= VisualIdChanged;
        }
        public void InsertVisualData(Vector2Int gridPosition,
                                     ShapeVisualData visualProp)
        {
            // this function takes up 70% of the time it takes to generate a grid

            int hash = gridPosition.GetHashCode_Unique();
            Vector3 offset = LayerGridShape.GetTesselatedPosition(gridPosition);

            // every time we insert a visual vData, we must check if the grid position already has a visual vData, if it does, we must remove it because we might have to assign a new visual vData to it..thus moving said grid mesh to a new fused mesh

            if(redrawMode == false)
            {
                DeleteShape(gridPosition);

                CellVisualDatas.Add(gridPosition, visualProp);
            }

            FusedMesh existing = null;

            LayerFusedMeshes.TryGetValue(visualProp, out existing);

            if (existing != null)
            {
                // inserting the mesh takes 60% of the time it takes to generate a grid
                LayerFusedMeshes[visualProp].InsertMesh(shapeMesh, hash, offset);
            }
            else
            {
                FusedMesh fusedMesh = new FusedMesh();

                // if we are adding a new visualData, we must make sure that the grid we are adding it to does not already have a visualData

                fusedMesh.InsertMesh(shapeMesh, hash, offset);
                LayerFusedMeshes.Add(visualProp, fusedMesh);

                SetEvent(visualProp);
            }

        }
        public void RemoveVisualData(Vector2Int gridPosition)
        {
            // removing a visual vData is thesame as inserting a default visual vData

            InsertVisualData(gridPosition, defaultVisualProp);
        }
        public void DeleteShape(Vector2Int gridPosition)
        {
            int hash = gridPosition.GetHashCode_Unique();
            // We will straight up delete the mesh at the grid position

            ShapeVisualData existing = null;

            CellVisualDatas.TryGetValue(gridPosition, out existing);

            if (existing != null)
            {
                LayerFusedMeshes[existing].RemoveMesh(hash);

                if (LayerFusedMeshes[existing].IsEmpty)
                {
                    LayerFusedMeshes.Remove(existing);
                    RemoveEvent(existing);
                }
            }
        }
        public bool HasVisualData(Vector2Int gridPosition)
        {
            return CellVisualDatas.ContainsKey(gridPosition);
        }
        public ShapeVisualData GetVisualProperties(Vector2Int gridPosition)
        {
            ShapeVisualData existing = null;

            CellVisualDatas.TryGetValue(gridPosition, out existing);

            return existing;
        }
        public void VisualIdChanged(ShapeVisualData sender)
        {
            ShapeVisualData changedProp = LayerFusedMeshes.Keys.FirstOrDefault
                                    (x => x == sender);

            // make sure the changedProp exists
            // if it doesn't, it means that the visual vData was never inserted in the first place or has been removed
            if (changedProp != null)
            {
                // When a visual prop has changed, we need to see if there is another visual prop that looks like it

                ShapeVisualData identicalProp = LayerFusedMeshes.Keys.FirstOrDefault
                            (x => (x != changedProp && x.Equals(changedProp)));

                // if there is a identicalProp, combine it with the old vData
                if (identicalProp != null)
                {
                    // if there is a identicalProp, combine the fused meshes
                    FusedMesh preExistingMesh = LayerFusedMeshes[identicalProp];
                    FusedMesh changedMesh = LayerFusedMeshes[changedProp];

                    // remove the old fused mesh
                    LayerFusedMeshes.Remove(changedProp);

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

                    LayerFusedMeshes[identicalProp] = preExistingMesh;
                }
                else
                {
                    // if there is no identicalProp, it means that the visual vData is still unique, nothing more is required
                }

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
                // this visualData does not exist in this layer, remove it
                // this should never occur so as long as we are removing the event whenever we delete a position
            }

        }
        public void CombineFusedMeshes()
        {
            // Performance Test
            // for 50 x 50: 0 - 0.0001 seconds
            // for 100 x 100: 0 - 0.0001 seconds

            List<FusedMesh> allMeshes = LayerFusedMeshes.Values.ToList();

            LayerMesh = FusedMesh.CombineToSubmesh(allMeshes);
            // create new mats for each sub mesh, assign list to renderer
            SetMaterials();
        }
        public void UpdateMesh()
        {
            //foreach (var vData in LayerFusedMeshes.Keys)
            //{
            //    LayerFusedMeshes[vData].UpdateMesh();
            //}

            // convert above to for

            for (int i = 0; i < LayerFusedMeshes.Count; i++)
            {
                LayerFusedMeshes.ElementAt(i).Value.UpdateMesh();
            }

            CombineFusedMeshes();

            if (LayerMesh != null)
            {
                meshFilter.sharedMesh = LayerMesh;
            }
        }

        /// <summary>
        /// When redrawing the mesh, we want to skip various checks to expediate the process
        /// </summary>
        bool redrawMode = false;
        /// <summary>
        /// Clears the mesh and redraws all visual data into the layer. This is useful when the equality comparison has been changed. For example, if you want to render the map based on visual equality
        /// </summary>
        public void RedrawLayer()
        {
            // because we are reinserting all the data back, we have to cache the visual data and grid visual ids and then clear them, then as we call insertVisualData, the method will reinsert the data back
            
            LayerFusedMeshes.Clear();

            redrawMode = true;
            foreach (Vector2Int gridPosition in CellVisualDatas.Keys)
            {
                ShapeVisualData visual = CellVisualDatas[gridPosition];

                RemoveEvent(visual);
                
                InsertVisualData(gridPosition, visual);
            }
            redrawMode = false;
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

            if (LayerMesh != null)
            {
                foreach (FusedMesh fused in LayerFusedMeshes.Values)
                {
                    fused.ClearFusedMesh();
                }

                LayerFusedMeshes.Clear();
            }

            LayerMesh = null;
            defaultVisualProp = null;
            LayerGridShape = null;

            useVisualEquality = false;

            CellVisualDatas.Clear();

            SharedMaterials.Clear();
            PropertyBlocks.Clear();
        }
        private void SetMaterials()
        {
            SharedMaterials.Clear();
            PropertyBlocks.Clear();

            foreach (ShapeVisualData vData in LayerFusedMeshes.Keys)
            {
                ShapeRenderData rData = vData.GetShapeRenderData();

                SharedMaterials.Add(rData.SharedMaterial);

                if (rData.PropertyBlock == null)
                {
                    PropertyBlocks.Add(new MaterialPropertyBlock());
                }
                else
                {
                    PropertyBlocks.Add(rData.PropertyBlock);
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
                        List<ShapeVisualData> visualProps)
        {
            layerId = sLayer.layerId;

            GridShape shape = container.GetGridShape(sLayer.shapeId);
            Initialize(shape);

            for (int i = 0; i < sLayer.gridPositions.Count; i++)
            {
                Vector2Int pos = sLayer.gridPositions[i];

                Guid id = sLayer.gridVisualIds[i];
                ShapeVisualData data
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
            public List<Guid> gridVisualIds;

            public SerializedLayer(LayeredMesh layer, MapVisualContainer container)
            {
                shapeId = layer.LayerGridShape.UniqueShapeName;
                layerId = layer.layerId;

                gridPositions = layer.CellVisualDatas.Keys.ToList();
                gridVisualIds = new List<Guid>();

                foreach (Vector2Int cell in gridPositions)
                {
                    gridVisualIds.Add(layer.CellVisualDatas[cell].VisualId);
                }
            }
        }
        

        /// <summary>
        /// Just used to quick create layers. Nothing majors
        /// </summary>
        public struct LayerCreator
        {
            public string layerId;
            public GridShape shape;
            public ShapeVisualData defaultVData;
            public bool setBaselayer;
            public bool useVisualEquality;

            public LayerCreator(string layerId, GridShape shape, ShapeVisualData defaultVData, bool setBaselayer = false, bool useVisualEquality = false)
            {
                this.layerId = layerId;
                this.shape = shape;
                this.defaultVData = defaultVData;
                this.setBaselayer = setBaselayer;
                this.useVisualEquality = useVisualEquality;
            }
        }
    }
}
