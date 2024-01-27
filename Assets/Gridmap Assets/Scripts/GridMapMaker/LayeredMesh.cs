
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using Assets.Gridmap_Assets.Scripts.Miscellaneous;
using Assets.Scripts.GridMapMaker;
using Assets.Scripts.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Assets.Scripts.GridMapMaker.FusedMesh;
using Debug = UnityEngine.Debug;

namespace Assets.Gridmap_Assets.Scripts.Mapmaker
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class LayeredMesh : MonoBehaviour, ISerializationCallbackReceiver
    {
        /*
        5 meshes means 5 game objects to update, 5 renderer components to cull.
        1 mesh with 5 materials means 1 game object to update, 1 renderer component to cull.

        The benefit of 5 meshes is that each part can be separately culled if necessary, potentially reducing draw calls.
        The benefit of 1 mesh is less CPU overhead.
        */
        
        // A layered mesh is a collection of fused meshes that are then combined together to make one mesh.
        // Each fused mesh is a unique visually, meaning it may have a different texture, color, etc. However each fused mesh has thesame shape

        private Dictionary<ShapeVisualData, FusedMesh> LayerFusedMeshes
                                      = new Dictionary<ShapeVisualData, FusedMesh>();
        public GridShape LayerGridShape { get; private set; }

        private Mesh LayerMesh { get; set; }
        private Mesh shapeMesh;

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        private List<Material> SharedMaterials { get; set; } = new List<Material>();
        public List<MaterialPropertyBlock> PropertyBlocks { get; private set; }
                                             = new List<MaterialPropertyBlock>();

        private SortingLayerPicker meshLayer;
        public SortingLayerPicker MeshLayer
        {
            get => meshLayer;
            set
            {
                meshLayer = value;
                SetLayerInfo();
            }
        }

        private void Reset()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
        }
        private void OnValidate()
        {
            if (meshRenderer == null || meshFilter == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshFilter = GetComponent<MeshFilter>();
            }
        }

        private void SetLayerInfo()
        {
            meshRenderer.sortingLayerID = meshLayer.LayerID;
            meshRenderer.sortingOrder = meshLayer.OrderInLayer;
        }
        public void Initialize(GridShape gridShape)
        {
            Clear();
            LayerGridShape = gridShape;
            shapeMesh = gridShape.GetBaseShape();
        }

        public void InsertVisualData(ShapeVisualData visualData, Vector2Int gridPosition)
        {
            int hash = gridPosition.GetHashCode_Unique();
            Vector3 offset = LayerGridShape.GetTesselatedPosition(gridPosition);

            if (LayerFusedMeshes.ContainsKey(visualData))
            {
                LayerFusedMeshes[visualData].InsertMesh(shapeMesh, hash, offset);
            }
            else
            {
                FusedMesh fusedMesh = new FusedMesh();
                fusedMesh.InsertMesh(shapeMesh, hash, offset);
                LayerFusedMeshes.Add(visualData, fusedMesh);

                visualData.HashChanged += VisualData_HashChanged;

                SeriliazeDictBefore();
            }
        }
         
        private void InsertFusedMesh(ShapeVisualData visualData, FusedMesh mesh)
        {
            if (LayerFusedMeshes.ContainsKey(visualData))
            {
                // if the visual data already exists, combine the fused mesh with the existing one
                LayerFusedMeshes[visualData].CombineFusedMesh(mesh);
            }
            else
            {
                // if the visual data doesn't exist, add it to the dictionary
                LayerFusedMeshes.Add(visualData, mesh);
                visualData.HashChanged += VisualData_HashChanged;
            }
        }

        private void VisualData_HashChanged(ShapeVisualData sender, int newHash)
        {
            // When the hash of a visual data changes, it means that the visual data might no longer be unique. We need to check if it is still unique, if it is, we can just update that specific fused mesh. If it is not, we need to remove the old fused mesh and combine it with another fused mesh that has the same visual data.

            //Debug.Log($"Mesh {sender.VisualName} hash changed. " +
            //    $"Old hash: {sender.VisualHash}, new hash: {newHash}");

            ShapeVisualData oldData = LayerFusedMeshes.Keys.FirstOrDefault
                                    (x => x.GetHashCode() == newHash && x != sender);
            if (oldData != null)
            {
                // if there is a oldData, combine the fused meshes
                FusedMesh newMesh = LayerFusedMeshes[sender];
                FusedMesh oldMesh = LayerFusedMeshes[oldData];

                // remove the old fused mesh
                LayerFusedMeshes.Remove(sender);

                if (oldMesh.VertexCount > newMesh.VertexCount)
                {
                    oldMesh.CombineFusedMesh(newMesh);
                }
                else
                {
                    newMesh.CombineFusedMesh(oldMesh);
                    oldMesh = newMesh;
                }

                LayerFusedMeshes[oldData] = oldMesh;

                SeriliazeDictBefore();
            }

            // if there is no oldData, it means that the visual data is still unique, so we can just update the hash

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

        public void CombineFusedMeshes()
        {
            // Performance Test
            // for 50 x 50: 0 - 0.0001 seconds
            // for 100 x 100: 0 - 0.0001 seconds

            LayerMesh = FusedMesh.CombineToSubmesh(LayerFusedMeshes.Values.ToList());
            // create new mats for each sub mesh, assign list to renderer
            SetMaterials();
        }
        private void SetMaterials()
        {
            SharedMaterials.Clear();
            PropertyBlocks.Clear();

            foreach (ShapeVisualData data in LayerFusedMeshes.Keys)
            {
                SharedMaterials.Add(data.SharedMaterial);

                if (data.PropertyBlock == null)
                {
                    PropertyBlocks.Add(new MaterialPropertyBlock());
                }
                else
                {
                    PropertyBlocks.Add(data.PropertyBlock);
                }
            }

            meshRenderer.sharedMaterials = SharedMaterials.ToArray();

            for (int i = 0; i < SharedMaterials.Count; i++)
            {
                if (PropertyBlocks[i].isEmpty)
                {
                    continue;
                }

                meshRenderer.SetPropertyBlock(PropertyBlocks[i], i);
            }
        }
        public void UpdateMesh()
        {
            CombineFusedMeshes();

            if (LayerMesh != null)
            {
                meshFilter.sharedMesh = LayerMesh;
            }
        }
        public void Clear()
        {
            if (meshFilter != null)
            {
                meshFilter.mesh = null;
            }

            if (meshRenderer != null)
            {
                meshRenderer.materials = new Material[0];
            }

            foreach (FusedMesh fused in LayerFusedMeshes.Values)
            {
                fused.ClearFusedMesh();
            }

            LayerFusedMeshes?.Clear();
            SharedMaterials?.Clear();
            PropertyBlocks?.Clear();

            SeriliazeDictBefore();
        }

        void SeriliazeDictBefore()
        {
            
#if UNITY_EDITOR
            // dictionaries cannot be serialize so we have to do this
            visualDataList.Clear();
            fusedMeshesList.Clear();

            foreach (KeyValuePair<ShapeVisualData, FusedMesh> pair in LayerFusedMeshes)
            {
                visualDataList.Add(pair.Key);
                fusedMeshesList.Add(pair.Value);
            }
#endif
        }

        void SeriliazeDictAfter()
        {
#if UNITY_EDITOR

            EditorApplication.delayCall += () =>
            {
                // dictionaries cannot be serialize so we have to do this
                LayerFusedMeshes.Clear();

                for (int i = 0; i < visualDataList.Count; i++)
                {
                    LayerFusedMeshes.Add(visualDataList[i], fusedMeshesList[i]);
                    visualDataList[i].HashChanged += VisualData_HashChanged;
                }

                UpdateMesh();
            };
#endif
        }

#if UNITY_EDITOR
        
        [SerializeField] private bool UpdateOnVisualChange = true;

        List<ShapeVisualData> visualDataList = new List<ShapeVisualData>();
        List<SerializedFusedMesh> fusedMeshesList = new List<SerializedFusedMesh>();
#endif
        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {
            SeriliazeDictAfter();
        }
    }
}
