# Project Delta - 11일차 개발일지

## 개발 주제

**단일 테스트 방과 플레이어 그리드 위치 데이터 구현**

저장·런타임 기반 구축을 마치고 던전 탐험 구간(11~25일차)에 진입했다. 이번 일차에서는 실제 이동 기능을 넣기 전에, 던전 안에서 플레이어가 어느 칸에 있는지를 표현할 논리 좌표와 이를 확인할 최소 테스트 공간을 먼저 구성했다.

---

## 개발 목표

- `DungeonScene`에 탐험 기능 검증용 단일 테스트 방 구성
- Unity 월드 좌표와 분리된 정수 기반 플레이어 그리드 좌표 정의
- `PlayerRunState`에 현재 그리드 위치 연결
- Domain 계층이 `UnityEngine.Vector3` 등에 의존하지 않도록 유지
- 그리드 좌표 생성과 플레이어 초기 좌표를 EditMode 테스트로 검증
- 던전 탐험용 Player/Camera 기본 계층 구성
- 사용하지 않는 기본 `SampleScene` 정리

---

## 구현 내용

### 1. `GridPosition` 도메인 데이터 추가

`Assets/ProjectDelta/Scripts/Domain/GridPosition.cs`를 추가했다.

플레이어의 던전 위치를 Unity의 실수형 월드 좌표가 아니라 다음과 같은 정수 좌표로 표현한다.

```text
(0, 0)
(1, 0)
(1, 1)
```

`GridPosition`은 다음 값을 가진다.

```text
X    — 가로 그리드 좌표
Z    — 세로 그리드 좌표
Zero — 시작 원점 (0, 0)
```

Domain 계층에는 `UnityEngine.Vector3` 또는 `Vector2Int`를 넣지 않았다. 따라서 이후 저장, 자동 던전 생성, 테스트 코드가 Unity 오브젝트 없이도 플레이어 위치를 다룰 수 있다.

---

### 2. `PlayerRunState`에 현재 그리드 위치 연결

기존 `PlayerRunState`에 다음 런타임 상태를 추가했다.

```text
CurrentGridPosition
```

새 런을 시작한 플레이어의 기본 위치는 `GridPosition.Zero`, 즉 `(0, 0)`이다.

현재 단계에서는 이 값을 `RunData` 저장 DTO까지 연결하지 않았다. 실제 방 이동 후 자동 저장 연결은 후속 일정에서 별도로 처리한다.

---

### 3. `DungeonScene` 단일 테스트 방 구성

`DungeonScene`에 던전 탐험 기능을 시험할 최소 공간을 만들었다.

```text
===Dungeon===
├─ GridOrigin
├─ TestRoom
│  ├─ Floor
│  ├─ Ceiling
│  ├─ Wall_North
│  ├─ Wall_South
│  ├─ Wall_East
│  └─ Wall_West
└─ Player
   └─ Main Camera
```

테스트 방은 Floor, Ceiling, 동·서·남·북 벽으로 구성했으며 각 벽에는 기본 BoxCollider가 포함되어 있다.

`GridOrigin`과 `Player`의 시작 위치는 월드 원점으로 맞췄다. `Main Camera`는 `Player`의 자식으로 이동하고 눈높이 기준 Local Y를 `1.6`으로 설정했다.

이번 일차에서는 방의 시각적 완성도보다 앞으로 이동·벽 판정·문·방 전환 시스템을 반복해서 시험할 수 있는 최소 공간 확보를 우선했다.

---

### 4. 위치와 시점의 책임 분리 기반 마련

Project Delta의 탐험 방식은 다음 구조를 사용한다.

```text
플레이어 위치 → 정수 그리드 좌표
플레이어 시점 → 마우스로 자유 회전
WASD 이동    → 현재 보는 방향을 기준으로 한 칸씩 이동
```

따라서 마우스로 주변을 둘러보더라도 `CurrentGridPosition`은 변하지 않는다.

11일차에서는 위치 데이터만 먼저 구현했고, 마우스 자유 시점과 시점 기준 이동 입력은 후속 일차에서 연결한다.

---

### 5. EditMode 테스트 2종 추가

`GridPositionTests.cs`를 추가해 새로운 그리드 데이터의 기본 동작을 검증하도록 했다.

```text
Constructor_StoresCoordinates
→ GridPosition 생성 시 X/Z 좌표가 정확히 저장되는지 확인

PlayerRunState_StartsAtGridOrigin
→ PlayerRunState 생성 시 CurrentGridPosition이 (0, 0)인지 확인
```

테스트 코드가 Domain 타입을 직접 사용할 수 있도록 `ProjectDelta.Tests.EditMode.asmdef`에 `ProjectDelta.Domain` 참조를 추가했다.

---

## 문제 및 수정

### 6. Unity 자동 변경 파일 정리

11일차 작업 확인 과정에서 실제 개발 대상이 아닌 다음 파일의 변경 가능성을 다시 확인했다.

```text
Assets/Scenes/SampleScene.unity
Project-Delta.slnx
```

최종 변경 기준에서 `Project-Delta.slnx`는 11일차 변경에서 제외됐다.

기본 `SampleScene`은 Project Delta의 Build Settings에 등록되어 있지 않고 실제 게임 씬도 `Assets/ProjectDelta/Scenes` 아래에서 관리하고 있으므로, 재생성·추적 혼선을 없애기 위해 다음 두 파일을 삭제했다.

```text
Assets/Scenes/SampleScene.unity
Assets/Scenes/SampleScene.unity.meta
```

Build Settings의 기존 5개 게임 씬에는 영향이 없다.

---

## 현재 11일차 전체 흐름

```text
GridPosition 도메인 좌표 정의
↓
PlayerRunState.CurrentGridPosition 연결
↓
DungeonScene에 GridOrigin과 단일 TestRoom 구성
↓
Player를 월드 원점에 배치
↓
Main Camera를 Player 자식으로 구성
↓
GridPosition EditMode 테스트 2종 추가
↓
불필요한 SampleScene 정리
↓
12일차 시점 기준 WASD 한 칸 이동 구현 준비 완료
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/GridPosition.cs
Assets/ProjectDelta/Scripts/Domain/GridPosition.cs.meta
Assets/ProjectDelta/Tests/EditMode/GridPositionTests.cs
Assets/ProjectDelta/Tests/EditMode/GridPositionTests.cs.meta
Devlogs/Day11/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scenes/DungeonScene.unity
Assets/ProjectDelta/Scripts/Domain/PlayerRunState.cs
Assets/ProjectDelta/Tests/EditMode/ProjectDelta.Tests.EditMode.asmdef
```

---

## 삭제 파일

```text
Assets/Scenes/SampleScene.unity
Assets/Scenes/SampleScene.unity.meta
```

---

## 최종 확인 항목

11일차 완료 기준은 다음과 같다.

- `DungeonScene`에 `===Dungeon=== / GridOrigin / TestRoom / Player` 구조가 존재
- 테스트 방에 Floor, Ceiling, 동·서·남·북 벽이 존재
- Main Camera가 Player의 자식으로 구성됨
- `GridPosition`이 UnityEngine 타입에 의존하지 않음
- `PlayerRunState.CurrentGridPosition` 기본값이 `(0, 0)`임
- EditMode 테스트에 그리드 좌표 테스트 2종이 추가됨
- `ProjectDelta.Tests.EditMode`가 Domain 어셈블리를 참조함
- `SampleScene`은 Build Settings에 포함되어 있지 않으며 삭제 후 기존 게임 씬 5종은 유지됨
- GitHub 변경 내역에 불필요한 `Project-Delta.slnx` 수정이 남아 있지 않음

Unity Console의 컴파일 결과와 Test Runner의 실제 통과 결과는 로컬 Unity Editor에서 최종 확인한다.

---

## 다음 개발 방향

12일차에는 **시점 기준 WASD 한 칸 이동과 이동 가능 여부 검사**를 구현한다.

예정 흐름:

```text
카메라 Yaw를 4방향 이동 기준으로 변환
↓
W/A/S/D를 현재 시점 기준 상대 방향으로 변환
↓
현재 GridPosition에서 목표 GridPosition 계산
↓
목표 칸 이동 가능 여부 확인
↓
통과 가능하면 플레이어를 정확히 한 칸 이동
↓
후속 벽·문 판정 시스템과 연결할 수 있는 구조 확보
```

마우스 자유 시점 자체와 맵의 플레이어 방향 화살표는 위치 데이터와 분리된 방향 상태로 확장한다.
