# 136일차 : 설정 화면 UI 배율 및 자막 옵션 구현

## 목표
- 기획서 8.1절 "UI·그래픽 기본 방향" 중 "UI 배율 옵션(소/보통/대)"과 "자막 지원"을
  실제로 켜고 저장할 수 있게 구현

## 구현 내용

### 1. 설정 읽기/쓰기 연결
- `SettingsData`(`UiSettings.UiScale`, `AccessibilitySettings.SfxSubtitles`)와
  `SaveService.ReadSettings()`/`WriteSettings()`는 이미 있었지만 실제로 쓰는 곳이 없었다
- `ApplicationFlow.ReadOrCreateSettings()`/`SaveSettings()` - 프로필과 같은 방식으로
  Presentation이 Infrastructure를 직접 참조하지 않고 중계

### 2. UI 배율 공용 유틸리티
- `UiScaleSettings`(Presentation) - 소(0.85)/보통(1.0)/대(1.2) 3단계 배율 값을
  OnGUI 화면과 Canvas 화면 양쪽에서 같은 값으로 공유
  - OnGUI 화면: `GUI.matrix`를 화면 중앙 기준으로 스케일(`GUIUtility.ScaleAroundPivot`)
  - Canvas 화면: `CanvasScaler`(ScaleWithScreenSize)의 `referenceResolution`을 배율의
    역수로 줄여서, CanvasScaler 자체의 매 프레임 재계산 로직과 충돌 없이 더 크게/작게
    렌더링되도록 함

### 3. 설정 화면 (`SettingsSceneController`)
- 기존엔 "뒤로가기"만 있는 완전 임시 화면이었던 걸 실제 항목으로 채움
- UI 배율 소/보통/대 3단 버튼(현재 선택 강조 표시)
- 자막 표시 켜기/끄기 토글
- 변경 즉시 저장(기획서 9.1절 - 설정은 런과 별개로 즉시 저장)

### 4. 배율 적용 범위
- 타이틀·로비(OnGUI) + CG 갤러리·도전과제 갤러리(Canvas) 4개 화면에 적용
- NPC 상호작용·상점 화면은 이번엔 범위에서 제외 - 8.2절 정식 UI 전환 때 함께 정리 예정

## 버그 수정 (컴파일 에러)
- `UiScaleSettings.cs`에 `UnityEngine.UI`(`CanvasScaler`), `ProjectDelta.Data`(`SettingsData`)
  using 누락 - 추가해서 해결
