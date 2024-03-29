﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;
using Random = System.Random;

namespace Assets.Scripts.Miscellaneous
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Will return the first component in all of the objects children of the given type,
        /// with the given name.Do not recommend for use in performance sensitive scenarios
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gameObject"></param>
        /// <param name="componentName"></param>
        /// <returns></returns>
        /// 

#if UNITY_EDITOR
        public static void ClearLog()
        {
            var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
        }

#endif
        public static Bounds OrthographicBounds3D(this Camera camera)
        {
            float screenAspect = camera.aspect;
            float cameraHeight = camera.orthographicSize * 2;

            Vector3 position = camera.transform.localPosition;

            position.y = 0;

            Bounds bounds = new Bounds(position,
                            new Vector3(cameraHeight * screenAspect, 0, cameraHeight));
            return bounds;
        }

        public static T GetComponentByName<T>(this GameObject gameObject, string componentName, bool includeActive = true) where T : Component
        {
            return gameObject.GetComponentsInChildren<T>(includeActive)
             .First(x => x.name.Equals(componentName));
        }

        public static T GetComponentByName<T>(this Component gameObject, string componentName, bool includeActive = true) where T : Component
        {
            return gameObject.GetComponentsInChildren<T>(includeActive)
             .First(x => x.name.Equals(componentName));
        }


        public static GameObject GetGameObject(this GameObject gameObject, string objectName)
        {
            return gameObject.GetComponentByName<Transform>(objectName).gameObject;
        }

        public static void RemoveRange<T>(this List<T> collection, int startIndex, int count)
        {
            if (collection.Count > startIndex)
            {
                collection.RemoveRange(startIndex, count);
            }
        }

        private static Random random = new Random(Environment.TickCount);
        private static readonly object syncLock = new object();
        // https://csharpindepth.com/Articles/Random
        // why you should lock your random generator

        /// <summary>
        /// Gets a Random number being -1 and 1. These functions are ass do not use
        /// </summary>
        /// <param name="random"></param>
        /// <returns></returns>
        public static double NextDouble(this Random RandGenerator, double MinValue, double MaxValue)
        {
            lock (syncLock)
            {
                return RandGenerator.NextDouble() * (MaxValue - MinValue) + MinValue;
            }
        }

        public static float NextFloat(this Random RandGenerator, float MinValue, float MaxValue)
        {
            float ran = (float)(RandGenerator.NextDouble() * (MaxValue - MinValue) + MinValue);

            return ran;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <param name="minRange">min range of new value</param>
        /// <param name="maxRange">max range of new value</param>
        /// <returns></returns>
        public static float Normalize(float value, float minValue, float maxValue, float minRange, float maxRange)
        {
            // Calculate the range of the input values
            float valueRange = maxValue - minValue;

            // Normalize the value
            float normalizedValue = (value - minValue) / valueRange;
            normalizedValue = (normalizedValue * (maxRange - minRange)) + minRange;

            return normalizedValue;
        }

        /// <summary>
        /// Will log the given milliseconds in a readable format. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="timeInMilliseconds"></param>
        public static void LogTimer(string message, float timeInMilliseconds)
        {
            Debug.Log(ParseLogTimer(message, timeInMilliseconds));
        }

        public static string ParseLogTimer(string message, float timeInMilliseconds)
        {
            int minutes;
            float seconds;

            string log = "";

            if (timeInMilliseconds >= 60000)
            {
                minutes = (int)(timeInMilliseconds / 60000);
                seconds = (timeInMilliseconds % 60000) / 1000f;
                log = $"{message} {minutes} minutes {seconds} seconds";
            }
            else
            {
                log = $"{message} {timeInMilliseconds / 1000f} seconds";
            }

            return log;
        }

        public static bool TryRemoveElementsInRange<TValue>([DisallowNull] this IList<TValue> list, int index, int count, [NotNullWhen(false)] out Exception error)
        {
            try
            {
                if (list is List<TValue> genericList)
                {
                    genericList.RemoveRange(index, count);
                }
                else
                {
                    if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
                    if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
                    if (list.Count - index < count) throw new ArgumentException("index and count do not denote a valid range of elements in the list");

                    for (var i = count; i > 0; --i)
                        list.RemoveAt(index);
                }
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }

            error = null;
            return true;
        }

        public static int FindIndex(this int[] array, int[] subarray)
        {
            for (int i = 0; i <= array.Length - subarray.Length; i++)
            {
                bool found = true;

                for (int j = 0; j < subarray.Length; j++)
                {
                    if (array[i + j] != subarray[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    return i;
                }
            }

            return -1;
        }

        public static int FindIndex(this List<int> list, List<int> subList)
        {
            int[] mainArray = list.ToArray();
            int[] subArray = subList.ToArray();

            return mainArray.FindIndex(subArray);
        }

        public static int FindIndex(this List<int> list, int[] subArray)
        {
            int[] mainArray = list.ToArray();

            return mainArray.FindIndex(subArray);
        }


        public static Mesh CloneMesh(this Mesh parent)
        {
            Mesh mesh = new Mesh();
            mesh.vertices = parent.vertices;
            mesh.triangles = parent.triangles;
            mesh.uv = parent.uv;
            mesh.normals = parent.normals;
            mesh.tangents = parent.tangents;
            mesh.colors = parent.colors;
            mesh.bindposes = parent.bindposes;
            mesh.boneWeights = parent.boneWeights;
            mesh.subMeshCount = parent.subMeshCount;
            mesh.name = parent.name;
            mesh.bounds = parent.bounds;
            mesh.indexFormat = parent.indexFormat;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            return mesh;
        }

        /// <summary>
        /// Will set all the vertices of a mesh to the given color
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="color"></param>
        public static void SetFullColor(this Mesh mesh, Color color)
        {
            Color[] colors = new Color[mesh.vertexCount];

            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }

            mesh.colors = colors.ToArray();
        }

        /// <summary>
        /// Will set the color of a mesh in groups. All the groups must be of equal size. Ex, if the mesh has 12 vertices, and the group size is 4, then the color list must be of size 12.
        /// </summary>
        /// <param name="aMesh"></param>
        /// <param name="colors">The colors for each respective group</param>
        /// <param name="groupCount"> The number of vertices in one group</param>
        public static void SetGroupColors(this Mesh aMesh, Color[] colors,
            int groupCount)
        {
            if (aMesh.vertexCount % groupCount != 0)
            {
                throw new ArgumentNullException("Sizes of mesh vertices does not evenly fit into given group");
            }

            if (colors.Length % groupCount != 0)
            {
                throw new ArgumentNullException("Sizes of color array does not evenly fit into given group");
            }

            Color[] meshColors = new Color[aMesh.vertexCount];

            for (int i = 0; i < colors.Length; i++)
            {
                meshColors[i] = colors[i % groupCount];
            }

            aMesh.colors = colors.ToArray();
        }

        /// <summary>
        /// Makes the y property of the vector2Int the z property of a vector3int. The vector3Int y position is said to zero. 
        /// </summary>
        /// <param name="gridPosiotion"></param>
        /// <returns></returns>
        public static Vector3Int ToBoundsGridPos(this Vector2Int vector)
        {
            Vector3Int pos = new Vector3Int();

            pos.x = vector.x;
            pos.y = 0;
            pos.z = vector.y;

            return pos;
        }

        /// <summary>
        /// Makes the Z property of the vector3Int the y property of a vector3int.
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static Vector2Int ToGridPos(this Vector3Int vector)
        {
            Vector2Int pos = new Vector2Int();

            pos.x = vector.x;
            pos.y = vector.z;

            return pos;
        }
        public class GridPositionComparer : IEqualityComparer<Vector2Int>
        {
            public bool Equals(Vector2Int x, Vector2Int y)
            {
                return x.x == y.x && x.y == y.y;
            }

            public int GetHashCode(Vector2Int obj)
            {
                return obj.GetHashCode_Unique();
            }
        }


        /// <summary>
        /// A more unique hashcode for a vector2Int
        /// </summary>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static int GetHashCode_Unique(this Vector2Int vector)
        {
            unchecked
            {
                int hash = 1716777619;
                int multipler = 486187739;
                // Suitable nullity checks etc, of course :)
                hash = hash * multipler + vector.x.GetHashCode();
                hash = hash * multipler + vector.y.GetHashCode();
                hash = hash * multipler + vector.magnitude.GetHashCode();
                return hash;
            }
        }
        /// <summary>
        /// Compares grid positions. Smallest to Largest is from Bottom left to top right.
        /// Thus, the more left and the more down a position is, the smaller it is. 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static int CompareGridPosition(Vector2Int a, Vector2Int b)
        {
            if (a.y == b.y)
            {
                return a.x.CompareTo(b.x);
            }
            return a.y.CompareTo(b.y);
        }

        /// <summary>
        /// Overloading the '<' operatorCompares grid positions. Smallest to Largest is from Bottom left to top right.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsLessThan(this Vector2Int a, Vector2Int b)
        {
            int compare = CompareGridPosition(a, b);

            if (compare < 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Overloading the '>' operatorCompares grid positions. Smallest to Largest is from Bottom left to top right.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsGreaterThan(this Vector2Int a, Vector2Int b)
        {
            int compare = CompareGridPosition(a, b);

            if (compare > 0)
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// Overloading the '==' operatorCompares grid positions. Smallest to Largest is from Bottom left to top right.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool IsThesameAs(this Vector2Int a, Vector2Int b)
        {
            int compare = CompareGridPosition(a, b);

            if (compare == 0)
            {
                return true;
            }

            return false;
        }
    }
}