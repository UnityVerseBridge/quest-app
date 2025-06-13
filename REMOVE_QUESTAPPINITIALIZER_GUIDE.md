# QuestAppInitializer 제거 가이드

## 제거 이유
QuestAppInitializer는 더 이상 필요하지 않습니다:
- UnityVerseConfig가 자동으로 플랫폼 감지 (Quest/Mobile)
- WebRtcManager가 WebRTC 초기화 처리
- Legacy ConnectionConfig 대신 UnityVerseConfig 사용

## 제거 방법

### 1. Unity Editor에서 Scene 수정
1. `Assets/Scenes/SampleScene.unity` 열기
2. Hierarchy에서 QuestAppInitializer 컴포넌트가 있는 GameObject 찾기
3. QuestAppInitializer 컴포넌트 제거 (Remove Component)
4. Scene 저장

### 2. Script 파일 삭제
Unity Editor에서:
- `Assets/Scripts/QuestAppInitializer.cs` 삭제

또는 파일 시스템에서:
```bash
rm /Users/kimhyeonwoo/Documents/GitHub/UnityVerse/quest-app/UnityProject/Assets/Scripts/QuestAppInitializer.cs
rm /Users/kimhyeonwoo/Documents/GitHub/UnityVerse/quest-app/UnityProject/Assets/Scripts/QuestAppInitializer.cs.meta
```

## 확인 사항
- UnityVerseBridgeManager가 있는 GameObject에 UnityVerseConfig가 할당되어 있는지 확인
- UnityVerseConfig의 Role Detection이 "Automatic"으로 설정되어 있는지 확인
- Quest 프로젝트에서는 자동으로 Host(Quest) 역할로 감지됨