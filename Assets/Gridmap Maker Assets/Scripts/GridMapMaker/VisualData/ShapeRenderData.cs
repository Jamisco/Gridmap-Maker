using System;
using UnityEngine;

namespace GridMapMaker
{
    /// <summary>
    /// This simple contains a material and a property block used to render the Shape
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

        public ShapeRenderData(Shader shader, 
                               MaterialPropertyBlock propertyBlock, string name = "")
        {
            visualName = name;
            sharedMaterial = new Material(shader);
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

        public bool VisuallyEqual(ShapeRenderData other)
        {
            if (ShapeRenderType == other.ShapeRenderType)
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

            if (sharedMaterial != other.sharedMaterial)
            {
                return false;
            }
            
            MaterialPropertyBlock block = other.propertyBlock;
            // for each type of 
            foreach (MaterialPropertyType propType in
                                          Enum.GetValues(typeof(MaterialPropertyType)))
            {
                string[] propertyNames = sharedMaterial.GetPropertyNames(propType);

                foreach (string propertyName in propertyNames)
                {
                    object value1 = propertyBlock.GetValue(propertyName, propType);
                    object value2 = block.GetValue(propertyName, propType);

                    // if one of the value is null, then both values must be true for these values to be thesame
                    if(value1 == null || value2 == null)
                    {
                        if(value1 == value2)
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    
                    bool same = value1.Equals(value2);

                    if (!same)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

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
