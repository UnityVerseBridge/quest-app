using UnityEngine;
using System.Reflection;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Debug helper to check platform detection
    /// </summary>
    public class PlatformDebugger : MonoBehaviour
    {
        void Start()
        {
            Debug.Log($"[PlatformDebugger] === Platform Detection Debug ===");
            Debug.Log($"[PlatformDebugger] Application.platform: {Application.platform}");
            Debug.Log($"[PlatformDebugger] RuntimePlatform is Android: {Application.platform == RuntimePlatform.Android}");
            Debug.Log($"[PlatformDebugger] RuntimePlatform is Editor: {Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor}");
            
            // Check XR settings
            Debug.Log($"[PlatformDebugger] XRSettings.enabled: {UnityEngine.XR.XRSettings.enabled}");
            Debug.Log($"[PlatformDebugger] XRSettings.isDeviceActive: {UnityEngine.XR.XRSettings.isDeviceActive}");
            Debug.Log($"[PlatformDebugger] XRSettings.loadedDeviceName: {UnityEngine.XR.XRSettings.loadedDeviceName}");
            
#if UNITY_XR_MANAGEMENT
            Debug.Log("[PlatformDebugger] UNITY_XR_MANAGEMENT is defined");
            var xrSettings = UnityEngine.XR.Management.XRGeneralSettings.Instance;
            if (xrSettings != null)
            {
                Debug.Log($"[PlatformDebugger] XRGeneralSettings.Instance exists");
                if (xrSettings.Manager != null)
                {
                    Debug.Log($"[PlatformDebugger] XRManagerSettings exists");
                    if (xrSettings.Manager.activeLoader != null)
                    {
                        Debug.Log($"[PlatformDebugger] Active XR Loader: {xrSettings.Manager.activeLoader.GetType().Name}");
                    }
                    else
                    {
                        Debug.Log("[PlatformDebugger] No active XR loader");
                    }
                }
                else
                {
                    Debug.Log("[PlatformDebugger] XRManagerSettings is null");
                }
            }
            else
            {
                Debug.Log("[PlatformDebugger] XRGeneralSettings.Instance is null");
            }
#else
            Debug.Log("[PlatformDebugger] UNITY_XR_MANAGEMENT is NOT defined");
#endif
            
            // Check for OVR components
            try
            {
                var ovrManagerType = System.Type.GetType("OVRManager, Oculus.VR");
                if (ovrManagerType != null)
                {
                    Debug.Log("[PlatformDebugger] OVRManager type found");
                    var instanceProperty = ovrManagerType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceProperty != null)
                    {
                        var instance = instanceProperty.GetValue(null);
                        Debug.Log($"[PlatformDebugger] OVRManager.instance: {(instance != null ? "exists" : "null")}");
                    }
                }
                else
                {
                    Debug.Log("[PlatformDebugger] OVRManager type NOT found");
                }
                
                // Check for OVRCameraRig
                var cameraRig = FindFirstObjectByType(System.Type.GetType("OVRCameraRig, Oculus.VR"));
                Debug.Log($"[PlatformDebugger] OVRCameraRig in scene: {(cameraRig != null ? "found" : "not found")}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlatformDebugger] Error checking OVR components: {e.Message}");
            }
            
            Debug.Log($"[PlatformDebugger] === End Platform Detection Debug ===");
        }
    }
}