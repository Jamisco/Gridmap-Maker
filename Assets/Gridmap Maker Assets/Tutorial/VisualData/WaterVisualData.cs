using GridMapMaker;
using System;
using UnityEngine;

namespace GridMapMaker.Tutorial
{
    [Serializable]
    public class WaterVisualData : ShapeVisualData
    {
        [SerializeField]
        public Color mainColor;

        [SerializeField]
        public Color secondaryColor;
        public WaterVisualData(Material material)
        {
            this.sharedMaterial = material;

            try
            {
                mainColor = material.GetColor("_MainColor");
                secondaryColor = material.GetColor("_SecondColor");
            }
            catch (Exception)
            {
                Debug.LogError("Material does not have the required properties for WaterVisualData. Verify that the material is correct.");
            }
        }
        protected override void SetMaterialPropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.Clear();

            // set mainColor and secondaryColor
            try
            {
                propertyBlock.SetColor("_MainColor", mainColor);
                propertyBlock.SetColor("_SecondColor", secondaryColor);
            }
            catch (Exception)
            {
                Debug.LogError("Material does not have the required properties for WaterVisualData. Verify that the material is correct.");
            }
        }

        public override int CalculateVisualHash()
        {
            return HashCode.Combine(sharedMaterial, mainColor, secondaryColor);
        }
    }
}
