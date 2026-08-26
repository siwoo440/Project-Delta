# Project Delta 83일차 개발일지

## 개발 정보

- 개발 일자: 2026-08-26
- 최신 커밋: `627b39386ee8d0a1e1ad635ef71180488e164fca`
- 기준 커밋: `11552fee0d5d0b74f40c00333ab8bd4bd606c61a`
- 현재 커밋 제목: `83`
- 개발 주제: 도주 성공 인카운터 완료, 플레이어 상태 유지, 탐험 이동 기반 상태이상 처리 및 도주 버튼 연결

---

# 개발 목표

도주 성공을 재전투를 위한 후퇴 상태로 사용하지 않고, 보상 없이 현재 인카운터를 완료하는 방식으로 변경했다.

도주 성공 시 몬스터는 제거하고 플레이어가 전투에서 잃은 HP·MP·정력과 남아 있는 상태이상은 그대로 유지한다.

도주 후 탐험에 남은 상태이상은 플레이어가 실제로 이동할 때마다 한 번씩 적용하고 남은 지속 횟수를 감소시키도록 구성했다.

기존 전투 HUD에서 비활성화되어 있던 도주 버튼을 실제 `ConfirmFlee()` 처리와 연결하여 전투 중 사용할 수 있도록 수정했다.

---

# 주요 개발 내용

## 1. 도주 성공 인카운터 완료

`EncounterResult`의 `Escaped` 결과가 다음 조건을 만족하도록 변경했다.

- 현재 인카운터 완료
- 현재 몬스터 제거
- 승리 보상 없음
- 탐험 상태로 복귀
- 완료 상태 자동 저장

즉 도주한 몬스터와 다시 전투하는 재전투 구조는 사용하지 않는다.

---

## 2. 플레이어 전투 상태 유지

도주 성공 시 전투 참가자가 가지고 있던 현재 자원 상태를 런 상태에 유지한다.

유지 대상:

- 현재 HP
- 현재 Mana
- 현재 Stamina
- 유지 가능한 상태이상

전투에서 받은 피해나 사용한 자원은 도주했다고 자동 회복되지 않는다.

---

## 3. 상태이상 지속 데이터 보존

기존 상태 ID 목록만으로는 남은 지속시간과 중첩을 복원할 수 없기 때문에, 상태이상별 실제 런 상태 데이터를 추가했다.

저장 항목:

- DefinitionId
- SourceInstanceId
- RemainingDuration
- StackCount
- AppliedValue
- EffectKind
- TargetStat

`ExtraAction`처럼 전투 라운드에서만 의미가 있는 효과는 탐험으로 넘기지 않는다.

---

## 4. 도주 후 탐험 상태이상 처리

`ExplorationStatusEffectService`를 추가하여 도주 후 남은 상태이상을 탐험 이동 단위로 처리한다.

### 지속 피해

성공한 이동 1회마다 피해를 적용하고 남은 지속 횟수를 1 감소시킨다.

예시:

`중독 3회 → 이동 → 2회 → 이동 → 1회 → 이동 → 제거`

### 지속 회복

성공한 이동 1회마다 회복을 적용하고 지속 횟수를 감소시킨다.

### 기절

기절 상태에서는 이동 입력 자체를 한 번 소비한다.

`기절 2회 → 이동 입력 실패/1회 남음 → 이동 입력 실패/제거 → 다음 입력부터 정상 이동`

기절 때문에 이동하지 못한 입력에서는 다른 중독·재생 상태는 별도로 틱하지 않는다.

### 일반 지속 효과

탐험에서 직접 수치 효과를 적용하지 않는 상태도 실제 이동이 성공하면 지속 횟수를 감소시킨다.

---

## 5. 방 이동과 한 칸 이동 연동

`PlayerGridMovementController`에 탐험 상태이상 틱을 연결했다.

- 방 내부 한 칸 이동 성공 → 상태이상 1틱
- 다음 방으로 이동 성공 → 상태이상 1틱
- 벽이나 닫힌 통로 때문에 이동 실패 → 상태이상 소모 없음
- 입력 잠금 중 이동 실패 → 상태이상 소모 없음
- 기절 → 이동 자체는 실패하고 기절만 1회 소모

방 이동 시에는 상태이상 틱을 먼저 반영한 후 자동 저장한다.

---

## 6. 저장 데이터 확장

`RunData`와 `DungeonSaveMapper`를 확장하여 도주 후 상태를 저장 파일에도 유지하도록 했다.

추가 저장 대상:

- CurrentHealth
- CurrentMana
- CurrentStamina
- 지속 상태이상 상세 데이터

이를 통해 다음 흐름을 지원한다.

`도주 → 자동 저장 → 게임 종료 → 이어하기 → 도주 당시 HP·자원·상태이상 복원`

---

## 7. 다음 전투 상태이상 복원

도주 후 탐험에 남아 있던 상태이상이 모두 소모되기 전에 새로운 전투에 진입하면 해당 상태를 새로운 플레이어 `BattleParticipant`에 다시 적용한다.

전투 시작 후에는 런 상태의 임시 복사본을 비워 중복 적용을 방지한다.

승리나 패배로 전투가 정상 종료되면 기존 전투 종료 규칙에 따라 상태이상을 정리한다.

---

## 8. 도주 완료 저장 중복 제거

82일차 `BattleCheckpointCoordinator`에는 `Escaped` 결과를 감시해 별도로 저장하는 로직이 있었다.

83일차에서는 도주가 인카운터 완료로 직접 처리되면서 기존 Encounter 완료 저장 흐름을 사용하므로 별도 도주 저장 감시를 제거했다.

이를 통해:

- 동일 도주 결과 이중 저장 방지
- `자동 저장 되었습니다.` 메시지 중복 표시 방지

를 처리했다.

---

## 9. 도주 버튼 작동 수정

기존 `BattleHudController`는 `actionButtons` 배열의 버튼을 매 프레임 전부 비활성화하고 있었으며, 공격·방어 버튼과 달리 도주 버튼에는 클릭 이벤트가 연결되어 있지 않았다.

83일차에서 다음을 수정했다.

- 도주 버튼 자동 탐색
- `OnFleeButtonClicked()` 추가
- `ExplorationMonsterEncounterController.ConfirmFlee()` 연결
- 플레이어 `AwaitingAction` 차례에서 도주 버튼 활성화
- 적 행동 중에는 도주 버튼 비활성화
- HUD 종료 시 도주 버튼 이벤트 해제

`BattleHudActionButtonResolver`가 다음 순서로 도주 버튼을 찾는다.

1. 명시적으로 지정된 `fleeButton`
2. 버튼 이름 또는 자식 Text의 `도주`
3. `Flee`
4. `Escape`
5. 기존 HUD 배열 구조의 3번째 버튼 fallback

Scene 또는 Inspector를 별도로 수정하지 않아도 기존 HUD 구조에서 사용할 수 있도록 구성했다.

---

# 테스트

## Day83EscapeStatusPersistenceTests

다음 동작을 검증하는 EditMode 테스트를 추가했다.

- 도주 시 상태이상 보존
- 전투 전용 추가 행동 상태 제외
- 다음 전투 진입 시 상태 복원
- 탐험 이동 시 지속 피해 적용
- 지속시간 만료 후 상태 제거
- 기절 이동 입력 소비
- 도주 결과의 인카운터 완료 및 몬스터 제거
- 현재 HP·Mana·Stamina 및 상태이상 저장·복원

## BattleHudActionButtonResolverTests

도주 버튼 탐색 로직 테스트를 추가했다.

- `도주` 라벨 버튼 탐색
- 기존 actionButtons 3번째 버튼 fallback
- 명시적 fleeButton 우선 적용

## EncounterResultResolverTests

기존 도주 테스트의 기대값을 83일차 규칙에 맞게 변경했다.

이전:

`Escaped → CompletesRoom false / RemovesMonster false`

변경:

`Escaped → CompletesRoom true / RemovesMonster true`

---

# 변경 파일

82일차 기준 총 19개 파일이 변경되었다.

## 수정

- `Assets/ProjectDelta/Scripts/Application/BattleSession.cs`
- `Assets/ProjectDelta/Scripts/Application/EncounterResult.cs`
- `Assets/ProjectDelta/Scripts/Data/DungeonSaveMapper.cs`
- `Assets/ProjectDelta/Scripts/Data/RunData.cs`
- `Assets/ProjectDelta/Scripts/Domain/PlayerRunState.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleCheckpointCoordinator.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs`
- `Assets/ProjectDelta/Tests/EditMode/EncounterResultResolverTests.cs`

## 생성

- `Assets/ProjectDelta/Scripts/Application/ExplorationStatusEffectService.cs`
- `Assets/ProjectDelta/Scripts/Application/ExplorationStatusEffectService.cs.meta`
- `Assets/ProjectDelta/Scripts/Application/PersistentPlayerStatusService.cs`
- `Assets/ProjectDelta/Scripts/Application/PersistentPlayerStatusService.cs.meta`
- `Assets/ProjectDelta/Scripts/Presentation/BattleHudActionButtonResolver.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleHudActionButtonResolver.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/BattleHudActionButtonResolverTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleHudActionButtonResolverTests.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/Day83EscapeStatusPersistenceTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/Day83EscapeStatusPersistenceTests.cs.meta`

삭제 파일은 없다.

---

# 최종 동작 흐름

## 도주 성공

`전투 → 도주 선택 → 확률 판정 성공 → 플레이어 HP/MP/SP 유지 → 상태이상 보존 → 몬스터 제거 → 인카운터 완료 → 자동 저장 → 탐험 복귀`

승리 보상은 지급하지 않는다.

## 도주 실패

`도주 선택 → 확률 판정 실패 → 해당 행동 소비 → 다음 행동자로 진행`

## 탐험

`실제 이동 성공 → 남은 상태이상 1회 적용 → 지속 횟수 -1 → 0이면 제거`

## 다음 전투

`탐험 상태이상 남음 → 전투 시작 → 상태이상 BattleParticipant에 복원 → 전투 진행`

## 게임 비정상 종료

82일차 규칙은 그대로 유지한다.

`전투 중 비정상 종료 → 전투 직전 체크포인트 → 같은 인카운터 전투 처음부터 재시작`

---

# 검증 상태

최신 `main`은 82일차 커밋에서 1개 커밋 앞선 83일차 커밋이다.

최신 커밋에서 다음 사항을 소스 기준으로 확인했다.

- 도주 결과가 인카운터 완료 및 몬스터 제거로 변경됨
- 도주 상태이상 보존 서비스 포함
- 탐험 이동 상태이상 처리 서비스 포함
- 플레이어 현재 HP·Mana·Stamina 저장 확장
- 상세 상태이상 저장·복원 구조 포함
- 도주 완료 중복 저장 제거
- 도주 버튼 `ConfirmFlee()` 연결
- 플레이어 행동 차례에서 도주 버튼 활성화
- 기존 실패 테스트 기대값 갱신
- 도주 버튼 탐색 테스트 추가

GitHub Commit Status에는 등록된 CI 결과가 없다.

따라서 GitHub 소스 정적 확인 기준으로 차단할 만한 문제는 확인되지 않았지만, Unity Editor 실제 컴파일 및 전체 Test Runner 통과 여부는 로컬 Unity 실행 결과를 기준으로 최종 확인해야 한다.
