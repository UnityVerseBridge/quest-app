using UnityEngine;
using UnityVerseBridge.Core; // WebRtcManager 사용
using UnityVerseBridge.Core.DataChannel.Data; // 데이터 구조 사용
using System; // Exception 사용
using TouchPhase = UnityVerseBridge.Core.DataChannel.Data.TouchPhase; // 명시적 타입 지정

namespace UnityVerseBridge.QuestApp
{
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

        [Header("Touch Visualization")]
        [SerializeField] private Camera vrCamera; // VR 카메라 (터치 좌표 변환용)
        [SerializeField] private float touchRayDistance = 10f; // 레이캐스트 거리
        [SerializeField] private GameObject touchPointerPrefab; // 터치 위치 표시용 프리팹
        
        private GameObject currentTouchPointer;
        
        private void ProcessTouchData(TouchData data)
        {
            // 수신된 데이터 로그 출력
            Debug.Log($"[VrTouchReceiver] Touch: ID={data.touchId}, Phase={data.phase}, Pos=({data.positionX:F3}, {data.positionY:F3})");

            // VR 카메라가 없으면 메인 카메라 사용
            if (vrCamera == null)
            {
                vrCamera = Camera.main;
                if (vrCamera == null)
                {
                    Debug.LogError("[VrTouchReceiver] No camera found for touch processing!");
                    return;
                }
            }

            // 정규화된 좌표를 스크린 좌표로 변환
            Vector3 screenPos = new Vector3(
                data.positionX * Screen.width,
                data.positionY * Screen.height,
                0f
            );

            // 스크린 좌표를 월드 좌표로 변환 (레이캐스트)
            Ray ray = vrCamera.ScreenPointToRay(screenPos);
            RaycastHit hit;
            
            Vector3 worldPos;
            if (Physics.Raycast(ray, out hit, touchRayDistance))
            {
                worldPos = hit.point;
                Debug.Log($"[VrTouchReceiver] Touch hit at: {worldPos}, Object: {hit.collider.gameObject.name}");
                
                // UI 오브젝트인 경우 클릭 이벤트 발생
                if (hit.collider.GetComponent<UnityEngine.UI.Button>() != null)
                {
                    hit.collider.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                }
            }
            else
            {
                // 레이캐스트가 실패하면 카메라 전방 일정 거리에 위치
                worldPos = ray.origin + ray.direction * touchRayDistance;
            }

            // 터치 상태에 따른 처리
            switch (data.phase)
            {
                case TouchPhase.Began:
                    if (touchPointerPrefab != null && currentTouchPointer == null)
                    {
                        currentTouchPointer = Instantiate(touchPointerPrefab, worldPos, Quaternion.identity);
                    }
                    break;
                    
                case TouchPhase.Moved:
                    if (currentTouchPointer != null)
                    {
                        currentTouchPointer.transform.position = worldPos;
                    }
                    break;
                    
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (currentTouchPointer != null)
                    {
                        Destroy(currentTouchPointer);
                        currentTouchPointer = null;
                    }
                    break;
            }
        }
    }
}