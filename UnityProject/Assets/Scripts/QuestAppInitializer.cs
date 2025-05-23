using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityVerseBridge.Core; // WebRtcManager 사용
using UnityVerseBridge.Core.Signaling; // SignalingClient 사용
using UnityVerseBridge.Core.Signaling.Data; // SignalingMessageBase 사용
using UnityVerseBridge.QuestApp.Signaling; // SystemWebSocketAdapter 사용

namespace UnityVerseBridge.QuestApp
{
    [System.Serializable]
    public class RegisterMessage : SignalingMessageBase
    {
        public string peerId;
        public string clientType;
        public string roomId;
        
        public RegisterMessage()
        {
            type = "register";
        }
    }
    
    /// <summary>
    /// Quest 앱 시작 시 WebRtcManager와 플랫폼별 Signaling Adapter를 초기화하고 연결을 시작합니다.
    /// </summary>
    public class QuestAppInitializer : MonoBehaviour
    {
        private string clientId;
        private string roomId = "room_123"; // 테스트용 룸 ID
        private SystemWebSocketAdapter webSocketAdapter; // WebSocket 어댑터
        private SignalingClient signalingClient; // 시그널링 클라이언트
        private bool hasMobilePeer = false; // Mobile peer가 접속했는지 여부

        [Tooltip("씬에 있는 WebRtcManager를 연결합니다.")]
        [SerializeField] private WebRtcManager webRtcManager;
        [Tooltip("WebRTC 설정을 담은 ScriptableObject 또는 컴포넌트를 연결합니다 (선택 사항).")]
        [SerializeField] private WebRtcConfiguration webRtcConfiguration; // 예시, 실제 타입에 맞게 조정
        [Tooltip("접속할 시그널링 서버의 기본 주소입니다.")]
        [SerializeField] private string defaultSignalingServerUrl = "ws://localhost:8080";
        [Tooltip("앱 시작 시 자동으로 시그널링 및 WebRTC 연결을 시도합니다.")]
        [SerializeField] private bool autoConnectOnStart = true;

        void Start()
        {
            Debug.Log("[QuestAppInitializer] Start() 호출됨");
            
            // 클라이언트 ID 생성
            clientId = "quest_" + SystemInfo.deviceUniqueIdentifier;

            if (webRtcManager == null)
            {
                Debug.LogError("[QuestAppInitializer] WebRtcManager not found in scene. Please add WebRtcManager component to a GameObject.");
                return; // WebRtcManager 없이는 진행 불가
            }

            // WebRtcManager 역할 설정 (QuestApp은 Offerer)
            webRtcManager.SetRole(true);

            // WebSocket 어댑터와 시그널링 클라이언트 생성
            webSocketAdapter = new SystemWebSocketAdapter();
            Debug.Log("[QuestAppInitializer] SystemWebSocketAdapter 생성됨.");

            signalingClient = new SignalingClient();
            Debug.Log("[QuestAppInitializer] SignalingClient 생성됨.");

            // --- 중요: WebRtcManager에 SignalingClient 설정 ---
            if (webRtcManager != null && signalingClient != null)
            {
                webRtcManager.SetupSignaling(signalingClient);
                Debug.Log("[QuestAppInitializer] WebRtcManager에 SignalingClient 설정 완료.");
            }
            // --- 여기까지 추가 ---

            // 3. (선택 사항) 구성(Configuration) 설정
            if (webRtcConfiguration != null)
            {
                webRtcManager.SetConfiguration(webRtcConfiguration);
                Debug.Log("[QuestAppInitializer] WebRtcManager.SetConfiguration() 호출 완료.");
            }
            else
            {
                Debug.LogWarning("[QuestAppInitializer] webRtcConfiguration이 할당되지 않았습니다. WebRtcManager의 기본 설정을 사용합니다.");
            }

            // 4. 시그널링 자동 연결
            if (autoConnectOnStart)
            {
                Debug.Log($"[QuestAppInitializer] autoConnectOnStart=true. 시그널링 서버 ({defaultSignalingServerUrl}) 연결 시도...");
                StartSignalingConnection();
            }
            else
            {
                Debug.Log("[QuestAppInitializer] autoConnectOnStart=false. 자동 연결을 시작하지 않습니다.");
            }
            Debug.Log("[QuestAppInitializer] Start() 완료.");
        }

        private async void StartSignalingConnection()
        {
            try
            {
                // SignalingClient에 WebSocket 어댑터를 연결하고 서버에 연결
                await signalingClient.InitializeAndConnect(webSocketAdapter, defaultSignalingServerUrl);
                Debug.Log("[QuestAppInitializer] SignalingClient 연결 성공");
                
                // WebSocket이 열려있는지 확인
                await Task.Delay(100); // 연결 안정화 대기
                
                // 클라이언트 등록
                await RegisterClient();
                
                // peer-joined 이벤트 구독
                signalingClient.OnSignalingMessageReceived += HandleSignalingMessage;
                
                // Register 완료 후 mobile peer를 기다림
                Debug.Log("[QuestAppInitializer] Waiting for mobile peer to join...");
                
                // Mobile peer가 연결될 때까지 기다림 (timeout: 30초)
                int waitTime = 0;
                while (!hasMobilePeer && waitTime < 30000)
                {
                    await Task.Delay(100);
                    waitTime += 100;
                }
                
                if (hasMobilePeer && webRtcManager != null)
                {
                    Debug.Log("[QuestAppInitializer] Mobile peer joined! Starting PeerConnection...");
                    webRtcManager.StartPeerConnection();
                }
                else
                {
                    Debug.LogError("[QuestAppInitializer] Timeout waiting for mobile peer or WebRtcManager is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestAppInitializer] 시그널링 연결 실패: {ex.Message}");
                // 재시도 로직
                Debug.Log("[QuestAppInitializer] 5초 후 재연결 시도...");
                await Task.Delay(5000);
                StartSignalingConnection();
            }
        }
        
        private async Task RegisterClient()
        {
            try
            {
                var registerMessage = new RegisterMessage
                {
                    peerId = clientId,
                    clientType = "quest",
                    roomId = roomId
                };
                
                string jsonMessage = JsonUtility.ToJson(registerMessage);
                Debug.Log($"[QuestAppInitializer] Registering client: {jsonMessage}");
                
                // JSON 문자열로 직접 전송
                if (webSocketAdapter != null)
                {
                    await webSocketAdapter.SendText(jsonMessage);
                    Debug.Log("[QuestAppInitializer] Client registered successfully");
                }
                else
                {
                    Debug.LogError("[QuestAppInitializer] WebSocketAdapter is null");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestAppInitializer] Failed to register client: {ex.Message}");
            }
        }
        
        private void HandleSignalingMessage(string type, string jsonData)
        {
            if (type == "peer-joined")
            {
                try
                {
                    var peerInfo = JsonUtility.FromJson<PeerJoinedMessage>(jsonData);
                    if (peerInfo.clientType == "mobile")
                    {
                        Debug.Log($"[QuestAppInitializer] Mobile peer joined: {peerInfo.peerId}");
                        hasMobilePeer = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[QuestAppInitializer] Failed to parse peer-joined message: {ex.Message}");
                }
            }
        }
        
        void Update()
        {
            // SystemWebSocket의 메시지 큐 처리
            webSocketAdapter?.DispatchMessageQueue();
            
            // SignalingClient의 메시지 처리
            signalingClient?.DispatchMessages();
        }
        
        void OnDestroy()
        {
            if (signalingClient != null)
            {
                signalingClient.OnSignalingMessageReceived -= HandleSignalingMessage;
            }
        }
    }
    
    [System.Serializable]
    public class PeerJoinedMessage
    {
        public string type;
        public string peerId;
        public string clientType;
    }
}