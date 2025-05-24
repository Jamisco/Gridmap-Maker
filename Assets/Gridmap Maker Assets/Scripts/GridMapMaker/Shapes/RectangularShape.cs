using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridMapMaker
{
    [Serializable]
    [CreateAssetMenu(fileName = "RectangularShape", menuName = MenuName + "Rectangle")]
    public class RectangularShape : GridShape
    {
        public float Width { get => 1 * scale.x; }
        public float Height { get => 1 * scale.y; }

        private void OnValidate()
        {
            SetBaseValues();
            SetWorldSpaceSize();
        }

        protected override void SetBaseValues()
        {
            SetBaseVertices();
            SetBaseTriangles();
            SetBaseUVs();

            //BaseOrientation = Orientation.XY;
        }
        private void SetBaseVertices()
        {
            BaseVertices = new List<Vector2>
            {
                new Vector2(- (Width / 2), (Height / 2)),
                new Vector2(Width / 2, Height / 2),
                new Vector2((Width / 2), - (Height / 2)),
                new Vector2(-(Width / 2), - (Height / 2)),
            };
        }
        private void SetBaseTriangles()
        {
            // this is a base hex with 4 triangles
            BaseTriangles = new List<int>
            {
                0, 1, 2,
                0, 2, 3
            };
        }
        private void SetBaseUVs()
        {
            BaseUVs = new List<Vector2>
            {
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f),
                new Vector2(0f, 0f)
            };
        }

        protected override Vector2 GetBaseTesselatedPosition(int x, int y)
        {
            return new Vector2(x * Width, y * Height);
        }

        protected override Vector2Int GetBaseGridCoordinate(Vector2 localPosition)
        {
            int x =  Mathf.RoundToInt(localPosition.x / Width);
            int y = Mathf.RoundToInt(localPosition.y / Height);

            return new Vector2Int(x, y);
        }
    }
}
