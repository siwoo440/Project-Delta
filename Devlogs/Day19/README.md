# Project Delta - 19일차 개발일지

## 개발 주제

**RoomView 프리팹 및 TestRoom_A/B RoomDefinition 연결 마무리**

기획서 10.3절 "방 표현"을 기반으로 `RoomView`를 구현했다. 동시에 18일차부터 두 번의 일차에 걸쳐 미뤄졌던 `TestRoom_A`/`TestRoom_B`의 `RoomDefinition` 연결을 마무리했다.

---

## 개발 목표

- `RoomView`로 문/테마/계단/상자/비밀 벽/NPC 지점/환경 소품을 "표시만" 하는 진입점 마련
- 콘텐츠가 아직 없는 항목(계단·상자·비밀 벽·NPC 지점·환경 소품)은 빈 자리 표시만 준비
- `TestRoom_A`/`TestRoom_B`에 실제 `RoomDefinition` 에셋 연결
- 두 방에 `RoomView` 컴포넌트 부착

---

## 구현 내용

### 1. 판정과 표시의 경계 재확인

`RoomPassageController`를 다시 살펴본 결과, 문 열기 판정(`GridPassage.TryOpenDoor`)은 이미 Domain에 있고 컨트롤러는 그 결과로 시각만 갱신하고 있었다. 기획서 원칙(*"RoomView가 조우 확률이나 이벤트 결과를 직접 결정하지 않는다"*)을 이미 만족하고 있어서, 별도 리팩터링 없이 그 위에 `RoomView`를 얹었다.

---

### 2. RoomContentMarker — 콘텐츠 자리 표시 전용

```text
RoomContentType
├─ Stairs
├─ Chest
├─ SecretWall
├─ NpcPoint
└─ AmbientProp
```

`RoomContentMarker`는 이 중 하나의 종류를 가진 빈 자리 표시만 한다. 스스로 아무것도 생성하거나 판정하지 않는다. 실제 배치는 던전 생성 시스템(26~35일차)의 몫이라, 오늘은 어떤 방에도 실제 마커를 심지 않았다 — 아직 아무도 쓰지 않는 빈 오브젝트만 늘어나기 때문이다.

---

### 3. RoomView — 문 + 콘텐츠 자리를 묶는 진입점

```text
RoomView (RequireComponent: RoomPassageController)
├─ PassageController — 문 상태 조회
├─ ThemeId — 방 테마 (TODO: 던전 생성 연결)
└─ GetMarkers(RoomContentType) — 종류별 콘텐츠 자리 조회
```

`Awake()`에서 자식의 `RoomContentMarker`를 전부 모아 종류별로 정리해둔다. 지금은 조회할 마커가 없지만, 이후 생성 시스템이 "이 방에 상자가 몇 개 자리가 있는지" 물어볼 수 있는 API를 미리 마련했다.

---

### 4. TestRoom_A / TestRoom_B RoomDefinition 연결 마무리

18일차부터 두 번 미뤄졌던 항목이다. 14~15일차의 하드코딩 값을 그대로 옮겨 에셋으로 만들었다.

```text
RoomDefinition_TestRoom_A (ROOM_TEST_A)
├─ (0,0) North Door (일반)
├─ (1,0) North Door (잠김)
├─ (-1,0) North Wall
└─ (0,2) North Door (방 경계)

RoomDefinition_TestRoom_B (ROOM_TEST_B)
└─ (0,-2) South Door (방 경계)
```

`DungeonScene`의 두 `RoomPassageController`에 각각 연결하고, 동시에 `RoomView` 컴포넌트도 추가했다.

---

## 적용 중 발견된 문제 및 수정

없음. 18일차부터 이어지던 "RoomDefinition이 지정되지 않았습니다" 경고가 이번 일차에 해소되었다.

---

## 현재 19일차 전체 흐름

```text
RoomPassageController의 판정/표시 분리 상태 재확인 (이미 준수 중)
↓
RoomContentType + RoomContentMarker 구현 (빈 자리 표시 전용)
↓
RoomView 구현 (문 상태 + 콘텐츠 자리 조회 진입점)
↓
RoomDefinition_TestRoom_A/B 에셋 생성
↓
DungeonScene에 연결 + RoomView 컴포넌트 부착
↓
기존 "RoomDefinition 미지정" 경고 해소
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Presentation/RoomContentMarker.cs
Assets/ProjectDelta/Scripts/Presentation/RoomView.cs
Assets/ProjectDelta/Data/Rooms/RoomDefinition_TestRoom_A.asset
Assets/ProjectDelta/Data/Rooms/RoomDefinition_TestRoom_B.asset
Devlogs/Day19/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scenes/DungeonScene.unity (roomDefinition 연결, RoomView 컴포넌트 추가)
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

19일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- `TestRoom_A`/`TestRoom_B` 실행 시 "RoomDefinition이 지정되지 않았습니다" 경고가 더 이상 나오지 않음
- 기존 문/벽/방 이동 동작이 그대로 유지됨
- `RoomView.GetMarkers()`가 마커가 없을 때 빈 목록을 반환함 (예외 없음)
- `RoomView`가 `RoomPassageController` 없이는 부착되지 않음 (`RequireComponent`)

---

## 다음 개발 방향

다음 20일차에는 **정식 방 진입·이탈과 현재 방 관리**를 구현한다. 지금은 `PlayerGridMovementController`가 `passageController`/`roomOrigin`을 직접 갈아끼우는 방식인데, 이를 `RoomView` 단위로 정리한다.

예정 흐름:

```text
현재 RoomView를 추적하는 별도 상태(예: DungeonRunState.CurrentRoomView) 마련
↓
방 진입 시: 이전 방 정리(가이드 숨김 등) → 새 방 RoomView 활성화
↓
방 이탈 시: 되돌아갈 수 없다는 3.1절 원칙 반영 준비 (이전 층 복귀 불가와 같은 패턴)
↓
18일차 미로 방 10개 중 하나를 실제로 DungeonScene에 배치해 RoomView 흐름 검증
```
