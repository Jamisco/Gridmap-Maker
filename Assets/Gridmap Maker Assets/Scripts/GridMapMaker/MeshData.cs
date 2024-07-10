using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GridMapMaker
{
    /// <summary>
    /// A simple struct used to hold mesh data. This is used because the default Mesh class is not thread safe and can only be accessed from the main thread.
    /// </summary>
    public struct MeshData
    {
        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Color> colors;
        private List<Vector2> uvs;

        public List<Vector3> Vertices
        {
            get => vertices;
            set => vertices = new List<Vector3>(value);
        }
        public List<int> Triangles
        {
            get => triangles;
            set => triangles = new List<int>(value);
        }
        public List<Color> Colors
        {
            get => colors;
            set => colors = new List<Color>(value);
        }
        public List<Vector2> Uvs
        {
            get => uvs;
            set => uvs = new List<Vector2>(value);
        }

        public int vertexCount { get { return Vertices.Count; } }
        public int TriangleCount { get { return Triangles.Count; } }

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

        public Mesh GetMesh()
        {
            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(Colors);
            mesh.SetUVs(0, uvs);

            return mesh;
        }
    }

    /// <summary>
    /// This is used to conduct multithreaded operations on a mesh data.
    /// </summary>
    public struct ConcurrentMeshData
    {
        private ConcurrentBag<Vector3> vertices;
        private ConcurrentBag<int> triangles;
        private ConcurrentBag<Color> colors;
        private ConcurrentBag<Vector2> uvs;

        public ConcurrentBag<Vector3> Vertices
        {
            get => vertices;
            set => vertices = new ConcurrentBag<Vector3>(value);
        }
        public ConcurrentBag<int> Triangles
        {
            get => triangles;
            set => triangles = new ConcurrentBag<int>(value);
        }
        public ConcurrentBag<Color> Colors
        {
            get => colors;
            set => colors = new ConcurrentBag<Color>(value);
        }
        public ConcurrentBag<Vector2> Uvs
        {
            get => uvs;
            set => uvs = new ConcurrentBag<Vector2>(value);
        }

        public int vertexCount { get { return Vertices.Count; } }
        public int TriangleCount { get { return Triangles.Count; } }

        public void Init()
        {
            vertices = new ConcurrentBag<Vector3>();
            triangles = new ConcurrentBag<int>();
            colors = new ConcurrentBag<Color>();
            uvs = new ConcurrentBag<Vector2>();
        }
        public Mesh GetMesh()
        {
            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices.ToList());
            mesh.SetTriangles(triangles.ToList(), 0);
            mesh.SetColors(Colors.ToList());
            mesh.SetUVs(0, uvs.ToList());

            return mesh;
        }

        public MeshData GetMeshData()
        {
            MeshData meshData = new MeshData();
            meshData.Vertices = vertices.ToList();
            meshData.Triangles = triangles.ToList();
            meshData.Colors = Colors.ToList();
            meshData.Uvs = uvs.ToList();

            return meshData;
        }
    }
}
