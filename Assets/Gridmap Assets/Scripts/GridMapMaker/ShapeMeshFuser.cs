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
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEditor.Progress;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker
{
    /// <summary>
    /// This will add a given mesh and the given offset positions and fuse them into one mesh
    /// </summary>
    public class ShapeMeshFuser
    {
        const int MAX_VERTICES = 65534;

        private Dictionary<int, Vector2Int> shapePositions;
        private Dictionary<int, Color> positionColors;
        private Vector3 positionOffset;

        public List<Vector2Int> InsertedPositions => shapePositions.Values.ToList();
        public Vector3 PositionOffset => positionOffset;

        private List<Vector3> Vertices;
        private List<int> Triangles;
        private List<Vector2> Uvs;
        private List<Color> Colors;

        private MeshData shapeMesh;
        private (int vertexCount, int triangleCount) shapeMeshSize;

        private List<MeshData> finalMeshData = new List<MeshData>();
        private GridShape gridShape;
        public GridShape GridShape => gridShape;
        public int VertexCount { get { return Vertices.Count; } }
        public int TriangleCount { get { return Triangles.Count; } }
        public bool IsEmpty { get { return shapePositions.Count == 0; } }

        private bool pendingUpdate;
        public bool PendingUpdate => pendingUpdate;
        public ShapeMeshFuser(GridShape shape, Vector3 positionOffset = new Vector3())
        {
            gridShape = shape;
            shapeMesh = shape.ShapeMesh;

            this.positionOffset = positionOffset;

            shapeMeshSize = (shapeMesh.vertexCount, shapeMesh.Triangles.Count());

            Vertices = new List<Vector3>();
            Triangles = new List<int>();
            Colors = new List<Color>();
            Uvs = new List<Vector2>();

            shapePositions = new Dictionary<int, Vector2Int>();
            positionColors = new Dictionary<int, Color>();
        }
        public ShapeMeshFuser(GridShape shape, Vector3 positionOffset, List<Vector2Int> gridPositions) :
                            this(shape, positionOffset)
        {
            foreach (Vector2Int item in gridPositions)
            {
                InsertHelper(item.GetHashCode_Unique(), item, Color.white);
            }
        }

        /// <summary>
        /// This will insert a position into the fuser. There is no error checking for duplicates
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="position"></param>
        /// <param name="color"></param>
        private void InsertHelper(int hash, Vector2Int position, Color color)
        {
            shapePositions.Add(hash, position);
            positionColors.Add(hash, color);
            
            pendingUpdate = true;
        }

        private void RemoveHelper(int hash, Vector2Int position)
        {
            shapePositions.Remove(hash);
            positionColors.Remove(hash);

            pendingUpdate = true;
        }

        public void InsertPosition(int hash, Vector2Int position, Color color = default)
        {
            if (shapePositions.ContainsKey(hash))
            {
                return;
            }

            InsertHelper(hash, position, color);
        }

        public void InsertPosition(Vector2Int position, Color color = default)
        {
            int hash = position.GetHashCode_Unique();

            if (shapePositions.ContainsKey(hash))
            {
                return;
            }

            InsertHelper(hash, position, color);
        }

        public void RemovePosition(Vector2Int position)
        {
            int hash = position.GetHashCode_Unique();

            if (shapePositions.ContainsKey(hash))
            {
                RemoveHelper(hash, position);
            }
        }

        public void RemovePosition(int hash)
        {
            if (shapePositions.ContainsKey(hash))
            {
                RemoveHelper(hash, shapePositions[hash]);
            }
        }


        /// <summary>
        /// Will combine the given fuser with this fuser. Note that positions that already exist will be ignored
        /// </summary>
        /// <param name="meshFuser"></param>
        public void CombineFuser(ShapeMeshFuser meshFuser)
        {
            foreach (KeyValuePair<int, Vector2Int> item in meshFuser.shapePositions)
            {
                int hash = item.Value.GetHashCode_Unique();

                if (!shapePositions.ContainsKey(hash))
                {
                    InsertHelper(hash, item.Value, meshFuser.positionColors[hash]);
                }
            }

            pendingUpdate = true;
        }

        /// <summary>
        /// Fuses the mesh. Uses multithreading for faster processing
        /// </summary>
        public void FuseMesh_Fast()
        {
            if (!pendingUpdate)
            {
                return;
            }

            finalMeshData.Clear();
            Vertices.Clear();
            Triangles.Clear();
            Colors.Clear();
            Uvs.Clear();

            int numOfMeshes = Mathf.CeilToInt((shapePositions.Count * shapeMeshSize.vertexCount) /
                (float)MAX_VERTICES);

            int subMeshCount = Mathf.CeilToInt(shapePositions.Count / numOfMeshes);

            List<int> subMeshGroups = new List<int>();
            List<MeshData> mdg = new List<MeshData>();

            int totalCount = 0;

            while(true)
            {
                MeshData md = new MeshData();
                int remaining = shapePositions.Count - totalCount;
                int subMeshSize = (remaining < subMeshCount) ? remaining : subMeshCount;

                int vc = subMeshSize * shapeMeshSize.vertexCount;
                int tc = subMeshSize * shapeMesh.TriangleCount;

                md.Vertices = Enumerable.Repeat(Vector3.zero, vc).ToList();
                md.Uvs = Enumerable.Repeat(Vector2.zero, vc).ToList();
                md.Colors = Enumerable.Repeat(Color.white, vc).ToList();
                md.Triangles = Enumerable.Repeat(0, tc).ToList();

                finalMeshData.Add(md);

                totalCount += subMeshCount;

                if(totalCount >= shapePositions.Count)
                {
                    break;
                }

            }

            List<int> keys = shapePositions.Keys.ToList();

            Parallel.For(0, shapePositions.Count, i =>
            {
                int groupIndex = i / subMeshCount;

                int insertIndex = i % subMeshCount;

                int vi = insertIndex * shapeMeshSize.vertexCount;
                int ti = insertIndex * shapeMeshSize.triangleCount;

                Vector2Int pos = shapePositions[keys[i]];
                Vector3 offset = gridShape.GetTesselatedPosition(pos) - positionOffset;
                Color color = positionColors[keys[i]];

                try
                {
                    for (int j = 0; j < shapeMeshSize.vertexCount; j++)
                    {
                        finalMeshData[groupIndex].Vertices[vi] = shapeMesh.Vertices[j] + offset;
                        finalMeshData[groupIndex].Colors[vi] = color;
                        finalMeshData[groupIndex].Uvs[vi] = shapeMesh.Uvs[j];
                        vi++;
                    }

                    for (int j = 0; j < shapeMeshSize.triangleCount; j++)
                    {
                        finalMeshData[groupIndex].Triangles[ti] = shapeMesh.Triangles[j] + (insertIndex * shapeMeshSize.vertexCount);
                        ti++;
                    }
                }
                catch (Exception)
                {

                    int asdsd = 23;
                }

            });

            Vertices.Clear();
            Triangles.Clear();
            Colors.Clear();
            Uvs.Clear();

            // i have a list of x length, im dividing the list into groups of 3, give list of numbers that denote the end index of each group

            //if (numOfMeshes > 1)
            //{
            //    List<int> subMeshGroups = new List<int>();

            //    for (int i = subMeshCount; i < shapePositions.Count + subMeshCount; i += subMeshCount)
            //    {
            //        finalMeshData.Add(ExtractMeshData(i));
            //    }
            //}
            //else
            //{
            //    finalMeshData.Add(cMesh);
            //}

            //MeshData ExtractMeshData(int i)
            //{
            //    MeshData meshData = new MeshData();
            //    meshData.Vertices = cMesh.Vertices.GetRange(i * shapeMeshSize.vertexCount, subMeshCount * shapeMeshSize.vertexCount);

            //    meshData.Uvs = cMesh.Uvs.GetRange(i * shapeMeshSize.vertexCount, subMeshCount * shapeMeshSize.vertexCount);

            //    meshData.Colors = cMesh.Colors.GetRange(i * shapeMeshSize.vertexCount, subMeshCount * shapeMeshSize.vertexCount);

            //    meshData.Triangles = cMesh.Triangles.GetRange(i * shapeMeshSize.triangleCount, subMeshCount * shapeMeshSize.triangleCount);

            //    return meshData;
            //}

            /*
            foreach (int key in shapePositions.Keys)
            {
                Vector2Int pos = shapePositions[key];
                Vector3 offset = gridShape.GetBaseTesselatedPosition(pos) - positionOffset;
                Color color = positionColors[key];
                
                for (int j = 0; j < shapeMeshSize.vertexCount; j++)
                {
                    Vertices.Add(shapeMesh.Vertices[j] + offset);
                    Colors.Add(color);
                    Uvs.Add(shapeMesh.Uvs[j]);
                }

                for (int j = 0; j < shapeMeshSize.triangleCount; j++)
                {
                    Triangles.Add(shapeMesh.Triangles[j] + (subMeshGroup * shapeMeshSize.vertexCount));
                }

                subMeshGroup++;

                if (subMeshGroup == subMeshCount)
                {
                    meshData.Vertices = Vertices;
                    meshData.Colors  = Colors;
                    meshData.Uvs = Uvs;
                    meshData.Triangles = Triangles;

                    finalMeshData.Add(meshData);

                    meshData = new MeshData();

                    Vertices.Clear();
                    Triangles.Clear();
                    Colors.Clear();
                    Uvs.Clear();

                    subMeshCount += subMeshCount;
                    subMeshGroup = 0;

                    subMeshCount = Mathf.Min(shapePositions.Count - 1, subMeshCount);
                }
            }
            */

            pendingUpdate = false;
        }
        public void FuseMesh()
        {
            if (!pendingUpdate)
            {
                return;
            }

            finalMeshData.Clear();
            Vertices.Clear();
            Triangles.Clear();
            Colors.Clear();
            Uvs.Clear();

            int numOfMeshes = Mathf.CeilToInt((shapePositions.Count * shapeMeshSize.vertexCount) /
                (float)MAX_VERTICES);

            int subMeshCount = shapePositions.Count / numOfMeshes;

            int subMeshGroup = 0;

            MeshData meshData = new MeshData();

            foreach (int key in shapePositions.Keys)
            {
                Vector2Int pos = shapePositions[key];
                Vector3 offset = gridShape.GetTesselatedPosition(pos) - positionOffset;
                Color color = positionColors[key];

                for (int j = 0; j < shapeMeshSize.vertexCount; j++)
                {
                    Vertices.Add(shapeMesh.Vertices[j] + offset);
                    Colors.Add(color);
                    Uvs.Add(shapeMesh.Uvs[j]);
                }

                for (int j = 0; j < shapeMeshSize.triangleCount; j++)
                {
                    Triangles.Add(shapeMesh.Triangles[j] + (subMeshGroup * shapeMeshSize.vertexCount));
                }

                subMeshGroup++;

                if (subMeshGroup == subMeshCount)
                {
                    meshData.Vertices = Vertices;
                    meshData.Colors = Colors;
                    meshData.Uvs = Uvs;
                    meshData.Triangles = Triangles;

                    finalMeshData.Add(meshData);

                    meshData = new MeshData();

                    Vertices.Clear();
                    Triangles.Clear();
                    Colors.Clear();
                    Uvs.Clear();

                    subMeshCount += subMeshCount;

                    subMeshCount = Mathf.Min(shapePositions.Count - 1, subMeshCount);
                }
            }

            pendingUpdate = false;
        }

        public List<MeshData> GetFusedMeshes()
        {
            return finalMeshData;
        }
        public void Clear()
        {
            shapePositions.Clear();
            positionColors.Clear();
            InsertedPositions.Clear();
            Vertices.Clear();
            Triangles.Clear();
            Colors.Clear();
            Uvs.Clear();

            pendingUpdate = true;
        }
    }
}
