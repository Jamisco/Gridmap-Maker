using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData
{
    [Serializable]
    public abstract class VisualProperties : IEquatable<VisualProperties>
    {
        public delegate void VisualIdChanged(int oldHash, ShapeVisualData newVisualData);

        public event VisualIdChanged VisualIdChange;
        
        protected Material sharedMaterial;
        protected MaterialPropertyBlock propertyBlock;
        protected abstract ISerializedVisual SerializedData { get; }

        [ShowOnlyField]
        [SerializeField]
        protected int visualId;
        public virtual int VisualId
        {
            get => visualId; protected set => visualId = value;
        }
        public virtual T ShallowCopy<T>() where T : VisualProperties
        {
            return (T)MemberwiseClone();
        }
        public abstract void  SetMaterialProperties();

        /// <summary>
        /// This is will check if the visual hash has changed. If it has, it will call the event to notify the visual data has changed.
        /// </summary>
        public virtual void CheckVisualHashChanged()
        {
            int oldHash = VisualId;
            int newHash = GenerateVisualId();

            if (oldHash != newHash)
            {
                // Set the new hash here, so we dont have to call the generate visual id again.
                VisualId = newHash;
                OnVisualHashChanged(oldHash);
            }
        }
        protected virtual void OnVisualHashChanged(int oldId)
        {
            VisualIdChange?.Invoke(oldId, GetShapeVisualData());
        }
        public abstract int GenerateVisualId();
        public virtual ShapeVisualData GetShapeVisualData()
        {
            SetMaterialProperties();
            return new ShapeVisualData(sharedMaterial, propertyBlock, VisualId);
        }
        
        /// <summary>
        /// Use this to initialize the private variable you use to fulfill the IserializedData abstract variable. When you set the data, we can then serialize the the entire class with that variable. Then when we deserialize, we simply get the data from said variable. The reason we dont return a serialize datatype is due to type casting issues. 
        /// </summary>
        /// <param name="container"></param>
        public abstract void SetSerializeData(MapVisualContainer container);
        public abstract void DeserializeData(MapVisualContainer container);
        public virtual bool Equals(VisualProperties other)
        {
            return other.VisualId == VisualId;
        }
        public override int GetHashCode()
        {
            return VisualId;
        }
    }
}
