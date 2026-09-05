# 142일차 : 던전 미니맵·전체 지도 정식 UI 전환

## 목표
- 기획서 8.2절 "화면별 정식 UI" 전환 - 141일차 이벤트 전투 화면에 이어,
  `DungeonMinimapController`의 미니맵과 M 전체 지도를 기존 `OnGUI` 기반 표시에서
  런타임 UGUI로 전환
- 던전 공개 상태, 현재 방 추적, 타일 공개, 지도 스냅샷, M/Esc/마우스 휠 입력 등
  기존 탐험 로직은 유지하고 화면 표시 계층만 교체
- 파일 규모가 큰 미니맵을 142일차 단독 범위로 처리하고, 나머지 탐험 HUD 화면은
  143일차로 분리

## 구현 내용

### 1. DungeonMinimapController의 OnGUI 표시부 전환
- 기존 `OnGUI()`를 `BuildDungeonMinimapRuntimeUi142()`로 전환
- 기존 `GUI.BeginGroup`, `GUI.EndGroup`, `GUI.DrawTexture`, `GUI.Label`,
  `GUI.color`, `GUI.matrix`, `GUI.skin`, `GUIUtility.RotateAroundPivot` 호출을
  `DungeonMinimapRuntimeGuiProxy` 경유 방식으로 변경
- 플레이어 중심 미니맵, 전체 지도, 방 종류 표시, 탐험률/층 정보, 계단 거리,
  방 연결선, 벽·타일·콘텐츠 문자, 플레이어 방향 표시 등 기존 지도 표현을 유지

### 2. 미니맵 전용 IMGUI 호환 프록시
- `DungeonMinimapRuntimeGuiProxy`를 추가해 기존 좌표 기반 IMGUI 호출을
  UGUI 렌더링용 노드 데이터로 기록
- 그룹 클리핑, 텍스처, 라벨, 색상, 글꼴 크기/정렬/스타일, 플레이어 방향 회전 정보를
  `DungeonMinimapRuntimeFrame`에 저장
- 미니맵에서 실제로 사용하는 기능만 구현해 141일차 이벤트 전투용 범용 프록시와
  분리된 전용 구조로 구성

### 3. 런타임 UGUI View
- `DungeonMinimapRuntimeView`가 전용 Canvas와 CanvasScaler를 런타임에 생성
- `RuntimeUiFactory.EnsureEventSystem()`과 공용 UI 생성 헬퍼를 재사용
- `UiScaleSettings`를 적용해 프로젝트의 기준 해상도 및 UI 배율 정책과 연결
- 기존 `GUI.BeginGroup` 영역은 `RectMask2D`가 적용된 RectTransform으로 변환해
  미니맵/전체 지도 범위 밖 요소를 클리핑
- `GUI.DrawTexture`는 `RawImage`, `GUI.Label`은 UGUI `Text`로 변환하고
  플레이어 방향 아이콘의 회전도 RectTransform 회전으로 반영

### 4. 상태 변경 시점 화면 갱신
- `DungeonMinimapRuntimeAdapter`가 장면의 `DungeonMinimapController`를 자동 탐색하고
  `BuildDungeonMinimapRuntimeUi142()`를 연결
- 기존 지도 표시 코드를 프레임 데이터 생성 용도로 실행하고, 생성된 노드 구조의
  상태 지문이 달라진 경우에만 실제 UGUI 오브젝트를 다시 생성
- 플레이어 이동, 방향 변경, 공개 타일 변화, 전체 지도 열림/닫힘, 줌 변화 등
  실제 화면 상태가 바뀔 때만 View를 갱신

### 5. 기존 탐험 로직 유지
- `DungeonMinimapRevealTracker`, `DungeonMinimapSnapshotBuilder`,
  `RunContext.Current.Dungeon`의 공개 방 복원/병합 로직은 기존 흐름을 유지
- M키 전체 지도 열기/닫기, Esc 닫기, 마우스 휠 줌 계산도 기존 `Update()` 흐름 유지
- 상세 타일 지도를 만들 수 없는 경우 기존 방 단위 지도 표시로 복구하는 fallback도 유지

### 6. 자동 소스 패처
- Editor 전용 `Day142DungeonMinimapOnGuiPatcher`와
  `Day142DungeonMinimapSourcePatcherCore` 추가
- 프로젝트 로드 시 `DungeonMinimapController.cs`의 기존 `OnGUI()`를 찾아
  런타임 UI 빌더 이름으로 변경하고 관련 GUI 호출을 전용 프록시 호출로 치환
- 패치 마커와 변환 메서드 이름을 검사해 같은 파일의 중복 변환을 차단

## 정리
- 약 2,000줄 규모의 `DungeonMinimapController`에서 던전 탐색·공개 상태 계산은
  유지하면서 지도 렌더링 계층을 UGUI로 분리
- 미니맵과 M 전체 지도를 하나의 전용 런타임 View/Adapter/Proxy 구조로 연결
- 141일차 이벤트 전투 화면과 마찬가지로 기존 기능을 다시 작성하지 않고
  현재 동작을 보존하는 방향으로 정식 UI 전환

## 남은 사항
- 143일차: 던전 탐험의 나머지 오버레이 화면 정식 UI 전환
- 대상:
  `DungeonLobbyReturnHudController.cs`
  `StairsInteractionController.cs`
  `PlayerDoorInteractionController.cs`
  `LoadingSceneController.cs`
