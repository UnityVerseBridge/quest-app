using UnityEngine;
using UnityVerseBridge.Core.Utils;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Adapter for QuestDebugLogger that uses the core OnScreenDebugger
    /// This maintains backward compatibility while using the core implementation
    /// </summary>
    public class QuestDebugLoggerAdapter : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private string[] filterKeywords = new string[] { "VrStreamSender", "WebRtc", "RenderTexture", "VideoStreamTrack", "Quest" };
        
        void Start()
        {
            // Create OnScreenDebugger instance
            var debugger = OnScreenDebugger.Instance;
            
            // Apply Quest-specific filters
            if (filterKeywords != null && filterKeywords.Length > 0)
            {
                debugger.SetFilter(filterKeywords, false); // false = show only logs with these keywords
            }
            
            // Show the debugger
            OnScreenDebugger.Show();
            
            Debug.Log("[QuestDebugLoggerAdapter] OnScreenDebugger initialized with Quest filters");
        }
        
        // Static wrapper methods for backward compatibility
        public static void Log(string message)
        {
            OnScreenDebugger.Log(message);
        }
        
        public static void LogError(string message)
        {
            OnScreenDebugger.LogError(message);
        }
        
        public static void LogWarning(string message)
        {
            OnScreenDebugger.LogWarning(message);
        }
    }
}