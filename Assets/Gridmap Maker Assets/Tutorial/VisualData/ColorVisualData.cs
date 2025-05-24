using GridMapMaker;
using System;
using UnityEngine;

namespace GridMapMaker.Tutorial
{
    [Serializable]
    /// <summary>
    /// The ColorVisualData class is a simple script that inherits from the ShapeVisualData class. It is used to store color visual data for the grid map maker. If you are want to display only a color onto a shape, this class is the one to use. if you want to use alpha property of the color, make sure your shader has Blend SrcAlpha OneMinusSrcAlpha
    /// </summary>
    public class ColorVisualData : ShapeVisualData
    {    
        public ColorVisualData(Material mat, Color mainColor)
        {
            sharedMaterial = mat;
            VisualColor = mainColor;
            DataRenderMode = RenderMode.MeshColor;

        }
        protected override void SetMaterialPropertyBlock()
        {
            propertyBlock = new MaterialPropertyBlock();
            propertyBlock.Clear();

            // ColorVisualData does not use the material's color property
        }

        public override int CalculateVisualHash()
        {
            return HashCode.Combine(sharedMaterial, VisualColor);
        }
    }
}