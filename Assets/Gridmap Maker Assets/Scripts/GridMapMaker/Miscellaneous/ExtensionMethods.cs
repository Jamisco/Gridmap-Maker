using System.Collections.Generic;
using UnityEngine;

namespace GridMapMaker
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts the Camera positions to Bounds. Useful if you want to check if the camera is within a certain area
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Bounds OrthographicBounds3D(this Camera camera)
        {
            float screenAspect = camera.aspect;
            float cameraHeight = camera.orthographicSize * 2;

            Vector3 position = camera.transform.localPosition;

            position.y = 0.01f;

            Bounds bounds = new Bounds(position,
                            new Vector3(cameraHeight * screenAspect,0, cameraHeight));
            return bounds;
        }

        /// <summary>
        /// Used to log the time it took to complete a task. Will return a formatted string with the time in minutes and seconds
        /// </summary>
        /// <param name="message"></param>
        /// <param name="timeInMilliseconds"></param>
        /// <returns></returns>
        public static string ParseLogTimer(string message, float timeInMilliseconds)
        {
            int minutes;
            float seconds;

            string log = "";

            if (timeInMilliseconds >= 60000)
            {
                minutes = (int)(timeInMilliseconds / 60000);
                seconds = timeInMilliseconds % 60000 / 1000f;
                log = $"{message} {minutes} minutes {seconds} seconds";
            }
            else
            {
                string time = timeInMilliseconds / 1000f + "";
                string spacer = " ".PadRight(10, ' ');
                log = $"{message} {spacer} {time} seconds";
            }

            return log;
        }
        /// <summary>
        /// A more unique hashcode for a vector2Int which significantly less collisions than the default hashcode.
        /// The max values for x and y are 65534 individually. Thus the max map size is 65534 x 65534, which is more than enough for most use cases. Going higher will result in a collision
        /// </summary>
        /// <param timerName="vector"></param>
        /// <returns></returns>
        public static int GetHashCode_Unique(this Vector2Int vector)
        {
            int hash = (System.UInt16)vector.x << 16 | (System.UInt16)vector.y & 0xFFFF;
            return hash;
        }
        /// <summary>
        /// Given a property type, will return the value of the property from the MaterialPropertyBlock
        /// </summary>
        /// <param name="propBlock"></param>
        /// <param name="propertyName"></param>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public static object GetValue(this MaterialPropertyBlock propBlock, string propertyName, MaterialPropertyType propertyType)
        {
            switch (propertyType)
            {
                case MaterialPropertyType.Float:

                    return propBlock.GetFloat(propertyName);

                case MaterialPropertyType.Int:

                    return propBlock.GetInt(propertyName);

                case MaterialPropertyType.Vector:

                    return propBlock.GetVector(propertyName);

                case MaterialPropertyType.Matrix:

                    return propBlock.GetMatrix(propertyName);
                case MaterialPropertyType.Texture:

                    return propBlock.GetTexture(propertyName);

                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Helper class to find the edges of a mesh. This is useful for finding the edges of a mesh for a polygon collider.
    /// Code taken from https://discussions.unity.com/t/get-outer-edge-vertices-c/145202/2
    /// </summary>
    public static class EdgeHelpers
    {
        public struct Edge
        {
            public int v1;
            public int v2;
            public int triangleIndex;
            public Edge(int aV1, int aV2, int aIndex)
            {
                v1 = aV1;
                v2 = aV2;
                triangleIndex = aIndex;
            }
        }

        public static List<Edge> GetEdges(int[] aIndices)
        {
            List<Edge> result = new List<Edge>();
            for (int i = 0; i < aIndices.Length; i += 3)
            {
                int v1 = aIndices[i];
                int v2 = aIndices[i + 1];
                int v3 = aIndices[i + 2];
                result.Add(new Edge(v1, v2, i));
                result.Add(new Edge(v2, v3, i));
                result.Add(new Edge(v3, v1, i));
            }
            return result;
        }

        public static List<Edge> FindBoundary(this List<Edge> aEdges)
        {
            List<Edge> result = new List<Edge>(aEdges);
            for (int i = result.Count - 1; i > 0; i--)
            {
                for (int n = i - 1; n >= 0; n--)
                {
                    if (result[i].v1 == result[n].v2 && result[i].v2 == result[n].v1)
                    {
                        // shared edge so remove both
                        result.RemoveAt(i);
                        result.RemoveAt(n);
                        i--;
                        break;
                    }
                }
            }
            return result;
        }

        public static List<Edge> SortEdges(this List<Edge> aEdges)
        {
            List<Edge> result = new List<Edge>(aEdges);
            for (int i = 0; i < result.Count - 2; i++)
            {
                Edge E = result[i];
                for (int n = i + 1; n < result.Count; n++)
                {
                    Edge a = result[n];
                    if (E.v2 == a.v1)
                    {
                        // in this case they are already in order so just continoue with the next one
                        if (n == i + 1)
                            break;
                        // if we found a match, swap them with the next one after "i"
                        result[n] = result[i + 1];
                        result[i + 1] = a;
                        break;
                    }
                }
            }
            return result;
        }
    }
}