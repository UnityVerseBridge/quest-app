using UnityEngine;
using UnityVerseBridge.Core;
using Unity.WebRTC;
using System.Collections;

public class QuestAppInitializer : MonoBehaviour
{
    [SerializeField] private UnityVerseBridgeManager bridgeManager;
    
    void Awake()
    {
        // Force Quest platform detection
        if (bridgeManager == null)
        {
            bridgeManager = GetComponent<UnityVerseBridgeManager>();
            if (bridgeManager == null)
            {
                bridgeManager = FindObjectOfType<UnityVerseBridgeManager>();
            }
        }
        
        // Wait for Unity to properly initialize XR
        StartCoroutine(ConfigureForQuest());
    }
    
    IEnumerator ConfigureForQuest()
    {
        // Wait for XR initialization
        yield return new WaitForSeconds(0.5f);
        
        if (bridgeManager != null && bridgeManager.ConnectionConfig != null)
        {
            // Force Quest configuration
            bridgeManager.ConnectionConfig.clientType = ClientType.Quest;
            
            Debug.Log("[QuestAppInitializer] Forced clientType to Quest");
            
            // Check if VR is properly detected
            bool isVR = false;
            
#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android device, check for Quest
            if (UnityEngine.XR.XRSettings.enabled)
            {
                isVR = true;
                Debug.Log($"[QuestAppInitializer] XR Enabled: {UnityEngine.XR.XRSettings.loadedDeviceName}");
            }
#else
            // In Editor, force Quest mode for testing
            isVR = true;
            Debug.Log("[QuestAppInitializer] Editor mode - forcing Quest configuration");
#endif
            
            if (isVR)
            {
                Debug.Log("[QuestAppInitializer] VR mode confirmed, Quest app ready");
            }
            else
            {
                Debug.LogWarning("[QuestAppInitializer] VR not detected, but proceeding with Quest configuration");
            }
        }
        else
        {
            Debug.LogError("[QuestAppInitializer] UnityVerseBridgeManager or ConnectionConfig not found!");
        }
    }
}
