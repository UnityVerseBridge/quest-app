using UnityEngine;
using UnityVerseBridge.Core; // WebRtcManager 사용
using UnityVerseBridge.QuestApp.Signaling; // MetaVoiceWebSocketAdapter 사용

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Quest 앱 시작 시 WebRtcManager와 플랫폼별 Signaling Adapter를 초기화합니다.
    /// </summary>
    public class QuestAppInitializer : MonoBehaviour
    {
        // 씬에 있는 WebRtcManager를 Inspector에서 연결하거나 FindObjectOfType으로 찾음
        [SerializeField] private WebRtcManager webRtcManager;
        // 필요시 WebRtcConfiguration도 여기서 관리 가능
        [SerializeField] private WebRtcConfiguration webRtcConfiguration;

        void Start()
        {
            // WebRtcManager 인스턴스 찾기 (Inspector 할당 안됐을 경우)
            if (webRtcManager == null)
            {
                webRtcManager = FindObjectOfType<WebRtcManager>();
            }

            if (webRtcManager == null)
            {
                Debug.LogError("WebRtcManager를 찾을 수 없습니다! 초기화 실패.");
                return;
            }

            // 1. Quest 환경에 맞는 WebSocket 어댑터 생성
            var metaAdapter = new MetaVoiceWebSocketAdapter();

            // 2. WebRtcManager의 초기화 메서드를 호출하여 어댑터 주입 및 시그널링 시작 요청
            //    (WebRtcManager에 아래와 같은 InitializeSignaling 메서드가 있다고 가정)
            webRtcManager.InitializeSignaling(metaAdapter); // WebRtcManager가 내부적으로 SignalingClient 생성 및 초기화

             // 3. (선택 사항) 구성(Configuration)도 여기서 설정 가능
             // webRtcManager.SetConfiguration(webRtcConfiguration);

             // 4. (선택 사항) 시그널링 자동 연결 (WebRtcManager의 Start에서 이미 한다면 중복 호출 불필요)
             // webRtcManager.ConnectSignaling();
        }
    }

    // --- Quest Initializer용 Assembly Definition ---
    // 경로: quest-app/UnityProject/Assets/Scripts/UnityVerseBridge.QuestApp.asmdef
    /* JSON 내용:
    {
        "name": "UnityVerseBridge.QuestApp",
        "rootNamespace": "UnityVerseBridge.QuestApp",
        "references": [
            "UnityVerseBridge.Core.Runtime", // Core 로직 참조
            "UnityVerseBridge.QuestApp.Signaling.Adapters", // Meta Adapter 참조
            "Oculus.VR" // OVRInput 등 사용 시 필요
            // 기타 필요한 참조들
        ],
        "includePlatforms": [ "Android" ], // 선택 사항
        // ... 나머지 asmdef 설정 ...
    }
    */
}