
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

        private Dictionary<ShapeVisualData, FusedMesh> LayerFusedMeshes
                                    = new Dictionary<ShapeVisualData, FusedMesh>();
        
        private Dictionary<Vector2Int, int> GridVisualIds
                              = new Dictionary<Vector2Int, int>();

        public List<VisualProperties> VisualDatas = new List<VisualProperties>();
         
        private Mesh LayerMesh { get; set; }
        private Mesh shapeMesh;

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        private List<Material> SharedMaterials { get; set; } = new List<Material>();
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
        public void Initialize(SerializedLayer sLayer, List<VisualProperties> visuals)
        {
            Initialize(sLayer.shape);

            for (int i = 0; i < sLayer.gridPositions.Count; i++)
            {
                Vector2Int pos = sLayer.gridPositions[i];

                int id = sLayer.gridIds[i];
                VisualProperties data = visuals.First(x => x.VisualId == id);
                
                InsertVisualData(data, pos);
            }

            UpdateMesh();
        }
        public void Initialize(GridShape gridShape)
        {
            Clear();
            LayerGridShape = gridShape;
            shapeMesh = gridShape.GetBaseShape();
            SetMeshInfo();
        }
         
        void SetEvent(VisualProperties visualProp)
        {
            visualProp.VisualIdChange += VisualIdChanged;
        }

        public void InsertVisualData<T>(T visualProp,
                                    Vector2Int gridPosition) where T : VisualProperties
        {
            int hash = gridPosition.GetHashCode_Unique();
            Vector3 offset = LayerGridShape.GetTesselatedPosition(gridPosition);
            ShapeVisualData visualData = visualProp.GetShapeVisualData();

            if (LayerFusedMeshes.ContainsKey(visualData))
            {
                LayerFusedMeshes[visualData].InsertMesh(shapeMesh, hash, offset);
                GridVisualIds[gridPosition] = visualProp.GetVisualId();
            }
            else
            {
                FusedMesh fusedMesh = new FusedMesh();
                fusedMesh.InsertMesh(shapeMesh, hash, offset);
                LayerFusedMeshes.Add(visualData, fusedMesh);
                GridVisualIds.Add(gridPosition, visualProp.GetVisualId());

                SetEvent(visualProp);
                VisualDatas.Add(visualProp);
            }
        }

        public void InsertVisualData(VisualProperties visualProp,
                                            Vector2Int gridPosition)
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
                fusedMesh.InsertMesh(shapeMesh, hash, offset);
                LayerFusedMeshes.Add(visualData, fusedMesh);

                SetEvent(visualProp);
                VisualDatas.Add(visualProp);
            }
        }

        public void VisualIdChanged(int oldVisualId, ShapeVisualData newData)
        {
            ShapeVisualData oldData = LayerFusedMeshes.Keys.FirstOrDefault
                                    (x => x.VisualHash == oldVisualId);

            // make sure the oldData exists
            // if it doesn't, it means that the visual data was never inserted in the first place 
            if (!oldData.IsNullOrEmpty())
            {
                // check to see if the new visual data is already in the dictionary
                ShapeVisualData preExistingData = LayerFusedMeshes.Keys.FirstOrDefault
                                    (x => x.VisualHash == newData.VisualHash);;

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
        }
        public void CombineFusedMeshes()
        {
            // Performance Test
            // for 50 x 50: 0 - 0.0001 seconds
            // for 100 x 100: 0 - 0.0001 seconds

            LayerMesh = FusedMesh.CombineToSubmesh(LayerFusedMeshes.Values.ToList());
            // create new mats for each sub mesh, assign list to renderer
            SetMaterials();
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

        [Serializable]
        public struct SerializedLayer
        {
            // the question now is should each layer stores its visual properties
            // or should the visual properties be stored in a gridmanager class
            public List<Vector2Int> gridPositions;
            public List<int> gridIds;

            public GridShape shape;

            public SerializedLayer(GridShape shape,
                                Dictionary<Vector2Int, int> gridVisualIds)
            {
                this.shape = shape;
                gridPositions = gridVisualIds.Keys.ToList();
                gridIds = gridVisualIds.Values.ToList();
            }
        }
        public SerializedLayer SerializeLayer()
        {
            return new SerializedLayer(LayerGridShape, GridVisualIds);

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
