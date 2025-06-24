using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GridMapMaker
{

    // copy this and add at the top child class
    //[CreateAssetMenu(fileName = "GridShape", menuName = MenuName + "NewShape")]



    /// <summary>
    /// The GridShape class is the base class for all shapes in the GridMapMaker package. All shapes must inherit from this class. The class contains all the necessary methods and properties to create a shape that can be drawn on a gridmap.
    /// </summary>
    [Serializable]
    public abstract class GridShape : ScriptableObject, IEquatable<GridShape>
    {
        public const string MenuName = "GridMapMaker/GridShape/";

        [SerializeField]
        private string uniqueShapeName = "Unkown Shape Name";

        [SerializeField]
        private List<Vector2> baseVertices;

        [SerializeField]
        private List<Vector2> baseUVs;

        [SerializeField]
        private List<int> baseTriangles;

        /// <summary>
        /// The scale of the Shape. This scale the dimensions of the shapes vertices. This is set by the gridManager. You have to multiply your shapes (height and width) or vertices by this value if you plan on scaling the shape. If not, you can ignore this value.
        /// </summary>
        public virtual Vector2 scale { get; set; } = Vector2.one;

        /// <summary>
        /// This value will be set by the gridManager. You do not have to use this at all.
        /// </summary>
        protected Vector2 cellGap;
       /// <summary>
        /// This will contain the edge/bounds basePosition of the Shape.
        /// It is used to find/calculate the bounds and tesselated basePosition of a Shape
        /// </summary>
        private ShapeVertexBounds svb;

        public Vector2 CellGap
        {
            get { return cellGap; }
            set { cellGap = value; }
        }

        protected Bounds shapeBounds;

        /// <summary>
        /// The bounds of the shape. Bounds of the shape are used to calculate the bounds of the gridmap or small part of the gridmap
        /// </summary>
        public Bounds ShapeBounds
        {
            get { return shapeBounds; }
            protected set { shapeBounds = value; }
        }

        [SerializeField]
        private Orientation baseOrientation;

        [SerializeField]
        [ShowOnlyField]
        [Tooltip("This is the size of the shape in world space. This is for editor display purposes only. This value is not used or to be used any calculations.")]
        protected Vector2 ShapeWorldSize;

        private Orientation shapeOrientation;
        /// <summary>
        /// The orientation of the Shape when being displayed in a gridmap. XY means the Shape is displayed in the XY plane, XZ means the Shape is displayed in the XZ plane
        /// </summary>
        public enum Orientation { XY, XZ };
        /// <summary>
        /// The default orientation of the Shape vertices. 
        /// Which orientation are the base vertices in? When drawn will the they be in the XY plane or the XZ plane?
        /// </summary>
        public Orientation BaseOrientation
        {
            get
            {
                return baseOrientation;
            }
            protected set
            {
                baseOrientation = value;
            }
        }

        /// <summary>
        /// This is the orientation of the shape when it is drawn on the gridmap. This is the orientation that will be used to draw the shape on the gridmap and is used to calculate the bounds of the shape.
        /// </summary>
        public Orientation ShapeOrientation
        {
            get
            {
                return shapeOrientation;
            }
            set
            {
                if(shapeOrientation != value)
                {
                    shapeOrientation = value;
                    UpdateOrientation();
                }

            }
        }

        /// <summary>
        /// A unique name for your shape. Names are used to find specific Gridshapes in lists/collections. Thus all gridshape classes must have a unique name
        /// </summary>
        public string UniqueShapeName
        {
            get { return uniqueShapeName; }
            set { uniqueShapeName = value; }
        }
        /// <summary>
        /// The minimum number of vertices required to make a Shape
        /// </summary>
        public List<Vector2> BaseVertices
        {
            get { return baseVertices; }
            set { baseVertices = value; }
        }
        /// <summary>
        /// The minimum number of Uvs required to map a texture onto a Shape
        /// </summary>
        public List<Vector2> BaseUVs
        {
            get { return baseUVs; }
            set { baseUVs = value; }
        }

        /// <summary>
        /// The minimum number of triangles required to make a Shape
        /// </summary>
        public List<int> BaseTriangles
        {
            get { return baseTriangles; }
            set { baseTriangles = value; }
        }

        private MeshData shapeMesh;
        /// <summary>
        /// This function will return the base version of the Shape
        /// </summary>
        /// <returns></returns>
        public MeshData ShapeMesh
        {
            get => shapeMesh; private set => shapeMesh = value;
        }
        private List<Vector3> GetOrientedVertices()
        {
            List<Vector3> orientedVertices = new List<Vector3>();
            
            foreach (Vector3 vertex in BaseVertices)
            {
                Vector3 orientedVertex = new Vector3();
                
                switch (ShapeOrientation)
                {
                    case Orientation.XY:
                        orientedVertex = new Vector3(vertex.x, vertex.y, 0);
                        break;
                    case Orientation.XZ:
                        orientedVertex = new Vector3(vertex.x, 0, vertex.y);
                        break;
                }
                
                orientedVertices.Add(orientedVertex);
            }
            
            return orientedVertices;
        }

        /// <summary>
        // This is for editor display purposes only.
        // This way you know a rough idea of the size of the shape in world space.
        // If your sprite/image has a PPU of 100 and the shapeworldsize is 1x1, then the image will fit exactly within the shape bounds
        /// </summary>
        protected virtual void SetWorldSpaceSize()
        {
            try
            {
                SetBounds();
                ShapeWorldSize = new Vector2(shapeBounds.size.x, shapeBounds.size.y);
            }
            catch (Exception)
            {
            }
        }

        private void SetShapeMeshData()
        {
            MeshData meshData = new MeshData();
            
            meshData.Vertices = GetOrientedVertices();
            meshData.Uvs = BaseUVs;
            meshData.Triangles = BaseTriangles;

            List<Color> colors = new List<Color>();

            for (int i = 0; i < meshData.vertexCount; i++)
            {
                colors.Add(Color.white);
            }

            meshData.Colors = colors;

            shapeMesh = meshData;
        }

        /// <summary>
        /// Sets all the fields and properties of your shape to their base values. This method is the first method called whenever the shape is updated.
        /// </summary>
        protected abstract void SetBaseValues();

        /// <summary>
        /// Once all the base values and shape settings (ie cell gap) have been set. This method will update the shapeMesh, bounds and other properties of the shape accordingly.
        /// </summary>
        public void UpdateShape()
        {
            SetBaseValues();

            SetBounds();
            SetShapeMeshData();
            // updating orientation will set the shapeMesh
            UpdateOrientation();

        }

        /// <summary>
        /// The method will return the basePosition of a Shape on the grid given its coordinates denoted x, y. Your calculation should give the basePosition respective to the BaseOrientation
        /// </summary>
        /// <param timerName="cx">X coordinate on the grid</param>
        /// <param timerName="cy">Y coordinate on the grid</param>
        /// <returns></returns>
        protected abstract Vector2 GetBaseTesselatedPosition(int x, int y);
        /// <summary>
        /// The method will return the adjusted local position, denoting where to draw the shape. This method takes into account the orientation, cell gap and other settings of the shape
        /// </summary>
        ///  <param timerName="gridPosition">The grid basePosition of the Shape</param>
        /// <returns></returns>
        public Vector3 GetTesselatedPosition(Vector2Int gridPosition)
        {
            return GetTesselatedPosition(gridPosition.x, gridPosition.y);
        }

        /// <summary>
        /// The method will return the adjusted local position, denoting where to draw the shape. This method takes into account the orientation, cell gap and other settings of the shape
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public virtual Vector3 GetTesselatedPosition(int x, int y)
        {
            Vector2 pos = GetBaseTesselatedPosition(x, y);
                
            pos.x += (x * cellGap.x);
            pos.y += (y * cellGap.y);

            if (shapeOrientation == Orientation.XY)
            {
                return new Vector3(pos.x, pos.y, 0);
            }
            else
            {
                return new Vector3(pos.x, 0, pos.y);
            }
        }
        /// <summary>
        /// Thesame as the gettesselated method, but will not change from vector2 to vector3.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private Vector2 GetTesselatedPosition_V2(int x, int y)
        {
            Vector2 pos = GetBaseTesselatedPosition(x, y);

            pos.x += (x * cellGap.x);
            pos.y += (x * cellGap.y);

            return pos;
        }

        /// <summary>
        /// Given a local basePosition, the method will return the grid coordinate of the basePosition. Your calculation should give the grid coordinate respective to the BaseOrientation
        /// </summary>
        /// <param timerName="localPosition"></param>
        /// <returns></returns>
        protected abstract Vector2Int GetBaseGridCoordinate(Vector2 localPosition);

        /// <summary>
        /// Given a local position, get the grid position. This method will take into account the orientation of the shape and the cell gap for you. Thus there is no need to worry about such things.
        /// </summary>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        public Vector2Int GetGridCoordinate(Vector3 localPosition)
        {
            Vector2 pos;

            if (shapeOrientation == Orientation.XY)
            {
                pos = new Vector2(localPosition.x, localPosition.y);
            }
            else
            {
                pos = new Vector2(localPosition.x, localPosition.z);
            }


            return GetBaseGridCoordinate(pos);
        }

        /// <summary>
        /// When the orientation of the shape has been changed, this update the shape accordingly
        /// </summary>
        public void UpdateOrientation()
        {
            SetShapeMeshData();
        }
    /// <summary>
        /// Will set the bounds of the shape. Bounds of the shape are used to calculate the bounds of the gridmap or small part of the gridmap
        /// </summary>
        /// <param timerName="top"></param>
        /// <param timerName="bot"></param>
        /// <param timerName="left"></param>
        /// <param timerName="right"></param>
        protected void SetBounds()
        {
            Vector2 top = baseVertices[0];
            Vector2 bot = baseVertices[0];
            Vector2 left = baseVertices[0];
            Vector2 right = baseVertices[0];

            foreach (Vector2 vertex in baseVertices)
            {
                if (vertex.x < left.x)
                {
                    left = vertex;
                }

                if (vertex.x > right.x)
                {
                    right = vertex;
                }

                if (vertex.y > top.y)
                {
                    top = vertex;
                }

                if (vertex.y < bot.y)
                {
                    bot = vertex;
                }
            }

            svb = new ShapeVertexBounds();

            svb.left = left;
            svb.right = right;
            svb.top = top;
            svb.bot = bot;

            svb.leftF = left.x;
            svb.rightF = right.x;
            svb.topF = top.y;
            svb.botF = bot.y;

            // we then gridOffset the tesselated positions by the vertex positions to give us the bounds/edges
            Vector2 min = new Vector3(svb.leftF, svb.botF);
            Vector2 max = new Vector3(svb.rightF, svb.topF);

            shapeBounds = new Bounds((min + max) / 2, max - min);
        }

        /*
        /// <summary>
        /// If tesselation is Uniform, such as it is with rectangles, we can simply multiple the gridPosition gridOffset directly to the Shape bounds without having to get the tesselated localPosition of each cell. THIS WILL NOT WORK FOR UN-UNIFORM TESSELATION SUCH AS HEXES
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

        //    // cx and cy denote the center basePosition of the bounds
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
        */

        /// <summary>
        /// Given two positions, get the bounds that encapsulates the grid that results from drawing from min to max position. This method should work with all shapes, but you can override it if you want, perhaps to make it faster. This method is generally used to get the bounds of a layer, chunk or grid. You might also want to use this to see if a specific position is within the bounds of two positions.
        /// </summary>
        /// <param name="minGridPosition"></param>
        /// <param name="maxGridPosition"></param>
        /// <returns></returns>
        public virtual Bounds GetGridBounds(Vector2Int minGridPosition,
                                Vector2Int maxGridPosition)
        {
            // This method works my crawling the edges of the shape clock wise and getting the leftmost, rightmost, topmost and bottommost points of the shape.

            Vector2 minTesPos = GetTesselatedPosition(minGridPosition);
            Vector2 maxTesPos = GetTesselatedPosition(maxGridPosition);

            float left = minTesPos.x;
            float bottom = minTesPos.y;

            float right = maxTesPos.x;
            float top = maxTesPos.y;

            // Walk from bot left to top left
            for (int y = minGridPosition.y; y <= maxGridPosition.y; y++)
            {
                Vector3 pos = GetTesselatedPosition(new Vector2Int(minGridPosition.x, y));

                pos += new Vector3(svb.leftF, 0);

                if(pos.x < left)
                {
                    left = pos.x;
                }
            }

            // Walk top left to top right
            for (int x = minGridPosition.x; x <= maxGridPosition.x; x++)
            {
                Vector3 pos = GetTesselatedPosition(new Vector2Int(x, maxGridPosition.y));

                pos += new Vector3(0, svb.topF);

                if (pos.y > top)
                {
                    top = pos.y;
                }
            }

            // Walk top right to bottom right
            for (int y = maxGridPosition.y; y >= minGridPosition.y; y--)
            {
                Vector3 pos = GetTesselatedPosition(new Vector2Int(maxGridPosition.x, y));
                pos += new Vector3(svb.rightF, 0);
                if (pos.x > right)
                {
                    right = pos.x;
                }
            }

            // Walk bottom right to bottom left
            for (int x = maxGridPosition.x; x >= minGridPosition.x; x--)
            {
                Vector3 pos = GetTesselatedPosition(new Vector2Int(x, minGridPosition.y));
                pos += new Vector3(0, svb.botF);
                if (pos.y < bottom)
                {
                    bottom = pos.y;
                }
            }

            Vector2 min = new Vector2(left, bottom);
            Vector2 max = new Vector2(right, top);

            Vector2 size = max - min;
            Vector2 center = min + size / 2f;

            Bounds b1 = new Bounds(center, size);

            return b1;
        }

        /// <summary>
        /// Checks whether the current shapes contains the given shape.
        /// Used this to see if a given shape is contained within another shape. This is useful when you want to overlay shapes on top of each other.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool WithinShapeBounds(GridShape other)
        {
            return shapeBounds.Contains(other.ShapeBounds.min) && shapeBounds.Contains(other.ShapeBounds.max);
        }

        /// <summary>
        /// Given a local position, and a grid coordinate, check if said local position is in the shape at the given coordinate. This is useful when you want to map a local position to a grid position. It is quite expensive so only use this if you are unable to map local to grid position on your own accord
        /// </summary>
        /// <param name="localPosition"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsLocalPositionInShape(Vector2 localPosition, int x, int y)
        {
            List<Vector2> vertices = new List<Vector2>();

            Vector2 offset = GetTesselatedPosition_V2(x, y);

            for (int j = 0; j < BaseVertices.Count; j++)
            {
                vertices.Add(BaseVertices[j] + offset);
            }

            double minX = vertices[0].x;
            double maxX = vertices[0].x;
            double minY = vertices[0].y;
            double maxY = vertices[0].y;

            for (int i = 1; i < vertices.Count; i++)
            {
                Vector2 q = vertices[i];
                minX = Math.Min(q.x, minX);
                maxX = Math.Max(q.x, maxX);
                minY = Math.Min(q.y, minY);
                maxY = Math.Max(q.y, maxY);
            }

            if (localPosition.x < minX || localPosition.x > maxX || localPosition.y < minY || localPosition.y > maxY)
            {
                return false;
            }

            // https://wrf.ecse.rpi.edu/Research/Short_Notes/pnpoly.html
            bool inside = false;
            for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
            {
                if ((vertices[i].y > localPosition.y) != (vertices[j].y > localPosition.y) &&
                     localPosition.x < (vertices[j].x - vertices[i].x) * (localPosition.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// The hashcode of a shape is simply the hashcode of its UNIQUE name.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if(uniqueShapeName == null)
            {
                uniqueShapeName = "Unkown Shape Name";
            }
            return uniqueShapeName.GetHashCode();
        }
        /// <summary>
        /// 2 shapes are equal if they have thesame name
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(GridShape other)
        {
            // compare names

            return uniqueShapeName.Equals(other);
        }
        /// <summary>
        /// A simple struct to hold the edge positions of a shape
        /// </summary>
        private struct ShapeVertexBounds
        {
            public Vector2 top;
            public Vector2 bot;
            public Vector2 left;
            public Vector2 right;

            public float topF;
            public float botF;
            public float leftF;
            public float rightF;
        }
    }
}
