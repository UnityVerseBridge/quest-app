using UnityEngine;
using UnityEngine.Rendering;

namespace UnityVerseBridge.QuestApp
{
    /// <summary>
    /// Quest 디바이스에서 RenderTexture 호환성을 보장하는 헬퍼 클래스
    /// </summary>
    public static class QuestRenderTextureHelper
    {
        /// <summary>
        /// Quest 디바이스에 최적화된 RenderTexture를 생성합니다.
        /// </summary>
        public static RenderTexture CreateCompatibleRenderTexture(int width, int height, string name = "StreamTexture")
        {
            // Quest에서 지원하는 포맷 결정
            RenderTextureFormat format = RenderTextureFormat.BGRA32;
            
            // Vulkan API 사용 시 포맷 조정
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
            {
                // Vulkan에서 BGRA32가 지원되지 않을 수 있음
                if (!SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.BGRA32))
                {
                    Debug.LogWarning($"[QuestRenderTextureHelper] BGRA32 not supported on Vulkan, trying ARGB32");
                    format = RenderTextureFormat.ARGB32;
                }
            }
            
            // RenderTexture 생성
            var rt = new RenderTexture(width, height, 0, format)
            {
                name = name,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1,
                depthBufferBits = 0,
                enableRandomWrite = false,
                useDynamicScale = false,
                vrUsage = VRTextureUsage.None, // VR 용도로 사용하지 않음 (스트리밍용)
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            
            // 즉시 생성
            if (!rt.Create())
            {
                Debug.LogError($"[QuestRenderTextureHelper] Failed to create RenderTexture");
                return null;
            }
            
            // Linear 색상 공간 확인
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                rt.sRGB = false; // Linear space에서는 sRGB 변환 비활성화
            }
            
            Debug.Log($"[QuestRenderTextureHelper] Created RenderTexture: {name}, Size: {width}x{height}, Format: {format}, GraphicsAPI: {SystemInfo.graphicsDeviceType}");
            
            return rt;
        }
        
        /// <summary>
        /// 기존 RenderTexture가 Quest에서 호환되는지 확인하고 필요시 재생성
        /// </summary>
        public static RenderTexture EnsureCompatibility(RenderTexture existing, int width, int height)
        {
            if (existing == null)
            {
                return CreateCompatibleRenderTexture(width, height);
            }
            
            // 포맷 호환성 확인
            bool needsRecreation = false;
            
            if (!existing.IsCreated())
            {
                Debug.Log($"[QuestRenderTextureHelper] RenderTexture not created, creating...");
                needsRecreation = true;
            }
            else if (existing.width != width || existing.height != height)
            {
                Debug.Log($"[QuestRenderTextureHelper] Size mismatch, recreating...");
                needsRecreation = true;
            }
            else if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan && 
                     existing.format == RenderTextureFormat.BGRA32 && 
                     !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.BGRA32))
            {
                Debug.Log($"[QuestRenderTextureHelper] Format incompatible with Vulkan, recreating...");
                needsRecreation = true;
            }
            
            if (needsRecreation)
            {
                if (existing.IsCreated())
                {
                    existing.Release();
                }
                Object.Destroy(existing);
                return CreateCompatibleRenderTexture(width, height, existing.name);
            }
            
            return existing;
        }
    }
}