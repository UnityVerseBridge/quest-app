# URP Missing Types 경고 해결 방법

## 문제
```
Missing types referenced from component UniversalRenderPipelineGlobalSettings on game object UniversalRenderPipelineGlobalSettings:
    UnityEngine.Rendering.LightmapSamplingSettings, Unity.RenderPipelines.Core.Runtime (1 object)
    UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusionDynamicResources, Unity.RenderPipelines.Universal.Runtime (1 object)
    UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusionPersistentResources, Unity.RenderPipelines.Universal.Runtime (1 object)
    UnityEngine.Rendering.VrsRenderPipelineRuntimeResources, Unity.RenderPipelines.Core.Runtime (1 object)
```

## 해결 방법

### 옵션 1: URP 패키지 업데이트 (권장)
1. Window > Package Manager 열기
2. Packages: In Project 선택
3. Universal RP 패키지 찾기
4. 최신 버전으로 업데이트 (17.0.3 이상)

### 옵션 2: Global Settings 재생성
1. Edit > Project Settings > Graphics
2. UniversalRenderPipelineGlobalSettings 찾기
3. 기존 설정 삭제 후 새로 생성

### 옵션 3: 경고 무시
이 경고는 기능에 영향을 주지 않으므로 무시해도 됩니다.
URP의 새 기능들을 사용하지 않는다면 문제없습니다.

## 참고
- Unity 6에서 URP가 업데이트되면서 발생하는 호환성 경고
- 실제 렌더링이나 스트리밍에는 영향 없음