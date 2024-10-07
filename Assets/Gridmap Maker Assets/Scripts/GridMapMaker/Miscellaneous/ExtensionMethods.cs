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
}