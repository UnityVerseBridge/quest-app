# Meta XR Simulator 설정 가이드

## Meta XR Simulator가 Unity Editor에서 제대로 작동하도록 설정

### 1. XR Plug-in Management 설정

1. **Edit → Project Settings → XR Plug-in Management** 열기

2. **PC, Mac & Linux Standalone** 탭에서:
   - ✅ **Initialize XR on Startup** 체크
   - ✅ **OpenXR** 선택
   
3. **OpenXR 설정** (⚙️ 아이콘 클릭):
   - **Render Mode**: Single Pass Instanced
   - **Depth Submission Mode**: None
   
4. **OpenXR Feature Groups**:
   - ✅ **Meta XR Simulator** 활성화
   - ✅ **Meta XR Feature Group** 활성화

### 2. Meta XR Simulator 설정

1. **Window → Meta → XR Simulator** 열기

2. **Simulator Settings**:
   - **Device**: Quest 3 (또는 원하는 디바이스)
   - **Controller Type**: Touch Plus
   - **Room Scale**: Standing/Room Scale

3. **Play Mode**에서:
   - Scene 뷰에서 WASD로 이동
   - 마우스로 회전
   - Shift로 빠른 이동

### 3. 문제 해결

#### XR 세션이 즉시 종료되는 경우:

1. **OpenXR Runtime 확인**:
   - Edit → Project Settings → XR Plug-in Management → OpenXR
   - **Play Mode OpenXR Runtime**: Unity Mock Runtime 선택

2. **Meta XR Simulator 패키지 확인**:
   ```
   Window → Package Manager
   Packages: In Project
   "Meta XR Simulator" 버전 확인 (최신 버전 권장)
   ```

3. **XR Interaction Toolkit 설정**:
   - XR Interaction Toolkit이 설치되어 있다면
   - Edit → Project Settings → XR Interaction Toolkit
   - **Use XR Device Simulator in scenes** 체크 해제

### 4. 스트리밍 테스트

1. **UnityVerseBridge_Quest** GameObject 확인:
   - VR Camera가 제대로 할당되었는지
   - UnityVerseConfig의 Role이 "Host"인지

2. **Play 모드**:
   - Console에서 에러 없이 실행되는지 확인
   - "[QuestVideoExtension] Created RenderTexture: 1280x720" 로그 확인

3. **시그널링 서버 연결**:
   - 서버가 실행 중인지 확인
   - Room ID가 일치하는지 확인

### 5. UnityVerseBridge와 함께 사용하기

1. **자동 설정**:
   - "UnityVerseBridge/Create Quest Setup" 메뉴 사용 시 자동으로 XRSessionMonitor 추가됨
   - Editor에서 자동으로 카메라 생성 및 컨트롤러 추가

2. **Editor Camera Controls**:
   - WASD: 이동
   - Q/E: 아래/위 이동  
   - 우클릭 + 드래그: 시점 회전
   - Shift: 빠른 이동

3. **디버깅**:
   - XRSessionMonitor가 XR 세션 상태를 모니터링
   - Meta XR Simulator 감지 시 자동으로 설정 가이드 표시

### 6. 권장 사항

- Unity 2022.3 LTS 최신 버전 사용
- Meta XR SDK 최신 버전 유지
- Unity를 재시작하면 대부분의 XR 관련 문제 해결됨

### 7. 대안: Editor에서 XR 없이 테스트

XR Simulator 없이 테스트하려면:
1. PC 플랫폼에서 OpenXR 비활성화
2. UnityVerseBridge가 자동으로 Editor 카메라 생성
3. SimpleEditorCameraController로 VR처럼 이동하며 테스트
4. 실제 Quest 빌드로 최종 검증

**참고**: EditorXRDisabler는 더 이상 필요하지 않습니다. UnityVerseBridge가 자동으로 Editor 환경을 감지하고 적절한 카메라를 설정합니다.