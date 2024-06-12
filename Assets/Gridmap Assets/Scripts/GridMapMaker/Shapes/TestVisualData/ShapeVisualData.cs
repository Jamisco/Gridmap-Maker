using Assets.Scripts.GridMapMaker;
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
        protected virtual int VisualHash { get; private set; }

        public delegate void VisualDataChanged(ShapeVisualData sender);

        public event VisualDataChanged VisualDataChange;

        protected Material sharedMaterial;
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

        public RenderMode ShapeRenderMode { get; set; } = RenderMode.Material;
        public enum RenderMode { Material, MeshColor };
        protected abstract ISerializedVisual SerializedData { get; }

        public static string mainTexProperty = "_MainTex";
        public static string mainColorProperty = "_Color";

        public const int DEFAULT_VISUAL_HASH = -1111111111;
        public ShapeVisualData()
        {
            VisualId = Guid.NewGuid();

            VisualHash = DEFAULT_VISUAL_HASH;
        }

        /// <summary>
        /// The Color Shader is the default shader used to render the color of the mesh. It is a simple shader that instructs the meshlayer to color the vertices of the mesh.Calling this method will make sure it is initialized
        /// </summary>
        public static void CreateDefaultVisual(Color defaultShapeColor)
        {
            if (colorShader == null)
            {
                colorShader = Shader.Find("GridMapMaker/MeshColorShader");
            }

            singletonInstance = new ColorVisualData(defaultShapeColor);
            singletonInstance.sharedMaterial = new Material(colorShader);
        }

        private static ShapeVisualData singletonInstance;
        public static ShapeVisualData GetDefaultVisual()
        {
            if(singletonInstance == null)
            {
                CreateDefaultVisual(Color.white);
            }
            
            return singletonInstance;
        }

        public static ShapeVisualData GetColorVisualData(Color color)
        {
            return new ColorVisualData(color);
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
            int oldHash = VisualHash;
            int newHash = GetVisualHash();

            if (oldHash != newHash)
            {
                OnVisualDataChanged(this);
                VisualHash = newHash;
            }
        }
        protected virtual void OnVisualDataChanged(ShapeVisualData sender)
        {
            VisualDataChange?.Invoke(this);
        }

        public virtual ShapeRenderData GetShapeRenderData()
        {
            SetMaterialPropertyBlock();
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
            return VisualId.GetHashCode();
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
        private class ColorVisualData : ShapeVisualData
        {
            protected override ISerializedVisual SerializedData => null;
            public ColorVisualData(Color mainColor)
            {
                ShapeRenderMode = RenderMode.MeshColor;
                this.mainColor = mainColor;
            }
            public override T DeepCopy<T>()
            {
                ColorVisualData clone = new ColorVisualData(mainColor);
                return clone as T;
            }
            public override void SetMaterialPropertyBlock()
            {
                propertyBlock = new MaterialPropertyBlock();
            }
            public override void SetSerializeData(MapVisualContainer container)
            {
                // not needed
            }
            public override void DeserializeData(MapVisualContainer container)
            {
                // not needed
            }
        }
    }
}
