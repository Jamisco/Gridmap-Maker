using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    [Tooltip("This class is used to store a simple texture and/or color for a shape." +
             "\n Use this to quickly store visual data for all your shapes.")] 
    public class QuickVisualData
    {
        [SerializeField] private string visualName;
        [SerializeField] private Material material;
        [SerializeField] private Texture texture;
        [SerializeField] private Color color;
        [SerializeField] private int uniqueSeed;
        [ShowOnlyField][SerializeField] private int visualHash;

        public string VisualName { get => visualName; set => visualName = value; }
        public Material Material { get => material; set => material = value; }
        public Texture Texture { get => texture; set => texture = value; }
        public Color Color { get => color; set => color = value; }
        public int VisualHash
        {
            get
            {
                return visualHash;
            }
        }
        public int UniqueSeed
        {
            get
            {
                return uniqueSeed;
            }
        }


        public QuickVisualData(string visualName, Texture texture, int uniqueSeed = 0)
        {
            this.texture = texture;
            this.visualName = visualName;
            this.uniqueSeed = uniqueSeed;
            color = Color.white;

            visualHash = 0;
            visualHash = GetHashCode();
        }
        public QuickVisualData(string visualName, Color color, int uniqueHash = 0)
        {
            this.visualName = visualName;
            this.color = color;
            this.uniqueSeed = uniqueHash;

            texture = null;

            visualHash = 0;
            visualHash = GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            QuickVisualData other = (QuickVisualData)obj;

            if (other.VisualHash == VisualHash)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ValidateHashCode()
        {
            visualHash = GetHashCode();
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(visualName, texture, color, uniqueSeed);
        }

        /// <summary>
        /// Creates a ShapeVisualData object from this QuickVisualData. Be advised that this will usually create ShapeVisualData with different visual hash when it is called, even tho the data is the same.
        /// </summary>
        /// <param name="uniqueSeed"></param>
        /// <returns></returns>
        public ShapeVisualData CreateShapeVisualData(int uniqueSeed = 0)
        {
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            
            if (texture != null)
            {
                props.SetTexture("_MainTex", texture);
            }

            props.SetColor("_Color", Color);
            
            return new ShapeVisualData(material, props, uniqueSeed);
        }
    }
}
