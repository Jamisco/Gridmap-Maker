using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    /// This simple contains a material and a property block used to render the shape
    /// Contains a visual hash to identify other structs with the same visual data (material and property block)
    public struct ShapeVisualData : IEquatable<ShapeVisualData>
    {
        [ShowOnlyField]
        private Material sharedMaterial;

        [SerializeField]
        [ShowOnlyField]
        private int visualId;      
        
        private MaterialPropertyBlock propertyBlock;   
        
        public Material SharedMaterial
        {
            get
            {
                return sharedMaterial;
            }
        }
        public MaterialPropertyBlock PropertyBlock { get => propertyBlock; }

        public int VisualId { get => visualId; }

        public ShapeVisualData(Material material, 
                               MaterialPropertyBlock propertyBlock, int visualId)
        {
            sharedMaterial = material;
            this.propertyBlock = propertyBlock;
            this.visualId = visualId;
        }

        public bool IsNullOrEmpty()
        {
            if (sharedMaterial == null || propertyBlock == null)
            {
                return true;
            }

            return false;
        }
        public bool Equals(ShapeVisualData other)
        {
            return visualId == other.visualId;
        }

        public override int GetHashCode()
        {
            return visualId;
        }
    }
}
