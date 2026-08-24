# Project Delta - 43일차 개발일지

## 개발 목표

42일차의 단순 `IsActive` 기반 Encounter 세션을 정식 생명주기 상태 머신으로 확장한다.

이번 일차에서는 실제 전투 계산을 추가하지 않고, Encounter가 시작되고 진행되고 정리된 뒤 탐험으로 복귀하는 흐름을 명확한 상태와 전환 규칙으로 분리한다.

- `EncounterState` 추가
- `EncounterContext` 추가
- `Idle → Starting → Active → Resolving → Finished → Idle` 상태 머신 구현
- 잘못된 상태 전환 차단
- 기존 GridPosition 기반 몬스터 접촉 흐름 유지
- Encounter 시작 시 탐험 입력 잠금
- Active 상태에서만 테스트 OnGUI 표시
- 테스트 종료 시 Resolving / Finished 단계를 거쳐 탐험 복귀
- 비정상 종료용 ForceReset 처리
- 상태 전환 EditMode 테스트 확장

---

## 구현 내용

### 1. EncounterState 추가

Encounter의 현재 단계를 bool 값 하나로 표현하지 않고 다음 5개 상태로 구분하도록 변경했다.

```text
Idle
Starting
Active
Resolving
Finished
```

각 상태의 역할은 다음과 같다.

```text
Idle
- Encounter 없음
- 정상 탐험 상태

Starting
- 플레이어와 몬스터 접촉 확인 완료
- Encounter Context 생성
- 탐험 입력 잠금 준비

Active
- Encounter 진행 상태
- 현재 테스트 OnGUI가 표시되는 단계

Resolving
- Encounter 결과 처리 단계
- 현재는 테스트 몬스터 비활성화 처리

Finished
- 결과 처리가 끝난 상태
- 탐험 복귀 준비
```

### 2. EncounterContext 추가

현재 Encounter가 어디에서 어떤 몬스터와 시작되었는지 보관하기 위해 `EncounterContext`를 추가했다.

현재 보관 정보:

```text
RoomId
MonsterDefinitionId
MonsterGridPosition
```

42일차에서 Player와 Monster의 RoomId / GridPosition을 비교해 접촉을 판정한 뒤, 성공한 몬스터의 정보를 Context에 보관한다.

### 3. ExplorationEncounterSession 상태 머신 확장

기존 42일차 `ExplorationEncounterSession`의 단순 Active 구조를 상태 머신으로 변경했다.

정상 전환 순서:

```text
TryBegin()
Idle → Starting

TryActivate()
Starting → Active

TryBeginResolve()
Active → Resolving

TryFinish()
Resolving → Finished

TryReset()
Finished → Idle
```

각 전환 메서드는 현재 상태가 올바른 경우에만 성공한다.

따라서 다음과 같이 중간 단계를 건너뛰는 전환은 허용하지 않는다.

```text
Idle → Active
Idle → Resolving
Starting → Finished
Active → Starting
```

### 4. Encounter 시작 Context 생성

플레이어와 몬스터가 다음 조건을 만족하면 Encounter 시작을 요청한다.

```text
Player.RoomId == Monster.RoomId
Player.GridPosition == Monster.GridPosition
```

성공하면 `EncounterContext`를 생성하고 상태를 `Starting`으로 변경한다.

RoomId 또는 MonsterDefinitionId가 없거나, 방 또는 GridPosition이 서로 다르면 `Idle` 상태를 유지한다.

### 5. 기존 42일차 접촉 흐름 유지

`ExplorationMonsterEncounterController`는 기존과 동일하게 플레이어 이동 완료 시점에 현재 방의 `SpawnedMonsters`를 조회한다.

```text
플레이어 이동 완료
↓
현재 RoomId의 Monster 조회
↓
RoomId 비교
↓
GridPosition 비교
↓
접촉 성공
↓
Encounter Starting
```

Collider의 `OnTriggerEnter`나 물리 충돌을 Encounter 시작 기준으로 사용하지 않는다.

### 6. Starting 단계 처리

Encounter 시작에 성공하면 다음 처리를 수행한다.

```text
activeMonster 저장
↓
Player 이동 입력 잠금
↓
마우스 커서를 UI 상태로 전환
↓
Starting
↓
Active 전환
```

현재는 별도의 페이드나 카메라 연출이 없으므로 `Starting`에서 바로 `Active`로 이동한다.

상태 자체는 이후 전투 화면 전환이나 연출을 넣을 수 있도록 분리해 둔다.

### 7. Active 상태 테스트 UI

OnGUI 테스트 Encounter는 `EncounterState.Active` 상태에서만 표시한다.

표시 내용:

```text
ENCOUNTER

State : Active
Monster : MON_TEST

전투 인카운터가 진행 중입니다.

[테스트 종료]
```

이를 통해 현재 Encounter 상태를 화면에서 직접 확인할 수 있다.

### 8. Resolving / Finished 결과 처리

`[테스트 종료]` 버튼을 누르면 즉시 Idle로 돌아가지 않고 다음 순서를 거친다.

```text
Active
↓
Resolving
↓
현재 테스트 몬스터 비활성화
↓
Finished
↓
탐험 입력 및 커서 복구
↓
Idle
```

현재 `Resolving` 단계에서는 테스트 몬스터 비활성화만 처리한다.

향후 실제 전투 시스템이 생기면 이 단계에서 승리·패배·회피·보상·방 상태 반영 등의 결과를 연결할 수 있다.

### 9. ForceReset 안전 초기화

Controller가 비활성화되거나 Encounter 도중 예상치 못한 중단이 발생해도 탐험 입력 잠금이나 Encounter Context가 남지 않도록 `ForceReset()`을 추가했다.

```text
Context = null
State = Idle
```

Controller 종료 시에는 탐험 입력과 커서를 복구한 뒤 ForceReset을 수행한다.

### 10. EditMode 테스트 확장

`ExplorationEncounterSessionTests`를 43일차 상태 머신 기준으로 수정했다.

현재 테스트는 총 10개다.

1. 새 Session이 Idle / Context 없음 상태로 시작
2. 같은 방·같은 GridPosition 접촉 시 Starting 및 Context 생성
3. 다른 방 또는 다른 위치에서는 Idle 유지
4. Starting에서만 Active 전환 허용
5. Active에서만 Resolving 전환 허용
6. Resolving에서만 Finished 전환 허용
7. Finished에서만 Idle Reset 허용 및 Context 제거
8. Idle이 아닌 상태에서 중복 Encounter 시작 차단
9. ForceReset으로 어떤 상태에서도 Idle 복귀
10. 필수 ID가 없을 때 Encounter 시작 차단

---

## 43일차 동작 흐름

```text
탐험
↓
Idle
↓
몬스터 접촉
↓
Starting
- EncounterContext 생성
- 이동 입력 잠금
- 커서 UI 상태
↓
Active
- 테스트 Encounter OnGUI 표시
↓
[테스트 종료]
↓
Resolving
- 테스트 몬스터 비활성화
↓
Finished
- 탐험 제어 복구
↓
Idle
```

---

## 변경 파일

### 생성

- `Assets/ProjectDelta/Scripts/Application/EncounterContext.cs`
- `Assets/ProjectDelta/Scripts/Application/EncounterContext.cs.meta`
- `Assets/ProjectDelta/Scripts/Application/EncounterState.cs`
- `Assets/ProjectDelta/Scripts/Application/EncounterState.cs.meta`

### 수정

- `Assets/ProjectDelta/Scripts/Application/ExplorationEncounterSession.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Assets/ProjectDelta/Tests/EditMode/ExplorationEncounterSessionTests.cs`

### 삭제

- 없음

---

## 최신 커밋 확인

확인한 최신 커밋:

- SHA: `04679ba6a534e94ab91e326cc77fa14dd3c5031a`
- 현재 커밋 메시지: `a`
- 이전 커밋: `46f97226eae37d803536972dcc5ba645535cc65f`
- 이전 커밋 메시지: `42일차 : GridPosition 기반 몬스터 접촉 및 Encounter 진입 구현`

최신 커밋은 42일차보다 정확히 1개 커밋 앞선 상태이며, 43일차 작업으로 7개 파일이 변경되었다.

GitHub 변경 내역을 확인한 범위에서는 43일차 목표와 충돌하는 명확한 구조적 문제는 확인되지 않았다.

다만 해당 커밋에는 GitHub CI 상태와 GitHub Actions 실행 기록이 없으므로 실제 Unity 컴파일 성공 및 EditMode Test Runner 통과 여부는 저장소 정보만으로 확인할 수 없다.

---

## 43일차 결과

42일차의 단순 Encounter Active 여부가 명시적인 생명주기 상태 머신으로 확장되었다.

현재 Encounter는 `Idle → Starting → Active → Resolving → Finished → Idle` 순서로만 정상 전환하며, Encounter Context가 현재 방·몬스터·GridPosition 정보를 유지한다.

현재는 테스트 OnGUI와 테스트 몬스터 비활성화만 사용하며 실제 공격·턴·데미지·보상 시스템은 포함하지 않는다.

다음 단계에서는 이 상태 머신 위에 Encounter 화면, 대상 정보, 행동 선택과 같은 실제 인카운터 UI 및 Command 입력 구조를 연결할 수 있다.
