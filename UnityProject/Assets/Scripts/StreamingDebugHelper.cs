using UnityEngine;
using UnityEngine.UI;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Quest 디바이스에서 스트리밍 문제를 디버그하는 헬퍼
    /// </summary>
    public class StreamingDebugHelper : MonoBehaviour
    {
        [Header("Debug Display")]
        [SerializeField] private RawImage debugPreview;
        [SerializeField] private Text debugText;
        
        [Header("References")]
        [SerializeField] private VrStreamSender streamSender;
        [SerializeField] private WebRtcManager webRtcManager;
        
        private RenderTexture streamTexture;
        
        void Start()
        {
            // VrStreamSender에서 RenderTexture 가져오기
            if (streamSender != null)
            {
                var rtField = streamSender.GetType().GetField("sourceRenderTexture", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (rtField != null)
                {
                    streamTexture = rtField.GetValue(streamSender) as RenderTexture;
                }
            }
            
            // Debug preview 설정
            if (debugPreview != null && streamTexture != null)
            {
                debugPreview.texture = streamTexture;
                Debug.Log($"[StreamingDebugHelper] Debug preview set to {streamTexture.name}");
            }
        }
        
        void Update()
        {
            if (debugText == null) return;
            
            string status = "Streaming Debug Info:\n";
            
            // WebRTC 상태
            if (webRtcManager != null)
            {
                status += $"Signaling: {webRtcManager.IsSignalingConnected}\n";
                status += $"WebRTC: {webRtcManager.IsWebRtcConnected}\n";
                status += $"PC State: {webRtcManager.GetPeerConnectionState()}\n";
            }
            
            // RenderTexture 상태
            if (streamTexture != null)
            {
                status += $"RT Created: {streamTexture.IsCreated()}\n";
                status += $"RT Size: {streamTexture.width}x{streamTexture.height}\n";
                status += $"RT Format: {streamTexture.format}\n";
            }
            
            // 카메라 정보
            var cameras = Camera.allCameras;
            status += $"Active Cameras: {cameras.Length}\n";
            foreach (var cam in cameras)
            {
                if (cam.targetTexture != null)
                {
                    status += $"- {cam.name} -> {cam.targetTexture.name}\n";
                }
            }
            
            debugText.text = status;
        }
        
        // 테스트 메서드들
        [ContextMenu("Force Recreate RenderTexture")]
        public void ForceRecreateRenderTexture()
        {
            if (streamSender != null)
            {
                streamSender.enabled = false;
                streamSender.enabled = true;
                Debug.Log("[StreamingDebugHelper] Forced VrStreamSender restart");
            }
        }
        
        [ContextMenu("Log Graphics Info")]
        public void LogGraphicsInfo()
        {
            Debug.Log($"[StreamingDebugHelper] Graphics Device: {SystemInfo.graphicsDeviceName}");
            Debug.Log($"[StreamingDebugHelper] Graphics API: {SystemInfo.graphicsDeviceType}");
            Debug.Log($"[StreamingDebugHelper] Multi-threaded Rendering: {SystemInfo.graphicsMultiThreaded}");
            Debug.Log($"[StreamingDebugHelper] Render Texture Support: {SystemInfo.supportsRenderTextures}");
        }
    }
}