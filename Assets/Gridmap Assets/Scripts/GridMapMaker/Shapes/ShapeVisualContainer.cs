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
          "Only the last visual in the list will be updated when you modify the visual in the inspector. If you want to update all visuals, click the 'Validate Visual Hashes' button.")]
        [SerializeField]
        private List<QuickVisualData> QuickVisualDatas = new List<QuickVisualData>();

        private Dictionary<string, ShapeVisualData> ShapeVisuals
                             = new Dictionary<string, ShapeVisualData>();

        private void OnValidate()
        {
            if (QuickVisualDatas.Count > 0)
            {
                QuickVisualDatas[QuickVisualDatas.Count - 1].ValidateHashCode();
            }
        }

        private void ValidateVisualHashes()
        {
            for (int i = 0; i < QuickVisualDatas.Count; i++)
            {
                QuickVisualDatas[i].ValidateHashCode();
            }

            FillDictionary();
        }

        public void FillDictionary()
        {
            ShapeVisuals.Clear();
            QuickVisualData quickData;

            for (int i = 0; i < QuickVisualDatas.Count; i++)
            {
                if (ShapeVisuals.ContainsKey(QuickVisualDatas[i].VisualName))
                {
                    continue;
                }

                quickData = QuickVisualDatas[i];
                
                ShapeVisuals.Add(QuickVisualDatas[i].VisualName, 
                                                 quickData.CreateShapeVisualData());
            }
        }

        public ShapeVisualData GetRandom()
        {
            return ShapeVisuals.Values.ElementAt(UnityEngine.Random.Range(0, ShapeVisuals.Count));
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
            }
        }
#endif

    }
}
