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
    public abstract class MapVisualContainer : ScriptableObject
    {
        [SerializeField]
        protected List<Object> MapObjects = new List<Object>();
        
        [SerializeField]
        protected List<GridShape> GridShapes = new List<GridShape>();

        public abstract Guid GetGuid(Object obj);
        public abstract Object GetObject(Guid id);

        public GridShape GetGridShape(string shapeId)
        {
            return GridShapes.Where(x => x.UniqueShapeName == shapeId).FirstOrDefault();
        }

        public List<GridShape> GetGridShapes()
        {
            return new List<GridShape>(GridShapes);
        }

        public T GetRandomObject<T>() where T : Object
        {
            List<Object> objects
                = MapObjects.Where(x => x.GetType() == typeof(T)).ToList();

            if (objects.Count == 0)
            {
                return default;
            }

            return objects[UnityEngine.Random.Range(0, objects.Count)] as T;
        }
    }

}
