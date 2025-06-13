using UnityEngine;
using System.Collections;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// VR과 일반 화면에서 실제 Screen 크기를 확인하는 디버거
    /// </summary>
    public class ScreenSizeDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool logEveryFrame = false;
        [SerializeField] private float logInterval = 2f;
        
        private float lastLogTime;
        
        void Start()
        {
            LogScreenInfo("Start");
            StartCoroutine(DelayedLog());
        }
        
        IEnumerator DelayedLog()
        {
            // Wait for VR initialization
            yield return new WaitForSeconds(2f);
            LogScreenInfo("After 2 seconds");
        }
        
        void Update()
        {
            if (logEveryFrame || (Time.time - lastLogTime > logInterval))
            {
                lastLogTime = Time.time;
                LogScreenInfo("Update");
            }
        }
        
        void LogScreenInfo(string context)
        {
            UnityEngine.Debug.Log($"\n=== SCREEN SIZE DEBUG [{context}] ===");
            UnityEngine.Debug.Log($"Platform: {Application.platform}");
            UnityEngine.Debug.Log($"Screen.width: {Screen.width}");
            UnityEngine.Debug.Log($"Screen.height: {Screen.height}");
            UnityEngine.Debug.Log($"Screen.currentResolution: {Screen.currentResolution.width}x{Screen.currentResolution.height}@{Screen.currentResolution.refreshRate}Hz");
            UnityEngine.Debug.Log($"Screen.dpi: {Screen.dpi}");
            UnityEngine.Debug.Log($"Screen.orientation: {Screen.orientation}");
            
            if (Camera.main != null)
            {
                UnityEngine.Debug.Log($"\n--- Main Camera ---");
                UnityEngine.Debug.Log($"Camera.pixelWidth: {Camera.main.pixelWidth}");
                UnityEngine.Debug.Log($"Camera.pixelHeight: {Camera.main.pixelHeight}");
                UnityEngine.Debug.Log($"Camera.pixelRect: {Camera.main.pixelRect}");
                UnityEngine.Debug.Log($"Camera.aspect: {Camera.main.aspect}");
                UnityEngine.Debug.Log($"Camera.fieldOfView: {Camera.main.fieldOfView}");
                UnityEngine.Debug.Log($"Camera.stereoEnabled: {Camera.main.stereoEnabled}");
                UnityEngine.Debug.Log($"Camera.targetTexture: {Camera.main.targetTexture}");
                
                if (Camera.main.targetTexture != null)
                {
                    var rt = Camera.main.targetTexture;
                    UnityEngine.Debug.Log($"RenderTexture size: {rt.width}x{rt.height}");
                    UnityEngine.Debug.Log($"RenderTexture format: {rt.format}");
                }
            }
            
            // Check all cameras
            Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            UnityEngine.Debug.Log($"\n--- All Cameras ({allCameras.Length}) ---");
            foreach (var cam in allCameras)
            {
                UnityEngine.Debug.Log($"{cam.name}: {cam.pixelWidth}x{cam.pixelHeight}, Active: {cam.gameObject.activeInHierarchy}, Enabled: {cam.enabled}");
            }
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            UnityEngine.Debug.Log($"\n--- XR Settings ---");
            UnityEngine.Debug.Log($"XR.enabled: {UnityEngine.XR.XRSettings.enabled}");
            UnityEngine.Debug.Log($"XR.isDeviceActive: {UnityEngine.XR.XRSettings.isDeviceActive}");
            UnityEngine.Debug.Log($"XR.loadedDeviceName: {UnityEngine.XR.XRSettings.loadedDeviceName}");
            UnityEngine.Debug.Log($"XR.eyeTextureWidth: {UnityEngine.XR.XRSettings.eyeTextureWidth}");
            UnityEngine.Debug.Log($"XR.eyeTextureHeight: {UnityEngine.XR.XRSettings.eyeTextureHeight}");
            UnityEngine.Debug.Log($"XR.renderViewportScale: {UnityEngine.XR.XRSettings.renderViewportScale}");
            #endif
            
            // Canvas info
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            UnityEngine.Debug.Log($"\n--- All Canvases ({allCanvases.Length}) ---");
            foreach (var canvas in allCanvases)
            {
                var rect = canvas.GetComponent<RectTransform>();
                UnityEngine.Debug.Log($"{canvas.name}: Mode={canvas.renderMode}, Size={rect.rect.width}x{rect.rect.height}, Camera={canvas.worldCamera?.name ?? "none"}");
            }
            
            UnityEngine.Debug.Log("=== END DEBUG ===\n");
        }
        
        void OnGUI()
        {
            int y = 250;
            int lineHeight = 20;
            
            GUI.Label(new Rect(10, y, 400, lineHeight), $"Screen: {Screen.width}x{Screen.height}");
            y += lineHeight;
            
            if (Camera.main != null)
            {
                GUI.Label(new Rect(10, y, 400, lineHeight), $"Camera: {Camera.main.pixelWidth}x{Camera.main.pixelHeight}");
                y += lineHeight;
                
                #if UNITY_ANDROID && !UNITY_EDITOR
                if (UnityEngine.XR.XRSettings.enabled)
                {
                    GUI.Label(new Rect(10, y, 400, lineHeight), $"XR Eye: {UnityEngine.XR.XRSettings.eyeTextureWidth}x{UnityEngine.XR.XRSettings.eyeTextureHeight}");
                    y += lineHeight;
                }
                #endif
            }
        }
    }
}