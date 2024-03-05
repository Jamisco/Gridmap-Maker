using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    [CreateAssetMenu(fileName = "RectangularShape", menuName = MenuName + "Rectangle")]
    public class RectangularShape : GridShape
    {
        public float Width;
        public float Height;

        private void OnValidate()
        {
            UpdateShape();
        }
        private void UpdateShape()
        {
            SetBaseVertices();
            SetBaseTriangles();
            SetBaseUVs();
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

        public override Mesh GetBaseShape()
        {
            UpdateShape();
            Mesh mesh = new Mesh();
            mesh.vertices = BaseVertices.ToArray();
            mesh.uv = BaseUVs.ToArray();
            mesh.triangles = BaseTriangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }
        public override Vector3 GetTesselatedPosition(int x, int y)
        {
            return new Vector3(x * Width, 0f, y * Height);
        }

        public override Vector3 GetTesselatedPosition(Vector2Int gridPosition)
        {
            return GetTesselatedPosition(gridPosition.x, gridPosition.y);
        }
    }
}
