using System;
using UnityEngine;

namespace GridMapMaker
{
    public class ColorVisualData : ShapeVisualData
    {
        [SerializeField]
        [HideInInspector]
        private SerializedColorVisualData serializedData;
        protected override ISerializedVisual SerializedData => serializedData;
        public ColorVisualData(Shader shader, Color mainColor)
        {
            this.mainColor = mainColor;
            this.shader = shader;
            DataRenderMode = RenderMode.MeshColor;
        }
        public override T DeepCopy<T>()
        {
            ColorVisualData clone = new ColorVisualData(shader, mainColor);
            return clone as T;
        }
        public override void SetMaterialPropertyBlock()
        {
            propertyBlock = new MaterialPropertyBlock();
        }
        public override void SetSerializeData(MapVisualContainer container)
        {
            serializedData = new SerializedColorVisualData(this);
        }
        protected override void DeserializeVisualData(MapVisualContainer container)
        {
            // by default this is set to material in the ShapeVisualData class, so we must reset it here
            DataRenderMode = RenderMode.MeshColor;
            mainColor = serializedData.mainColor;
            shader = container.GetShader(serializedData.shaderName);
        }

        [Serializable]
        public struct SerializedColorVisualData : ISerializedVisual
        {
            public Color mainColor;
            public string shaderName;
            public SerializedColorVisualData(ColorVisualData col)
            {
                this.mainColor = col.mainColor;
                shaderName = col.shader.name;
            }
        }
    }
}