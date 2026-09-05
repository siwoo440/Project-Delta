# 131일차 : 엔딩 구조 및 판정 로직 (기획서 7.1~7.2절)

## 목표
- 기획서(구글독스) 7.1~7.2절을 확인해, 주요 엔딩 15종의 구조와 판정 로직을 실제 수치
  그대로 구현 - 이번엔 로직/구조만 만들고 최종 선택 화면 UI나 세부 문구는 이후 일차로 미룸

## 확인된 기획 내용
- 공식 엔딩 3범주 45개: 주요 엔딩 15 + 몬스터 개별 엔딩 20 + NPC 개별 엔딩 10
  (패배 기록 31개는 별도, 달성률 미포함)
- 주요 엔딩 15개는 귀환 계열(1~8)/잔류 계열(9~13)/몬스터 하렘(14)/마왕의 종(15) 4갈래
- 판정 시점: 5층 도달 → 마왕전 결과 → 최종 선택(귀환/잔류) 화면
- 한 회차에 공식 엔딩은 하나만 획득, 캐릭터 개별 엔딩은 마왕전 이전에 발생(선택 시 즉시 회차 종료)

## 구현 내용

### 1. 데이터/규칙 (Domain)
- `MainEndingId` - 15개 엔딩 열거형 + 표시명 + 기억 파편 보상(기획서 표 그대로: 5/10/15)
- `MainEndingConditions` - 판정에 필요한 조건 DTO(보스 결과, 최종 선택, 장비+유물 수,
  저주 아이템 수, HP/정력 비율, 탐색률, 도감 완성 여부, 개별 엔딩 충족 수, 관계 만렙 여부)
- `MainEndingRule.Evaluate()` - 패배/항복 → 마왕의 종 최우선, 하렘 조건 다음, 이후
  귀환/잔류 갈래에서 구체적 조건부터 검사해 안 맞으면 기본형(현실로의 귀환/던전의 왕)으로
  떨어지는 우선순위 - 기획서에 명시된 우선순위가 없어 구현 판단으로 정함(검토 필요)

### 2. 실제로 계산 가능하게 만든 조건
- `EquipmentItemState`에 `IsCursed` 필드 추가(장착 시점 스냅샷), `EquipmentRunState`에
  `EquippedCount`/`CursedEquippedCount()` 추가 - "빈손의 귀환"/"저주를 품은 귀환" 등 판정 가능
- `DungeonRunState.IsCurrentFloorFullyExplored()` 신설 + `DungeonFloorController.
  TryDescend()`에서 층 이동 직전에 체크해 `RunStatistics.FullyExploredFloorCount` 누적 -
  "완전한 탐험자의 귀환" 판정 가능 (5층은 AdvanceFloor를 타지 않아 실시간 체크로 보정)

### 3. 아직 항상 기본값인 조건 (자리만 마련)
- 몬스터 도감 100% → `RunStatistics.HasFullMonsterDex` 항상 `false`
- 개별 엔딩 5개 이상 충족 → `IndividualEndingConditionsMetCount` 항상 `0`
- 관계 20종 만렙(하렘) → `HasAllRelationshipsMaxed` 항상 `false`

### 4. 판정/기록 파이프라인
- `RunSubStates.cs`의 빈 껍데기였던 `BattleRunState`/`RunStatistics`에 실제 필드 채움
- `MainEndingConditionsBuilder`(Application) - RunContext 여러 곳의 값을 조건 DTO로 조립
- `ApplicationFlow.TryFinalizeMainEnding()` - 이미 확정된 엔딩이면 재판정하지 않고 그대로
  반환(회차당 1개 보장), 새로 확정되면 `ProfileData.PermanentRecord.UnlockedMainEndingIds`에
  기록하고 기억의 조각 지급

## 다음 일차로 넘긴 부분
- `RunContext.Battle.SetBossOutcome()`/`SetFinalChoice()`를 실제로 호출하는 지점이 아직
  없음 - 마왕 전투 승리 판정(일반/이벤트 전투 구분)과 최종 선택 화면 UI 연결 필요
