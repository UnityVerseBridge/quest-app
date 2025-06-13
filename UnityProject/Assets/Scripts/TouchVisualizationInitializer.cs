using UnityEngine;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.Configuration;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// Quest 앱 시작 시 올바른 Touch Visualization을 설정하는 초기화 스크립트
    /// </summary>
    public class TouchVisualizationInitializer : MonoBehaviour
    {
        [Header("Initialization Settings")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool cleanupOldVisualizers = true;
        [SerializeField] private bool forceCanvasMode = true;
        
        void Awake()
        {
            if (autoInitialize)
            {
                InitializeTouchVisualization();
            }
        }
        
        [ContextMenu("Initialize Touch Visualization")]
        public void InitializeTouchVisualization()
        {
            Debug.Log("[TouchVisualizationInitializer] Starting initialization...");
            
            // 1. 기존 visualizer들 정리
            if (cleanupOldVisualizers)
            {
                CleanupOldVisualizers();
            }
            
            // 2. UnityVerseBridgeManager 확인
            var bridgeManager = FindFirstObjectByType<UnityVerseBridgeManager>();
            if (bridgeManager == null)
            {
                Debug.LogError("[TouchVisualizationInitializer] UnityVerseBridgeManager not found!");
                return;
            }
            
            // 3. TouchVisualizationConfig 확인
            var config = bridgeManager.GetType()
                .GetField("touchVisualizationConfig", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(bridgeManager) as TouchVisualizationConfig;
                
            if (config == null)
            {
                Debug.LogWarning("[TouchVisualizationInitializer] TouchVisualizationConfig not found in UnityVerseBridgeManager");
                return;
            }
            
            // 4. Canvas 모드로 강제 설정 (옵션)
            if (forceCanvasMode && config.mode != TouchVisualizationConfig.VisualizationMode.Canvas)
            {
                Debug.Log("[TouchVisualizationInitializer] Forcing Canvas mode");
                config.mode = TouchVisualizationConfig.VisualizationMode.Canvas;
            }
            
            // 5. TouchVisualizationManager 설정
            var touchManager = TouchVisualizationManager.Instance;
            if (touchManager != null)
            {
                touchManager.SetVisualizationMode(TouchVisualizationManager.VisualizationMode.Canvas);
                touchManager.EnableTouchVisualization = true;
                Debug.Log("[TouchVisualizationInitializer] TouchVisualizationManager configured for Canvas mode");
            }
            
            // 6. 올바른 CanvasTouchVisualizer가 있는지 확인
            var canvasVisualizer = FindFirstObjectByType<CanvasTouchVisualizer>();
            if (canvasVisualizer == null)
            {
                // 생성
                GameObject vizGO = new GameObject("CanvasTouchVisualizer");
                canvasVisualizer = vizGO.AddComponent<CanvasTouchVisualizer>();
                Debug.Log("[TouchVisualizationInitializer] Created CanvasTouchVisualizer");
            }
            
            Debug.Log("[TouchVisualizationInitializer] Initialization complete");
        }
        
        private void CleanupOldVisualizers()
        {
            Debug.Log("[TouchVisualizationInitializer] Cleaning up old visualizers...");
            
            // SimpleTouchVisualizer 제거
            var simpleViz = FindObjectsByType<SimpleTouchVisualizer>(FindObjectsSortMode.None);
            foreach (var viz in simpleViz)
            {
                Debug.Log($"[TouchVisualizationInitializer] Removing SimpleTouchVisualizer: {viz.name}");
                DestroyImmediate(viz.gameObject);
            }
            
            // DualTouchVisualizer 제거
            var dualViz = FindObjectsByType<DualTouchVisualizer>(FindObjectsSortMode.None);
            foreach (var viz in dualViz)
            {
                Debug.Log($"[TouchVisualizationInitializer] Removing DualTouchVisualizer: {viz.name}");
                DestroyImmediate(viz.gameObject);
            }
            
            // HybridTouchVisualizer 제거
            var hybridViz = FindObjectsByType<HybridTouchVisualizer>(FindObjectsSortMode.None);
            foreach (var viz in hybridViz)
            {
                Debug.Log($"[TouchVisualizationInitializer] Removing HybridTouchVisualizer: {viz.name}");
                DestroyImmediate(viz.gameObject);
            }
            
            // TouchCanvas 아래의 잘못된 오브젝트 제거
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in allCanvases)
            {
                if (canvas.name.Contains("TouchCanvas"))
                {
                    Transform[] children = canvas.GetComponentsInChildren<Transform>();
                    foreach (var child in children)
                    {
                        if (child != canvas.transform && 
                            child.name.Contains("TouchVisualizer"))
                        {
                            Debug.Log($"[TouchVisualizationInitializer] Removing invalid object from TouchCanvas: {child.name}");
                            DestroyImmediate(child.gameObject);
                        }
                    }
                }
            }
        }
        
        [ContextMenu("Test Touch Removal")]
        public void TestTouchRemoval()
        {
            // CanvasIndicator 테스트를 위한 메서드
            var indicators = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in indicators)
            {
                if (obj.name.Contains("TouchIndicator_") || obj.name.Contains("CanvasIndicator_"))
                {
                    Debug.Log($"[TouchVisualizationInitializer] Found indicator: {obj.name}, Parent: {obj.transform.parent?.name ?? "None"}");
                }
            }
        }
    }
}