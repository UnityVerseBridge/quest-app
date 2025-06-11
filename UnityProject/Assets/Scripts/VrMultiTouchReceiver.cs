using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.DataChannel.Data;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Quest VR에서 여러 모바일 기기로부터 터치 입력을 수신하고
    /// 각 기기별로 다른 색상으로 2D UI에 표시하는 컴포넌트입니다.
    /// </summary>
    public class VrMultiTouchReceiver : MonoBehaviour
    {
        [Header("WebRTC Manager")]
        [SerializeField] private WebRtcManager webRtcManager;

        [Header("Touch Display Settings")]
        [Tooltip("터치 포인터를 표시할 캔버스입니다.")]
        [SerializeField] private Canvas touchCanvas;
        
        [Tooltip("터치 포인터 프리팹입니다.")]
        [SerializeField] private GameObject touchPointerPrefab;
        
        [Tooltip("피어별 색상 팔레트입니다.")]
        [SerializeField] private Color[] peerColors = new Color[]
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            Color.magenta,
            Color.cyan,
            new Color(1f, 0.5f, 0f), // Orange
            new Color(0.5f, 0f, 1f)  // Purple
        };

        [Header("Touch Visualization")]
        [Tooltip("터치 포인터의 기본 크기입니다.")]
        [SerializeField] private float pointerSize = 50f;
        
        [Tooltip("터치 시 포인터 크기 애니메이션입니다.")]
        [SerializeField] private bool animateOnTouch = true;
        
        [Tooltip("터치 트레일을 표시합니다.")]
        [SerializeField] private bool showTouchTrail = true;
        
        [Tooltip("피어 이름을 표시합니다.")]
        [SerializeField] private bool showPeerLabel = true;

        [Header("Camera Reference")]
        [Tooltip("VR 카메라 참조입니다.")]
        [SerializeField] private Camera vrCamera;

        // 피어별 터치 정보 관리
        private Dictionary<string, PeerTouchInfo> peerTouches = new Dictionary<string, PeerTouchInfo>();
        private Dictionary<string, Color> peerColorMap = new Dictionary<string, Color>();
        private int nextColorIndex = 0;

        private class PeerTouchInfo
        {
            public string PeerId { get; set; }
            public GameObject PointerObject { get; set; }
            public Image PointerImage { get; set; }
            public Text PeerLabel { get; set; }
            public TrailRenderer Trail { get; set; }
            public Vector2 CurrentPosition { get; set; }
            public bool IsActive { get; set; }
            public float LastTouchTime { get; set; }
            public List<Vector2> TouchHistory { get; set; } = new List<Vector2>();
        }

        void Awake()
        {
            if (webRtcManager == null)
            {
                webRtcManager = FindFirstObjectByType<WebRtcManager>();
                if (webRtcManager == null)
                {
                    Debug.LogError("[VrMultiTouchReceiver] WebRtcManager not found!");
                    enabled = false;
                    return;
                }
            }

            // 캔버스 설정
            SetupCanvas();
            
            // VR 카메라 찾기
            if (vrCamera == null)
            {
                var cameraRig = FindFirstObjectByType<OVRCameraRig>();
                if (cameraRig != null)
                {
                    vrCamera = cameraRig.centerEyeAnchor.GetComponent<Camera>();
                }
            }

            // 기본 포인터 프리팹 생성
            if (touchPointerPrefab == null)
            {
                CreateDefaultPointerPrefab();
            }
        }

        void OnEnable()
        {
            if (webRtcManager != null)
            {
                webRtcManager.OnMultiPeerDataChannelMessageReceived += HandleDataChannelMessage;
                webRtcManager.OnPeerDisconnected += HandlePeerDisconnected;
            }
        }

        void OnDisable()
        {
            if (webRtcManager != null)
            {
                webRtcManager.OnMultiPeerDataChannelMessageReceived -= HandleDataChannelMessage;
                webRtcManager.OnPeerDisconnected -= HandlePeerDisconnected;
            }

            // 모든 터치 포인터 정리
            foreach (var touchInfo in peerTouches.Values)
            {
                if (touchInfo.PointerObject != null)
                {
                    Destroy(touchInfo.PointerObject);
                }
            }
            peerTouches.Clear();
            peerColorMap.Clear();
        }

        void Update()
        {
            // 터치 포인터 위치 업데이트 및 페이드아웃
            foreach (var touchInfo in peerTouches.Values)
            {
                if (touchInfo.IsActive)
                {
                    UpdatePointerPosition(touchInfo);
                    
                    // 일정 시간 후 페이드아웃
                    float timeSinceTouch = Time.time - touchInfo.LastTouchTime;
                    if (timeSinceTouch > 0.5f)
                    {
                        float alpha = Mathf.Lerp(1f, 0f, (timeSinceTouch - 0.5f) / 0.5f);
                        SetPointerAlpha(touchInfo, alpha);
                        
                        if (timeSinceTouch > 1f)
                        {
                            touchInfo.IsActive = false;
                            touchInfo.PointerObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void SetupCanvas()
        {
            if (touchCanvas == null)
            {
                // 캔버스 생성
                GameObject canvasObject = new GameObject("Touch Display Canvas");
                touchCanvas = canvasObject.AddComponent<Canvas>();
                touchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                // Canvas Scaler 추가
                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                // GraphicRaycaster 추가
                canvasObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void CreateDefaultPointerPrefab()
        {
            // 기본 포인터 프리팹 생성
            touchPointerPrefab = new GameObject("Touch Pointer Prefab");
            
            // 포인터 이미지
            Image pointerImage = touchPointerPrefab.AddComponent<Image>();
            pointerImage.sprite = CreateCircleSprite();
            pointerImage.color = Color.white;
            
            // RectTransform 설정
            RectTransform rectTransform = touchPointerPrefab.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(pointerSize, pointerSize);
            
            // 레이블 추가
            GameObject labelObject = new GameObject("Peer Label");
            labelObject.transform.SetParent(touchPointerPrefab.transform);
            Text label = labelObject.AddComponent<Text>();
            label.text = "Peer";
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 14;
            label.color = Color.white;
            
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0, -pointerSize);
            labelRect.sizeDelta = new Vector2(100, 20);
            
            // Trail Renderer 추가 (옵션)
            if (showTouchTrail)
            {
                TrailRenderer trail = touchPointerPrefab.AddComponent<TrailRenderer>();
                trail.time = 0.5f;
                trail.startWidth = pointerSize * 0.5f;
                trail.endWidth = 0f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            touchPointerPrefab.SetActive(false);
        }

        private Sprite CreateCircleSprite()
        {
            // 간단한 원형 스프라이트 생성
            Texture2D texture = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dx = x - 32;
                    float dy = y - 32;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (distance < 30)
                    {
                        float alpha = 1f - (distance / 30f);
                        pixels[y * 64 + x] = new Color(1, 1, 1, alpha);
                    }
                    else
                    {
                        pixels[y * 64 + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        }

        private void HandleDataChannelMessage(string peerId, string jsonData)
        {
            try
            {
                // 메시지 타입 확인
                var baseMsg = JsonUtility.FromJson<DataChannelMessageBase>(jsonData);
                if (baseMsg?.type == "touch")
                {
                    // 터치 데이터 파싱
                    var touchData = JsonUtility.FromJson<TouchData>(jsonData);
                    ProcessTouchData(peerId, touchData);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[VrMultiTouchReceiver] Failed to parse message from {peerId}: {e.Message}");
            }
        }

        private void ProcessTouchData(string peerId, TouchData touchData)
        {
            // 피어의 터치 정보 가져오기 또는 생성
            if (!peerTouches.TryGetValue(peerId, out var touchInfo))
            {
                touchInfo = CreatePeerTouchInfo(peerId);
                peerTouches[peerId] = touchInfo;
            }

            // 터치 위치 업데이트
            touchInfo.CurrentPosition = new Vector2(touchData.positionX, touchData.positionY);
            touchInfo.LastTouchTime = Time.time;
            touchInfo.IsActive = true;

            // 터치 히스토리 추가 (트레일용)
            touchInfo.TouchHistory.Add(touchInfo.CurrentPosition);
            if (touchInfo.TouchHistory.Count > 50)
            {
                touchInfo.TouchHistory.RemoveAt(0);
            }

            // 포인터 활성화
            if (!touchInfo.PointerObject.activeSelf)
            {
                touchInfo.PointerObject.SetActive(true);
            }

            // 터치 단계에 따른 시각화
            switch (touchData.phase)
            {
                case UnityVerseBridge.Core.DataChannel.Data.TouchPhase.Began:
                    if (animateOnTouch)
                    {
                        AnimateTouchBegan(touchInfo);
                    }
                    break;
                case UnityVerseBridge.Core.DataChannel.Data.TouchPhase.Moved:
                    // 이동 중
                    break;
                case UnityVerseBridge.Core.DataChannel.Data.TouchPhase.Ended:
                case UnityVerseBridge.Core.DataChannel.Data.TouchPhase.Canceled:
                    if (animateOnTouch)
                    {
                        AnimateTouchEnded(touchInfo);
                    }
                    break;
            }
        }

        private PeerTouchInfo CreatePeerTouchInfo(string peerId)
        {
            // 색상 할당
            if (!peerColorMap.ContainsKey(peerId))
            {
                peerColorMap[peerId] = peerColors[nextColorIndex % peerColors.Length];
                nextColorIndex++;
            }

            // 포인터 오브젝트 생성
            GameObject pointerObject = Instantiate(touchPointerPrefab, touchCanvas.transform);
            pointerObject.name = $"Touch_{peerId}";
            pointerObject.SetActive(false);

            var touchInfo = new PeerTouchInfo
            {
                PeerId = peerId,
                PointerObject = pointerObject,
                PointerImage = pointerObject.GetComponent<Image>(),
                PeerLabel = pointerObject.GetComponentInChildren<Text>(),
                Trail = pointerObject.GetComponent<TrailRenderer>()
            };

            // 색상 설정
            Color peerColor = peerColorMap[peerId];
            touchInfo.PointerImage.color = peerColor;
            
            if (touchInfo.Trail != null)
            {
                touchInfo.Trail.startColor = peerColor;
                touchInfo.Trail.endColor = new Color(peerColor.r, peerColor.g, peerColor.b, 0f);
            }

            // 레이블 설정
            if (touchInfo.PeerLabel != null && showPeerLabel)
            {
                touchInfo.PeerLabel.text = $"Player {nextColorIndex}";
                touchInfo.PeerLabel.gameObject.SetActive(true);
            }
            else if (touchInfo.PeerLabel != null)
            {
                touchInfo.PeerLabel.gameObject.SetActive(false);
            }

            return touchInfo;
        }

        private void UpdatePointerPosition(PeerTouchInfo touchInfo)
        {
            if (touchCanvas == null || touchInfo.PointerObject == null) return;

            // 정규화된 좌표를 스크린 좌표로 변환
            Vector2 screenPosition = new Vector2(
                touchInfo.CurrentPosition.x * Screen.width,
                touchInfo.CurrentPosition.y * Screen.height
            );

            // RectTransform 위치 설정
            RectTransform rectTransform = touchInfo.PointerObject.GetComponent<RectTransform>();
            rectTransform.position = screenPosition;
        }

        private void SetPointerAlpha(PeerTouchInfo touchInfo, float alpha)
        {
            if (touchInfo.PointerImage != null)
            {
                Color color = touchInfo.PointerImage.color;
                color.a = alpha;
                touchInfo.PointerImage.color = color;
            }

            if (touchInfo.PeerLabel != null)
            {
                Color labelColor = touchInfo.PeerLabel.color;
                labelColor.a = alpha;
                touchInfo.PeerLabel.color = labelColor;
            }
        }

        private void AnimateTouchBegan(PeerTouchInfo touchInfo)
        {
            if (touchInfo.PointerObject == null) return;

            // 터치 시작 시 확대 애니메이션
            StartCoroutine(AnimateScale(touchInfo.PointerObject, Vector3.one * 1.2f, 0.1f));
        }

        private void AnimateTouchEnded(PeerTouchInfo touchInfo)
        {
            if (touchInfo.PointerObject == null) return;

            // 터치 종료 시 축소 애니메이션
            StartCoroutine(AnimateScale(touchInfo.PointerObject, Vector3.one, 0.1f));
        }

        private IEnumerator AnimateScale(GameObject target, Vector3 targetScale, float duration)
        {
            if (target == null) yield break;
            
            RectTransform rectTransform = target.GetComponent<RectTransform>();
            Vector3 startScale = rectTransform.localScale;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                
                // EaseOutBack 효과 시뮬레이션
                t = 1f - Mathf.Pow(1f - t, 3f);
                
                rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            rectTransform.localScale = targetScale;
        }

        private void HandlePeerDisconnected(string peerId)
        {
            if (peerTouches.TryGetValue(peerId, out var touchInfo))
            {
                if (touchInfo.PointerObject != null)
                {
                    Destroy(touchInfo.PointerObject);
                }
                peerTouches.Remove(peerId);
            }
        }

        // 공개 메서드들
        public void SetPeerColor(string peerId, Color color)
        {
            peerColorMap[peerId] = color;
            
            if (peerTouches.TryGetValue(peerId, out var touchInfo))
            {
                touchInfo.PointerImage.color = color;
                if (touchInfo.Trail != null)
                {
                    touchInfo.Trail.startColor = color;
                    touchInfo.Trail.endColor = new Color(color.r, color.g, color.b, 0f);
                }
            }
        }

        public void ClearAllTouches()
        {
            foreach (var touchInfo in peerTouches.Values)
            {
                touchInfo.IsActive = false;
                touchInfo.PointerObject.SetActive(false);
            }
        }

        public Dictionary<string, Vector2> GetActiveTouches()
        {
            var activeTouches = new Dictionary<string, Vector2>();
            foreach (var kvp in peerTouches)
            {
                if (kvp.Value.IsActive)
                {
                    activeTouches[kvp.Key] = kvp.Value.CurrentPosition;
                }
            }
            return activeTouches;
        }
    }
}