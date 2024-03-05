using Assets.Scripts.GridMapMaker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData
{
    [Serializable]
    public class GridTester : MonoBehaviour
    {
        [SerializeField]
        GridManager gridManager;

        [SerializeField]
        GridShape aShape;

        [SerializeField]
        BasicVisual basicVisual;


        private void OnValidate()
        {
            basicVisual.VisualHashChanged();
        }

        public void GenerateGrid()
        {
            //gridManager.GenerateGrid(aShape, basicVisual);
            gridManager.GenerateGrid(aShape);
        }
        public void ClearGrid()
        {
            gridManager.Clear();
        }

        public void SaveMap()
        {
            gridManager.SerializeMap();
        }

        public void LoadMap()
        {
            gridManager.DeserializeMap();
        }


#if UNITY_EDITOR
        [CustomEditor(typeof(GridTester))]
        public class ClassButtonEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                GridTester exampleScript = (GridTester)target;

                if (GUILayout.Button("Generate Grid"))
                {
                    exampleScript.GenerateGrid();
                }

                if (GUILayout.Button("Clear Grid"))
                {
                    exampleScript.ClearGrid();
                }

                if (GUILayout.Button("Save Map"))
                {
                    exampleScript.SaveMap();
                }

                if (GUILayout.Button("Load Map"))
                {
                    exampleScript.LoadMap();
                }
            }
        }
#endif
    }
}
