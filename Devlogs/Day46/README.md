# Project Delta - 46일차 개발일지

## 개발 목표

45일차에 구현한 Encounter 행동 선택 구조를 실제 탐험 진행 상태와 연결한다.

이번 일차의 핵심 목표는 다음과 같다.

- 전투·회피 선택 결과를 최종 `EncounterResult`로 분리
- 전투 결과를 방 완료 상태에 반영
- 완료된 방의 몬스터 재Encounter 방지
- 기존 던전 저장 구조를 사용해 Encounter 완료 상태 저장
- 저장 후 복원 시 완료 상태 유지
- 회피 시 방과 몬스터를 완료 처리하지 않고 탐험으로 복귀
- 테스트 종료 버튼을 행동 선택 이후에만 사용할 수 있도록 정리
- `MON_TEST.png`를 이용한 테스트 몬스터 Sprite 적용

---

## 구현 내용

### 1. EncounterOutcome 추가

Encounter 종료 결과를 표현하는 최소 결과 종류를 추가했다.

```text
MonsterDefeated
Escaped
```

현재는 실제 전투 시스템이 아직 없으므로 전투 승패 전체를 구현하지 않고,
46일차에 필요한 탐험 복귀 결과만 구분한다.

---

### 2. EncounterResult 추가

기존 `EncounterCommandResult`는 전투·회피 버튼을 선택했는지 나타내는 행동 결과였다.

46일차에서는 행동 선택 결과와 실제 Encounter 종료 결과를 분리하기 위해
`EncounterResult`를 추가했다.

보관 정보:

```text
RoomId
MonsterDefinitionId
Outcome
CompletesRoom
RemovesMonster
```

현재 규칙:

```text
MonsterDefeated
→ 방 완료
→ 몬스터 제거

Escaped
→ 방 미완료
→ 몬스터 유지
```

---

### 3. EncounterResultResolver 추가

실제 Battle 시스템이 구현되기 전까지,
45일차에서 선택한 Command를 테스트용 Encounter 결과로 변환한다.

```text
Battle
→ MonsterDefeated

Escape
→ Escaped
```

Context가 없거나 알 수 없는 Command인 경우 결과를 만들지 않는다.

이 Resolver는 이후 실제 전투 결과 시스템이 구현되면 교체할 수 있는 임시 연결 계층이다.

---

### 4. 전투 결과를 방 완료 상태에 반영

전투 결과가 `MonsterDefeated`일 경우 현재 몬스터가 속한 방의
`RoomInstance.MarkCompleted()`를 호출한다.

기존 프로젝트에는 이미 다음 저장 경로가 존재한다.

```text
RoomInstance.Completed
↓
DungeonSaveMapper
↓
RoomRunState.Completed
↓
RunData
```

따라서 Encounter 전용 완료 목록을 새로 만들지 않고
기존 방 완료 상태를 Encounter 해결 상태의 기준으로 재사용한다.

---

### 5. 몬스터 완료 상태와 화면 상태 연결

`ExplorationMonsterMarker`에서 부모 `RoomPassageController`의
현재 `RoomInstance`를 확인할 수 있도록 연결했다.

완료된 방이라면:

```text
RoomInstance.Completed == true
↓
몬스터 비활성화
↓
재Encounter 불가
```

전투 결과를 처음 적용할 때에도 방을 완료 처리한 뒤
몬스터 GameObject를 비활성화한다.

---

### 6. 완료된 방 재Encounter 방지

Encounter 시작 전에 현재 몬스터가 속한 방의 완료 상태를 확인한다.

```text
몬스터 존재
+
GameObject 활성
+
Room Completed == false
↓
Encounter 검사 진행
```

이미 해결된 방에서는 Encounter를 다시 시작하지 않는다.

이를 통해 방을 다시 방문하거나 저장 상태를 복원했을 때
같은 Encounter가 반복 실행되는 문제를 방지한다.

---

### 7. 기존 저장 시스템과 연결

새 Save 시스템을 만들지 않고 기존 `ApplicationFlow.SaveDungeonProgress()`를 사용한다.

전투 결과 적용 흐름:

```text
Battle 선택
↓
테스트 종료
↓
MonsterDefeated
↓
RoomInstance.MarkCompleted()
↓
몬스터 비활성화
↓
SaveDungeonProgress()
↓
DungeonSaveMapper.BuildFromRunContext()
↓
RoomRunState.Completed = true
↓
런 저장
```

현재 프로젝트의 기존 방 완료 저장·복원 구조를 그대로 활용하므로
중복 저장 데이터를 추가하지 않는다.

---

### 8. 회피 결과 처리

회피는 전투와 다르게 방을 완료하지 않는다.

```text
Escape 선택
↓
테스트 종료
↓
Escaped
↓
Room Completed 변경 없음
↓
몬스터 유지
↓
탐험 복귀
```

따라서 이후 플레이어가 다시 몬스터 주변으로 접근하면
새 Encounter를 시작할 수 있다.

---

### 9. Encounter 종료 순서 정리

46일차 결과 반영 흐름은 다음과 같다.

```text
Active
↓
전투 또는 회피 선택
↓
EncounterResult 생성
↓
Resolving
↓
결과 검증
↓
방·몬스터 상태 반영
↓
필요 시 저장
↓
Finished
↓
탐험 이동 잠금 복원
↓
Idle
```

결과 적용에 실패하면 정상 완료로 진행하지 않고
Encounter를 안전하게 중단하도록 구성했다.

---

### 10. 테스트 종료 버튼 활성 조건 변경

기존 TestEnd 버튼은 Active 상태에서 바로 사용할 수 있었다.

46일차에서는 전투 또는 회피 중 하나를 선택한 뒤에만
테스트 종료 버튼을 사용할 수 있도록 변경했다.

```text
Encounter 진입 직후
→ TestEnd 비활성

전투 또는 회피 선택
→ TestEnd 활성
```

따라서 선택 없이 Encounter를 임의로 종료하는 테스트 흐름을 막는다.

---

### 11. MON_TEST Sprite 적용

45일차에 준비한 Billboard 구조를 실제 이미지로 확인할 수 있도록
다음 테스트 몬스터 이미지가 추가되었다.

```text
Assets/ProjectDelta/Resources/MonsterSprites/MON_TEST.png
```

Unity Import 설정은 Sprite 타입으로 저장되어 있으며:

```text
Texture Type = Sprite
Sprite Mode = Single
Alpha Is Transparency = 활성
Mip Map = 비활성
```

상태로 확인된다.

`MonsterDefinitionId = MON_TEST`와 파일 이름이 일치하므로
45일차의 `Resources.Load<Sprite>()` 기반 Billboard 로더가 해당 이미지를 사용할 수 있다.

---

## 46일차 전체 동작 흐름

### 전투 선택

```text
몬스터 주변 접근
↓
Encounter 시작
↓
Battle 선택
↓
중복 행동 선택 잠금
↓
TestEnd 활성
↓
테스트 종료
↓
EncounterResult = MonsterDefeated
↓
RoomInstance.Completed = true
↓
몬스터 비활성화
↓
던전 진행 저장
↓
탐험 복귀
↓
같은 방 재진입
↓
Encounter 재실행 안 함
```

### 회피 선택

```text
몬스터 주변 접근
↓
Encounter 시작
↓
Escape 선택
↓
TestEnd 활성
↓
테스트 종료
↓
EncounterResult = Escaped
↓
Room Completed 유지
↓
몬스터 유지
↓
탐험 복귀
↓
다시 접근 가능
```

---

## 테스트 추가

### EncounterResultResolverTests

다음 항목을 검증하도록 테스트를 추가했다.

- Battle → `MonsterDefeated`
- Battle 결과는 방 완료 처리 대상
- Battle 결과는 몬스터 제거 대상
- Escape → `Escaped`
- Escape 결과는 방 완료 대상이 아님
- Escape 결과는 몬스터 제거 대상이 아님
- Context 누락 시 결과 생성 실패
- 알 수 없는 Command 시 결과 생성 실패

### EncounterRoomCompletionSaveTests

기존 던전 저장 구조와 Encounter 완료 상태 연결을 검증한다.

- `RoomInstance.MarkCompleted()` 후 저장
- 저장된 `RoomRunState.Completed == true`
- `DungeonSaveMapper.BeginRestore()` 후 완료 상태 유지

---

## 이번 일차에서 제외한 내용

다음 내용은 아직 구현하지 않는다.

- 실제 BattleContext
- 플레이어와 몬스터 턴 순서
- 공격·방어 계산
- HP 감소
- 실제 전투 승패 판정
- 실제 회피 확률 계산
- 전투 보상
- 경험치 및 드롭
- 복수 몬스터 Encounter
- 몬스터 AI
- 벽·Line of Sight 기반 감지
- 전투용 별도 캐릭터 연출

현재 Battle 선택은 실제 전투를 수행하는 것이 아니라
46일차 저장·복귀 흐름을 검증하기 위한 테스트용 `MonsterDefeated` 결과로 변환한다.

---

## 변경 파일

45일차 완료 커밋과 비교해 최신 커밋에서 총 16개 파일이 변경되었다.

### 생성

- `Assets/ProjectDelta/Resources/MonsterSprites/MON_TEST.png`
- `Assets/ProjectDelta/Resources/MonsterSprites/MON_TEST.png.meta`
- `Assets/ProjectDelta/Scripts/Application/EncounterOutcome.cs`
- `Assets/ProjectDelta/Scripts/Application/EncounterOutcome.cs.meta`
- `Assets/ProjectDelta/Scripts/Application/EncounterResult.cs`
- `Assets/ProjectDelta/Scripts/Application/EncounterResult.cs.meta`
- `Assets/ProjectDelta/Scripts/Application/EncounterResultResolver.cs`
- `Assets/ProjectDelta/Scripts/Application/EncounterResultResolver.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/EncounterResultResolverTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/EncounterResultResolverTests.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/EncounterRoomCompletionSaveTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/EncounterRoomCompletionSaveTests.cs.meta`

### 수정

- `Assets/ProjectDelta/Scripts/Presentation/EncounterPanelController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterMarker.cs`
- `Project-Delta.slnx`

### 삭제

없음.

`Project-Delta.slnx` 변경은 프로젝트 파일 갱신에 따른 1줄 교체 수준의 변경이다.

---

## 최신 커밋 확인

확인한 최신 `main` 커밋:

```text
SHA
3f1ed81a4a764bb65940fc223abf98280115a8e7

현재 커밋 메시지
46
```

이전 커밋:

```text
11b579fab4113fa03b5e36482f1639ea3377f353
45일차 : 인카운터 행동 제어·8방향 포착 및 몬스터 빌보드 외형 기반 구현
```

GitHub 비교 결과:

```text
ahead_by = 1
behind_by = 0
total_commits = 1
변경 파일 = 16개
```

저장소 정적 검토에서는 다음 구조가 확인되었다.

- `EncounterOutcome` 추가
- `EncounterResult` 추가
- Battle / Escape 테스트 결과 변환 구조 추가
- 방 완료 상태를 기존 `RoomInstance.Completed`에 연결
- 전투 결과 시 던전 진행 저장 호출
- 완료된 방의 몬스터 재Encounter 방지
- 회피 결과는 방을 완료하지 않음
- 행동 선택 후 TestEnd 버튼 활성화
- 저장·복원 검증용 EditMode 테스트 추가
- `MON_TEST.png` Sprite 리소스 추가
- Sprite Import 설정에서 Sprite / Single / Alpha Transparency / Mipmap 비활성 확인

GitHub Combined Status와 Workflow Runs에는 실행 기록이 없다.

따라서 이 개발일지는 저장소 코드와 커밋 구조에 대한 정적 검토를 기준으로 작성한다.
Unity Editor 실제 컴파일 성공과 EditMode Test Runner 전체 통과 여부는 GitHub에서 확인할 수 없다.

---

## 46일차 결과

45일차까지는 Encounter를 시작하고 전투·회피 행동을 선택하는 단계까지 구현되어 있었다.

46일차에서는 그 선택을 최종 Encounter 결과로 변환하고,
전투 결과를 방 완료 상태와 기존 저장 데이터에 연결했다.

전투로 해결된 방은 `Completed` 상태가 저장되며,
완료된 방의 몬스터는 다시 Encounter를 발생시키지 않는다.

회피는 방을 완료하지 않으므로 몬스터를 유지한 채 탐험으로 돌아갈 수 있다.

또한 45일차에 준비한 몬스터 Billboard 구조에 실제 `MON_TEST.png`가 추가되어
테스트 몬스터의 2D Sprite 표시를 확인할 수 있는 리소스 상태까지 준비되었다.

다음 단계에서는 이 Encounter 결과 구조 위에 실제 전투 상태와 턴 진행 로직을 연결한다.
