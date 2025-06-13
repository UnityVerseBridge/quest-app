using UnityEditor;
using UnityEngine;
using System.Linq;

namespace UnityVerseBridge.Quest.Editor
{
    /// <summary>
    /// Handles URP-related warnings during build
    /// </summary>
    public static class URPBuildProcessor
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            // Check for URP Global Settings and fix if needed
            CheckURPGlobalSettings();
        }
        
        static void CheckURPGlobalSettings()
        {
            var globalSettings = Resources.FindObjectsOfTypeAll<ScriptableObject>()
                .Where(obj => obj.name == "UniversalRenderPipelineGlobalSettings")
                .FirstOrDefault();
                
            if (globalSettings != null)
            {
                Debug.Log("[URPBuildProcessor] Found URP Global Settings. If you see missing type warnings, " +
                    "please ensure you have the latest URP package installed via Package Manager.");
            }
        }
        
        [MenuItem("UnityVerseBridge/Fix/Check URP Configuration")]
        public static void CheckURPConfiguration()
        {
            bool hasURP = false;
            
            // Check if URP is installed
            #if UNITY_2019_3_OR_NEWER
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(URPBuildProcessor).Assembly);
            if (packageInfo != null)
            {
                Debug.Log($"[URPBuildProcessor] Package found: {packageInfo.displayName}");
            }
            #endif
            
            // Check for URP assemblies
            var urpAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name.Contains("Unity.RenderPipelines.Universal"));
                
            if (urpAssembly != null)
            {
                hasURP = true;
                Debug.Log("[URPBuildProcessor] URP is installed.");
            }
            else
            {
                Debug.LogWarning("[URPBuildProcessor] URP is not installed. " +
                    "Please install Universal RP package from Package Manager if you need it.");
            }
            
            // Check for missing types
            CheckURPGlobalSettings();
        }
    }
}