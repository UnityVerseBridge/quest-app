using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// VR 환경에서 터치 입력을 받아 처리하는 간단한 클릭 핸들러
    /// 3D 오브젝트에 부착하여 사용
    /// </summary>
    public class VRClickHandler : MonoBehaviour
    {
        [Header("Click Events")]
        [SerializeField] private UnityEvent onVRClick;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool changeColorOnClick = true;
        [SerializeField] private Color clickColor = Color.red;
        [SerializeField] private float colorResetDelay = 0.5f;
        
        private Renderer objectRenderer;
        private Color originalColor;
        
        void Start()
        {
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null && objectRenderer.material != null)
            {
                originalColor = objectRenderer.material.color;
            }
            
            // 콜라이더가 없으면 추가
            if (GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[VRClickHandler] No collider found on {gameObject.name}. Adding BoxCollider.");
                gameObject.AddComponent<BoxCollider>();
            }
        }
        
        /// <summary>
        /// VrTouchReceiver에서 SendMessage로 호출됨
        /// </summary>
        public void OnVRClick()
        {
            Debug.Log($"[VRClickHandler] {gameObject.name} clicked!");
            
            // 이벤트 실행
            onVRClick?.Invoke();
            
            // 시각적 피드백
            if (changeColorOnClick && objectRenderer != null)
            {
                objectRenderer.material.color = clickColor;
                Invoke(nameof(ResetColor), colorResetDelay);
            }
            
            // 간단한 애니메이션 효과
            StartCoroutine(ScaleAnimation());
        }
        
        private void ResetColor()
        {
            if (objectRenderer != null)
            {
                objectRenderer.material.color = originalColor;
            }
        }
        
        /// <summary>
        /// 테스트용 메서드들
        /// </summary>
        public void TestMethod1()
        {
            Debug.Log("[VRClickHandler] Test Method 1 called!");
        }
        
        public void TestMethod2()
        {
            Debug.Log("[VRClickHandler] Test Method 2 called!");
        }
        
        public void ToggleObject(GameObject target)
        {
            if (target != null)
            {
                target.SetActive(!target.activeSelf);
                Debug.Log($"[VRClickHandler] Toggled {target.name} to {target.activeSelf}");
            }
        }
        
        private IEnumerator ScaleAnimation()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;
            
            // Scale up
            float elapsed = 0f;
            float duration = 0.15f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }
            
            // Scale down with bounce
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
    }
}