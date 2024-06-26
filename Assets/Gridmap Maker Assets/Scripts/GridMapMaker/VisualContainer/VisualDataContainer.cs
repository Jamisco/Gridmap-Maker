using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GridMapMaker
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
            }
        }
#endif
    }
}
