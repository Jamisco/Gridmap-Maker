using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridMapMaker
{
    [Serializable]
    [CreateAssetMenu(fileName = "HexagonalShape", menuName = MenuName + "Hexagon")]
    public class HexagonalShape : GridShape
    {
        public float Width { get => 1 * scale.x; }
        public float Depth { get => 1 * scale.y; }
        private void OnValidate()
        {
            // create one giant mesh rendered and just add invidual shapes too it.
            // it will be likle a pesudo mesh layer.
            // we will use sprites default and just change the color of the shapes directly.
            // we can use the shape mesh fuser as our guide
            SetBaseValues();

            SetWorldSpaceSize();
        }

        float xTesselationConstant;
        float yTesselationConstant;

        protected override void SetBaseValues()
        {
            SetBaseVertices();
            SetBaseTriangles();
            SetBaseUVs();
            
            //BaseOrientation = Orientation.XZ;

            xTesselationConstant = (Width / 2.0f);
            yTesselationConstant = (Depth - Depth / 4.0f);
        }
        private void SetBaseVertices()
        {
            BaseVertices = new List<Vector2>
            {
                new Vector2(0f, Depth / 2),
                new Vector2(Width / 2, 0.25f * Depth),
                new Vector2(Width / 2, -0.25f * Depth),
                
                new Vector2(0f, - (Depth / 2)),
                new Vector2(-(Width / 2), -(0.25f * Depth)),
                new Vector2(-(Width / 2), (0.25f * Depth)),
            };
        }
        private void SetBaseTriangles()
        {
            // this is a base hex with 4 triangles
            BaseTriangles = new List<int>
            {
                4, 5, 0,
                4, 0, 1,
                4, 1, 2,
                4, 2, 3
            };
        }
        private void SetBaseUVs()
        {
            BaseUVs = new List<Vector2>
            {
                new Vector2(0.5f, 1),
                new Vector2(1, 0.75f),
                new Vector2(1, 0.25f),
                new Vector2(0.5f, 0),
                new Vector2(0, 0.25f),
                new Vector2(0, 0.75f)
            };   
        }

        protected override Vector2 GetBaseTesselatedPosition(int x, int y)
        {
            Vector2 position = new Vector2();
            // Calculate the center of each hexagon
            position.x = x * Width + ((y % 2) * (xTesselationConstant));
            position.y = y * yTesselationConstant;

            return position;
        }

        protected override Vector2Int GetBaseGridCoordinate(Vector2 localPosition)
        {
            // this function works as follows
            // we get a ball park estimate of the grid coordinate
            // then we iterate over the surrounding grid coordinates within a certain range
            // denoted by count variable
            // we calculate the distance of each grid coordinate from the local position
            // and return the grid coordinate with the smallest distance

            //float x = localPosition.x / (Width + cellGap.x);

            //// revese the function from getbasetesselation
            //float z = localPosition.y / (Depth + cellGap.y);

            float y = localPosition.y / (yTesselationConstant + cellGap.y);
            int y1 = Mathf.RoundToInt(y);
            
            // given a  position, choose random x postion, compare only y values
            float x = (localPosition.x - (y1 % 2) * (xTesselationConstant)) / (Width + cellGap.x);

            int x1 = Mathf.RoundToInt(x);

            // because of the way hexes are shaped, multiple grids can share thesame x or y world position. So when we narrow the world position to a specific grid position, we then check the surrounding positions to get the precise grid positions

            return GetClosestGrid(x1, y1);

            Vector2Int GetClosestGrid(int maxX, int maxZ)
            { 
                int count = 1;

                int xMin = Mathf.Max(0, maxX - count);
                int zMin = Mathf.Max(0, maxZ - count);

                maxX += count;
                maxZ += count;

                for (int x = maxX; x >= xMin; x--)
                {
                    for (int z = maxZ; z >= zMin; z--)
                    {
                        if (IsLocalPositionInShape(localPosition, x, z))
                        {
                            return new Vector2Int(x, z);
                        }
                    }
                }

                // this should never run
                return Vector2Int.left;
            }
        }
    }
}
