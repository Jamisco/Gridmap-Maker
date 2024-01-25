using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker
{
    public abstract class ShapeVisualData
    {
        public delegate void VisualDataChanged(object sender);

        // Declare the event using the delegate
        public event VisualDataChanged DataChanged;

        public abstract int VisualHash { get; protected set; }

        public abstract void SetHashCode();

        
    }
}
