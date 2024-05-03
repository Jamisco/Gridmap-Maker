using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.Miscellaneous
{
    [Serializable]

    /// The reason for this class is that the SortingLayer class is not serializable.
    /// This is a workaround. Allowing us to serialize the sorting meshSortLayer.
    public struct SortingLayerPicker
    {
        [SerializeField] private string layerName;
        [SerializeField] private int orderInLayer;
        [ShowOnlyField] [SerializeField] private int layerID;

        public string LayerName
        {
            get => layerName;
            private set => layerName = value;
        }
        public int LayerID
        {
            get => layerID;
            private set => layerID = value;
        }
        public int OrderInLayer
        {
            get => orderInLayer;
            private set => orderInLayer = value;
        }

        public SortingLayerPicker(string layerName, int orderInLayer)
        {
            this.layerName = layerName;
            this.orderInLayer = orderInLayer;
            layerID = 0;
        }

        public void Validate()
        {
            layerID = SortingLayer.NameToID(LayerName);

            if (layerID == 0)
            {
                LayerName = "Default";
            }
        }
    }
}
