# Project Delta 82일차 개발일지

## 개발 정보

- 개발 일자: 2026-08-26
- 최신 커밋: `68db40940510053ea062b81e320400cb374ab620`
- 기준 커밋: `3bafde07cd097307f2e6e1e1aa7fae5f8da5daee`
- 기존 커밋 제목: `82`
- 개발 주제: 전투 직전 체크포인트 자동 저장 및 비정상 종료 후 재전투 복원

---

# 개발 목표

전투 내부의 턴·HP·상태이상·난수 상태를 실시간 저장하지 않고, 플레이어가 전투를 선택한 직후 전투 시작 전에 체크포인트를 자동 저장하도록 구조를 변경했다.

전투 도중 게임이 비정상 종료되거나 강제 종료될 경우 마지막 전투 상태를 복구하지 않고, 저장된 인카운터 체크포인트를 이용하여 같은 몬스터와의 전투를 처음부터 다시 시작하도록 구성했다.

자동 저장 성공 시 화면 왼쪽 위에 `자동 저장 되었습니다.` 메시지를 잠시 표시하고 자동으로 사라지도록 구현했다.

---

# 주요 개발 내용

## 1. 전투 직전 체크포인트 저장

`BattleEncounterCommand`에서 전투 선택이 확정되면 실제 Battle 시작 전에 `SaveBattleEncounterCheckpoint()`를 호출하도록 연결했다.

체크포인트에는 전투 내부 상태 대신 다음 정보만 저장한다.

- 방 ID
- 대표 몬스터 ID
- 몬스터 그리드 위치
- 실제 전투에 사용되는 몬스터 그룹 구성

이를 통해 전투 진행 도중에는 별도의 저장을 수행하지 않는다.

---

## 2. 체크포인트 저장 데이터 추가

`RunData`에 `BattleEncounterCheckpointData`를 추가했다.

`BattleEncounterCheckpointStore`가 체크포인트의 생성, 복원, 저장 데이터 반영, 초기화를 담당한다.

새 게임 시작, 패배, 런 포기 시에는 메모리에 남아 있는 전투 체크포인트를 제거하도록 처리했다.

---

## 3. 이어하기 시 전투 재시작

저장 파일에 전투 체크포인트가 존재하면 `ContinueGame()`에서 이를 복원한다.

`DungeonScene` 로드 후 `BattleCheckpointCoordinator`가 복원된 던전과 몬스터가 준비될 때까지 대기한 뒤 다음 정보를 검증한다.

- 저장된 방과 현재 몬스터 방 일치
- 저장된 대표 몬스터 ID 일치
- 몬스터 그리드 좌표 일치
- 몬스터 그룹 구성 및 순서 일치
- 해당 인카운터가 아직 완료되지 않았는지 확인

조건이 일치하면 기존 Encounter 흐름을 다시 시작하고 `Battle` 명령을 선택하여 전투를 처음부터 재개한다.

전투 내부의 라운드, 행동 순서, 적의 손실 HP, 상태이상, CombatRng 진행 상태는 복원하지 않는다.

---

## 4. 전투 중 저장 제외

82일차에서는 `ConfirmAttack`, `ConfirmDefend`, `ConfirmSkill`, 적 행동 처리 등 전투 행동 내부에 자동 저장을 추가하지 않았다.

따라서 전투 중 비정상 종료 시 다음과 같이 동작한다.

`인카운터 → 전투 선택 → 자동 저장 → 전투 진행 → 비정상 종료 → 이어하기 → 같은 인카운터 복원 → 전투 처음부터 시작`

플레이어 환경 문제로 전투 도중 저장이 끊기더라도 중간 전투 데이터의 불완전한 복원을 시도하지 않는다.

---

## 5. 전투 종료 후 저장

승리 후 보상 선택이 완료되면 기존 Encounter 완료 저장 흐름을 그대로 사용한다.

도주 성공 시에는 `BattleCheckpointCoordinator`가 `EncounterOutcome.Escaped` 결과를 감지한 뒤 `SaveDungeonProgress()`를 호출하여 도주가 완료된 탐험 상태를 저장한다.

패배 시에는 기존 패배 및 런 종료 흐름을 유지한다.

---

## 6. 자동 저장 알림 UI

`AutoSaveNotification` 이벤트를 추가하고 `ApplicationFlow.TryWriteDungeonProgress()`에서 저장이 완료되면 이벤트를 발생시키도록 구성했다.

`AutoSaveToastController`는 런타임에서 자동 생성되므로 Scene 또는 Inspector 수동 연결이 필요 없다.

표시 내용:

`자동 저장 되었습니다.`

표시 위치:

- 화면 왼쪽 위
- 약 1.25초 동안 표시
- 이후 약 0.35초 동안 페이드아웃
- 게임 Time Scale의 영향을 받지 않도록 실시간 기준 처리

기존 방 진입 자동 저장 등 `SaveDungeonProgress()`를 사용하는 저장에도 동일한 메시지가 표시된다.

---

# 변경 파일

## 수정

- `Assets/ProjectDelta/Scripts/Application/ApplicationFlow.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleEncounterCommand.cs`
- `Assets/ProjectDelta/Scripts/Data/RunData.cs`

## 생성

- `Assets/ProjectDelta/Scripts/Application/AutoSaveNotification.cs`
- `Assets/ProjectDelta/Scripts/Application/AutoSaveNotification.cs.meta`
- `Assets/ProjectDelta/Scripts/Data/BattleEncounterCheckpointStore.cs`
- `Assets/ProjectDelta/Scripts/Data/BattleEncounterCheckpointStore.cs.meta`
- `Assets/ProjectDelta/Scripts/Presentation/AutoSaveToastController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/AutoSaveToastController.cs.meta`
- `Assets/ProjectDelta/Scripts/Presentation/BattleCheckpointCoordinator.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleCheckpointCoordinator.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/BattleEncounterCheckpointTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleEncounterCheckpointTests.cs.meta`

총 13개 파일 변경.

---

# 테스트 추가

`BattleEncounterCheckpointTests`에 다음 EditMode 테스트를 추가했다.

1. 체크포인트 Capture 후 RunData 반영 확인
2. 저장된 체크포인트 Restore 확인
3. Clear 후 대기 체크포인트 제거 확인
4. 유효하지 않은 저장 데이터가 복원되지 않는지 확인

---

# 최종 동작 흐름

## 일반 전투

`몬스터 인카운터 → 전투 선택 → 체크포인트 자동 저장 → 자동 저장 메시지 → 전투 시작 → 전투 중 저장 없음 → 승리 → 보상 선택 → Encounter 완료 저장`

## 전투 도중 비정상 종료

`전투 직전 체크포인트 저장 → 전투 진행 → 게임 비정상 종료 → 이어하기 → Dungeon 복원 → 체크포인트 검증 → 같은 Encounter 재생성 → 같은 전투 처음부터 시작`

## 도주

`전투 → 도주 성공 → Encounter 종료 → 탐험 상태 자동 저장 → 체크포인트가 없는 정상 탐험 상태로 계속`

---

# 검증

GitHub 최신 커밋은 81일차 커밋 바로 다음 1개 커밋으로 연결되어 있으며, 81일차 대비 변경 파일은 13개다.

정적 코드 확인 기준으로 다음 흐름을 확인했다.

- 전투 선택 직전 체크포인트 저장
- 전투 내부 행동 단위 저장 미구현
- 저장 데이터에서 체크포인트 복원
- 같은 방·몬스터·위치·그룹 검증 후 재전투
- 도주 후 자동 저장
- 새 게임·패배·런 포기 시 체크포인트 초기화
- 자동 저장 성공 이벤트 및 좌상단 토스트 연결
- 체크포인트 EditMode 테스트 4개 포함

GitHub Commit Status에는 등록된 CI 결과가 없어 Unity Editor 컴파일 및 Test Runner 실제 실행 성공 여부는 GitHub만으로 확인할 수 없다.
