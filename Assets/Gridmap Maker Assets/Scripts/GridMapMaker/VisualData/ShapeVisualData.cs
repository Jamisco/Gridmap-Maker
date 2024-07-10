using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GridMapMaker
{
    /// <summary>
    /// A base class for all visual data. This class is used to create visuals for shapes.
    /// </summary>
    [Serializable]
    public abstract class ShapeVisualData : IEquatable<ShapeVisualData>
    {
        [ShowOnlyField]
        [SerializeField]
        private string visualName = "";

        /// <summary>
        /// The reason we have this is because unity cannot serialize System.guid. So we have to convert it to a string and then back to a guid during serialization/deserialization.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        private string visualIdString;

        [SerializeField]
        private int visualHash;

        /// <summary>
        /// For faster comparison, Generate a visual hash code such that if two visual data are equal, they should have the same hash code.
        /// This Should return a hash code such that 2 visual data that look thesame should have thesame hash. Make sure the hashcode is unique only for a specific visual look.
        /// </summary>
        /// 
        public virtual string VisualName
        {
            get => visualName; protected set => visualName = value;
        }
        public int VisualHash { get => visualHash; protected set => visualHash = value; }

        /// <summary>
        /// Similar to getHashCode, if 2 visuals have thesame reference, they should be equal. This is a unique identifier for a visual data during serialization/deserilization
        /// </summary>
        public Guid VisualId { get; private set; }

        /// <summary>
        /// The hashcode of the visualID, cached for performance during insertion/comparisons.
        /// This is not persistent and will change per session.
        /// </summary>
        ///  
        public int VisualIdHash { get; private set; }

        public delegate void VisualDataChanged(ShapeVisualData sender);

        public event VisualDataChanged VisualDataChange;
        protected virtual MaterialPropertyBlock PropertyBlock { get; set; }

        [SerializeField]
        protected Shader shader;

        [SerializeField]
        public Color mainColor;

        [SerializeField]
        public Texture2D mainTexture;

        [SerializeField]
        [HideInInspector]
        private RenderMode dataRenderMode = RenderMode.Material;
        public RenderMode DataRenderMode { get => dataRenderMode; set => dataRenderMode = value; }
        /// <summary>
        /// The render mode of the visual data. This will determine where we should used a material or a color the render the shape. For example, the ColorVisualData has its renderMode set to MeshColor. Generally, you should use Material render mode. If you want to use a color, you should use the ColorVisualData class provided.
        /// </summary>
        public enum RenderMode { Material, MeshColor };

        /// <summary>
        /// The default name of a texture property in ALL unity shaders
        /// </summary>
        public static string mainTexProperty = "_MainTex";
        /// <summary>
        /// The default name of a color property in ALL unity shaders
        /// </summary>
        public static string mainColorProperty = "_Color";

        public const int DEFAULT_VISUAL_HASH = -1111111111;
        public ShapeVisualData()
        {
            VisualId = Guid.NewGuid();

            visualIdString = VisualId.ToString();
            VisualIdHash = visualIdString.GetHashCode();

            visualHash = DEFAULT_VISUAL_HASH;
        }

        /// <summary>
        /// Set the material properties of your shader in this method. This will be called when the visual data is being used to render a Shape
        /// </summary>
        /// 
        public abstract void SetMaterialPropertyBlock();

        /// <summary>
        /// Will check if the visual hash has changed. If the hash hash changed, it will raise OnVisualDataChanged event and then update the visual hash.
        /// </summary>
        public void ValidateVisualHash()
        {
            int oldHash = visualHash;
            int newHash = GetVisualHash();

            if (oldHash != newHash)
            {
                OnVisualDataChanged(this);
                visualHash = newHash;
            }
        }
        protected virtual void OnVisualDataChanged(ShapeVisualData sender)
        {
            VisualDataChange?.Invoke(this);
        }

        /// <summary>
        /// Will call the SetMaterialPropertyBlock method and then return a new ShapeRenderData object
        /// </summary>
        /// <returns></returns>
        public virtual ShapeRenderData GetShapeRenderData()
        {
            SetMaterialPropertyBlock();
            return new ShapeRenderData(shader, PropertyBlock, visualName);
        }
        /// <summary>
        /// Will return a shallow copy of the visual data. This is useful when you want to create a new visual data that looks thesame as the original and pass it around. The returned visual data will STILL SHARE REFERENCES. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T ShallowCopy<T>() where T : ShapeVisualData
        {
            return (T)MemberwiseClone();
        }
        /// <summary>
        /// This will get the render data of the visual data and then get the VisualHash of said data. This is an expensive operation and it is recommended you override and create your own hash code based on your visual data.
        /// </summary>
        /// <returns></returns>
        public virtual int GetVisualHash()
        {
            ShapeRenderData data = GetShapeRenderData();
            return data.GetVisualHash();
        }

        /// <summary>
        /// The hashcode is simply the hashcode of the visual id. This is used to quickly compare visuals. If 2 visuals have thesame reference, they should be equal.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return VisualIdHash;
        }
        /// <summary>
        /// Does a deep copy by serializing and deserializing the visual data. This is useful when you want to create a new visual data that looks thesame as the original. The returned visual data will NOT SHARE REFERENCES.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="container"></param>
        /// <returns></returns>
        public virtual ShapeVisualData DeepCopy(MapVisualContainer container)
        {
            ShapeVisualData copy = MemberwiseClone() as ShapeVisualData;

            copy.SerializeVisualData(container);
            copy.DeserializeVisualData(container);

            return copy;
        }
        public abstract ShapeVisualData DeepCopy();

        public virtual bool Equals(ShapeVisualData other)
        {
            return VisualIdHash == other.VisualIdHash;
        }
        /// <summary>
        /// Returns true if 2 visuals look thesame. This is a very expensive operation than the default equality check and should be used sparingly.
        /// Highly recommended to implement this if you also want to draw shapes that look the same as one. If implemented properly could significantly increase performance.
        /// If you implement this, you should also implement GetVisualEqualityHash too. 
        /// </summary>
        /// <param timerName="other"></param>
        /// <returns></returns>
        public bool VisuallyEquals(ShapeVisualData other)
        {
            return visualHash == other.visualHash;
        }


        private static string instStr = "instanceID";

        [SerializeField]
        [HideInInspector]
        protected string SerializedData;

        public virtual void SerializeVisualData(MapVisualContainer visualContainer = null)
        {
            // reset the data or else it would show up in the allLines variable list.
            // might be best to cache it and store it back into the data variable if this methods crashes
            SerializedData = "";
            string data = JsonUtility.ToJson(this, true);

            if (visualContainer == null)
            {
                SerializedData = data;
                return;
            }

            List<string> allLines = data.Split('\n').ToList();

            // find all lines that contain instance ID and modify them to contain the guid of the texture

            for (int i = 0; i < allLines.Count; i++)
            {
                if (allLines[i].Contains(instStr))
                {
                    string instanceValue = allLines[i].Split(' ').Last();

                    int instanceID = -1;

                    // if it doesnt parse, this means the instanceID is actually a field and not a value
                    if (int.TryParse(instanceValue, out instanceID))
                    {
                        if(instanceID == 0)
                        {
                            //value is null
                            continue;
                        }
                        string gid = visualContainer.GetGuid(instanceID).ToString();
                        //string gid = Guid.NewGuid().ToString();
                        allLines[i] = allLines[i].Replace(instanceValue, gid);
                    }
                }
            }

            // combine list back to one string

            SerializedData = string.Join("\n", allLines);
        }

        /// <summary>
        /// Returns a deserialized version of the visual data.
        /// It is recommended you override this and create your own deserialization method in order to avoid boxing and unboxing of the data.
        /// </summary>
        /// <param name="visualContainer"></param>
        /// <returns></returns>
        public virtual ShapeVisualData DeserializeVisualData(MapVisualContainer visualContainer = null)
        {
            List<string> allLines = SerializedData.Split('\n').ToList();

            // find all lines that contain instance ID and modify them to contain the guid of the texture

            for (int i = 0; i < allLines.Count; i++)
            {
                if (allLines[i].Contains(instStr))
                {
                    string instanceValue = allLines[i].Split(' ').Last();

                    Guid gid = Guid.Empty;

                    // if it doesnt parse, this means the instanceID is actually a field and not a value
                    if (Guid.TryParse(instanceValue, out gid))
                    {
                        int id = visualContainer.GetObjectId(gid);
                        //string gid = Guid.NewGuid().ToString();
                        allLines[i] = allLines[i].Replace(instanceValue, id.ToString());
                    }
                }
            }

            string data = string.Join("\n", allLines);

            ShapeVisualData vData = JsonUtility.FromJson(data, GetType()) as ShapeVisualData;

            vData.VisualId = Guid.Parse(vData.visualIdString);
            vData.VisualIdHash = vData.visualIdString.GetHashCode();

            return vData;
        }
    }
}
