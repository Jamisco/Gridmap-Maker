using System;
using UnityEngine;
using GridMapMaker;

namespace GridMapMaker.Tutorial
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

        public LandVisualData(Material mat, float temp, float rain)
        {
            temperature = temp;
            this.rain = rain;

            sharedMaterial = mat;

            SetMaterialPropertyBlock();
        }
        protected override void SetMaterialPropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.Clear();

            propertyBlock.SetFloat("_Temperature", Temperature);
            propertyBlock.SetFloat("_Rain", Rain);
        }
        public override int CalculateVisualHash()
        {
            return HashCode.Combine(SharedMaterial, Temperature, Rain);
        }

    }
}
