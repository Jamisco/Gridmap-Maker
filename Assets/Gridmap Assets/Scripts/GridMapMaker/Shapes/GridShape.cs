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
        public abstract Mesh GetShapeMesh();
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

        /// <summary>
        /// Given a local position, the method will return the grid coordinate of the position
        /// </summary>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        public abstract Vector2Int GetGridCoordinate(Vector3 localPosition);

        /// <summary>
        /// Will return just the float of the respective bounds
        /// </summary>
        /// <param name="top"></param>
        /// <param name="bot"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        private void GetVertexBounds(out float top, out float bot, out float left, out float right)
        {
            Vector3 top1, bot1, left1, right1;

            GetVertexBounds(out top1, out bot1, out left1, out right1);

            top = top1.y;
            bot = bot1.y;

            left = left1.x;
            right = right1.x;
        }

        /// <summary>
        /// Will return a Vector with the respective bounds
        /// </summary>
        /// <param name="top"></param>
        /// <param name="bot"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        private void GetVertexBounds(out Vector3 top, out Vector3 bot, out Vector3 left, out Vector3 right)
        {
             top = baseVertices[0];
             bot = baseVertices[0];
             left = baseVertices[0];
             right = baseVertices[0];

            foreach (Vector3 vertex in baseVertices)
            {
                if (vertex.y > top.y)
                {
                    top = vertex;
                }

                if (vertex.y < bot.y)
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
        /// For example, if the vertex bounds is set to top, the method will return the vertex with the highest y value vice versa for the other bot etc..
        /// </summary>
        /// <param name="bounds"></param>
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
        /// <param name="gridPosition"></param>
        /// <param name="vertexBounds"></param>
        /// <returns></returns>
        public virtual Vector3 GetVertexBoundsOffset(Vector2Int gridPosition, VertexBounds vertexBounds)
        {
            Vector3 vertexOffset = GetVertexBoundsPosition(vertexBounds);

            Vector3 position = GetTesselatedPosition(gridPosition);

            return position + vertexOffset;
        }

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
            Vector3 min = new Vector3(botTes.x + left, botTes.y + bot);
            Vector3 max = new Vector3(topTes.x + right, topTes.y + top);

            return new Bounds((min + max) / 2, max - min);
        }

        public virtual Bounds GetShapeBounds()
        {
            float top, bot, left, right;

            // First, we get the topmost, leftmost etc... vertex positions
            GetVertexBounds(out top, out bot, out left, out right);

            // secondly we have to get the tesselatedPosition on the map
            Vector3 botTes = Vector3.zero;
            Vector3 topTes = Vector3.zero;

            // we then offset the tesselated positions by the vertex positions to give us the bounds/edges
            Vector3 min = new Vector3(botTes.x + left, botTes.y + bot);
            Vector3 max = new Vector3(topTes.x + right, topTes.y + top);

            return new Bounds((min + max) / 2, max - min);
        }

        public virtual bool WithinShapeBounds(GridShape anotherShape)
        {
            Bounds currentShape = GetShapeBounds();

            Bounds otherShape = anotherShape.GetShapeBounds();

            return currentShape.Contains(otherShape);
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
