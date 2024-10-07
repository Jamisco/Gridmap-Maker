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
    public class SnowVisualData : ShapeVisualData
    {
        [SerializeField]
        float intensity;

        public float Intensity { get => intensity; set => intensity = value;}
        public SnowVisualData(Shader shader, float intensity)
        {
            this.shader = shader;
            this.intensity = intensity;

            VisualHash = GetVisualHash();
        }

        public override ShapeVisualData DeepCopy()
        {
            return new SnowVisualData(shader, intensity);
        }

        public override void SetMaterialPropertyBlock()
        {
            if (PropertyBlock == null)
            {
                PropertyBlock = new MaterialPropertyBlock();
            }

            PropertyBlock.Clear();

            PropertyBlock.SetFloat("_Intensity", intensity);
        }

        public override int GetVisualHash()
        {
            return intensity.GetHashCode();
        }
    }
}
