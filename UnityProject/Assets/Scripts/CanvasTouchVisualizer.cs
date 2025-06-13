using UnityEngine;
using UnityEngine.UI;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.DataChannel.Data;
using System.Collections;
using System.Collections.Generic;
using TouchPhase = UnityVerseBridge.Core.DataChannel.Data.TouchPhase;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// 2D Canvas를 사용한 터치 시각화 시스템
    /// 절대 위치와 상대 위치를 모두 표시
    /// </summary>
    public class CanvasTouchVisualizer : MonoBehaviour
    {
        [Header("Canvas Settings")]
        [SerializeField] private Canvas touchCanvas;
        [SerializeField] private RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay;
        
        [Header("Touch Indicator Settings")]
        [SerializeField] private GameObject touchIndicatorPrefab;
        [SerializeField] private float indicatorSize = 50f;
        [SerializeField] private Color touchColor = Color.red;
        [SerializeField] private bool showCoordinates = true;
        [SerializeField] private bool showBothCoordinates = true; // 절대/상대 좌표 모두 표시
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        
        private Dictionary<int, TouchIndicator> touches = new Dictionary<int, TouchIndicator>();
        private WebRtcManager webRtcManager;
        
        private class TouchIndicator
        {
            public GameObject gameObject;
            public RectTransform rectTransform;
            public Image image;
            public Text coordinateText;
            public Vector2 normalizedPosition;
            public Vector2 screenPosition;
        }
        
        void Start()
        {
            InitializeCanvas();
            CreateTouchIndicatorPrefab();
            
            // Find WebRtcManager
            webRtcManager = FindFirstObjectByType<WebRtcManager>();
            if (webRtcManager != null)
            {
                webRtcManager.OnDataChannelMessageReceived += OnDataChannelMessage;
                Debug.Log("[CanvasTouchVisualizer] Connected to WebRtcManager");
            }
        }
        
        void InitializeCanvas()
        {
            if (touchCanvas == null)
            {
                // Check if canvas already exists
                GameObject existingCanvas = GameObject.Find("TouchVisualizationCanvas");
                if (existingCanvas != null)
                {
                    touchCanvas = existingCanvas.GetComponent<Canvas>();
                    if (touchCanvas == null)
                    {
                        touchCanvas = existingCanvas.AddComponent<Canvas>();
                    }
                    // Ensure it starts hidden
                    existingCanvas.SetActive(false);
                    Debug.Log("[CanvasTouchVisualizer] Found existing TouchVisualizationCanvas");
                }
                else
                {
                    // Create new canvas
                    GameObject canvasGO = new GameObject("TouchVisualizationCanvas");
                    touchCanvas = canvasGO.AddComponent<Canvas>();
                    touchCanvas.renderMode = canvasRenderMode;
                    touchCanvas.sortingOrder = 100; // Make sure it's on top
                    
                    // Add CanvasScaler for resolution independence
                    CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;
                    
                    // Add GraphicRaycaster (though we don't need input)
                    canvasGO.AddComponent<GraphicRaycaster>();
                    
                    // Start with canvas hidden
                    canvasGO.SetActive(false);
                    Debug.Log("[CanvasTouchVisualizer] Created new TouchVisualizationCanvas");
                }
            }
        }
        
        void CreateTouchIndicatorPrefab()
        {
            if (touchIndicatorPrefab == null)
            {
                // Create default prefab
                touchIndicatorPrefab = new GameObject("TouchIndicatorPrefab");
                touchIndicatorPrefab.SetActive(false);
                
                // Set prefab name to avoid confusion
                touchIndicatorPrefab.name = "TouchIndicatorPrefab";
                
                // Add RectTransform
                RectTransform rect = touchIndicatorPrefab.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(indicatorSize, indicatorSize);
                
                // Add Image for visual indicator
                Image image = touchIndicatorPrefab.AddComponent<Image>();
                image.color = touchColor;
                
                // Create a simple circle sprite
                Texture2D circleTexture = CreateCircleTexture(64);
                image.sprite = Sprite.Create(circleTexture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
                
                // Add text for coordinates
                GameObject textGO = new GameObject("CoordinateText");
                textGO.transform.SetParent(touchIndicatorPrefab.transform, false);
                
                RectTransform textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(indicatorSize * 0.5f + 10, -10);
                textRect.offsetMax = new Vector2(200, 10);
                
                Text text = textGO.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 14;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleLeft;
                
                // Add outline for better visibility
                Outline outline = textGO.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1, -1);
            }
        }
        
        Texture2D CreateCircleTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size);
            float center = size / 2f;
            float radius = size / 2f - 1;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (distance <= radius)
                    {
                        float alpha = 1f - (distance / radius) * 0.3f; // Slight fade at edges
                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }
            
            texture.Apply();
            return texture;
        }
        
        void OnDataChannelMessage(string json)
        {
            try
            {
                if (json.Contains("\"type\":\"touch\""))
                {
                    var touch = JsonUtility.FromJson<TouchData>(json);
                    ProcessTouch(touch);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CanvasTouchVisualizer] Error processing touch: {e.Message}");
            }
        }
        
        void ProcessTouch(TouchData touch)
        {
            if (debugMode)
            {
                Debug.Log($"[CanvasTouchVisualizer] Processing touch {touch.touchId}, phase: {touch.phase}");
            }
            
            // Ensure canvas exists before processing
            if (touchCanvas == null || touchCanvas.gameObject == null)
            {
                Debug.LogWarning("[CanvasTouchVisualizer] TouchCanvas is null, reinitializing...");
                InitializeCanvas();
            }
            
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                // Remove touch when ended
                RemoveTouch(touch.touchId);
            }
            else
            {
                // Update or create touch
                UpdateTouch(touch);
            }
        }
        
        void UpdateTouch(TouchData touch)
        {
            Vector2 normalizedPos = new Vector2(touch.positionX, touch.positionY);
            
            // Calculate screen position based on streaming resolution
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
            
            if (!touches.TryGetValue(touch.touchId, out TouchIndicator indicator))
            {
                // Create new touch indicator
                indicator = CreateTouchIndicator();
                touches[touch.touchId] = indicator;
                
                // Show canvas when first touch starts
                if (touches.Count == 1 && touchCanvas != null)
                {
                    touchCanvas.gameObject.SetActive(true);
                }
            }
            
            // Update position and data
            indicator.normalizedPosition = normalizedPos;
            indicator.screenPosition = screenPos;
            
            // Set position on canvas
            indicator.rectTransform.position = screenPos;
            
            // Update coordinate text
            if (showCoordinates && indicator.coordinateText != null)
            {
                if (showBothCoordinates)
                {
                    indicator.coordinateText.text = $"Abs: ({screenPos.x:F0}, {screenPos.y:F0})\n" +
                                                   $"Rel: ({normalizedPos.x:F2}, {normalizedPos.y:F2})";
                }
                else
                {
                    indicator.coordinateText.text = $"({screenPos.x:F0}, {screenPos.y:F0})";
                }
            }
            
            if (debugMode)
            {
                Debug.Log($"[CanvasTouchVisualizer] Touch {touch.touchId} at screen: {screenPos}, normalized: {normalizedPos}");
            }
        }
        
        TouchIndicator CreateTouchIndicator()
        {
            GameObject go = Instantiate(touchIndicatorPrefab, touchCanvas.transform);
            go.SetActive(true);
            
            // 고유한 이름 설정 (디버깅 용이)
            go.name = $"TouchIndicator_{System.DateTime.Now.Ticks}";
            
            TouchIndicator indicator = new TouchIndicator
            {
                gameObject = go,
                rectTransform = go.GetComponent<RectTransform>(),
                image = go.GetComponent<Image>(),
                coordinateText = go.GetComponentInChildren<Text>()
            };
            
            // Apply current settings
            indicator.image.color = touchColor;
            indicator.rectTransform.sizeDelta = new Vector2(indicatorSize, indicatorSize);
            
            if (indicator.coordinateText != null)
            {
                indicator.coordinateText.gameObject.SetActive(showCoordinates);
            }
            
            return indicator;
        }
        
        void Update()
        {
            // 정기적으로 touches 상태를 확인하여 문제 감지
            if (debugMode && Time.frameCount % 60 == 0) // 1초마다
            {
                if (touches.Count > 0)
                {
                    Debug.Log($"[CanvasTouchVisualizer] Active touches: {touches.Count}");
                    foreach (var kvp in touches)
                    {
                        Debug.Log($"  - Touch {kvp.Key}: GameObject exists = {kvp.Value.gameObject != null}");
                    }
                }
            }
            
            // Canvas 상태 확인 및 동기화
            if (touchCanvas != null && touchCanvas.gameObject != null)
            {
                bool shouldBeActive = touches.Count > 0;
                if (touchCanvas.gameObject.activeSelf != shouldBeActive)
                {
                    if (debugMode)
                    {
                        Debug.Log($"[CanvasTouchVisualizer] Correcting canvas state: touches={touches.Count}, active={shouldBeActive}");
                    }
                    touchCanvas.gameObject.SetActive(shouldBeActive);
                }
            }
        }
        
        void RemoveTouch(int touchId)
        {
            if (touches.TryGetValue(touchId, out TouchIndicator indicator))
            {
                if (indicator.gameObject != null)
                {
                    Destroy(indicator.gameObject);
                }
                touches.Remove(touchId);
                
                if (debugMode)
                {
                    Debug.Log($"[CanvasTouchVisualizer] Removed touch {touchId}, remaining touches: {touches.Count}");
                }
                
                // Hide canvas when no touches remain
                if (touches.Count == 0 && touchCanvas != null && touchCanvas.gameObject != null)
                {
                    if (debugMode)
                    {
                        Debug.Log("[CanvasTouchVisualizer] No touches remaining, hiding canvas");
                    }
                    touchCanvas.gameObject.SetActive(false);
                    
                    // Double check in next frame
                    StartCoroutine(VerifyCanvasHidden());
                }
            }
            else
            {
                if (debugMode)
                {
                    Debug.LogWarning($"[CanvasTouchVisualizer] Attempted to remove non-existent touch {touchId}");
                }
            }
        }
        
        IEnumerator VerifyCanvasHidden()
        {
            yield return null; // Wait one frame
            
            if (touches.Count == 0 && touchCanvas != null && touchCanvas.gameObject != null && touchCanvas.gameObject.activeSelf)
            {
                Debug.LogWarning("[CanvasTouchVisualizer] Canvas still active after removing all touches, forcing hide");
                touchCanvas.gameObject.SetActive(false);
            }
        }
        
        void OnDestroy()
        {
            if (webRtcManager != null)
            {
                webRtcManager.OnDataChannelMessageReceived -= OnDataChannelMessage;
            }
            
            // Clean up all touches
            foreach (var indicator in touches.Values)
            {
                if (indicator.gameObject != null)
                {
                    Destroy(indicator.gameObject);
                }
            }
            touches.Clear();
            
            // Clean up prefab
            if (touchIndicatorPrefab != null)
            {
                Destroy(touchIndicatorPrefab);
            }
        }
        
        // Public methods for runtime configuration
        public void SetTouchColor(Color color)
        {
            touchColor = color;
            foreach (var indicator in touches.Values)
            {
                if (indicator.image != null)
                {
                    indicator.image.color = color;
                }
            }
        }
        
        public void SetIndicatorSize(float size)
        {
            indicatorSize = size;
            foreach (var indicator in touches.Values)
            {
                if (indicator.rectTransform != null)
                {
                    indicator.rectTransform.sizeDelta = new Vector2(size, size);
                }
            }
        }
        
        public void SetShowCoordinates(bool show)
        {
            showCoordinates = show;
            foreach (var indicator in touches.Values)
            {
                if (indicator.coordinateText != null)
                {
                    indicator.coordinateText.gameObject.SetActive(show);
                }
            }
        }
    }
}