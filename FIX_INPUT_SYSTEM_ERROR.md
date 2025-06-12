# InputSystem NullReferenceException 해결 방법

## 문제
```
NullReferenceException: Object reference not set to an instance of an object
UnityEngine.InputSystem.InputManager.FireStateChangeNotifications
```

이 에러는 Unity 6의 Input System과 Meta XR SDK가 충돌할 때 발생합니다.

## 해결 방법

### 방법 1: InputSystemFix 스크립트 사용 (적용됨)
`Assets/Scripts/InputSystemFix.cs` 스크립트가 자동으로 이 문제를 해결합니다.

### 방법 2: Input System 설정 조정
1. Edit > Project Settings > Input System Package
2. "Update Mode"를 "Process Events In Fixed Update"로 변경
3. Unity 재시작

### 방법 3: XR Interaction Toolkit 업데이트
1. Window > Package Manager
2. XR Interaction Toolkit을 최신 버전으로 업데이트
3. Input System도 최신 버전으로 업데이트

### 방법 4: Legacy Input Manager 사용
프로젝트가 새로운 Input System을 사용하지 않는다면:
1. Edit > Project Settings > Player
2. Configuration > Active Input Handling을 "Input Manager (Old)"로 변경

## 참고
- 이 문제는 Unity 6과 Meta XR SDK의 알려진 호환성 문제입니다
- InputSystemFix.cs는 임시 해결책이며, Unity나 Meta의 공식 패치가 나올 때까지 사용합니다
- 실제 빌드(APK)에서는 이 에러가 발생하지 않을 수 있습니다