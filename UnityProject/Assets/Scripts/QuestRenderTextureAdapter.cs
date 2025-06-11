using UnityEngine;
using UnityVerseBridge.Core.Utils;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Example adapter showing how to use the core RenderTextureHelper in Quest apps
    /// </summary>
    public static class QuestRenderTextureAdapter
    {
        /// <summary>
        /// Creates a Quest-optimized RenderTexture using the core helper
        /// </summary>
        public static RenderTexture CreateQuestStreamingTexture(int width, int height)
        {
            // Use the core RenderTextureHelper
            var rt = RenderTextureHelper.CreateForWebRTCStreaming(width, height);
            
            if (rt != null)
            {
                Debug.Log($"[QuestRenderTextureAdapter] Created Quest streaming texture: {width}x{height}");
            }
            
            return rt;
        }
        
        /// <summary>
        /// Ensures a RenderTexture is compatible with Quest hardware
        /// </summary>
        public static RenderTexture EnsureQuestCompatibility(RenderTexture existing, int width, int height)
        {
            // Use the core helper with Quest-specific requirements
            return RenderTextureHelper.EnsureCompatibility(existing, width, height, requireDepthBuffer: true);
        }
    }
}