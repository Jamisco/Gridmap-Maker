using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    public class ShapeVisualData
    {
        public delegate void VisualDataChanged(object sender);

        // Declare the event using the delegate
        public event VisualDataChanged DataChanged;
        public int VisualHash { get; private set; }

        private Texture mainTexture;
        public Texture MainTexture
        {
            get
            {
                return mainTexture;
            }
            set
            {
                mainTexture = value;
                propertyBlock.SetTexture("_MainTex", value);
                OnDataChanged();
            }
        }
        
        private Color mainColor;
        public Color MainColor
        {
            get
            {
                return mainColor;
            }
            set
            {
                mainColor = value;
                propertyBlock.SetColor("_Color", value);
                OnDataChanged();
            }
        }

        [SerializeReference] private int uniqueSeed = 0;
        public int UniqueSeed
        {
            get
            {
                return uniqueSeed;
            }
            set
            {
                uniqueSeed = value;
            }
        }

        [SerializeReference] private Material sharedMaterial;
        public Material SharedMaterial
        {
            get
            {
                return sharedMaterial;
            }
            set
            {
                sharedMaterial = value;
                OnDataChanged();
            }
        }

        private MaterialPropertyBlock propertyBlock;
        public MaterialPropertyBlock PropertyBlock
        {
            get
            {
                return propertyBlock;
            }
            set
            {
                propertyBlock = value;
                OnDataChanged();
            }
        }

        private bool useProperty = true;

        public ShapeVisualData(Material material, int uniqueSeed = 0)
        {
            SharedMaterial = material;
            propertyBlock = new MaterialPropertyBlock();
            SetVisualHash();
        }

        public ShapeVisualData(Material material, MaterialPropertyBlock propertyBlock,                  int uniqueSeed = 0)
        {
            SharedMaterial = material;
            this.propertyBlock = propertyBlock;
            SetVisualHash();
        }

        /// <summary>
        /// Two materials or material property blocks are only thesame if they have thesame reference. Thus it is paramount you thesame references throught out your map
        /// </summary>
        private void SetVisualHash()
        {
            VisualHash = HashCode.Combine(SharedMaterial, PropertyBlock, uniqueSeed);
        }

        protected virtual void OnDataChanged()
        {
            SetVisualHash();
            DataChanged?.Invoke(this);
        }

        /// <summary>
        /// Two reference are thesame if the look visually thesame. In other words, two references are thesame if they have thesame mainColor or mainTexture
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            ShapeVisualData other = (ShapeVisualData)obj;

            if (other.VisualHash == VisualHash)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return VisualHash;
        }
    }
}
