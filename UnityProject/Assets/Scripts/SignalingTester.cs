using UnityEngine;
using NativeWebSocket; // NativeWebSocket 네임스페이스 사용

public class SignalingTester : MonoBehaviour
{
    // 접속할 시그널링 서버 주소 (ws:// 로 시작)
    // 주의: 실제 서버 IP 주소와 포트 번호로 변경해야 합니다!
    // 예: 동일 PC -> "ws://localhost:8080"
    // 예: 다른 PC -> "ws://192.168.0.5:8080"
    string serverUrl = "ws://localhost:8080"; // <--- 여기를 실제 서버 주소로 변경하세요!

    WebSocket websocket; // WebSocket 클라이언트 객체

    async void Start()
    {
        Debug.Log($"Connecting to Signaling Server: {serverUrl}");
        websocket = new WebSocket(serverUrl);

        // 연결 성공 시 호출될 함수 등록
        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
            // 연결 성공 후 테스트 메시지 보내기 (선택 사항)
            SendWebSocketMessage("Hello from Unity!");
        };

        // 에러 발생 시 호출될 함수 등록
        websocket.OnError += (e) =>
        {
            Debug.LogError("WebSocket Error! " + e);
        };

        // 연결 종료 시 호출될 함수 등록
        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        // 서버로부터 메시지 수신 시 호출될 함수 등록
        websocket.OnMessage += (bytes) =>
        {
            // 메시지는 byte 배열로 도착합니다.
            // 여기서는 간단히 문자열로 변환하여 로그 출력
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Message received from server: " + message);

            // 만약 서버가 JSON을 보낸다면 파싱 시도 가능
            // try {
            //     var json = JsonUtility.FromJson<YourMessageType>(message);
            //     Debug.Log($"Received message type: {json.type}");
            // } catch (System.Exception ex) {
            //     Debug.LogError($"Failed to parse JSON: {message} - {ex.Message}");
            // }
        };

        // 서버에 연결 시도 (비동기 방식)
        await websocket.Connect();
    }

    void Update()
    {
        // NativeWebSocket 메시지 큐를 계속 처리해야 합니다.
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            // Send data updates in the Update loop if needed
            // Example: websocket.SendText("Update message");
        }
        // Dispatch messages received from the server. Required for NativeWebSocket.
        websocket?.DispatchMessageQueue();
#endif
    }

    // 서버로 텍스트 메시지를 보내는 함수
    public async void SendWebSocketMessage(string message)
    {
        if (websocket.State == WebSocketState.Open)
        {
            Debug.Log($"Sending message to server: {message}");
            // Send text message as plain text
            await websocket.SendText(message);

            // 또는 JSON 형태로 보내려면:
            // YourMessageType dataToSend = new YourMessageType { type = "greeting", content = message };
            // string jsonMessage = JsonUtility.ToJson(dataToSend);
            // Debug.Log($"Sending JSON to server: {jsonMessage}");
            // await websocket.SendText(jsonMessage);
        }
        else
        {
            Debug.LogWarning("WebSocket is not open. Cannot send message.");
        }
    }

    // 애플리케이션 종료 시 WebSocket 연결 확실히 닫기
    private async void OnApplicationQuit()
    {
        if (websocket != null && websocket.State != WebSocketState.Closed)
        {
            Debug.Log("Closing WebSocket connection on application quit.");
            await websocket.Close();
        }
    }
}

// (선택 사항) JSON 메시지 예시 타입 정의
// [System.Serializable]
// public class YourMessageType
// {
//     public string type;
//     public string content;
//     // 필요한 다른 필드들...
// }