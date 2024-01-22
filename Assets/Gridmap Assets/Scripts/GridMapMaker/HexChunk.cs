using Assets.Gridmap_Assets.Scripts.Mapmaker;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;


namespace Assets.Scripts.GridMapMaker
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class HexChunk : MonoBehaviour
    {
        FusedMesh fusedMesh;

        public void Init()
        {
            fusedMesh = new FusedMesh();
        }

        public void AddHex(Mesh mesh, Vector3 offset)
        {
            fusedMesh.InsertMesh(mesh, offset.GetHashCode(), offset);
        }

        public void DrawMesh()
        {
            GetComponent<MeshFilter>().mesh = fusedMesh.Mesh;
            GetComponent<MeshCollider>().sharedMesh = fusedMesh.Mesh;
        }

        public void Clear()
        {
            if(fusedMesh != null)
            {
                fusedMesh.ClearFusedMesh();
            }

            GetComponent<MeshFilter>().mesh = null;
            GetComponent<MeshCollider>().sharedMesh = null;

        }
    }
}
