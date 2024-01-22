using UnityEngine;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using System.Collections;
using Assets.Scripts.Miscellaneous;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Namespace.
namespace Assets.Scripts.GridMapMaker
{
    public class GridManager : MonoBehaviour
    {
        public Vector2Int GridSize;

        public HexagonalShape hexShape;

        public HexChunk hexChunk;

        public void GenerateGrid()
        {
            Mesh defMesh = hexShape.GetBaseShape();
            
            hexChunk.Init();

            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    Color rand = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));

                    defMesh.SetFullColor(rand);

                    Vector3 offset = hexShape.GetTesselatedPosition(x, y);

                    hexChunk.AddHex(defMesh, offset);
                    hexChunk.DrawMesh();
                }
            }

            hexChunk.DrawMesh();
        }

        private void OnValidate()
        {
            hexChunk.Clear();

            GenerateGrid();
        }

        public float time = 0.5f;
        private IEnumerator GenerateGridCoroutine()
        {
            Mesh defMesh;
            hexChunk.Init();
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    defMesh = hexShape.GetBaseShape();

                    Color rand = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));

                    defMesh.SetFullColor(rand);

                    Vector3 offset = hexShape.GetTesselatedPosition(x, y);

                    hexChunk.AddHex(defMesh, offset);
                    hexChunk.DrawMesh();
                    // Yielding null to wait for the next frame
                    yield return new WaitForSeconds(time);
                }
            }
        }

        public void StartGenerateGridCoroutine()
        {
            StartCoroutine(GenerateGridCoroutine());
        }


        public void ClearGrid()
        {
            hexChunk.Clear();
            StopAllCoroutines();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GridManager))]
    public class ClassButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GridManager exampleScript = (GridManager)target;

            if (GUILayout.Button("Generate Grid"))
            {
                exampleScript.GenerateGrid();
            }

            if (GUILayout.Button("Generate Grid Delay"))
            {
                exampleScript.StartGenerateGridCoroutine();
            }

            if (GUILayout.Button("Clear Grid"))
            {
                exampleScript.ClearGrid();
            }
        }
    }
#endif

}