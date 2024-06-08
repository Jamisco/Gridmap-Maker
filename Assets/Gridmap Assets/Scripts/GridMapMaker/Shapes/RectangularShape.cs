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
            BaseVertices = new List<Vector3>
            {
                new Vector3(- (Width / 2), 0f, (Height / 2)),
                new Vector3(Width / 2, 0f, Height / 2),
                new Vector3((Width / 2), 0f, - (Height / 2)),
                new Vector3(-(Width / 2), 0f, - (Height / 2)),
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

        protected override Vector3 GetBaseTesselatedPosition(int x, int y)
        {
            return new Vector3(x * Width + (x * cellGap.x), 0f,
                               y * Height + (y * cellGap.y));
        }

        public override Vector2Int GetGridCoordinate(Vector3 localPosition)
        {
            return new Vector2Int(Mathf.CeilToInt(localPosition.x / Width), 
                Mathf.CeilToInt(localPosition.z / Height));
        }
    }
}
