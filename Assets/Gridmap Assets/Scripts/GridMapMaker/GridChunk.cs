using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using Assets.Gridmap_Assets.Scripts.Mapmaker;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Assets.Gridmap_Assets.Scripts.Mapmaker.LayeredMesh;

namespace Assets.Scripts.GridMapMaker
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class GridChunk : MonoBehaviour
    {
        // A grid chunk is a collection of layered meshes.

        Dictionary<SortingLayer, LayeredMesh> layeredMesh = new Dictionary<SortingLayer, LayeredMesh>();

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        public void Initialize()
        {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
        }

        public void Clear()
        {
            meshFilter.mesh = null;
        }

    }
}
