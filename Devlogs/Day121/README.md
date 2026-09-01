# Project Delta - 121일차 개발일지

## 작업 개요

일반 몬스터와 구분되는 "상위 개체(보스)" 개념을 만드는 날이다. 기획서가 요구한 등급·고정 능력치·보상 상향에 더해, 사용자 추가 요청으로 "보스가 있는 층은 계단이 보스 방에 생기고, 보스를 쓰러뜨려야 그 계단이 나타난다"는 규칙까지 넣었다.

핵심은 네 가지다.

1. 일반/정예/보스 등급 구분.
2. 등급별 고정 능력치 배율.
3. 등급별 보상(경험치·골드·드롭 확률) 상향.
4. 계단 방 = 보스 방, 보스를 쓰러뜨려야 계단 등장 (추가 요청).

재도전 규칙(패배 후 같은 보스 재조우)은 사용자 요청으로 빼고 진행하지 않았다.

---

## Part 1. 등급과 고정 능력치

`Assets/ProjectDelta/Scripts/Data/MonsterTier.cs`(신규), `MonsterDefinition.cs`

- `MonsterTier`(Normal/Elite/Boss)를 `MonsterDefinition`과 같은 어셈블리(Data)에 뒀다 - `RoomTypeRules`가 `RoomType`과 한 파일에 있는 것과 같은 이유로, 능력치·보상 배율 규칙(`MonsterTierRules`)이 Application을 거치지 않고 바로 쓰인다.
- 54일차부터 층 보정이나 개체 편차 자체가 없었기 때문에, 등급 배율(정예 1.3배, 보스 1.7배)이 곧 그 몬스터의 확정된 스탯이 된다 - "고정 능력치"라는 표현 그대로다. `ExplorationMonsterEncounterController.BeginTestBattle()`에서 HP/공격/방어/저항에 적용했다.
- "전용 스킬"은 새 코드가 필요 없었다 - 68일차부터 있던 `MonsterAiProfile`로 몬스터마다 이미 고유 스킬을 지정할 수 있다.

---

## Part 2. 보상 상향

`Assets/ProjectDelta/Scripts/Data/MonsterDefinition.cs`, `Assets/ProjectDelta/Scripts/Application/BattleDropService.cs`

- 경험치: `MonsterDefinition.ExperienceReward` getter가 등급 배율을 곱해서 돌려준다 - 79일차 `PlayerGrowthService`가 이 값을 그대로 합산하므로 한 곳만 고치면 됐다.
- 골드·아이템 드롭: `BattleDropService.RollBattleDrops`에서 몬스터별 등급 배율을 골드 굴림과 아이템 드롭 확률(basis points)에 곱했다.
- 기억 파편 지급 확률은 넣지 않았다 - `ProfileData`에 필드만 있고 실제로 지급하는 코드가 아직 어디에도 없다(125일차 몫). 없는 시스템을 미리 만드는 셈이라 뺐다.

---

## Part 3. 계단 방 = 보스 방 (추가 요청)

`Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs`, `ExplorationMonsterEncounterController.cs`, `Assets/ProjectDelta/Scripts/Domain/RoomType.cs`

- `RoomType`에 `Boss` 값을 추가했다(이 파일 자체 주석에 "121일차까지 값이 늘어날 예정"이라고 미리 적혀 있었다).
- `ApplyBossRoomType()` - 문 연결 직후, 몬스터를 채우기 전에 그 층의 계단 방을 항상 `RoomType.Boss`로 확정한다. 지금은 모든 층에 보스가 있는 것으로 뒀다 - 특정 층만 보스가 있게 하려면 이 메서드 하나만 조건을 걸면 된다.
- `EnsureBossRoomHasMonster()` - 계단 방은 기존 Combat 방 보장 로직(`EnsureCombatRoomsHaveMonsters`)에서 항상 제외돼 있어서(원래 몬스터가 없는 게 정상인 방이었다), 같은 배치 절차를 이 방 하나에 대해서만 따로 실행해 몬스터를 채운다.
- `PlaceRuntimeStairs()`를 고쳤다 - 계단 방이 보스 방이고 아직 안 깼으면(`RoomInstance.Completed == false`) 계단을 만들지 않고 건너뛴다.
- `ExplorationMonsterEncounterController.TryApplyEncounterResult()`에서, 몬스터를 **실제로 쓰러뜨렸을 때만**(`EncounterOutcome.MonsterDefeated`, 도망 제외) `DungeonFloorController.NotifyRoomEncounterCompleted()`를 호출해 그 순간 계단을 방 가운데(`GridPosition.Zero`, 기존 배치 위치 그대로)에 생성한다. 도망쳐서 계단을 여는 우회는 막았다.
- 저장/이어하기도 같은 순서(방 상태 복원 → 등급 강제 → 계단 배치 판정 → 몬스터 보장)를 그대로 타므로, 이미 깬 보스 방을 불러오면 계단이 바로 보이고 안 깼으면 몬스터가 다시 배치된 채 계단은 계속 숨겨진다.

---

## 테스트

- `MonsterTierRulesTests` - 등급별 능력치/보상 배율 대소 관계, 표시 이름.
- `BattleDropServiceTests`에 보스 등급 골드 2배 테스트 1개 추가.

계단 방/보스 배치 로직(`DungeonFloorController`)은 씬·프리팹에 강하게 의존하는 코드라 자동 테스트를 추가하지 않았다 - 기존에도 이 클래스를 직접 단위 테스트하는 파일이 없었다. Unity 에디터가 없는 환경이라 실제 플레이로 확인하지 못했다.
