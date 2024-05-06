using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using Assets.Gridmap_Assets.Scripts.Miscellaneous;
using Assets.Scripts.GridMapMaker;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker
{
    /// <summary>
    /// This will add a given mesh and the given offset positions and fuse them into one mesh
    /// </summary>
    public class ShapeMeshFuser
    {
        const int MAX_VERTICES = 65534;

        private Dictionary<int, Vector2Int> shapePositions;
        private Vector3 positionOffset;

        public Vector3 PositionOffset => positionOffset;

        private List<Vector3> Vertices;
        private List<int> Triangles;
        private List<Vector2> UVs;
        private List<Color> Colors;

        private Mesh shapeMesh;
        private (int vertexCount, int triangleCount) shapeMeshSize;

        private List<Mesh> finalMeshes = new List<Mesh>();
        private GridShape gridShape;
        public GridShape GridShape => gridShape;
        public int VertexCount { get { return Vertices.Count; } }
        public int TriangleCount { get { return Triangles.Count; } }
        public bool IsEmpty { get { return shapePositions.Count == 0; } }

        private bool pendingUpdate;
        public bool PendingUpdate => pendingUpdate;
        public ShapeMeshFuser(GridShape shape, Vector3 positionOffset = new Vector3())
        {
            shapeMesh = shape.GetShapeMesh();
            gridShape = shape;

            this.positionOffset = positionOffset;

            shapeMeshSize = (shapeMesh.vertexCount, shapeMesh.triangles.Count());

            Vertices = new List<Vector3>();
            Triangles = new List<int>();
            Colors = new List<Color>();
            UVs = new List<Vector2>();

            shapePositions = new Dictionary<int, Vector2Int>();
        }
        public ShapeMeshFuser(GridShape shape, Vector3 positionOffset, List<Vector2Int> gridPositions): 
                            this(shape, positionOffset)
        {
            foreach (Vector2Int item in gridPositions)
            {
                shapePositions.Add(item.GetHashCode_Unique(), item);
            }
        }

        public void InsertPosition(Vector2Int position)
        {
            int hash = position.GetHashCode_Unique();

            if(shapePositions.ContainsKey(hash))
            {
                return;
            }

            shapePositions.Add(hash, position);
            pendingUpdate = true;


        }

        public void RemovePosition(Vector2Int position)
        {
            int hash = position.GetHashCode_Unique();

            if (shapePositions.ContainsKey(hash))
            {
                shapePositions.Remove(hash);
                pendingUpdate = true;
            }
        }

        public void UpdateMesh()
        {
            if (!pendingUpdate)
            {
                return;
            }

            finalMeshes.Clear();
            Vertices.Clear();
            Triangles.Clear();
            Colors.Clear();
            UVs.Clear();

            int numOfMeshes = Mathf.CeilToInt((shapePositions.Count * shapeMeshSize.vertexCount) /
                (float)MAX_VERTICES);

            int subMeshCount = shapePositions.Count / numOfMeshes;

            int subMeshIndex = 0;

            Mesh meshData = new Mesh();

            foreach(int key in shapePositions.Keys)
            {
                Vector2Int pos = shapePositions[key];
                Vector3 offset = gridShape.GetTesselatedPosition(pos) - positionOffset;

                for (int j = 0; j < shapeMeshSize.vertexCount; j++)
                {
                    Vertices.Add(shapeMesh.vertices[j] + offset);
                    Colors.Add(shapeMesh.colors[j]);
                    UVs.Add(shapeMesh.uv[j]);
                }

                for (int j = 0; j < shapeMeshSize.triangleCount; j++)
                {
                    Triangles.Add(shapeMesh.triangles[j] + (subMeshIndex * shapeMeshSize.vertexCount));
                }

                subMeshIndex++;

                if (subMeshIndex == subMeshCount)
                {
                    meshData.vertices = Vertices.ToArray();
                    meshData.colors = Colors.ToArray();
                    meshData.uv = UVs.ToArray();
                    meshData.triangles = Triangles.ToArray();

                    finalMeshes.Add(meshData);

                    meshData = new Mesh();
                    
                    Vertices.Clear();
                    Triangles.Clear();
                    Colors.Clear();
                    UVs.Clear();

                    subMeshCount += subMeshCount;
                    subMeshIndex = 0;

                    subMeshCount = Mathf.Min(shapePositions.Count - 1, subMeshCount);
                }
            }

            pendingUpdate = false;
        }

        public List<Mesh> GetAllMeshes()
        {
            return finalMeshes;
        }
        public void Clear()
        {
            shapePositions.Clear();
            pendingUpdate = true;
        }

        public struct MeshData
        {
            public List<Vector3> vertices;
            public List<int> triangles;
            public List<Color> colors;
            public List<Vector2> uvs;

            public int VertexCount { get { return vertices.Count; } }
            public int TriangleCount { get { return triangles.Count; } }

            public MeshData(Mesh data)
            {
                vertices = new List<Vector3>();
                triangles = new List<int>();
                colors = new List<Color>();
                uvs = new List<Vector2>();

                vertices.AddRange(data.vertices);
                triangles.AddRange(data.triangles);
                colors.AddRange(data.colors);
                uvs.AddRange(data.uv);
            }
        }
    }
}
