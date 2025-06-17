# UnityVerse Quest App

Meta Quest VR 애플리케이션 - UnityVerseBridge Core 패키지를 사용한 VR-모바일 스트리밍 예제

## 개요

이 Quest 애플리케이션은 다음 기능을 보여줍니다:
- VR 카메라를 모바일 디바이스로 스트리밍
- 모바일 디바이스로부터 터치 입력 수신
- 원격 터치에 반응하는 상호작용 VR 오브젝트
- Meta XR SDK 통합

## 요구사항

- Unity 6 LTS (6000.0.33f1) 또는 Unity 2022.3 LTS
- Meta XR SDK 77.0.0
- UnityVerseBridge Core 패키지
- Android Build Support
- Quest 2/3/Pro 디바이스

## 프로젝트 설정

1. 이 저장소를 클론
2. Unity에서 `UnityProject` 폴더 열기
3. 필수 패키지 임포트:
   - UnityVerseBridge Core
   - Meta XR SDK
   - Unity WebRTC

## 구성

1. 샘플 씬 열기: `Assets/Scenes/QuestStreamingDemo.unity`
2. `UnityVerseBridge_Quest` GameObject 찾기
3. UnityVerseConfig 설정:
   - Signaling URL: 시그널링 서버 주소
   - Room ID: 고유한 룸 식별자
   - Auto Connect: 자동 연결 활성화

## Quest 빌드

1. File > Build Settings
2. Android 플랫폼으로 전환
3. Player Settings:
   - Minimum API Level: 29
   - Target API Level: 32+
   - Texture Compression: ASTC
4. XR Plug-in Management:
   - Oculus 활성화
5. Build and Run

## 주요 스크립트

### VRClickHandler
원격 터치 입력 처리 예제:
- 시각적 피드백 (색상 변경, 스케일 애니메이션)
- Unity Events 트리거 가능
- 3D 오브젝트에 연결하여 사용

### TouchVisualizationManager
터치 시각화 시스템:
- Canvas 기반 터치 포인트 표시
- 멀티터치 지원
- 디버그 모드에서 좌표 표시

### InputSystemFix
Unity 6 + Meta XR 호환성 문제 해결:
- 런타임에 자동으로 적용
- OpenXR 로더 초기화 문제 수정

## Editor 테스트

1. Meta XR Simulator 설치
2. Window > Meta > XR Simulator
3. "Play Mode OpenXR Runtime" 활성화
4. Play 모드 진입

## 문제 해결

### Unity 6 Input System 오류
`InputSystemFix.cs`가 자동으로 문제를 해결합니다. 오류가 지속되면:
1. Edit > Project Settings > Player
2. Active Input Handling을 "Input Manager (Old)"로 설정

### URP Missing Types 경고
기능에는 영향 없음. 해결하려면:
1. Universal RP 패키지를 최신 버전으로 업데이트
2. 또는 URP Global Settings 재생성

### 빌드 오류
1. Android Build Support 설치 확인
2. Meta XR SDK 올바르게 임포트 확인
3. 최소 API 레벨 29+ 확인

## 샘플 씬 구성

데모 씬 포함 요소:
- VR 카메라 스트리밍 설정
- 상호작용 가능한 3D 오브젝트 (큐브, 구)
- 터치 시각화 캔버스
- 디버그 UI (비활성화 가능)

Quest 앱을 먼저 실행한 후, 동일한 룸 ID로 모바일 앱을 연결하세요.

## 성능 최적화

- 기본 스트리밍 해상도: 640x360 @ 30fps
- 적응형 품질: 연결된 피어 수에 따라 조정
- H264 하드웨어 가속 활용
- 혼합 현실을 위한 Passthrough 지원

## 라이선스

루트 저장소의 LICENSE 파일을 참조하세요.