
using Assets.Gridmap_Assets.Scripts.GridMapMaker;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using Assets.Gridmap_Assets.Scripts.Miscellaneous;
using Assets.Scripts.GridMapMaker;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData.ShapeVisualData;
using static Assets.Scripts.GridMapMaker.FusedMesh;
using static Assets.Scripts.GridMapMaker.GridManager;
using static Assets.Scripts.Miscellaneous.ExtensionMethods;
using Debug = UnityEngine.Debug;

namespace Assets.Gridmap_Assets.Scripts.Mapmaker
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

        [SerializeField]
        private bool useVisualEquality;

        public bool UseVisualEquality
        {
            get => useVisualEquality;
            set
            {
                useVisualEquality = value;

                visualDataComparer.UseVisualHash = value;
            }
        }
        
        public GridShape LayerGridShape { get; private set; }

        VisualDataComparer visualDataComparer = new VisualDataComparer();

        private ShapeVisualData defaultVisualProp;

        /// <summary>
        // The grouping of visual datas that are used to make a mesh.
        // So cells that have "equal" visualData will be group here and drawn as one
        /// </summary>
        private Dictionary<ShapeVisualData, ShapeMeshFuser> VisualDataGroup;
        /// <summary>
        /// The original visualData used for each cell. We store this so we can redraw the mesh if need be
        /// </summary>
        private Dictionary<Vector2Int, ShapeVisualData> CellVisualDatas
                          = new Dictionary<Vector2Int, ShapeVisualData>();
        private Mesh shapeMesh;

        private Bounds layerBounds;

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
        private List<Material> SharedMaterials { get; set; }
                                            = new List<Material>();
        public List<MaterialPropertyBlock> PropertyBlocks { get; private set; }
                                        = new List<MaterialPropertyBlock>();

        [SerializeField]
        private bool UpdateOnVisualChange;

        private void OnValidate()
        {

        }
        public void SortLayer(SortAxis axis, float offset)
        {
            Vector3 pos = gameObject.transform.position;

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

            gameObject.transform.position = pos;
        }
        private void Initialize(GridShape gridShape)
        {
            Clear();
            
            LayerGridShape = gridShape;
            shapeMesh = gridShape.GetShapeMesh();
        }

        public void Initialize(string layerId, GridChunk chunk, GridShape gridShape, ShapeVisualData defaultVisual, bool useVisualEquality = true)
        {
            gridChunk = chunk;
            
            Initialize(gridShape);

            gameObject.name = layerId;

            this.layerId = layerId;
            this.defaultVisualProp = defaultVisual;
            this.useVisualEquality = useVisualEquality;
            visualDataComparer.UseVisualHash = useVisualEquality;

            VisualDataGroup = new Dictionary<ShapeVisualData, ShapeMeshFuser>(visualDataComparer);

            chunkOffset = gridShape.GetTesselatedPosition(chunk.StartPosition);

            SetLayerBounds();
        }

        public void Initialize(MeshLayerInfo layerInfo, GridChunk chunk)
        {
            gridChunk = chunk;
            LayerGridShape = layerInfo.Shape;

            Initialize(layerInfo.Shape);

            gameObject.name = layerId;

            layerId = layerInfo.LayerId;
            defaultVisualProp = layerInfo.DefaultVisualData;
            useVisualEquality = layerInfo.UseVisualEquality;

            visualDataComparer.UseVisualHash = useVisualEquality;

            VisualDataGroup = new Dictionary<ShapeVisualData, ShapeMeshFuser>(visualDataComparer);

            chunkOffset = LayerGridShape.GetTesselatedPosition(chunk.StartPosition);

            SetLayerBounds();
        }

        private void SetLayerBounds()
        {
            Vector2Int min = gridChunk.StartPosition;
            Vector2Int max = gridChunk.EndPosition;

            layerBounds = LayerGridShape.GetGridBounds(min, max);
        }

        public Bounds GetBounds(Vector3 gridWorldPosition = new Vector3())
        {
            Bounds bounds = layerBounds;

            bounds.center += gridWorldPosition;

            return bounds;
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

            TimeLogger.StartTimer(12, "InsertPosition");

            // 36% of the time is spent here
            if (reInsertMode == false)
            {
                // the delete method will remove the position if it exists. So we dont have to check if the gridPositions exist before deleting
                DeleteShape(gridPosition);

                CellVisualDatas.Add(gridPosition, visualProp);
            }

            // 10.2% of the time is spent here
            ShapeMeshFuser meshFuser = null;
            
            VisualDataGroup.TryGetValue(visualProp, out meshFuser);
            
            if (meshFuser == null)
            {
                // 13.5% of the time is spent here
                meshFuser = new ShapeMeshFuser(LayerGridShape, chunkOffset);

                VisualDataGroup.Add(visualProp, meshFuser);

                SetEvent(visualProp);
            }

            // 20% of the time is spent here
            meshFuser.InsertPosition(gridPosition);
            
            TimeLogger.StopTimer(12);
        }
        public void RemoveVisualData(Vector2Int gridPosition)
        {
            // removing a visual vData is thesame as inserting a default visual vData

            InsertVisualData(gridPosition, defaultVisualProp);
        }

        public void DeleteShape(Vector2Int gridPosition)
        {
            // We will straight up delete the mesh at the grid position

            ShapeVisualData existing = null;

            CellVisualDatas.TryGetValue(gridPosition, out existing);

            if (existing != null)
            {
                VisualDataGroup[existing].RemovePosition(gridPosition);

                if (VisualDataGroup[existing].IsEmpty)
                {
                    VisualDataGroup.Remove(existing);
                    RemoveEvent(existing);
                }

                CellVisualDatas.Remove(gridPosition);
            }
        }
        public bool HasVisualData(Vector2Int gridPosition)
        {
            return CellVisualDatas.ContainsKey(gridPosition);
        }
        public ShapeVisualData GetVisualProperties(Vector2Int gridPosition)
        {
            ShapeVisualData existing = null;

            CellVisualDatas.TryGetValue(gridPosition, out existing);

            return existing;
        }
        public void VisualIdChanged(ShapeVisualData sender)
        {
            ShapeVisualData changedProp = VisualDataGroup.Keys.FirstOrDefault
                                    (x => x == sender);

            // make sure the changedProp exists
            // if it doesn't, it means that the visual vData was never inserted in the first place or has been removed
            if (changedProp != null)
            {
                // When a visual prop has changed, we need to see if there is another visual prop that looks like it

                ShapeVisualData identicalData = VisualDataGroup.Keys.FirstOrDefault
                            (x => (x != changedProp && x.Equals(changedProp)));

                // if there is a identicalData, combine it with the old vData
                if (identicalData != null)
                {
                    // if there is a identicalData, combine the fused meshes
                    ShapeMeshFuser prePositions = VisualDataGroup[identicalData];
                    ShapeMeshFuser changedPositions = VisualDataGroup[changedProp];

                    // remove the old fused mesh
                    VisualDataGroup.Remove(changedProp);

                    prePositions.CombineFuser(changedPositions);

                    VisualDataGroup[identicalData] = prePositions;
                }


#if UNITY_EDITOR
                // if we are in editor, the event was most likely raised during a serialization process, and we can't update the mesh during serialization. So we wait until the serialization process is done, then update the mesh. 
                if (UpdateOnVisualChange)
                {
                    EditorApplication.delayCall += () =>
                    {
                        UpdateMesh();
                    };
                }
#else
                UpdateMesh();
#endif
            }
            else
            {
                // this visualData does not exist in this layer, remove it
                // this should never occur so as long as we are removing the event whenever we delete a position
            }

        }

        private List<GameObject> layerMeshes = new List<GameObject>();
        public void CreateFusedMeshes()
        {
            TimeLogger.StartTimer(1516, "CreateFusedMeshes");
            
            List<SmallMesh> smallMeshes = new List<SmallMesh>();

            foreach (var vData in VisualDataGroup.Keys)
            {
                ShapeMeshFuser m = VisualDataGroup[vData];
                // this line accounts for 80% of the time it takes to draw our map
                m.UpdateMesh();

                List<Mesh> tempMeshes = m.GetAllMeshes();

                for (int i = 0; i < tempMeshes.Count; i++)
                {
                    smallMeshes.Add(new SmallMesh(vData, tempMeshes[i]));
                }
            }

            // group meshes that are within the max vert limit, combined them, with sub meshes, use material from visual data               
            GroupAndDrawMeshes();
            
            TimeLogger.StopTimer(1516);
            
            GameObject CreateMeshHolder(string objName = "Layer Mesh")
            {
                GameObject meshHold = new GameObject(objName);
                meshHold.transform.SetParent(transform);

                meshHold.transform.localPosition = Vector3.zero;

                // add mesh components

                MeshFilter meshF = meshHold.AddComponent<MeshFilter>();
                MeshRenderer meshR = meshHold.AddComponent<MeshRenderer>();

                return meshHold;
            }

            void GroupAndDrawMeshes()
            {
                smallMeshes.Sort((x, y) => x.VertexCount.CompareTo(y.VertexCount));

                MaxMesh mm = MaxMesh.Default();

                List<MaxMesh> mmGroup = new List<MaxMesh>();

                for(int i = 0; i < smallMeshes.Count; i++)
                {
                    if (mm.CanAdd(smallMeshes[i].smallMesh) )
                    {
                        mm.Add(smallMeshes[i].vData, smallMeshes[i].smallMesh);
                    }
                    else
                    {
                        mmGroup.Add(mm);

                        mm = MaxMesh.Default();

                        mm.Add(smallMeshes[i].vData, smallMeshes[i].smallMesh);
                    }
                }

                if(mm.VertexCount > 0)
                {
                    mmGroup.Add(mm);
                }

                int x = 1;
                foreach (MaxMesh m in mmGroup)
                {
                    GameObject meshHolder = CreateMeshHolder("Mesh " + x++);
                    layerMeshes.Add(meshHolder);
                    
                    Mesh mesh = new Mesh();

                    mesh = FusedMesh.CombineToSubmesh(m.smallMesh);

                    List<Material> sharedMats = new List<Material>();
                    List<MaterialPropertyBlock> matProps = new List<MaterialPropertyBlock>();

                    foreach(ShapeVisualData vData in m.vDatas)
                    {
                        ShapeRenderData srd = vData.GetShapeRenderData();

                        sharedMats.Add(srd.SharedMaterial);
                        matProps.Add(srd.PropertyBlock);
                    }

                    MeshRenderer ren = meshHolder.GetComponent<MeshRenderer>();

                    ren.sharedMaterials = sharedMats.ToArray();

                    for(int i = 0; i < sharedMats.Count; i++) 
                    {
                        ren.SetPropertyBlock(matProps[i], i);
                    }

                    meshHolder.GetComponent<MeshFilter>().sharedMesh = mesh;
                }
            }
        }
        public void UpdateMesh()
        {
            TimeLogger.StartTimer(71451, "Update Mesh");
            
            // delete all child game objects 
            foreach (GameObject go in layerMeshes)
            {
                DestroyImmediate(go);
            }

            layerMeshes.Clear();

            CreateFusedMeshes();

            TimeLogger.StopTimer(71451);
        }

        /// <summary>
        /// When redrawing the mesh, we want to skip various checks to expediate the process
        /// </summary>
        bool reInsertMode = false;
        
        /// <summary>
        /// Clears the mesh and reinserts all visual data back into the layer. This is useful when the equality comparison for the visual properties has been changed.
        /// </summary>
        public void ReInsertPositions()
        {
            // because we are reinserting all the data back, we have to cache the visual data and grid visual ids and then clear them, then as we call insertVisualData, the method will reinsert the data back
            
            VisualDataGroup.Clear();

            reInsertMode = true;
            foreach (Vector2Int gridPosition in CellVisualDatas.Keys)
            {
                ShapeVisualData visual = CellVisualDatas[gridPosition];

                RemoveEvent(visual);
                
                InsertVisualData(gridPosition, visual);
            }
            reInsertMode = false;
        }
        public void Clear()
        {
            if (VisualDataGroup != null)
            {
                VisualDataGroup.Clear();
            }

            
            defaultVisualProp = null;
            LayerGridShape = null;

            useVisualEquality = false;

            CellVisualDatas.Clear();

            SharedMaterials.Clear();
            PropertyBlocks.Clear();
        }
        public SerializedLayer SerializeLayer(MapVisualContainer container)
        {
            return new SerializedLayer(this, container);
        }
        public void Deserialize(SerializedLayer sLayer,
                        MapVisualContainer container,
                        List<ShapeVisualData> visualProps)
        {
            layerId = sLayer.layerId;

            GridShape shape = container.GetGridShape(sLayer.shapeId);
            Initialize(shape);

            for (int i = 0; i < sLayer.gridPositions.Count; i++)
            {
                Vector2Int pos = sLayer.gridPositions[i];

                Guid id = sLayer.gridVisualIds[i];
                ShapeVisualData data
                            = visualProps.First(x => x.VisualId == id);

                InsertVisualData(pos, data);
            }

            UpdateMesh();
        }

        private struct MaxMesh
        {
            public List<ShapeVisualData> vDatas;
            public List<Mesh> smallMesh;

            public int VertexCount;

            private void Init()
            {
                vDatas = new List<ShapeVisualData>();
                smallMesh = new List<Mesh>();

                VertexCount = 0;
            }
            public void Add(ShapeVisualData vData, Mesh fuser)
            {
                vDatas.Add(vData);
                smallMesh.Add(fuser);

                VertexCount += fuser.vertexCount;
            }

            public bool CanAdd(Mesh fuser)
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
            public Mesh smallMesh;

            public int VertexCount => smallMesh.vertexCount;
            public SmallMesh(ShapeVisualData vData, Mesh fuser)
            {
                this.vData = vData;
                this.smallMesh = fuser;
            }
            public void Deconstruct(out ShapeVisualData vData, out Mesh fuser)
            {
                vData = this.vData;
                fuser = this.smallMesh;
            }
        }

        [Serializable]
        public struct SerializedLayer
        {
            [SerializeField]
            public string shapeId;
            public string layerId;
            // the question now is should each layer stores its visual properties
            // or should the visual properties be stored in a gridmanager class
            public List<Vector2Int> gridPositions;
            public List<Guid> gridVisualIds;

            public SerializedLayer(MeshLayer layer, MapVisualContainer container)
            {
                shapeId = layer.LayerGridShape.UniqueShapeName;
                layerId = layer.layerId;

                gridPositions = layer.CellVisualDatas.Keys.ToList();
                gridVisualIds = new List<Guid>();

                foreach (Vector2Int cell in gridPositions)
                {
                    gridVisualIds.Add(layer.CellVisualDatas[cell].VisualId);
                }
            }
        }
        
        /// <summary>
        /// Just used to quick create layers. Nothing majors
        /// </summary>
       
    }

   
}
