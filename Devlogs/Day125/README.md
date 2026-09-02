# 125일차 : 기억의 조각(영구 성장 재화) 지급 시스템

## 목표
- `ProfileData.PermanentGrowth`에 이미 정의만 돼 있던 `MemoryShards`/`TotalMemoryShardsEarned`를
  실제로 채워주는 지급 로직 신설
- 지급·누적·로비 화면 표시까지만 구현 (조각을 소비하는 영구 강화 상점은 범위 밖 - 추후 별도 일차)

## 구현 내용

### 1. 몬스터별 조각 보상치
- `MonsterDefinition`에 `memoryShardReward` 필드 신설(기본값 1)
- `ExperienceReward`와 같은 방식으로 `MonsterTierRules.GetRewardMultiplier(tier)`를 곱해
  보스·정예가 일반 몬스터보다 더 많은 조각을 주도록 함
- 기존 몬스터 에셋은 별도 수정 없이 기본값 1이 자동 적용됨

### 2. 처치 목록 → 조각 환산
- `PlayerGrowthService.CalculateMemoryShards()` 신설 - 기존 `CalculateBattleExperience()`와
  동일한 합산 패턴

### 3. 지급 시점 연결
- `ExplorationMonsterEncounterController.ApplyVictoryGrowth()`가 경험치를 적용한 직후
  `ApplyMemoryShardGrowth()`를 호출
- `ApplicationFlow.ReadOrCreateProfile()`로 프로필을 읽어
  `PermanentGrowth.MemoryShards`/`TotalMemoryShardsEarned`,
  `LifetimeStats.TotalMemoryShardsCollected`/`MonstersDefeated`를 갱신하고 `WriteProfile()`로
  즉시 저장 (119일차 `EventBattleController`가 프로필을 저장하던 방식과 동일한 경로 재사용)
- 추후 보상 화면에서 쓸 수 있도록 `LastMemoryShardsEarned` 프로퍼티도 함께 노출

### 4. 로비 화면에 보유량 표시
- `LobbySceneController`가 씬 진입 시(`OnEnable`) 프로필을 한 번 읽어 "기억의 조각 N"을
  제목 아래에 표시 (전투는 던전에서만 일어나므로 로비에 머무는 동안은 다시 읽지 않음)
