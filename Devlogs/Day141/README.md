# 141일차 : 이벤트 전투 화면 정식 UI 전환

## 목표
- 기획서 8.2절 "화면별 정식 UI" 전환 - 140일차 NPC 상호작용/상점 화면에 이어,
  별도 이벤트 전투 화면을 기존 `OnGUI` 기반 표시에서 런타임 UGUI로 전환
- 기존 `EventBattleController`의 행동 판정, 주도권, 자원, 상성, 아이템 사용, 승패 처리
  흐름은 유지하고 화면 입력·표시 계층만 교체

## 구현 내용

### 1. EventBattleController의 OnGUI 표시부 전환
- 기존 `OnGUI()`를 `BuildEventBattleRuntimeUi141()`로 전환하고,
  `GUI`/`GUILayout` 호출을 `EventBattleRuntimeGuiProxy`를 통해 런타임 UI 프레임으로 기록
- 대상 목록, 플레이어 MP/정력, 주도권 상태, 행동/아이템 탭, 전투 로그, 포기 버튼 등
  기존 이벤트 전투 화면 구성을 그대로 유지
- 행동 버튼의 사용 가능 여부는 현재 플레이어 차례와 자원 비용을 기준으로 유지하고,
  선택 대상의 행동 상성도 기존 `[약점]`/`[강점]` 표시를 그대로 사용

### 2. 런타임 UGUI 자동 연결
- `EventBattleRuntimeAdapter`가 장면의 `EventBattleController`를 자동으로 찾아
  변환된 UI 빌더를 연결
- 기존 OnGUI 코드를 프레임 생성 용도로 실행하고, 실제 표시 상태가 바뀐 경우에만
  `EventBattleRuntimeView`를 다시 그리도록 상태 지문 방식으로 갱신
- UGUI 버튼·토글·텍스트·슬라이더·선택 입력을 기존 OnGUI 반환값으로 다시 전달해
  기존 컨트롤러 분기와 판정 메서드를 그대로 재사용

### 3. UGUI 화면 구성
- `EventBattleRuntimeView`가 전용 Canvas, CanvasScaler, GraphicRaycaster와 EventSystem을
  런타임에 준비하고 화면 전체를 UGUI로 생성
- 기준 해상도 1920×1080, Screen Space Overlay 방식으로 구성
- 본문은 `RectMask2D` + `ScrollRect` 조합으로 만들어 가변 길이 행동·아이템·로그가
  화면을 넘더라도 스크롤 가능하게 처리
- 실제 아트 자산 없이 현재 개발 단계에 맞춰 색상 패널과 텍스트 중심으로 표시

### 4. IMGUI 호환 프록시
- `EventBattleRuntimeGuiProxy`에서 기존 `GUI`/`GUILayout` 호출에 필요한 버튼, 라벨,
  박스, 레이아웃 그룹, 스크롤, 입력 요소, 스타일 및 옵션 호출을 UGUI용 노드로 변환
- `GUILayout.BeginScrollView(Vector2, GUIStyle, params GUILayoutOption[])` 형태를 포함해
  기존 이벤트 전투 화면에서 사용하던 스타일 오버로드를 호환
- 기존 `GUI.enabled`, `GUI.color`, `GUI.skin` 상태도 프록시에서 유지해 원래 표시 조건을
  가능한 그대로 재사용

### 5. 자동 소스 패처
- Editor 전용 `Day141EventBattleOnGuiPatcher`와
  `Day141EventBattleSourcePatcherCore`를 추가
- 프로젝트 로드 시 `EventBattleController.cs`의 `OnGUI()`를 한 번만 찾아
  런타임 UI 빌더 이름으로 변경하고 관련 GUI 호출을 프록시 호출로 치환
- 패치 마커와 메서드 이름 검사를 통해 같은 파일이 반복 변환되지 않도록 처리

## 정리
- 이벤트 전투의 실제 판정·서비스 호출 구조는 유지하고 화면 표시 방식을 런타임 UGUI로
  교체했다
- `Object.FindFirstObjectByType` 호출은 `UnityEngine.Object`로 명시해 타입 모호성을 제거
- 전투 로그 스크롤에서 사용하는 `GUIStyle` 포함 `BeginScrollView` 오버로드를 추가해
  기존 이벤트 전투 UI 호출 형태와 프록시 시그니처를 일치시켰다

## 남은 사항
- 142일차: 던전 탐험 오버레이 화면 정식 UI 전환
- `DungeonMinimapController.cs` 규모가 큰 만큼 실제 착수 시 미니맵 단독 작업과
  나머지 복귀 HUD·계단·문 상호작용·로딩 화면의 분리 여부를 결정
