using System; // Exception 클래스 사용을 위해 추가
using System.Collections;
using UnityEngine;
using Unity.WebRTC; // VideoStreamTrack 사용
using UnityVerseBridge.Core; // WebRtcManager 사용
using UnityEngine.Rendering; // CommandBuffer 사용
using UnityEngine.UI; // RawImage 클래스 사용을 위해 추가

namespace UnityVerseBridge.QuestApp
{
    public class VrStreamSender : MonoBehaviour
    {
        [SerializeField] private WebRtcManager webRtcManager;
        [SerializeField] private RenderTexture sourceRenderTexture; // Inspector에서 할당
        
        [Header("카메라 설정")]
        [SerializeField] private Camera targetCamera; // 스트리밍할 카메라
        [Tooltip("게임 뷰와 스트림 동시 표시를 위한 옵션")]
        [SerializeField] private bool showInGameView = true;
        [SerializeField] private UnityEngine.UI.RawImage previewImage; // UnityEngine.UI.RawImage로 타입 변경

        private VideoStreamTrack videoStreamTrack;
        private bool trackAdded = false;
        private Camera mirrorCamera; // URP에서 사용하는 복제 카메라
        
        void Start()
        {
            if (webRtcManager == null)
            {
                Debug.LogError("[VrStreamSender] WebRtcManager가 할당되지 않았습니다!");
                enabled = false;
                return;
            }
            
            Debug.Log($"[VrStreamSender] WebRTC connection state at start: {webRtcManager.IsWebRtcConnected}");

            
            // 카메라 설정 - 지정 안 된 경우 Main Camera 사용
            if (targetCamera == null)
            {

                targetCamera = Camera.main;
            }
                
            if (targetCamera == null)
            {
                Debug.LogError("[VrStreamSender] targetCamera를 찾을 수 없습니다! 스트리밍이 불가능합니다.");
                enabled = false;
                return;
            }

            
            // Quest 디바이스에서 호환되는 RenderTexture 확보
            if (sourceRenderTexture == null)
            {
                Debug.LogWarning("[VrStreamSender] sourceRenderTexture가 Inspector에서 할당되지 않아 Quest 호환 텍스처를 생성합니다.");
                sourceRenderTexture = QuestRenderTextureHelper.CreateCompatibleRenderTexture(1280, 720, "StreamRenderTexture_AutoCreated");
            }
            else
            {
                Debug.Log($"[VrStreamSender] sourceRenderTexture 호환성 확인: {sourceRenderTexture.name}");
                sourceRenderTexture = QuestRenderTextureHelper.EnsureCompatibility(sourceRenderTexture, sourceRenderTexture.width, sourceRenderTexture.height);
            }
            
            if (sourceRenderTexture == null || !sourceRenderTexture.IsCreated())
            {
                Debug.LogError("[VrStreamSender] Failed to create Quest-compatible RenderTexture!");
                enabled = false;
                return;
            }
            
            Debug.Log($"[VrStreamSender] Using RenderTexture: {sourceRenderTexture.name}, Size: {sourceRenderTexture.width}x{sourceRenderTexture.height}, Format: {sourceRenderTexture.format}, Created: {sourceRenderTexture.IsCreated()}");


            // 게임 뷰 표시 여부에 따라 다른 설정
            // showInGameView = true: VR 헤드셋에도 표시하고 스트리밍도 함
            // showInGameView = false: 스트리밍만 하고 VR 헤드셋에는 표시 안 함
        if (showInGameView)
            {
                SetupMirrorCamera(); // 미러 카메라를 생성하여 두 곳에 렌더링
            }
            else
        {
            // 메인 카메라의 출력을 RenderTexture로 변경 (헤드셋에 안 보임)
                targetCamera.targetTexture = sourceRenderTexture;
        }
            
            // UI 미리보기 설정 (선택 사항)
            if (previewImage != null)
            {
                previewImage.texture = sourceRenderTexture;
            }

            
            // WebRTC 연결 성공 이벤트 구독
            webRtcManager.OnWebRtcConnected += StartStreaming;
            
            // Check if we should add video track immediately (if peer connection exists)
            StartCoroutine(CheckAndAddVideoTrackEarly());
        }
        
        /// <summary>
        /// 미러 카메라를 생성하여 VR 헤드셋과 스트리밍을 동시에 처리합니다.
        /// 메인 카메라는 VR 헤드셋에 그대로 표시하고,
        /// 미러 카메라는 RenderTexture에 렌더링하여 스트리밍합니다.
        /// </summary>
        private void SetupMirrorCamera()
        {

            if (mirrorCamera != null)
            {
                Debug.LogWarning("[VrStreamSender] 기존 mirrorCamera가 이미 존재합니다. 정리 후 다시 생성합니다.");
                Destroy(mirrorCamera.gameObject);
            }

            // 미러 카메라용 게임 오브젝트 생성
            GameObject mirrorCameraObj = new GameObject("MirrorCamera_" + targetCamera.name);
            mirrorCameraObj.transform.parent = targetCamera.transform; // 원본 카메라의 자식으로 설정하여 함께 움직이도록 함
            mirrorCameraObj.transform.localPosition = Vector3.zero;
            mirrorCameraObj.transform.localRotation = Quaternion.identity;
            mirrorCameraObj.transform.localScale = Vector3.one;
            
            // 미러 카메라 컴포넌트 추가 및 설정 복사
            mirrorCamera = mirrorCameraObj.AddComponent<Camera>();
            
            // CopyFrom: 메인 카메라의 모든 설정을 복사
            // FOV, Near/Far plane, Culling mask, Clear flags 등 모두 포함
            mirrorCamera.CopyFrom(targetCamera);

            
            // 스트리밍을 위한 설정 오버라이드
            mirrorCamera.targetTexture = sourceRenderTexture; // 미러 카메라는 RenderTexture로 출력
            
            // 렌더링 순서 설정 (URP/Built-in 모두 지원)
            // 미러 카메라를 먼저 렌더링하여 RenderTexture에 저장
            mirrorCamera.depth = targetCamera.depth - 1; 
            mirrorCamera.enabled = true;
            
            // 원본 카메라는 VR 헤드셋에 직접 렌더링
            targetCamera.targetTexture = null;


        }
        
        // 미러 카메라의 변환을 메인 카메라와 동기화 (이제 사용 안 함)
        /*
        private System.Collections.IEnumerator SyncCameraTransform()
        {
            while (true)
            {
                if (targetCamera != null && mirrorCamera != null)
                {
                    // 위치와 회전 동기화
                    mirrorCamera.transform.position = targetCamera.transform.position;
                    mirrorCamera.transform.rotation = targetCamera.transform.rotation;
                    
                    // 카메라 속성 동기화 (필요한 경우)
                    mirrorCamera.fieldOfView = targetCamera.fieldOfView;
                }
                
                yield return null; // 다음 프레임까지 대기
            }
        }
        */

        private IEnumerator CheckAndAddVideoTrackEarly()
        {
            // Wait a bit for initialization
            yield return new WaitForSeconds(2.0f);
            
            // Check if peer connection exists but we haven't added track yet
            if (webRtcManager != null && !trackAdded)
            {
                var pcState = webRtcManager.GetPeerConnectionState();
                Debug.Log($"[VrStreamSender] Checking if we should add video track early. PC State: {pcState}");
                
                if (pcState == RTCPeerConnectionState.New || pcState == RTCPeerConnectionState.Connecting)
                {
                    Debug.Log("[VrStreamSender] Adding video track early to trigger negotiation...");
                    AddVideoTrack();
                }
            }
        }
        
        void OnDestroy()
        {
            // 이벤트 구독 해지 및 트랙 정리
            if (webRtcManager != null)
            {

                webRtcManager.OnWebRtcConnected -= StartStreaming;
            }
            
            StopStreaming(); // 스트리밍 중지 (트랙 제거 등)
            
            // 미러 카메라 정리
            if (mirrorCamera != null)
            {
                if (Application.isPlaying)
                    Destroy(mirrorCamera.gameObject);
                else
                    DestroyImmediate(mirrorCamera.gameObject);
                mirrorCamera = null;
            }
            
            // 타겟 카메라 설정 복원 (showInGameView가 false였을 경우에만)
            if (targetCamera != null && !showInGameView && sourceRenderTexture == targetCamera.targetTexture)
            {

                targetCamera.targetTexture = null; // 복원 시에는 null로 설정 (원래 상태로)
            }

        }

        private async void StartStreaming()
        {
            if (trackAdded)
            {
                Debug.LogWarning("[VrStreamSender] 이미 비디오 트랙이 추가되어 스트리밍 중입니다. 중복 호출 방지.");
                return;
            }

            Debug.Log("[VrStreamSender] WebRTC Connected. Starting video stream...");
            
            // Wait a bit for initial negotiation to complete
            await System.Threading.Tasks.Task.Delay(1000);
            
            AddVideoTrack();
        }
        
        private void AddVideoTrack()
        {
            if (trackAdded)
            {
                Debug.LogWarning("[VrStreamSender] Video track already added.");
                return;
            }

            if (sourceRenderTexture == null)
            {
                Debug.LogError("[VrStreamSender] sourceRenderTexture가 null입니다! 비디오 트랙을 생성할 수 없습니다.");
                return;
            }
            if (!sourceRenderTexture.IsCreated())
            {
                 Debug.LogError($"[VrStreamSender] sourceRenderTexture ({sourceRenderTexture.name})가 생성되지 않았습니다! 비디오 트랙을 생성할 수 없습니다.");
                return;
            }


            try
            {
                // Get supported format for current graphics API
                var gfxType = SystemInfo.graphicsDeviceType;
                var supportedFormat = WebRTC.GetSupportedRenderTextureFormat(gfxType);
                Debug.Log($"[VrStreamSender] Graphics API: {gfxType}, Supported format: {supportedFormat}");
                
                videoStreamTrack = new VideoStreamTrack(sourceRenderTexture);
                Debug.Log($"[VrStreamSender] VideoStreamTrack created successfully. Track ID: {videoStreamTrack.Id}");
                
                // Check if track is enabled
                if (!videoStreamTrack.Enabled)
                {
                    Debug.LogWarning("[VrStreamSender] Video track is not enabled. Enabling...");
                    videoStreamTrack.Enabled = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[VrStreamSender] VideoStreamTrack 생성 중 예외 발생: {e.Message}\n{e.StackTrace}");
                return;
            }
            
            if (webRtcManager == null)
            {
                Debug.LogError("[VrStreamSender] WebRtcManager가 null이므로 비디오 트랙을 추가할 수 없습니다.");
                return;
            }

            try
            {
                webRtcManager.AddVideoTrack(videoStreamTrack);
                trackAdded = true;
                Debug.Log("[VrStreamSender] Video track added to WebRTC connection - this should trigger negotiation");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VrStreamSender] WebRtcManager.AddVideoTrack() 중 예외 발생: {e.Message}\n{e.StackTrace}");
                trackAdded = false; // 실패 시 플래그 원복
            }
        }

        private void StopStreaming()
        {
            if (videoStreamTrack != null)
            {
                // TODO: WebRtcManager에 RemoveTrack 기능 구현 필요
                // if (webRtcManager != null) webRtcManager.RemoveTrack(videoStreamTrack);
                videoStreamTrack.Dispose(); // 트랙 리소스 해제
                videoStreamTrack = null;
                trackAdded = false;
            }

        }
    }
}
