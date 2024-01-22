using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    public abstract class GridShape
    {
        [Tooltip("The Distance from the Left to the Right vertex")]
        public abstract float Width { get; set; }

        [Tooltip("The Distance from the Top to the Bottom vertex")]
        public abstract float Height { get; set; }

        /// <summary>
        /// The minimum number of vertices required to make a shape
        /// </summary>
        public abstract List<Vector3> BaseVertices { get; set; }

        /// <summary>
        /// The minimum number of UVs required to map a texture onto a shape
        /// </summary>
        public abstract List<Vector2> BaseUV { get; set; }

        /// <summary>
        /// The minimum number of triangles required to make a shape
        /// </summary>
        public abstract List<int> BaseTriangles { get; set; }

        /// <summary>
        /// This function will return the base version of the shape
        /// </summary>
        /// <returns></returns>
        public abstract Mesh GetBaseShape();


        /// <summary>
        /// The method will return the position of a shape on the grid given its coordinates denoted x, y
        /// </summary>
        /// <param name="x">X coordinate on the grid</param>
        /// <param name="y">Y coordinate on the grid</param>
        /// <returns></returns>
        public abstract Vector3 GetTesselatedPosition(int x, int y);
            
    }
}
