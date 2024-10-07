using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace GridMapMaker.Tutorial
{
    [Serializable]
    public class NoiseGenerator : MonoBehaviour
    {
        [Header("Landscape Noise")]

        public NoiseSettings landNoiseSettings;

        [Header("Rain Noise")]

        public NoiseSettings rainNoiseSettings;

        [Header("Temperature Noise")]

        public NoiseSettings tempNoiseSettings;

        float[,] landValues;
        float[,] rainValues;
        float[,] tempValues;

        AverageValues avg;
        public bool NoiseModified;

        public bool printAverage = false;
        private void OnValidate()
        {
            // everytime a value is changed, the noise is modified, thus we need to recompute the noise
            NoiseModified = true;
        }

        public void ComputeNoises(Vector2Int planetSize)
        {
            // if the noise settings are not modified, then no need to recompute the noise
            if(!NoiseModified && planetSize == landNoiseSettings.PlanetSize)
            {
                return;
            }

            landNoiseSettings.PlanetSize = planetSize;
            rainNoiseSettings.PlanetSize = planetSize;
            tempNoiseSettings.PlanetSize = planetSize;

            landNoiseSettings.Init();
            rainNoiseSettings.Init();
            tempNoiseSettings.Init();

            avg = new AverageValues(planetSize.x * planetSize.y);
            avg.Init("Land");
            avg.Init("Rain");
            avg.Init("Temp");

            landValues = new float[planetSize.x, planetSize.y];
            rainValues = new float[planetSize.x, planetSize.y];
            tempValues = new float[planetSize.x, planetSize.y];

            Parallel.For(0, planetSize.x, i =>
            {
                for (int j = 0; j < planetSize.y; j++)
                {
                    ComputeLandNoise(i, j);
                    ComputeRainNoise(i, j);
                    ComputeTempNoise(i, j);
                }
            });

            NoiseModified = false;

            if(printAverage)
            {
                Debug.Log(avg.GetString());
            }
        }

        private void ComputeLandNoise(int x, int y)
        {
            landValues[x, y] = landNoiseSettings.GetNoise(x, y);

            avg.Add("Land", landValues[x, y]);
        }
        private void ComputeRainNoise(int x, int y)
        {
            rainValues[x, y] = rainNoiseSettings.GetNoise(x, y);
            avg.Add("Rain", rainValues[x, y]);
        }
        private void ComputeTempNoise(int x, int y)
        {
            tempValues[x, y] = tempNoiseSettings.GetNoise(x, y);
            avg.Add("Temp", tempValues[x, y]);
        }

        public float GetLandNoise(int x, int y)
        {
            return landValues[x, y];
        }
        public float GetRainNoise(int x, int y)
        {
            return rainValues[x, y];
        }

        public float GetTempNoise(int x, int y)
        {
            return tempValues[x, y];
        }

        public string GetPercentString()
        {
            return avg.GetString();
        }


        [Serializable]
        public struct NoiseSettings
        {
            FastNoiseLite noiseGenerator;
            public FastNoiseLite.FractalType fractalType;
            public FastNoiseLite.NoiseType noiseType;

            public int seed;
            public float frequency;
            public float multiplier;
            public float scale;

            [Range(1, 20)]
            [SerializeField] int fractal;

            [Range(0, 1)]
            [SerializeField] float minValue;

            [Range(0, 1)]
            [SerializeField] float maxValue;

            public Vector2Int offset;

            public Vector2Int PlanetSize { get; set; } 
            // code this part

            public void Init()
            {
                noiseGenerator = new FastNoiseLite(seed);
                noiseGenerator.SetFractalType(fractalType);
                noiseGenerator.SetNoiseType(noiseType);
                noiseGenerator.SetFrequency(frequency);
            }
            public float GetNoise(int x, int y)
            {
                float tempNoise = noiseGenerator.GetNoise((float)x / (PlanetSize.x + offset.x), (float)y / (PlanetSize.y + offset.y));

                tempNoise *= multiplier;

                tempNoise = Mathf.Clamp(tempNoise, minValue, maxValue);

                return tempNoise;
            }

            public float GetNoise2(int x, int y)
            {
                float tempNoise = noiseGenerator.GetNoise((float)x / (PlanetSize.x + offset.x), (float)y / (PlanetSize.y + offset.y));

                tempNoise *= multiplier;

                tempNoise = Mathf.Clamp(tempNoise, minValue, maxValue);

                return tempNoise;
            }
        }

        public struct AverageValues
        {
            public Dictionary<string, List<int>> percents;

            public int max;
            public AverageValues(int max)
            {
                percents = new Dictionary<string, List<int>>();

                this.max = max;
            }
            public void Add(string k, float num)
            {
                int index = (int)(num * 10);

                index = Mathf.Clamp(index, 0, 9);

                percents[k][index]++;
            }

            public void Init(string k)
            {
                percents.Add(k, new List<int>());

                for (int i = 0; i < 10; i++)
                {
                    percents[k].Add(0);
                }
            }

            public string GetString()
            {
                string str = "";

                foreach (string key in percents.Keys)
                {
                    str += key + ": ";

                    foreach (int i in percents[key])
                    {
                        float p = (float)i / max;

                        str += p.ToString("F") + " --- ";
                    }

                    str += "\n";
                }

                return str;
            }
        }
    }
}
