using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// Quest 앱 시작 시 Touch Visualization을 자동으로 정리하고 설정
    /// </summary>
    [DefaultExecutionOrder(-1000)] // 다른 스크립트보다 먼저 실행
    public class TouchVisualizationAutoSetup : MonoBehaviour
    {
        private static bool hasInitialized = false;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            if (!hasInitialized)
            {
                hasInitialized = true;
                Debug.Log("[TouchVisualizationAutoSetup] Initializing touch visualization setup...");
            }
        }
        
        void Awake()
        {
            StartCoroutine(SetupTouchVisualization());
        }
        
        IEnumerator SetupTouchVisualization()
        {
            // 프레임 대기 (다른 오브젝트들이 생성되기를 기다림)
            yield return null;
            
            Debug.Log("[TouchVisualizationAutoSetup] Starting cleanup and setup...");
            
            // 1. 불필요한 Canvas들 제거
            CleanupUnwantedCanvases();
            
            // 2. 불필요한 Touch Visualizer들 제거
            CleanupUnwantedVisualizers();
            
            // 3. TouchVisualizationManager 설정
            SetupTouchVisualizationManager();
            
            // 4. 최종 확인
            yield return new WaitForSeconds(0.5f);
            VerifySetup();
            
            Debug.Log("[TouchVisualizationAutoSetup] Setup complete");
        }
        
        void CleanupUnwantedCanvases()
        {
            string[] unwantedCanvasNames = {
                "TouchCanvas",
                "Touch Display Canvas",
                "TouchDisplayCanvas",
                "Touch_Canvas" // 다양한 변형 처리
            };
            
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in allCanvases)
            {
                foreach (var unwantedName in unwantedCanvasNames)
                {
                    if (canvas.name.Contains(unwantedName) && !canvas.name.Contains("TouchVisualizationCanvas"))
                    {
                        Debug.Log($"[TouchVisualizationAutoSetup] Removing unwanted canvas: {canvas.name}");
                        Destroy(canvas.gameObject);
                    }
                }
            }
        }
        
        void CleanupUnwantedVisualizers()
        {
            // SimpleTouchVisualizer 제거
            var simpleViz = FindObjectsByType<SimpleTouchVisualizer>(FindObjectsSortMode.None);
            foreach (var viz in simpleViz)
            {
                Debug.Log($"[TouchVisualizationAutoSetup] Removing SimpleTouchVisualizer: {viz.name}");
                Destroy(viz.gameObject);
            }
            
            // DualTouchVisualizer 제거
            var dualViz = FindObjectsByType<DualTouchVisualizer>(FindObjectsSortMode.None);
            foreach (var viz in dualViz)
            {
                Debug.Log($"[TouchVisualizationAutoSetup] Removing DualTouchVisualizer: {viz.name}");
                Destroy(viz.gameObject);
            }
            
            // HybridTouchVisualizer 제거
            var hybridViz = FindObjectsByType<HybridTouchVisualizer>(FindObjectsSortMode.None);
            foreach (var viz in hybridViz)
            {
                Debug.Log($"[TouchVisualizationAutoSetup] Removing HybridTouchVisualizer: {viz.name}");
                Destroy(viz.gameObject);
            }
            
            // 불필요한 프리팹들 제거
            string[] unwantedPrefabNames = {
                "TouchVisualizer",
                "CanvasIndicatorPrefab",
                "Touch Pointer Prefab",
                "TouchPointerPrefab"
            };
            
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                foreach (var unwantedName in unwantedPrefabNames)
                {
                    if (obj.name == unwantedName)
                    {
                        Debug.Log($"[TouchVisualizationAutoSetup] Removing unwanted object: {obj.name}");
                        Destroy(obj);
                    }
                }
            }
        }
        
        void SetupTouchVisualizationManager()
        {
            var manager = TouchVisualizationManager.Instance;
            if (manager != null)
            {
                // Canvas 모드로 설정
                manager.SetVisualizationMode(TouchVisualizationManager.VisualizationMode.Canvas);
                manager.EnableTouchVisualization = true;
                Debug.Log("[TouchVisualizationAutoSetup] TouchVisualizationManager set to Canvas mode");
            }
        }
        
        void VerifySetup()
        {
            // TouchVisualizationCanvas가 있는지 확인
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            bool foundCorrectCanvas = false;
            
            foreach (var canvas in canvases)
            {
                if (canvas.name == "TouchVisualizationCanvas")
                {
                    foundCorrectCanvas = true;
                    Debug.Log($"[TouchVisualizationAutoSetup] ✓ Found TouchVisualizationCanvas (Active: {canvas.gameObject.activeSelf})");
                }
                else if (canvas.name.Contains("Touch") && canvas.name != "TouchVisualizationCanvas")
                {
                    Debug.LogWarning($"[TouchVisualizationAutoSetup] ✗ Unexpected canvas found: {canvas.name}");
                }
            }
            
            if (!foundCorrectCanvas)
            {
                Debug.LogWarning("[TouchVisualizationAutoSetup] TouchVisualizationCanvas not found - it will be created on first touch");
            }
            
            // CanvasTouchVisualizer가 있는지 확인
            var canvasVisualizer = FindFirstObjectByType<CanvasTouchVisualizer>();
            if (canvasVisualizer != null)
            {
                Debug.Log("[TouchVisualizationAutoSetup] ✓ CanvasTouchVisualizer found and ready");
            }
            else
            {
                Debug.LogWarning("[TouchVisualizationAutoSetup] CanvasTouchVisualizer not found");
            }
        }
        
        // Editor에서 수동 실행용
        [ContextMenu("Force Cleanup and Setup")]
        public void ForceSetup()
        {
            StartCoroutine(SetupTouchVisualization());
        }
    }
}