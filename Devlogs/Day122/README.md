# Project Delta - 122일차 개발일지

## 작업 개요

121일차에 만든 "보스 등급/고정 능력치/보상"은 사실 아무 몬스터에나 Boss 등급을 다는 임시 처리였다. 122일차는 이걸 실제 보스 4종 콘텐츠와 연결하는 날이다. 추가로 사용자가 발견한 "전투 화면에 몬스터 이름이 영문 ID로 나온다"는 문제도 같이 고쳤다.

핵심은 두 가지다.

1. 보스 Context - 4종 상위 개체를 전용 방·상성·페이즈·후퇴 규칙에 연결한다.
2. 전투 참가자 이름을 실제 한글 표시 이름으로 보여준다.

---

## Part 1. 몬스터 이름 표시 버그

`Assets/ProjectDelta/Scripts/Presentation/RuntimeMonsterDefinitionLookup.cs`(신규), `BattleParticipantSlotView.cs`

원인을 확인해보니 `MonsterDefinition` 데이터 자체는 처음부터 전부 한글이었다("알라우네", "고블린 퀸" 등). 문제는 전투 화면 슬롯(`BattleParticipantSlotView.Bind`)이 `participant.DefinitionId`(예: `MON_SLIME_QUEEN`)를 그대로 이름 칸에 넣고 있었던 것이었다.

114일차 `RuntimeItemDefinitionLookup`과 완전히 같은 패턴으로 `RuntimeMonsterDefinitionLookup`을 만들어 연결했다 - `Resources.FindObjectsOfTypeAll<MonsterDefinition>()`으로 캐시를 만들고, ID/에셋명/표시명 아무거나로 찾을 수 있게 했다. 인스턴스 ID에 슬롯 번호가 붙는 경우("MON_SLIME_QUEEN#1")도 `#` 앞부분만 잘라 조회하도록 처리했다. 플레이어는 "PLAYER"라는 별도 ID라 그냥 "플레이어"로 고정했다.

---

## Part 2. 보스 4종을 전용 방·상성에 연결

`Assets/ProjectDelta/Data/Monster/Monster Definition/{SlimeQueen,GoblinQueen,Minotaur,DragonKin}.asset`, `MonsterDefinition.cs`

이미 존재하던 슬라임 퀸·고블린 퀸·미노타우르·용 수인 자산(체력이 60→70→90→110로 이미 순서가 매겨져 있었다)에 데이터를 채웠다.

- `tier: 2`(Boss)로 확정.
- 118일차에 만들어두고 비워뒀던 이벤트 전투 상성 필드에 실제 값을 넣었다 - 예를 들어 슬라임 퀸은 달래기·경청에 약하고 고백·포옹엔 강하게, 각 보스마다 성격에 맞춰 2개씩 지정했다.
- `MonsterDefinition`에 `phaseCount`(기본 1)·`canRetreat`·`canSurrenderOnly` 필드를 새로 추가했다 - 4종 전부 2페이즈, 후퇴 가능으로 설정.

`DungeonFloorController.CollectBossEncounters()`(신규)가 지금 층 번호로 4종을 순서대로 돌려가며 그 층 계단 방(=보스 방, 121일차)에 배치한다 - "전용 방과 연결"의 실제 구현이다. Boss 등급 몬스터가 하나도 없으면 기존 폴백 전체로 안전하게 되돌아간다.

---

## Part 3. 페이즈와 후퇴 규칙

`Assets/ProjectDelta/Scripts/Application/BossPhaseRule.cs`(신규), `BattleHudController.cs`, `ExplorationMonsterEncounterController.cs`

- `BossPhaseRule.GetCurrentPhase(currentHp, maxHp, phaseCount)` - 체력을 페이즈 수만큼 균등 구간으로 나눠 지금 페이즈를 계산하는 순수 함수. 2페이즈 보스는 체력 50% 이하로 내려가는 순간 2페이즈가 된다.
- 전투 화면 상단 텍스트(`BattleHudController.RefreshBattleState`)에 상대 중 보스가 있으면 "{이름} N/2페이즈"를 덧붙여 보여준다.
- **후퇴 후 재전투** - 원래는 도망(Escaped)도 승리와 똑같이 방을 "완료" 처리해 몬스터를 지워버렸다. 보스(Tier == Boss, canRetreat)에게서 도망친 경우에는 방을 비우지 않도록 `TryApplyEncounterResult`를 고쳤다. 121일차의 "승리해야만 계단이 열린다" 규칙과 맞물려서, 이기기 전까지는 몇 번이든 다시 도전할 수 있고 이긴 뒤에는(계단이 열리고 방이 완료 처리되므로) 자연스럽게 재도전이 막힌다 - "후퇴 후 재전투·승리 후 재도전 금지"를 별도 특수 규칙 없이 기존 흐름의 조합만으로 만족시켰다.

---

## 테스트

- `BossPhaseRuleTests` - 1페이즈 고정, 2페이즈 경계값(정확히 50%/51%), 체력 0일 때 마지막 페이즈, 3페이즈 저체력, maxHp 0일 때 예외 없음.

씬 UI(보스 방 배치, 페이즈 텍스트, 이름 표시)는 Unity 에디터가 없는 환경이라 실제 플레이로 확인하지 못했다.
