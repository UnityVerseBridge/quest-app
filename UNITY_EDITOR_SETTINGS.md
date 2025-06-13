# Unity Editor Settings for Quest App

## Input System 설정 확인

quest-app 프로젝트에서 Play 모드 문제를 해결하기 위한 설정 가이드입니다.

### 1. Input System Package 설정

1. **Edit → Project Settings → Player** 로 이동
2. **Configuration** 섹션에서 **Active Input Handling** 확인:
   - `Input Manager (Old)` - Legacy Input System만 사용
   - `Input System Package (New)` - 새로운 Input System만 사용  
   - `Both` - 두 시스템 모두 사용 (권장)

현재 에러를 보면 `Input System Package (New)`로 설정되어 있는 것 같습니다.
XR Interaction Toolkit과의 호환성을 위해 `Both`로 변경하는 것을 권장합니다.

### 2. XR Settings 확인

1. **Edit → Project Settings → XR Plug-in Management** 로 이동
2. **Oculus** 또는 **OpenXR** 플러그인이 활성화되어 있는지 확인
3. PC 플랫폼에서는 Mock HMD나 Desktop 모드가 활성화되어 있는지 확인

### 3. Tags 추가

1. **Edit → Project Settings → Tags and Layers** 로 이동
2. Tags 섹션에 `UnityVerseUI` 태그 추가

### 4. WebRTC Package 확인

Package Manager에서 다음을 확인:
- **WebRTC** 패키지가 3.0.0-pre.8 이상 버전인지 확인
- 의존성 패키지들이 모두 설치되어 있는지 확인

### 5. Editor에서 VR 테스트

Quest 앱을 Editor에서 테스트할 때:
1. **XR Device Simulator** 사용 (Package Manager에서 설치)
2. 또는 **Mock HMD** 설정 사용
3. Play 모드에서 Scene 뷰를 VR 뷰로 전환

### 문제 해결

Play가 즉시 중단되는 경우:
1. Console에서 에러 확인 (Clear 후 Play)
2. `WebRTC.Initialize()` 관련 에러가 있는지 확인
3. XR 관련 에러가 있는지 확인
4. Unity를 재시작하고 다시 시도