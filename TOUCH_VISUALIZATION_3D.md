# Quest App 3D 터치 시각화

## 개요
Quest 앱에서 모바일 터치 입력을 3D 공간에 빨간 구체로 표시합니다. 이 구체는 VR 카메라로 스트리밍되므로 모바일 화면에서도 볼 수 있습니다.

## 작동 방식

### 1. 터치 좌표 처리 흐름
```
Mobile 터치 → Quest 3D 공간에 빨간 구체 표시 → VR 카메라로 촬영 → 모바일로 스트리밍
```

### 2. TouchInputHandler 설정
- **자동 활성화**: UnityVerseBridgeManager가 Host 모드에서 자동으로 추가
- **항상 표시**: Debug Mode와 관계없이 항상 빨간 구체 표시
- **크기**: 0.2 단위 (이전 0.1에서 2배 증가)
- **색상**: 진한 빨간색 (80% 불투명도) + Emission 효과

### 3. 시각화 특징
- **히트 시**: 터치가 오브젝트에 닿으면 해당 위치에 표시
- **미스 시**: 터치가 빈 공간을 향하면 레이 방향 10m 지점에 표시
- **터치 종료**: 터치가 끝나면 구체가 사라짐

## 설정 확인

### Quest App (UnityVerseBridge_Quest)
1. TouchInputHandler 컴포넌트 확인:
   - `Show Touch Visualizer`: ✓ (체크됨)
   - `Touch Visualizer Prefab`: None (자동 생성)
   - `VR Camera`: [자동 감지됨]

### Mobile App (UnityVerseBridge_Mobile)
1. MobileInputExtension 컴포넌트 확인:
   - `Show Touch Visualizer`: ☐ (체크 해제)
   - 모바일에서는 터치 시각화 표시 안 함

## 커스터마이징

### 구체 크기 변경
TouchInputHandler.cs에서:
```csharp
visualizer.transform.localScale = Vector3.one * 0.3f; // 더 크게
```

### 색상 변경
```csharp
renderer.material.color = new Color(0f, 1f, 0f, 0.8f); // 녹색
renderer.material.SetColor("_EmissionColor", new Color(0f, 1f, 0f, 1f));
```

### 커스텀 프리팹 사용
1. 3D 오브젝트 프리팹 생성
2. TouchInputHandler의 `Touch Visualizer Prefab` 필드에 할당
3. 자동으로 해당 프리팹 사용

## 성능 고려사항
- 각 터치마다 하나의 3D 오브젝트 생성
- 최대 5개 동시 터치 지원 (설정 가능)
- 터치 종료 시 오브젝트 재사용

## 문제 해결

### 빨간 구체가 안 보일 때
1. TouchInputHandler가 추가되었는지 확인
2. VR Camera가 제대로 할당되었는지 확인
3. Console에서 터치 수신 로그 확인:
   ```
   [TouchInputHandler] Touch from default hit: ... at (x, y, z)
   ```

### 위치가 이상할 때
1. 화면 해상도 차이 확인 (Debug Mode 활성화)
2. 카메라 FOV 설정 확인
3. 종횡비 차이 확인

### 스트리밍에 안 보일 때
1. 빨간 구체가 카메라 시야각 내에 있는지 확인
2. Emission 설정이 제대로 되어있는지 확인
3. 조명 설정 확인

## 장점
- 모바일에서 터치한 위치를 VR 공간에서 직접 확인 가능
- 스트리밍을 통해 모바일에서도 피드백 확인 가능
- 3D 공간에서의 정확한 터치 위치 파악