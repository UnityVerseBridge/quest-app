using UnityEngine;
using UnityEngine.UI;
using UnityVerseBridge.Core;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Quest 앱에서 Room ID를 표시하여 Mobile 앱에서 입력할 수 있도록 함
    /// QR 코드 대신 텍스트로 표시 (ZXing 라이브러리 없이 구현)
    /// </summary>
    public class RoomIdDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text roomIdText;
        [SerializeField] private Text serverUrlText;
        [SerializeField] private GameObject infoPanel;
        [SerializeField] private Button copyButton;
        
        [Header("Settings")]
        [SerializeField] private ConnectionConfig connectionConfig;
        
        void Start()
        {
            DisplayRoomInfo();
            
            // Copy button 설정
            if (copyButton != null)
            {
                copyButton.onClick.AddListener(CopyRoomIdToClipboard);
            }
        }
        
        private void DisplayRoomInfo()
        {
            if (connectionConfig == null) return;
            
            string roomId = connectionConfig.GetRoomId();
            string serverUrl = connectionConfig.signalingServerUrl;
            
            // UI에 표시
            if (roomIdText != null)
            {
                roomIdText.text = $"Room ID: {roomId}";
                // 폰트 크기를 크게 해서 읽기 쉽게
                roomIdText.fontSize = 32;
            }
            
            if (serverUrlText != null)
            {
                serverUrlText.text = $"Server: {serverUrl}";
            }
            
            Debug.Log($"[RoomIdDisplay] Displaying room info - Room: {roomId}, Server: {serverUrl}");
        }
        
        private void CopyRoomIdToClipboard()
        {
            if (connectionConfig == null) return;
            
            string roomId = connectionConfig.GetRoomId();
            GUIUtility.systemCopyBuffer = roomId;
            
            Debug.Log($"[RoomIdDisplay] Copied room ID to clipboard: {roomId}");
            
            // 시각적 피드백
            if (copyButton != null)
            {
                var buttonText = copyButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Copied!";
                    Invoke(nameof(ResetCopyButtonText), 2f);
                }
            }
        }
        
        private void ResetCopyButtonText()
        {
            if (copyButton != null)
            {
                var buttonText = copyButton.GetComponentInChildren<Text>();
                if (buttonText != null)
                {
                    buttonText.text = "Copy Room ID";
                }
            }
        }
        
        public void ShowInfoPanel()
        {
            if (infoPanel != null)
            {
                infoPanel.SetActive(true);
            }
        }
        
        public void HideInfoPanel()
        {
            if (infoPanel != null)
            {
                infoPanel.SetActive(false);
            }
        }
    }
}