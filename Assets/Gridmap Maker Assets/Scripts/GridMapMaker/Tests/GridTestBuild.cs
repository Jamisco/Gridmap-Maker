using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace GridMapMaker.Tutorial
{
    /// <summary>
    /// This Script will make use of a Canvas UI group. 
    /// This script is used to test the Gridmanager and its subsequent classes and functions in a build environment
    /// </summary>
    public class GridTestBuild : MonoBehaviour
    {

        public GridTester grid;
        public Button button;

        public InputField gridSize;
        public InputField chunkSize;
        public Toggle multiThread;
        public Toggle visualEqual;
        public Toggle chunkActive;
        public Text generateTime;
        public InputField camText;

        public Button saveBtn;
        public Button loadBtn;

        public Text fpsText;

        Camera cam;
        private void Start()
        {
            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = 0;
            button.onClick.AddListener(OnButtonClick);
            saveBtn.onClick.AddListener(grid.SaveMap);
            loadBtn.onClick.AddListener(grid.LoadMap);

            cam = Camera.main;
        }

        private void OnButtonClick()
        {
            int gs = Convert.ToInt32(gridSize.text);
            int cs = Convert.ToInt32(chunkSize.text);
            bool mt = multiThread.isOn;
            bool ve = visualEqual.isOn;

            Stopwatch sw = Stopwatch.StartNew();

            grid.GenerateGrid(gs, cs, mt, ve);

            sw.Stop();

            generateTime.text = "Generate Time: " + ExtensionMethods.ParseLogTimer("", sw.ElapsedMilliseconds);
        }

        [SerializeField]
        public float updateInterval = 1000f; // Time in milliseconds between updates

        private float deltaTime = 0.0f;
        private float nextUpdate = 0.0f;

        int sds = 50;
        private void Update()
        {
            // show fps every x milliseconds

            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            cam.orthographicSize = Int32.TryParse(camText.text, out sds) ? sds : 50;

            // Check if it's time to update the display
            if (Time.time * 1000 > nextUpdate)
            {
                nextUpdate = Time.time * 1000 + updateInterval;
                float fps = 1.0f / deltaTime;
                fpsText.text = string.Format("{0:0.} fps", fps);
                grid.DisableUnseenChunk = chunkActive.isOn;
            }

            //fpsText.text = "FPS: " + (1 / Time.deltaTime).ToString("F2");
        }
    }
}
