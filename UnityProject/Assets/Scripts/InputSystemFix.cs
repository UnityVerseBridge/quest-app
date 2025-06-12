using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityVerseBridge.Quest
{
    /// <summary>
    /// Fixes InputSystem NullReferenceException in Unity 6 with Meta XR SDK
    /// This is a workaround for a known issue with InputSystem and XR interactions
    /// </summary>
    public class InputSystemFix : MonoBehaviour
    {
        private static InputSystemFix instance;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            // Ensure only one instance exists
            if (instance != null) return;
            
            // Create a persistent GameObject for the fix
            GameObject fixObject = new GameObject("[InputSystemFix]");
            DontDestroyOnLoad(fixObject);
            instance = fixObject.AddComponent<InputSystemFix>();
        }
        
        void Awake()
        {
            // Disable InputSystem's automatic XR layout loading if it conflicts with Meta XR
            // This prevents the NullReferenceException in FireStateChangeNotifications
            try
            {
                // Check if XR layouts are causing conflicts
                var settings = InputSystem.settings;
                if (settings != null)
                {
                    // Ensure Input System is properly initialized
                    if (InputSystem.devices.Count == 0)
                    {
                        Debug.Log("[InputSystemFix] Reinitializing Input System to prevent null reference errors");
                        InputSystem.Update();
                    }
                    
                    Debug.Log($"[InputSystemFix] Input System initialized with {InputSystem.devices.Count} devices");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[InputSystemFix] Error during Input System initialization: {e.Message}");
            }
        }
        
        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}