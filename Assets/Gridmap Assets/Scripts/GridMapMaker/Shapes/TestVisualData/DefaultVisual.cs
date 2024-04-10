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
    public class DefaultVisual : VisualProperties
    {
        private Color defaultColor;

        [SerializeField]
        [HideInInspector]
        private SerializedDefaultVisual serializedData;
        protected override ISerializedVisual SerializedData => serializedData;

        public DefaultVisual(Material material, Color color = default)
        {
            sharedMaterial = material;
            propertyBlock = new MaterialPropertyBlock();

            defaultColor = color;

            visualId = GenerateVisualId();
        }        

        public override int GenerateVisualId()
        {
            int id1 = defaultColor.ToString().GetHashCode();
            int id2 = sharedMaterial.GetInstanceID();

            return id1 ^ id2;
        }

        public override void SetMaterialProperties()
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

            visualId = serializedData.visualId;
        }

        [Serializable]
        public struct SerializedDefaultVisual : ISerializedVisual
        {
            public string sharedMaterial;
            public Color defaultColor;
            public int visualId;

            public SerializedDefaultVisual(DefaultVisual defaultVisual, MapVisualContainer container)
            {
                visualId = defaultVisual.visualId;
                defaultColor = defaultVisual.defaultColor;
                sharedMaterial = container
                                .GetGuid(defaultVisual.sharedMaterial)
                                .ToString();
            }
        }
    }
}
