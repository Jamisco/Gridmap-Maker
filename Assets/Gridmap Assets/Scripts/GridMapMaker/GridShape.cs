using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    public abstract class GridShape
    {
        public abstract Vector3[] BaseVertices { get; set; }
        public abstract Vector2[] BaseUV { get; set; }
        public abstract int[] BaseTriangles { get; set; }

        public abstract Mesh GetBaseShape();
            
    }
}
