using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GridMapMaker
{
    [RequireComponent(typeof(LayerHandle))]
    [Serializable]
    public class MeshLayer : MonoBehaviour
    {
        /*
        5 meshes means 5 game objects to update, 5 renderer components to cull.
        1 mesh with 5 materials means 1 game object to update, 1 renderer component to cull.

        The benefit of 5 meshes is that each part can be separately culled if necessary, potentially reducing draw calls.
        The benefit of 1 mesh is less CPU overhead.
        */
        // A layered mesh is a collection of fused meshes that are then combined together to make one mesh.
        // Each fused mesh is a unique visually, meaning it may have a different texture, color, etc. However each fused mesh has thesame Shape
        const int MAX_VERTICES = 65534;
        GridChunk gridChunk;

        public MeshLayerSettings LayerSettings
        {
            get
            {
                return gridChunk.GridManager.GetLayerInfo(layerId);
            }
        }
        public bool UseVisualEquality
        {
            get
            {
                return visualDataComparer.UseVisualHash;
            }
            set
            {
                if(visualDataComparer.UseVisualHash != value)
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
                if(layerGridShape != value)
                {
                    layerGridShape = value;

                    foreach (ShapeMeshFuser fuser in MaterialVisualGroup.Values)
                    {
                        fuser.GridShape = layerGridShape;
                    }

                    ColorVisualGroup.GridShape = layerGridShape;
                    ValidateLayerBounds();
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
        /// The original visualData used for each cell. We store this so we can redraw the mesh if need be
        /// </summary>
        public Dictionary<int, ShapeVisualData> CellVisualDatas
                          = new Dictionary<int, ShapeVisualData>();

        public Dictionary<int, Vector2Int> CellGridPositions = new Dictionary<int, Vector2Int>();

        private Bounds layerBounds;

        public Bounds LayerBounds
        {
            get
            {
                return layerBounds;
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
        private Shader colorShader;
        private ShapeVisualData defaultVisualData;

        private void OnValidate()
        {

        }
        public void SortLayer(SortAxis axis, float offset)
        {
            Vector3 pos = gameObject.transform.localPosition;

            switch (axis)
            {
                case SortAxis.X:
                    pos.x = offset;
                    break;
                case SortAxis.Y:
                    pos.y = offset;
                    break;
                case SortAxis.Z:
                    pos.z = offset;
                    break;
                default:
                    break;
            }

            gameObject.transform.localPosition = pos;
        }
        private void Initialize(GridShape gridShape)
        {
            Clear();
            
            layerGridShape = gridShape;
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

            colorShader = chunk.GridManager.ColorShader;

            defaultVisualData = chunk.GridManager.DefaultVisualData;

            ValidateLayerBounds();
        }
        private void ValidateAllSettings()
        {
            chunkOffset = layerGridShape.GetTesselatedPosition(gridChunk.StartPosition);
            ValidateLayerBounds();
        }
        private void ValidateLayerBounds()
        {
            Vector2Int min = gridChunk.StartPosition;
            Vector2Int max = gridChunk.EndPosition;

            layerBounds = LayerGridShape.GetGridBounds(min, max);
        }

        void SetEvent(ShapeVisualData visualProp)
        {
            visualProp.VisualDataChange += VisualIdChanged;
        }
        void RemoveEvent(ShapeVisualData visualProp)
        {
            visualProp.VisualDataChange -= VisualIdChanged;
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
                // the delete method will remove the position if it exists. So we dont have to check if the gridPositions exist before deleting
                DeleteShape(gridPosition);

                CellVisualDatas.Add(hash, visualProp);
                CellGridPositions.Add(hash, gridPosition);
            }

            // Mesh that draw only their colors are inserted into 1 group.
            // This is because we can draw all the color meshes in one draw call by simply painting the mesh vertices themselves
            if (visualProp.DataRenderMode == ShapeVisualData.RenderMode.MeshColor)
            {
                ColorVisualGroup.InsertPosition(hash, gridPosition, visualProp.mainColor);
                return;
            }

            // 10.2% of the time is spent here
            ShapeMeshFuser meshFuser = null;
            
            MaterialVisualGroup.TryGetValue(visualProp, out meshFuser);
            
            if (meshFuser == null)
            {
                // 13.5% of the time is spent here
                meshFuser = new ShapeMeshFuser(LayerGridShape, chunkOffset);

                MaterialVisualGroup.Add(visualProp, meshFuser);

                SetEvent(visualProp);
            }

            // 20% of the time is spent here
            meshFuser.InsertPosition(hash, gridPosition);
            
            //TimeLogger.StopTimer(2313);
        }

        public void RemoveVisualData(Vector2Int gridPosition)
        {
            // removing a visual vData is thesame as inserting a default visual vData
            InsertVisualData(gridPosition, defaultVisualData);
        }

        private void DeleteShape(int hash)
        {
            // We will straight up delete the mesh at the grid position. No visual data will be inserted

            ShapeVisualData existing = null;

            CellVisualDatas.TryGetValue(hash, out existing);

            if (existing != null)
            {
                if (MaterialVisualGroup.ContainsKey(existing))
                {
                    MaterialVisualGroup[existing].RemovePosition(hash);

                    if (MaterialVisualGroup[existing].IsEmpty)
                    {
                        MaterialVisualGroup.Remove(existing);
                        RemoveEvent(existing);
                    }
                }
                else
                {
                    ColorVisualGroup.RemovePosition(hash);
                }

                CellVisualDatas.Remove(hash);
                CellGridPositions.Remove(hash);
            }
        }
        public void DeleteShape(Vector2Int gridPosition)
        {
            // We will straight up delete the mesh at the grid position

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
                    RemoveEvent(existing);
                }

                CellVisualDatas.Remove(hash);
                CellGridPositions.Remove(hash);
            }
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
        public void VisualIdChanged(ShapeVisualData sender)
        {
            ShapeVisualData changedProp = MaterialVisualGroup.Keys.FirstOrDefault
                            (x => visualDataComparer.Equals(x, sender));

            // make sure the changedProp exists
            // if it doesn't, it means that the visual vData was never inserted in the first place or has been removed
            if (changedProp != null)
            {
                // When a visual prop has changed, we need to see if there is another visual prop that looks like it

                ShapeVisualData identicalData = MaterialVisualGroup.Keys.FirstOrDefault
                            (x => visualDataComparer.Equals(x, changedProp));

                // if there is a identicalData, combine it with the old vData
                if (identicalData != null)
                {
                    // if there is a identicalData, combine the fused meshes
                    ShapeMeshFuser prePositions = MaterialVisualGroup[identicalData];
                    ShapeMeshFuser changedPositions = MaterialVisualGroup[changedProp];

                    // remove the old fused mesh
                    prePositions.CombineFuser(changedPositions);

                    MaterialVisualGroup.Remove(changedProp);

                    MaterialVisualGroup[identicalData] = prePositions;
                }

             // if we are in editor, the event was most likely raised during a serialization process, and we can't update the mesh during serialization. So we wait until the serialization process is done, then update the mesh. 

                if (gridChunk.GridManager.RedrawOnVisualHashChanged)
                {
#if UNITY_EDITOR
                    EditorApplication.delayCall += () =>
                    {
                        DrawLayer();
                        return;
                    };
#endif
                    DrawLayer();
                }
            }
            else
            {
                RemoveEvent(sender);
                // this visualData does not exist in this layer, remove it
                // this should never occur so as long as we are removing the event whenever we delete a position
            }
        }

        private List<GameObject> layerMeshes = new List<GameObject>();
        public void FusedMeshGroups()
        {
            //TimeLogger.StartTimer(1516, "FusedMeshGroups");
            List<SmallMesh> smallMeshes = new List<SmallMesh>();

            bool useMultiThread = gridChunk.GridManager.UseMultithreading;
            
            foreach (ShapeVisualData vData in MaterialVisualGroup.Keys)
            {
                ShapeMeshFuser m = MaterialVisualGroup[vData];
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
                smallMeshes.Add(new SmallMesh(defaultVisualData, colorMeshes[i]));
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
            foreach (GameObject go in layerMeshes)
            {
                DestroyImmediate(go);
            }

            layerMeshes.Clear();

            GroupAndDrawMeshes();

            //TimeLogger.StopTimer(71451);
        }

        List<MaxMesh> maxMeshGroup = new List<MaxMesh>();
        // group meshes that are within the max vert limit, combined them, with sub meshes, use material from visual data  
        private void GroupAndDrawMeshes()
        {      
            int x = 1;
            foreach (MaxMesh m in maxMeshGroup)
            {
                GameObject meshHolder = CreateMeshHolder("Mesh " + x++);
                layerMeshes.Add(meshHolder);

                Mesh mesh = new Mesh();

                List<Mesh> subMeshes = m.smallMesh.Select((x) => x.GetMesh()).ToList();

                mesh = ShapeMeshFuser.CombineToSubmesh(subMeshes);

                List<Material> sharedMats = new List<Material>();
                List<MaterialPropertyBlock> matProps = new List<MaterialPropertyBlock>();

                foreach (ShapeVisualData vData in m.vDatas)
                {
                    ShapeRenderData srd = vData.GetShapeRenderData();

                    sharedMats.Add(srd.SharedMaterial);
                    matProps.Add(srd.PropertyBlock);
                }

                MeshRenderer ren = meshHolder.GetComponent<MeshRenderer>();

                ren.sharedMaterials = sharedMats.ToArray();

                for (int i = 0; i < sharedMats.Count; i++)
                {
                    ren.SetPropertyBlock(matProps[i], i);
                }

                meshHolder.GetComponent<MeshFilter>().sharedMesh = mesh;
                meshHolder.GetComponent<MeshCollider>().sharedMesh = mesh;
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
            MeshCollider meshC = meshHold.AddComponent<MeshCollider>();

            return meshHold;
        }
// This method is much faster, about 5x the time it takes to fuse and draw the mesh, but it is buggy, I havent figured it out yet...
        //public void ChangeOrientation()
        //{
        //    List<MeshData> meshDatas = new List<MeshData>();

        //    foreach (GameObject item in layerMeshes)
        //    {
        //        MeshFilter mf = item.GetComponent<MeshFilter>();
        //        meshDatas.Add(new MeshData(mf.sharedMesh));
        //    }

        //    // convert to normal for

        //    foreach (MeshData data in meshDatas)
        //    {
        //        for (int x = 0; x < data.vertexCount; x++)
        //        {
        //            data.Vertices[x] = data.Vertices[x].SwapYZ();
        //        }
        //    }

        //    for (int i = 0; i < layerMeshes.Count; i++)
        //    {
        //        GameObject item = layerMeshes[i];
        //        MeshFilter mf = item.GetComponent<MeshFilter>();
        //        mf.sharedMesh = meshDatas[i].GetMesh();
        //    }

        //    meshDatas.Clear();
        //}
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

                RemoveEvent(visual);
                
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

        public SerializedMeshLayer GetSerializedLayer()
        {
            return new SerializedMeshLayer(this);
        }

        public void Clear()
        {
            if (MaterialVisualGroup != null)
            {
                foreach (ShapeMeshFuser item in MaterialVisualGroup.Values)
                {
                    item.Clear();
                }

                foreach (GameObject item in layerMeshes)
                {
                    MeshFilter mf = item.GetComponent<MeshFilter>();
                    DestroyImmediate(mf.sharedMesh);

                    DestroyImmediate(item);
                }

                layerMeshes.Clear();
                MaterialVisualGroup.Clear();
                CellVisualDatas.Clear();
                ColorVisualGroup.Clear();
                CellGridPositions.Clear();
            }

            ColorVisualGroup = null;
            MaterialVisualGroup = null;
            layerGridShape = null;
        }

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
                if(VertexCount + fuser.vertexCount <= MAX_VERTICES)
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

    /// <summary>
    /// This is used to compare visuals.
    /// </summary>
    public class VisualDataComparer : IEqualityComparer<ShapeVisualData>
    {
        public bool UseVisualHash { get; set; }
        public bool Equals(ShapeVisualData x, ShapeVisualData y)
        {
            if (UseVisualHash == true)
            {
                return x.VisuallyEquals(y);
            }
            else
            {
                return x.Equals(y);
            }
        }
        public int GetHashCode(ShapeVisualData obj)
        {
            if (UseVisualHash == true)
            {
                return obj.VisualHash;
            }
            else
            {
                return obj.VisualIdHash;
            }
        }
    }

    [Serializable]
    public struct SerializedMeshLayer
    {
        [SerializeField]
        public string layerId;

        [SerializeField]
        public List<Vector2Int> gridPositions;

        [SerializeField]
        public List<string> visualDatas;

        public SerializedMeshLayer(MeshLayer layer)
        {
            layerId = layer.LayerId;
            gridPositions = new List<Vector2Int>();
            visualDatas = new List<string>();

            foreach (int hash in layer.CellVisualDatas.Keys)
            {
                gridPositions.Add(layer.CellGridPositions[hash]);
                visualDatas.Add(layer.CellVisualDatas[hash].VisualId.ToString());
            }
        }
    }
}
