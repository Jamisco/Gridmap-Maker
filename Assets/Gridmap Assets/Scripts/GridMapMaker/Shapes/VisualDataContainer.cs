using Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Object = UnityEngine.Object;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [Serializable]
    [CreateAssetMenu(fileName = "VisualDataContainer", menuName = "GridMapMaker/VisualDataContainer", order = 0)]
    public class VisualDataContainer : MapVisualContainer
    {

#if UNITY_EDITOR
        [CustomEditor(typeof(VisualDataContainer))]
        public class ClassButtonEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                VisualDataContainer exampleScript = (VisualDataContainer)target;

                if (GUILayout.Button("Validate Objects"))
                {
                    exampleScript.ValidateObjects();
                }

                if (GUILayout.Button("Test Json"))
                {
                    Color green = Color.green;
                    string json = JsonUtility.ToJson("Color" + green);

                    Debug.Log(json);
                }
            }
        }
#endif
    }
}
