
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Text.Json;


namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    public class BasicVisual : VisualProperties
    {
        
        [SerializeField]
        public Texture2D mainTexture;
        [SerializeField]
        public Color mainColor;

        [SerializeField]
        [HideInInspector]
        private SerializedBasicVisual serializedData;
        
        static string textName = "_MainTex";
        static string colorName = "_Color";

        public BasicVisual(Material material, Texture2D texture, Color color)
        {
            sharedMaterial = material;
            mainTexture = texture;
            mainColor = color;

            visualId = GenerateVisualId();
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
                propertyBlock.SetTexture(textName, mainTexture);
            }

            propertyBlock.SetColor(colorName, mainColor);
        }
        public override int GenerateVisualId()
        {
            int id1 = (mainTexture == null) ? 0 : mainTexture.GetInstanceID();
            int id2 = mainColor.ToString().GetHashCode();
            int id3 = sharedMaterial.GetInstanceID();

            return id1 ^ id2 ^ id3;
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

            visualId = serializedData.visualId;
        }

        //public override T ShallowCopy<T>()
        //{
        //    BasicVisual clone = new BasicVisual(sharedMaterial, mainTexture, mainColor);
        //    clone.visualId = visualId;
        //    return clone as T; 
        //}

        [Serializable]
        public struct SerializedBasicVisual : ISerializedVisual
        {
            public string mainTexture;
            public string sharedMaterial;
            public Color mainColor;
            public int visualId;

            public SerializedBasicVisual(BasicVisual basicVisual, MapVisualContainer container)
            {
                visualId = basicVisual.visualId;
                mainColor = basicVisual.mainColor;
                mainTexture = container.GetGuid(basicVisual.mainTexture)
                    .ToString();
                sharedMaterial = container.GetGuid(basicVisual.sharedMaterial)
                    .ToString();
            }
        }
    }
}
