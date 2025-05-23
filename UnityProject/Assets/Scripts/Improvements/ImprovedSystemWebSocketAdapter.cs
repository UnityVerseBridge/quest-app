using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityVerseBridge.Core.Signaling;
using CoreWebSocketState = UnityVerseBridge.Core.Signaling.WebSocketState;
using NetWebSocketState = System.Net.WebSockets.WebSocketState;

namespace UnityVerseBridge.QuestApp.Signaling.Improved
{
    /// <summary>
    /// 개선된 System.Net.WebSockets 어댑터 - 동적 버퍼 관리
    /// </summary>
    public class ImprovedSystemWebSocketAdapter : IWebSocketClient
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _receiveTask;
        private CoreWebSocketState _state = CoreWebSocketState.Closed;
        private readonly Queue<Action> _messageQueue = new Queue<Action>();

        // 이벤트
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
                
                Debug.Log($"[ImprovedSystemWebSocket] Connecting to {url}");
                
                await _webSocket.ConnectAsync(new Uri(url), _cancellationTokenSource.Token);
                
                _state = CoreWebSocketState.Open;
                Debug.Log("[ImprovedSystemWebSocket] Connected!");
                
                _receiveTask = ReceiveLoop();
                
                lock (_messageQueue)
                {
                    _messageQueue.Enqueue(() => OnOpen?.Invoke());
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ImprovedSystemWebSocket] Connect failed: {ex.Message}");
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
                
                Debug.Log("[ImprovedSystemWebSocket] Closed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ImprovedSystemWebSocket] Close error: {ex.Message}");
            }
        }

        public async Task Send(byte[] bytes)
        {
            if (_state != CoreWebSocketState.Open)
            {
                Debug.LogError("[ImprovedSystemWebSocket] Cannot send - not open");
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
                Debug.LogError($"[ImprovedSystemWebSocket] Send failed: {ex.Message}");
                OnError?.Invoke(ex.Message);
            }
        }

        public async Task SendText(string message)
        {
            if (_state != CoreWebSocketState.Open)
            {
                Debug.LogError("[ImprovedSystemWebSocket] Cannot send - not open");
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
                Debug.LogError($"[ImprovedSystemWebSocket] SendText failed: {ex.Message}");
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
            // 동적 버퍼 관리를 위한 MemoryStream
            using var messageStream = new MemoryStream();
            var buffer = new ArraySegment<byte>(new byte[8192]); // 8KB 초기 버퍼
            
            try
            {
                while (_webSocket.State == NetWebSocketState.Open && 
                       !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    WebSocketReceiveResult result;
                    messageStream.SetLength(0); // 스트림 초기화
                    
                    // 전체 메시지를 수신할 때까지 반복
                    do
                    {
                        result = await _webSocket.ReceiveAsync(buffer, _cancellationTokenSource.Token);
                        
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Debug.Log("[ImprovedSystemWebSocket] Close message received");
                            return;
                        }
                        
                        messageStream.Write(buffer.Array, buffer.Offset, result.Count);
                        
                    } while (!result.EndOfMessage);
                    
                    // 완전한 메시지 처리
                    if (result.MessageType == WebSocketMessageType.Text || 
                        result.MessageType == WebSocketMessageType.Binary)
                    {
                        var messageBytes = messageStream.ToArray();
                        
                        lock (_messageQueue)
                        {
                            _messageQueue.Enqueue(() => OnMessage?.Invoke(messageBytes));
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[ImprovedSystemWebSocket] Receive loop cancelled");
            }
            catch (WebSocketException wsEx)
            {
                Debug.LogError($"[ImprovedSystemWebSocket] WebSocket error: {wsEx.WebSocketErrorCode} - {wsEx.Message}");
                lock (_messageQueue)
                {
                    _messageQueue.Enqueue(() => OnError?.Invoke(wsEx.Message));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ImprovedSystemWebSocket] Receive error: {ex.Message}");
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
