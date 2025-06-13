using UnityEngine;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.DataChannel.Data;
using System.Collections.Generic;
using TouchPhase = UnityVerseBridge.Core.DataChannel.Data.TouchPhase;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// 3D 큐브와 Screen Space 평면을 모두 사용하는 터치 시각화
    /// 둘 다 게임 오브젝트로 구현되어 스트리밍에 포함됨
    /// </summary>
    public class DualTouchVisualizer : MonoBehaviour
    {
        [Header("Visualization Mode")]
        [SerializeField] private VisualizationMode mode = VisualizationMode.Both;
        
        [Header("3D Cube Settings")]
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private float cubeSize = 0.2f;
        [SerializeField] private float cubeDistance = 2f;
        [SerializeField] private Color cubeColor = Color.red;
        
        [Header("Screen Plane Settings")]
        [SerializeField] private bool createScreenPlane = false; // 기본적으로 비활성화
        [SerializeField] private GameObject screenPlane;
        [SerializeField] private GameObject planeDotPrefab;
        [SerializeField] private float planeDistance = 0.5f;
        [SerializeField] private float dotSize = 0.05f;
        [SerializeField] private Color dotColor = Color.yellow;
        
        [Header("Debug")]
        [SerializeField] private bool showCoordinates = true;
        [SerializeField] private TextMesh coordinateTextPrefab;
        
        private Dictionary<int, TouchVisualization> touches = new Dictionary<int, TouchVisualization>();
        private WebRtcManager webRtcManager;
        private Camera mainCamera;
        
        public enum VisualizationMode
        {
            Cube3D,
            ScreenPlane,
            Both
        }
        
        private class TouchVisualization
        {
            public Vector2 normalizedPos;
            public Vector2 screenPos;
            public GameObject cube3D;
            public GameObject planeDot;
            public TextMesh coordinateText;
        }
        
        void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                UnityEngine.Debug.LogError("[DualTouchVisualizer] No main camera found!");
                enabled = false;
                return;
            }
            
            // Create screen plane if enabled and not exists
            if (createScreenPlane && screenPlane == null)
            {
                CreateScreenPlane();
            }
            
            // Create default prefabs if not assigned
            CreateDefaultPrefabs();
            
            // Find WebRtcManager
            webRtcManager = FindFirstObjectByType<WebRtcManager>();
            if (webRtcManager != null)
            {
                webRtcManager.OnDataChannelMessageReceived += OnMessage;
                UnityEngine.Debug.Log("[DualTouchVisualizer] Connected to WebRtcManager");
            }
        }
        
        void CreateScreenPlane()
        {
            // Create a plane that matches the camera's view
            screenPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenPlane.name = "ScreenPlane";
            
            // Remove collider
            Destroy(screenPlane.GetComponent<Collider>());
            
            // Remove or disable renderer completely
            var renderer = screenPlane.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Option 1: Disable renderer completely
                renderer.enabled = false;
                
                // Option 2: If we need it for positioning, make it truly transparent
                // var material = new Material(Shader.Find("Sprites/Default"));
                // material.color = new Color(0, 0, 0, 0);
                // renderer.material = material;
            }
            
            // Position in front of camera
            UpdateScreenPlane();
        }
        
        void UpdateScreenPlane()
        {
            if (screenPlane == null || mainCamera == null) return;
            
            float distance = planeDistance;
            float height = 2f * distance * Mathf.Tan(mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * mainCamera.aspect;
            
            screenPlane.transform.position = mainCamera.transform.position + mainCamera.transform.forward * distance;
            screenPlane.transform.rotation = mainCamera.transform.rotation;
            screenPlane.transform.localScale = new Vector3(width, height, 1f);
        }
        
        void CreateDefaultPrefabs()
        {
            // Create 3D cube prefab
            if (cubePrefab == null)
            {
                cubePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubePrefab.name = "TouchCubePrefab";
                cubePrefab.transform.localScale = Vector3.one * cubeSize;
                
                var renderer = cubePrefab.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = cubeColor;
                
                Destroy(cubePrefab.GetComponent<Collider>());
                cubePrefab.SetActive(false);
            }
            
            // Create plane dot prefab
            if (planeDotPrefab == null)
            {
                planeDotPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                planeDotPrefab.name = "PlaneDotPrefab";
                planeDotPrefab.transform.localScale = Vector3.one * dotSize;
                
                var renderer = planeDotPrefab.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Unlit/Color"));
                renderer.material.color = dotColor;
                
                Destroy(planeDotPrefab.GetComponent<Collider>());
                planeDotPrefab.SetActive(false);
            }
            
            // Create text prefab for coordinates
            if (coordinateTextPrefab == null && showCoordinates)
            {
                GameObject textObj = new GameObject("CoordinateTextPrefab");
                coordinateTextPrefab = textObj.AddComponent<TextMesh>();
                coordinateTextPrefab.fontSize = 50;
                coordinateTextPrefab.color = Color.white;
                coordinateTextPrefab.anchor = TextAnchor.MiddleLeft;
                coordinateTextPrefab.characterSize = 0.01f;
                textObj.SetActive(false);
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
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[DualTouchVisualizer] Error: {e.Message}");
            }
        }
        
        void UpdateTouch(int touchId, Vector2 normalizedPos, Vector2 screenPos)
        {
            if (!touches.TryGetValue(touchId, out TouchVisualization viz))
            {
                viz = new TouchVisualization();
                
                // Create 3D cube
                if (mode == VisualizationMode.Cube3D || mode == VisualizationMode.Both)
                {
                    viz.cube3D = Instantiate(cubePrefab);
                    viz.cube3D.SetActive(true);
                    viz.cube3D.name = $"TouchCube_{touchId}";
                }
                
                // Create screen plane dot
                if (mode == VisualizationMode.ScreenPlane || mode == VisualizationMode.Both)
                {
                    viz.planeDot = Instantiate(planeDotPrefab);
                    viz.planeDot.SetActive(true);
                    viz.planeDot.name = $"PlaneDot_{touchId}";
                    viz.planeDot.transform.SetParent(screenPlane.transform, false);
                }
                
                // Create coordinate text
                if (showCoordinates && coordinateTextPrefab != null)
                {
                    GameObject textObj = Instantiate(coordinateTextPrefab.gameObject);
                    textObj.SetActive(true);
                    viz.coordinateText = textObj.GetComponent<TextMesh>();
                }
                
                touches[touchId] = viz;
            }
            
            viz.normalizedPos = normalizedPos;
            viz.screenPos = screenPos;
            
            // Update positions
            UpdateVisualizationPositions(viz);
        }
        
        void UpdateVisualizationPositions(TouchVisualization viz)
        {
            // Update 3D cube position (viewport-based)
            if (viz.cube3D != null)
            {
                Vector3 viewportPos = new Vector3(viz.normalizedPos.x, viz.normalizedPos.y, cubeDistance);
                Vector3 worldPos = mainCamera.ViewportToWorldPoint(viewportPos);
                viz.cube3D.transform.position = worldPos;
                viz.cube3D.transform.LookAt(mainCamera.transform);
            }
            
            // Update screen plane dot position
            if (viz.planeDot != null)
            {
                // Convert normalized position to local plane coordinates
                float x = (viz.normalizedPos.x - 0.5f);
                float y = (viz.normalizedPos.y - 0.5f);
                viz.planeDot.transform.localPosition = new Vector3(x, y, -0.01f); // Slightly in front of plane
            }
            
            // Update coordinate text
            if (viz.coordinateText != null)
            {
                if (viz.cube3D != null)
                {
                    viz.coordinateText.transform.position = viz.cube3D.transform.position + Vector3.up * 0.15f;
                    viz.coordinateText.transform.rotation = mainCamera.transform.rotation;
                }
                else if (viz.planeDot != null)
                {
                    viz.coordinateText.transform.position = viz.planeDot.transform.position + Vector3.up * 0.05f;
                    viz.coordinateText.transform.rotation = mainCamera.transform.rotation;
                }
                
                viz.coordinateText.text = $"({viz.normalizedPos.x:F2}, {viz.normalizedPos.y:F2})\n" +
                                         $"({viz.screenPos.x:F0}, {viz.screenPos.y:F0})";
            }
        }
        
        void RemoveTouch(int touchId)
        {
            if (touches.TryGetValue(touchId, out TouchVisualization viz))
            {
                if (viz.cube3D != null) Destroy(viz.cube3D);
                if (viz.planeDot != null) Destroy(viz.planeDot);
                if (viz.coordinateText != null) Destroy(viz.coordinateText.gameObject);
                touches.Remove(touchId);
            }
        }
        
        void Update()
        {
            // Update screen plane position to follow camera (if needed for debugging)
            if (screenPlane != null && screenPlane.activeSelf)
            {
                UpdateScreenPlane();
            }
            
            // Update all visualization positions (in case camera moved)
            foreach (var viz in touches.Values)
            {
                UpdateVisualizationPositions(viz);
            }
        }
        
        void OnDestroy()
        {
            if (webRtcManager != null)
            {
                webRtcManager.OnDataChannelMessageReceived -= OnMessage;
            }
            
            // Clean up all touches
            foreach (var viz in touches.Values)
            {
                if (viz.cube3D != null) Destroy(viz.cube3D);
                if (viz.planeDot != null) Destroy(viz.planeDot);
                if (viz.coordinateText != null) Destroy(viz.coordinateText.gameObject);
            }
            
            // Clean up prefabs and plane
            if (cubePrefab != null) Destroy(cubePrefab);
            if (planeDotPrefab != null) Destroy(planeDotPrefab);
            if (coordinateTextPrefab != null) Destroy(coordinateTextPrefab.gameObject);
            if (screenPlane != null) Destroy(screenPlane);
        }
    }
}