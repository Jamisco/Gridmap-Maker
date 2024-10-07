using System;
using UnityEngine;

namespace GridMapMaker
{
    /// <summary>
    /// The ColorVisualData class is a simple script that inherits from the ShapeVisualData class. It is used to store color visual data for the grid map maker. If you are want to display only a color onto a shape, this class is the one to use.
    /// </summary>
    public class ColorVisualData : ShapeVisualData
    {
        public ColorVisualData(Shader shader, Color mainColor)
        {
            this.mainColor = mainColor;
            this.shader = shader;

            VisualHash = GetVisualHash();

            DataRenderMode = RenderMode.MeshColor;
        }
        public override ShapeVisualData DeepCopy()
        {
            ColorVisualData clone = new ColorVisualData(shader, mainColor);
            return clone;
        }
        public override void SetMaterialPropertyBlock()
        {
            PropertyBlock = new MaterialPropertyBlock();
        }
    }
}