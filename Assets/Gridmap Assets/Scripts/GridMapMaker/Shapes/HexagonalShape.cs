using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    [CreateAssetMenu(fileName = "HexagonalShape", menuName = MenuName + "Hexagon")]
    public class HexagonalShape : GridShape
    {
        [SerializeField]
        public float Width;
        
        [SerializeField]
        public float Depth;

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
                new Vector3(0f, 0f, Depth / 2),
                new Vector3(Width / 2, 0f, 0.25f * Depth),
                new Vector3(Width / 2, 0f, -0.25f * Depth),
                
                new Vector3(0f, 0f, - (Depth / 2)),
                new Vector3(-(Width / 2), 0f, -(0.25f * Depth)),
                new Vector3(-(Width / 2), 0f, (0.25f * Depth)),
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
        public override Mesh GetShapeMesh()
        {
            UpdateShape();
            
            Mesh baseMesh = new Mesh();

            baseMesh.vertices = BaseVertices.ToArray();
            baseMesh.uv = BaseUVs.ToArray();
            baseMesh.triangles = BaseTriangles.ToArray();

            ExtensionMethods.SetFullColor(baseMesh, Color.green);

            return baseMesh;
        }
        public override Vector3 GetTesselatedPosition(int x, int y)
        {
            Vector3 position = new Vector3();
            // Calculate the center of each hexagon
            position.x = x * Width + (y % 2) * (Width / 2.0f) + (x * cellGap.x);
            position.y = 0;
            position.z = y * (Depth - Depth / 4.0f) + (y * cellGap.y); ;

            return position;
        }
        public override Vector3 GetTesselatedPosition(Vector2Int gridPosition)
        {
            return GetTesselatedPosition(gridPosition.x, gridPosition.y);
        }
        public override Vector2Int GetGridCoordinate(Vector3 localPosition)
        {
            localPosition.y = 0;

            float x = localPosition.x / Width;
            float z = localPosition.z / Depth;

            int x1 = Mathf.CeilToInt(x);
            int z1 = Mathf.CeilToInt(z);

            return GetClosestGrid(x1, z1);

            Vector2Int GetClosestGrid(int maxX, int maxZ)
            {
                Vector2Int closest = Vector2Int.left;
                float prevDistance = float.MaxValue;
                float distance = -1;

                int count = 2;

                int xMin = Mathf.Max(0, maxX - count);
                int zMin = Mathf.Max(0, maxZ - count);

                for (int x = maxX; x >= xMin; x--)
                {
                    for (int z = maxZ; z >= zMin; z--)
                    {
                        Vector3 pos = GetTesselatedPosition(x, z);
                        distance = Vector3.Distance(pos, localPosition);

                        if (distance < prevDistance)
                        {
                            prevDistance = distance;
                            closest = new Vector2Int(x, z);
                        }
                    }
                }

                // this should never run
                return closest;
            }
        }
    }
}
