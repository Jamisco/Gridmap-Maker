using System;
using UnityEngine;
using static GridMapMaker.ShapeVisualData;

namespace GridMapMaker
{
    /// <summary>
    /// A basic visual Data class with Color and texture.
    /// This should be used with the provided Default shader.
    /// </summary>
    [Serializable]
    public class BasicVisual : ShapeVisualData
    {
        [SerializeField]
        public Texture2D mainTexture;

        [SerializeField]
        private Color mainColor = Color.white;
        public BasicVisual(Material mat, Texture2D texture, Color color)
        {
            sharedMaterial = mat;
            mainTexture = texture;
            mainColor = color;

            VisualColor = mainColor;

            propertyBlock = null;
        }

        protected override void SetMaterialPropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.Clear();

            if (mainTexture != null)
            {
                propertyBlock.SetTexture(_mainTex, mainTexture);
            }

            propertyBlock.SetColor(_mainColor, mainColor);
        }

        public override int CalculateVisualHash()
        {
            return HashCode.Combine(sharedMaterial, mainTexture, mainColor);
        }
    }
}
