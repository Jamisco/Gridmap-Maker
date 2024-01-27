using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    public class ShapeVisualData : ISerializationCallbackReceiver
    {
        public delegate void VisualHashChanged(ShapeVisualData sender, int newHash);

        // Declare the event using the delegate
        public event VisualHashChanged HashChanged;

        [SerializeField]
        [Tooltip("The name of the visual data. The name does not influence the visual hash and thus, if two visual data only differ in name, they will still have the same hash.")]
        private string visualName;
        
        [Tooltip("The name of the visual data. The name does not influence the visual hash and thus, if two visual data only differ in name, they will still have the same hash.")]
        public string VisualName
        {
            get
            {
                return visualName;
            }
            set
            {
                visualName = value;
            }
        }

        [SerializeField]
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
                ValidateHashCode();
            }
        }

        [SerializeField]
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
                ValidateHashCode();
            }
        }

        [SerializeField] private Material sharedMaterial;
        public Material SharedMaterial
        {
            get
            {
                return sharedMaterial;
            }
            set
            {
                sharedMaterial = value;
                ValidateHashCode();
            }
        }
        
        [SerializeField] private int uniqueSeed = 0;
        public int UniqueSeed
        {
            get
            {
                return uniqueSeed;
            }
            set
            {
                uniqueSeed = value;
                ValidateHashCode();
            }
        }

        [ShowOnlyField]
        [SerializeField]
        private bool MainTextureInserted = false;

        [ShowOnlyField]
        [SerializeField]
        private bool MainColorInserted = false;

        [ShowOnlyField]
        [SerializeField]
        private int visualHash;
        public int VisualHash { get => visualHash; }

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
                ValidateHashCode();
            }
        }
        public ShapeVisualData(string name, Material material, int uniqueSeed = 0)
        {
            visualName = name;
            SharedMaterial = material;
            propertyBlock = new MaterialPropertyBlock();
            visualHash = GetVisualHash();
        }
        public ShapeVisualData(string name, Material material, MaterialPropertyBlock propertyBlock, int uniqueSeed = 0)
        {
            visualName = name;
            SharedMaterial = material;
            this.propertyBlock = propertyBlock;
            visualHash = GetVisualHash();
        }

        /// <summary>
        /// Two materials or material property blocks are only thesame if they have thesame reference. Thus it is paramount you thesame references throught out your map
        /// </summary>
        private int GetVisualHash()
        {
            return HashCode.Combine(sharedMaterial, mainTexture, mainColor, propertyBlock, uniqueSeed);
        }
        
        /// <summary>
        /// Sets the visual hash to its proper hash. Wont trigger the on visual hash changed event
        /// </summary>
        public void ValidateHashCode()
        { 
            if (visualHash != GetVisualHash())
            {
                OnVisualHashChanged();
            }
        }

        protected virtual void OnVisualHashChanged()
        {
            int newHash = GetVisualHash();
            HashChanged?.Invoke(this, newHash);
            visualHash = newHash;
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

        public void InsertMainIntoMaterialProperty()
        {
            if (propertyBlock == null)
            {
                MainTextureInserted = false;
                MainColorInserted = false;
                return;
            }

            MainTextureInserted = false;

            if (mainTexture != null)
            {
                propertyBlock.SetTexture("_MainTex", mainTexture);
                MainTextureInserted = true;
            }

            propertyBlock.SetColor("_Color", mainColor);
            MainColorInserted = true;

            // be advised that changing the properties such as texture or color of a material property does not change said Material hash codes. So we do it manually
            OnVisualHashChanged();
        }

        public void OnValidate()
        {
            InsertMainIntoMaterialProperty();
            ValidateHashCode();
        }


        public void OnBeforeSerialize()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        public void OnAfterDeserialize()
        {
            this.OnValidate();
        }
    }
}
