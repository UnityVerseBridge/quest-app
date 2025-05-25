using UnityEngine;
using UnityEngine.UI;
using UnityVerseBridge.Core;
using UnityVerseBridge.QuestApp;
using UnityVerseBridge.Core.Signaling;
using UnityVerseBridge.Core.Signaling.Adapters;
using System.Threading.Tasks;
using TMPro;

namespace UnityVerseBridge.QuestApp.Test
{
    public class WebRtcConnectionTester : MonoBehaviour
    {
        [Header("필수 컴포넌트")]
        [SerializeField] private WebRtcManager webRtcManager;
        [SerializeField] private VrStreamSender streamSender;
        [SerializeField] private VrTouchReceiver touchReceiver;
        [SerializeField] private VrHapticRequester hapticRequester;

        [Header("UI 요소")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button connectButton;
        [SerializeField] private Button disconnectButton;
        [SerializeField] private Button sendTestMessageButton;
        [SerializeField] private InputField serverUrlInput;

        [Header("테스트 설정")]
        [SerializeField] private string defaultServerUrl = "ws://localhost:8080";
        [SerializeField] private RenderTexture testRenderTexture;
        [SerializeField] private bool autoConnectOnStartByTester = false; // 테스터의 자동 연결 플래그 (QuestAppInitializer와 역할 분리)
        private bool isInitializerHandlingConnection = false; // QuestAppInitializer가 연결을 처리 중인지 확인

        private bool uiDrivenIsConnected = false; // WebRTC 연결 상태 (UI 업데이트 및 버튼 제어용)

        void Start()
        {
            Debug.Log("[WebRtcConnectionTester] Start() 호출됨");
            if (serverUrlInput != null)
                serverUrlInput.text = defaultServerUrl;

            if (connectButton != null)
                connectButton.onClick.AddListener(HandleConnectButtonClicked);
            
            if (disconnectButton != null)
                disconnectButton.onClick.AddListener(HandleDisconnectButtonClicked);
            
            if (sendTestMessageButton != null)
                sendTestMessageButton.onClick.AddListener(SendTestMessage);

            CheckComponents();

            if (webRtcManager != null)
            {
                Debug.Log("[WebRtcConnectionTester] WebRtcManager 이벤트 구독 설정 중...");
                webRtcManager.OnSignalingConnected += HandleSignalingClientConnected;
                webRtcManager.OnSignalingDisconnected += HandleSignalingClientDisconnected;
                webRtcManager.OnWebRtcConnected += HandleWebRtcActualConnected;
                webRtcManager.OnWebRtcDisconnected += HandleWebRtcActualDisconnected;
                webRtcManager.OnDataChannelOpened += HandleDataChannelOpened;
                webRtcManager.OnDataChannelClosed += HandleDataChannelClosed;
                webRtcManager.OnDataChannelMessageReceived += HandleDataChannelMessage;
            }
            else
            {
                LogStatus("오류: WebRtcManager가 할당되지 않아 이벤트 구독 불가!");
                if (connectButton != null) connectButton.interactable = false;
                if (disconnectButton != null) disconnectButton.interactable = false;
                if (sendTestMessageButton != null) sendTestMessageButton.interactable = false;
            }

            UpdateUI(); // 초기 UI 상태 설정
            
            // QuestAppInitializer가 이미 연결을 처리하고 있는지 확인
            var initializer = FindObjectOfType<QuestAppInitializer>();
            if (initializer != null && initializer.enabled)
            {
                isInitializerHandlingConnection = true;
                Debug.Log("[WebRtcConnectionTester] QuestAppInitializer가 연결을 처리합니다. 테스터는 모니터링만 수행합니다.");
            }
            else if (autoConnectOnStartByTester && webRtcManager != null)
            {
                Debug.Log("[WebRtcConnectionTester] autoConnectOnStartByTester=true. 연결 시도...");
                InitiateConnection();
            }
            Debug.Log("[WebRtcConnectionTester] Start() 완료.");
        }

        void OnDestroy()
        {
            if (webRtcManager != null)
            {
                Debug.Log("[WebRtcConnectionTester] WebRtcManager 이벤트 구독 해지 중...");
                webRtcManager.OnSignalingConnected -= HandleSignalingClientConnected;
                webRtcManager.OnSignalingDisconnected -= HandleSignalingClientDisconnected;
                webRtcManager.OnWebRtcConnected -= HandleWebRtcActualConnected;
                webRtcManager.OnWebRtcDisconnected -= HandleWebRtcActualDisconnected;
                webRtcManager.OnDataChannelOpened -= HandleDataChannelOpened;
                webRtcManager.OnDataChannelClosed -= HandleDataChannelClosed;
                webRtcManager.OnDataChannelMessageReceived -= HandleDataChannelMessage;
            }
            Debug.Log("[WebRtcConnectionTester] OnDestroy() 완료.");
        }

        private void CheckComponents()
        {
            if (webRtcManager == null)
            {
                LogStatus("오류: WebRtcManager가 할당되지 않았습니다! 테스터 비활성화.");
                if (connectButton != null) connectButton.interactable = false;
                enabled = false; 
                return;
            }
            if (streamSender == null) LogStatus("경고: VrStreamSender가 할당되지 않았습니다.");

            if (touchReceiver == null)
                LogStatus("경고: VrTouchReceiver가 할당되지 않았습니다. 터치 입력을 받을 수 없습니다.");

            if (hapticRequester == null)
                LogStatus("경고: VrHapticRequester가 할당되지 않았습니다. 햅틱 요청을 보낼 수 없습니다.");
        }

        private void HandleConnectButtonClicked()
        {
            Debug.Log("[WebRtcConnectionTester] 연결 버튼 클릭됨.");
            InitiateConnection();
        }

        private async void InitiateConnection() 
        {
            if (isInitializerHandlingConnection)
            {
                LogStatus("QuestAppInitializer가 연결을 처리 중입니다.");
                return;
            }
            
            if (webRtcManager == null)
            {
                LogStatus("오류: WebRtcManager가 없어 연결할 수 없습니다.");
                return;
            }

            if (webRtcManager.IsSignalingConnected || webRtcManager.IsWebRtcConnected)
            {
                LogStatus("이미 연결되었거나 연결 시도 중입니다 (WebRtcManager 기준).");
                UpdateUI(); // UI 최신화
                return;
            }

            string serverUrl = (serverUrlInput != null && !string.IsNullOrEmpty(serverUrlInput.text)) 
                               ? serverUrlInput.text 
                               : defaultServerUrl;
            
            LogStatus($"WebRtcManager를 통해 시그널링 서버 ({serverUrl}) 연결 시작 요청...");
            // WebRtcManager의 새로운 메서드 호출 (QuestAppInitializer에서 이미 SignalingClient를 설정했다고 가정)
            await webRtcManager.StartSignalingAndPeerConnection(serverUrl); 
            // 실제 연결 상태는 이벤트를 통해 HandleSignalingClientConnected/HandleWebRtcActualConnected에서 업데이트됨
            UpdateUI(); // 요청 후 즉시 UI 업데이트 (버튼 비활성화 등)
        }

        private void HandleDisconnectButtonClicked()
        {
            Debug.Log("[WebRtcConnectionTester] 연결 종료 버튼 클릭됨.");
            if (webRtcManager == null)
            {
                LogStatus("오류: WebRtcManager가 없어 연결 해제할 수 없습니다.");
                return;
            }
            webRtcManager.Disconnect();
            LogStatus("WebRtcManager에 연결 종료 요청됨.");
            UpdateUI(); // 요청 후 즉시 UI 업데이트
        }

        public void SendTestMessage()
        {
            if (webRtcManager == null || !webRtcManager.IsDataChannelOpen)
            {
                LogStatus("데이터 채널이 열려있지 않아 메시지를 보낼 수 없습니다.");
                return;
            }
            var testMessage = new { type = "test", content = "Hello from Quest Tester!", timestamp = System.DateTime.Now.ToString() };
            string json = JsonUtility.ToJson(testMessage);
            webRtcManager.SendDataChannelMessage(json);
            LogStatus($"테스트 메시지 전송됨: {json.Substring(0, Mathf.Min(json.Length, 100))}...");
        }

        private void HandleSignalingClientConnected()
        {
            LogStatus("시그널링 클라이언트 연결됨 (WebRtcManager 이벤트 수신)");
            UpdateUI();
        }
        
        private void HandleSignalingClientDisconnected()
        {
            LogStatus("시그널링 클라이언트 연결 끊김 (WebRtcManager 이벤트 수신)");
            uiDrivenIsConnected = false; 
            UpdateUI();
        }

        private void HandleWebRtcActualConnected()
        {
            uiDrivenIsConnected = true;
            LogStatus("WebRTC 실제 연결 성공! (WebRtcManager 이벤트 수신)");
            UpdateUI();
        }

        private void HandleWebRtcActualDisconnected()
        {
            uiDrivenIsConnected = false;
            LogStatus("WebRTC 실제 연결 종료 (WebRtcManager 이벤트 수신)");
            UpdateUI();
        }

        private void HandleDataChannelOpened(string channelId)
        {
            LogStatus($"데이터 채널 열림 (ID: {channelId}). 메시지 전송 가능.");
            UpdateUI();
        }

        private void HandleDataChannelClosed()
        {
            LogStatus("데이터 채널 닫힘.");
            UpdateUI();
        }

        private void HandleDataChannelMessage(string message)
        {
            LogStatus($"메시지 수신: {message.Substring(0, Mathf.Min(message.Length, 100))}...");
        }

        private void LogStatus(string message)
        {
            Debug.Log($"[WebRtcConnectionTester] {message}");
            if (statusText != null)
            {
                string[] lines = statusText.text.Split('\n');
                string newText = message;
                int maxLines = 4;
                for (int i = 0; i < Mathf.Min(lines.Length, maxLines); i++)
                {
                    if (!string.IsNullOrEmpty(lines[i]))
                        newText += "\n" + lines[i];
                }
                statusText.text = newText;
            }
        }

        private void UpdateUI()
        {
            // WebRtcManager의 실제 상태를 기반으로 UI 업데이트
            bool signalingConnected = webRtcManager != null && webRtcManager.IsSignalingConnected;
            bool webRtcConnected = webRtcManager != null && webRtcManager.IsWebRtcConnected;
            bool dataChannelOpen = webRtcManager != null && webRtcManager.IsDataChannelOpen;

            // 로컬 isConnected 플래그도 WebRTC 연결 상태를 따르도록 함 (주로 버튼 활성화에 사용)
            uiDrivenIsConnected = webRtcConnected; 

            if (connectButton != null)
                connectButton.interactable = !signalingConnected && !webRtcConnected;
            
            if (disconnectButton != null)
                disconnectButton.interactable = signalingConnected || webRtcConnected;
            
            if (sendTestMessageButton != null)
                sendTestMessageButton.interactable = webRtcConnected && dataChannelOpen;
            
            if (serverUrlInput != null)
                serverUrlInput.interactable = !signalingConnected && !webRtcConnected;
                
            Debug.Log($"[WebRtcConnectionTester] UpdateUI: Signaling={signalingConnected}, WebRTC={webRtcConnected}, DataChannel={dataChannelOpen}, uiDrivenIsConnected={uiDrivenIsConnected}");
        }
    }
} 