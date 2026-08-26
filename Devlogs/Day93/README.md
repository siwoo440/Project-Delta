# Project Delta — 93일차 개발 일지

- 날짜: 2026-08-26
- 기준 브랜치: `main`
- 최신 커밋: `13a5b28f550b00d7a20958fc78ad90aa6080bf21`
- 현재 커밋 메시지: `a`
- 비교 기준: `5c710dba6cb8024572d5118b6f20fd8fd0d4359f`
  - `92일차 : 인벤토리 가득 참 처리 및 아이템 교체 선택 구현`

---

## 1. 오늘의 목표

93일차는 91일차의 아이템 분류 규칙과 92일차의 인벤토리 획득 구조를 기반으로
**소비 아이템의 실제 사용 효과와 탐험·전투 사용 처리**를 구현하는 작업을 진행했다.

핵심 목표는 다음과 같다.

- 아이템마다 사용 가능한 상황과 사용 효과를 데이터로 설정
- HP / MP / 정력 회복 효과 구현
- 실제 적용 가능한 효과를 먼저 계산하는 Preview 구조 적용
- 효과 적용에 성공한 경우에만 아이템 수량 1 감소
- 탐험에서는 `PlayerRunState`, 전투에서는 `BattleParticipant`를 수정
- 전투 아이템 사용을 플레이어 행동 1회로 처리
- 선택 아이템 HUD에 최소 `사용` 기능 연결
- ItemDefinition 조회를 기존 문자열 호환 구조에서 정식 Item ID 쪽으로 보정
- EditMode 테스트 추가

---

## 2. 아이템 사용 효과 데이터

새로운 아이템 사용 데이터 구조를 추가했다.

### ItemUseContext

| 값 | 의미 |
| --- | --- |
| `Both` | 탐험과 전투 모두 사용 가능 |
| `Exploration` | 탐험에서만 사용 가능 |
| `Battle` | 전투에서만 사용 가능 |

### ItemUseEffectKind

| 값 | 효과 |
| --- | --- |
| `None` | 효과 없음 |
| `RestoreHp` | HP 회복 |
| `RestoreMana` | MP 회복 |
| `RestoreStamina` | 정력 회복 |

하나의 ItemDefinition에 여러 Use Effect를 등록할 수 있도록 배열 구조로 만들었다.

예:

```text
소형 회복약
Category = Consumable
UseContext = Both

UseEffects
- RestoreHp / 25
```

---

## 3. ItemDefinition 확장

기존 ItemDefinition의 다음 데이터는 그대로 유지한다.

- DisplayName
- Icon
- Description
- Category
- MaxStackSize

93일차에서 다음 데이터가 추가됐다.

```text
UseContext
UseEffects[]
```

이를 통해 아이템 에셋 자체가
**언제 사용할 수 있고 어떤 효과를 발생시키는지** 직접 정의할 수 있게 됐다.

---

## 4. ItemUseService

아이템 사용 규칙을 UI 코드에서 직접 처리하지 않고
Application 계층의 `ItemUseService`에 모았다.

주요 처리 단계는 다음과 같다.

```text
아이템 슬롯 확인
→ ItemDefinition 확인
→ Category 사용 가능 여부 확인
→ 현재 Context 사용 가능 여부 확인
→ 실제 적용 가능한 효과 Preview
→ 효과 적용
→ 아이템 수량 1 감소
```

### Preview

Preview에서는 실제 인벤토리와 플레이어 상태를 변경하지 않는다.

확인하는 내용:

- 올바른 슬롯인가
- ItemDefinition과 슬롯이 일치하는가
- 사용 가능한 Category인가
- 현재 탐험/전투 상황에서 사용할 수 있는가
- 효과 데이터가 존재하는가
- 실제로 증가시킬 수 있는 자원이 있는가

### Commit

Preview가 성공한 경우에만 실제 효과를 적용하고
아이템 수량을 1 감소시킨다.

---

## 5. 회복량 Clamp

회복 효과는 최대 수치를 넘지 않도록 실제 적용량을 계산한다.

예:

```text
HP 90 / 100
회복약 효과 +25

→ 실제 회복량 +10
→ HP 100 / 100
→ 아이템 수량 -1
```

반대로 이미 최대치라면:

```text
HP 100 / 100
회복약 사용 시도

→ 적용 가능한 효과 없음
→ 사용 실패
→ 아이템 수량 유지
```

따라서 효과가 전혀 없는 상태에서 소비 아이템이 낭비되지 않는다.

---

## 6. 탐험 중 아이템 사용

탐험 상태에서는 `PlayerRunState`의 현재 자원을 직접 변경한다.

지원 자원:

```text
CurrentHp
CurrentMana
CurrentStamina
```

성공적으로 사용한 뒤에는 런 진행 상태를 저장하는 기존 흐름과 연결한다.

---

## 7. 전투 중 아이템 사용

전투에서는 탐험의 `PlayerRunState`를 직접 수정하지 않고
현재 BattleContext의 플레이어 `BattleParticipant`를 변경한다.

이를 위해 BattleParticipant에 다음 회복 API를 확장했다.

```text
Heal()
RestoreMana()
RestoreStamina()
```

전투가 종료될 때 기존 `FinishBattle()` 흐름에서
BattleParticipant의 현재 HP / MP / 정력을 다시 PlayerRunState로 복원하므로
기존 전투 자원 동기화 구조를 유지한다.

---

## 8. 전투 아이템을 행동 1회로 처리

전투 중 소비 아이템 사용은 무료 행동으로 처리하지 않는다.

흐름:

```text
PLAYER TURN
→ 인벤토리 아이템 선택
→ 사용 가능 여부 Preview
→ BattleSession.ResolveAction 진입
→ 아이템 효과 적용
→ 수량 1 감소
→ BattleActionResult 기록
→ 다음 행동자 진행
```

즉 공격 / 방어 / 스킬과 동일하게
**플레이어 행동 1회를 소비하는 정식 전투 행동**으로 처리한다.

---

## 9. 플레이어 인벤토리 HUD

기존 선택 아이템 패널을 활용해 최소한의 `사용` 기능을 연결했다.

흐름:

```text
인벤토리 슬롯 클릭
→ 아이템 상세 표시
→ [사용]
```

사용 버튼은 현재 아이템 Category와
탐험 / 전투 상황에서 실제로 사용할 수 있는지를 Preview하여 활성화한다.

사용 결과는 선택 아이템 설명 영역에 표시할 수 있도록 구성했다.

예:

```text
사용 완료 : HP +25
사용 완료 : MP +20
현재 적용할 수 있는 회복 효과가 없습니다.
현재 상황에서는 사용할 수 없습니다.
```

94일차의 정식 인벤토리 조작 UI 전에
기능 검증에 필요한 최소 UI만 추가한 구조다.

---

## 10. Item ID 연결 보정

기존 상자 데이터는 문자열 기반 호환 구조가 남아 있다.

93일차에서는 `RuntimeItemDefinitionLookup`이 ItemDefinition을 찾았을 때
가능하면 정식 `Definition.Id`를 Canonical Item ID로 사용하도록 보정했다.

```text
기존 상자 문자열
→ RuntimeItemDefinitionLookup
→ ItemDefinition
→ Definition.Id
→ Inventory ItemId
```

이를 통해 이후 인벤토리 아이템 사용 시
ItemDefinition과 슬롯을 더 안정적으로 연결할 기반을 만들었다.

---

## 11. 테스트 추가

`ItemUseServiceTests`를 추가하여 다음 상황을 검증할 수 있도록 했다.

- 탐험 Preview가 플레이어와 인벤토리를 수정하지 않음
- HP 회복 성공
- 최대 HP 초과 회복 Clamp
- 자원이 최대일 때 아이템이 소비되지 않음
- MP만 회복
- 정력만 회복
- 중요 아이템 사용 차단
- 잘못된 Context 사용 차단
- 마지막 1개 사용 시 슬롯 비움
- 전투에서는 BattleParticipant의 자원만 변경
- 전투 사용 시 인벤토리 수량 감소

---

## 12. 구현 중 컴파일 오류 수정

93일차 전투 아이템 사용 코드를 기존 BattleSession 흐름에 연결하는 과정에서
현재 프로젝트에 존재하지 않는 API 이름을 참조하는 문제가 발생했다.

### 잘못된 호출

```text
ApplyRoundStartStatusEffectsIfNeeded(actor)
HasPendingActorsInCurrentRound
```

### 최종 수정

`ApplyRoundStartStatusEffectsIfNeeded()` 호출은 제거했다.

라운드 시작 상태 이상 처리는 이미 기존 `BattleSession.TryStartRound()` 내부에서
`BattleRoundStatusProcessor.ApplyStartOfRoundEffects()`를 통해 처리되기 때문이다.

또한 BattleSession의 실제 속성명에 맞춰:

```text
HasPendingActorsInCurrentRound
→ HasPendingActorsThisRound
```

으로 수정했다.

현재 최신 소스에서는 위 잘못된 두 참조가 검색되지 않는 상태다.

---

## 13. 92일차 대비 변경 범위

92일차 커밋과 비교하여 93일차에서는 다음 주요 파일이 변경됐다.

### 생성

```text
Assets/ProjectDelta/Scripts/Application/ItemUseService.cs
Assets/ProjectDelta/Scripts/Application/ItemUseService.cs.meta
Assets/ProjectDelta/Scripts/Data/ItemUseDefinition.cs
Assets/ProjectDelta/Scripts/Data/ItemUseDefinition.cs.meta
Assets/ProjectDelta/Tests/EditMode/ItemUseServiceTests.cs
Assets/ProjectDelta/Tests/EditMode/ItemUseServiceTests.cs.meta
```

### 수정

```text
Assets/ProjectDelta/Scripts/Application/BattleParticipant.cs
Assets/ProjectDelta/Scripts/Data/ItemDefinition.cs
Assets/ProjectDelta/Scripts/Presentation/ChestInteractionController.cs
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs
Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs
Assets/ProjectDelta/Scripts/Presentation/RuntimeItemDefinitionLookup.cs
```

컴파일 오류 수정 과정에서 Day93 보정용 BAT / PowerShell 파일도 현재 커밋에 포함되어 있다.

---

## 14. 현재 상태

93일차 기준 아이템 시스템은 다음 흐름까지 연결됐다.

```text
아이템 획득
→ Stack / 슬롯 배치
→ 아이템 Category 확인
→ 사용 Context 확인
→ 사용 효과 Preview
→ 탐험 또는 전투 자원 회복
→ 성공 시 아이템 수량 감소
```

현재 구현 범위의 주요 사용 효과는:

- HP 회복
- MP 회복
- 정력 회복

이다.

버리기, 장비 장착, 판매, 유물·저주 특수 행동 등은 이번 일차 범위에 포함하지 않았다.

---

## 15. 검증 메모

GitHub `main`의 최신 상태를 기준으로 확인했다.

- 최신 커밋: `13a5b28f550b00d7a20958fc78ad90aa6080bf21`
- 최신 커밋 메시지: `a`
- 92일차 기준보다 1개 커밋 앞섬
- `ItemUseService` 추가 확인
- `ItemUseDefinition` 추가 확인
- ItemDefinition의 UseContext / UseEffects 확장 확인
- BattleParticipant의 MP / 정력 회복 API 추가 확인
- ExplorationMonsterEncounterController의 전투 아이템 행동 추가 확인
- 잘못된 `ApplyRoundStartStatusEffectsIfNeeded` 참조가 최신 저장소 검색에서 발견되지 않음
- 잘못된 `HasPendingActorsInCurrentRound` 참조가 최신 저장소 검색에서 발견되지 않음
- 최신 커밋에 연결된 GitHub status check 결과는 없음

현재 환경에서는 Unity Editor를 실행할 수 없으므로
**실제 Unity 컴파일 및 EditMode Test Runner 통과 여부는 검증하지 못했다.**

GitHub 최신 소스의 정적 구조를 확인한 범위에서는
93일차 종료를 막는 새로운 명백한 소스 참조 문제는 발견하지 못했다.

---

## 16. 다음 단계 준비

93일차에서 실제 아이템 사용 동작까지 연결했으므로
다음 단계에서는 인벤토리의 정식 상호작용 UI를 확장할 수 있다.

예:

```text
아이템 선택
→ 사용
→ 장착
→ 버리기
→ 상세 정보
```

93일차에서 만든 `ItemCategoryRules`와 `ItemUseService`를 그대로 재사용하여
UI가 게임 규칙을 직접 판단하지 않는 구조를 유지할 수 있다.
