# UnityVerseBridge Quest App

Meta Quest VR 헤드셋에서 실행되는 Unity 애플리케이션으로, 모바일 기기와 WebRTC를 통해 실시간으로 연결됩니다.

## 🎯 프로젝트 개요

이 앱은 Quest VR 환경의 화면을 실시간으로 모바일 기기로 스트리밍하고, 모바일에서의 터치 입력을 VR 공간에서 처리할 수 있게 합니다.

**주요 기능:**
- VR 카메라 뷰를 모바일로 실시간 스트리밍
- 모바일 터치 입력을 VR 공간 좌표로 변환
- 햅틱 피드백 요청 및 처리
- 룸 기반 자동 매칭

## 🎮 사용 시나리오

1. **VR 협업**: VR 사용자의 시야를 모바일 사용자와 공유
2. **원격 제어**: 모바일 기기로 VR 내 오브젝트 조작
3. **관전 모드**: VR 게임/경험을 외부에서 관찰

## 🛠️ 기술 스택

- Unity 6 LTS (6000.0.33f1) 또는 Unity 2022.3 LTS
- Meta XR SDK (구 Oculus Integration)
- Unity WebRTC 3.0.0-pre.8+
- UnityVerseBridge.Core Package

## 📋 요구사항

### 하드웨어
- Meta Quest 2/3/Pro
- 개발용 PC (Windows/Mac)

### 소프트웨어
- Unity 6 LTS (6000.0.33f1) 이상 또는 Unity 2022.3 LTS
- Meta XR SDK
- Android Build Support

## 🚀 설치 및 실행

### 1. 프로젝트 클론
```bash
git clone https://github.com/UnityVerseBridge/quest-app.git
cd quest-app
```

### 2. Unity에서 프로젝트 열기
- Unity Hub에서 "Open" > 프로젝트 폴더 선택
- Unity 6 LTS 또는 Unity 2022.3 LTS 버전으로 열기

### 3. 필수 패키지 설치
Package Manager에서 다음 패키지들이 설치되어 있는지 확인:
- XR Plugin Management
- Oculus XR Plugin
- Unity WebRTC
- TextMeshPro
- UnityVerseBridge.Core (로컬 패키지)

### 4. 빌드 설정
1. File > Build Settings
2. Platform을 Android로 변경
3. Texture Compression: ASTC
4. Player Settings:
   - Minimum API Level: 29
   - Target API Level: 32+

### 5. Quest 디바이스 설정
1. Quest를 개발자 모드로 설정
2. USB로 PC에 연결
3. Build and Run

## 📁 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── QuestAppInitializer.cs    # 앱 초기화 및 연결 관리
│   ├── VrStreamSender.cs         # VR 화면 스트리밍
│   ├── VrTouchReceiver.cs        # 터치 입력 처리
│   ├── VrHapticRequester.cs      # 햅틱 피드백
│   └── WebRtcConnectionTester.cs # 연결 테스트 UI
├── Scenes/
│   └── SampleScene.unity         # 메인 씬
├── Prefabs/
│   └── WebRTC/                   # WebRTC 관련 프리팹
└── StreamTexture.renderTexture   # 스트리밍용 텍스처
```

## 💡 핵심 컴포넌트 설명

### QuestAppInitializer
앱의 진입점으로 WebRTC 초기화와 시그널링 연결을 관리합니다.

**주요 역할:**
- WebRTC.Update() 코루틴 시작 (필수!)
- 시그널링 서버 연결
- 룸 등록 및 피어 대기
- PeerConnection 생성 타이밍 제어

### VrStreamSender
VR 카메라의 렌더링을 RenderTexture로 캡처하여 WebRTC로 전송합니다.

**구현 특징:**
- 게임 뷰와 스트림 동시 표시를 위한 미러 카메라 사용
- RenderTexture 포맷 최적화 (BGRA32)
- 동적 해상도 조정 가능

### VrTouchReceiver
모바일에서 전송된 터치 데이터를 VR 공간 좌표로 변환합니다.

**처리 과정:**
1. 정규화된 좌표 (0-1) 수신
2. VR 카메라 기준 Ray 생성
3. 물리 Raycast로 3D 위치 계산
4. UI/3D 오브젝트 상호작용 처리

## 🔧 설정 가이드

### StreamTexture 설정
Inspector에서 다음과 같이 설정:
- Size: 1280x720 (또는 원하는 해상도)
- Color Format: R8G8B8A8_SRGB
- Depth Buffer: 24 bit depth
- ✅ Create 버튼 클릭

### 씬 계층 구조
```
SampleScene
├── XR Origin
│   ├── Camera Offset
│   │   └── Main Camera (태그: MainCamera)
│   └── Controllers
├── WebRTC Manager
├── Quest App Manager
│   ├── VrStreamSender
│   └── VrTouchReceiver
└── UI Canvas
```

### 컴포넌트 연결
1. **QuestAppInitializer**:
   - WebRtcManager 할당
   - ConnectionConfig 할당
   - Auto Connect: true

2. **VrStreamSender**:
   - Target Camera: Main Camera
   - Source Render Texture: StreamTexture
   - Show In Game View: true

3. **VrTouchReceiver**:
   - WebRtcManager 할당
   - VR Camera: Main Camera (자동 감지)

## 🌐 시그널링 서버 연결

### ConnectionConfig 설정
```
Signaling Server URL: ws://YOUR_SERVER_IP:YOUR_PORT
Room ID: default-room (또는 원하는 룸 이름)
Client Type: Quest
Auto Connect: true
Connection Timeout: 30
```

포트는 시그널링 서버의 .env 파일에서 설정한 포트를 사용합니다.

## 🎨 터치 시각화 (선택사항)

터치 위치를 VR에서 시각화하려면:

1. TouchPointer 프리팹 생성 (구/큐브)
2. VrTouchReceiver의 Touch Pointer Prefab에 할당
3. 터치 시 해당 위치에 포인터 표시

## 🐛 문제 해결

### 스트리밍이 안 되는 경우
1. Console에서 에러 확인
2. `VideoStreamTrack created successfully` 로그 확인
3. RenderTexture가 Created 상태인지 확인
4. WebRTC 패키지 버전 확인 (3.0.0-pre.8+)

### 터치가 인식되지 않는 경우
1. DataChannel 열림 상태 확인
2. `[VrTouchReceiver] Touch: ID=0...` 로그 확인
3. Raycast 대상 오브젝트에 Collider 있는지 확인

### 성능 최적화
- RenderTexture 해상도 조정 (720p 권장)
- Fixed Foveated Rendering 활성화 (향후 구현)
- 불필요한 후처리 효과 비활성화

## 📱 모바일 앱과의 연동

1. 동일한 시그널링 서버에 연결
2. 동일한 룸 ID 사용
3. Quest 앱이 먼저 실행되어야 함 (Offerer 역할)

## 🔒 보안 고려사항

- 프로덕션 환경에서는 WSS (WebSocket Secure) 사용
- 인증 토큰 구현 (ConnectionConfig.authKey)
- 룸 ID를 동적으로 생성하여 사용

## 🚧 향후 개발 계획

### 우선순위 높음
- 오디오 스트리밍 지원
- AR 모드 (Quest 3 패스스루)

### 중간 우선순위  
- RemoveTrack 기능 구현
- 1:N 연결 지원

### 장기 계획
- 성능 최적화 (Fixed Foveated Rendering)
- 동적 해상도 조정
- 클라우드 렌더링 지원

## 📄 라이선스

이 프로젝트는 BSD 3-Clause 라이선스를 따릅니다. 자세한 내용은 [LICENSE](LICENSE) 파일을 참고하세요.

## 👥 제작자

- **kugorang** - [GitHub](https://github.com/kugorang)

---

문제가 있거나 제안사항이 있으시면 [Issues](https://github.com/UnityVerseBridge/quest-app/issues)에 등록해주세요.
