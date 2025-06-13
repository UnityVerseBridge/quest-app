using UnityEditor;
using UnityEngine;
using System;

namespace UnityVerseBridge.Quest.Editor
{
    /// <summary>
    /// Suppresses Tutorial Framework warnings during runtime and build
    /// </summary>
    public static class TutorialFrameworkSuppressor
    {
        private static bool isInitialized = false;
        private static LogType originalLogType;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RuntimeInitialize()
        {
            SuppressTutorialWarnings();
        }
        
        public static void SuppressTutorialWarnings()
        {
            if (isInitialized) return;
            
            // Store original log type
            originalLogType = Debug.unityLogger.filterLogType;
            
            // Add log handler to filter Tutorial Framework messages
            Application.logMessageReceived += HandleLog;
            isInitialized = true;
        }
        
        private static void HandleLog(string logString, string stackTrace, LogType type)
        {
            // Filter out Tutorial Framework instantiation errors
            if (logString.Contains("BuildStartedCriterion") && 
                logString.Contains("must be instantiated using the ScriptableObject.CreateInstance"))
            {
                // Suppress this specific error
                return;
            }
        }
        
        public static void RestoreLogging()
        {
            if (!isInitialized) return;
            
            Application.logMessageReceived -= HandleLog;
            Debug.unityLogger.filterLogType = originalLogType;
            isInitialized = false;
        }
    }
}