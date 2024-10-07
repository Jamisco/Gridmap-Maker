using GridMapMaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Worldmap.VisualDatas
{
    [Serializable]
    public class WaterVisualData : ShapeVisualData
    {
        [SerializeField]
        public Color secondaryColor;
        public WaterVisualData(Material material)
        {
            this.material = material;

            try
            {
                mainColor = material.GetColor("_MainColor");
                secondaryColor = material.GetColor("_SecondColor");
            }
            catch (Exception)
            {
                Debug.LogError("Material does not have the required properties for WaterVisualData. Verify that the material is correct.");
            }

            this.VisualHash = GetVisualHash();
        }
        public override void SetMaterialPropertyBlock()
        {
            if (PropertyBlock == null)
            {
                PropertyBlock = new MaterialPropertyBlock();
            }

            PropertyBlock.Clear();

            // set mainColor and secondaryColor
            try
            {
                PropertyBlock.SetColor("_MainColor", mainColor);
                PropertyBlock.SetColor("_SecondColor", secondaryColor);
            }
            catch (Exception)
            {
                Debug.LogError("Material does not have the required properties for WaterVisualData. Verify that the material is correct.");
            }
        }

        public override int GetVisualHash()
        {
            return material.GetHashCode();
        }

        public override ShapeVisualData DeepCopy()
        {
            return new WaterVisualData(material);
        }
    }
}
