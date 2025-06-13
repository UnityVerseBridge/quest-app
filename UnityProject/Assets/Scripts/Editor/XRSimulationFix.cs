using UnityEditor;
using UnityEngine;
using System.IO;
#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace UnityVerseBridge.Quest.Editor
{
    /// <summary>
    /// Fixes XR Simulation asset conflicts during build
    /// </summary>
#if UNITY_2018_1_OR_NEWER
    public class XRSimulationFix : IPreprocessBuildWithReport, IPostprocessBuildWithReport
#else
    public class XRSimulationFix
#endif
    {
        public int callbackOrder => -1000; // Run early

        private const string XR_TEMP_PATH = "Assets/XR/Temp";
        private const string XR_RESOURCES_PATH = "Assets/XR/Resources";
        private const string XR_USER_SETTINGS_PATH = "Assets/XR/UserSimulationSettings/Resources";

#if UNITY_2018_1_OR_NEWER
        public void OnPreprocessBuild(BuildReport report)
#else
        public void OnPreprocessBuild()
#endif
        {
            // Clean up any existing temp files
            CleanupTempFiles();
            
            // Ensure XR directories exist
            EnsureDirectoryExists("Assets/XR");
            EnsureDirectoryExists(XR_TEMP_PATH);
        }

#if UNITY_2018_1_OR_NEWER
        public void OnPostprocessBuild(BuildReport report)
#else
        public void OnPostprocessBuild()
#endif
        {
            // Clean up after build
            CleanupTempFiles();
        }

        private void CleanupTempFiles()
        {
            // Clean up temp directory
            if (Directory.Exists(XR_TEMP_PATH))
            {
                string[] tempFiles = Directory.GetFiles(XR_TEMP_PATH, "*.asset");
                foreach (string file in tempFiles)
                {
                    try
                    {
                        File.Delete(file);
                        File.Delete(file + ".meta");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[XRSimulationFix] Could not delete temp file: {file} - {e.Message}");
                    }
                }
            }

            // Clean up duplicate XRSimulation files if they exist
            CleanupDuplicateFile(XR_RESOURCES_PATH, "XRSimulationRuntimeSettings.asset");
            CleanupDuplicateFile(XR_USER_SETTINGS_PATH, "XRSimulationPreferences.asset");
        }

        private void CleanupDuplicateFile(string directory, string filename)
        {
            if (!Directory.Exists(directory)) return;

            string filePath = Path.Combine(directory, filename);
            if (File.Exists(filePath))
            {
                try
                {
                    // Check if this is a duplicate (0 bytes or very small)
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length < 100) // Likely a placeholder
                    {
                        File.Delete(filePath);
                        File.Delete(filePath + ".meta");
                        Debug.Log($"[XRSimulationFix] Removed duplicate file: {filePath}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[XRSimulationFix] Could not check/delete file: {filePath} - {e.Message}");
                }
            }
        }

        private void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }

    /// <summary>
    /// Menu item to manually clean up XR Simulation conflicts
    /// </summary>
    public static class XRSimulationCleanupMenu
    {
        [MenuItem("UnityVerseBridge/Fix/Clean XR Simulation Conflicts")]
        public static void CleanXRSimulationConflicts()
        {
            var fix = new XRSimulationFix();
#if UNITY_2018_1_OR_NEWER
            fix.OnPreprocessBuild(null);
#else
            fix.OnPreprocessBuild();
#endif
            
            AssetDatabase.Refresh();
            Debug.Log("[XRSimulationFix] XR Simulation conflicts cleaned up");
        }
    }
}