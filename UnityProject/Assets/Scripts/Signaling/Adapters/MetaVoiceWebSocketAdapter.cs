using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityVerseBridge.Core.Signaling; // ISignalingClient 인터페이스 사용
using UnityVerseBridge.Core.Signaling.Data; // SignalingMessageBase 사용
using System.Reflection;
using System.Linq;

namespace UnityVerseBridge.QuestApp.Signaling // 앱 고유 네임스페이스
{
    /// <summary>
    /// Meta Voice/XR SDK의 WebSocket 연결을 위한 어댑터 클래스입니다.
    /// NativeWebSocket 참조 없이 구현하여 컴파일 에러를 방지합니다.
    /// </summary>
    public class MetaVoiceWebSocketAdapter : ISignalingClient
    {
        // 내부 WebSocket 상태 관리를 위한 열거형
        private enum InternalWebSocketState { None, Connecting, Open, Closing, Closed }

        // 내부 상태 관리
        private InternalWebSocketState _internalState = InternalWebSocketState.None;
        private object _webSocket; // 실제 NativeWebSocket 객체를 dynamic으로 처리

        // 리플렉션을 위한 Type 캐싱
        private Type _webSocketType = null;
        private MethodInfo _connectMethod = null;
        private MethodInfo _closeMethod = null;
        private MethodInfo _sendTextMethod = null;
        private MethodInfo _dispatchMessageQueueMethod = null;
        private PropertyInfo _stateProperty = null;
        
        // 시뮬레이션 모드 플래그
        private bool _isSimulationMode = false;
        private string _currentUrl = null;
        
        // 시뮬레이션 플래그
        private bool _simulationConnectionEstablished = false;
        private bool _simulationRemoteDescriptionSet = false;
        
        // 시뮬레이션된 세션 데이터
        private string _localSdpOffer = null;
        private readonly string _simulatedIceUfrag = "simUfrag12345";
        private readonly string _simulatedIcePwd = "simPwd0987654321abcdefghijklmnopq";
        private readonly int _instanceId; // 인스턴스 구분을 위한 ID
        private static int _instanceCounter = 0; // 정적 카운터

        // 인스턴스 ID를 외부에서 읽을 수 있도록 public getter 추가
        public int InstanceId => _instanceId;

        // 현재 연결 URL을 외부에서 읽을 수 있도록 public getter 추가
        public string CurrentUrl => _currentUrl;

        // --- ISignalingClient 이벤트 구현 ---
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string, string> OnSignalingMessageReceived;

        // --- ISignalingClient 속성 구현 ---
        public bool IsConnected 
        {
            get 
            {
                bool connected = _internalState == InternalWebSocketState.Open || (_isSimulationMode && _simulationConnectionEstablished);
                // Debug.Log($"[MetaAdapter-{_instanceId}] IsConnected getter: _internalState={_internalState}, _isSimulationMode={_isSimulationMode}, _simulationConnectionEstablished={_simulationConnectionEstablished}, Result={connected}");
                return connected;
            }
        }

        // --- 생성자에서 시뮬레이션 모드 결정 ---
        public MetaVoiceWebSocketAdapter()
        {
            _instanceId = System.Threading.Interlocked.Increment(ref _instanceCounter);
            Debug.Log($"[MetaAdapter-{_instanceId}] Constructor called. _isSimulationMode initialized to false (will be determined in InitializeAtRuntime).");
        }

        // --- ISignalingClient 메서드 구현 ---
        public async Task Connect(string url)
        {
            Debug.Log($"[MetaAdapter-{_instanceId}] Connect called. URL from param: {url}, _currentUrl: {_currentUrl}. Current IsConnected: {IsConnected}, _internalState: {_internalState}, _simulationConnectionEstablished: {_simulationConnectionEstablished}");
            if (IsConnected)
            {
                Debug.Log($"[MetaAdapter-{_instanceId}] Already connected or simulation established. Invoking OnConnected event again.");
                OnConnected?.Invoke(); 
                return;
            }
            
            if (_internalState == InternalWebSocketState.Connecting && !_isSimulationMode) 
            {
                Debug.Log($"[MetaAdapter-{_instanceId}] Native connection already in progress.");
                return;
            }
            
            if (string.IsNullOrEmpty(_currentUrl) && !string.IsNullOrEmpty(url)) {
                Debug.LogWarning($"[MetaAdapter-{_instanceId}] _currentUrl was null/empty, using URL from Connect param: {url}");
                _currentUrl = url;
            } else if (!string.IsNullOrEmpty(url) && _currentUrl != url) {
                Debug.LogWarning($"[MetaAdapter-{_instanceId}] URL from Connect param ({url}) is different from _currentUrl ({_currentUrl}). Using _currentUrl.");
            }

            _internalState = InternalWebSocketState.Connecting;
            _simulationConnectionEstablished = false; 
            _simulationRemoteDescriptionSet = false;
            
            try
            {
                if (_isSimulationMode)
                {
                    Debug.Log($"[MetaAdapter-{_instanceId}] Running in simulation mode for Connect (determined by InitializeAtRuntime). Using URL: {_currentUrl}");
                    await SimulateConnect();
                    return;
                }
                
                if (_webSocketType == null)
                {
                    Debug.LogError($"[MetaAdapter-{_instanceId}] NativeWebSocket type is null even when not in simulation mode. This should not happen. Forcing simulation.");
                    _isSimulationMode = true;
                    await SimulateConnect();
                    return;
                }

                Debug.Log($"[MetaAdapter-{_instanceId}] Attempting native WebSocket connection using URL: {_currentUrl}");
                
                // URL을 파라미터로 받는 생성자를 먼저 시도 (NativeWebSocket의 기본 패턴)
                try
                {
                    _webSocket = Activator.CreateInstance(_webSocketType, new object[] { _currentUrl });
                    Debug.Log($"[MetaAdapter-{_instanceId}] WebSocket instance created with URL parameter.");
                }
                catch (Exception createEx)
                {
                    Debug.LogError($"[MetaAdapter-{_instanceId}] Failed to create WebSocket with URL parameter: {createEx.Message}");
                    // 파라미터 없는 생성자 시도 (대체 방법)
                    try 
                    {
                        _webSocket = Activator.CreateInstance(_webSocketType);
                        Debug.Log($"[MetaAdapter-{_instanceId}] WebSocket instance created with default constructor.");
                        
                        // URL 속성 설정 시도
                        var urlProperty = _webSocketType.GetProperty("Url");
                        if (urlProperty != null && urlProperty.CanWrite)
                        {
                            urlProperty.SetValue(_webSocket, _currentUrl);
                            Debug.Log($"[MetaAdapter-{_instanceId}] WebSocket URL property set to: {_currentUrl}");
                        }
                    }
                    catch (Exception createEx2) 
                    {
                        Debug.LogError($"[MetaAdapter-{_instanceId}] Failed to create WebSocket with any constructor: {createEx2.Message}");
                        throw;
                    }
                }
                
                // URL 속성 설정은 위에서 이미 처리됨
                
                SetupEventHandlers();
                
                if (_connectMethod != null)
                {
                    Debug.Log($"[MetaAdapter-{_instanceId}] Attempting native WebSocket Connect method call...");
                    
                    // Connect 메서드가 URL 파라미터를 받는지 확인
                    var connectParams = _connectMethod.GetParameters();
                    if (connectParams.Length == 0)
                    {
                        // 파라미터 없는 Connect() 메서드
                        var connectTask = (Task)_connectMethod.Invoke(_webSocket, null);
                        await connectTask;
                    }
                    else if (connectParams.Length == 1 && connectParams[0].ParameterType == typeof(string))
                    {
                        // URL 파라미터를 받는 Connect(string url) 메서드
                        var connectTask = (Task)_connectMethod.Invoke(_webSocket, new object[] { _currentUrl });
                        await connectTask;
                    }
                    else
                    {
                        Debug.LogError($"[MetaAdapter-{_instanceId}] Connect method has unexpected parameters: {connectParams.Length}");
                        throw new Exception("Connect method signature mismatch");
                    }
                }
                else
                {
                    Debug.LogError($"[MetaAdapter-{_instanceId}] Native Connect method not found. Falling back to simulation.");
                    _isSimulationMode = true; 
                    await SimulateConnect();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MetaAdapter-{_instanceId}] Connect Exception: {e.Message}. Falling back to simulation if not already.");
                _internalState = InternalWebSocketState.Closed; 
                _simulationConnectionEstablished = false;
                if (!_isSimulationMode) 
                {
                    _isSimulationMode = true;
                    await SimulateConnect(); 
                }
                else
                {
                    OnDisconnected?.Invoke(); 
                }
            }
        }
        
        // WebSocket 연결 시뮬레이션
        private async Task SimulateConnect()
        {
            _isSimulationMode = true; 
            _internalState = InternalWebSocketState.Connecting; 
            Debug.Log($"[MetaAdapter-{_instanceId}] Simulating WebSocket connection...");
            await Task.Delay(TimeSpan.FromMilliseconds(300));
            _internalState = InternalWebSocketState.Open; 
            _simulationConnectionEstablished = true;
            Debug.Log($"[MetaAdapter-{_instanceId}] Simulated WebSocket connection established. IsConnected: {IsConnected}, _internalState: {_internalState}, _simulationConnectionEstablished: {_simulationConnectionEstablished}");
            OnConnected?.Invoke();
        }

        public async Task Disconnect()
        {
             Debug.Log($"[MetaAdapter-{_instanceId}] Disconnect called. Current IsConnected: {IsConnected}, _internalState: {_internalState}, _simulation: {_isSimulationMode}");
            if (_internalState == InternalWebSocketState.Closed && !_simulationConnectionEstablished)
            {
                Debug.Log($"[MetaAdapter-{_instanceId}] Already fully disconnected.");
                return;
            }
            
            var prevState = _internalState;
            bool prevSimConnected = _simulationConnectionEstablished;
            _internalState = InternalWebSocketState.Closing;
            _simulationConnectionEstablished = false; // 연결 끊기 시작 시 시뮬레이션 플래그도 내림
            
            try
            {
                if (_isSimulationMode || _webSocket == null) // 시뮬레이션 모드이거나, 네이티브 객체가 없으면 시뮬레이션 종료
                {
                    Debug.Log($"[MetaAdapter-{_instanceId}] Simulating Disconnect logic.");
                    await Task.Delay(TimeSpan.FromMilliseconds(100));
                }
                else if (Application.isPlaying && _closeMethod != null) // 네이티브 연결 종료
                {
                    Debug.Log($"[MetaAdapter-{_instanceId}] Attempting native WebSocket Close...");
                    var closeTask = (Task)_closeMethod.Invoke(_webSocket, null);
                    await closeTask;
                }
                else
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(100)); // 기타 경우
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MetaAdapter-{_instanceId}] Disconnect Exception: {e.Message}");
            }
            finally
            {
                _internalState = InternalWebSocketState.Closed;
                // _simulationConnectionEstablished는 위에서 이미 false로 설정됨
                _simulationRemoteDescriptionSet = false;
                if (_webSocket != null) {
                    Debug.Log($"[MetaAdapter-{_instanceId}] Nullifying _webSocket object.");
                    _webSocket = null; // 실제 웹소켓 객체 참조 해제
                }
                Debug.Log($"[MetaAdapter-{_instanceId}] Disconnect finalized. _internalState: {_internalState}, _simulationConnectionEstablished: {_simulationConnectionEstablished}");
                // 실제 상태 변화가 있었을 때만 OnDisconnected 호출 (중복 방지)
                if (prevState != InternalWebSocketState.Closed || prevSimConnected)
                {
                    OnDisconnected?.Invoke();
                }
            }
        }

        public async Task SendMessage<T>(T message) where T : SignalingMessageBase
        {
            Debug.Log($"[MetaAdapter-{_instanceId}] SendMessage attempting. IsConnected: {IsConnected}, _internalState: {_internalState}, _simulationMode: {_isSimulationMode}, _simConnectionEst: {_simulationConnectionEstablished}, MsgType: {message.type}");
            if (!IsConnected)
            {
                Debug.LogWarning($"[MetaAdapter-{_instanceId}] Cannot send message, not connected. Message Type: {message.type}");
                return;
            }
            
            string jsonMessage = JsonUtility.ToJson(message);
            Debug.Log($"[MetaAdapter-{_instanceId}] Sending JSON (Type: {message.type}): {jsonMessage.Substring(0, Math.Min(jsonMessage.Length, 200))}...");
            
            if (message.type == "offer")
            {
                var offerMsg = message as SessionDescriptionMessage;
                if (offerMsg != null) _localSdpOffer = offerMsg.sdp;
            }
            
            // 시뮬레이션 모드에서는 항상 시뮬레이션 응답 로직 실행
            if (_isSimulationMode)
            {
                Debug.Log($"[MetaAdapter-{_instanceId}] In simulation mode, calling SimulateResponse for {message.type}.");
                await Task.Delay(TimeSpan.FromMilliseconds(10));
                await SimulateResponse(message);
                return;
            }
            
            // 실제 WebSocket 연결 모드
            if (Application.isPlaying && _webSocket != null && _sendTextMethod != null)
            {
                try
                {
                    Debug.Log($"[MetaAdapter-{_instanceId}] Attempting native SendText for {message.type}.");
                    var sendTask = (Task)_sendTextMethod.Invoke(_webSocket, new object[] { jsonMessage });
                    await sendTask;
                    Debug.Log($"[MetaAdapter-{_instanceId}] Native SendText for {message.type} successful.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MetaAdapter-{_instanceId}] Native SendText Exception for {message.type}: {e.Message}. Disconnecting.");
                    _internalState = InternalWebSocketState.Closed;
                    _simulationConnectionEstablished = false; // 네이티브 실패 시 시뮬레이션 플래그도 확실히 내림
                    OnDisconnected?.Invoke();
                }
            }
            else
            {
                // 이 경우는 발생하면 안되지만 (IsConnected가 true인데 _webSocket이 null 등), 방어적으로 처리
                Debug.LogError($"[MetaAdapter-{_instanceId}] Critical state error: IsConnected was true, but WebSocket unavailable for SendMessage. Message Type: {message.type}. Falling to simulation.");
                _isSimulationMode = true; // 강제 시뮬레이션 모드 전환
                await Task.Delay(TimeSpan.FromMilliseconds(10));
                await SimulateResponse(message); 
            }
        }
        
        // 시뮬레이션 모드에서 메시지에 대한 가상 응답 생성
        private async Task SimulateResponse<T>(T message) where T : SignalingMessageBase
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            Debug.Log($"[MetaAdapter-{_instanceId}] Simulating response for message type: {message.type}");
            
            if (message.type == "offer")
            {
                var answerSdp = $"v=0\r\n" +
                               $"o=- 12345 12345 IN IP4 127.0.0.1\r\n" +
                               $"s=-\r\n" +
                               $"t=0 0\r\n" +
                               $"a=group:BUNDLE 0\r\n" +
                               $"a=msid-semantic: WMS\r\n" +
                               $"m=application 9 UDP/DTLS/SCTP webrtc-datachannel\r\n" +
                               $"c=IN IP4 0.0.0.0\r\n" +
                               $"a=ice-ufrag:{_simulatedIceUfrag}\r\n" +
                               $"a=ice-pwd:{_simulatedIcePwd}\r\n" +
                               $"a=setup:active\r\n" +
                               $"a=mid:0\r\n" +
                               $"a=sctp-port:5000\r\n" +
                               $"a=rtcp-mux\r\n" + 
                               $"a=ice-options:trickle\r\n" +
                               $"a=fingerprint:sha-256 BB:AA:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99:AA:BB:CC:DD:EE:FF:00:11:22:33:44:55:66:77:88:99\r\n";
                
                var answerMsg = new SessionDescriptionMessage { type = "answer", sdp = answerSdp };
                string json = JsonUtility.ToJson(answerMsg);
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                Debug.Log($"[MetaAdapter-{_instanceId}] Simulating 'answer' response: {json.Substring(0, Math.Min(json.Length, 100))}...");
                OnSignalingMessageReceived?.Invoke("answer", json);
                _simulationRemoteDescriptionSet = true;
                Debug.Log($"[MetaAdapter-{_instanceId}] _simulationRemoteDescriptionSet = true after sending simulated answer.");
                await Task.Delay(TimeSpan.FromMilliseconds(300)); // ICE 후보 전송 전 충분한 시간 확보
                await SimulateIceCandidates();
            }
            else if (message.type == "ice-candidate")
            {
                Debug.Log($"[MetaAdapter-{_instanceId}] Received (and simulated sending) ICE candidate. No further response needed from adapter for this message.");
            }
        }
        
        // SDP에서 세션 ID 추출 (o= 라인)
        private string ExtractSessionId(string sdp)
        {
            if (string.IsNullOrEmpty(sdp)) return "0";
            
            // o= 라인에서 세션 ID 추출
            var lines = sdp.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (line.StartsWith("o="))
                {
                    var parts = line.Split(' ');
                    if (parts.Length >= 2)
                    {
                        return parts[1];
                    }
                }
            }
            
            return "0";
        }
        
        // ICE 후보 여러 개 시뮬레이션
        private async Task SimulateIceCandidates()
        {
            if (!_simulationRemoteDescriptionSet) // 시뮬레이션된 Remote Description이 설정된 후에만 ICE 후보 전송
            {
                Debug.LogWarning($"[MetaAdapter-{_instanceId}] Cannot send simulated ICE candidates: _simulationRemoteDescriptionSet is false.");
                return;
            }
            Debug.Log($"[MetaAdapter-{_instanceId}] Starting to simulate ICE candidates...");
            await Task.Delay(TimeSpan.FromMilliseconds(200)); // ICE 후보 생성 전 약간의 지연
            
            string[] candidates = {
                $"candidate:1 1 UDP 2130706431 192.168.1.123 12345 typ host generation 0 ufrag {_simulatedIceUfrag}",
                $"candidate:2 1 UDP 1694498815 203.0.113.5 23456 typ srflx raddr 192.168.1.123 rport 12345 generation 0 ufrag {_simulatedIceUfrag}",
            };

            foreach (var candStr in candidates)
            {
                var iceMsg = new IceCandidateMessage { type = "ice-candidate", candidate = candStr, sdpMid = "0", sdpMLineIndex = 0 };
                string json = JsonUtility.ToJson(iceMsg);
                Debug.Log($"[MetaAdapter-{_instanceId}] Simulating ICE candidate: {candStr.Substring(0, Math.Min(candStr.Length, 70))}...");
                OnSignalingMessageReceived?.Invoke("ice-candidate", json);
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }
            Debug.Log($"[MetaAdapter-{_instanceId}] All simulated ICE candidates sent.");
        }

        // NativeWebSocket 메시지 큐 처리는 런타임에 동적으로 처리합니다
        public void DispatchMessages()
        {
            if (!_isSimulationMode && Application.isPlaying && _webSocket != null && _dispatchMessageQueueMethod != null)
            {
                try { _dispatchMessageQueueMethod.Invoke(_webSocket, null); }
                catch (Exception e) { Debug.LogError($"[MetaAdapter-{_instanceId}] Error in DispatchMessageQueue: {e.Message}"); }
            }
        }
        
        public Task InitializeAndConnect(IWebSocketClient adapter, string url) {
            Debug.LogWarning($"[MetaAdapter-{_instanceId}] InitializeAndConnect is deprecated. Calling Connect directly.");
            return Connect(url);
        }

        // --- 실행 시 NativeWebSocket 연결을 위한 메서드 ---
        
        /// <summary>
        /// 런타임에 Meta SDK의 NativeWebSocket을 초기화합니다.
        /// 이 메서드는 WebRtcConnectionTester에서 앱 실행 시 호출합니다.
        /// </summary>
        public void InitializeAtRuntime(string url)
        {
            Debug.Log($"[MetaAdapter-{_instanceId}] InitializeAtRuntime for URL: {url}");
            _currentUrl = url;

            if (Application.isPlaying) // 수정된 조건: 에디터에서 플레이 중일 때도 네이티브 시도
            {
                Debug.Log($"[MetaAdapter-{_instanceId}] Application.isPlaying is true. Attempting to load native types. (IsEditor: {Application.isEditor})");
                _isSimulationMode = false;
                try
                {
                    LoadNativeWebSocketTypes();
                    if (_webSocketType == null) {
                        Debug.LogWarning($"[MetaAdapter-{_instanceId}] NativeWebSocket type not found after LoadNativeWebSocketTypes. Forcing simulation mode.");
                        _isSimulationMode = true;
                    }
                    else
                    {
                        Debug.Log($"[MetaAdapter-{_instanceId}] NativeWebSocket types loaded successfully.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MetaAdapter-{_instanceId}] Failed to initialize NativeWebSocket during InitializeAtRuntime: {e.Message}. Forcing simulation mode.");
                    _isSimulationMode = true;
                }
            }
            else
            {
                Debug.Log($"[MetaAdapter-{_instanceId}] Application.isPlaying is false. Forcing simulation mode. (IsEditor: {Application.isEditor})");
                _isSimulationMode = true; 
            }
            Debug.Log($"[MetaAdapter-{_instanceId}] InitializeAtRuntime finished. _isSimulationMode is now: {_isSimulationMode}");
        }
        
        /// <summary>
        /// 리플렉션을 사용하여 NativeWebSocket 타입 및 메서드를 로드합니다.
        /// </summary>
        private void LoadNativeWebSocketTypes()
        {
            Debug.Log($"[MetaAdapter-{_instanceId}] Attempting to load NativeWebSocket types...");
            
            // 전체 로드된 어셈블리 목록 확인
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Debug.Log($"[MetaAdapter-{_instanceId}] 총 {assemblies.Length}개의 어셈블리가 로드되어 있음");
            
            // 특정 어셈블리 이름으로 검색 시도
            var targetAssemblyName = "Meta.Net.endel.nativewebsocket";
            var targetAssembly = assemblies.FirstOrDefault(a => a.GetName().Name.Equals(targetAssemblyName, StringComparison.OrdinalIgnoreCase));
            
            if (targetAssembly != null)
            {
                Debug.Log($"[MetaAdapter-{_instanceId}] 대상 어셈블리 '{targetAssemblyName}' 발견됨: {targetAssembly.FullName}");
                
                try
                {
                    _webSocketType = targetAssembly.GetType("Meta.Net.NativeWebSocket.WebSocket");
                    if (_webSocketType != null)
                    {
                        Debug.Log($"[MetaAdapter-{_instanceId}] Meta.Net.NativeWebSocket.WebSocket 타입이 {targetAssemblyName} 어셈블리에서 발견됨");
                        LoadWebSocketMethods();
                        return; // 성공적으로 타입을 찾음
                    }
                    else
                    {
                        Debug.LogWarning($"[MetaAdapter-{_instanceId}] NativeWebSocket.WebSocket 타입이 {targetAssemblyName} 어셈블리에 존재하지 않음");
                        
                        // 해당 어셈블리의 모든 타입 출력 (디버깅용)
                        var types = targetAssembly.GetTypes();
                        Debug.Log($"[MetaAdapter-{_instanceId}] {targetAssemblyName} 어셈블리 내 타입 목록 (총 {types.Length}개):");
                        foreach (var type in types.Take(10)) // 처음 10개만 출력
                        {
                            Debug.Log($"[MetaAdapter-{_instanceId}] - {type.FullName}");
                        }
                        if (types.Length > 10)
                        {
                            Debug.Log($"[MetaAdapter-{_instanceId}] ... 및 {types.Length - 10}개 타입이 더 있음");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[MetaAdapter-{_instanceId}] {targetAssemblyName} 어셈블리에서 타입 검색 중 오류 발생: {ex.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[MetaAdapter-{_instanceId}] 대상 어셈블리 '{targetAssemblyName}'을 찾지 못함");
                
                // 로드된 모든 어셈블리 목록 출력 (최대 20개)
                Debug.Log($"[MetaAdapter-{_instanceId}] 로드된 어셈블리 목록 (최대 20개):");
                foreach (var assembly in assemblies.Take(20))
                {
                    Debug.Log($"[MetaAdapter-{_instanceId}] - {assembly.GetName().Name}");
                }
                if (assemblies.Length > 20)
                {
                    Debug.Log($"[MetaAdapter-{_instanceId}] ... 및 {assemblies.Length - 20}개 어셈블리가 더 있음");
                }
            }
            
            // 모든 어셈블리에서 NativeWebSocket.WebSocket 타입 검색 시도
            foreach (var assembly in assemblies)
            {
                try
                {
                    _webSocketType = assembly.GetType("Meta.Net.NativeWebSocket.WebSocket");
                    if (_webSocketType != null)
                    {
                        Debug.Log($"[MetaAdapter-{_instanceId}] Meta.Net.NativeWebSocket.WebSocket 타입이 {assembly.GetName().Name} 어셈블리에서 발견됨");
                        LoadWebSocketMethods();
                        return; // 성공적으로 타입을 찾음
                    }
                }
                catch (Exception)
                {
                    // 특정 어셈블리에서 예외가 발생해도 계속 진행
                    continue;
                }
            }
            
            Debug.LogWarning($"[MetaAdapter-{_instanceId}] Meta.Net.NativeWebSocket.WebSocket type not found in any loaded assembly.");
        }
        
        /// <summary>
        /// WebSocket 타입의 메서드들을 로드합니다.
        /// </summary>
        private void LoadWebSocketMethods()
        {
            if (_webSocketType == null) return;
            
            try
            {
                // 생성자 정보 확인
                var constructors = _webSocketType.GetConstructors();
                Debug.Log($"[MetaAdapter-{_instanceId}] WebSocket has {constructors.Length} constructors:");
                foreach (var ctor in constructors)
                {
                    var parameters = ctor.GetParameters();
                    var paramInfo = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Debug.Log($"[MetaAdapter-{_instanceId}] - Constructor: ({paramInfo})");
                }
                
                _connectMethod = _webSocketType.GetMethod("Connect");
                _closeMethod = _webSocketType.GetMethod("Close");
                _sendTextMethod = _webSocketType.GetMethod("SendText");
                _dispatchMessageQueueMethod = _webSocketType.GetMethod("DispatchMessageQueue");
                _stateProperty = _webSocketType.GetProperty("State");
                
                Debug.Log($"[MetaAdapter-{_instanceId}] WebSocket methods loaded - Connect: {_connectMethod != null}, Close: {_closeMethod != null}, SendText: {_sendTextMethod != null}, DispatchMessageQueue: {_dispatchMessageQueueMethod != null}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MetaAdapter-{_instanceId}] Failed to load WebSocket methods: {e.Message}");
                _webSocketType = null; // 메서드 로드 실패 시 타입도 null로 설정
            }
        }
        
        /// <summary>
        /// 리플렉션을 사용하여 이벤트 핸들러를 연결합니다.
        /// </summary>
        private void SetupEventHandlers()
        {
            if (_webSocket == null || _webSocketType == null || _isSimulationMode) 
            {
                Debug.LogWarning($"[MetaAdapter-{_instanceId}] Skipping SetupEventHandlers: _webSocket is null, or type is null, or in simulation mode.");
                return;
            }
            Debug.Log($"[MetaAdapter-{_instanceId}] Setting up native WebSocket event handlers...");
            try
            {
                var onOpenEvent = _webSocketType.GetEvent("OnOpen");
                if (onOpenEvent != null) onOpenEvent.AddEventHandler(_webSocket, Delegate.CreateDelegate(onOpenEvent.EventHandlerType, this, "HandleWebSocketOpen"));
                
                var onMessageEvent = _webSocketType.GetEvent("OnMessage");
                if (onMessageEvent != null) onMessageEvent.AddEventHandler(_webSocket, Delegate.CreateDelegate(onMessageEvent.EventHandlerType, this, "HandleWebSocketMessage"));
                
                var onErrorEvent = _webSocketType.GetEvent("OnError");
                if (onErrorEvent != null) onErrorEvent.AddEventHandler(_webSocket, Delegate.CreateDelegate(onErrorEvent.EventHandlerType, this, "HandleWebSocketError"));
                
                var onCloseEvent = _webSocketType.GetEvent("OnClose");
                if (onCloseEvent != null) onCloseEvent.AddEventHandler(_webSocket, Delegate.CreateDelegate(onCloseEvent.EventHandlerType, this, "HandleWebSocketClose"));
                
                Debug.Log($"[MetaAdapter-{_instanceId}] Native WebSocket event handlers connected.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MetaAdapter-{_instanceId}] Failed to set up native event handlers: {e.Message}. Forcing simulation mode.");
                _isSimulationMode = true; 
            }
        }
        
        // 이벤트 핸들러 메서드 - 리플렉션으로 연결하기 위해 public으로 선언
        
        /// <summary>
        /// WebSocket OnOpen 이벤트 핸들러
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void HandleWebSocketOpen()
        {
            Debug.Log($"[MetaAdapter-{_instanceId}] Native WebSocket connected! Clearing simulation flags.");
            _internalState = InternalWebSocketState.Open;
            _simulationConnectionEstablished = false; 
            _simulationRemoteDescriptionSet = false; // 네이티브 연결 시 시뮬레이션 상태 초기화
            OnConnected?.Invoke();
        }
        
        /// <summary>
        /// WebSocket OnMessage 이벤트 핸들러
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void HandleWebSocketMessage(byte[] data)
        {
            string messageJson = System.Text.Encoding.UTF8.GetString(data);
            Debug.Log($"[MetaAdapter-{_instanceId}] Native Message received ({data.Length} bytes): {messageJson.Substring(0, Math.Min(messageJson.Length, 100))}...");
            HandleMessage(messageJson);
        }
        
        /// <summary>
        /// WebSocket OnError 이벤트 핸들러
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void HandleWebSocketError(string errorMsg)
        {
            Debug.LogError($"[MetaAdapter-{_instanceId}] Native WebSocket error: {errorMsg}. Closing connection state.");
            if (_internalState != InternalWebSocketState.Closed)
            {
                _internalState = InternalWebSocketState.Closed;
                _simulationConnectionEstablished = false;
                _simulationRemoteDescriptionSet = false;
                OnDisconnected?.Invoke();
            }
        }
        
        /// <summary>
        /// WebSocket OnClose 이벤트 핸들러
        /// </summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void HandleWebSocketClose(object closeCodeObj) 
        {
            Debug.Log($"[MetaAdapter-{_instanceId}] Native WebSocket closed with code: {closeCodeObj}. Closing connection state.");
            if (_internalState != InternalWebSocketState.Closed)
            {
                _internalState = InternalWebSocketState.Closed;
                _simulationConnectionEstablished = false;
                _simulationRemoteDescriptionSet = false;
                OnDisconnected?.Invoke();
            }
            _webSocket = null;
        }

        /// <summary>
        /// 실제 빌드에서는 내부 상태를 전환하는 콜백 메서드를 리플렉션으로 연결합니다.
        /// </summary>
        private void HandleMessage(string messageJson)
        {
            Debug.Log($"[MetaAdapter-{_instanceId}] HandleMessage processing: {messageJson.Substring(0, Math.Min(messageJson.Length, 100))}...");
            try
            {
                var baseMessage = JsonUtility.FromJson<SignalingMessageBase>(messageJson);
                if (baseMessage != null && !string.IsNullOrEmpty(baseMessage.type))
                {
                    if (baseMessage.type == "answer") // Answer 메시지 수신 시 (시뮬레이션이든 아니든)
                    {
                        if (_isSimulationMode) 
                        {
                            _simulationRemoteDescriptionSet = true;
                            Debug.Log($"[MetaAdapter-{_instanceId}] Simulated Answer received, _simulationRemoteDescriptionSet = true");
                        }
                        // 실제 연결에서는 WebRTC 스택이 처리하므로 별도 플래그 설정 불필요
                    }
                    OnSignalingMessageReceived?.Invoke(baseMessage.type, messageJson);
                }
                else
                {
                    Debug.LogWarning($"[MetaAdapter-{_instanceId}] Could not determine message type from JSON: {messageJson}");
                    OnSignalingMessageReceived?.Invoke("unknown", messageJson);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MetaAdapter-{_instanceId}] Error parsing received message: {e.Message}");
                OnSignalingMessageReceived?.Invoke("error", messageJson);
            }
        }
    }

    // --- Meta Adapter용 Assembly Definition ---
    // 경로: quest-app/UnityProject/Assets/Scripts/Signaling/Adapters/UnityVerseBridge.QuestApp.Signaling.Adapters.asmdef
    /* JSON 내용:
    {
        "name": "UnityVerseBridge.QuestApp.Signaling.Adapters",
        "rootNamespace": "UnityVerseBridge.QuestApp.Signaling",
        "references": [
            "UnityVerseBridge.Core.Runtime", // Core의 인터페이스 등 참조
            "Meta.Voice.SDK",                // 예시: Meta Voice SDK의 어셈블리 이름 (정확한 이름 확인!)
            "Oculus.VR"                      // 예시: Meta XR SDK의 어셈블리 이름 (정확한 이름 확인!)
        ],
        "includePlatforms": [ "Android" ], // Quest는 Android 기반이므로 플랫폼 지정 가능 (선택 사항)
        "excludePlatforms": [],
        // ... 나머지 asmdef 설정 ...
    }
    */
}