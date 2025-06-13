using UnityEngine;
using UnityVerseBridge.Core;
using System.Collections.Generic;

namespace UnityVerse.QuestApp
{
    /// <summary>
    /// Touch 시각화를 중앙에서 관리하는 매니저 (UIManager와 유사한 패턴)
    /// </summary>
    public class TouchVisualizationManager : MonoBehaviour
    {
        private static TouchVisualizationManager _instance;
        public static TouchVisualizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<TouchVisualizationManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("TouchVisualizationManager");
                        _instance = go.AddComponent<TouchVisualizationManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        [Header("Visualization Settings")]
        [SerializeField] private bool enableTouchVisualization = true;
        [SerializeField] private VisualizationMode visualizationMode = VisualizationMode.Canvas;
        
        [Header("Visual Settings")]
        [SerializeField] private Color touchColor = Color.red;
        [SerializeField] private float touchSize = 0.05f;
        [SerializeField] private bool showCoordinates = true;
        [SerializeField] private bool showDebugInfo = false;
        
        public enum VisualizationMode
        {
            OnGUI,      // SimpleTouchVisualizer 스타일
            GameObject, // DualTouchVisualizer 스타일
            Canvas,     // CanvasTouchVisualizer 스타일 (2D UI)
            Both        // OnGUI + Canvas
        }
        
        // Properties
        public bool EnableTouchVisualization 
        { 
            get => enableTouchVisualization; 
            set
            {
                enableTouchVisualization = value;
                UpdateVisualizerStates();
            }
        }
        
        public VisualizationMode Mode
        {
            get => visualizationMode;
            set
            {
                visualizationMode = value;
                UpdateVisualizerStates();
            }
        }
        
        // Touch visualizers
        private SimpleTouchVisualizer simpleVisualizer;
        private DualTouchVisualizer dualVisualizer;
        private CanvasTouchVisualizer canvasVisualizer;
        
        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        void Start()
        {
            InitializeVisualizers();
        }
        
        private void InitializeVisualizers()
        {
            // Only create Canvas visualizer - others are deprecated
            if (visualizationMode == VisualizationMode.Canvas || visualizationMode == VisualizationMode.Both)
            {
                canvasVisualizer = FindFirstObjectByType<CanvasTouchVisualizer>();
                if (canvasVisualizer == null)
                {
                    GameObject go = new GameObject("CanvasTouchVisualizer");
                    canvasVisualizer = go.AddComponent<CanvasTouchVisualizer>();
                }
            }
            
            // Find existing deprecated visualizers but don't create new ones
            simpleVisualizer = FindFirstObjectByType<SimpleTouchVisualizer>();
            dualVisualizer = FindFirstObjectByType<DualTouchVisualizer>();
            
            UpdateVisualizerStates();
        }
        
        private void UpdateVisualizerStates()
        {
            // Update SimpleTouchVisualizer
            if (simpleVisualizer != null)
            {
                bool shouldEnable = enableTouchVisualization && 
                    (visualizationMode == VisualizationMode.OnGUI || visualizationMode == VisualizationMode.Both);
                simpleVisualizer.enabled = shouldEnable;
                
                if (shouldEnable)
                {
                    simpleVisualizer.EnableVisualization = true;
                }
            }
            
            // Update DualTouchVisualizer
            if (dualVisualizer != null)
            {
                bool shouldEnable = enableTouchVisualization && 
                    visualizationMode == VisualizationMode.GameObject;
                dualVisualizer.enabled = shouldEnable;
            }
            
            // Update CanvasTouchVisualizer
            if (canvasVisualizer != null)
            {
                bool shouldEnable = enableTouchVisualization && 
                    (visualizationMode == VisualizationMode.Canvas || visualizationMode == VisualizationMode.Both);
                canvasVisualizer.enabled = shouldEnable;
            }
        }
        
        // Public methods for runtime control
        public void ShowTouchVisualization()
        {
            EnableTouchVisualization = true;
        }
        
        public void HideTouchVisualization()
        {
            EnableTouchVisualization = false;
        }
        
        public void ToggleTouchVisualization()
        {
            EnableTouchVisualization = !EnableTouchVisualization;
        }
        
        public void SetVisualizationMode(VisualizationMode mode)
        {
            Mode = mode;
        }
        
        // Settings
        public void SetTouchColor(Color color)
        {
            touchColor = color;
            // Apply to visualizers if needed
        }
        
        public void SetTouchSize(float size)
        {
            touchSize = size;
            // Apply to visualizers if needed
        }
        
        public void SetShowCoordinates(bool show)
        {
            showCoordinates = show;
            // Apply to visualizers if needed
        }
        
        public void SetShowDebugInfo(bool show)
        {
            showDebugInfo = show;
            // Apply to visualizers if needed
        }
    }
}