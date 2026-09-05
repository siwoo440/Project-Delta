# 138일차 : 해상도 및 세이프존 대응 구현

## 목표
- 기획서 8.1절 "UI·그래픽 기본 방향" 중 해상도 대응(1280×720~3840×2160)과
  "울트라와이드 대응 세이프존 유지"·"중앙 플레이 영역 보존" 구현

## 구현 내용

### 1. 카메라 종횡비 세이프존 (필러박스/레터박스)
- `AspectRatioSafeAreaController`(Presentation) - 씬 파일을 직접 편집하지 않고,
  `SceneManager.sceneLoaded` 이벤트에 걸어 씬이 로드될 때마다 자동으로
  `Camera.main`에 붙는다(`[RuntimeInitializeOnLoadMethod]`로 앱 시작 시 한 번 등록)
- 기준 종횡비 16:9 대비 화면이 더 넓으면(울트라와이드) 좌우를 필러박스 처리해 중앙
  16:9 영역만 플레이 화면으로 유지, 더 좁으면(세로형 등) 위아래를 레터박스 처리
- 오버레이 UI(Canvas ScreenSpaceOverlay)는 카메라 rect와 무관하게 전체 화면을 그대로
  쓰므로 영향 없음 - 3D 게임 화면(던전·전투)만 대상

### 2. Canvas 해상도 대응 통일
- 전투 HUD 계열(`BattleSpeedRuntimeHud`, `BattleDebugLogOverlay` 등)은 이미
  `matchWidthOrHeight = 0.5`(가로세로 균형)를 쓰고 있었지만, CG·도전과제 갤러리
  (133~134일차)는 이 값을 설정하지 않아 기본값(가로 기준)으로 남아 있었다
- `UiScaleSettings.ApplyToCanvasScaler()`에 `screenMatchMode`/`matchWidthOrHeight = 0.5f`를
  표준으로 추가해, 이 헬퍼를 쓰는 화면(CG·도전과제 갤러리)이 자동으로 다른 화면들과
  같은 기준을 따르도록 통일

## 남은 사항
- NPC 상호작용·상점 등 OnGUI 화면은 Canvas가 아니라 `UiScaleSettings.ApplyGuiMatrix()`로
  배율만 적용 중이라 이번 통일 대상에서 제외 - 8.2절 정식 UI 전환 때 Canvas로 옮기면서
  같이 정리 예정
