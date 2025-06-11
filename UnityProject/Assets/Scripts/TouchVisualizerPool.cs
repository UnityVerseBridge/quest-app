using UnityEngine;
using UnityEngine.UI;
using UnityVerseBridge.Core.Utils;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Pooled touch visualizer for VrMultiTouchReceiver
    /// Improves performance by reusing touch pointer objects
    /// </summary>
    public class TouchVisualizerPool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private GameObject touchPointerPrefab;
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private int maxPoolSize = 50;
        
        [Header("Visualization Settings")]
        [SerializeField] private float pointerSize = 50f;
        [SerializeField] private bool showTrail = true;
        [SerializeField] private float trailTime = 0.5f;
        
        private ObjectPool<TouchPointer> pointerPool;
        private Transform poolParent;
        
        public class TouchPointer : MonoBehaviour
        {
            public Image pointerImage;
            public Text peerLabel;
            public TrailRenderer trail;
            public RectTransform rectTransform;
            
            public void Initialize()
            {
                pointerImage = GetComponent<Image>();
                peerLabel = GetComponentInChildren<Text>();
                trail = GetComponent<TrailRenderer>();
                rectTransform = GetComponent<RectTransform>();
            }
            
            public void SetColor(Color color)
            {
                if (pointerImage != null)
                    pointerImage.color = color;
                    
                if (trail != null)
                {
                    trail.startColor = color;
                    trail.endColor = new Color(color.r, color.g, color.b, 0f);
                }
            }
            
            public void SetLabel(string label)
            {
                if (peerLabel != null)
                {
                    peerLabel.text = label;
                    peerLabel.gameObject.SetActive(!string.IsNullOrEmpty(label));
                }
            }
            
            public void Reset()
            {
                if (trail != null)
                    trail.Clear();
                    
                transform.localScale = Vector3.one;
                SetLabel("");
            }
        }
        
        void Awake()
        {
            // Create pool parent
            poolParent = new GameObject("TouchPointerPool").transform;
            poolParent.SetParent(transform);
            poolParent.gameObject.SetActive(false);
            
            // Ensure prefab exists
            if (touchPointerPrefab == null)
            {
                touchPointerPrefab = CreateDefaultPointerPrefab();
            }
            
            // Initialize pool
            pointerPool = new ObjectPool<TouchPointer>(
                createFunc: CreatePointer,
                onGet: OnGetPointer,
                onReturn: OnReturnPointer,
                initialSize: initialPoolSize,
                maxSize: maxPoolSize
            );
        }
        
        public TouchPointer GetPointer()
        {
            return pointerPool.Get();
        }
        
        public void ReturnPointer(TouchPointer pointer)
        {
            pointerPool.Return(pointer);
        }
        
        private TouchPointer CreatePointer()
        {
            var go = Instantiate(touchPointerPrefab, poolParent);
            var pointer = go.GetComponent<TouchPointer>();
            
            if (pointer == null)
            {
                pointer = go.AddComponent<TouchPointer>();
            }
            
            pointer.Initialize();
            
            // Set size
            if (pointer.rectTransform != null)
            {
                pointer.rectTransform.sizeDelta = new Vector2(pointerSize, pointerSize);
            }
            
            // Configure trail
            if (showTrail && pointer.trail != null)
            {
                pointer.trail.time = trailTime;
                pointer.trail.enabled = true;
            }
            
            return pointer;
        }
        
        private void OnGetPointer(TouchPointer pointer)
        {
            pointer.Reset();
        }
        
        private void OnReturnPointer(TouchPointer pointer)
        {
            pointer.Reset();
        }
        
        private GameObject CreateDefaultPointerPrefab()
        {
            var prefab = new GameObject("TouchPointerPrefab");
            
            // Add components
            var image = prefab.AddComponent<Image>();
            image.sprite = CreateCircleSprite();
            
            var pointer = prefab.AddComponent<TouchPointer>();
            
            // Add label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(prefab.transform);
            var label = labelGO.AddComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 14;
            
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchoredPosition = new Vector2(0, -pointerSize);
            labelRect.sizeDelta = new Vector2(100, 20);
            
            // Add trail if needed
            if (showTrail)
            {
                var trail = prefab.AddComponent<TrailRenderer>();
                trail.time = trailTime;
                trail.startWidth = pointerSize * 0.5f;
                trail.endWidth = 0f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            prefab.SetActive(false);
            return prefab;
        }
        
        private Sprite CreateCircleSprite()
        {
            var texture = new Texture2D(64, 64);
            var pixels = new Color[64 * 64];
            
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
        
        void OnDestroy()
        {
            pointerPool?.Clear();
        }
    }
}