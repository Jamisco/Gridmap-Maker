using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    public class RectangularShape : GridShape
    {
        private List<Vector3> baseVertices;
        private List<Vector2> baseUVs;
        private List<int> baseTriangles;

        public float Width;
        public float Height;
        public override List<Vector3> BaseVertices
        {
            get => baseVertices;
            set => baseVertices = value;
        }
        public override List<Vector2> BaseUV
        {
            get => baseUVs;
            set => baseUVs = value;
        }
        public override List<int> BaseTriangles
        {
            get => baseTriangles;
            set => baseTriangles = value;
        }

        private void UpdateShape()
        {
            SetBaseVertices();
            SetBaseTriangles();
            SetBaseUVs();
        }
        private void SetBaseVertices()
        {
            baseVertices = new List<Vector3>
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
            baseTriangles = new List<int>
            {
                0, 1, 2,
                0, 2, 3
            };
        }
        private void SetBaseUVs()
        {
            baseUVs = new List<Vector2>
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
            mesh.vertices = baseVertices.ToArray();
            mesh.uv = baseUVs.ToArray();
            mesh.triangles = baseTriangles.ToArray();
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
