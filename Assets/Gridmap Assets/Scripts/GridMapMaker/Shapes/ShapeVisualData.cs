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
    public struct ShapeVisualData : IEqualityComparer<ShapeVisualData>
    {
        [ShowOnlyField]
        private Material sharedMaterial;
        
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
        public int VisualHash { get => visualId; }

        public ShapeVisualData(Material material, 
                            MaterialPropertyBlock propertyBlock, int visualHash)
        {
            sharedMaterial = material;
            this.propertyBlock = propertyBlock;
            this.visualId = visualHash;
        }

        public bool Equals(ShapeVisualData x, ShapeVisualData y)
        {
            return x.visualId == y.visualId;
        }

        public int GetHashCode(ShapeVisualData obj)
        {
            return obj.visualId;   
        }

        public bool IsNullOrEmpty()
        {
            if (sharedMaterial == null || propertyBlock == null)
            {
                return true;
            }

            return false;
        }
    }
}
