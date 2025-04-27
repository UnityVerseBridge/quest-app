using UnityEngine;
using UnityVerseBridge.Core; // WebRtcManager 사용
using UnityVerseBridge.Core.DataChannel.Data; // 데이터 구조 사용
using System; // Exception 사용

public class VrTouchReceiver : MonoBehaviour
{
    [SerializeField] private WebRtcManager webRtcManager;

    // 터치 위치를 시각화할 프리팹 또는 오브젝트 (선택 사항)
    // [SerializeField] private GameObject touchIndicatorPrefab;
    // 터치 위치를 표시할 기준 표면 (예: 가상의 캔버스)
    // [SerializeField] private Transform touchSurface;

    void Start()
    {
        if (webRtcManager == null)
        {
            Debug.LogError("WebRtcManager가 Inspector에 할당되지 않았습니다!");
            enabled = false;
            return;
        }
        // 데이터 채널 메시지 수신 이벤트 구독
        webRtcManager.OnDataChannelMessageReceived += HandleDataChannelMessageReceived;
    }

    void OnDestroy()
    {
        // 이벤트 구독 해지
        if (webRtcManager != null)
        {
            webRtcManager.OnDataChannelMessageReceived -= HandleDataChannelMessageReceived;
        }
    }

    private void HandleDataChannelMessageReceived(string jsonData)
    {
        // Debug.Log($"[VrTouchReceiver] Raw message received: {jsonData}"); // 원시 데이터 로그

        if (string.IsNullOrEmpty(jsonData)) return;

        try
        {
            // 1. 메시지 타입 확인을 위해 기본 클래스로 파싱
            DataChannelMessageBase baseMsg = JsonUtility.FromJson<DataChannelMessageBase>(jsonData);

            if (baseMsg == null || string.IsNullOrEmpty(baseMsg.type))
            {
                Debug.LogWarning($"[VrTouchReceiver] Cannot determine message type: {jsonData}");
                return;
            }

            // 2. 타입에 따라 분기하여 처리
            if (baseMsg.type == "touch")
            {
                TouchData touchData = JsonUtility.FromJson<TouchData>(jsonData);
                if (touchData != null)
                {
                    // 3. 수신된 터치 데이터 처리 (★ 여기부터 구현 ★)
                    ProcessTouchData(touchData);
                }
            }
            // else if (baseMsg.type == "other_type") { ... }

        }
        catch (Exception e)
        {
            Debug.LogError($"[VrTouchReceiver] Failed to parse JSON message: '{jsonData}' | Error: {e.Message}");
        }
    }

    private void ProcessTouchData(TouchData data)
    {
        // 수신된 데이터 로그 출력 (기본)
        Debug.Log($"[VrTouchReceiver] Processed Touch: ID={data.touchId}, Phase={data.phase}, Pos=({data.positionX:F3}, {data.positionY:F3})");

        // --- 다음 단계에서 구현할 내용 ---
        // TODO: 수신된 정규화 좌표(data.positionX, data.positionY)를
        //       VR 공간 내의 적절한 좌표로 변환하는 로직 필요.
        //       (예: 특정 UI 패널, 가상 테이블 위 등 기준 표면에 매핑)

        // TODO: 변환된 좌표에 시각적 피드백(파티클, 포인터 등)을 표시하거나
        //       해당 위치의 가상 객체와 상호작용하는 로직 구현.

        // TODO: 터치 상태(Phase)에 따라 다른 처리 구현 (Began, Moved, Ended)
        //       예: Began - 표시 생성, Moved - 표시 이동, Ended - 표시 제거
    }
}