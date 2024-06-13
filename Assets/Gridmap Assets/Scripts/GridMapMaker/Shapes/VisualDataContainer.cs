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
        [SerializeField]
        private List<string> matchingGuids = new List<string>();

        [SerializeField]
        private List<VisualObject> visualObjects = new List<VisualObject>();

        private Dictionary<Object, Guid> objectGuid = new Dictionary<Object, Guid>();
        private Dictionary<Guid, Object> guiDict = new Dictionary<Guid, Object>();

        bool CheckDictInit
        {
            get 
            {
                int count = MapObjects.Count;

                if(objectGuid.Count != count || guiDict.Count != count)
                {
                    return false;
                }

                return true;
            }
        }
        
        private void Awake()
        {
            //Debug.Log("Awake");
            //Debug.Log("All Object Count: " + visualObjects.Count);
            //Debug.Log("Matching Guids Count: " + matchingGuids.Count);
            //Debug.Log("Visual Objects Count: " + visualObjects.Count);

            InitDictionaries();
        }
        public override Guid GetGuid(Object obj)
        {
            if (!CheckDictInit)
            {
                InitDictionaries();
            }

            Guid g = Guid.Empty;

            if (obj == null)
            {
                return g;
            }

            objectGuid.TryGetValue(obj, out g);

            return g;
        }
        public override Object GetObject(Guid id)
        {
            if (!CheckDictInit)
            {
                InitDictionaries();
            }
            
            Object obj = null;

            if (id == Guid.Empty)
            {
                return obj;
            }

            guiDict.TryGetValue(id, out obj);

            return obj;
        }
        private void AddToDict(VisualObject obj)
        {
            objectGuid.Add(obj.MainObject, obj.Gid);
            guiDict.Add(obj.Gid, obj.MainObject);
        }
        private void CreateVisualAddDict(Object obj)
        {
            Guid ng = Guid.NewGuid();

            objectGuid.Add(obj, ng);
            guiDict.Add(ng, obj);

            VisualObject visual = new VisualObject(obj, ng);

            visualObjects.Add(visual);
        }
        private void RemoveFromDict(Object obj)
        {
            guiDict.Remove(objectGuid[obj]);
            objectGuid.Remove(obj);
        }
        private bool GuidExistsInDict(Object gui)
        {
            Guid g = Guid.Empty;

            objectGuid.TryGetValue(gui, out g);

            if (g != Guid.Empty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public virtual void ValidateObjects()
        {
            // makes sure every matching object in the ALLObjects, has a matching GUI
            // the guid list will not delete ID's unless specifically asked too   

            MapObjects = MapObjects.Distinct().ToList();

            // first we remove any visual objects that are not in the MapObjects list
            for (int i = 0; i < visualObjects.Count; i++)
            {
                bool exists = MapObjects.Any(x => visualObjects[i].MainObject == x);

                if (!exists)
                {
                    visualObjects.RemoveAt(i);
                }
            }

            Debug.Log("Cleared");
            matchingGuids.Clear();

            // we use this to create a new list of visual objects that match the MapObjects list. It also removes any duplicates
            List<VisualObject> matchingList = new List<VisualObject>();

            foreach (Object item in MapObjects)
            {
                VisualObject vo = visualObjects.FirstOrDefault(x => x.MainObject == item);

                Guid g = vo.Gid;

                if (g == Guid.Empty)
                {
                    g = Guid.NewGuid();
                    vo = new VisualObject(item, g);
                }

                matchingGuids.Add(g.ToString());
                matchingList.Add(vo);
            }

            visualObjects = matchingList;

            InitDictionaries();

            // this will make sure unity saves changes to disk. Need for all objects that are not modified in editor
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
            
        }
        void InitDictionaries()
        {
            objectGuid.Clear();
            guiDict.Clear();


            if (matchingGuids.Count != MapObjects.Count)
            {
                Debug.LogError("Matching Guids list is not the same as the MapObjects list. This can cause issues. Please Validate your Visual Container before continuing.");
            }

            // sometimes the matching guids list is more than the MapObjects list. Because the user deleted or added objects without proper validation

            for (int i = 0; i < MapObjects.Count; i++)
            {
                Guid g = Guid.Parse(matchingGuids[i]);
                Object obj = MapObjects[i];

                objectGuid.Add(obj, g);
                guiDict.Add(g, obj);
            }
        }
        public void OnBeforeSerialize()
        {

        }
        public void OnAfterDeserialize()
        {
            // since dictionaries are not serializable, we need to recreate them after deserialization
            InitDictionaries();
        }

        [Serializable]
        public struct VisualObject
        {
            [SerializeField]
            [HideInInspector]
            private Object obj;

            [SerializeField]
            private Guid gid;

            [SerializeField]
            [ShowOnlyField]
            private string ObjectName;

            [SerializeField]
            [ShowOnlyField]
            private string GidString;

            public Object MainObject { get => obj; set => obj = value; }
            public Guid Gid { get => gid; set => gid = value; }

            public VisualObject(Object obj, Guid gid)
            {
                this.gid = gid;
                this.obj = obj;

                ObjectName = obj.ToString();
                GidString = gid.ToString();
            }
        }

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
