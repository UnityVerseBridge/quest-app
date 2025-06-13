# Unity Editor Play Mode 즉시 종료 문제 해결

## 문제 증상
Unity Editor에서 Play 버튼을 누르면 XR 세션이 시작되었다가 즉시 종료됨:
- `OnSessionStateChange: 4 -> 5` (Running -> Stopping)
- `OnSessionEnd` 
- `Shutting down OpenXR`

## 원인
- Unity Editor에서 Meta XR Simulator가 활성화되어 있지만 제대로 작동하지 않음
- Quest XR 플러그인이 실제 VR 헤드셋 없이 실행될 때 발생하는 문제

## 해결 방법

### 방법 1: Meta XR Simulator 비활성화 (즉시 해결)
1. **Project Settings 열기**
   - Edit → Project Settings → XR Plug-in Management → PC, Mac & Linux Standalone
   
2. **Meta XR Simulator 비활성화**
   - "Initialize XR on Startup" 체크 해제
   - 또는 OpenXR 플러그인 전체 체크 해제

3. **EditorXRDisabler 사용 (자동화)**
   - Task에서 생성한 `EditorXRDisabler.cs` 스크립트를 씬에 추가
   - 이 스크립트가 Editor에서 XR을 자동으로 비활성화

### 방법 2: XR Device Simulator 사용
1. **Package Manager 열기**
   - Window → Package Manager
   
2. **XR Device Simulator 설치**
   - Unity Registry 선택
   - "XR Device Simulator" 검색
   - Install 클릭

3. **XR Simulation 활성화**
   - Edit → Project Settings → XR Plug-in Management
   - PC, Mac & Linux Standalone 탭 선택
   - "Initialize XR on Startup" 체크 해제
   - 또는 "OpenXR" 대신 "Mock HMD" 사용

### 방법 2: Editor에서 XR 비활성화
1. **Project Settings 열기**
   - Edit → Project Settings → XR Plug-in Management
   
2. **PC 플랫폼 설정**
   - PC, Mac & Linux Standalone 탭 선택
   - 모든 XR 플러그인 체크 해제

3. **Android 플랫폼은 유지**
   - Android 탭에서는 Oculus/OpenXR 유지

### 방법 3: Editor 전용 설정
1. **UnityVerseBridgeManager 설정**
   - Inspector에서 UnityVerseBridgeManager 선택
   - Debug Display Mode: GUI (VR 헤드셋 없이도 표시됨)

2. **Editor 조건부 컴파일**
   ```csharp
   #if UNITY_EDITOR && !UNITY_ANDROID
   // Editor에서는 XR 없이 테스트
   #endif
   ```

## 추가 설정

### UnityVerseUI 태그 추가
1. Edit → Project Settings → Tags and Layers
2. Tags 섹션에서 + 클릭
3. "UnityVerseUI" 입력

### Input System 설정
1. Edit → Project Settings → Player
2. Configuration → Active Input Handling
3. "Both" 선택 (Legacy와 New Input System 모두 사용)

## 테스트 방법

### Editor에서 테스트
1. XR 플러그인 비활성화 상태로 Play
2. Scene 뷰에서 카메라 움직임 확인
3. Console에서 WebRTC 연결 로그 확인

### 실제 Quest 빌드
1. File → Build Settings
2. Android 플랫폼 선택
3. Build and Run

## 문제가 지속될 경우
1. Unity 재시작
2. Library 폴더 삭제 후 재임포트
3. Unity 2022.3 LTS 최신 버전 확인