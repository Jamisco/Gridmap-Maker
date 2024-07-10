using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Debug = UnityEngine.Debug;

namespace GridMapMaker
{
    /// <summary>
    /// A simply static class used to log the time it takes to do something.
    /// </summary>
    public static class TimeLogger
    {
        static Dictionary<int, Stopwatch> timers = new Dictionary<int, Stopwatch>();
        static Dictionary<int, string> timerName = new Dictionary<int, string>();

        /// <summary>
        /// Will add a new timer. If said timer already exist, the previous timer will be left alone, but its layerName will change
        /// </summary>
        /// <param layerName="id"></param>
        /// <param layerName="name"></param>
        public static void InsertTimer(int id, string name = "")
        {
            if (!timers.ContainsKey(id))
            {
                timers.Add(id, new Stopwatch());
                timerName.Add(id, name);
            }
            else
            {
                timerName[id] = name;
            }
        }

        /// <summary>
        /// Starts the timer with the given id. If it doesnt exist one is created. If it does
        /// </summary>
        /// <param layerName="id"></param>
        /// <param layerName="name"></param>
        public static void StartTimer(int id, string name = "")
        {
            if (!timers.ContainsKey(id))
            {
                InsertTimer(id, name);
            }

            timers[id].Start();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="lazy"> The lazy string simply exist so you can copy/paste the StartTimer line and replace with StopTimer </param>
        public static void StopTimer(int id, string lazy = "")
        {
            if (!timers.ContainsKey(id))
            {
                return;
            }

            timers[id].Stop();
        }

        public static void StopAllTimers()
        {
            foreach (int id in timers.Keys.ToList())
            {
                timers[id].Stop();
            }
        }

        public static void ResetTimer(int id)
        {
            if (!timers.ContainsKey(id))
            {
                return;
            }

            timers[id].Reset();
        }

        public static void RemoveTimer(int id)
        {
            if (!timers.ContainsKey(id))
            {
                return;
            }

            timers.Remove(id);
        }

        private const int la = -20;
        private const int ra = 20;
        public static string GetLog(int id, string addOn = "")
        {
            if (!timers.ContainsKey(id))
            {
                return "Timer with " + id + " doesnt exist";
            }

            string msg = timerName[id] + " Took: ";

            if (msg == "")
            {
                msg = "Timer " + id + " took: ";
            }

            msg = addOn +  $"{msg}\t";

            Stopwatch sw;

            timers.TryGetValue(id, out sw);

            return ExtensionMethods.ParseLogTimer(msg, sw.ElapsedMilliseconds);
        }

        public static void Log(int id, string addOn = "")
        {
            if (!timers.ContainsKey(id))
            {
                return;
            }

            string str = GetLog(id, addOn);

            Debug.Log(str);
        }

        public static void LogAll(string title = "")
        {
            // give date and time in am and pm

            string date = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt");

            string fullLog = "Log Time: " + date + "\n";

            fullLog += (title != "") ? "Title: " + title + "\n" : "";

            fullLog += "\n";

            foreach (int id in timers.Keys.ToList())
            {
                string l = GetLog(id);

                fullLog += l + "\n";
            }

            Debug.Log(fullLog);
        }
        public static void ClearTimers()
        {
            timers.Clear();
            timerName.Clear();
        }


    }
}
