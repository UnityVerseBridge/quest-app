using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Quest 디바이스에서 디버그 로그를 화면에 표시하는 헬퍼
    /// </summary>
    public class QuestDebugLogger : MonoBehaviour
    {
        private static QuestDebugLogger instance;
        private List<string> logs = new List<string>();
        private const int maxLogs = 15;
        private GUIStyle logStyle;
        private bool showLogs = true;
        
        [Header("Display Settings")]
        [SerializeField] private int fontSize = 20;
        [SerializeField] private Color logColor = Color.white;
        [SerializeField] private Color errorColor = Color.red;
        [SerializeField] private Color warningColor = Color.yellow;
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                Application.logMessageReceived += HandleLog;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void OnDestroy()
        {
            if (instance == this)
            {
                Application.logMessageReceived -= HandleLog;
            }
        }
        
        void HandleLog(string logString, string stackTrace, LogType type)
        {
            // WebRTC 관련 로그만 필터링
            if (!logString.Contains("VrStreamSender") && 
                !logString.Contains("WebRtc") && 
                !logString.Contains("RenderTexture") &&
                !logString.Contains("VideoStreamTrack") &&
                !logString.Contains("Quest"))
            {
                return;
            }
            
            Color color = logColor;
            string prefix = "";
            
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    color = errorColor;
                    prefix = "[ERROR] ";
                    break;
                case LogType.Warning:
                    color = warningColor;
                    prefix = "[WARN] ";
                    break;
                default:
                    prefix = "[INFO] ";
                    break;
            }
            
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            string formattedLog = $"<color=#{colorHex}>{prefix}{logString}</color>";
            
            logs.Add(formattedLog);
            
            if (logs.Count > maxLogs)
            {
                logs.RemoveAt(0);
            }
        }
        
        void OnGUI()
        {
            if (!showLogs) return;
            
            if (logStyle == null)
            {
                logStyle = new GUIStyle(GUI.skin.label);
                logStyle.fontSize = fontSize;
                logStyle.wordWrap = true;
                logStyle.richText = true;
            }
            
            // 배경 박스
            GUI.Box(new Rect(10, 10, Screen.width - 20, (fontSize + 5) * logs.Count + 50), "Quest Debug Logs");
            
            // 토글 버튼
            if (GUI.Button(new Rect(Screen.width - 110, 15, 90, 30), showLogs ? "Hide" : "Show"))
            {
                showLogs = !showLogs;
            }
            
            // 로그 표시
            GUILayout.BeginArea(new Rect(15, 50, Screen.width - 30, Screen.height - 60));
            
            foreach (string log in logs)
            {
                GUILayout.Label(log, logStyle);
            }
            
            GUILayout.EndArea();
        }
        
        public static void Log(string message)
        {
            Debug.Log($"[QuestDebug] {message}");
        }
        
        public static void LogError(string message)
        {
            Debug.LogError($"[QuestDebug] {message}");
        }
        
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"[QuestDebug] {message}");
        }
    }
}