using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GridMapMaker
{
    /// <summary>
    /// A base class for all visual data. This class is used to create visuals for shapes.
    /// Dont modify the base fields from editor. 
    /// </summary>
    [Serializable]
    public abstract class ShapeVisualData : IEquatable<ShapeVisualData>
    {

        public const string _mainTex = "_MainTex";
        public const string _mainColor = "_Color";
        /// <summary>
        /// A custom name you can use to identify the visual data. This is not used for anything and is just a custom name you can use to identify the visual data.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private string visualName = "";

        /// <summary>
        /// A custom name you can use to identify the visual data. This is not used for anything and is just a custom name you can use to identify the visual data.
        /// </summary>
        public string VisualName
        {
            get => visualName;
            protected set => visualName = value;
        }

        /// <summary>
        /// The name of the material this visual data is using.
        /// </summary>
        public string MaterialName
        {
            get
            {
                if (sharedMaterial == null)
                {
                    return "";
                }

                return sharedMaterial.name;
            }
        }

        /// <summary>
        /// This color will be used when rendermode is set to MeshColor. Note that this will not use nor set the color property of the material instead it will set the color directly into the vertices of the shape.
        /// </summary>
        public Color VisualColor { get; protected set; } = Color.white;

        [SerializeField]
        [HideInInspector]
        private int visualHash = defaultHash;
        private const int defaultHash = -111111;
        /// <summary>
        /// The VisualHash is used to identify if 2 visual Data are thesame. That is, if 2 visualdata have thesame material and thesame properties, they should have thesame hash. It is Highly recommended you override the GetVisualHash method and cache the hashcode in this variable.
        public int VisualHash
        {
            get
            {
                if(visualHash == defaultHash)
                {
                    visualHash = CalculateVisualHash();
                }

                return visualHash;
            }
        }

        public delegate void MaterialPropertyChanged(ShapeVisualData sender);

        public event MaterialPropertyChanged MaterialPropertyChange;

        [SerializeField]
        [HideInInspector]
        protected MaterialPropertyBlock propertyBlock = null;
        public virtual MaterialPropertyBlock PropertyBlock
        {
            get => propertyBlock;
            protected set
            {
                propertyBlock = value;
                OnMaterialPropertyChanged(this);
            }
        }

        [SerializeField]
        [HideInInspector]
        protected Material sharedMaterial;

        public Material SharedMaterial
        {
            get => sharedMaterial;
            protected set
            {
                sharedMaterial = value;
            }
        }

        [SerializeField]
        [HideInInspector]
        private RenderMode dataRenderMode = RenderMode.Material;
        public RenderMode DataRenderMode { get => dataRenderMode; set => dataRenderMode = value; }
        /// <summary>
        /// The render mode of the visual data. This will determine whether  we should use a material or a color to render the shape. For example, the ColorVisualData has its renderMode set to MeshColor. Generally, you should use Material render mode. If you want to use only a color, you should use the ColorVisualData class provided. Note that when you use meshcolor, the color is set directly into the vertices of the shape, It will not be set by the material
        /// </summary>
        public enum RenderMode { Material, MeshColor };


        /// <summary>
        /// Set the material properties of your material in this method. You should call this on your own accord. Be advised that changing the material properties will not take effect until the encapsulating shape is drawn or redrawn. If you wish to update the material properties immediately, you should call the OnMaterialPropertyChanged method.
        /// </summary>
        /// 
        protected abstract void SetMaterialPropertyBlock();

        /// <summary>
        /// This will call SetMaterial properties and then calculates and sets the visual hash. This is useful when you want to update the material properties before rendering.
        /// </summary>
        public void ValidateVisualHash()
        {
            SetMaterialPropertyBlock();
            InvalidateVisualData();
        }

        /// <summary>
        /// Call this method when you want to invoke the MaterialPropertyChange event. This is useful when you want to update the material properties and notify the listeners that the material properties have changed. Recommended to use this method instead at the end of SetMaterialPropertyBlock.
        /// </summary>
        /// <param name="sender"></param>
        protected virtual void OnMaterialPropertyChanged(ShapeVisualData sender)
        {
            MaterialPropertyChange?.Invoke(this);
        }

        /// <summary>
        /// Will return a shallow copy of the visual data. This is useful when you want to create a new visual data that looks thesame as the original and pass it around. The returned visual data will STILL SHARE REFERENCES. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T ShallowCopy<T>() where T : ShapeVisualData
        {
            return (T)MemberwiseClone();
        }

        private void InvalidateVisualData()
        {
            // this is used to invalidate the visual data. This will cause the visual data to be recalculated and the hash to be updated the next time visualHash is accessed.
            // this is useful when you want to update the visual data without having to call ValidateVisualData
            visualHash = -111111;
        }

        /// <summary>
        /// This gets and combines the properties from the material in order to get a unique hash.
        /// This is an expensive operation and it is recommended you override and create your own hash code based on your visual data.
        /// </summary>
        /// <returns></returns>
        protected int CalculateVisualHash_Expensive()
        {
            SetMaterialPropertyBlock();
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

        /// <summary>
        /// Sets the shared material of the visual data. This is useful when you want to have a seperate material that is no longer shared with the original material. Be sure to pass a new reference of the material and also modify the properties of the material.
        /// </summary>
        /// <param name="newMat"></param>
        public virtual void SetMaterial(Material newMat)
        {
            sharedMaterial = newMat;
            InvalidateVisualData();
        }

        /// <summary>
        /// This is used to get a unique hash for the visual data. Such that if 2 visuals have thesame material and properties, they will have thesame hash.
        /// By default, this is the same as GetVisualHash_Expensive. It is recommended you override this and create your own hash code based on your material properties.
        /// For example, if you have a texture and a color, you can do Hash.combine(Material, texture, color) to get a unique hash.
        /// </summary>
        /// <returns></returns>
        public virtual int CalculateVisualHash()
        {
            return CalculateVisualHash_Expensive();
        }

        /// <summary>
        /// The hashcode simply calls the GetVisualHash method. Thus it is hihgly recommended you override GetVisualHash().
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return sharedMaterial.GetHashCode();
        }


        public virtual bool Equals(ShapeVisualData other)
        {
            return VisualHash == other.VisualHash;
        }

        /// <summary>
        /// This is used to compare visual data for equality or hash(reference).
        /// The reason we use a seperate comparer is because sometimes we dont know if we should compare visuals by hash or by equality. 
        /// </summary>
        public class VisualDataComparer : IEqualityComparer<ShapeVisualData>
        {
            public bool UseVisualHash { get; set; }
            public bool Equals(ShapeVisualData x, ShapeVisualData y)
            {
                if (UseVisualHash == true)
                {
                    if (x.VisualHash == y.VisualHash)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (x.sharedMaterial == y.sharedMaterial)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            public int GetHashCode(ShapeVisualData obj)
            {
                if (UseVisualHash == true)
                {
                    return obj.VisualHash;
                }
                else
                {
                    return obj.GetHashCode();
                }
            }
        }

    }
}
