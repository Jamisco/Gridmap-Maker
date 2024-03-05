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
    public abstract class VisualProperties
    {
        public delegate void VisualIdChanged(int oldHash, ShapeVisualData newVisualData);

        public event VisualIdChanged VisualIdChange;
        Material SharedMaterial { get;}
        MaterialPropertyBlock PropertyBlock { get;}
        SerializedVisual SerializedData { get; }
        public abstract int VisualId { get; }
        public abstract void  SetMaterialProperties();
        public abstract void VisualHashChanged();

        protected virtual void OnVisualHashChanged(int oldId)
        {
            VisualIdChange?.Invoke(oldId, GetShapeVisualData());
        }
        public abstract int GenerateVisualId();
        public abstract int GetVisualId();
        public abstract ShapeVisualData GetShapeVisualData();     
        public abstract void SerializeData(MapVisualContainer container);
        public abstract void DeserializeData(MapVisualContainer container);
        public override int GetHashCode()
        {
            return GetVisualId();
        }
    }
}
