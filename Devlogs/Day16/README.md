# Project Delta - 16일차 개발일지

## 개발 주제

**이동 입력 잠금·같은 프레임 중복 이동·상호작용 중복 방지 구현**

이 개발일지는 뒤늦게 작성됐다. GitHub에는 "16일차 : 이동 입력 잠금·같은 프레임 중복 이동·상호작용 중복 방지 구현"이라는 제목으로 커밋(`e27ed8b`)이 이미 올라가 있었지만, 실제 diff를 확인해보니 `DungeonScene.unity`의 조명 위치값 한 줄만 바뀌어 있었고 이동 잠금 코드도, 개발일지도 존재하지 않았다. 17일차 작업을 시작하기 전 이 간극을 먼저 메웠다.

---

## 개발 목표

- 이동 처리 중에는 추가 WASD 입력을 무시하는 잠금 상태 도입
- 같은 프레임에 여러 이동이 겹쳐 처리되지 않도록 방지
- 이동 중 F 상호작용도 함께 차단

---

## 구현 내용

### 1. 이동 잠금 플래그

`PlayerGridMovementController`에 `isMoving` 필드와 공개 프로퍼티 `IsMoving`을 추가했다.

```text
TryMove() 진입 시
playerState == null || isMoving
→ 이동 처리 즉시 중단
```

이동이 시작되면 잠기고, 끝나면 풀리는 구조라 같은 프레임은 물론 이동이 끝나기 전까지 들어오는 모든 추가 WASD 입력이 무시된다.

---

### 2. 상호작용 중복 방지

`PlayerDoorInteractionController.OnInteract` 맨 앞에 이동 잠금 확인을 추가했다.

```text
movementController.IsMoving == true
→ 상호작용 처리 중단
```

이동 중에 F를 눌러 문을 열거나 열쇠를 소모하는 것을 막아, 이동과 상호작용이 같은 프레임에 겹치는 상황을 방지한다.

---

### 3. 실제 잠금 유지 시간은 17일차에서 결정됨

이 시점에는 이동 자체가 여전히 즉시 텔레포트였기 때문에, 잠금이 유지되는 시간은 사실상 한 프레임 정도였다. 이동을 실제 보간 시간(0.15초)만큼 걸리게 만드는 작업은 17일차 몫이며, `isMoving`을 켜고 끄는 지점 자체는 17일차의 이동 코루틴(`MoveRoutine`)이 그대로 이어받아 사용한다.

```text
16일차: isMoving 플래그와 검사 지점만 존재 (잠금 시간 ≈ 1프레임)
↓
17일차: MoveRoutine 코루틴이 같은 플래그를 실제 이동 시간(0.15초) 동안 유지
```

---

## 적용 중 발견된 문제 및 수정

### 4. 커밋 메시지와 실제 코드의 불일치

원래 `e27ed8b` 커밋은 이 기능을 구현했다고 되어 있었지만 실제로는 코드가 없었다. 원인은 확인할 수 없었다(다른 세션에서 작업된 커밋). 17일차 작업 착수 전에 실제 코드와 이 개발일지로 간극을 메웠다.

---

## 현재 16일차 전체 흐름

```text
GitHub 최신 커밋(e27ed8b) 확인 → 코드 누락 발견
↓
isMoving 잠금 플래그 추가
↓
TryMove() 최상단에서 잠금 확인
↓
PlayerDoorInteractionController에도 동일 확인 추가
↓
실제 이동 보간(0.15초 유지)은 17일차에서 이어서 구현
```

---

## 생성 파일

```text
Devlogs/Day16/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs (isMoving 필드·검사)
Assets/ProjectDelta/Scripts/Presentation/PlayerDoorInteractionController.cs (이동 중 상호작용 차단)
```

실제 코드 변경은 17일차 커밋에 함께 포함되어 있다 — 이동 잠금과 위치 보간이 같은 코루틴 안에서 구현되어 분리하지 않았다.

---

## 삭제 파일

없음.

---

## 최종 확인 항목

16일차 완료 기준은 다음과 같다.

- `isMoving`이 이동 시작 시 true, 종료 시 false로 전환됨
- 이동 중 추가 WASD 입력이 무시됨
- 이동 중 F 입력이 무시됨
- 이동 완료 후 입력이 다시 허용됨

---

## 다음 개발 방향

다음 17일차에는 **한 칸/방 이동 위치 보간**을 구현한다 — `isMoving`이 실제 이동 시간(0.15초) 동안 유지되도록 즉시 텔레포트를 코루틴 기반 SmoothStep 보간으로 교체하는 작업이다. (이미 완료되어 [Devlogs/Day17](../Day17/README.md)에 기록되어 있다.)
