using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    public abstract class GridShape : ScriptableObject
    {
        public const string MenuName = "GridMapMaker/GridShape/";
        [SerializeField]
        private string uniqueShapeId;
        
        [SerializeField]
        private List<Vector3> baseVertices;
        
        [SerializeField]
        private List<Vector2> baseUVs;

        [SerializeField]
        private List<int> baseTriangles;

        public string UniqueShapeId
        {
            get { return uniqueShapeId; }
            set { uniqueShapeId = value; }
        }
        /// <summary>
        /// The minimum number of vertices required to make a shape
        /// </summary>
        public List<Vector3> BaseVertices
        {
            get { return baseVertices; }
            set { baseVertices = value; }
        }

        /// <summary>
        /// The minimum number of UVs required to map a texture onto a shape
        /// </summary>
        public List<Vector2> BaseUVs
        {
            get { return baseUVs; }
            set { baseUVs = value; }
        }

        /// <summary>
        /// The minimum number of triangles required to make a shape
        /// </summary>
        public List<int> BaseTriangles
        {
            get { return baseTriangles; }
            set { baseTriangles = value; }
        }

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

        /// <summary>
        /// The method will return the position of a shape on the grid given its coordinates denoted in Vector2Int
        /// </summary>
        ///  <param name="gridPosition">The grid position of the shape</param>
        /// <returns></returns>
        public abstract Vector3 GetTesselatedPosition(Vector2Int gridPosition);

    }
}
