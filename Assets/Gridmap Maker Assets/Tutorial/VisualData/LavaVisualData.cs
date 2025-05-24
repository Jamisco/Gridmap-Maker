using System;
using UnityEngine;
using GridMapMaker;

namespace GridMapMaker.Tutorial
{
    [Serializable]
    public class LavaVisualData : ShapeVisualData
    {
        public LavaVisualData(Material material)
        {
            sharedMaterial = material;
        }
        protected override void SetMaterialPropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.Clear();
        }

        public override int CalculateVisualHash()
        {
            return ("Lava").GetHashCode();
        }
    }
}
