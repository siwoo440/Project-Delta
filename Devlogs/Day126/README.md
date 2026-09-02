# 126일차 : 기억의 조각 소비 - 영구 능력치 강화 상점

## 목표
- 125일차에서 지급만 되던 기억의 조각(영구 성장 재화)을 실제로 "소비"할 수 있게
- 공격력/방어력/최대체력 영구 강화 하나만 우선 구현 (인벤토리·유물·상점·탐험 강화는 범위 밖)

## 구현 내용

### 1. 강화 규칙
- `PermanentStatUpgradeRule`(Domain) 신설 - 공격력/방어력/최대체력 3종, 레벨당 효과
  (+2/+2/+10), 최대 10레벨, 다음 레벨 비용은 `5 × (현재 레벨 + 1)`로 갈수록 비싸짐
- `ProfileData`를 직접 참조하지 않고 `Dictionary<string,int>`만 받아 처리해
  Domain 레이어의 "제로 의존성" 원칙(RoomTypeRules·MonsterTierRules와 같은 이유)을 유지

### 2. 저장 위치
- `ProfileData.PermanentGrowth.PermanentStatUpgradeLevels` 딕셔너리 신설 - 강화 항목 ID를
  키로 레벨을 저장

### 3. 효과 적용
- `PlayerRunState`에 `PermanentBonusStats` 필드 신설, `GetFinalStats()` 합산에 포함
  (BaseStats + AllocatedStats + TemporaryStats + EquipmentBonuses + PermanentBonusStats)
- `ApplicationFlow.StartNewGame()`: 런 시작 시 프로필의 강화 레벨을 읽어 채우고
  HP/MP/정력을 새 최대치로 완전 회복
- `ApplicationFlow.ContinueGame()`: 이어하기는 저장된 현재 자원을 그대로 쓰되(강제 회복 없음)
  강화 보너스는 동일하게 반영
- `ApplicationFlow.TryPurchasePermanentStatUpgrade(statId)`: 조각 차감 + 레벨 증가 + 즉시 저장

### 4. 로비 상점 UI
- `LobbySceneController`에 "강화" 버튼 신설 - 누르면 항목별 현재 레벨/다음 비용/구매 버튼을
  보여주는 패널이 열림
- 조각이 부족하거나 이미 최대 레벨이면 구매 버튼 비활성화, 구매 성공 시 보유 조각·레벨 표시가
  즉시 갱신됨
