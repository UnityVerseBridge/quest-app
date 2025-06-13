using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// 불필요한 TouchVisualizer 인스턴스들을 정리하는 유틸리티
    /// </summary>
    public class CleanupTouchVisualizers : MonoBehaviour
    {
        [Header("Cleanup Settings")]
        [SerializeField] private bool cleanupOnStart = true;
        [SerializeField] private bool cleanupDuplicateVisualizers = true;
        [SerializeField] private bool cleanupOrphanedIndicators = true;
        [SerializeField] private bool debugMode = true;
        
        void Start()
        {
            if (cleanupOnStart)
            {
                PerformCleanup();
            }
        }
        
        [ContextMenu("Perform Cleanup")]
        public void PerformCleanup()
        {
            int cleanedCount = 0;
            
            // 1. TouchCanvas 아래의 잘못된 TouchVisualizer 정리
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var canvas in allCanvases)
            {
                if (canvas.name.Contains("TouchCanvas"))
                {
                    // TouchVisualizer_1 같은 잘못된 오브젝트 찾기
                    Transform[] children = canvas.GetComponentsInChildren<Transform>();
                    foreach (var child in children)
                    {
                        if (child != canvas.transform && 
                            child.name.Contains("TouchVisualizer") && 
                            !child.GetComponent<CanvasTouchVisualizer>())
                        {
                            if (debugMode)
                                Debug.Log($"[CleanupTouchVisualizers] Removing invalid TouchVisualizer: {child.name}");
                            
                            DestroyImmediate(child.gameObject);
                            cleanedCount++;
                        }
                    }
                }
            }
            
            // 2. 중복된 TouchVisualizer 컴포넌트 정리
            if (cleanupDuplicateVisualizers)
            {
                // SimpleTouchVisualizer 중복 제거
                var simpleVisualizers = FindObjectsByType<SimpleTouchVisualizer>(FindObjectsSortMode.None);
                if (simpleVisualizers.Length > 1)
                {
                    for (int i = 1; i < simpleVisualizers.Length; i++)
                    {
                        if (debugMode)
                            Debug.Log($"[CleanupTouchVisualizers] Removing duplicate SimpleTouchVisualizer");
                        
                        DestroyImmediate(simpleVisualizers[i].gameObject);
                        cleanedCount++;
                    }
                }
                
                // DualTouchVisualizer 중복 제거
                var dualVisualizers = FindObjectsByType<DualTouchVisualizer>(FindObjectsSortMode.None);
                if (dualVisualizers.Length > 1)
                {
                    for (int i = 1; i < dualVisualizers.Length; i++)
                    {
                        if (debugMode)
                            Debug.Log($"[CleanupTouchVisualizers] Removing duplicate DualTouchVisualizer");
                        
                        DestroyImmediate(dualVisualizers[i].gameObject);
                        cleanedCount++;
                    }
                }
                
                // CanvasTouchVisualizer 중복 제거 (TouchVisualizationManager가 관리하는 것 제외)
                var canvasVisualizers = FindObjectsByType<CanvasTouchVisualizer>(FindObjectsSortMode.None);
                if (canvasVisualizers.Length > 1)
                {
                    // TouchVisualizationManager가 생성한 것을 찾기
                    CanvasTouchVisualizer managerCreated = null;
                    foreach (var viz in canvasVisualizers)
                    {
                        if (viz.gameObject.name == "CanvasTouchVisualizer")
                        {
                            managerCreated = viz;
                            break;
                        }
                    }
                    
                    // 나머지 제거
                    foreach (var viz in canvasVisualizers)
                    {
                        if (viz != managerCreated)
                        {
                            if (debugMode)
                                Debug.Log($"[CleanupTouchVisualizers] Removing duplicate CanvasTouchVisualizer: {viz.name}");
                            
                            DestroyImmediate(viz.gameObject);
                            cleanedCount++;
                        }
                    }
                }
            }
            
            // 3. 고아 상태의 CanvasIndicator 정리
            if (cleanupOrphanedIndicators)
            {
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains("CanvasIndicator_") && 
                        obj.transform.parent == null)
                    {
                        if (debugMode)
                            Debug.Log($"[CleanupTouchVisualizers] Removing orphaned indicator: {obj.name}");
                        
                        DestroyImmediate(obj);
                        cleanedCount++;
                    }
                }
            }
            
            if (debugMode)
            {
                Debug.Log($"[CleanupTouchVisualizers] Cleanup complete. Removed {cleanedCount} objects.");
            }
        }
        
        // Editor에서 수동으로 실행할 수 있는 메서드들
        [ContextMenu("Remove All Touch Visualizers")]
        public void RemoveAllTouchVisualizers()
        {
            // 모든 종류의 TouchVisualizer 제거
            var allVisualizers = new List<Component>();
            allVisualizers.AddRange(FindObjectsByType<SimpleTouchVisualizer>(FindObjectsSortMode.None));
            allVisualizers.AddRange(FindObjectsByType<DualTouchVisualizer>(FindObjectsSortMode.None));
            allVisualizers.AddRange(FindObjectsByType<CanvasTouchVisualizer>(FindObjectsSortMode.None));
            allVisualizers.AddRange(FindObjectsByType<HybridTouchVisualizer>(FindObjectsSortMode.None));
            
            foreach (var viz in allVisualizers)
            {
                DestroyImmediate(viz.gameObject);
            }
            
            Debug.Log($"[CleanupTouchVisualizers] Removed all {allVisualizers.Count} touch visualizers.");
        }
        
        [ContextMenu("Keep Only Canvas Visualizer")]
        public void KeepOnlyCanvasVisualizer()
        {
            // Canvas 방식만 남기고 나머지 제거
            var toRemove = new List<Component>();
            toRemove.AddRange(FindObjectsByType<SimpleTouchVisualizer>(FindObjectsSortMode.None));
            toRemove.AddRange(FindObjectsByType<DualTouchVisualizer>(FindObjectsSortMode.None));
            toRemove.AddRange(FindObjectsByType<HybridTouchVisualizer>(FindObjectsSortMode.None));
            
            foreach (var viz in toRemove)
            {
                DestroyImmediate(viz.gameObject);
            }
            
            // TouchVisualizationManager가 Canvas 모드를 사용하도록 설정
            var manager = TouchVisualizationManager.Instance;
            if (manager != null)
            {
                manager.SetVisualizationMode(TouchVisualizationManager.VisualizationMode.Canvas);
                manager.ShowTouchVisualization();
            }
            
            Debug.Log($"[CleanupTouchVisualizers] Removed {toRemove.Count} non-canvas visualizers.");
        }
    }
}