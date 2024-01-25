using Assets.Gridmap_Assets.Scripts.GridMapMaker;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using Assets.Scripts.GridMapMaker;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.Mapmaker
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class LayeredMesh : MonoBehaviour
    {
        // A layered mesh is a collection of fused meshes that are then combined together to make one mesh.
        // Each fused mesh is a unique visually, meaning it may have a different texture, color, etc. However each fused mesh has thesame shape

        private Dictionary<ShapeVisualData, FusedMesh> LayerFusedMeshes
                                      = new Dictionary<ShapeVisualData, FusedMesh>();
        public GridShape LayerGridShape { get; private set; }

        private Mesh LayerMesh { get; set; }
        private Mesh shapeMesh;

        [SerializeField] private SortingLayer sortingLayer;
        public SortingLayer SortingLayer
        {
            get => sortingLayer;
            set
            {
                sortingLayer = value;
                SetLayerInfo();
            }
        }

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        private List<Material> SharedMaterials { get; set; } = new List<Material>();
        public List<MaterialPropertyBlock> PropertyBlocks { get; private set; } 
                                                     = new List<MaterialPropertyBlock>();

        private void Reset()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
        }

        private void SetLayerInfo()
        {
            //meshRenderer.sortingLayerID = sortingLayer.id;
            meshRenderer.sortingOrder = 1;
        }

        private void OnValidate()
        {
            if (meshRenderer == null || meshFilter == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshFilter = GetComponent<MeshFilter>();
            }
        }

        public void Initialize(GridShape gridShape)
        {
            Clear();
            LayerGridShape = gridShape;
            shapeMesh = gridShape.GetBaseShape();
            SortingLayer = sortingLayer;
        }

        public void InsertVisualData(ShapeVisualData visualData, Vector2Int gridPosition)
        {
            int hash = gridPosition.GetHashCode_Unique();
            Vector3 offset = LayerGridShape.GetTesselatedPosition(gridPosition);

            if (LayerFusedMeshes.ContainsKey(visualData))
            {
                LayerFusedMeshes[visualData].InsertMesh(shapeMesh, hash, offset);
            }
            else
            {
                FusedMesh fusedMesh = new FusedMesh();
                fusedMesh.InsertMesh(shapeMesh, hash, offset);
                LayerFusedMeshes.Add(visualData, fusedMesh);
            }
        }

        public void CombineFusedMeshes()
        {
            LayerMesh = new Mesh();
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

            foreach (FusedMesh fused in LayerFusedMeshes.Values)
            {
                fused.ClearFusedMesh();
            }
            LayerFusedMeshes?.Clear();
            SharedMaterials?.Clear();
            PropertyBlocks?.Clear();
        }
    }
}
