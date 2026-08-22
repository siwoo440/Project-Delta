# Project Delta - 17일차 개발일지

## 개발 주제

**한 칸/방 이동 위치 보간과 이동 잠금 구현 (16일차 복구 포함)**

작업을 시작하기 전 GitHub 최신 커밋(`e27ed8b`, "16일차 : 이동 입력 잠금·같은 프레임 중복 이동·상호작용 중복 방지 구현")을 확인한 결과, 실제 diff에는 `DungeonScene.unity`의 조명 위치값 한 줄만 바뀌어 있었고 이동 잠금 코드는 존재하지 않았다. 16일차 커밋 메시지와 실제 구현이 어긋나 있었던 것이다.

이동 잠금(16일차 몫)과 위치 보간(17일차 몫)은 결국 같은 코드 경로(이동 코루틴)이기 때문에, 둘을 나눠서 구현했다가 바로 갈아엎기보다 이번 일차에 함께 구현했다.

---

## 개발 목표

- WASD 한 칸 이동을 즉시 텔레포트 대신 약 0.15초 보간으로 변경
- 방 A → 방 B 경계 이동도 문을 통과하듯 부드럽게 연결
- 이동 중 추가 WASD 입력 차단 (16일차 몫)
- 이동 중 F 상호작용 차단
- 논리 그리드 위치(`PlayerRunState.CurrentGridPosition`)와 화면 위치(`Transform.position`) 분리 — 논리는 즉시 갱신, 화면만 보간
- 마우스 자유 시점 회전은 변경하지 않고 그대로 유지

---

## 구현 내용

### 1. 이동 잠금 플래그 추가

`PlayerGridMovementController`에 `isMoving` 필드와 공개 프로퍼티 `IsMoving`을 추가했다.

```text
TryMove() 진입 시
playerState == null || isMoving
→ 이동 처리 즉시 중단
```

---

### 2. 즉시 이동을 보간 코루틴으로 교체

기존 `ApplyWorldPosition()`은 `transform.position`을 즉시 대입했다. 이를 두 단계로 나눴다.

```text
CalculateWorldPosition(gridPosition)
→ 목표 월드 위치만 계산 (부수효과 없음)

ApplyWorldPosition(gridPosition)
→ Awake 초기 배치 전용, 즉시 대입 유지

MoveRoutine(targetWorldPosition)
→ 코루틴, moveDuration(0.15초)에 걸쳐 SmoothStep 보간
```

```text
MoveRoutine 흐름
isMoving = true
↓
시작 위치 저장
↓
경과 시간만큼 SmoothStep(t) 보간 반복
↓
목표 위치로 정확히 고정
↓
isMoving = false
```

SmoothStep(`t * t * (3 - 2t)`)을 적용해 시작과 끝이 느려지고 중간이 빨라지는 곡선을 만들었다.

---

### 3. 논리 위치와 화면 위치 분리

`CommitGridMove`와 `TryMoveAcrossRoomBoundary` 모두 다음 순서로 바뀌었다.

```text
playerState.CurrentGridPosition = target   // 논리 위치 즉시 갱신
↓
StartCoroutine(MoveRoutine(...))            // 화면 위치만 보간
```

문/벽 판정, 다음 이동 가능 여부 계산 등 게임 규칙은 항상 최신 논리 좌표를 기준으로 동작하고, 화면 표현만 뒤따라간다.

---

### 4. 방 경계 이동도 같은 코루틴 재사용

```text
passageController = 목적 방
roomOrigin = 목적 방 Transform
CurrentRoomId = 목적 방 ID
CurrentGridPosition = 목적 방 입구 칸
↓
MoveRoutine(CalculateWorldPosition(입구 칸))
```

`roomOrigin`을 먼저 목적 방으로 바꾼 뒤 목표 월드 위치를 계산하기 때문에, 보간 시작 위치(현재 실제 Transform, 아직 A방 공간)와 목표 위치(B방 계산 좌표)가 자연스럽게 이어져 별도의 특수 처리 없이 "문을 통과하는" 움직임이 나온다.

---

### 5. F 상호작용도 이동 중 차단

`PlayerDoorInteractionController.OnInteract` 맨 앞에 확인을 추가했다.

```text
movementController.IsMoving == true
→ 상호작용 처리 중단
```

---

### 6. 마우스 자유 시점은 변경 없음

`PlayerLookController`는 손대지 않았다. 위치 이동만 보간하고 시점 회전은 기존처럼 매 프레임 즉시 반응한다.

---

## 적용 중 발견된 문제 및 수정

### 7. 16일차 커밋 내용과 실제 구현의 불일치

위 개발 주제에 정리한 대로, 16일차 커밋(`e27ed8b`)은 이동 잠금을 구현했다고 되어 있었지만 실제로는 씬 조명 위치만 변경되어 있었다. Devlogs 폴더에도 Day16 개발일지가 없었다. 원인은 확인하지 못했지만(다른 세션에서 작업), 17일차 착수 전에 실제 코드를 먼저 만들어 이 간극을 메웠다.

---

## 현재 17일차 전체 흐름

```text
GitHub 최신 커밋 확인 → 16일차 코드 누락 발견
↓
isMoving 잠금 플래그 추가
↓
즉시 대입 방식을 MoveRoutine 코루틴으로 교체 (SmoothStep 보간)
↓
논리 위치(즉시) / 화면 위치(보간) 분리
↓
방 경계 이동도 동일 코루틴으로 통합 → 문 통과 연출 자연 발생
↓
F 상호작용 이동 중 차단 추가
↓
마우스 시점 회전은 그대로 유지
```

---

## 생성 파일

```text
Devlogs/Day17/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs
Assets/ProjectDelta/Scripts/Presentation/PlayerDoorInteractionController.cs
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

17일차 완료 기준은 다음과 같다 (16일차 몫 포함).

- Unity 컴파일 오류 없음
- WASD 한 칸 이동이 순간이동 대신 약 0.15초 동안 부드럽게 진행됨
- 이동 중 추가 WASD 입력이 무시됨
- 이동 중 F 입력이 무시됨
- 이동 완료 후 입력이 다시 허용됨
- 벽 방향 이동은 보간을 시작하지 않고 그 자리에 유지됨
- 닫힌 문 방향도 동일하게 처리됨
- TestRoom_A → TestRoom_B 경계 이동이 끊김 없이 부드럽게 연결됨
- TestRoom_B → TestRoom_A 복귀도 동일하게 동작함
- 이동 중에도 마우스 자유 시점 회전이 유지됨
- 이동 가능 칸 가이드가 계속 표시됨
- 기존 문·열쇠 상호작용이 그대로 동작함
- 논리 `CurrentGridPosition`과 이동 완료 후 최종 Transform 위치가 일치함

이번 일차는 코루틴과 씬의 실제 방 오브젝트에 의존하는 동작이라 EditMode 자동 테스트로 깔끔하게 검증하기 어려워, 위 항목은 Unity Editor에서 직접 Play하여 수동으로 확인했다.

---

## 다음 개발 방향

다음 18일차에는 **RoomDefinition / RoomInstance** 구조를 구현한다.

지금까지는 `TestRoom_A`/`TestRoom_B` 두 개의 하드코딩된 테스트 방과 `TestRoomTransitionController`의 임시 연결로 검증해왔다. 18일차부터는 이를 정식 데이터 구조로 옮긴다.

예정 흐름:

```text
RoomDefinition — 방의 정적 정의(문/벽 배치, 크기 등)
↓
RoomInstance — 런타임에 배치된 방 인스턴스 + 현재 상태
↓
TestRoomTransitionController의 하드코딩 연결을
RoomDefinition 기반 연결 데이터로 교체 준비
```

19일차 RoomView 프리팹, 20일차 정식 방 진입·이탈 관리로 이어진다.
