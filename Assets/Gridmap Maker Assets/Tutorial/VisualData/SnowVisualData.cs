using System;
using UnityEngine;
using GridMapMaker;

namespace GridMapMaker.Tutorial
{
    [Serializable]
    public class SnowVisualData : ShapeVisualData
    {
        [SerializeField]
        float intensity;

        public float Intensity { get => intensity; set => intensity = value;}
        public SnowVisualData(Material sharedMat, float intensity)
        {
            this.sharedMaterial = sharedMat;
            this.intensity = intensity;
            SetMaterialPropertyBlock();
        }

        protected override void SetMaterialPropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.Clear();

            propertyBlock.SetFloat("_Intensity", intensity);
        }

        public override int CalculateVisualHash()
        {
            return intensity.GetHashCode();
        }
    }
}
