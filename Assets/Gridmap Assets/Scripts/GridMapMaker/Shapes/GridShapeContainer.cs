using Assets.Gridmap_Assets.Scripts.Mapmaker;
using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    [CreateAssetMenu(fileName = "GridShapeContainer", menuName = "GridMapMaker/GridShapeContainer")]
    public class GridShapeContainer : ScriptableObject
    {
        [SerializeField]
        private List<GridShape> ShapesList = new List<GridShape>();
        
        private Dictionary<string, GridShape> ShapesDict 
                            = new Dictionary<string, GridShape>();

        public T GetShape<T>() where T : GridShape
        {
            foreach (GridShape shape in ShapesList)
            {
                if (shape is T)
                {
                    return (T)shape;
                }
            }

            return null;
        }

        void CheckForDuplicate()
        {
            Dictionary<string, GridShape> tempDict = new Dictionary<string, GridShape>();

            int i = 0;
            bool hasDuplicate = false;
            
            foreach (GridShape shape in ShapesList)
            {
                if (tempDict.ContainsKey(shape.UniqueShapeName))
                {
                    hasDuplicate = true;
                    Debug.Log($"Shape ID: {shape.UniqueShapeName} at index {i} Already Exists");
                }
                else
                {
                    tempDict.Add(shape.UniqueShapeName, shape);
                }

                i++;
            }

            if (!hasDuplicate)
            {
                Debug.Log("No Duplicate Found");
            }
        }

        public void Initialize()
        {
            ShapesDict.Clear();
            
            foreach (GridShape shape in ShapesList)
            {
                // some unknown bug cause shapes to be null during invalidate...dunno why
                if(shape == null)
                {
                    continue;
                }

                if (ShapesDict.ContainsKey(shape.UniqueShapeName))
                {
                    continue;
                }
                else
                {
                    ShapesDict.Add(shape.UniqueShapeName, shape);
                }
            }
        }

        public GridShape GetRandom()
        {
            return ShapesDict.Values.ElementAt(UnityEngine.Random.Range(0, ShapesDict.Count));
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(GridShapeContainer))]
        public class ClassButtonEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                GridShapeContainer myScript = (GridShapeContainer)target;

                if (GUILayout.Button("Check For Duplicate Shape"))
                {
                    myScript.CheckForDuplicate();
                }

                if (GUILayout.Button("Initialize Dictionary"))
                {
                    myScript.Initialize();
                }
            }
        }
#endif

    }
}
