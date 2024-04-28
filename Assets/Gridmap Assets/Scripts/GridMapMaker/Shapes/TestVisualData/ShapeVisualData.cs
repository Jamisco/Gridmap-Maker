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
        /// For faster comparison, Generate a visual hash code such that if two visual data are equal, they should have the same hash code
        /// </summary>
        protected int VisualEqualityHash { get; set; }

        public delegate void VisualDataChanged(ShapeVisualData sender);

        public event VisualDataChanged VisualIdChange;

        protected Material sharedMaterial;
        protected MaterialPropertyBlock propertyBlock;
        protected abstract ISerializedVisual SerializedData { get; }

        public static string mainTexProperty = "_MainTex";
        public static string mainColorProperty = "_Color";

        /// <summary>
        /// This is a list of types to ignore when using reflection to check for equality
        /// </summary>
        /// 
        protected virtual List<Type> IgnoredTypes { get; set; }

        public Guid VisualId { get; private set; }

        public ShapeVisualData()
        {
            VisualId = Guid.NewGuid();

            IgnoredTypes = new List<Type>();
            // this is done intentionally because 
            IgnoredTypes.Add(typeof(MaterialPropertyBlock));
            IgnoredTypes.Add(typeof(ISerializedVisual));
        }

        public abstract void SetMaterialProperties();

        public void VisualIdChanged()
        {
            OnVisualIdChanged(this);
        }
        protected virtual void OnVisualIdChanged(ShapeVisualData sender)
        {
            VisualIdChange?.Invoke(this);
        }
        public virtual ShapeRenderData GetShapeRenderData()
        {
            SetMaterialProperties();
            return new ShapeRenderData(sharedMaterial, propertyBlock, visualName);
        }
        public virtual T ShallowCopy<T>() where T : ShapeVisualData
        {
            return (T)MemberwiseClone();
        }
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
        public virtual bool VisuallyEquals(ShapeVisualData other)
        {
            ShapeRenderData data = GetShapeRenderData();
            ShapeRenderData otherData = other.GetShapeRenderData();

            bool val = data.VisuallyEqual(otherData);
            return val;
        }

        /// <summary>
        /// just a default hash code to use when the visual data is not set to use visual equality
        /// </summary>
        private const int defaultHashCode = -1111111;

        /// <summary>
        ///  This is used to generate a hash code for the visual data. Implement this if you want to use visual equality to compare visuals.
        /// This Should return a hash code such that 2 visual data that are thesame should have thesame hash. Make sure the hashcode is unique only for a specific visual look 
        /// If your method is too expensive/time taking, it might be best to cache the hash and change the VisualDataComparer to compare cache values accordingly
        /// </summary>
        public virtual int GetVisualEqualityHash()
        {
            ShapeRenderData data = GetShapeRenderData();
            return data.GetVisualHash();
        }
        

        /// <summary>
        /// This is used to compare visuals.
        /// </summary>
        public class VisualDataComparer : IEqualityComparer<ShapeVisualData>
        {
            public bool UseVisualEquality { get; set; }
            public bool Equals(ShapeVisualData x, ShapeVisualData y)
            {
                if (UseVisualEquality == true)
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
                if (UseVisualEquality == true)
                {
                    return obj.GetVisualEqualityHash();
                }
                else
                {
                    return obj.GetHashCode();
                }
            }
        }
    }
}
