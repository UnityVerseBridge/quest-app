// VrHapticRequester.cs 예시
using UnityEngine;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.DataChannel.Data;

public class VrHapticRequester : MonoBehaviour
{
    [SerializeField] private WebRtcManager webRtcManager;

    // 예시: OVRInput 또는 다른 입력 시스템으로 버튼 클릭 감지
    void Update()
    {
        // 예시: Oculus 컨트롤러의 A 버튼 클릭 시 기본 진동 요청
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            RequestHapticFeedback(HapticCommandType.VibrateDefault);
        }
    }

    public void RequestHapticFeedback(HapticCommandType type, float duration = 0.1f) // 기본 지속 시간 0.1초
    {
        if (webRtcManager != null && webRtcManager.IsWebRtcConnected) // IsDataChannelOpen 체크가 더 좋음
        {
            HapticCommand command = new HapticCommand(type, duration);
            Debug.Log($"[VrHapticRequester] Sending Haptic Command: {type}");
            webRtcManager.SendDataChannelMessage(command);
        }
    }
}