using System;
using System.Collections;
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
        private bool mobilePeerJoined = false;

        [Header("Dependencies")]
        [SerializeField] private WebRtcManager webRtcManager;
        [SerializeField] private MultiPeerWebRtcManager multiPeerWebRtcManager;
        [SerializeField] private ConnectionConfig connectionConfig;
        [SerializeField] private WebRtcConfiguration webRtcConfiguration;
        
        // Flag to determine which manager to use
        private bool useMultiPeer => multiPeerWebRtcManager != null;
        
        [Header("Settings")]
        [SerializeField] private bool autoConnectOnStart = true;

        void Start()
        {
            try
            {
                // Critical: WebRTC.Update() coroutine must be started first
                StartCoroutine(WebRTC.Update());
                
                // Platform-specific initialization
                StartCoroutine(InitializeWithPlatformDelay());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestAppInitializer] Failed to initialize: {ex.Message}");
            }
        }
        
        private IEnumerator InitializeWithPlatformDelay()
        {
            // macOS Metal needs extra initialization time
            if (Application.platform == RuntimePlatform.OSXEditor || 
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                yield return null;
            }
            
            InitializeApp();
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

            // WebSocket 어댑터와 시그널링 클라이언트 생성
            webSocketAdapter = new SystemWebSocketAdapter();
            signalingClient = new SignalingClient();
            
            if (useMultiPeer)
            {
                // MultiPeerWebRtcManager 설정
                multiPeerWebRtcManager.SetupSignaling(signalingClient);
            }
            else
            {
                // WebRtcManager 설정
                webRtcManager.SetRole(true);
                webRtcManager.SetupSignaling(signalingClient);
                
                if (webRtcConfiguration != null)
                {
                    webRtcManager.SetConfiguration(webRtcConfiguration);
                }

                // Set role as Offerer
                webRtcManager.SetRole(true);
                // Disable auto-start to control timing manually
                webRtcManager.autoStartPeerConnection = false;
            }

            if (autoConnectOnStart)
            {
                var serverUrl = connectionConfig.signalingServerUrl;
                Debug.Log($"[QuestAppInitializer] Connecting to {serverUrl}...");
                StartCoroutine(DelayedSignalingConnection());
            }
        }

        private bool ValidateDependencies()
        {
            if (webRtcManager == null && multiPeerWebRtcManager == null)
            {
                Debug.LogError("[QuestAppInitializer] Neither WebRtcManager nor MultiPeerWebRtcManager is assigned!");
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
                    if (useMultiPeer)
                    {
                        Debug.Log("[QuestAppInitializer] Using MultiPeerWebRtcManager - waiting for peers to join...");
                        await Task.Delay(500); // Wait for signaling to stabilize
                        multiPeerWebRtcManager.StartAsHost(connectionConfig.GetRoomId());
                    }
                    else if (webRtcManager != null)
                    {
                        Debug.Log("[QuestAppInitializer] Creating PeerConnection immediately after registration...");
                        await Task.Delay(500); // Wait for signaling to stabilize
                        webRtcManager.CreatePeerConnection();
                        webRtcManager.CreateDataChannel();
                        Debug.Log("[QuestAppInitializer] PeerConnection and DataChannel created. Waiting for mobile peer...");
                    }
                    
                    await WaitForMobilePeer();
                    
                    if (hasMobilePeer)
                    {
                        if (useMultiPeer)
                        {
                            Debug.Log("[QuestAppInitializer] Mobile peer joined. MultiPeerWebRtcManager will handle negotiations automatically.");
                        }
                        else if (webRtcManager != null)
                        {
                            Debug.Log("[QuestAppInitializer] Mobile peer joined. Waiting for video track to be added before creating offer...");
                            // Don't create offer immediately - wait for video track to be added
                            // The negotiation will be triggered automatically when tracks are added
                        }
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
                    var peerInfo = JsonUtility.FromJson<UnityVerseBridge.Core.Signaling.Messages.PeerJoinedMessage>(jsonData);
                    if (peerInfo.clientType == "mobile")
                    {
                        Debug.Log($"[QuestAppInitializer] Mobile peer joined: {peerInfo.peerId}");
                        hasMobilePeer = true;
                        mobilePeerJoined = true;  // Set this flag!
                        
                        // Trigger negotiation if we have tracks ready
                        if (useMultiPeer)
                        {
                            // MultiPeerWebRtcManager handles negotiations automatically
                            Debug.Log("[QuestAppInitializer] MultiPeer mode - automatic negotiation");
                        }
                        else if (webRtcManager != null && webRtcManager.GetPeerConnectionState() == RTCPeerConnectionState.New)
                        {
                            // Give the VrStreamSender a chance to add tracks
                            StartCoroutine(TriggerNegotiationAfterDelay());
                        }
                    }
                }
                else if (type == "client-ready")
                {
                    Debug.Log($"[QuestAppInitializer] Client ready message received: {jsonData}");
                    mobilePeerJoined = true;  // Set this flag too!
                    hasMobilePeer = true;
                    
                    // Mobile client is ready, trigger negotiation now
                    if (useMultiPeer)
                    {
                        Debug.Log("[QuestAppInitializer] Client ready - MultiPeer mode handles automatically");
                    }
                    else if (webRtcManager != null)
                    {
                        var state = webRtcManager.GetPeerConnectionState();
                        Debug.Log($"[QuestAppInitializer] PeerConnection state: {state}");
                        
                        // If negotiation is needed, start it
                        if (state != RTCPeerConnectionState.Closed && state != RTCPeerConnectionState.Failed)
                        {
                            StartCoroutine(TriggerNegotiationAfterDelay());
                        }
                    }
                }
                else if (type == "error")
                {
                    var error = JsonUtility.FromJson<ErrorMessage>(jsonData);
                    Debug.LogError($"[QuestAppInitializer] Server error: {error.error} (context: {error.context})");
                    
                    // "Room already has a host" 에러 처리
                    if (error.error != null && error.error.Contains("Room already has a host"))
                    {
                        Debug.LogWarning("[QuestAppInitializer] Room already has a host. Disconnecting and retrying with new room ID...");
                        
                        // 현재 연결 종료
                        DisconnectAndCleanup();
                        
                        // 세션 room ID 재설정
                        connectionConfig.ResetSessionRoomId();
                        
                        // 재연결 시도
                        StartCoroutine(RetryConnectionWithNewRoom());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QuestAppInitializer] Failed to handle message: {ex.Message}");
            }
        }
        
        private IEnumerator TriggerNegotiationAfterDelay()
        {
            // Wait a bit for tracks to be added
            yield return new WaitForSeconds(1.0f);
            
            if (!useMultiPeer && webRtcManager != null)
            {
                var state = webRtcManager.GetPeerConnectionState();
                Debug.Log($"[QuestAppInitializer] Checking if negotiation is needed. PC State: {state}, isNegotiating: {webRtcManager.IsNegotiating}");
                
                // Start negotiation if we're not already negotiating
                if (!webRtcManager.IsNegotiating && state != RTCPeerConnectionState.Closed && state != RTCPeerConnectionState.Failed)
                {
                    Debug.Log("[QuestAppInitializer] Starting negotiation after mobile peer is ready...");
                    webRtcManager.StartNegotiation();
                }
            }
        }
        
        void Update()
        {
            webSocketAdapter?.DispatchMessageQueue();
            signalingClient?.DispatchMessages();
        }
        
        private IEnumerator DelayedSignalingConnection()
        {
            // Wait for WebRTC initialization to complete
            yield return new WaitForSeconds(0.5f);
            StartSignalingConnection();
        }

        void OnDestroy()
        {
            DisconnectAndCleanup();
        }
        
        private void DisconnectAndCleanup()
        {
            Debug.Log("[QuestAppInitializer] Disconnecting and cleaning up...");
            
            if (signalingClient != null)
            {
                signalingClient.OnSignalingMessageReceived -= HandleSignalingMessage;
            }
            
            if (useMultiPeer && multiPeerWebRtcManager != null)
            {
                multiPeerWebRtcManager.DisconnectAll();
            }
            else if (webRtcManager != null)
            {
                webRtcManager.Disconnect();
            }
            
            if (signalingClient != null)
            {
                // SignalingClient doesn't have Dispose method, just clean up references
                signalingClient = null;
            }
            
            if (webSocketAdapter != null)
            {
                // SystemWebSocketAdapter cleanup - no explicit close needed
                webSocketAdapter = null;
            }
            
            hasMobilePeer = false;
            mobilePeerJoined = false;
        }
        
        private IEnumerator RetryConnectionWithNewRoom()
        {
            Debug.Log("[QuestAppInitializer] Waiting before retry...");
            yield return new WaitForSeconds(2f);
            
            Debug.Log($"[QuestAppInitializer] Retrying with new room ID: {connectionConfig.GetRoomId()}");
            
            // 재초기화
            InitializeApp();
        }
    }
}
