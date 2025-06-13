using UnityEngine;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.DataChannel.Data;
using System.Collections.Generic;
using TouchPhase = UnityVerseBridge.Core.DataChannel.Data.TouchPhase;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// Simple OnGUI-based touch position debugger for Quest
    /// Shows touch positions directly on screen using OnGUI
    /// </summary>
    public class TouchPositionDebugger : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebug = true;
        [SerializeField] private float dotSize = 40f;
        [SerializeField] private Color dotColor = Color.red;
        
        private Dictionary<int, Vector2> touchPositions = new Dictionary<int, Vector2>();
        private WebRtcManager webRtcManager;
        private Texture2D dotTexture;
        
        void Start()
        {
            // Create dot texture
            CreateDotTexture();
            
            // Find WebRtcManager
            StartCoroutine(FindWebRtcManager());
        }
        
        System.Collections.IEnumerator FindWebRtcManager()
        {
            yield return new WaitForSeconds(2f);
            
            webRtcManager = FindFirstObjectByType<WebRtcManager>();
            if (webRtcManager != null)
            {
                webRtcManager.OnDataChannelMessageReceived += OnDataChannelMessageReceived;
                UnityEngine.Debug.Log("[TouchPositionDebugger] Connected to WebRtcManager");
            }
            else
            {
                UnityEngine.Debug.LogError("[TouchPositionDebugger] WebRtcManager not found!");
            }
        }
        
        void CreateDotTexture()
        {
            int size = 64;
            dotTexture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size / 2f, size / 2f);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance <= size / 2f - 2)
                    {
                        pixels[y * size + x] = dotColor;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            
            dotTexture.SetPixels(pixels);
            dotTexture.Apply();
        }
        
        void OnDataChannelMessageReceived(string jsonData)
        {
            try
            {
                var baseMsg = JsonUtility.FromJson<DataChannelMessageBase>(jsonData);
                if (baseMsg?.type == "touch")
                {
                    var touchData = JsonUtility.FromJson<TouchData>(jsonData);
                    
                    // Convert normalized position to screen position
                    Vector2 screenPos = new Vector2(
                        touchData.positionX * Screen.width,
                        touchData.positionY * Screen.height
                    );
                    
                    if (touchData.phase == TouchPhase.Ended || touchData.phase == TouchPhase.Canceled)
                    {
                        touchPositions.Remove(touchData.touchId);
                    }
                    else
                    {
                        touchPositions[touchData.touchId] = screenPos;
                    }
                    
                    UnityEngine.Debug.Log($"[TouchPositionDebugger] Touch {touchData.touchId} at screen pos: {screenPos}, normalized: ({touchData.positionX:F3}, {touchData.positionY:F3})");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[TouchPositionDebugger] Error parsing touch data: {e.Message}");
            }
        }
        
        void OnGUI()
        {
            if (!enableDebug) return;
            
            // Show status
            GUI.Label(new Rect(10, 10, 400, 30), $"TouchPositionDebugger - Active touches: {touchPositions.Count}");
            GUI.Label(new Rect(10, 40, 400, 30), $"Screen: {Screen.width}x{Screen.height}");
            
            // Draw touch positions
            foreach (var kvp in touchPositions)
            {
                Vector2 pos = kvp.Value;
                
                // OnGUI has Y-axis inverted
                float guiY = Screen.height - pos.y;
                
                // Draw dot
                Rect dotRect = new Rect(pos.x - dotSize/2, guiY - dotSize/2, dotSize, dotSize);
                GUI.DrawTexture(dotRect, dotTexture);
                
                // Draw label
                GUI.Label(new Rect(pos.x + dotSize/2, guiY - 10, 200, 20), 
                    $"Touch {kvp.Key}: ({pos.x:F0}, {pos.y:F0})");
            }
        }
        
        void OnDestroy()
        {
            if (webRtcManager != null)
            {
                webRtcManager.OnDataChannelMessageReceived -= OnDataChannelMessageReceived;
            }
        }
    }
    
    // DataChannelMessageBase만 정의 (TouchData와 TouchPhase는 이미 import됨)
    [System.Serializable]
    public class DataChannelMessageBase
    {
        public string type;
    }
}