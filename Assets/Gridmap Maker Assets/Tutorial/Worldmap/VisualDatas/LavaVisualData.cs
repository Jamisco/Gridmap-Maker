using System;
using UnityEngine;

namespace GridMapMaker.Tutorial
{
    [Serializable]
    public class LavaVisualData : ShapeVisualData
    {
        public LavaVisualData(Shader shader)
        {
            this.shader = shader;

            this.VisualHash = GetVisualHash();
        }
        public override void SetMaterialPropertyBlock()
        {
            if (PropertyBlock == null)
            {
                PropertyBlock = new MaterialPropertyBlock();
            }

            PropertyBlock.Clear();
        }

        public override int GetVisualHash()
        {
            return ("Lava").GetHashCode();
        }

        public override ShapeVisualData DeepCopy()
        {
            return new LavaVisualData(shader);
        }
    }
}
