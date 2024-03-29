﻿using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Assets.Scripts.GridMapMaker
{
    public class FusedMesh
    {
        // A fused mesh is a collection of meshes that are fused together to make one mesh.

        private List<int> MeshHashes;
        // vertex and triangle size
        private List<(int vertexCount, int triangleCount)> MeshSizes;

        private List<Vector3> Vertices;
        private List<int> Triangles;
        private List<Vector2> UVs;
        private List<Color> Colors;

        public int VertexCount { get { return Vertices.Count; } }
        public int TriangleCount { get { return Triangles.Count; } }

        public Mesh Mesh;


        private void Init()
        {
            MeshHashes = new List<int>();
            MeshSizes = new List<(int, int)>();

            Vertices = new List<Vector3>();
            Triangles = new List<int>();
            Colors = new List<Color>();
            UVs = new List<Vector2>();

            Mesh = new Mesh();
            Mesh.MarkDynamic();
        }
        public FusedMesh()
        {
            Init();
        }

        public FusedMesh(SerializedFusedMesh serializedFusedMesh)
        {
            MeshHashes = serializedFusedMesh.MeshHashes;
            MeshSizes = serializedFusedMesh.MeshSizes;

            Vertices = serializedFusedMesh.Vertices;
            Triangles = serializedFusedMesh.Triangles;
            Colors = serializedFusedMesh.Colors;
            UVs = serializedFusedMesh.UVs;

            Mesh = new Mesh();
            Mesh.MarkDynamic();
            UpdateMesh();
        }

        public FusedMesh(List<Mesh> meshes, List<int> hashes, List<Vector3> offsets)
        {
            Init();

            if (!(meshes.Count == hashes.Count && meshes.Count == offsets.Count))
            {
                throw new Exception("List must be thesame size");
            }

            for (int i = 0; i < meshes.Count; i++)
            {
                AddMesh_NoUpdate(meshes[i], hashes[i], offsets[i]);
            }

            UpdateMesh();
        }

        /// <summary>
        /// This constructors uses multithreading to fuse the meshes. This is faster but can cause problems when the meshes are not properly indexed. Make sure your arrays are in order
        /// </summary>
        /// <param name="meshes"></param>
        /// <param name="hashes"></param>
        /// <param name="offsets"></param>
        /// <param name="vertTriIndex"></param>
        /// <param name="totalCounts"></param>
        /// <exception cref="Exception"></exception>
        public FusedMesh(List<MeshData> meshes, List<int> hashes, List<Vector3> offsets,
                         List<(int vertCount, int triStart)> vertTriIndex,
                            (int totalVerts, int triStart) totalCounts)
        {
            Init();

            if (!(meshes.Count == hashes.Count && meshes.Count == offsets.Count
                                               && meshes.Count == vertTriIndex.Count))
            {
                throw new Exception("List must be thesame size");
            }

            FillList();


            bool hasColors = true;
            
            Parallel.For(0, meshes.Count, i =>
            {
                InsertMesh_NoUpdate(meshes[i], hashes[i], offsets[i], vertTriIndex[i], i);
            });
            
            void InsertMesh_NoUpdate(MeshData mesh, int hash, Vector3 offset,
                                    (int vertexCount, int triStart) counts, int index)
            {
                MeshHashes[index] = hash;
                MeshSizes[index] = (mesh.VertexCount, mesh.TriangleCount);

                List<Vector3> hexVertices = new List<Vector3>();
                List<int> hexTris = new List<int>();

                foreach (Vector3 v in mesh.vertices)
                {
                    hexVertices.Add(v + offset);
                }

                foreach (int tri in mesh.triangles)
                {
                    hexTris.Add(tri + counts.vertexCount);
                }


                // we check if the mesh has colors, if it does we add them to the list
                // since colors and vertex count MUST match, we simply check if the count is the same
                // The reason we clear the colors array is because if one mesh has colors and the other doesn't the mesh will be invalid because color count and vertex count must match. Thus, ALl meshes must either have a color or not have one
                if (mesh.colors.Count != hexVertices.Count && hasColors == true)
                {
                    hasColors = false;
                    Colors.Clear();
                }
                            
                int x = 0;
                int start = counts.vertexCount;
                for (int i = start; i < start + hexVertices.Count; i++)
                {
                    Vertices[i] = hexVertices[x];

                    // there might exist an error here if the mesh we are fusing all have colors, but one mesh doesnt have colors. A thread issue my occur where we are tring to access the i index of colors array but it got cleared...this is rare but it might happen
                    if (hasColors)
                    {
                        Colors[i] = mesh.colors[x];
                    }

                    UVs[i] = mesh.uvs[x];

                    x++;

                }

                x = 0;
                for (int i = counts.triStart; i < counts.triStart + hexTris.Count; i++)
                {
                    Triangles[i] = hexTris[x];
                    x++;
                }
            }

            void FillList()
            {
                for (int i = 0; i < meshes.Count; i++)
                {
                    MeshSizes.Add((0, 0));
                    MeshHashes.Add(0);
                }

                for (int i = 0; i < totalCounts.totalVerts; i++)
                {
                    Vertices.Add(Vector3.zero);
                    Colors.Add(Color.white);
                    UVs.Add(Vector2.zero);
                }

                for (int i = 0; i < totalCounts.triStart; i++)
                {
                    Triangles.Add(0);
                }
            }

            UpdateMesh();
        }

        public void CombineFusedMesh(FusedMesh fusedMesh)
        {
            if (fusedMesh == null)
            {
                throw new Exception("Fused mesh is null");
            }

            if (fusedMesh.MeshSizes.Count == 0)
            {
                throw new Exception("Fused mesh is empty");
            }

            for (int i = 0; i < fusedMesh.MeshSizes.Count; i++)
            {
                AddMesh_NoUpdate(fusedMesh.Mesh, fusedMesh.MeshHashes[i], Vector3.zero);
            }

            UpdateMesh();
        }
        private void AddMesh_NoUpdate(Mesh mesh, int hash, Vector3 offset)
        {
            int index = MeshHashes.IndexOf(hash);

            if (index != -1)
            {
                //Debug.Log("Mesh already exists..Replacing. Offset: " + offset);
                RemoveMesh(hash, index);
            }

            AddToList(hash, mesh.vertexCount, mesh.triangles.Length);

            AddMeshAtEnd(mesh, offset);
        }

        /// <summary>
        /// Gets the start location of the vertices and triangles of a particular mesh. The index will be the index of the hex hash
        /// </summary>
        /// <param name="index">ndex of the hex hash</param>
        /// <returns></returns>
        private (int vertIndex, int triIndex) GetMeshIndices(int index)
        {
            if (index != -1)
            {
                var size = MeshSizes[index];

                int triIndex = 0;
                int vertIndex = 0;

                for (int i = 0; i < index; i++)
                {
                    triIndex += MeshSizes[i].triangleCount;
                    vertIndex += MeshSizes[i].vertexCount;
                }

                return (vertIndex, triIndex);
            }

            return (-1, -1);
        }
        
        /// <summary>
        /// Returns true or false if mesh was successfully removed
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        private bool RemoveMesh_NoUpdate(int hash, int position = -1)
        {
            int index = position == -1 ? MeshHashes.IndexOf(hash) : position;

            if(index == -1)
            {
                return false;
            }

            var size = MeshSizes[index];

            (int vert, int tri) indices = GetMeshIndices(index);

            if(indices.vert == -1 || indices.tri == -1)
            {
                return false;
            }

            try
            {
                Exception e = new Exception("Error when removing mesh");

                // error might occur if some of the below list are empty.
                // this might be because they were never filled to begin with
                Vertices.TryRemoveElementsInRange(indices.vert, 
                                                size.vertexCount, out e);

                Triangles.TryRemoveElementsInRange(indices.tri, 
                                                    size.triangleCount, out e);

                if (Colors.Count > 0)
                {
                    Colors.TryRemoveElementsInRange(indices.vert,
                                                     size.vertexCount, out e);
                }

                if (UVs.Count > 0)
                {
                    UVs.TryRemoveElementsInRange(indices.vert, size.vertexCount, out e);
                }
            }
            catch (Exception ex)
            {
                return false;
            }

            RemoveFromList(index);

            RecalculateTriangles(-size.vertexCount, indices.tri);

            return true;
        }

        /// <summary>
        /// Fused the given mesh into the current one. Be advised that if you are adding a a mesh with a hash that already exists, the old mesh will be removed and the new one will be added
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="hash"></param>
        /// <param name="offset"></param>
        public void InsertMesh(Mesh mesh, int hash, Vector3 offset)
        {
            AddMesh_NoUpdate(mesh, hash, offset);

            UpdateMesh();
        }
        /// <summary>
        /// Returns true or false if mesh was successfully removed
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="position">The position you want to StartPosition searching from </param>
        /// <returns></returns>
        public bool RemoveMesh(int hash, int position = -1)
        {
            bool removed = RemoveMesh_NoUpdate(hash, position);

            if (removed)
            {
                UpdateMesh();
            }

            return removed;
        }

        private void AddToList(int hash, int vertexCount, int triangleCount)
        {
            MeshHashes.Add(hash);
            MeshSizes.Add((vertexCount, triangleCount));
        }
        private void RemoveFromList(int index)
        {
            MeshHashes.RemoveAt(index);
            MeshSizes.RemoveAt(index);
        }
        private void AddMeshAtEnd(Mesh aMesh, Vector3 offset)
        {
            List<Vector3> hexVertices = new List<Vector3>();
            List<int> hexTris = new List<int>();

            foreach (Vector3 v in aMesh.vertices)
            {
                hexVertices.Add(v + offset);
            }

            foreach (int tri in aMesh.triangles)
            {
                hexTris.Add(tri + Vertices.Count);
            }

            Vertices.AddRange(hexVertices);
            Triangles.AddRange(hexTris);
            Colors.AddRange(aMesh.colors);
            UVs.AddRange(aMesh.uv);
        }
        /// <summary>
        /// When you modify the triangles, you now also have to recalculate the triangles that came after it, such that the index are proper
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="startIndex"></param>
        private void RecalculateTriangles(int offset, int startIndex = 0)
        {
            for (int i = startIndex; i < Triangles.Count; i++)
            {
                Triangles[i] += offset;
            }
        }

        public bool HasMesh(int hash)
        {
            return MeshHashes.IndexOf(hash) == -1 ? false : true;
        }

        /// <summary>
        /// Returns the a NEW mesh with the given hash. Returns null if no mesh was found. Modifying this mesh has no effect on the current fused mesh. If you want to modify the BaseFushMesh, after you have modified the returned mesh, you must call RemoveMesh and then InsertMesh again. Be advised, depending on the size of the fused mesh, this could be a costly operation.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public Mesh GetMesh(int hash)
        {
            int index = MeshHashes.IndexOf(hash);

            if (index != -1)
            {
                Mesh mesh = new Mesh();

                (int vert, int tri) indices = GetMeshIndices(index);

                var size = MeshSizes[index];

                mesh.vertices = Vertices.GetRange(indices.vert,                 size.vertexCount).ToArray();

                // remember that triangles vertices are increased by the current index of vertex array. Hence when retrieving them, we must account for that offset by subtracting said index
                int[] triangles = Triangles.GetRange(indices.tri, size.triangleCount).ToArray();

                for (int i = 0; i < triangles.Length; i++)
                {
                    triangles[i] -= indices.vert;
                }

                mesh.triangles = triangles;

                if (Colors.Count > 0)
                {
                    mesh.colors = Colors.GetRange(indices.vert,
                                size.vertexCount).ToArray();
                }

                if(UVs.Count > 0)
                {
                    mesh.uv = UVs.GetRange(indices.vert, size.vertexCount).ToArray();
                }

                return mesh;
            }

            return null;
        }

        public Mesh GetMesh()
        {
            Mesh mesh = new Mesh();

            mesh.vertices = Vertices.ToArray();

            mesh.triangles = Triangles.ToArray();
            mesh.colors = Colors.ToArray();
            mesh.uv = UVs.ToArray();

            return mesh;
        }

        private void UpdateMesh()
        {
            //It is important to call Clear before assigning new vertices or triangles. Unity always checks the supplied triangle indices whether they don't reference out of bounds vertices. Calling Clear then assigning vertices then triangles makes sure you never have out of bounds data.

            Mesh.Clear();

            Mesh.vertices = Vertices.ToArray();
            Mesh.triangles = Triangles.ToArray();
            Mesh.colors = Colors.ToArray();
            Mesh.uv = UVs.ToArray();
        }

        public static implicit operator Mesh(FusedMesh f)
        {
            return f.Mesh;
        }
        public static implicit operator SerializedFusedMesh(FusedMesh f)
        {
            return new SerializedFusedMesh(f);
        }

        public static Mesh CombineToSubmesh(List<FusedMesh> subMesh)
        {
            Mesh newMesh = new Mesh();
            
            CombineInstance[] tempArray = new CombineInstance[subMesh.Count];

            for (int i = 0; i < subMesh.Count; i++)
            {
                CombineInstance subInstance = new CombineInstance();

                subInstance.mesh = subMesh[i];

                tempArray[i] = subInstance;
            }

            newMesh.CombineMeshes(tempArray, false, false);

            return newMesh;
        }

        public static Mesh CombineToSubmesh(List<Mesh> subMesh)
        {
            Mesh newMesh = new Mesh();

            CombineInstance[] tempArray = new CombineInstance[subMesh.Count];

            for (int i = 0; i < subMesh.Count; i++)
            {
                CombineInstance subInstance = new CombineInstance();

                subInstance.mesh = subMesh[i];

                tempArray[i] = subInstance;
            }

            newMesh.CombineMeshes(tempArray, false, false);

            return newMesh;
        }

        public Mesh CombineToSubmesh(Mesh subMesh)
        {
            Mesh newMesh = new Mesh();

            newMesh = Mesh;

            CombineInstance subInstance = new CombineInstance();

            subInstance.mesh = subMesh;

            CombineInstance[] tempArray = new CombineInstance[0];

            newMesh.CombineMeshes(tempArray);

            return newMesh;
        }

        public void ClearFusedMesh()
        {
            MeshHashes?.Clear();
            MeshSizes?.Clear();

            Vertices.Clear();
            Triangles.Clear();
            Colors.Clear();
            UVs.Clear();

            UpdateMesh();
        }

        // create a struct to hold MeshData

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

        [Serializable]
        public struct SerializedFusedMesh
        {
            public List<(int vertexCount, int triangleCount)> MeshSizes { get; set; }
            public List<int> MeshHashes { get; set; }

            public List<Vector3> Vertices { get; set; }
            public List<int> Triangles { get; set; }
            public List<Vector2> UVs { get; set; }
            public List<Color> Colors { get; set; }

            public SerializedFusedMesh(FusedMesh fusedMesh)
            {
                MeshHashes = fusedMesh.MeshHashes;
                MeshSizes = fusedMesh.MeshSizes;
                Vertices = fusedMesh.Vertices;
                Triangles = fusedMesh.Triangles;
                UVs = fusedMesh.UVs;
                Colors = fusedMesh.Colors;
                
            }
            public FusedMesh Deserialize()
            {
                return new FusedMesh(this);
            }

            public static implicit operator FusedMesh(SerializedFusedMesh f)
            {
                return f.Deserialize();
            }
        }
    }
}
