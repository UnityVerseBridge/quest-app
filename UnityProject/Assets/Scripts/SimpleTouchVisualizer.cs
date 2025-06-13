using UnityEngine;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.DataChannel.Data;
using System.Collections.Generic;
using TouchPhase = UnityVerseBridge.Core.DataChannel.Data.TouchPhase;

namespace UnityVerse.QuestApp
{
    public class SimpleTouchVisualizer : MonoBehaviour
    {
        [Header("Touch Visualization Settings")]
        [SerializeField] private bool enableVisualization = true;
        [SerializeField] private Color touchColor = Color.red;
        [SerializeField] private int touchSize = 50;
        [SerializeField] private bool showCoordinates = true;
        [SerializeField] private bool showDebugInfo = true;
        
        private Dictionary<int, Vector2> touches = new Dictionary<int, Vector2>();
        private WebRtcManager webRtcManager;
        
        // Public property to control visualization
        public bool EnableVisualization
        {
            get => enableVisualization;
            set => enableVisualization = value;
        }
        
        void Start()
        {
            // WebRtcManager 찾기
            webRtcManager = FindFirstObjectByType<WebRtcManager>();
            if (webRtcManager != null)
            {
                webRtcManager.OnDataChannelMessageReceived += OnMessage;
                UnityEngine.Debug.Log("[SimpleTouchVisualizer] Connected to WebRtcManager");
            }
        }
        
        void OnMessage(string json)
        {
            try
            {
                if (json.Contains("\"type\":\"touch\""))
                {
                    var touch = JsonUtility.FromJson<TouchData>(json);
                    
                    // 스트리밍 해상도 기준으로 계산
                    const float STREAM_WIDTH = 1280f;
                    const float STREAM_HEIGHT = 720f;
                    
                    Vector2 pos = new Vector2(
                        touch.positionX * STREAM_WIDTH,
                        touch.positionY * STREAM_HEIGHT
                    );
                    
                    // 실제 화면 크기에 맞춰 스케일 조정
                    float scaleX = Screen.width / STREAM_WIDTH;
                    float scaleY = Screen.height / STREAM_HEIGHT;
                    float scale = Mathf.Min(scaleX, scaleY);
                    
                    // 중앙 정렬을 위한 오프셋
                    float offsetX = (Screen.width - STREAM_WIDTH * scale) / 2f;
                    float offsetY = (Screen.height - STREAM_HEIGHT * scale) / 2f;
                    
                    // 최종 스크린 좌표
                    pos.x = pos.x * scale + offsetX;
                    pos.y = pos.y * scale + offsetY;
                    
                    if (touch.phase == TouchPhase.Ended)
                        touches.Remove(touch.touchId);
                    else
                        touches[touch.touchId] = pos;
                        
                    UnityEngine.Debug.Log($"[SimpleTouchVisualizer] Touch at {pos} (normalized: {touch.positionX:F3}, {touch.positionY:F3})");
                }
            }
            catch { }
        }
        
        void OnGUI()
        {
            if (!enableVisualization) return;
            
            // 디버그 정보 표시 (선택적)
            if (showDebugInfo)
            {
                GUI.Label(new Rect(10, 10, 400, 20), $"Screen: {Screen.width}x{Screen.height}");
                GUI.Label(new Rect(10, 30, 400, 20), $"Stream: 1280x720");
                
                float scaleX = Screen.width / 1280f;
                float scaleY = Screen.height / 720f;
                float scale = Mathf.Min(scaleX, scaleY);
                GUI.Label(new Rect(10, 50, 400, 20), $"Scale: {scale:F2} (X:{scaleX:F2}, Y:{scaleY:F2})");
            }
            
            // 터치 표시
            GUI.color = touchColor;
            foreach (var touch in touches.Values)
            {
                float y = Screen.height - touch.y; // Y 반전
                float halfSize = touchSize * 0.5f;
                GUI.Box(new Rect(touch.x - halfSize, y - halfSize, touchSize, touchSize), "");
                
                if (showCoordinates)
                {
                    GUI.Label(new Rect(touch.x + halfSize + 5, y - 10, 200, 20), $"({touch.x:F0}, {touch.y:F0})");
                }
            }
        }
        
        void OnDestroy()
        {
            if (webRtcManager != null)
                webRtcManager.OnDataChannelMessageReceived -= OnMessage;
        }
    }
}