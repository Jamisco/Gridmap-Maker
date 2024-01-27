using Assets.Scripts.GridMapMaker;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes
{
    [CreateAssetMenu(fileName = "ShapeVisualMaker", menuName = "GridMapMaker/ShapeVisualMaker", order = 0)]
    [Serializable]
    public class ShapeVisualContainer : ScriptableObject
    {
        [Tooltip("The list of visuals that will be used to create the fused meshes. It is important that they are all unique. Visuals which are not unique will be Ignored. If two visuals will temporarily be the same, use the unique seed to make them unique." +
          "Only the last visual in the list will be updated when you modify the visual in the inspector. If you want to update all visuals,  click the 'Validate Visual Hashes' button.")]

        [SerializeField]
        private List<ShapeVisualData> VisualDatas = new List<ShapeVisualData>();


        private void OnValidate()
        {
            if (VisualDatas.Count > 0)
            {
                VisualDatas[VisualDatas.Count - 1].ValidateHashCode();
            }
        }

        private void ValidateVisualHashes()
        {
            for (int i = 0; i < VisualDatas.Count; i++)
            {
                VisualDatas[i].ValidateHashCode();
            }
        }

        private void UpdateMainMaterialProperties()
        {
            for (int i = 0; i < VisualDatas.Count; i++)
            {
                VisualDatas[i].InsertMainIntoMaterialProperty();
            }
        }

        public void ClearAllHashes()
        {
            VisualDatas.Clear();
        }
        
        public ShapeVisualData GetRandom()
        {
            return VisualDatas.ElementAt(UnityEngine.Random.Range(0, VisualDatas.Count));
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(ShapeVisualContainer))]
        public class ClassButtonEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                ShapeVisualContainer exampleScript = (ShapeVisualContainer)target;

                if (GUILayout.Button("Validate Visual Hashes"))
                {
                    exampleScript.ValidateVisualHashes();
                }

                if (GUILayout.Button("Insert Mains Into Material Properties"))
                {
                    exampleScript.UpdateMainMaterialProperties();
                }
            }
        }
#endif

    }
}
