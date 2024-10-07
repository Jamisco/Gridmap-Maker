using GridMapMaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Worldmap.VisualDatas
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
