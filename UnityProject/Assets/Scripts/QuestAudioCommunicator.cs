using UnityEngine;
using UnityVerseBridge.Core;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Quest 앱에서 양방향 오디오 통신을 담당하는 클래스
    /// Core의 AudioStreamManager를 Quest 환경에 맞게 설정합니다.
    /// </summary>
    public class QuestAudioCommunicator : MonoBehaviour
    {
        [SerializeField] private AudioStreamManager audioStreamManager;
        
        void Start()
        {
            if (audioStreamManager == null)
            {
                audioStreamManager = GetComponent<AudioStreamManager>();
                if (audioStreamManager == null)
                {
                    audioStreamManager = gameObject.AddComponent<AudioStreamManager>();
                }
            }
            
            // Quest 전용 설정 적용
            ConfigureForQuest();
        }
        
        private void ConfigureForQuest()
        {
            // Quest에서는 기본적으로 마이크와 스피커 모두 활성화
            audioStreamManager.SetMicrophoneEnabled(true);
            audioStreamManager.SetSpeakerEnabled(true);
            
            Debug.Log("[QuestAudioCommunicator] Configured AudioStreamManager for Quest bidirectional audio");
        }
    }
}
