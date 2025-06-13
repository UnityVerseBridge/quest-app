using UnityEngine;
using UnityEngine.UI;
using UnityVerseBridge.Core;
using UnityVerseBridge.Core.DataChannel.Data;
using System.Collections;
using System.Collections.Generic;
using TouchPhase = UnityVerseBridge.Core.DataChannel.Data.TouchPhase;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// 간단하고 확실한 터치 시각화 시스템
    /// </summary>
    [DefaultExecutionOrder(1000)] // TouchInputHandler 이후에 실행
    public class SimpleTouchVisualizationSystem : MonoBehaviour
    {
        private Canvas touchCanvas;
        private Dictionary<int, GameObject> activeTouches = new Dictionary<int, GameObject>();
        private Dictionary<int, float> touchStartTimes = new Dictionary<int, float>();
        private WebRtcManager webRtcManager;
        private const float TOUCH_TIMEOUT = 5f; // Remove touches after 5 seconds if no end event
        
        void Awake()
        {
            Debug.Log("[SimpleTouchVisualizationSystem] Awake called");
        }
        
        void Start()
        {
            Debug.Log("[SimpleTouchVisualizationSystem] Start called");
            
            // Canvas 생성
            CreateCanvas();
            
            // WebRtcManager 연결
            StartCoroutine(ConnectToWebRtc());
            
            // 기존 쓰레기들 제거
            StartCoroutine(CleanupGarbage());
            
            // QuestTouchExtension 비활성화
            DisableQuestTouchExtension();
        }
        
        void DisableQuestTouchExtension()
        {
            var questTouchExt = FindFirstObjectByType<UnityVerseBridge.Core.Extensions.Quest.QuestTouchExtension>();
            if (questTouchExt != null)
            {
                Debug.Log("[SimpleTouchVisualizationSystem] Disabling QuestTouchExtension");
                questTouchExt.enabled = false;
                
                // Canvas가 있으면 제거
                var canvas = questTouchExt.GetComponentInChildren<Canvas>();
                if (canvas != null)
                {
                    Destroy(canvas.gameObject);
                }
            }
        }
        
        void CreateCanvas()
        {
            // 기존 TouchVisualizationCanvas가 있으면 사용
            GameObject existingCanvas = GameObject.Find("TouchVisualizationCanvas");
            if (existingCanvas != null)
            {
                Destroy(existingCanvas);
            }
            
            // 새 Canvas 생성
            GameObject canvasGO = new GameObject("TouchVisualizationCanvas");
            touchCanvas = canvasGO.AddComponent<Canvas>();
            touchCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            touchCanvas.sortingOrder = 999;
            
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // 배경 테스트용 이미지 추가
            GameObject bgGO = new GameObject("DebugBackground");
            bgGO.transform.SetParent(touchCanvas.transform, false);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.1f); // 반투명 검정
            RectTransform bgRect = bg.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            
            // 시작 시 숨김
            canvasGO.SetActive(false);
            
            Debug.Log($"[SimpleTouchVisualizationSystem] Canvas created at {canvasGO.transform.position}, layer: {canvasGO.layer}");
        }
        
        IEnumerator ConnectToWebRtc()
        {
            Debug.Log("[SimpleTouchVisualizationSystem] Starting WebRTC connection...");
            
            int attempts = 0;
            while (webRtcManager == null && attempts < 50)
            {
                webRtcManager = FindFirstObjectByType<WebRtcManager>();
                if (webRtcManager != null)
                {
                    webRtcManager.OnDataChannelMessageReceived += OnTouchDataReceived;
                    Debug.Log($"[SimpleTouchVisualizationSystem] Successfully connected to WebRTC after {attempts} attempts");
                    
                    // 현재 이벤트 리스너 수 확인
                    var field = typeof(WebRtcManager).GetField("OnDataChannelMessageReceived", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var eventDelegate = field.GetValue(webRtcManager) as System.Delegate;
                        if (eventDelegate != null)
                        {
                            Debug.Log($"[SimpleTouchVisualizationSystem] Total event listeners: {eventDelegate.GetInvocationList().Length}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"[SimpleTouchVisualizationSystem] WebRtcManager not found, attempt {attempts}");
                }
                attempts++;
                yield return new WaitForSeconds(0.1f);
            }
            
            if (webRtcManager == null)
            {
                Debug.LogError("[SimpleTouchVisualizationSystem] Failed to connect to WebRTC!");
            }
        }
        
        void OnTouchDataReceived(string json)
        {
            Debug.Log($"[SimpleTouchVisualizationSystem] Raw message received: {json}");
            
            if (!json.Contains("\"type\":\"touch\"")) 
            {
                Debug.Log("[SimpleTouchVisualizationSystem] Not a touch message, ignoring");
                return;
            }
            
            try
            {
                var touchData = JsonUtility.FromJson<TouchData>(json);
                
                // Core TouchPhase enum values: Began=0, Moved=1, Ended=2, Canceled=3
                int phaseValue = (int)touchData.phase;
                
                string phaseName = phaseValue switch
                {
                    0 => "Began",
                    1 => "Moved",
                    2 => "Ended",
                    3 => "Canceled",
                    _ => $"Unknown({phaseValue})"
                };
                
                Debug.Log($"[SimpleTouchVisualizationSystem] Touch parsed: ID={touchData.touchId}, Phase={phaseName}(value={phaseValue}), Pos=({touchData.positionX:F3}, {touchData.positionY:F3})");
                
                // Check if this is an end phase
                // TouchPhase.Ended = 2, TouchPhase.Canceled = 3
                bool isEndPhase = phaseValue == 2 || phaseValue == 3;
                
                if (isEndPhase)
                {
                    Debug.Log($"[SimpleTouchVisualizationSystem] Removing touch {touchData.touchId} due to phase: {phaseName} (value={phaseValue})");
                    RemoveTouch(touchData.touchId);
                }
                else
                {
                    // Began (0) or Moved (1)
                    UpdateTouch(touchData);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SimpleTouchVisualizationSystem] Error parsing touch data: {e.Message}");
                Debug.LogError($"[SimpleTouchVisualizationSystem] JSON was: {json}");
                Debug.LogException(e);
            }
        }
        
        void UpdateTouch(TouchData touchData)
        {
            Debug.Log($"[SimpleTouchVisualizationSystem] UpdateTouch called for ID: {touchData.touchId}");
            
            // Canvas 확인
            if (touchCanvas == null || touchCanvas.gameObject == null)
            {
                Debug.LogError("[SimpleTouchVisualizationSystem] TouchCanvas is null!");
                CreateCanvas();
            }
            
            if (!activeTouches.ContainsKey(touchData.touchId))
            {
                // Began이 아닌데 새 터치가 들어왔다면 Began으로 간주
                Debug.Log($"[SimpleTouchVisualizationSystem] Creating new touch indicator for ID: {touchData.touchId} (Phase: {touchData.phase})");
                
                // 새 터치 표시 생성
                GameObject touchGO = new GameObject($"Touch_{touchData.touchId}");
                touchGO.transform.SetParent(touchCanvas.transform, false);
                
                Image img = touchGO.AddComponent<Image>();
                img.color = Color.red;
                img.rectTransform.sizeDelta = new Vector2(50, 50);
                
                // 간단한 원 텍스처
                Texture2D tex = new Texture2D(32, 32);
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16));
                        tex.SetPixel(x, y, dist < 15 ? Color.white : Color.clear);
                    }
                }
                tex.Apply();
                img.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
                
                activeTouches[touchData.touchId] = touchGO;
                touchStartTimes[touchData.touchId] = Time.time;
                
                Debug.Log($"[SimpleTouchVisualizationSystem] Touch indicator created. Total touches: {activeTouches.Count}");
                
                // 첫 터치 시 Canvas 표시
                if (activeTouches.Count == 1)
                {
                    touchCanvas.gameObject.SetActive(true);
                    Debug.Log($"[SimpleTouchVisualizationSystem] Canvas shown. Active: {touchCanvas.gameObject.activeSelf}");
                    
                    // 강제로 다시 활성화
                    StartCoroutine(ForceShowCanvas());
                }
            }
            
            // 위치 업데이트
            GameObject touch = activeTouches[touchData.touchId];
            Vector2 screenPos = new Vector2(
                touchData.positionX * 1280f,
                touchData.positionY * 720f
            );
            
            // 스케일 조정
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            screenPos *= scale;
            screenPos += new Vector2(
                (Screen.width - 1280f * scale) / 2f,
                (Screen.height - 720f * scale) / 2f
            );
            
            touch.transform.position = screenPos;
            
            Debug.Log($"[SimpleTouchVisualizationSystem] Touch {touchData.touchId} position updated to: {screenPos}, Canvas active: {touchCanvas.gameObject.activeSelf}");
        }
        
        void RemoveTouch(int touchId)
        {
            Debug.Log($"[SimpleTouchVisualizationSystem] RemoveTouch called for ID: {touchId}");
            
            if (activeTouches.ContainsKey(touchId))
            {
                GameObject touchObj = activeTouches[touchId];
                if (touchObj != null)
                {
                    Debug.Log($"[SimpleTouchVisualizationSystem] Destroying touch object: {touchObj.name}");
                    Destroy(touchObj);
                }
                activeTouches.Remove(touchId);
                touchStartTimes.Remove(touchId);
                
                Debug.Log($"[SimpleTouchVisualizationSystem] Touch removed: {touchId}, remaining: {activeTouches.Count}");
                
                // 모든 터치가 끝나면 Canvas 숨김
                if (activeTouches.Count == 0 && touchCanvas != null && touchCanvas.gameObject != null)
                {
                    touchCanvas.gameObject.SetActive(false);
                    Debug.Log("[SimpleTouchVisualizationSystem] Canvas hidden - no more active touches");
                }
            }
            else
            {
                Debug.LogWarning($"[SimpleTouchVisualizationSystem] Tried to remove non-existent touch: {touchId}");
            }
        }
        
        IEnumerator CleanupGarbage()
        {
            while (true)
            {
                // TouchCanvas 제거
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
                foreach (var canvas in canvases)
                {
                    if (canvas != touchCanvas && 
                        (canvas.name == "TouchCanvas" || canvas.name == "Touch Canvas"))
                    {
                        Debug.LogWarning($"[SimpleTouchVisualizationSystem] Destroying garbage: {canvas.name}");
                        DestroyImmediate(canvas.gameObject);
                    }
                }
                
                // TouchVisualizer 제거
                GameObject[] objects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var obj in objects)
                {
                    if (obj.name.Contains("TouchVisualizer_"))
                    {
                        Debug.LogWarning($"[SimpleTouchVisualizationSystem] Destroying garbage: {obj.name}");
                        DestroyImmediate(obj);
                    }
                }
                
                // TouchVisualizationCanvas 상태 확인 - 터치가 없으면 강제로 비활성화
                if (touchCanvas != null && touchCanvas.gameObject != null)
                {
                    if (activeTouches.Count == 0 && touchCanvas.gameObject.activeSelf)
                    {
                        Debug.LogWarning("[SimpleTouchVisualizationSystem] Force hiding TouchVisualizationCanvas - no active touches");
                        touchCanvas.gameObject.SetActive(false);
                    }
                    else if (activeTouches.Count > 0 && !touchCanvas.gameObject.activeSelf)
                    {
                        Debug.LogWarning("[SimpleTouchVisualizationSystem] Force showing TouchVisualizationCanvas - has active touches");
                        touchCanvas.gameObject.SetActive(true);
                    }
                }
                
                // Clean up stale touches that didn't receive end events
                var staleTouches = new List<int>();
                foreach (var kvp in touchStartTimes)
                {
                    if (Time.time - kvp.Value > TOUCH_TIMEOUT)
                    {
                        staleTouches.Add(kvp.Key);
                    }
                }
                
                foreach (int touchId in staleTouches)
                {
                    Debug.LogWarning($"[SimpleTouchVisualizationSystem] Removing stale touch {touchId} after timeout");
                    RemoveTouch(touchId);
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        IEnumerator ForceShowCanvas()
        {
            yield return null;
            if (touchCanvas != null && !touchCanvas.gameObject.activeSelf && activeTouches.Count > 0)
            {
                Debug.LogWarning("[SimpleTouchVisualizationSystem] Force showing canvas!");
                touchCanvas.gameObject.SetActive(true);
            }
        }
        
        void OnDestroy()
        {
            if (webRtcManager != null)
            {
                webRtcManager.OnDataChannelMessageReceived -= OnTouchDataReceived;
            }
        }
    }
}