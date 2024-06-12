using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Rendering;
using UnityEngine;
using static Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.BasicVisual;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData
{
    public class DefaultVisual : ShapeVisualData
    {
        private Color defaultColor;

        [SerializeField]
        [HideInInspector]
        private SerializedDefaultVisual serializedData;
        protected override ISerializedVisual SerializedData => serializedData;

        public static Material spritesDefault = new Material(Shader.Find("Sprites/Default"));

        public DefaultVisual(Material material, Color color = default)
        {
            sharedMaterial = material;
            propertyBlock = new MaterialPropertyBlock();

            defaultColor = color;
        }        

        public override void SetMaterialPropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            propertyBlock.Clear();
            propertyBlock.SetColor("_Color", defaultColor);
        }

        public override void SetSerializeData(MapVisualContainer container)
        {
            serializedData = new SerializedDefaultVisual(this, container);
        }
        public override void DeserializeData(MapVisualContainer container)
        {
            Guid sm = Guid.Parse(serializedData.sharedMaterial);

            sharedMaterial = (Material)container.GetObject(sm);

            defaultColor = serializedData.defaultColor;
        }

        public override T DeepCopy<T>()
        {
            DefaultVisual copy = new DefaultVisual(sharedMaterial, defaultColor);
            return copy as T;
        }

        [Serializable]
        public struct SerializedDefaultVisual : ISerializedVisual
        {
            public string sharedMaterial;
            public Color defaultColor;
            
            
            public SerializedDefaultVisual(DefaultVisual defaultVisual, MapVisualContainer container)
            {
                defaultColor = defaultVisual.defaultColor;
                sharedMaterial = container
                                .GetGuid(defaultVisual.sharedMaterial)
                                .ToString();
            }
        }
    }
}
