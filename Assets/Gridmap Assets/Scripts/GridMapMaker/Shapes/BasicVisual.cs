
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
        private Material sharedMaterial;
        private MaterialPropertyBlock propertyBlock;
        
        [SerializeField]
        public Texture2D mainTexture;
        [SerializeField]
        public Color mainColor;

        [SerializeField]
        [HideInInspector]
        private SerializedBasicVisual serializedData;

        [ShowOnlyField]
        [SerializeField]
        private int visualId;
        
        static string textName = "_MainTex";
        static string colorName = "_Color";

        public BasicVisual(Material sharedMateiral, Texture2D texture, Color color)
        {
            sharedMaterial = sharedMateiral;
            mainTexture = texture;
            mainColor = color;

            visualId = GenerateVisualId();
            propertyBlock = new MaterialPropertyBlock();
        }
        public Material SharedMaterial => sharedMaterial;

        public MaterialPropertyBlock PropertyBlock => propertyBlock;
        private SerializedVisual SerializedData => serializedData;
        public override int VisualId => visualId;

        public override void SetMaterialProperties()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            // remember that setting the properties does not in any way change the hash of the propertyBlock reference

            if(mainTexture != null)
            {
                propertyBlock.SetTexture(textName, mainTexture);
            }

            propertyBlock.SetColor(colorName, mainColor);
        }

        public override void VisualHashChanged()
        {
            int oldHash = visualId;
            int newHash = GenerateVisualId();

            if (oldHash != newHash)
            {
                visualId = newHash;

                OnVisualHashChanged(oldHash);
            }
        }
        public override ShapeVisualData GetShapeVisualData()
        {
            SetMaterialProperties();
            return new ShapeVisualData(sharedMaterial, propertyBlock, visualId);
        }
        public override int GenerateVisualId()
        {
            int id1 = (mainTexture == null) ? 0 : mainTexture.GetInstanceID();
            int id2 = mainColor.ToString().GetHashCode();
            int id3 = sharedMaterial.GetInstanceID();

            return id1 ^ id2 ^ id3;
        }
        public override int GetVisualId()
        {
            return visualId;
        }

        public override void SerializeData(MapVisualContainer container)
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

        [Serializable]
        public struct SerializedBasicVisual : SerializedVisual
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
