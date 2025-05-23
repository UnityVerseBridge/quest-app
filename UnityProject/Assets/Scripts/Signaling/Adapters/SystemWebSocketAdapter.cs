using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityVerseBridge.Core.Signaling;
using CoreWebSocketState = UnityVerseBridge.Core.Signaling.WebSocketState;
using NetWebSocketState = System.Net.WebSockets.WebSocketState;

namespace UnityVerseBridge.QuestApp.Signaling
{
    /// <summary>
    /// System.Net.WebSockets를 사용하는 IWebSocketClient 구현체
    /// Unity Editor와 Quest 디바이스 모두에서 작동
    /// </summary>
    public class SystemWebSocketAdapter : IWebSocketClient
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _receiveTask;
        private CoreWebSocketState _state = CoreWebSocketState.Closed;
        private readonly Queue<Action> _messageQueue = new Queue<Action>();

        // IWebSocketClient 이벤트
        public event Action OnOpen;
        public event Action<byte[]> OnMessage;
        public event Action<string> OnError;
        public event Action<ushort> OnClose;

        public CoreWebSocketState State => _state;

        public async Task Connect(string url)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _webSocket = new ClientWebSocket();
                _state = CoreWebSocketState.Connecting;
                
                Debug.Log($"[SystemWebSocket] Connecting to {url}");
                
                await _webSocket.ConnectAsync(new Uri(url), _cancellationTokenSource.Token);
                
                _state = CoreWebSocketState.Open;
                Debug.Log("[SystemWebSocket] Connected!");
                
                // 수신 태스크 시작
                _receiveTask = ReceiveLoop();
                
                // OnOpen 이벤트를 메시지 큐에 추가
                lock (_messageQueue)
                {
                    _messageQueue.Enqueue(() => OnOpen?.Invoke());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SystemWebSocket] Connect failed: {ex.Message}");
                _state = CoreWebSocketState.Closed;
                OnError?.Invoke(ex.Message);
                throw;
            }
        }

        public async Task Close()
        {
            try
            {
                _state = CoreWebSocketState.Closing;
                
                if (_webSocket?.State == NetWebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                
                _cancellationTokenSource?.Cancel();
                
                if (_receiveTask != null)
                {
                    try { await _receiveTask; } catch { }
                }
                
                _webSocket?.Dispose();
                _webSocket = null;
                _state = CoreWebSocketState.Closed;
                
                Debug.Log("[SystemWebSocket] Closed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SystemWebSocket] Close error: {ex.Message}");
            }
        }

        public async Task Send(byte[] bytes)
        {
            if (_state != CoreWebSocketState.Open)
            {
                Debug.LogError("[SystemWebSocket] Cannot send - not open");
                return;
            }

            try
            {
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Binary,
                    true,
                    _cancellationTokenSource.Token
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SystemWebSocket] Send failed: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public async Task SendText(string message)
        {
            if (_state != CoreWebSocketState.Open)
            {
                Debug.LogError("[SystemWebSocket] Cannot send - not open");
                return;
            }

            try
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    _cancellationTokenSource.Token
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SystemWebSocket] SendText failed: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public void DispatchMessageQueue()
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count > 0)
                {
                    _messageQueue.Dequeue()?.Invoke();
                }
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new ArraySegment<byte>(new byte[4096]);
            
            try
            {
                while (_webSocket.State == NetWebSocketState.Open && 
                       !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                    
                    if (result.MessageType == WebSocketMessageType.Text || 
                        result.MessageType == WebSocketMessageType.Binary)
                    {
                        var messageBytes = new byte[result.Count];
                        Array.Copy(buffer.Array, 0, messageBytes, 0, result.Count);
                        
                        lock (_messageQueue)
                        {
                            _messageQueue.Enqueue(() => OnMessage?.Invoke(messageBytes));
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("[SystemWebSocket] Close message received");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[SystemWebSocket] Receive loop cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SystemWebSocket] Receive error: {ex.Message}");
                lock (_messageQueue)
                {
                    _messageQueue.Enqueue(() => OnError?.Invoke(ex.Message));
                }
            }
            finally
            {
                _state = CoreWebSocketState.Closed;
                lock (_messageQueue)
                {
                    _messageQueue.Enqueue(() => OnClose?.Invoke(1000)); // Normal closure
                }
            }
        }
    }
}