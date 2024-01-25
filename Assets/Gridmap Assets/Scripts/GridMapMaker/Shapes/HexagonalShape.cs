using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    public class HexagonalShape : GridShape
    {
        private List<Vector3> baseVertices;
        private List<Vector2> baseUVs;
        private List<int> baseTriangles;

        public float Width;
        
        public float Height;
        public override List<Vector3> BaseVertices { get => baseVertices; 
                                                     set => baseVertices = value; }
        public override List<Vector2> BaseUV { get => baseUVs; 
                                               set => baseUVs = value; }
        public override List<int> BaseTriangles { get => baseTriangles; 
                                                  set => baseTriangles = value; }

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
                new Vector3(0f, 0f, Height / 2),
                new Vector3(Width / 2, 0f, 0.25f * Height),
                new Vector3(Width / 2, 0f, -0.25f * Height),
                
                new Vector3(0f, 0f, - (Height / 2)),
                new Vector3(-(Width / 2), 0f, -(0.25f * Height)),
                new Vector3(-(Width / 2), 0f, (0.25f * Height)),
            };
        }
        private void SetBaseTriangles()
        {
            // this is a base hex with 4 triangles
            baseTriangles = new List<int>
            {
                4, 5, 0,
                4, 0, 1,
                4, 1, 2,
                4, 2, 3
            };
        }
        private void SetBaseUVs()
        {
            baseUVs = new List<Vector2>
            {
                new Vector2(0.5f, 1),
                new Vector2(1, 0.75f),
                new Vector2(1, 0.25f),
                new Vector2(0.5f, 0),
                new Vector2(0, 0.25f),
                new Vector2(0, 0.75f)
            };   
        }
        public override Mesh GetBaseShape()
        {
            UpdateShape();
            
            Mesh baseMesh = new Mesh();

            baseMesh.vertices = baseVertices.ToArray();
            baseMesh.uv = baseUVs.ToArray();
            baseMesh.triangles = baseTriangles.ToArray();

            ExtensionMethods.SetFullColor(baseMesh, Color.green);

            return baseMesh;
        }
        public override Vector3 GetTesselatedPosition(int x, int y)
        {
            Vector3 position = new Vector3();
            // Calculate the center of each hexagon
            position.x = x * Width + (y % 2) * (Width / 2.0f);
            position.y = 0;
            position.z = y * (Height - Height / 4.0f);

            return position;

        }
        public override Vector3 GetTesselatedPosition(Vector2Int gridPosition)
        {
            return GetTesselatedPosition(gridPosition.x, gridPosition.y);
        }
    }
}
