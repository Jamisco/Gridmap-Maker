using Assets.Scripts.GridMapMaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.Mapmaker
{
    public class LayeredMesh
    {
        public GameObject LayerGameObject { get; set; }
        public FusedMesh LayerFusedMesh { get; set; }
        public int OrderInLayer
        {
            get
            {
                return meshRenderer.sortingOrder;
            }
            set
            {
                meshRenderer.sortingOrder = value;
            }
        }

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;


        HexChunk layerChunk;
        public LayeredMesh(HexChunk chunk, string layerName, int layer, Material material, int order = 0)
        {
            LayerGameObject = new GameObject(layerName);
            LayerGameObject.transform.SetParent(chunk.transform);
            LayerGameObject.transform.position = chunk.transform.position;

            layerChunk = chunk;

            meshRenderer = LayerGameObject.AddComponent<MeshRenderer>();
            meshFilter = LayerGameObject.AddComponent<MeshFilter>();

            LayerFusedMesh = new FusedMesh();

            meshRenderer.material = material;

            meshRenderer.sortingLayerID = layer;
            meshRenderer.sortingOrder = order;
        }

        public void UpdateMesh()
        {
            if (LayerGameObject != null)
            {
                if (LayerFusedMesh != null)
                {
                    meshFilter.mesh = LayerFusedMesh.Mesh;
                }
            }
        }
    }
}
