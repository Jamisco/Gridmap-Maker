using GridMapMaker;
using System;
using UnityEngine;

namespace Assets.Worldmap.VisualDatas
{
    [Serializable]
    public class LandVisualData : ShapeVisualData
    {
        [SerializeField]
        private float temperature;

        [SerializeField]
        private float rain;
        public float Temperature { get => temperature; set => temperature = value; }
        public float Rain { get => rain; set => rain = value; }

        public LandVisualData(float temp, float rain, Shader shader)
        {
            temperature = temp;
            this.rain = rain;
            this.shader = shader;
            SetMaterialPropertyBlock();
            VisualHash = GetVisualHash();
        }
        public override void SetMaterialPropertyBlock()
        {
            if (PropertyBlock == null)
            {
                PropertyBlock = new MaterialPropertyBlock();
            }

            PropertyBlock.Clear();

            PropertyBlock.SetFloat("_Temperature", Temperature);
            PropertyBlock.SetFloat("_Rain", Rain);
        }
        public override int GetVisualHash()
        {
            return HashCode.Combine(Temperature, Rain);
        }
        public override ShapeVisualData DeepCopy()
        {
            return new LandVisualData(temperature, rain, shader);
        }

    }
}
