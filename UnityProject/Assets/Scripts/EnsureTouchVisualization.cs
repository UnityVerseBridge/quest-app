using UnityEngine;
using UnityVerseBridge.Core;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// SimpleTouchVisualizationSystem이 존재하도록 보장
    /// </summary>
    [RequireComponent(typeof(UnityVerseBridgeManager))]
    public class EnsureTouchVisualization : MonoBehaviour
    {
        void Awake()
        {
            Debug.Log("[EnsureTouchVisualization] Ensuring touch visualization system...");
            
            // SimpleTouchVisualizationSystem 찾기
            var touchViz = FindFirstObjectByType<SimpleTouchVisualizationSystem>();
            if (touchViz == null)
            {
                // 새로운 GameObject에 추가
                GameObject vizGO = new GameObject("SimpleTouchVisualizationSystem");
                touchViz = vizGO.AddComponent<SimpleTouchVisualizationSystem>();
                Debug.Log("[EnsureTouchVisualization] Created SimpleTouchVisualizationSystem");
            }
            else
            {
                Debug.Log("[EnsureTouchVisualization] SimpleTouchVisualizationSystem already exists");
            }
            
            // CanvasTouchVisualizer는 비활성화
            var canvasViz = FindFirstObjectByType<CanvasTouchVisualizer>();
            if (canvasViz != null)
            {
                Debug.Log("[EnsureTouchVisualization] Disabling CanvasTouchVisualizer");
                canvasViz.enabled = false;
                Destroy(canvasViz.gameObject);
            }
        }
    }
}