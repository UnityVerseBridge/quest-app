using System; // Exception 클래스 사용을 위해 추가
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
            Debug.Log("[VrStreamSender] Start() 호출됨");
            if (webRtcManager == null)
            {
                Debug.LogError("[VrStreamSender] WebRtcManager가 할당되지 않았습니다!");
                enabled = false;
                return;
            }
            Debug.Log($"[VrStreamSender] WebRtcManager 할당됨: {webRtcManager.name}");
            
            // 카메라 설정 - 지정 안 된 경우 Main Camera 사용
            if (targetCamera == null)
            {
                Debug.Log("[VrStreamSender] targetCamera가 null이므로 Camera.main을 사용합니다.");
                targetCamera = Camera.main;
            }
                
            if (targetCamera == null)
            {
                Debug.LogError("[VrStreamSender] targetCamera를 찾을 수 없습니다! 스트리밍이 불가능합니다.");
                enabled = false;
                return;
            }
            Debug.Log($"[VrStreamSender] targetCamera 할당됨: {targetCamera.name}, 활성 상태: {targetCamera.gameObject.activeInHierarchy}");
            
            // RenderTexture 생성 또는 확인
            if (sourceRenderTexture == null)
            {
                Debug.LogWarning("[VrStreamSender] sourceRenderTexture가 Inspector에서 할당되지 않아 자동 생성합니다 (1280x720).");
                sourceRenderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.DefaultHDR); // HDR 포맷 사용 고려
                sourceRenderTexture.name = "StreamRenderTexture_AutoCreated";
                sourceRenderTexture.Create(); // 생성 후 바로 Create()
            }
            else
            {
                Debug.Log($"[VrStreamSender] sourceRenderTexture가 Inspector에서 할당됨: {sourceRenderTexture.name}, Size: {sourceRenderTexture.width}x{sourceRenderTexture.height}, Created: {sourceRenderTexture.IsCreated()}");
            }
            
            // 반드시 RenderTexture가 생성되고 초기화되었는지 확인
            if (!sourceRenderTexture.IsCreated())
            {
                Debug.LogWarning($"[VrStreamSender] sourceRenderTexture ({sourceRenderTexture.name})가 생성되지 않아 Create()를 호출합니다.");
                sourceRenderTexture.Create();
            }
            Debug.Log($"[VrStreamSender] sourceRenderTexture ({sourceRenderTexture.name}) 최종 상태 - Size: {sourceRenderTexture.width}x{sourceRenderTexture.height}, Created: {sourceRenderTexture.IsCreated()}, GraphicsFormat: {sourceRenderTexture.graphicsFormat}");

            // 게임 뷰 표시 여부에 따라 다른 설정
            if (showInGameView)
            {
                Debug.Log("[VrStreamSender] showInGameView=true. 미러 카메라 설정을 시작합니다.");
                SetupMirrorCamera(); // 새로운 방식: 카메라 복제 사용
            }
            else
            {
                Debug.Log("[VrStreamSender] showInGameView=false. targetCamera의 targetTexture를 sourceRenderTexture로 직접 설정합니다.");
                targetCamera.targetTexture = sourceRenderTexture;
            }
            
            // UI 미리보기 설정 (선택 사항)
            if (previewImage != null)
            {
                Debug.Log($"[VrStreamSender] previewImage ({previewImage.gameObject.name})에 sourceRenderTexture 할당 시도.");
                previewImage.texture = sourceRenderTexture;
                if (previewImage.texture == sourceRenderTexture)
                    Debug.Log("[VrStreamSender] previewImage에 텍스처 할당 성공.");
                else
                    Debug.LogError("[VrStreamSender] previewImage에 텍스처 할당 실패!");
            }
            else
            {
                Debug.Log("[VrStreamSender] previewImage가 할당되지 않았습니다.");
            }
            
            // WebRTC 연결 성공 이벤트 구독
            Debug.Log("[VrStreamSender] WebRtcManager.OnWebRtcConnected 이벤트 구독 시도.");
            webRtcManager.OnWebRtcConnected += StartStreaming;
            Debug.Log("[VrStreamSender] Start() 완료.");
        }
        
        // 새로운 방식: 메인 카메라와 동일하게 세팅된 복제 카메라를 생성하여 사용
        private void SetupMirrorCamera()
        {
            Debug.Log($"[VrStreamSender] SetupMirrorCamera() 호출됨 (targetCamera: {targetCamera.name}).");
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
            
            // 미러 카메라 설정
            mirrorCamera = mirrorCameraObj.AddComponent<Camera>();
            
            // 메인 카메라 설정 복사
            mirrorCamera.CopyFrom(targetCamera);
            Debug.Log("[VrStreamSender] mirrorCamera.CopyFrom(targetCamera) 완료.");
            
            // 특정 설정은 다시 지정
            mirrorCamera.targetTexture = sourceRenderTexture;
            // URP에서는 depth 값만으로 렌더링 순서 제어가 완벽하지 않을 수 있음. Stacked Camera 등을 고려할 수 있으나, 우선 depth로 시도.
            mirrorCamera.depth = targetCamera.depth - 1; 
            mirrorCamera.enabled = true; // 명시적으로 활성화
            targetCamera.targetTexture = null; // 원본 카메라는 게임 뷰에 직접 렌더링하도록 targetTexture를 null로 설정

            Debug.Log($"[VrStreamSender] mirrorCamera ({mirrorCamera.name}) 설정 완료: targetTexture={mirrorCamera.targetTexture?.name}, depth={mirrorCamera.depth}, enabled={mirrorCamera.enabled}");
            Debug.Log($"[VrStreamSender] targetCamera ({targetCamera.name}) 설정 완료: targetTexture is null: {targetCamera.targetTexture == null}");
            
            // 위치 및 회전 동기화는 이제 부모-자식 관계로 처리되므로 별도 코루틴 불필요
            // StartCoroutine(SyncCameraTransform()); // 제거
            
            Debug.Log("[VrStreamSender] 미러 카메라 설정 완료: 게임 뷰와 RenderTexture 모두 표시 (부모-자식 동기화)");
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

        void OnDestroy()
        {
            Debug.Log("[VrStreamSender] OnDestroy() 호출됨");
            // 이벤트 구독 해지 및 트랙 정리
            if (webRtcManager != null)
            {
                Debug.Log("[VrStreamSender] WebRtcManager.OnWebRtcConnected 이벤트 구독 해지.");
                webRtcManager.OnWebRtcConnected -= StartStreaming;
            }
            
            StopStreaming(); // 스트리밍 중지 (트랙 제거 등)
            
            // 미러 카메라 정리
            if (mirrorCamera != null)
            {
                Debug.Log($"[VrStreamSender] mirrorCamera ({mirrorCamera.name}) 제거 시도.");
                if (Application.isPlaying)
                    Destroy(mirrorCamera.gameObject);
                else
                    DestroyImmediate(mirrorCamera.gameObject);
                mirrorCamera = null;
                Debug.Log("[VrStreamSender] mirrorCamera 제거 완료.");
            }
            
            // 타겟 카메라 설정 복원 (showInGameView가 false였을 경우에만)
            if (targetCamera != null && !showInGameView && sourceRenderTexture == targetCamera.targetTexture)
            {
                Debug.Log($"[VrStreamSender] targetCamera ({targetCamera.name})의 targetTexture를 null로 복원합니다 (원래는 sourceRenderTexture였음).");
                targetCamera.targetTexture = null; // 복원 시에는 null로 설정 (원래 상태로)
            }
            Debug.Log("[VrStreamSender] OnDestroy() 완료.");
        }

        private void StartStreaming()
        {
            Debug.Log("[VrStreamSender] StartStreaming() 호출됨.");
            if (trackAdded)
            {
                Debug.LogWarning("[VrStreamSender] 이미 비디오 트랙이 추가되어 스트리밍 중입니다. 중복 호출 방지.");
                return;
            }

            Debug.Log("[VrStreamSender] WebRTC Connected. Starting video stream...");

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
            Debug.Log($"[VrStreamSender] sourceRenderTexture ({sourceRenderTexture.name}) 확인 완료 - Size: {sourceRenderTexture.width}x{sourceRenderTexture.height}, Created: {sourceRenderTexture.IsCreated()}, GraphicsFormat: {sourceRenderTexture.graphicsFormat}");

            try
            {
                Debug.Log("[VrStreamSender] VideoStreamTrack 생성 시도...");
                videoStreamTrack = new VideoStreamTrack(sourceRenderTexture);
                Debug.Log($"[VrStreamSender] VideoStreamTrack 생성 성공! Track ID: {videoStreamTrack.Id}, Kind: {videoStreamTrack.Kind}, ReadyState: {videoStreamTrack.ReadyState}");
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
                Debug.Log("[VrStreamSender] WebRtcManager.AddVideoTrack() 호출 시도...");
                webRtcManager.AddVideoTrack(videoStreamTrack);
                trackAdded = true;
                Debug.Log("[VrStreamSender] WebRtcManager.AddVideoTrack() 호출 성공. trackAdded = true.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[VrStreamSender] WebRtcManager.AddVideoTrack() 중 예외 발생: {e.Message}\n{e.StackTrace}");
                trackAdded = false; // 실패 시 플래그 원복
            }
        }

        private void StopStreaming()
        {
            Debug.Log("[VrStreamSender] StopStreaming() 호출됨.");
            if (videoStreamTrack != null)
            {
                Debug.Log($"[VrStreamSender] 기존 비디오 트랙 (ID: {videoStreamTrack.Id}) 정리 시도...");
                // TODO: WebRtcManager에 RemoveTrack 기능 구현 필요
                // if (webRtcManager != null) webRtcManager.RemoveTrack(videoStreamTrack);
                videoStreamTrack.Dispose(); // 트랙 리소스 해제
                videoStreamTrack = null;
                trackAdded = false;
                Debug.Log("[VrStreamSender] 비디오 스트림 트랙 중지 및 정리 완료. trackAdded = false.");
            }
            else
            {
                Debug.Log("[VrStreamSender] 정리할 비디오 트랙이 없습니다.");
            }
        }
    }
}
