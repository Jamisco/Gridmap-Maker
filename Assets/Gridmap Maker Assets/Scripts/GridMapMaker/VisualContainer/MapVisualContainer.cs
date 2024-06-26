using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GridMapMaker
{
    public abstract class MapVisualContainer : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        protected List<Object> MapObjects = new List<Object>();

        /// <summary>
        /// Please make sure your shader names are unique. Shaders are returned by name
        /// </summary>
        [SerializeField]
        protected List<Shader> MapShaders = new List<Shader>();

        /// <summary>
        /// Please make sure your shape names are unique. Shapes are returned by name
        /// </summary>
        [SerializeField]
        protected List<GridShape> GridShapes = new List<GridShape>();

        [SerializeField]
        private List<string> matchingGuids = new List<string>();

        [SerializeField]
        private List<VisualObject> visualObjects = new List<VisualObject>();

        private Dictionary<Object, Guid> object2Guid = new Dictionary<Object, Guid>();
        private Dictionary<Guid, Object> guid2Object = new Dictionary<Guid, Object>();

        [SerializeField]
        List<Object> allObjects = new List<Object>();

        private bool CheckDictInit
        {
            get
            {
                int count = allObjects.Count;

                if (object2Guid.Count != count || guid2Object.Count != count)
                {
                    return false;
                }

                return true;
            }
        }
        public Object GetMapObject(string name)
        {
            return MapObjects.Where(x => x.name == name).FirstOrDefault();
        }
        public List<Object> GetMapObjects()
        {
            return new List<Object>(MapObjects);
        }
        public Guid GetGuid(Object obj)
        {
            return object2Guid[obj];
        }
        public Object GetObject(Guid id)
        {
            return guid2Object[id];
        }
        public GridShape GetGridShape(string shapeId)
        {
            return GridShapes.Where(x => x.UniqueShapeName == shapeId).FirstOrDefault();
        }
        public List<GridShape> GetGridShapes()
        {
            return new List<GridShape>(GridShapes);
        }
        public Shader GetShader(string name)
        {
            return MapShaders.Where(x => x.name == name).FirstOrDefault();
        }
        public List<Shader> GetShaders()
        {
            return new List<Shader>(MapShaders);
        }
        public void ValidateObjects()
        {
            // makes sure every matching object in the ALLObjects, has a matching GUI
            // the guid list will not delete ID's unless specifically asked too   

            MapObjects = MapObjects.Distinct().ToList();
            MapShaders = MapShaders.Distinct().ToList();
            GridShapes = GridShapes.Distinct().ToList();

            allObjects.Clear();

            allObjects.AddRange(MapObjects);
             
            // first we remove any visual objects that are not in the MapObjects list
            for (int i = 0; i < visualObjects.Count; i++)
            {
                bool exists = allObjects.Any(x => visualObjects[i].MainObject == x);

                if (!exists)
                {
                    visualObjects.RemoveAt(i);
                }
            }

            Debug.Log("Cleared");
            matchingGuids.Clear();

            // we use this to create a new list of visual objects that match the MapObjects list. It also removes any duplicates
            List<VisualObject> matchingList = new List<VisualObject>();

            foreach (Object item in allObjects)
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
        private void InitDictionaries()
        {
            object2Guid.Clear();
            guid2Object.Clear();

            if (matchingGuids.Count != allObjects.Count)
            {
                Debug.LogError("Matching Guids list is not the same as the MapObjects list. This can cause issues. Please Validate your Visual Container before continuing.");
            }

            // sometimes the matching guids list is more than the MapObjects list. Because the user deleted or added objects without proper validation

            for (int i = 0; i < allObjects.Count; i++)
            {
                Guid g = Guid.Parse(matchingGuids[i]);
                Object obj = allObjects[i];

                object2Guid.Add(obj, g);
                guid2Object.Add(g, obj);
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

        public T GetRandomObject<T>() where T : Object
        {
            List<Object> objects
                = allObjects.Where(x => x.GetType() == typeof(T)).ToList();

            if (objects.Count == 0)
            {
                return default;
            }

            return objects[UnityEngine.Random.Range(0, objects.Count)] as T;
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
    }

}
