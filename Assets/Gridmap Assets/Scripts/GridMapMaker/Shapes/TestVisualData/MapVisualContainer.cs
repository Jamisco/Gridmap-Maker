using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Progress;
using Object = UnityEngine.Object;

namespace Assets.Gridmap_Assets.Scripts.GridMapMaker.Shapes.TestVisualData
{
    public abstract class MapVisualContainer : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        protected List<Object> AllObjects = new List<Object>();
        protected List<Guid> matchingGuids = new List<Guid>();

        private Dictionary<Object, Guid> objectGuid = new Dictionary<Object, Guid>();
        private Dictionary<Guid, Object> guiDict = new Dictionary<Guid, Object>();

        public Guid GetGuid(Object obj)
        {
            Guid g = Guid.Empty;

            if (obj == null)
            {
                return g;
            }

            objectGuid.TryGetValue(obj, out g);

            return g;
        }

        public Object GetObject(Guid id)
        {
            Object obj = null;

            if (id == Guid.Empty)
            {
                return obj;
            }

            guiDict.TryGetValue(id, out obj);

            return obj;
        }

        private void AddToDict(Object obj)
        {
            Guid ng = Guid.NewGuid();

            objectGuid.Add(obj, ng);
            guiDict.Add(ng, obj);
        }

        private void RemoveFromDict(Object obj)
        {
            guiDict.Remove(objectGuid[obj]);
            objectGuid.Remove(obj);
        }
        private bool GuidExists(Object gui)
        {
            Guid g = Guid.Empty;

            objectGuid.TryGetValue(gui, out g);

            if(g != Guid.Empty)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public T GetRandomObject<T>() where T : Object
        {
            List<Object> objects = AllObjects.Where(x => x.GetType() == typeof(T)).ToList();

            if (objects.Count == 0)
            {
                return default;
            }

            return objects[UnityEngine.Random.Range(0, objects.Count)] as T;
        }

        void InitDictionaries()
        {
            if (matchingGuids.Count != AllObjects.Count)
            {
                ValidateObjects();
            }
            
            objectGuid.Clear();
            guiDict.Clear();

            foreach (Object item in AllObjects)
            {
                AddToDict(item);
            }
        }

        public virtual void ValidateObjects()
        {
            // makes sure every matching object in the ALLObjects, has a matching GUI
            // the guid list will not delete ID's unless specifically asked too
            matchingGuids.Clear();

            AllObjects = AllObjects.Distinct().ToList();

            foreach (Object item in AllObjects)
            {
                if(!GuidExists(item))
                {
                    AddToDict(item);
                }

                Guid id = objectGuid[item];

                matchingGuids.Add(id);

                //Debug.Log($"Validated -- Object: {item.name}, Id: {id}");
            }
           
        }

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            InitDictionaries();
        }

        [Serializable]
        public struct VisualObject
        {
            [SerializeField]
            public readonly Object VObject;

            [ShowOnlyField]
            public readonly Guid Gid;

            public VisualObject(Object obj)
            {
                Gid = Guid.NewGuid();
                VObject = obj;
            }
        }

    }
}
