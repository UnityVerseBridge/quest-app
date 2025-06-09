using UnityEngine;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.DataChannel.Data;
using System.Collections;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Quest VR에서 발생하는 다양한 상호작용 이벤트를 감지하고
    /// 해당 이벤트에 대한 햅틱 피드백을 Mobile 디바이스로 전송하는 컴포넌트입니다.
    /// </summary>
    public class VrHapticRequester : MonoBehaviour
    {
        [Header("WebRTC Manager")]
        [SerializeField] private WebRtcManager webRtcManager;
        
        [Header("Haptic Settings")]
        [Tooltip("컨트롤러 버튼 입력에 대한 햅틱 피드백을 활성화합니다.")]
        [SerializeField] private bool enableButtonHaptics = true;
        
        [Tooltip("트리거 입력에 대한 햅틱 피드백을 활성화합니다.")]
        [SerializeField] private bool enableTriggerHaptics = true;
        
        [Tooltip("그립 입력에 대한 햅틱 피드백을 활성화합니다.")]
        [SerializeField] private bool enableGripHaptics = true;
        
        [Tooltip("손 추적 제스처에 대한 햅틱 피드백을 활성화합니다.")]
        [SerializeField] private bool enableHandTrackingHaptics = true;
        
        [Tooltip("충돌 이벤트에 대한 햅틱 피드백을 활성화합니다.")]
        [SerializeField] private bool enableCollisionHaptics = true;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        // 입력 상태 추적
        private float lastTriggerValue = 0f;
        private float lastGripValue = 0f;
        private bool isGrabbing = false;

        void Awake()
        {
            if (webRtcManager == null)
            {
                webRtcManager = FindFirstObjectByType<WebRtcManager>();
                if (webRtcManager == null)
                {
                    Debug.LogError("[VrHapticRequester] WebRtcManager not found!");
                    enabled = false;
                }
            }
        }

        void Update()
        {
            if (!webRtcManager.IsDataChannelOpen) return;

            // 버튼 입력 감지
            if (enableButtonHaptics)
            {
                DetectButtonInputs();
            }

            // 트리거 입력 감지
            if (enableTriggerHaptics)
            {
                DetectTriggerInputs();
            }

            // 그립 입력 감지
            if (enableGripHaptics)
            {
                DetectGripInputs();
            }

            // 손 추적 제스처 감지
            if (enableHandTrackingHaptics && OVRPlugin.GetHandTrackingEnabled())
            {
                DetectHandGestures();
            }
        }

        private void DetectButtonInputs()
        {
            // A 버튼 (오른손)
            if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                RequestHapticFeedback(HapticCommandType.VibrateShort, 0.1f, 0.8f);
                if (debugMode) Debug.Log("[VrHapticRequester] A button pressed");
            }
            
            // B 버튼 (오른손)
            if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
            {
                RequestHapticFeedback(HapticCommandType.VibrateShort, 0.1f, 0.8f);
                if (debugMode) Debug.Log("[VrHapticRequester] B button pressed");
            }
            
            // X 버튼 (왼손)
            if (OVRInput.GetDown(OVRInput.Button.Three, OVRInput.Controller.LTouch))
            {
                RequestHapticFeedback(HapticCommandType.VibrateShort, 0.1f, 0.8f);
                if (debugMode) Debug.Log("[VrHapticRequester] X button pressed");
            }
            
            // Y 버튼 (왼손)
            if (OVRInput.GetDown(OVRInput.Button.Four, OVRInput.Controller.LTouch))
            {
                RequestHapticFeedback(HapticCommandType.VibrateShort, 0.1f, 0.8f);
                if (debugMode) Debug.Log("[VrHapticRequester] Y button pressed");
            }
            
            // 조이스틱 클릭
            if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick) || 
                OVRInput.GetDown(OVRInput.Button.SecondaryThumbstick))
            {
                RequestHapticFeedback(HapticCommandType.VibrateShort, 0.05f, 0.5f);
                if (debugMode) Debug.Log("[VrHapticRequester] Thumbstick clicked");
            }
        }

        private void DetectTriggerInputs()
        {
            float rightTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            float leftTrigger = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);
            float currentTrigger = Mathf.Max(rightTrigger, leftTrigger);
            
            // 트리거를 완전히 당겼을 때
            if (lastTriggerValue < 0.9f && currentTrigger >= 0.9f)
            {
                RequestHapticFeedback(HapticCommandType.VibrateLong, 0.2f, 1.0f);
                if (debugMode) Debug.Log("[VrHapticRequester] Trigger fully pressed");
            }
            // 트리거를 절반 이상 당겼을 때
            else if (lastTriggerValue < 0.5f && currentTrigger >= 0.5f)
            {
                RequestHapticFeedback(HapticCommandType.VibrateShort, 0.05f, 0.3f);
                if (debugMode) Debug.Log("[VrHapticRequester] Trigger half pressed");
            }
            
            lastTriggerValue = currentTrigger;
        }

        private void DetectGripInputs()
        {
            float rightGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
            float leftGrip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);
            float currentGrip = Mathf.Max(rightGrip, leftGrip);
            
            // 그립 시작
            if (lastGripValue < 0.5f && currentGrip >= 0.5f)
            {
                isGrabbing = true;
                RequestHapticFeedback(HapticCommandType.VibrateDefault, 0.1f, 0.7f);
                if (debugMode) Debug.Log("[VrHapticRequester] Grip started");
            }
            // 그립 해제
            else if (lastGripValue >= 0.5f && currentGrip < 0.5f)
            {
                isGrabbing = false;
                RequestHapticFeedback(HapticCommandType.VibrateShort, 0.05f, 0.4f);
                if (debugMode) Debug.Log("[VrHapticRequester] Grip released");
            }
            
            lastGripValue = currentGrip;
        }

        private void DetectHandGestures()
        {
            // 손 추적을 사용한 제스처 감지 (추후 구현)
            // 예: 핀치, 손가락 탭, 주먹 쥐기 등
        }

        public void RequestHapticFeedback(HapticCommandType type, float duration = 0.1f, float intensity = 1.0f)
        {
            if (webRtcManager != null && webRtcManager.IsDataChannelOpen)
            {
                HapticCommand command = new HapticCommand(type, duration, intensity);
                if (debugMode) Debug.Log($"[VrHapticRequester] Sending Haptic Command: {type}, Duration: {duration}s, Intensity: {intensity}");
                webRtcManager.SendDataChannelMessage(command);
            }
        }

        // 충돌 이벤트 처리
        void OnCollisionEnter(Collision collision)
        {
            if (!enableCollisionHaptics || !webRtcManager.IsDataChannelOpen) return;
            
            // 충돌 강도에 따른 햅틱 피드백
            float impactForce = collision.relativeVelocity.magnitude;
            float normalizedForce = Mathf.Clamp01(impactForce / 10f); // 10m/s를 최대로 정규화
            
            if (normalizedForce > 0.1f) // 최소 임계값
            {
                float duration = Mathf.Lerp(0.05f, 0.3f, normalizedForce);
                RequestHapticFeedback(HapticCommandType.VibrateCustom, duration, normalizedForce);
                if (debugMode) Debug.Log($"[VrHapticRequester] Collision haptic: Force={impactForce:F2}, Intensity={normalizedForce:F2}");
            }
        }

        // 외부에서 햅틱 요청을 할 수 있는 공개 메서드들
        public void OnObjectGrabbed()
        {
            RequestHapticFeedback(HapticCommandType.VibrateDefault, 0.15f, 0.8f);
        }

        public void OnObjectReleased()
        {
            RequestHapticFeedback(HapticCommandType.VibrateShort, 0.05f, 0.5f);
        }

        public void OnMenuOpened()
        {
            RequestHapticFeedback(HapticCommandType.VibrateShort, 0.1f, 0.6f);
        }

        public void OnTeleport()
        {
            StartCoroutine(TeleportHapticSequence());
        }

        private IEnumerator TeleportHapticSequence()
        {
            // 텔레포트 시 특별한 햅틱 시퀀스
            RequestHapticFeedback(HapticCommandType.VibrateShort, 0.05f, 0.3f);
            yield return new WaitForSeconds(0.1f);
            RequestHapticFeedback(HapticCommandType.VibrateShort, 0.05f, 0.6f);
            yield return new WaitForSeconds(0.1f);
            RequestHapticFeedback(HapticCommandType.VibrateLong, 0.2f, 1.0f);
        }

        // Inspector에서 테스트용
        [ContextMenu("Test Short Haptic")]
        void TestShortHaptic()
        {
            RequestHapticFeedback(HapticCommandType.VibrateShort);
        }

        [ContextMenu("Test Long Haptic")]
        void TestLongHaptic()
        {
            RequestHapticFeedback(HapticCommandType.VibrateLong, 0.5f);
        }
    }
}