using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.Signaling;
using UnityVerseBridge.Core.Signaling.Data;
using UnityVerseBridge.Core.Signaling.Adapters;
using UnityVerseBridge.Core.Signaling.Messages;
using Unity.WebRTC;

namespace UnityVerseBridge.QuestApp
{
    public class QuestAppInitializer : MonoBehaviour
    {
        private string clientId;
        private SystemWebSocketAdapter webSocketAdapter;
        private SignalingClient signalingClient;
        private bool hasMobilePeer = false;

        [Header("Dependencies")]
        [SerializeField] private WebRtcManager webRtcManager;
        [SerializeField] private ConnectionConfig connectionConfig;
        [SerializeField] private WebRtcConfiguration webRtcConfiguration;
        
        [Header("Settings")]
        [SerializeField] private bool autoConnectOnStart = true;

        void Start()
        {
            try
            {
                // Critical: WebRTC.Update() coroutine must be started first
                StartCoroutine(WebRTC.Update());
                
                // Note: In newer Unity WebRTC versions, explicit Initialize() is not needed
                // WebRTC initializes automatically when first used
                
                InitializeApp();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestAppInitializer] Failed to initialize: {ex.Message}");
            }
        }

        private void InitializeApp()
        {
            Debug.Log("[QuestAppInitializer] Start() 호출됨");
            
            // Generate secure client ID
            var deviceId = SystemInfo.deviceUniqueIdentifier;
            clientId = $"quest_{GenerateHashedId(deviceId)}";

            if (!ValidateDependencies())
            {
                throw new InvalidOperationException("Required dependencies are missing");
            }

            // WebRtcManager 설정
            webRtcManager.SetRole(true);
            
            // WebSocket 어댑터와 시그널링 클라이언트 생성
            webSocketAdapter = new SystemWebSocketAdapter();
            signalingClient = new SignalingClient();
            
            webRtcManager.SetupSignaling(signalingClient);
            
            if (webRtcConfiguration != null)
            {
                webRtcManager.SetConfiguration(webRtcConfiguration);
            }

            if (autoConnectOnStart)
            {
                var serverUrl = connectionConfig.signalingServerUrl;
                Debug.Log($"[QuestAppInitializer] Connecting to {serverUrl}...");
                StartSignalingConnection();
            }
        }

        private bool ValidateDependencies()
        {
            if (webRtcManager == null)
            {
                Debug.LogError("[QuestAppInitializer] WebRtcManager not assigned!");
                return false;
            }
            
            if (connectionConfig == null)
            {
                Debug.LogError("[QuestAppInitializer] ConnectionConfig not assigned!");
                return false;
            }
            
            return true;
        }

        private string GenerateHashedId(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").Substring(0, 16).ToLower();
            }
        }

        private async void StartSignalingConnection()
        {
            int retryCount = 0;
            int maxRetries = connectionConfig.maxReconnectAttempts;
            
            while (retryCount < maxRetries)
            {
                try
                {
                    string serverUrl = connectionConfig.signalingServerUrl;
                    
                    // Authentication if required
                    string connectUrl = serverUrl;
                    if (connectionConfig.requireAuthentication)
                    {
                        Debug.Log("[QuestAppInitializer] Authenticating...");
                        bool authSuccess = await AuthenticationHelper.AuthenticateAsync(
                            serverUrl,
                            clientId, 
                            "quest", 
                            connectionConfig.authKey
                        );
                        
                        if (!authSuccess)
                        {
                            throw new Exception("Authentication failed");
                        }
                        
                        // Add token to URL if authenticated
                        connectUrl = AuthenticationHelper.AppendTokenToUrl(serverUrl);
                    }
                    
                    await signalingClient.InitializeAndConnect(webSocketAdapter, connectUrl);
                    Debug.Log("[QuestAppInitializer] SignalingClient 연결 성공");
                    
                    await Task.Delay(100);
                    await RegisterClient();
                    
                    signalingClient.OnSignalingMessageReceived += HandleSignalingMessage;
                    
                    // Wait for mobile peer
                    await WaitForMobilePeer();
                    
                    if (hasMobilePeer && webRtcManager != null)
                    {
                        Debug.Log("[QuestAppInitializer] Starting PeerConnection...");
                        webRtcManager.StartPeerConnection();
                    }
                    
                    break; // Success
                }
                catch (Exception ex)
                {
                    retryCount++;
                    Debug.LogError($"[QuestAppInitializer] Connection attempt {retryCount} failed: {ex.Message}");
                    
                    if (retryCount < maxRetries)
                    {
                        float delay = Mathf.Pow(2, retryCount - 1); // Exponential backoff
                        Debug.Log($"[QuestAppInitializer] Retrying in {delay} seconds...");
                        await Task.Delay((int)(delay * 1000));
                    }
                }
            }
            
            if (retryCount >= maxRetries)
            {
                Debug.LogError("[QuestAppInitializer] Max reconnection attempts reached. Connection failed.");
            }
        }

        // Removed AuthenticateAsync method - now using AuthenticationManager

        private async Task WaitForMobilePeer()
        {
            float timeout = connectionConfig.connectionTimeout * 1000;
            float waited = 0;
            
            Debug.Log("[QuestAppInitializer] Waiting for mobile peer...");
            
            while (!hasMobilePeer && waited < timeout)
            {
                await Task.Delay(100);
                waited += 100;
            }
            
            if (!hasMobilePeer)
            {
                Debug.LogWarning($"[QuestAppInitializer] Timeout waiting for mobile peer after {timeout/1000}s");
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
                    roomId = connectionConfig.GetRoomId()
                };
                
                string jsonMessage = JsonUtility.ToJson(registerMessage);
                Debug.Log($"[QuestAppInitializer] Registering client: {jsonMessage}");
                
                if (webSocketAdapter != null)
                {
                    await webSocketAdapter.SendText(jsonMessage);
                    Debug.Log("[QuestAppInitializer] Client registered successfully");
                }
                else
                {
                    throw new InvalidOperationException("WebSocketAdapter is null");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestAppInitializer] Failed to register client: {ex.Message}");
                throw;
            }
        }
        
        private void HandleSignalingMessage(string type, string jsonData)
        {
            try
            {
                if (type == "peer-joined")
                {
                    var peerInfo = JsonUtility.FromJson<PeerJoinedMessage>(jsonData);
                    if (peerInfo.clientType == "mobile")
                    {
                        Debug.Log($"[QuestAppInitializer] Mobile peer joined: {peerInfo.peerId}");
                        hasMobilePeer = true;
                    }
                }
                else if (type == "error")
                {
                    var error = JsonUtility.FromJson<ErrorMessage>(jsonData);
                    Debug.LogError($"[QuestAppInitializer] Server error: {error.error} (context: {error.context})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestAppInitializer] Failed to handle message: {ex.Message}");
            }
        }
        
        void Update()
        {
            webSocketAdapter?.DispatchMessageQueue();
            signalingClient?.DispatchMessages();
        }
        
        void OnDestroy()
        {
            if (signalingClient != null)
            {
                signalingClient.OnSignalingMessageReceived -= HandleSignalingMessage;
            }
            
            // Note: In newer Unity WebRTC versions, explicit Dispose() is not needed
            // Resources are cleaned up automatically
        }
    }
}
