using Assets.Worldmap.VisualDatas;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GridMapMaker.Tutorial
{
    /// <summary>
    /// The Ecosphere used by the Worldmap Tutorial Class. Used to map specific VisualData to specific grid points
    /// </summary>
    public class Ecosphere : MonoBehaviour
    {
        [SerializeField]
        public LandscapeProperties lProps;

        Dictionary<int, ShapeVisualData> biomeData = new Dictionary<int, ShapeVisualData>();

        Dictionary<int, ShapeVisualData> snowBiomeData = new Dictionary<int, ShapeVisualData>();

        BiomeBlockValues bbv;

        WaterVisualData lakeVdata;
        WaterVisualData seaVData;
        WaterVisualData oceanVData;
        LavaVisualData lavaVData;

        public struct BiomeBlockValues
        {
            public BiomeBlockValues(int i = 0)
            {
                landPos = new List<Vector2Int>();
                landData = new List<ShapeVisualData>();

                snowPos = new List<Vector2Int>();
                snowData = new List<ShapeVisualData>();
            }

            public List<Vector2Int> landPos { get; set; }
            public List<ShapeVisualData> landData { get; set; }
            public List<Vector2Int> snowPos { get; set; }
            public List<ShapeVisualData> snowData { get; set; }
        }

        public void SetBiomeData(NoiseGenerator noiseGen, Vector2Int planetSize)
        {
            float land, rain, temp;
            biomeData.Clear();
            usedVData.Clear();
            snowBiomeData.Clear();
            usedSnowVData.Clear();

            bbv = new BiomeBlockValues(1);

            oceanVData = new WaterVisualData(lProps.oceanMaterial);

            lavaVData = new LavaVisualData(lProps.lavaVisualData);

            Vector2Int pos;
            ShapeVisualData tData;
            ShapeVisualData sData;

            for (int x = 0; x < planetSize.x; x++)
            {
                for (int y = 0; y < planetSize.y; y++)
                {
                    land = noiseGen.GetLandNoise(x, y);
                    rain = noiseGen.GetRainNoise(x, y);
                    temp = noiseGen.GetTempNoise(x, y);

                    pos = new Vector2Int(x, y);

                    tData = GetBiome(land, rain, temp);
                    sData = GetSnowBiome(temp);

                    bbv.landPos.Add(pos);
                    bbv.landData.Add(tData);

                    biomeData.Add(pos.GetHashCode_Unique(), tData);

                    snowBiomeData.Add(pos.GetHashCode_Unique(), sData);

                    bbv.snowPos.Add(pos);
                    bbv.snowData.Add(sData);
                }
            }
        }

        public Dictionary<int, ShapeVisualData> usedVData = new Dictionary<int, ShapeVisualData>();
        public Dictionary<int, ShapeVisualData> usedSnowVData = new Dictionary<int, ShapeVisualData>();

        private float Round(float number)
        {
            // round to 2 decimal places
            number = (float)Math.Round(number, 2);

            float rf = roundingFactor / 100f;
            return (float)Math.Round(number / rf) * rf;
        }

        private ShapeVisualData GetSnowBiome(float temp)
        {
            if (temp <= lProps.snowThreshhold)
            {
                float normalize = 1 - temp / lProps.snowThreshhold;
                int te = Math.Clamp(Mathf.RoundToInt(normalize * 10), 0, 10);

                if (usedSnowVData.ContainsKey(te))
                {
                    return usedSnowVData[te];
                }
                else
                {
                    SnowVisualData svd = new SnowVisualData(lProps.snowShader, te / 10f);

                    usedSnowVData.Add(te, svd);

                    return svd;
                }
            }

            return null;
        }

        [Tooltip("Rounding factor for the biome data. Used to group noise values. For example, if you have 2 noise values .82 and .81, and your Rounding factor is 5, these noise values will be rounded to the next .05 values, thus, the noise value will actually be .80. A low rounding factor means more unique values vice value of high.")]
        [Range(1, 50)]
        public int roundingFactor = 1;

        private ShapeVisualData GetBiome(float land, float rain, float temp)
        {
            if (land < lProps.waterThreshold)
            {
                return oceanVData;
            }
            else
            {
                rain = Round(rain);
                temp = Round(temp);

                if (rain <= .1 && temp >= .85 && land >= .85)
                {
                    return lavaVData;
                }

                (float, float) rt = (rain, temp);

                if (usedVData.ContainsKey(rt.GetHashCode()))
                {
                    return usedVData[rt.GetHashCode()];
                }

                LandVisualData v = new LandVisualData(temp, rain, lProps.landShader);

                usedVData.Add(rt.GetHashCode(), v);

                return v;
            }
        }
        public ShapeVisualData GetBiomeVData(Vector2Int pos)
        {
            return biomeData[pos.GetHashCode_Unique()];
        }

        public ShapeVisualData GetSnowBiomeVData(Vector2Int pos)
        {
            return snowBiomeData[pos.GetHashCode_Unique()];
        }

        public Color GetBiomeColor(Vector2Int pos)
        {
            return biomeData[pos.GetHashCode_Unique()].mainColor;
        }

        public BiomeBlockValues GetBiomeData()
        {
            return bbv;
        }

        [Serializable]
        public struct LandscapeProperties
        {
            public Material oceanMaterial;

            public Shader snowShader;
            public Shader landShader;
            public Shader lavaVisualData;

            public float waterThreshold;
            public float snowThreshhold;
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Ecosphere))]
        public class ClassButtonEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                Ecosphere myScript = (Ecosphere)target;


            }
        }
#endif
    }
}
