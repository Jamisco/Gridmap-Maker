
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Text.Json;
using Unity.Mathematics;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    public class BasicVisual : ShapeVisualData
    {    
        [SerializeField]
        public Texture2D mainTexture;
        [SerializeField]
        public Color mainColor;

        [SerializeField]
        [HideInInspector]
        private SerializedBasicVisual serializedData;

        [SerializeField]
        private string name;
        public BasicVisual(Material material, Texture2D texture, Color color)
        {
            sharedMaterial = material;
            mainTexture = texture;
            mainColor = color;

            propertyBlock = new MaterialPropertyBlock();
        }
        public Material SharedMaterial => sharedMaterial;
        public MaterialPropertyBlock PropertyBlock => propertyBlock;
        protected override ISerializedVisual SerializedData => serializedData;
        public override void SetMaterialProperties()
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
        public override void DeserializeData(MapVisualContainer container)
        {
            Guid mt = Guid.Parse(serializedData.mainTexture);
            Guid sm = Guid.Parse(serializedData.sharedMaterial);
            
            mainTexture = (Texture2D)container.GetObject(mt);
            sharedMaterial = (Material)container.GetObject(sm);
            
            mainColor = serializedData.mainColor;
        }
        public override T DeepCopy<T>()
        {
            BasicVisual clone = new BasicVisual(sharedMaterial, mainTexture, mainColor);
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
            public string sharedMaterial;
            public Color mainColor;

            public SerializedBasicVisual(BasicVisual basicVisual, MapVisualContainer container)
            {
                mainColor = basicVisual.mainColor;
                mainTexture = container.GetGuid(basicVisual.mainTexture)
                    .ToString();
                sharedMaterial = container.GetGuid(basicVisual.sharedMaterial)
                    .ToString();
            }
        }
    }
}
