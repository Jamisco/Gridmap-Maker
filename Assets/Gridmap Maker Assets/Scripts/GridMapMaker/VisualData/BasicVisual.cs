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
        public BasicVisual(Shader shader, Texture2D texture, Color color)
        {
            base.shader = shader;
            base.material = null;
            mainTexture = texture;
            mainColor = color;

            VisualHash = GetVisualHash();

            PropertyBlock = new MaterialPropertyBlock();
        }

        public BasicVisual(Material material, Shader shader, Texture2D texture, Color color)
        {
            base.material = material;
            base.shader = shader;
            mainTexture = texture;
            mainColor = color;

            VisualHash = GetVisualHash();

            PropertyBlock = new MaterialPropertyBlock();
        }

        public override void SetMaterialPropertyBlock()
        {
            if (PropertyBlock == null)
            {
                PropertyBlock = new MaterialPropertyBlock();
            }

            PropertyBlock.Clear();

            if (mainTexture != null)
            {
                PropertyBlock.SetTexture(mainTexProperty, mainTexture);
            }

            PropertyBlock.SetColor(mainColorProperty, mainColor);
        }
        public override ShapeVisualData DeepCopy()
        {
            BasicVisual clone = new BasicVisual(shader, mainTexture, mainColor);
            return clone;
        }
        public override int GetVisualHash()
        {
            int mt = mainTexture != null ? mainTexture.GetHashCode() : 0;
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + mt;
                hash = hash * 23 + mainColor.GetHashCode();
                return hash;
            }
        }

        protected override void OnVisualDataChanged(ShapeVisualData sender)
        {
            base.OnVisualDataChanged(sender);
        }
    }
}
