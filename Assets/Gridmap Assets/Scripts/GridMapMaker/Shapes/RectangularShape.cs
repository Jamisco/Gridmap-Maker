using Assets.Gridmap_Assets.Scripts.Miscellaneous;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.HexagonalShape;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    [CreateAssetMenu(fileName = "RectangularShape", menuName = MenuName + "Rectangle")]
    public class RectangularShape : GridShape
    {
        public float Width { get => Scale.x; }
        public float Height { get => Scale.y; }

        private void OnValidate()
        {
            SetBaseValues();
        }

        protected override void SetBaseValues()
        {
            SetBaseVertices();
            SetBaseTriangles();
            SetBaseUVs();

            BaseOrientation = Orientation.XZ;
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
            return new Vector2Int(Mathf.CeilToInt(localPosition.x / (Width + cellGap.x)), 
                Mathf.CeilToInt(localPosition.y / (Height + cellGap.y)));
        }
    }
}
