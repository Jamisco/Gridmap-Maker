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
        public float width;
        public float height;
        private List<Vector3> baseVertices;
        private List<Vector2> baseUVs;
        private List<int> baseTriangles;

        public override float Width { get => width; set => width = value; }
        public override float Height { get => height; set => height = value; }
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
                new Vector3(0f, 0f, height / 2),
                new Vector3(width / 2, 0f, 0.25f * height),
                new Vector3(width / 2, 0f, -0.25f * height),
                
                new Vector3(0f, 0f, - (height / 2)),
                new Vector3(-(width / 2), 0f, -(0.25f * height)),
                new Vector3(-(width / 2), 0f, (0.25f * height)),
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
            position.x = x * width + (y % 2) * (width / 2.0f);
            position.y = 0;
            position.z = y * (height - height / 4.0f);

            return position;

        }
    }
}
