using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GridMapMaker
{
    /// <summary>
    /// The VisualDataContainer class is a simple script that inherits from the MapVisualContainer class. It is used to store visual data for the grid map maker. This class should suffice for most use cases.
    /// </summary>
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
