# Unity Editor에서 XR 없이 WebRTC 테스트하기

## 개요
Unity Editor에서 XR 세션이 즉시 종료되는 문제를 해결하고, WebRTC 스트리밍 기능만 테스트할 수 있도록 설정하는 가이드입니다.

## 해결 방법

### 1. EditorXRDisabler 컴포넌트 사용
씬에 빈 GameObject를 생성하고 `EditorXRDisabler` 컴포넌트를 추가합니다.

```
GameObject > Create Empty > "Editor Test Manager"
Add Component > EditorXRDisabler
```

설정 옵션:
- **Disable XR In Editor**: Unity Editor에서 XR을 비활성화합니다 (체크)
- **Use Editor Camera**: XR 카메라 대신 일반 카메라를 사용합니다 (체크)
- **Editor Camera**: 사용할 카메라를 지정하거나 자동 생성되게 둡니다

### 2. EditorTestSetup 컴포넌트 추가 (선택사항)
테스트 환경을 자동으로 설정하려면 같은 GameObject에 `EditorTestSetup` 컴포넌트를 추가합니다.

```
Add Component > EditorTestSetup
```

이 컴포넌트는:
- 기본 테스트 환경(바닥, 큐브 등)을 생성합니다
- RenderTexture를 자동으로 생성합니다
- UnityVerseBridgeManager를 자동으로 구성합니다

### 3. UnityVerseBridgeManager 설정
1. UnityVerseBridgeManager GameObject 찾기
2. Mode를 "Host"로 설정
3. Connection Config 할당 확인
4. Quest Stream Camera/Texture는 자동으로 설정됩니다

### 4. 실행 및 테스트
1. Signaling Server 실행
2. Unity Editor에서 Play
3. "Editor Test Mode Active" 메시지 확인
4. Mobile 앱에서 같은 Room ID로 연결
5. WebRTC 스트리밍 확인

## 주요 변경사항

### XRSettings.asset
- "VR Device Disabled": "True" 설정되어 있음

### QuestVideoExtension.cs
- Unity Editor에서 QUEST_SUPPORT 매크로 무시
- Editor에서는 일반 카메라 사용
- Passthrough 기능 Editor에서 비활성화

### 빌드 시 주의사항
- EditorXRDisabler는 Editor 전용이므로 빌드에는 포함되지 않음
- 실제 Quest 빌드 시에는 XR이 정상적으로 활성화됨

## 문제 해결

### "XR Session Terminated" 오류가 계속 나타날 때
1. XR Plug-in Management 설정 확인
2. Standalone 플랫폼에서 OpenXR Loader 비활성화
3. Project Settings > XR Plug-in Management > PC 탭에서 모든 XR 로더 체크 해제

### 카메라가 표시되지 않을 때
1. EditorXRDisabler의 Editor Camera 필드 확인
2. Main Camera 태그가 설정된 카메라가 있는지 확인
3. RenderTexture가 올바르게 생성되었는지 확인

### WebRTC 연결이 안 될 때
1. Signaling Server 연결 상태 확인
2. Room ID가 Mobile과 일치하는지 확인
3. 방화벽 설정 확인 (특히 UDP 포트)

## 디버그 정보
화면에 표시되는 디버그 정보:
- Quest Streaming: ON/OFF 상태
- Connected Peers: 연결된 피어 수
- Resolution: 스트리밍 해상도
- Platform: Unity Editor (XR Disabled)

노란색 텍스트로 Editor 모드임을 표시합니다.