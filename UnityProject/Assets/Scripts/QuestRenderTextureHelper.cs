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
            
            // RenderTexture 생성 - URP는 depth buffer가 필요함
            var rt = new RenderTexture(width, height, 24, format) // 24-bit depth buffer 추가
            {
                name = name,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1,
                enableRandomWrite = false,
                useDynamicScale = false,
                vrUsage = VRTextureUsage.None, // VR 용도로 사용하지 않음 (스트리밍용)
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt // URP 요구사항
            };
            
            // 즉시 생성
            if (!rt.Create())
            {
                Debug.LogError($"[QuestRenderTextureHelper] Failed to create RenderTexture");
                return null;
            }
            
            // 색상 공간 정보 로그 (sRGB는 읽기 전용 속성)
            if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            {
                Debug.Log($"[QuestRenderTextureHelper] Using Linear color space, sRGB: {rt.sRGB}");
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
            else if (existing.depth == 0) // URP requires depth buffer
            {
                Debug.Log($"[QuestRenderTextureHelper] No depth buffer, recreating for URP compatibility...");
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