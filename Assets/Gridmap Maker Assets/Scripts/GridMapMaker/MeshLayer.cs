using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GridMapMaker.ShapeVisualData;
using static UnityEditor.Progress;
using static GridMapMaker.EdgeHelpers;




#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GridMapMaker
{
    /// <summary>
    /// A layered mesh is a collection of fused meshes that are then combined together to make one mesh.
    /// Each fused mesh is unique visually, meaning it may have its own different texture, color, etc.          However each fused mesh has thesame Shape.
    ///  5 meshes with 5 materials means 5 game objects to update, 5 renderer components
    ///  1 mesh with 5 materials means 1 game object to update, 1 renderer component

    ///  The benefit of 5 meshes is that each part can be separately culled if necessary, potentially           reducing draw calls.
    ///  The benefit of 1 mesh is less CPU and GPU overhead.
    /// </summary>
    /// 
    [RequireComponent(typeof(LayerHandle))]
    [Serializable]
    public class MeshLayer : MonoBehaviour
    {
        const int MAX_VERTICES = 65534;
        public GridChunk gridChunk;
        public bool UseVisualEquality
        {
            get
            {
                return visualDataComparer.UseVisualHash;
            }
            set
            {
                if (visualDataComparer.UseVisualHash != value)
                {
                    visualDataComparer.UseVisualHash = value;
                    VisualEqualityChanged();
                }
            }
        }

        private GridShape layerGridShape;
        public GridShape LayerGridShape
        {
            get
            {
                return layerGridShape;
            }
            set
            {
                if (layerGridShape != value)
                {
                    layerGridShape = value;

                    foreach (ShapeMeshFuser fuser in MaterialVisualGroup.Values)
                    {
                        fuser.GridShape = layerGridShape;
                    }

                    ColorVisualGroup.GridShape = layerGridShape;
                    DrawLayer();
                }
            }
        }

        private VisualDataComparer visualDataComparer = new VisualDataComparer();

        /// <summary>
        // The grouping of visual datas that are used to make a mesh.
        // So cells that have "equal" visualData will be group here and drawn as one
        /// </summary>
        private Dictionary<ShapeVisualData, ShapeMeshFuser> MaterialVisualGroup;
        private ShapeMeshFuser ColorVisualGroup;

        /// <summary>
        /// The number of unique visuals in this mesh group
        /// </summary>
        public int VisualDataCount
        {
            get
            {
                int mc = (MaterialVisualGroup != null) ? MaterialVisualGroup.Count : 0;
                int cc = (!ColorVisualGroup.IsEmpty) ? 1 : 0;

                return mc + cc;
            }
        }
        /// <summary>
        /// The original visualData used for each cell. We store this so we can redraw the mesh if need be
        /// </summary>
        public Dictionary<int, ShapeVisualData> CellVisualDatas
                          = new Dictionary<int, ShapeVisualData>();

        public Dictionary<int, Vector2Int> CellGridPositions = new Dictionary<int, Vector2Int>();

        public Bounds LayerBounds
        {
            get
            {
                Bounds bounds = new Bounds();

                foreach (GameObject item in layerMeshObjs)
                {
                    MeshFilter mf = item.GetComponent<MeshFilter>();
                    if (mf != null)
                    {
                        bounds.Encapsulate(mf.sharedMesh.bounds);
                    }
                }

                return bounds;
            }
        }

        public List<Mesh> GetMeshes()
        {
            List<Mesh> meshes = new List<Mesh>();

            foreach (GameObject item in layerMeshObjs)
            {
                MeshFilter mf = item.GetComponent<MeshFilter>();
                meshes.Add(mf.sharedMesh);
            }

            return meshes;
        }

        public Mesh FullMesh
        {
            get
            {
                return ShapeMeshFuser.CombineToSubmesh(GetMeshes());
            }
        }

       
        /// <summary>
        /// This is use to offset the tesselated position so that the Shape is drawn with reference to the chunk. 
        /// For example, say we are drawing a cell at gridPosition 5, 0 and its tesselated position is (5, 0). if the chunk starts at gridPosition (3, 0), then the cell we are drawing will be the 3rd cell in the chunk. So we need to offset the tesselated position by 3 cells so that the cell is drawn at the correct position in the chunk.
        /// </summary>
        private Vector3 chunkOffset;

        private string layerId;
        public string LayerId
        {
            get => layerId;
        }


        private void OnValidate()
        {
            //ColorVisualGroup.InsertPosition(0, Vector2Int.zero, Color.white);
        }

        /// <summary>
        /// Will move/reposition the mesh layer by a specified offset along the specified axis. This is used to draw mesh layers on top of each other.
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="offset"></param>
        public void SortLayer(SortAxis axis, float offset)
        {
            Vector3 pos = transform.localPosition;

            // (transform.forward.z > 0 ? 1 : -1);
            // The reason we have this is because the mesh might be facing the opposite direction, so we need to adjust the offset accordingly

            switch (axis)
            {
                case SortAxis.X:
                    pos.x += offset;
                    break;
                case SortAxis.Y:
                    pos.y += offset;
                    break;
                case SortAxis.Z:
                    pos.z += offset;
                    break;
                default:
                    break;
            }

            transform.localPosition = pos;
        }
        public void Initialize(MeshLayerSettings layerInfo, GridChunk chunk)
        {
            Clear();

            gridChunk = chunk;
            layerGridShape = layerInfo.Shape;

            layerId = layerInfo.LayerId;

            gameObject.name = layerId;

            chunkOffset = layerGridShape.GetTesselatedPosition(chunk.StartPosition);

            visualDataComparer.UseVisualHash = layerInfo.UseVisualEquality;

            MaterialVisualGroup = new Dictionary<ShapeVisualData, ShapeMeshFuser>(visualDataComparer);

            ColorVisualGroup = new ShapeMeshFuser(layerGridShape, chunkOffset);

        }
        private void ValidateAllSettings()
        {
            chunkOffset = layerGridShape.GetTesselatedPosition(gridChunk.StartPosition);
        }

        public void InsertVisualData(Vector2Int gridPosition,
                                     ShapeVisualData visualProp)
        {
            // every time we insert a visual vData, we must check if the grid position already has a visual vData, if it does, we must remove it because we might have to assign a new visual Data to it..thus moving said grid mesh to a new fused mesh

            //TimeLogger.StartTimer(2313, "InsertPosition");

            int hash = gridPosition.GetHashCode_Unique();
            // 36% of the time is spent here
            if (reInsertMode == false)
            {
                // will remove the position if it exists. So we dont have to check if the gridPositions exist before deleting
                RemoveVisualData(gridPosition);

                CellVisualDatas.Add(hash, visualProp);
                CellGridPositions.Add(hash, gridPosition);
            }

            // Mesh that draw only their colors are inserted into 1 group.
            // This is because we can draw all the color meshes in one draw call by simply painting the mesh vertices themselves
            if (visualProp.DataRenderMode == ShapeVisualData.RenderMode.MeshColor)
            {
                ColorVisualGroup.InsertPosition(hash, gridPosition, visualProp.VisualColor);
                return;
            }

            // 10.2% of the time is spent here
            ShapeMeshFuser meshFuser = null;

            bool success = MaterialVisualGroup.TryGetValue(visualProp, out meshFuser);
            
            // doesnt already exist
            if(!success)
            {
                visualProp.ValidateVisualHash();
            }

            if (meshFuser == null)
            {
                // 13.5% of the time is spent here
                meshFuser = new ShapeMeshFuser(LayerGridShape, chunkOffset);

                MaterialVisualGroup.Add(visualProp, meshFuser);
            }

            // 20% of the time is spent here
            meshFuser.InsertPosition(hash, gridPosition);

            //TimeLogger.StopTimer(2313);
        }

        public void RemoveVisualData(Vector2Int gridPosition)
        {
            int hash = gridPosition.GetHashCode_Unique();
            ShapeVisualData existing = null;

            CellVisualDatas.TryGetValue(hash, out existing);

            if (existing != null)
            {
                ShapeMeshFuser mf = null;

                bool isMat = MaterialVisualGroup.TryGetValue(existing, out mf);

                mf = (mf == null) ? ColorVisualGroup : mf;

                mf.RemovePosition(gridPosition);

                if (mf.IsEmpty && isMat)
                {
                    MaterialVisualGroup.Remove(existing);
                }

                CellVisualDatas.Remove(hash);
                CellGridPositions.Remove(hash);
            }
        }

        /// <summary>
        /// Removes all visual data from the layer. This effectively clears the layer and removes all meshes associated with it. You can use this to reset the layer and then re-insert visual data without the need for reinitializing the layer.
        /// </summary>
        public void RemoveAllVisualData()
        {
            if (MaterialVisualGroup != null)
            {
                foreach (ShapeMeshFuser item in MaterialVisualGroup.Values)
                {
                    item.Clear();
                }
            }

            foreach (GameObject item in layerMeshObjs)
            {
                MeshFilter mf = item.GetComponent<MeshFilter>();

#if UNITY_EDITOR

                DestroyImmediate(mf.sharedMesh);
                DestroyImmediate(item);
#else
                Destroy(mf.sharedMesh);
                Destroy(item);
#endif
            }

            layerMeshObjs.Clear();
            MaterialVisualGroup.Clear();
            CellVisualDatas.Clear();
            ColorVisualGroup.Clear();
            CellGridPositions.Clear();
        }


        public bool HasVisualData(Vector2Int gridPosition)
        {
            return CellVisualDatas.ContainsKey(gridPosition.GetHashCode_Unique());
        }

        public ShapeVisualData GetVisualData(Vector2Int gridPosition)
        {
            ShapeVisualData existing = null;

            CellVisualDatas.TryGetValue(gridPosition.GetHashCode_Unique(), out existing);

            return existing;
        }

        private List<GameObject> layerMeshObjs = new List<GameObject>();
        public void FusedMeshGroups()
        {
            //TimeLogger.StartTimer(1516, "FusedMeshGroups");
            List<SmallMesh> smallMeshes = new List<SmallMesh>();

            bool useMultiThread = gridChunk.GridManager.UseMultithreading;

            foreach (ShapeVisualData vData in MaterialVisualGroup.Keys)
            {
                ShapeMeshFuser m;

                try
                {
                    m = MaterialVisualGroup[vData];
                }
                catch (Exception)
                {
                    m = null;
                }

                List<MeshData> tempMeshes;

                if (useMultiThread)
                {
                    tempMeshes = m.GetFuseMesh_Fast();
                }
                else
                {
                    tempMeshes = m.GetFuseMesh();
                }

                for (int i = 0; i < tempMeshes.Count; i++)
                {
                    smallMeshes.Add(new SmallMesh(vData, tempMeshes[i]));
                }
            }

            // Color Visual Group

            List<MeshData> colorMeshes;

            if (useMultiThread)
            {
                colorMeshes = ColorVisualGroup.GetFuseMesh_Fast();
            }
            else
            {
                colorMeshes = ColorVisualGroup.GetFuseMesh();
            }

            for (int i = 0; i < colorMeshes.Count; i++)
            {
                smallMeshes.Add(new SmallMesh(GridManager.DefaultColorVisualData, colorMeshes[i]));
            }

            smallMeshes.Sort((x, y) => x.VertexCount.CompareTo(y.VertexCount));

            // remove empty meshes

            for (int i = smallMeshes.Count - 1; i >= 0; i--)
            {
                if (smallMeshes[i].VertexCount == 0)
                {
                    smallMeshes.RemoveAt(i);
                }
            }

            MaxMesh mm = MaxMesh.Default();

            for (int i = 0; i < smallMeshes.Count; i++)
            {
                if (mm.CanAdd(smallMeshes[i].smallMesh))
                {
                    mm.Add(smallMeshes[i].vData, smallMeshes[i].smallMesh);
                }
                else
                {
                    maxMeshGroup.Add(mm);

                    mm = MaxMesh.Default();

                    mm.Add(smallMeshes[i].vData, smallMeshes[i].smallMesh);
                }
            }

            if (mm.VertexCount > 0)
            {
                maxMeshGroup.Add(mm);
            }

            // save memory
            smallMeshes.Clear();

            //TimeLogger.StopTimer(1516);
        }
        public void DrawFusedMesh()
        {
            //TimeLogger.StartTimer(71451, "Update Mesh");

            // delete all child game objects 
            foreach (GameObject go in layerMeshObjs)
            {
                DestroyImmediate(go);
            }

            layerMeshObjs.Clear();

            GroupAndDrawMeshes();

            //TimeLogger.StopTimer(71451);
        }
        /// <summary>
        /// This is a group meshes that are within the max vert limit. Since a mesh layer size might require a mesh to be larger than the max vert limit, we need to divide the meshes into groups of meshes that are within the max vert limit. Then we draw each mesh in its own game object group. Note once meshes are drawn, the list is cleared to save memory.
        /// </summary>
        List<MaxMesh> maxMeshGroup = new List<MaxMesh>();

        List<(ShapeVisualData, Material)> vDataMats = new List<(ShapeVisualData, Material)>();

        private void GroupAndDrawMeshes()
        {
            int x = 1;
            foreach (MaxMesh m in maxMeshGroup)
            {
                GameObject meshHolder = CreateMeshHolder("Mesh " + x++);
                layerMeshObjs.Add(meshHolder);

                Mesh mesh = new Mesh();

                List<Mesh> subMeshes = m.smallMesh.Select((x) => x.GetMesh()).ToList();

                mesh = ShapeMeshFuser.CombineToSubmesh(subMeshes);

                List<Material> sharedMats = new List<Material>();
                List<MaterialPropertyBlock> matProps = new List<MaterialPropertyBlock>();

                foreach (ShapeVisualData vData in m.vDatas)
                {
                    //vData.ValidateVisualData();

                    sharedMats.Add(vData.SharedMaterial);
                    matProps.Add(vData.PropertyBlock);

                    vDataMats.Add((vData, vData.SharedMaterial));
                    // for each visual data creating matching material
                }

                MeshRenderer ren = meshHolder.GetComponent<MeshRenderer>();

                ren.sharedMaterials = sharedMats.ToArray();

                for (int i = 0; i < sharedMats.Count; i++)
                {
                    ren.SetPropertyBlock(matProps[i], i);
                }

                meshHolder.GetComponent<MeshFilter>().sharedMesh = mesh;
            }

            // save memory
            maxMeshGroup.Clear();
        }

        private GameObject CreateMeshHolder(string objName = "Layer Mesh")
        {
            GameObject meshHold = new GameObject(objName);
            meshHold.transform.SetParent(transform);

            meshHold.transform.localPosition = Vector3.zero;

            // add mesh components

            MeshFilter meshF = meshHold.AddComponent<MeshFilter>();
            MeshRenderer meshR = meshHold.AddComponent<MeshRenderer>();

            return meshHold;
        }

        /// <summary>
        /// This will modify various properties of the shape mesh fusers such that when we fuse the meshes, they will be oriented correctly
        /// </summary>
        private void PrepareForOrientation()
        {
            ValidateAllSettings();

            foreach (ShapeMeshFuser fuser in MaterialVisualGroup.Values)
            {
                fuser.PositionOffset = chunkOffset;
                // whenever the orientation is changed, the shape mesh needs to be validated because its vertices have also changed
                fuser.ValidateShapeMesh();
            }

            ColorVisualGroup.PositionOffset = chunkOffset;
        }
        public void ValidateOrientation()
        {
            PrepareForOrientation();

            DrawLayer();
        }

        // this is a faster version of the ValidateOrientation method
        // there currently a bug with this method, it is not working as expected
        //private void ValidateOrientation_Fast()
        //{
        //    PrepareForOrientation();

        //    List<MeshData> meshDatas = new List<MeshData>();

        //    foreach (GameObject item in layerMeshes)
        //    {
        //        MeshFilter mf = item.GetComponent<MeshFilter>();
        //    }

        //    Parallel.ForEach(meshDatas, data =>
        //    {
        //        Parallel.For(0, data.vertexCount, x =>
        //        {
        //            data.Vertices[x] = data.Vertices[x].SwapYZ();
        //        });
        //    });

        //    for (int i = 0; i < layerMeshes.Count; i++)
        //    {
        //        GameObject item = layerMeshes[i];
        //        MeshFilter mf = item.GetComponent<MeshFilter>();
        //        mf.sharedMesh = meshDatas[i].GetMesh();
        //    }

        //    meshDatas.Clear();
        //}
        /// <summary>
        /// When redrawing the mesh, we want to skip various checks to expediate the process
        /// </summary>
        bool reInsertMode = false;
        /// <summary>
        /// Clears the mesh and reinserts all visual data back into the layer. This is useful when the equality comparison for the visual properties has been changed.
        /// </summary>
        private void VisualEqualityChanged()
        {
            // when the visual equality changes, we have to reinsert all positiosn inorder to regroup them appriopriately
            // because we are reinserting all the data back, we have to cache the visual data and grid visual ids and then clear them, then as we call insertVisualData, the method will reinsert the data back

            MaterialVisualGroup.Clear();
            ColorVisualGroup.Clear();

            reInsertMode = true;
            foreach (int hash in CellVisualDatas.Keys)
            {
                Vector2Int gridPosition = CellGridPositions[hash];
                ShapeVisualData visual = CellVisualDatas[hash];

                InsertVisualData(gridPosition, visual);
            }

            reInsertMode = false;

            DrawLayer();
        }
        public void DrawLayer()
        {
            FusedMeshGroups();

            DrawFusedMesh();
        }
        public void Clear()
        {
            if (MaterialVisualGroup != null)
            {
                foreach (ShapeMeshFuser item in MaterialVisualGroup.Values)
                {
                    item.Clear();
                }

                foreach (GameObject item in layerMeshObjs)
                {
                    MeshFilter mf = item.GetComponent<MeshFilter>();
                    DestroyImmediate(mf.sharedMesh);

                    DestroyImmediate(item);
                }

                layerMeshObjs.Clear();
                MaterialVisualGroup.Clear();
                CellVisualDatas.Clear();
                ColorVisualGroup.Clear();
                CellGridPositions.Clear();
            }

            ColorVisualGroup = null;
            MaterialVisualGroup = null;
            layerGridShape = null;
        }


        /// <summary>
        /// This struct is used to group/add meshes until the max vertices is reached
        /// </summary>
        private struct MaxMesh
        {
            public List<ShapeVisualData> vDatas;
            public List<MeshData> smallMesh;

            public int VertexCount;
            private void Init()
            {
                vDatas = new List<ShapeVisualData>();
                smallMesh = new List<MeshData>();

                VertexCount = 0;
            }
            public void Add(ShapeVisualData vData, MeshData fuser)
            {
                vDatas.Add(vData);
                smallMesh.Add(fuser);

                VertexCount += fuser.vertexCount;
            }
            public bool CanAdd(MeshData fuser)
            {
                if (VertexCount + fuser.vertexCount <= MAX_VERTICES)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public static MaxMesh Default()
            {
                MaxMesh def = new MaxMesh();

                def.Init();

                return def;
            }
        }

        /// <summary>
        /// This struct is used to group the visual data and the mesh data together
        /// </summary>
        private struct SmallMesh
        {
            public ShapeVisualData vData;
            public MeshData smallMesh;
            public int VertexCount => smallMesh.vertexCount;
            public SmallMesh(ShapeVisualData vData, MeshData fuser)
            {
                this.vData = vData;
                this.smallMesh = fuser;
            }
            public void Deconstruct(out ShapeVisualData vData, out MeshData fuser)
            {
                vData = this.vData;
                fuser = this.smallMesh;
            }
        }
    }
}
