using System;
using System.IO;
using UnityEngine;

namespace BestPractices.Setup
{
    /// <summary>
    /// Setup parameters written by <c>setup-project.sh</c> to
    /// <c>Assets/_BestPracticesSetup/setup-config.json</c> before the Editor runs headlessly.
    /// </summary>
    [Serializable]
    public class SetupConfig
    {
        public string projectName;
        public string[] samples;
        public bool fixVSCodeSlnx;

        private const string ConfigPath = "Assets/_BestPracticesSetup/setup-config.json";

        /// <summary>Reads and parses <c>setup-config.json</c> from the staged setup folder.</summary>
        public static SetupConfig Load()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", ConfigPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[BestPractice] Setup config not found at {ConfigPath}");
                return new SetupConfig();
            }

            string json = File.ReadAllText(fullPath);
            return JsonUtility.FromJson<SetupConfig>(json);
        }
    }
}
