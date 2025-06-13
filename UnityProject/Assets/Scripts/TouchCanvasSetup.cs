using UnityEngine;
using UnityEngine.UI;
using UnityVerseBridge.Core;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// Quest에서 터치 표시를 위한 Canvas 설정 도우미
    /// 다양한 Canvas 모드를 테스트할 수 있습니다
    /// </summary>
    public class TouchCanvasSetup : MonoBehaviour
    {
        [Header("Canvas Mode Selection")]
        [SerializeField] private CanvasMode canvasMode = CanvasMode.ScreenSpaceCamera;
        
        [Header("Canvas Settings")]
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float planeDistance = 1f;
        [SerializeField] private int sortingOrder = 999;
        
        [Header("World Space Settings")]
        [SerializeField] private float worldCanvasDistance = 2f;
        [SerializeField] private Vector3 worldCanvasScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        [Header("Test Visualization")]
        [SerializeField] private bool showTestDot = true;
        [SerializeField] private Vector2 testDotPosition = new Vector2(0.5f, 0.5f);
        
        private Canvas touchCanvas;
        private GameObject testDot;
        
        public enum CanvasMode
        {
            ScreenSpaceOverlay,
            ScreenSpaceCamera,
            WorldSpace
        }
        
        void Start()
        {
            SetupCanvas();
            
            if (showTestDot)
            {
                CreateTestDot();
            }
        }
        
        void SetupCanvas()
        {
            // Find or create canvas
            var touchHandler = FindFirstObjectByType<TouchInputHandler>();
            if (touchHandler != null)
            {
                var canvasField = typeof(TouchInputHandler).GetField("touchCanvas", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                touchCanvas = canvasField?.GetValue(touchHandler) as Canvas;
            }
            
            if (touchCanvas == null)
            {
                GameObject canvasObj = GameObject.Find("TouchCanvas");
                if (canvasObj != null)
                {
                    touchCanvas = canvasObj.GetComponent<Canvas>();
                }
            }
            
            if (touchCanvas == null)
            {
                UnityEngine.Debug.LogError("[TouchCanvasSetup] Touch Canvas not found!");
                return;
            }
            
            // Find camera if not assigned
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    // Try to find VR camera
                    Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                    foreach (var cam in cameras)
                    {
                        if (cam.gameObject.activeInHierarchy && cam.enabled)
                        {
                            targetCamera = cam;
                            break;
                        }
                    }
                }
            }
            
            // Configure canvas based on selected mode
            switch (canvasMode)
            {
                case CanvasMode.ScreenSpaceOverlay:
                    SetupScreenSpaceOverlay();
                    break;
                    
                case CanvasMode.ScreenSpaceCamera:
                    SetupScreenSpaceCamera();
                    break;
                    
                case CanvasMode.WorldSpace:
                    SetupWorldSpace();
                    break;
            }
            
            UnityEngine.Debug.Log($"[TouchCanvasSetup] Canvas configured - Mode: {canvasMode}, Camera: {targetCamera?.name ?? "none"}");
        }
        
        void SetupScreenSpaceOverlay()
        {
            touchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            touchCanvas.sortingOrder = sortingOrder;
            
            // Ensure canvas is at correct position
            touchCanvas.transform.position = Vector3.zero;
            touchCanvas.transform.rotation = Quaternion.identity;
            touchCanvas.transform.localScale = Vector3.one;
        }
        
        void SetupScreenSpaceCamera()
        {
            if (targetCamera == null)
            {
                UnityEngine.Debug.LogError("[TouchCanvasSetup] No camera found for Screen Space - Camera mode!");
                SetupScreenSpaceOverlay(); // Fallback
                return;
            }
            
            touchCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            touchCanvas.worldCamera = targetCamera;
            touchCanvas.planeDistance = planeDistance;
            touchCanvas.sortingOrder = sortingOrder;
            
            // Reset transform
            touchCanvas.transform.position = Vector3.zero;
            touchCanvas.transform.rotation = Quaternion.identity;
            touchCanvas.transform.localScale = Vector3.one;
        }
        
        void SetupWorldSpace()
        {
            if (targetCamera == null)
            {
                UnityEngine.Debug.LogError("[TouchCanvasSetup] No camera found for World Space mode!");
                return;
            }
            
            touchCanvas.renderMode = RenderMode.WorldSpace;
            
            // Position canvas in front of camera
            touchCanvas.transform.position = targetCamera.transform.position + targetCamera.transform.forward * worldCanvasDistance;
            touchCanvas.transform.rotation = targetCamera.transform.rotation;
            touchCanvas.transform.localScale = worldCanvasScale;
            
            // Set canvas size
            RectTransform canvasRect = touchCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1920, 1080);
        }
        
        void CreateTestDot()
        {
            // Create test dot to verify canvas is visible
            testDot = new GameObject("TestDot");
            testDot.transform.SetParent(touchCanvas.transform, false);
            
            var image = testDot.AddComponent<Image>();
            image.color = Color.green;
            image.raycastTarget = false;
            
            RectTransform rect = testDot.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 100);
            
            // Position at normalized coordinates
            if (touchCanvas.renderMode == RenderMode.WorldSpace)
            {
                float canvasWidth = 1920;
                float canvasHeight = 1080;
                rect.anchoredPosition = new Vector2(
                    (testDotPosition.x - 0.5f) * canvasWidth,
                    (testDotPosition.y - 0.5f) * canvasHeight
                );
            }
            else
            {
                rect.position = new Vector3(
                    testDotPosition.x * Screen.width,
                    testDotPosition.y * Screen.height,
                    0
                );
            }
            
            UnityEngine.Debug.Log($"[TouchCanvasSetup] Test dot created at position: {rect.position}");
        }
        
        void Update()
        {
            // Update test dot position for debugging
            if (testDot != null && Input.GetKeyDown(KeyCode.Space))
            {
                RectTransform rect = testDot.GetComponent<RectTransform>();
                testDotPosition = new Vector2(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
                
                if (touchCanvas.renderMode == RenderMode.WorldSpace)
                {
                    float canvasWidth = 1920;
                    float canvasHeight = 1080;
                    rect.anchoredPosition = new Vector2(
                        (testDotPosition.x - 0.5f) * canvasWidth,
                        (testDotPosition.y - 0.5f) * canvasHeight
                    );
                }
                else
                {
                    rect.position = new Vector3(
                        testDotPosition.x * Screen.width,
                        testDotPosition.y * Screen.height,
                        0
                    );
                }
                
                UnityEngine.Debug.Log($"[TouchCanvasSetup] Test dot moved to: {rect.position}");
            }
        }
        
        void OnGUI()
        {
            GUI.Label(new Rect(10, 100, 400, 30), $"Canvas Mode: {canvasMode}");
            GUI.Label(new Rect(10, 130, 400, 30), $"Canvas Active: {touchCanvas?.gameObject.activeInHierarchy ?? false}");
            GUI.Label(new Rect(10, 160, 400, 30), $"Test Dot Position: {testDotPosition}");
            
            if (touchCanvas != null)
            {
                GUI.Label(new Rect(10, 190, 400, 30), $"Canvas Render Mode: {touchCanvas.renderMode}");
                GUI.Label(new Rect(10, 220, 400, 30), $"Canvas Camera: {touchCanvas.worldCamera?.name ?? "none"}");
            }
        }
    }
}