using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.Rendering;
using static Assets.Scripts.Miscellaneous.ExtensionMethods;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData
{
    [Serializable]
    public abstract class ShapeVisualData : IEquatable<ShapeVisualData>
    {
        [ShowOnlyField]
        [SerializeField]
        private string visualName = "";
        public virtual string VisualName
        {
            get => visualName; protected set => visualName = value;
        }

        /// <summary>
        /// Similar to getHashCode, if 2 visuals have thesame reference, they should be equal. This is a unique identifier for a visual data during serialization/deserilization
        /// </summary>
        public Guid VisualId { get; private set; }

        /// <summary>
        /// For faster comparison, Generate a visual hash code such that if two visual data are equal, they should have the same hash code.
        /// This Should return a hash code such that 2 visual data that look thesame should have thesame hash. Make sure the hashcode is unique only for a specific visual look.
        /// </summary>
        protected virtual int VisualHash { get; set; }

        public delegate void VisualDataChanged(ShapeVisualData sender);

        public event VisualDataChanged VisualIdChange;

        protected Material sharedMaterial;
        protected MaterialPropertyBlock propertyBlock;
        protected abstract ISerializedVisual SerializedData { get; }

        public static string mainTexProperty = "_MainTex";
        public static string mainColorProperty = "_Color";

        public ShapeVisualData()
        {
            VisualId = Guid.NewGuid();

            VisualHash = -111111;
        }

        /// <summary>
        /// Set the material properties of your shader in this method. This will be called when the visual data is being used to render a shape
        /// </summary>
        public abstract void SetMaterialProperties();

        public void VisualIdChanged()
        {
            OnVisualIdChanged(this);
        }
        protected virtual void OnVisualIdChanged(ShapeVisualData sender)
        {
            VisualIdChange?.Invoke(this);
            VisualHash = GetShaderVisualHash();

        }
        public virtual ShapeRenderData GetShapeRenderData()
        {
            SetMaterialProperties();
            return new ShapeRenderData(sharedMaterial, propertyBlock, visualName);
        }

        /// <summary>
        /// Will return a shallow copy of the visual data. This is useful when you want to create a new visual data that looks thesame as the original. The returned visual data will STILL SHARE REFERENCES. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T ShallowCopy<T>() where T : ShapeVisualData
        {
            return (T)MemberwiseClone();
        }

        /// <summary>
        /// This will get the render data of the visual data and then get the VisualHash of said data. This is an expensive operation and should be used sparingly.
        /// </summary>
        /// <returns></returns>
        protected int GetShaderVisualHash()
        {
            ShapeRenderData data = GetShapeRenderData();
            return data.GetVisualHash();
        }

        /// <summary>
        /// Does a deep copy by serializing and deserializing the visual data. This is useful when you want to create a new visual data that looks thesame as the original. The returned visual data will NOT SHARE REFERENCES.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public virtual T DeepCopy<T>(MapVisualContainer container) where T : ShapeVisualData
        {
            T copy = (T)MemberwiseClone();

            copy.SetSerializeData(container);
            copy.DeserializeData(container);

            return copy;
        }
        public abstract T DeepCopy<T>() where T : ShapeVisualData;

        /// <summary>
        /// Use this to initialize the private variable you use to fulfill the IserializedData abstract variable. When you set the data, we can then serialize the the entire class with that variable. Then when we deserialize, we simply get the data from said variable. The reason we dont return a serialize datatype is due to type casting issues. 
        /// </summary>
        /// <param timerName="container"></param>
        public abstract void SetSerializeData(MapVisualContainer container);
        public abstract void DeserializeData(MapVisualContainer container);
        public virtual bool Equals(ShapeVisualData other)
        {
            return VisualId == other.VisualId;
        }

        /// <summary>
        /// Returns true if 2 visuals look thesame. This is a very expensive operation than the default equality check and should be used sparingly.
        /// Highly recommended to implement this if you also want to draw shapes that look the same as one. If implemented properly could significantly increase performance.
        /// If you implement this, you should also implement GetVisualEqualityHash too. 
        /// </summary>
        /// <param timerName="other"></param>
        /// <returns></returns>
        public bool VisuallyEquals(ShapeVisualData other)
        {
            return VisualHash == other.VisualHash;
        }
        
        /// <summary>
        /// This is used to compare visuals.
        /// </summary>
        public class VisualDataComparer : IEqualityComparer<ShapeVisualData>
        {
            public bool UseVisualHash { get; set; }
            public bool Equals(ShapeVisualData x, ShapeVisualData y)
            {
                if (UseVisualHash == true)
                {
                    return x.VisuallyEquals(y);
                }
                else
                {
                    return x.Equals(y);
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
