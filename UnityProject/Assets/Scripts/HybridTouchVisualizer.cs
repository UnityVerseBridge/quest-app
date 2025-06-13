using UnityEngine;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.DataChannel.Data;
using System.Collections.Generic;
using TouchPhase = UnityVerseBridge.Core.DataChannel.Data.TouchPhase;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// 큐브(3D)와 OnGUI(2D)를 모두 사용하는 하이브리드 터치 시각화
    /// 큐브는 스트리밍에 포함되고, OnGUI는 로컬 디버깅용
    /// </summary>
    public class HybridTouchVisualizer : MonoBehaviour
    {
        [Header("3D Cube Settings")]
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private float cubeSize = 0.2f;
        [SerializeField] private float cubeDistance = 2f;
        [SerializeField] private Material cubeMaterial;
        
        [Header("Debug Settings")]
        [SerializeField] private bool showOnGUI = true;
        [SerializeField] private bool showDebugInfo = true;
        
        private Dictionary<int, TouchInfo> touches = new Dictionary<int, TouchInfo>();
        private WebRtcManager webRtcManager;
        private Camera mainCamera;
        
        private class TouchInfo
        {
            public Vector2 normalizedPos;
            public Vector2 screenPos;
            public GameObject cube;
        }
        
        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                UnityEngine.Debug.LogError("[HybridTouchVisualizer] No main camera found!");
                enabled = false;
                return;
            }
            
            // Create default cube prefab if not assigned
            if (cubePrefab == null)
            {
                cubePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubePrefab.transform.localScale = Vector3.one * cubeSize;
                
                // Create red material
                if (cubeMaterial == null)
                {
                    cubeMaterial = new Material(Shader.Find("Standard"));
                    cubeMaterial.color = Color.red;
                    cubeMaterial.SetFloat("_Metallic", 0f);
                    cubeMaterial.SetFloat("_Glossiness", 0.5f);
                }
                
                cubePrefab.GetComponent<Renderer>().material = cubeMaterial;
                
                // Remove collider
                Destroy(cubePrefab.GetComponent<Collider>());
                
                cubePrefab.SetActive(false);
                cubePrefab.name = "TouchCubePrefab";
            }
            
            // Find WebRtcManager
            webRtcManager = FindFirstObjectByType<WebRtcManager>();
            if (webRtcManager != null)
            {
                webRtcManager.OnDataChannelMessageReceived += OnMessage;
                UnityEngine.Debug.Log("[HybridTouchVisualizer] Connected to WebRtcManager");
            }
        }
        
        void OnMessage(string json)
        {
            try
            {
                if (json.Contains("\"type\":\"touch\""))
                {
                    var touch = JsonUtility.FromJson<TouchData>(json);
                    
                    // Store normalized position
                    Vector2 normalizedPos = new Vector2(touch.positionX, touch.positionY);
                    
                    // Calculate screen position with streaming resolution
                    const float STREAM_WIDTH = 1280f;
                    const float STREAM_HEIGHT = 720f;
                    
                    Vector2 screenPos = new Vector2(
                        touch.positionX * STREAM_WIDTH,
                        touch.positionY * STREAM_HEIGHT
                    );
                    
                    // Scale to actual screen
                    float scaleX = Screen.width / STREAM_WIDTH;
                    float scaleY = Screen.height / STREAM_HEIGHT;
                    float scale = Mathf.Min(scaleX, scaleY);
                    
                    float offsetX = (Screen.width - STREAM_WIDTH * scale) / 2f;
                    float offsetY = (Screen.height - STREAM_HEIGHT * scale) / 2f;
                    
                    screenPos.x = screenPos.x * scale + offsetX;
                    screenPos.y = screenPos.y * scale + offsetY;
                    
                    if (touch.phase == TouchPhase.Ended)
                    {
                        RemoveTouch(touch.touchId);
                    }
                    else
                    {
                        UpdateTouch(touch.touchId, normalizedPos, screenPos);
                    }
                    
                    UnityEngine.Debug.Log($"[HybridTouchVisualizer] Touch {touch.touchId}: " +
                        $"Normalized({normalizedPos.x:F3}, {normalizedPos.y:F3}) " +
                        $"Screen({screenPos.x:F0}, {screenPos.y:F0})");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[HybridTouchVisualizer] Error: {e.Message}");
            }
        }
        
        void UpdateTouch(int touchId, Vector2 normalizedPos, Vector2 screenPos)
        {
            if (!touches.TryGetValue(touchId, out TouchInfo info))
            {
                info = new TouchInfo();
                
                // Create cube for this touch
                info.cube = Instantiate(cubePrefab);
                info.cube.SetActive(true);
                info.cube.name = $"TouchCube_{touchId}";
                
                touches[touchId] = info;
            }
            
            info.normalizedPos = normalizedPos;
            info.screenPos = screenPos;
            
            // Update cube position in 3D space
            UpdateCubePosition(info);
        }
        
        void UpdateCubePosition(TouchInfo info)
        {
            if (info.cube == null || mainCamera == null) return;
            
            // Method 1: Using viewport coordinates (상대적 좌표)
            Vector3 viewportPos = new Vector3(info.normalizedPos.x, info.normalizedPos.y, cubeDistance);
            Vector3 worldPos = mainCamera.ViewportToWorldPoint(viewportPos);
            
            info.cube.transform.position = worldPos;
            info.cube.transform.LookAt(mainCamera.transform);
        }
        
        void RemoveTouch(int touchId)
        {
            if (touches.TryGetValue(touchId, out TouchInfo info))
            {
                if (info.cube != null)
                {
                    Destroy(info.cube);
                }
                touches.Remove(touchId);
            }
        }
        
        void OnGUI()
        {
            if (!showOnGUI) return;
            
            // Debug info
            if (showDebugInfo)
            {
                GUI.Label(new Rect(10, 10, 400, 20), $"[Hybrid] Screen: {Screen.width}x{Screen.height}");
                GUI.Label(new Rect(10, 30, 400, 20), $"[Hybrid] Stream: 1280x720");
                GUI.Label(new Rect(10, 50, 400, 20), $"[Hybrid] Active touches: {touches.Count}");
                
                if (mainCamera != null)
                {
                    GUI.Label(new Rect(10, 70, 400, 20), $"[Hybrid] Camera FOV: {mainCamera.fieldOfView:F1}");
                }
            }
            
            // Draw touches using screen coordinates (절대적 좌표)
            GUI.color = new Color(1f, 0f, 0f, 0.5f); // Semi-transparent red
            foreach (var kvp in touches)
            {
                var info = kvp.Value;
                float y = Screen.height - info.screenPos.y; // Y 반전
                
                // Draw circle
                GUI.Box(new Rect(info.screenPos.x - 25, y - 25, 50, 50), "");
                
                // Draw cross
                GUI.color = Color.white;
                GUI.Box(new Rect(info.screenPos.x - 15, y - 1, 30, 2), "");
                GUI.Box(new Rect(info.screenPos.x - 1, y - 15, 2, 30), "");
                
                // Draw coordinates
                GUI.color = Color.yellow;
                GUI.Label(new Rect(info.screenPos.x + 30, y - 10, 200, 20), 
                    $"2D: ({info.screenPos.x:F0}, {info.screenPos.y:F0})");
                
                // Show 3D position if cube exists
                if (info.cube != null)
                {
                    Vector3 screenPoint = mainCamera.WorldToScreenPoint(info.cube.transform.position);
                    GUI.Label(new Rect(info.screenPos.x + 30, y + 10, 200, 20), 
                        $"3D→2D: ({screenPoint.x:F0}, {screenPoint.y:F0})");
                }
                
                GUI.color = new Color(1f, 0f, 0f, 0.5f);
            }
        }
        
        void OnDestroy()
        {
            if (webRtcManager != null)
            {
                webRtcManager.OnDataChannelMessageReceived -= OnMessage;
            }
            
            // Clean up all cubes
            foreach (var info in touches.Values)
            {
                if (info.cube != null)
                {
                    Destroy(info.cube);
                }
            }
            
            // Clean up prefab
            if (cubePrefab != null)
            {
                Destroy(cubePrefab);
            }
        }
    }
}