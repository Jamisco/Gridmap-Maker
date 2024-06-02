using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Scripts.Miscellaneous;
using Palmmedia.ReportGenerator.Core.Reporting.Builders.Rendering;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
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

        [ShowOnlyField]
        private Material sharedMaterial;

        private MaterialPropertyBlock propertyBlock;   
        
        public Material SharedMaterial
        {
            get
            {
                return sharedMaterial;
            }
        }
        public MaterialPropertyBlock PropertyBlock { get => propertyBlock; }

        public ShapeRenderData(Material material, 
                               MaterialPropertyBlock propertyBlock, string name = "")
        {
            visualName = name;
            sharedMaterial = material;
            this.propertyBlock = propertyBlock;
        }

        public bool IsNullOrEmpty()
        {
            if (sharedMaterial == null || propertyBlock == null)
            {
                return true;
            }

            return false;
        }

        public bool Equals(ShapeRenderData data)
        {
            if (data.sharedMaterial == sharedMaterial && data.propertyBlock == propertyBlock)
            {
                return true;
            }

            return false;
        }

        public bool VisuallyEqual(ShapeRenderData other)
        {
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
