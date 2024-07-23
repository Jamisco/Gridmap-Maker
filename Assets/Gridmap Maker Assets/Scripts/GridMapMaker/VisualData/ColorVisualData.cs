using System;
using UnityEngine;

namespace GridMapMaker
{
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