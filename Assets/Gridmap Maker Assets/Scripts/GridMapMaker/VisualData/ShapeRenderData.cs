using System;
using Unity.VisualScripting;
using UnityEngine;

namespace GridMapMaker
{
    /// <summary>
    /// This simply contains a material and a property block used to render the Shape
    /// </summary>
    /// 
    [Serializable]
    public struct ShapeRenderData
    {
        [ShowOnlyField]
        [SerializeField]
        private string visualName;
        public string VisualName { get => visualName; }

        public RenderType ShapeRenderType { get; private set; }
        public enum RenderType { Material, Color };

        [ShowOnlyField]
        private Material sharedMaterial;

        private MaterialPropertyBlock propertyBlock;

        public Color ShapeColor;
        
        public Material SharedMaterial
        {
            get
            {
                return sharedMaterial;
            }
        }
        public MaterialPropertyBlock PropertyBlock { get => propertyBlock; }

        public ShapeRenderData(Material material, Shader shader, 
                               MaterialPropertyBlock propertyBlock, string name = "")
        {
            visualName = name;

            if(material == null)
            {
                sharedMaterial = new Material(shader);
            }
            else
            {
                sharedMaterial = new Material(material);
            }
  
            this.propertyBlock = propertyBlock;

            ShapeRenderType = RenderType.Material;

            ShapeColor = Color.white;
        }

        public ShapeRenderData(Color shapeColor, string name = "")
        {
            visualName = name;
            ShapeColor = shapeColor;

            ShapeRenderType = RenderType.Color;
            sharedMaterial = null;
            propertyBlock = null;
        }

        public bool IsNullOrEmpty()
        {
            if (sharedMaterial == null || propertyBlock == null)
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// 2 Shape render Data are equal if they have thesame renderType, and the references of their material and property block is thesame
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ShapeRenderData other)
        {
            // if the render type is thesame, check if the render type is color, then check if the color is thesame, else check if the material and property block are thesame
            if (ShapeRenderType == other.ShapeRenderType )
            {
                if (ShapeRenderType == RenderType.Color)
                {
                    return ShapeColor == other.ShapeColor;
                }
            }
            else
            {
                return false;
            }
            
            if (other.sharedMaterial == sharedMaterial && other.propertyBlock == propertyBlock)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Will get the hash of all property values in a material property block to determine if two RenderData are equal. This is a default implementation provided for visual equality and it is EXTREMELY SLOW. It is recommended you not use it and implement a visualHash for all your visual data.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int GetVisualHash()
        {
            int hash = HashCode.Combine(sharedMaterial);

            foreach (MaterialPropertyType propType in
                                          Enum.GetValues(typeof(MaterialPropertyType)))
            {
                string[] propertyNames = sharedMaterial.GetPropertyNames(propType);

                foreach (string propertyName in propertyNames)
                {
                    object value = propertyBlock.GetValue(propertyName, propType);
                    hash = HashCode.Combine(hash, value);
                }
            }

            return hash;
        }
    }
}
