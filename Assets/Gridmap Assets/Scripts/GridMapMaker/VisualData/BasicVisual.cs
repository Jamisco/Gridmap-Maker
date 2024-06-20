using System;
using UnityEngine;

namespace GridMapMaker
{
    [Serializable]
    public class BasicVisual : ShapeVisualData
    {    
        [SerializeField]
        [HideInInspector]
        private SerializedBasicVisual serializedData;

        [SerializeField]
        private string name;
        public BasicVisual(Shader shader, Texture2D texture, Color color)
        {
            base.shader = shader;
            mainTexture = texture;
            mainColor = color;

            propertyBlock = new MaterialPropertyBlock();
        }
        public MaterialPropertyBlock PropertyBlock => propertyBlock;
        protected override ISerializedVisual SerializedData => serializedData;
        public override void SetMaterialPropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.Clear();

            if (mainTexture != null)
            {
                propertyBlock.SetTexture(mainTexProperty, mainTexture);
            }

            propertyBlock.SetColor(mainColorProperty, mainColor);
        }
        public override void SetSerializeData(MapVisualContainer container)
        {
            serializedData = new SerializedBasicVisual(this, container);
        }
        protected override void DeserializeVisualData(MapVisualContainer container)
        {
            Guid mt = Guid.Parse(serializedData.mainTexture);

            mainTexture = (Texture2D)container.GetObject(mt);
            shader = container.GetShader(serializedData.shaderName);
            
            mainColor = serializedData.mainColor;
        }
        public override T DeepCopy<T>()
        {
            BasicVisual clone = new BasicVisual(shader, mainTexture, mainColor);
            return clone as T;
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

        [Serializable]
        public struct SerializedBasicVisual : ISerializedVisual
        {
            public string mainTexture;
            public string shaderName;
            public Color mainColor;
            public SerializedBasicVisual(BasicVisual basicVisual, MapVisualContainer container)
            {
                mainColor = basicVisual.mainColor;
                mainTexture = container.GetGuid(basicVisual.mainTexture)
                    .ToString();
                shaderName = basicVisual.shader.name;
            }
        }
    }
}
