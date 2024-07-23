using Assets.Scripts.Procedural_Planet_Scripts;
using GridMapMaker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Procedural_Planet
{
    public class Biosphere : MonoBehaviour
    {
        [SerializeField]
        List<BiomeSettings> biomeSettings;

        public Shader mainShader;

        [SerializeField]
        [Range(0, 1)]
        public float landCutOff;

        public Color oceanColor = Color.blue;

        public string TexturePath = "Tutorial/Textures";
        Dictionary<Vector2Int, ShapeVisualData> biomeData = new Dictionary<Vector2Int, ShapeVisualData>();
        Texture2D[] GetTexturesFromPath()
        {
            string[] texturePaths = Directory.GetFiles(TexturePath, "*.png");

            List<Texture2D> textures = new List<Texture2D>();

            foreach (string path in texturePaths)
            {
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture>(path) as Texture2D;

                if (texture != null)
                {
                    textures.Add(texture);
                }
            }

            return textures.ToArray();
        }

        Texture2D[] defaultTextures;
        public void ValidateWithDefault()
        {
            biomeSettings.Clear();

            try
            {
                defaultTextures = GetTexturesFromPath();
            }
            catch (Exception)
            {
                Debug.LogError("Texture folder path is inaccurate. Please update the folder path in Biosphere script updating with default values");
                defaultTextures = null;
            }

            int c = 0;

            foreach (Biome biome in Enum.GetValues(typeof(Biome)))
            {
                BiomeSettings b = new BiomeSettings();
                b.name = biome;
                b.maxTempRainValue = DefaultBiomeTempRainValues[c];

                Texture2D tex = null;
                
                if(defaultTextures != null)
                {
                    tex = (c < defaultTextures.Count()) ? defaultTextures[c] : null;
                }
                
                b.texture = tex;

                b.SetAverageColor();

                biomeSettings.Add(b);
                c++;
            }

            orderedBiomes = biomeSettings.OrderBy(x => x.maxTempRainValue.x).ToList();
        }

        
        public void SetBiomeData(ref NoiseGenerator noiseGen, Vector2Int planetSize)
        {
            float land, rain, temp;
            biomeData.Clear();

            for (int x = 0; x < planetSize.x; x++)
            {
                for(int y = 0; y < planetSize.y; y++)
                {
                    land = noiseGen.GetLandNoise(x, y);
                    rain = noiseGen.GetRainNoise(x, y);
                    temp = noiseGen.GetTempNoise(x, y);

                    biomeData.Add(new Vector2Int(x, y), GetBiome(land, rain, temp));
                }
            }
        }

        List<BiomeSettings> orderedBiomes;
        private ShapeVisualData GetBiome(float land, float rain, float temp)
        {
            if (land < landCutOff)
            {
                return new ColorVisualData(mainShader, oceanColor);
            }

            temp = temp * 100;
            rain = rain * 100;

            if(orderedBiomes == null)
            {
                orderedBiomes = biomeSettings.OrderBy(x => x.maxTempRainValue.x).ToList();
            }

            List<BiomeSettings> biomeX = new List<BiomeSettings>();

            for (int i = 0; i < orderedBiomes.Count; i++)
            {
                BiomeSettings biomeSetting = orderedBiomes[i];
                if (temp <= biomeSetting.maxTempRainValue.x)
                {
                    biomeX.Add(biomeSetting);
                }
            }

            biomeX = biomeX.OrderBy(x => x.maxTempRainValue.y).ToList();

            BiomeSettings biome2Use = biomeX[0];

            for (int i = 0; i < biomeX.Count; i++)
            {
                BiomeSettings biomeSettings = biomeX[i];
                if (rain <= biomeSettings.maxTempRainValue.y)
                {
                    biome2Use = biomeSettings;
                    break;
                }
            }

            BasicVisual bv = new BasicVisual(mainShader, biome2Use.texture, biome2Use.averageColor);
            bv.DataRenderMode = ShapeVisualData.RenderMode.Material;

            return bv;
        }
        public ShapeVisualData GetBiomeVData(Vector2Int pos)
        {
            return biomeData[pos];
        }
        public Color GetBiomeColor(Vector2Int pos)
        {
            return biomeData[pos].mainColor;
        }

        public (List<Vector2Int>, List<ShapeVisualData>) GetBiomeData()
        {
            return (biomeData.Keys.ToList(), biomeData.Values.ToList());
        }

        public enum Landscape
        {
            Land,
            Lake,
            Sea,
            Ocean
        }

        public enum Biome
        {
            Tundra,
            ColdDesert,
            HotDesert,
            Taiga,
            TemperateGrassland,
            TropicalGrassland,
            TemperateSeasonalForest,
            TropicalSeasonalForest,
            TemperateRainForest,
            TropicalRainForest
        }

        // these are default values for the biomes. They are in the order of the Biome Enum.
        // you can change them in editor if you wish
        public static Vector2Int[] DefaultBiomeTempRainValues =
        {
            new Vector2Int(35, 20),
            new Vector2Int(77, 8),
            new Vector2Int(100, 17),

            new Vector2Int(47, 37),
            new Vector2Int(78, 25),
            new Vector2Int(100, 37),

            new Vector2Int(78, 43),
            new Vector2Int(100, 60),
            new Vector2Int(78, 82),

            new Vector2Int(100, 100),
        };

        [Serializable]
        public struct BiomeSettings
        {
            public Biome name;

/// <summary>
            /// Value denoting Temperature and Rain. X = Temperature, Y = Rain. On a scale of 0 to 100 for both.
            /// </summary>
            public Vector2Int maxTempRainValue;

            public Texture2D texture;

            public Color averageColor;

            /// <summary>
            /// The average color of the texture. This is used to color the biome in the planet.
            /// </summary>
            public void SetAverageColor()
            {
                if(texture == null)
                {
                    this.averageColor = Color.white;
                    return;
                }

                // make sure in the import settings of your texture, under advanced settings
                // read/write is enabled
                Color[] pixels = texture.GetPixels();
                Color averageColor = Color.black;

                foreach (Color pixel in pixels)
                {
                    averageColor += pixel;
                }

                averageColor /= pixels.Length;

                this.averageColor = averageColor;
            }
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(Biosphere))]
        public class ClassButtonEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                Biosphere myScript = (Biosphere)target;

                if (GUILayout.Button("Validate with Default Values"))
                {
                    myScript.ValidateWithDefault();
                }
            }
        }
#endif
    }
}
