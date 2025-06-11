using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;
using UnityVerseBridge.Core;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Quest 3의 MR 패스스루 화면을 캡처하여 여러 모바일 기기로 스트리밍하는 컴포넌트입니다.
    /// WebRtcManager의 multi-peer mode를 사용하여 1:N 스트리밍을 지원합니다.
    /// </summary>
    public class VrMRStreamSender : MonoBehaviour
    {
        [Header("WebRTC Manager")]
        [SerializeField] private WebRtcManager webRtcManager;

        [Header("MR Camera Settings")]
        [Tooltip("MR 화면을 캡처할 카메라입니다. (보통 CenterEyeAnchor)")]
        [SerializeField] private Camera mrCamera;
        
        [Tooltip("패스스루 레이어를 포함한 컬링 마스크입니다.")]
        [SerializeField] private LayerMask cullingMask = -1;
        
        [Tooltip("스트리밍 해상도입니다.")]
        [SerializeField] private Vector2Int streamResolution = new Vector2Int(1280, 720);

        [Header("Performance")]
        [Tooltip("적응형 해상도를 사용합니다.")]
        [SerializeField] private bool useAdaptiveResolution = true;
        
        [Tooltip("최소 해상도입니다.")]
        [SerializeField] private Vector2Int minResolution = new Vector2Int(640, 360);
        
        [Tooltip("연결된 피어 수에 따라 품질을 조정합니다.")]
        [SerializeField] private bool adjustQualityByPeerCount = true;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool capturePassthrough = true;

        private RenderTexture renderTexture;
        private VideoStreamTrack videoStreamTrack;
        private bool isStreaming = false;
        private float lastQualityCheck = 0f;
        private int currentPeerCount = 0;

        // OVR 패스스루 관련
        private OVRPassthroughLayer passthroughLayer;
        private OVRCameraRig cameraRig;

        void Awake()
        {
            if (webRtcManager == null)
            {
                webRtcManager = FindFirstObjectByType<WebRtcManager>();
                if (webRtcManager == null)
                {
                    Debug.LogError("[VrMRStreamSender] WebRtcManager not found!");
                    enabled = false;
                    return;
                }
            }

            // MR 카메라 찾기
            if (mrCamera == null)
            {
                cameraRig = FindFirstObjectByType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    mrCamera = cameraRig.centerEyeAnchor.GetComponent<Camera>();
                }
                
                if (mrCamera == null)
                {
                    Debug.LogError("[VrMRStreamSender] MR Camera not found!");
                    enabled = false;
                    return;
                }
            }

            // 패스스루 레이어 찾기
            passthroughLayer = FindFirstObjectByType<OVRPassthroughLayer>();
            
            SetupRenderTexture();
        }

        void OnEnable()
        {
            if (webRtcManager != null)
            {
                webRtcManager.OnPeerConnected += HandlePeerConnected;
                webRtcManager.OnPeerDisconnected += HandlePeerDisconnected;
                webRtcManager.OnSignalingConnected += StartStreaming;
                webRtcManager.OnSignalingDisconnected += StopStreaming;
            }
        }

        void OnDisable()
        {
            if (webRtcManager != null)
            {
                webRtcManager.OnPeerConnected -= HandlePeerConnected;
                webRtcManager.OnPeerDisconnected -= HandlePeerDisconnected;
                webRtcManager.OnSignalingConnected -= StartStreaming;
                webRtcManager.OnSignalingDisconnected -= StopStreaming;
            }
            
            StopStreaming();
        }

        private void SetupRenderTexture()
        {
            // RenderTexture 생성
            renderTexture = new RenderTexture(streamResolution.x, streamResolution.y, 24, RenderTextureFormat.BGRA32);
            renderTexture.Create();
            
            Debug.Log($"[VrMRStreamSender] RenderTexture created: {streamResolution.x}x{streamResolution.y}");
        }

        private void StartStreaming()
        {
            if (isStreaming) return;

            try
            {
                // 패스스루 활성화 확인
                if (capturePassthrough && passthroughLayer != null)
                {
                    passthroughLayer.enabled = true;
                    Debug.Log("[VrMRStreamSender] Passthrough layer enabled");
                }

                // 카메라 설정
                SetupMRCamera();

                // VideoStreamTrack 생성
                videoStreamTrack = new VideoStreamTrack(renderTexture);
                
                // MultiPeerManager에 비디오 트랙 추가
                webRtcManager.AddVideoTrack(videoStreamTrack);
                
                isStreaming = true;
                
                // 프레임 캡처 시작
                StartCoroutine(CaptureFrames());
                
                Debug.Log("[VrMRStreamSender] MR streaming started");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VrMRStreamSender] Failed to start streaming: {e.Message}");
            }
        }

        private void StopStreaming()
        {
            if (!isStreaming) return;

            isStreaming = false;

            // 비디오 트랙 제거
            if (videoStreamTrack != null && webRtcManager != null)
            {
                webRtcManager.RemoveTrack(videoStreamTrack);
                videoStreamTrack.Dispose();
                videoStreamTrack = null;
            }

            Debug.Log("[VrMRStreamSender] MR streaming stopped");
        }

        private void SetupMRCamera()
        {
            if (mrCamera == null) return;

            // 카메라 컬링 마스크 설정
            mrCamera.cullingMask = cullingMask;
            
            // 패스스루를 캡처하기 위한 설정
            if (capturePassthrough)
            {
                // OVRManager 설정 확인
                if (OVRManager.instance != null)
                {
                    OVRManager.instance.isInsightPassthroughEnabled = true;
                }
            }
        }

        private IEnumerator CaptureFrames()
        {
            var wait = new WaitForEndOfFrame();
            
            while (isStreaming)
            {
                yield return wait;

                if (mrCamera != null && renderTexture != null)
                {
                    // 현재 카메라의 타겟 텍스처 백업
                    var previousTarget = mrCamera.targetTexture;
                    
                    // MR 화면을 RenderTexture로 렌더링
                    mrCamera.targetTexture = renderTexture;
                    mrCamera.Render();
                    
                    // 원래 타겟으로 복원
                    mrCamera.targetTexture = previousTarget;
                }

                // 적응형 품질 조정
                if (useAdaptiveResolution && Time.time - lastQualityCheck > 2f)
                {
                    AdjustStreamingQuality();
                    lastQualityCheck = Time.time;
                }
            }
        }

        private void HandlePeerConnected(string peerId)
        {
            currentPeerCount = webRtcManager.ActiveConnectionsCount;
            Debug.Log($"[VrMRStreamSender] Peer connected: {peerId}, Total peers: {currentPeerCount}");
            
            if (adjustQualityByPeerCount)
            {
                AdjustStreamingQuality();
            }
        }

        private void HandlePeerDisconnected(string peerId)
        {
            currentPeerCount = webRtcManager.ActiveConnectionsCount;
            Debug.Log($"[VrMRStreamSender] Peer disconnected: {peerId}, Total peers: {currentPeerCount}");
            
            if (adjustQualityByPeerCount)
            {
                AdjustStreamingQuality();
            }
        }

        private void AdjustStreamingQuality()
        {
            if (!isStreaming || renderTexture == null) return;

            // 피어 수에 따른 해상도 조정
            Vector2Int newResolution = streamResolution;
            
            if (currentPeerCount > 3)
            {
                // 많은 피어가 연결된 경우 해상도 감소
                newResolution = new Vector2Int(
                    Mathf.Max(minResolution.x, streamResolution.x / 2),
                    Mathf.Max(minResolution.y, streamResolution.y / 2)
                );
            }
            else if (currentPeerCount > 5)
            {
                newResolution = minResolution;
            }

            // 해상도가 변경된 경우 RenderTexture 재생성
            if (newResolution.x != renderTexture.width || newResolution.y != renderTexture.height)
            {
                Debug.Log($"[VrMRStreamSender] Adjusting resolution: {newResolution.x}x{newResolution.y} for {currentPeerCount} peers");
                
                var oldTexture = renderTexture;
                renderTexture = new RenderTexture(newResolution.x, newResolution.y, 24, RenderTextureFormat.BGRA32);
                renderTexture.Create();
                
                // 비디오 트랙 업데이트 필요
                // WebRTC는 동적 해상도 변경을 직접 지원하지 않으므로, 
                // 실제 구현에서는 재협상이 필요할 수 있음
                
                oldTexture.Release();
                Destroy(oldTexture);
            }
        }

        void OnDestroy()
        {
            StopStreaming();
            
            if (renderTexture != null)
            {
                renderTexture.Release();
                Destroy(renderTexture);
            }
        }

        // UI에서 호출할 수 있는 공개 메서드들
        public void TogglePassthrough(bool enable)
        {
            capturePassthrough = enable;
            if (passthroughLayer != null)
            {
                passthroughLayer.enabled = enable;
            }
        }

        public void SetStreamingResolution(int width, int height)
        {
            streamResolution = new Vector2Int(width, height);
            if (isStreaming)
            {
                // 스트리밍 중이면 재시작
                StopStreaming();
                SetupRenderTexture();
                StartStreaming();
            }
        }

        public void SetAdaptiveQuality(bool enable)
        {
            useAdaptiveResolution = enable;
        }

        // 디버그 정보
        void OnGUI()
        {
            if (!showDebugInfo) return;

            GUI.Label(new Rect(10, 10, 300, 20), $"MR Streaming: {(isStreaming ? "ON" : "OFF")}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Connected Peers: {currentPeerCount}");
            GUI.Label(new Rect(10, 50, 300, 20), $"Resolution: {renderTexture?.width}x{renderTexture?.height}");
            GUI.Label(new Rect(10, 70, 300, 20), $"Passthrough: {(capturePassthrough ? "ON" : "OFF")}");
        }
    }
}