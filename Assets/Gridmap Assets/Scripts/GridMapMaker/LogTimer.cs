using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Assets.Scripts.Miscellaneous.ExtensionMethods;
using Debug = UnityEngine.Debug;

namespace Assets.Gridmap_Assets.Scripts.Miscellaneous
{
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
        /// Starts the timer with the given id. If it doesnt exist one is created
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

        public static void StopTimer(int id)
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

            msg = addOn + msg + "\t\t";

            Stopwatch sw;

            timers.TryGetValue(id, out sw);

            return ParseLogTimer(msg, sw.ElapsedMilliseconds);
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

        public static void LogAll()
        {
            string fullLog = "";
            foreach(int id in timers.Keys.ToList())
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
