using UnityEngine;
using UnityVerseBridge.Core;
using System.Collections;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// Debug helper script to diagnose touch input issues on Quest
    /// </summary>
    public class TouchDebugHelper : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = true;
        [SerializeField] private float debugInterval = 2f;
        
        [Header("Component References")]
        [SerializeField] private UnityVerseBridgeManager bridgeManager;
        [SerializeField] private TouchInputHandler touchHandler;
        [SerializeField] private WebRtcManager webRtcManager;
        
        [Header("Status")]
        [SerializeField] private bool isHost = false;
        [SerializeField] private bool isConnected = false;
        [SerializeField] private bool hasVRCamera = false;
        [SerializeField] private bool hasTouchCanvas = false;
        [SerializeField] private string currentStatus = "Not initialized";
        
        private float lastDebugTime;
        
        void Start()
        {
            StartCoroutine(InitializeDebugHelper());
        }
        
        IEnumerator InitializeDebugHelper()
        {
            yield return new WaitForSeconds(1f);
            
            // Find components
            if (bridgeManager == null)
                bridgeManager = FindFirstObjectByType<UnityVerseBridgeManager>();
                
            if (touchHandler == null)
                touchHandler = FindFirstObjectByType<TouchInputHandler>();
                
            if (webRtcManager == null)
                webRtcManager = FindFirstObjectByType<WebRtcManager>();
                
            if (bridgeManager == null)
            {
                UnityEngine.Debug.LogError("[TouchDebugHelper] UnityVerseBridgeManager not found!");
                currentStatus = "Bridge Manager not found";
                yield break;
            }
            
            // Check if we're in Host mode
            isHost = bridgeManager.Role == PeerRole.Host;
            
            if (!isHost)
            {
                UnityEngine.Debug.Log("[TouchDebugHelper] Not in Host mode, debug helper disabled");
                currentStatus = "Not in Host mode";
                enabled = false;
                yield break;
            }
            
            UnityEngine.Debug.Log("[TouchDebugHelper] Running in Host mode, starting debug monitoring");
            currentStatus = "Host mode - monitoring";
        }
        
        void Update()
        {
            if (!enableDebugLogging) return;
            if (Time.time - lastDebugTime < debugInterval) return;
            
            lastDebugTime = Time.time;
            PerformDebugCheck();
        }
        
        void PerformDebugCheck()
        {
            if (bridgeManager == null) return;
            
            // Update connection status
            isConnected = bridgeManager.IsConnected;
            
            // Check VR Camera
            var vrCameraField = typeof(UnityVerseBridgeManager).GetField("vrCamera", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var vrCamera = vrCameraField?.GetValue(bridgeManager) as Camera;
            hasVRCamera = vrCamera != null;
            
            // Check Touch Canvas
            if (touchHandler != null)
            {
                var canvasField = typeof(TouchInputHandler).GetField("touchCanvas", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var touchCanvas = canvasField?.GetValue(touchHandler) as Canvas;
                hasTouchCanvas = touchCanvas != null && touchCanvas.gameObject.activeInHierarchy;
            }
            
            // Log status
            UnityEngine.Debug.Log($"[TouchDebugHelper] Status Check:\n" +
                     $"- Connected: {isConnected}\n" +
                     $"- VR Camera: {(hasVRCamera ? "Found" : "Missing")}\n" +
                     $"- Touch Canvas: {(hasTouchCanvas ? "Found" : "Missing")}\n" +
                     $"- Touch Handler: {(touchHandler != null ? "Found" : "Missing")}\n" +
                     $"- WebRTC Manager: {(webRtcManager != null ? "Found" : "Missing")}");
                     
            if (hasVRCamera && vrCamera != null)
            {
                UnityEngine.Debug.Log($"[TouchDebugHelper] VR Camera Details:\n" +
                         $"- Name: {vrCamera.name}\n" +
                         $"- Active: {vrCamera.gameObject.activeInHierarchy}\n" +
                         $"- FOV: {vrCamera.fieldOfView}\n" +
                         $"- Position: {vrCamera.transform.position}");
            }
            
            // Update status
            if (!isConnected)
                currentStatus = "Not connected";
            else if (!hasVRCamera)
                currentStatus = "VR Camera missing!";
            else if (!hasTouchCanvas)
                currentStatus = "Touch Canvas missing!";
            else
                currentStatus = "All systems OK - waiting for touch";
        }
        
        // This will be called by TouchInputHandler when touch data is received
        public void OnTouchReceived(string touchInfo)
        {
            UnityEngine.Debug.Log($"[TouchDebugHelper] Touch received: {touchInfo}");
            currentStatus = $"Touch received: {touchInfo}";
        }
    }
}