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
        public RectangularShape rectShape;

        public LayeredMesh hexLayer;

        public LayeredMesh rectangleLayer;

        public ShapeVisualContainer visualContainer;

        public Texture texture;

        private void Awake()
        {
            
        }

        public void GenerateGrid()
        {
            hexLayer.Initialize(hexShape);
            rectangleLayer.Initialize(rectShape);
            
            for (int x = 0; x < GridSize.x; x++)
            {
                for (int y = 0; y < GridSize.y; y++)
                {
                    Vector2Int gridPosition = new Vector2Int(x, y);

                    hexLayer.InsertVisualData(visualContainer.GetRandom(), gridPosition);
                    rectangleLayer.InsertVisualData(visualContainer.GetRandom(), gridPosition);
                }
            }

            hexLayer.UpdateMesh();
            rectangleLayer.UpdateMesh();
        }

        public void print()
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            block.SetTexture("_MainTex", texture);

            Debug.Log("Block: " + block.GetHashCode());

            block.SetColor("_Color", Color.green);

            Debug.Log("Block: " + block.GetHashCode());
        }

        public void Clear()
        {
            hexLayer.Clear();
            rectangleLayer.Clear();
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