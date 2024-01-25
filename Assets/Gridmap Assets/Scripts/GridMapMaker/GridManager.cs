using UnityEngine;
using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes;
using System.Collections;
using Assets.Scripts.Miscellaneous;
using System;
using Random = UnityEngine.Random;
using static Assets.Gridmap_Assets.Scripts.Mapmaker.LayeredMesh;
using Assets.Gridmap_Assets.Scripts.Mapmaker;
using System.Collections.Generic;

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

        public LayeredMesh layerMesh;

        public ShapeVisualContainer visualContainer;

        public Texture texture;

        private void Awake()
        {
            
        }

        public void GenerateGrid()
        {
            layerMesh.Initialize(hexShape);
            
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);

                    layerMesh.InsertVisualData(visualContainer.GetRandom(), gridPosition);
                }
            }

            layerMesh.UpdateMesh();
        }

        public void print()
        {

        }

        public void Clear()
        {
            layerMesh.Clear();
        }

        private void OnValidate()
        {

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
                exampleScript.print();
            }

            if (GUILayout.Button("Clear Grid"))
            {
                exampleScript.Clear();
            }
        }
    }
#endif

}