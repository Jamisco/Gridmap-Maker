using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Assets.Scripts.Miscellaneous.HexFunctions;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    public abstract class GridShape : ScriptableObject, IEquatable<GridShape>
    {
        public const string MenuName = "GridMapMaker/GridShape/";
        
        [SerializeField]
        private string uniqueShapeName;

        [SerializeField]
        private List<Vector3> baseVertices;
        
        [SerializeField]
        private List<Vector2> baseUVs;

        [SerializeField]
        private List<int> baseTriangles;

        protected Vector2 cellGap;

        public Vector2 CellGap
        {
            get { return cellGap; }
            protected set { cellGap = value; }
        }

        protected Bounds shapeBounds;

        public Bounds ShapeBounds
        {
            get { return shapeBounds; }
            protected set { shapeBounds = value; }
        }

        public string UniqueShapeName
        {
            get { return uniqueShapeName; }
            set { uniqueShapeName = value; }
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
        public abstract Mesh GetShapeMesh();
        /// <summary>
        /// The method will return the position of a shape on the grid given its coordinates denoted cx, cy
        /// </summary>
        /// <param timerName="cx">X coordinate on the grid</param>
        /// <param timerName="cy">Y coordinate on the grid</param>
        /// <returns></returns>
        public abstract Vector3 GetTesselatedPosition(int x, int y);
        /// <summary>
        /// The method will return the position of a shape on the grid given its coordinates denoted in Vector2Int
        /// </summary>
        ///  <param timerName="gridPosition">The grid position of the shape</param>
        /// <returns></returns>
        public abstract Vector3 GetTesselatedPosition(Vector2Int gridPosition);

        /// <summary>
        /// Given a local position, the method will return the grid coordinate of the position
        /// </summary>
        /// <param timerName="localPosition"></param>
        /// <returns></returns>
        public abstract Vector2Int GetGridCoordinate(Vector3 localPosition);

        /// <summary>
        /// Will return just the float of the respective bounds
        /// </summary>
        /// <param timerName="top"></param>
        /// <param timerName="bot"></param>
        /// <param timerName="left"></param>
        /// <param timerName="right"></param>
        private void GetVertexBounds(out float top, out float bot, out float left, out float right)
        {
            Vector3 top1, bot1, left1, right1;

            GetVertexBounds(out top1, out bot1, out left1, out right1);
            
            top = top1.z;
            bot = bot1.z;

            left = left1.x;
            right = right1.x;
        }

        /// <summary>
        /// Will return a Vector with the respective bounds
        /// </summary>
        /// <param timerName="top"></param>
        /// <param timerName="bot"></param>
        /// <param timerName="left"></param>
        /// <param timerName="right"></param>
        private void GetVertexBounds(out Vector3 top, out Vector3 bot, out Vector3 left, out Vector3 right)
        {
             top = baseVertices[0];
             bot = baseVertices[0];
             left = baseVertices[0];
             right = baseVertices[0];

            foreach (Vector3 vertex in baseVertices)
            {
                if (vertex.z > top.z)
                {
                    top = vertex;
                }

                if (vertex.z < bot.z)
                {
                    bot = vertex;
                }

                if (vertex.x < left.x)
                {
                    left = vertex;
                }

                if (vertex.x > right.x)
                {
                    right = vertex;
                }
            }
        }

        /// <summary>
        /// Gets the vertex whose bounds are respective the the vertex bounds.
        /// For example, if the vertex bounds is set to top, the method will return the vertex with the highest cy value vice versa for the other bot etc..
        /// </summary>
        /// <param timerName="bounds"></param>
        /// <returns></returns>
        private Vector3 GetVertexBoundsPosition(VertexBounds bounds)
        {
            Vector3 top, bot, left, right;

            GetVertexBounds(out top, out bot, out left, out right);

            switch (bounds)
            {
                case VertexBounds.Top:
                    return top;
                case VertexBounds.Bottom:
                    return bot;
                case VertexBounds.Left:
                    return left;
                case VertexBounds.Right:
                    return right;
                default:
                    return Vector3.zero;
            }

        }

        /// <summary>
        /// Get the Vertex bounds position of a particular gridPosition
        /// </summary>
        /// <param timerName="gridPosition"></param>
        /// <param timerName="vertexBounds"></param>
        /// <returns></returns>
        public virtual Vector3 GetVertexBoundsOffset(Vector2Int gridPosition, VertexBounds vertexBounds)
        {
            Vector3 vertexOffset = GetVertexBoundsPosition(vertexBounds);

            Vector3 position = GetTesselatedPosition(gridPosition);

            return position + vertexOffset;
        }
        public virtual Bounds GetShapeBounds()
        {
            float top, bot, left, right;

            // First, we get the topmost, leftmost etc... vertex positions
            GetVertexBounds(out top, out bot, out left, out right);

            // we then offset the tesselated positions by the vertex positions to give us the bounds/edges
            Vector3 min = new Vector3(left, 0, bot);
            Vector3 max = new Vector3(right, 0, top);

            return new Bounds((min + max) / 2, max - min);
        }
        
        /// <summary>
        /// If tesselation is unition, such as it is with rectangles, we can simply multiple the gridPosition offset directly to the shape bounds without having to get the tesselated position of each cell. THIS WILL NOT WORK FOR UN-UNIFORM TESSELATION SUCH AS HEXES
        /// </summary>
        /// <param name="minGridPosition"></param>
        /// <param name="maxGridPosition"></param>
        /// <returns></returns>
        //public virtual Bounds GetGridBounds2(Vector2Int minGridPosition,
        //                                Vector2Int maxGridPosition)
        //{
        //    Bounds newBounds = new Bounds();

        //    // sx and sy denote the size of the bounds
        //    int sx = maxGridPosition.x - minGridPosition.x;
        //    int sy = maxGridPosition.y - minGridPosition.y;

        //    // cx and cy denote the center position of the bounds
        //    int cx = (maxGridPosition.x + minGridPosition.x) / 2;
        //    int cy = (maxGridPosition.y + minGridPosition.y) / 2;

        //    // the distance to travel from the center of one cell to the next immediate cell center
        //    Vector3 centerStep = shapeBounds.center + shapeBounds.size;

        //    float centerX = centerStep.x * cx;
        //    float centerZ = centerStep.z * cy;

        //    float sizeX = shapeBounds.size.x * (sx);
        //    float sizeZ = shapeBounds.size.z * (sy);

        //    Vector3 center = new Vector3(centerX, 0, centerZ);
        //    Vector3 size = new Vector3(sizeX, 0, sizeZ);

        //    newBounds.center = center;
        //    newBounds.size = size;


        //    Bounds b2 = GetGridBounds2(minGridPosition, maxGridPosition);
        //    return newBounds;
        //}

        public virtual Bounds GetGridBounds(Vector2Int minGridPosition,
                                        Vector2Int maxGridPosition)
        {
            float top, bot, left, right;

            // First, we get the topmost, leftmost etc... vertex positions
            GetVertexBounds(out top, out bot, out left, out right);

            // secondly we have to get the tesselatedPosition on the map
            Vector3 botTes = GetTesselatedPosition(minGridPosition);
            Vector3 topTes = GetTesselatedPosition(maxGridPosition);

            // we then offset the tesselated positions by the vertex positions to give us the bounds/edges
            Vector3 min = new Vector3(botTes.x + left, 0, botTes.z + bot);
            Vector3 max = new Vector3(topTes.x + right, 0, topTes.z + top);

            return new Bounds((min + max) / 2, max - min);
        }

        public virtual bool WithinShapeBounds(GridShape anotherShape)
        {
            Bounds currentShape = GetGridBounds(Vector2Int.zero, Vector2Int.zero);

            Bounds otherShape = anotherShape.GetShapeBounds();

            return currentShape.Contains(otherShape);
        }

        public virtual GridShape Init(GridShape shape, Vector2 cellGap)
        {
            GridShape s = Instantiate(shape);

            s.cellGap = cellGap;

            s.shapeBounds = GetShapeBounds();

            return s;
        }

        public bool Equals(GridShape other)
        {
            // compare names

            return uniqueShapeName.Equals(other);
        }

        public enum VertexBounds
        {
            Top,
            Bottom,
            Left,
            Right
        }

    }
}
