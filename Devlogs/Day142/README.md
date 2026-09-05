# 142일차 : 던전 탐험 오버레이 화면 정식 UI 전환

## 목표
- 기획서 8.2절 "화면별 정식 UI 전환"의 남은 탐험 오버레이 화면을 정리
- `DungeonLobbyReturnHudController`, `StairsInteractionController`,
  `PlayerDoorInteractionController`, `LoadingSceneController`의 기존 `OnGUI` 표시를
  런타임 UGUI로 전환
- 문·계단 상호작용 판정, 로비 복귀, 로딩 씬 전환 등 기존 게임 로직은 유지하고
  화면 표시 계층만 교체

## 구현 내용

### 1. 던전 로비 복귀 HUD 전환
- `DungeonLobbyReturnHudController`의 기존 `OnGUI()` 버튼 제거
- 우측 상단에 런타임 UGUI `Button`을 생성해 기존 `"로비로"` 기능 유지
- 버튼 클릭 시 기존 `ApplicationFlow.Current?.ReturnToLobby()` 호출 유지
- `RuntimeUiFactory.EnsureEventSystem()`과 `UiScaleSettings`를 사용해
  프로젝트 공용 UI 배율 정책 적용

### 2. 문 상호작용 안내 전환
- `PlayerDoorInteractionController`의 `OnGUI()`와 `GUIStyle` 제거
- 기존 `BuildPromptText()`와 `TryOpenDoor()` 흐름은 유지
- 화면 하단 중앙에 반투명 배경 + UGUI `Text` 기반 Prompt UI 추가
- `"열기 [F]"`, `"잠김 (열쇠 : N개)"` 안내를 기존 조건 그대로 표시
- `promptText`가 변경될 때만 `RefreshPromptUi()`로 텍스트와 표시 상태 갱신

### 3. 계단 상호작용 안내 전환
- `StairsInteractionController`의 `OnGUI()`와 `GUIStyle` 제거
- 정면 계단 판정, F 입력, Esc 취소, `TryDescend()` 호출 흐름 유지
- 화면 하단 중앙에 문 상호작용과 동일한 계열의 Prompt UGUI 추가
- 첫 번째 F 입력 시 확인 상태 진입,
  두 번째 F 입력 시 기존 층 이동 로직 실행
- `"계단 내려가기 [F]"`,
  `"이전 층으로 돌아갈 수 없습니다. 내려가시겠습니까? [F] 확인 / [Esc] 취소"`
  문구를 현재 상태에 맞춰 갱신

### 4. 로딩 화면 전환
- `LoadingSceneController`의 기존 `OnGUI()`와 `EnsureStyles()` 제거
- `RuntimeUiFactory.BuildScreenCanvas()`를 사용해 전체 화면 런타임 UGUI 생성
- 중앙에 `"로딩 중..."` Text 표시
- 기존 `"계속 (임시)"` 버튼을 UGUI `Button`으로 전환
- 버튼 클릭 시 기존 `ApplicationFlow.Current?.ProceedFromLoadingScreen()` 호출 유지
- 실제 로딩 진행률 및 자동 전환 기능은 기존 TODO 범위를 유지하고 이번 일차에서는 추가하지 않음

### 5. 공용 UI 구성 재사용
- 신규 범용 프록시나 Editor 자동 패처를 추가하지 않고 기존 컨트롤러에 직접 UGUI 적용
- `RuntimeUiFactory`의 EventSystem, UI 오브젝트, 텍스트, 버튼 생성 기능 재사용
- `UiScaleSettings`를 통해 기존 해상도 및 UI 배율 대응 구조 유지

## 수정 파일
- `Assets/ProjectDelta/Scripts/Presentation/DungeonLobbyReturnHudController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/StairsInteractionController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/PlayerDoorInteractionController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/LoadingSceneController.cs`

## 삭제된 요소
- 각 대상 화면의 `OnGUI()` 기반 표시
- 문·계단 화면의 `GUIStyle`
- 로딩 화면의 `EnsureStyles()`
- `GUI.Label`, `GUI.Button` 기반 직접 렌더링

## 유지된 로직
- 문 열기 판정 및 열쇠 소비 흐름
- 계단 정면 판정과 F/Esc 확인 흐름
- `DungeonFloorController.TryDescend()`
- `ApplicationFlow.Current?.ReturnToLobby()`
- `ApplicationFlow.Current?.ProceedFromLoadingScreen()`

## 정리
- 던전 탐험 중 남아 있던 작은 OnGUI 오버레이 화면 4개를 런타임 UGUI로 전환
- 기존 입력, 판정, 층 이동, 씬 전환 로직은 그대로 유지하고 표시 계층만 교체
- 복잡한 변환 계층 없이 각 컨트롤러에서 직접 UGUI를 구성해 구조를 단순하게 유지
