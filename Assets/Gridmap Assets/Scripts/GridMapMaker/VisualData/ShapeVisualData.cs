using System;
using UnityEngine;

namespace GridMapMaker
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

        [SerializeField]
        private string visualIdString;

        public string VisualIdString => visualIdString;

    /// <summary>
        /// Similar to getHashCode, if 2 visuals have thesame reference, they should be equal. This is a unique identifier for a visual data during serialization/deserilization
        /// </summary>
        public Guid VisualId { get; private set; }

        /// <summary>
        /// The hashcode of the visualID, cached for performance during insertion/comparisons
        /// </summary>
        ///  
        public int VisualIdHash { get; private set; }

        [SerializeField]
        private int visualHash;
        /// <summary>
        /// For faster comparison, Generate a visual hash code such that if two visual data are equal, they should have the same hash code.
        /// This Should return a hash code such that 2 visual data that look thesame should have thesame hash. Make sure the hashcode is unique only for a specific visual look.
        /// </summary>
        public int VisualHash { get => visualHash; protected set => visualHash = value; }

        public delegate void VisualDataChanged(ShapeVisualData sender);

        public event VisualDataChanged VisualDataChange;

        protected Shader shader;
        protected MaterialPropertyBlock propertyBlock;

        private static Shader colorShader;
        public static Shader ColorShader
        {
            get
            {
                if (colorShader == null)
                {
                    colorShader = Shader.Find("GridMapMaker/MeshColorShader");
                }

                return colorShader;
            }
        }

        [SerializeField]
        public Color mainColor;
        [SerializeField]
        public Texture2D mainTexture;

        public RenderMode DataRenderMode { get; set; } = RenderMode.Material;
        public enum RenderMode { Material, MeshColor };
        /// <summary>
        /// Create a struct that implements this interface. Said struct should save and serialize all data necessary to recreate your visual data. Create a field of type (your struct) and assign in this property.
        /// This will be used to serialize and deserialize the visual data. Dont forget to add the serializable attribute to the struct and the field you make.
        /// </summary>
        protected abstract ISerializedVisual SerializedData { get; }

        public static string mainTexProperty = "_MainTex";
        public static string mainColorProperty = "_Color";

        public const int DEFAULT_VISUAL_HASH = -1111111111;
        public ShapeVisualData()
        {
            VisualId = Guid.NewGuid();
            visualIdString = VisualId.ToString();

            VisualIdHash = VisualId.GetHashCode();
            visualHash = DEFAULT_VISUAL_HASH;
        }
    /// <summary>
        /// Set the material properties of your shader in this method. This will be called when the visual data is being used to render a Shape
        /// </summary>
        public abstract void SetMaterialPropertyBlock();
            /// <summary>
        /// Will check if the visual hash has changed. If the hash hash changed, it will raise OnVisualDataChanged event and then update the visual hash.
        /// </summary>
        public void ValidateVisualHash()
        {
            int oldHash = visualHash;
            int newHash = GetVisualHash();

            if (oldHash != newHash)
            {
                OnVisualDataChanged(this);
                visualHash = newHash;
            }
        }
        protected virtual void OnVisualDataChanged(ShapeVisualData sender)
        {
            VisualDataChange?.Invoke(this);
        }

        public virtual ShapeRenderData GetShapeRenderData()
        {
            SetMaterialPropertyBlock();
            return new ShapeRenderData(shader, propertyBlock, visualName);
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
        /// This will get the render data of the visual data and then get the VisualHash of said data. This is an expensive operation and it is recommended you override and create your own hash code based on your visual data.
        /// </summary>
        /// <returns></returns>
        public virtual int GetVisualHash()
        {
            ShapeRenderData data = GetShapeRenderData();
            return data.GetVisualHash();
        }
        public override int GetHashCode()
        {
            return VisualIdHash;
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
        protected abstract void DeserializeVisualData(MapVisualContainer container);
        public void DeserializeData(MapVisualContainer container)
        {
            VisualId = Guid.Parse(visualIdString);
            VisualIdHash = VisualId.GetHashCode();

            DeserializeVisualData(container);
        }
        public virtual bool Equals(ShapeVisualData other)
        {
            return VisualIdHash == other.VisualIdHash;
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
            return visualHash == other.visualHash;
        }
          
    }
}
