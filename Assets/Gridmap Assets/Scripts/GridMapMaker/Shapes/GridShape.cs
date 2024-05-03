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

        /// <summary>
        /// This will contain the edge/bounds position of the shape.
        /// It is used to find/calculate the bounds and tesselated position of a shape
        /// </summary>
        protected ShapeVertexBounds svb;

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
        /// Will return a Vector with the respective bounds
        /// </summary>
        /// <param timerName="top"></param>
        /// <param timerName="bot"></param>
        /// <param timerName="left"></param>
        /// <param timerName="right"></param>
        protected virtual void SetBounds()
        {
             Vector3 top = baseVertices[0];
             Vector3 bot = baseVertices[0];
             Vector3 left = baseVertices[0];
             Vector3 right = baseVertices[0];

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

            svb = new ShapeVertexBounds();

            svb.left = left;
            svb.right = right;
            svb.top = top;
            svb.bot = bot;

            svb.leftF = left.x;
            svb.rightF = right.x;
            svb.topF = top.z;
            svb.botF = bot.z;

            SetShapeBounds();
        }
     
        protected virtual void SetShapeBounds()
        {
            // we then gridOffset the tesselated positions by the vertex positions to give us the bounds/edges
            Vector3 min = new Vector3(svb.leftF, 0, svb.botF);
            Vector3 max = new Vector3(svb.rightF, 0, svb.topF);

            shapeBounds = new Bounds((min + max) / 2, max - min);
        }
        
        /// <summary>
        /// If tesselation is Uniform, such as it is with rectangles, we can simply multiple the gridPosition gridOffset directly to the shape bounds without having to get the tesselated position of each cell. THIS WILL NOT WORK FOR UN-UNIFORM TESSELATION SUCH AS HEXES
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
                                Vector2Int maxGridPosition, Vector3 gridOffset = new())
        {
            // secondly we have to get the tesselatedPosition on the map,
            // we must also account for the gridOffset, that is the current position of the grid. A cell at 0,0 will have a tesselated position of 0,0, but if the entire grid is shifted 5 units to the right, then the tesselated position of the cell at 0,0 will be 5,0
            
            Vector3 botTes = GetTesselatedPosition(minGridPosition) + gridOffset;
            Vector3 topTes = GetTesselatedPosition(maxGridPosition) + gridOffset;

            // we then offset the tesselated positions by the shape edge positions to give us the precise positions of the edge
            Vector3 min = new Vector3(botTes.x + svb.leftF, 0,
                                      botTes.z + svb.botF);

            Vector3 max = new Vector3(topTes.x + svb.rightF, 0,
                                      topTes.z + svb.topF);

            Bounds b1 = new Bounds((min + max) / 2, max - min);
            return b1;
        }
        public virtual bool WithinShapeBounds(GridShape anotherShape)
        {
            Bounds currentShape = GetGridBounds(Vector2Int.zero, Vector2Int.zero);

            Bounds otherShape = anotherShape.ShapeBounds;

            return currentShape.Contains(otherShape);
        }

        public virtual GridShape Init(GridShape shape, Vector2 cellGap)
        {
            GridShape s = Instantiate(shape);

            s.cellGap = cellGap;

            s.SetBounds();

            return s;
        }

        public bool Equals(GridShape other)
        {
            // compare names

            return uniqueShapeName.Equals(other);
        }

        public struct ShapeVertexBounds
        {
            public Vector3 top;
            public Vector3 bot;
            public Vector3 left;
            public Vector3 right;

            public float topF;
            public float botF;
            public float leftF;
            public float rightF;
        }
    }
}
