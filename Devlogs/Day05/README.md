# Project Delta - 5일차 개발일지

## 개발 주제

**RunContext와 현재 런 생명주기, PlayerRunState 플레이어 런타임 능력치 상태 구현**

기획서 10.2절 "런타임 상태"를 기반으로, 4일차에 만든 저장용 `RunData`와는 별개로 플레이 중 실제로 조작하는 런타임 객체 `RunContext`를 구현했다. 그 하위 상태 중 `PlayerRunState`만 이번 일차에 완전히 채웠다.

---

## 개발 목표

- `RunData`(저장 DTO, Data 계층)와 `RunContext`(런타임 객체, Domain 계층)를 명확히 분리
- `RunContext` 생성/폐기 생명주기(회차 시작 시 생성, 게임 오버·엔딩·회차 포기 시 폐기) 구현
- `PlayerRunState`를 기획서 10.2절 필드 그대로 구현
- "최종 능력치는 필요할 때 계산한다" 원칙 반영 — Base/Allocated/Temporary를 분리해 캐싱하지 않음
- 아직 시스템이 없는 나머지 8개 하위 상태는 placeholder로 자리만 확보

---

## 구현 내용

### 1. RunData vs RunContext 구분

같은 "런 상태"를 가리키는 두 문서 절(9.1, 10.2)이 있어서, 역할을 명확히 나눴다.

```text
RunData (4일차, Data 계층, ProjectDelta.Data)
└─ 저장용 DTO — 디스크에 JSON으로 쓰이는 모양

RunContext (5일차, Domain 계층, ProjectDelta.Domain)
└─ 런타임 객체 — 플레이 중 실제로 읽고 쓰는 모양
```

두 계층이 같은 이름(`DungeonRunState`, `CharacterRunState`)을 쓰기 때문에, `RunContext`는 별도 네임스페이스(`ProjectDelta.Domain`)에 두어 충돌을 피했다. 이후 SaveService가 붙는 일차에 `RunContext → RunData` 변환 로직이 추가될 예정이다.

---

### 2. RunContext 생명주기

```text
RunContext.Begin(runId)
→ 이미 진행 중인 런이 있으면 예외
→ Metadata·Player·Dungeon·Inventory·Skills·Characters·Events·Battle·Reward·Statistics 생성
→ RunContext.Current에 할당

RunContext.End()
→ RunContext.Current = null
```

기획서 규칙("회차 시작 시 생성, 게임 오버·엔딩·회차 포기 시 폐기")을 코드로 옮겼지만, 아직 "새 게임" 진입 흐름 자체가 없어서 `Begin`/`End`는 이번 일차에는 어디서도 호출되지 않는다. 해당 흐름이 생기는 일차에 연결한다.

---

### 3. PlayerRunState 구현

```text
PlayerRunState
├─ Level, Experience, UnusedStatPoints
├─ BaseStats / AllocatedStats / TemporaryStats (각각 StatBlock)
├─ CurrentHp, CurrentMana, CurrentStamina
├─ StatusEffects
├─ Gold
└─ CurrentRoomId
```

`StatBlock`은 기획서 6.1절 기본 능력치 표를 그대로 반영했다.

```text
StatBlock (9개)
├─ MaxHealth, MaxMana, MaxStamina
└─ Attack, Defense, Speed, Charm, Evasion, Resistance
```

최대 체력·마나·정력도 공격력 등과 같은 "스탯 포인트로 증가하는 능력치" 범주라, 별도 Max 필드로 고정하지 않고 `StatBlock`에 포함시켜 `GetFinalStats()`로 계산되게 했다.

```text
GetFinalStats()
= StatBlock.Sum(BaseStats, AllocatedStats, TemporaryStats)
```

기획서 원칙 그대로: *"최종 능력치는 필요할 때 계산한다. 기본 정의 데이터에 현재 능력치를 덮어쓰지 않는다."*

---

### 4. 나머지 8개 하위 상태 (placeholder)

```text
DungeonRunState   — 3.1~3.2절 던전 생성
InventoryRunState — 6.4절 인벤토리·장비·유물
SkillRunState     — 6.3절 스킬과 행동 숙련도
CharacterRunState — 5장 몬스터·NPC
EventRunState     — 3.4~3.5절 이벤트
BattleRunState    — 4장 전투
RewardRunState    — 6.5절 아이템·보상
RunStatistics     — 회차 단위 진행 통계
```

각 시스템이 실제로 구현되는 일차에 필드를 채우도록 빈 클래스 + 주석으로만 남겼다.

---

## 적용 중 발견된 문제 및 수정

없음.

---

## 현재 5일차 전체 흐름

```text
RunData(저장 DTO)와 RunContext(런타임 객체) 역할 분리
↓
이름 충돌 방지를 위해 RunContext를 ProjectDelta.Domain에 배치
↓
RunContext.Begin/End 생명주기 구현 (아직 미연결)
↓
PlayerRunState + StatBlock 완전 구현
↓
GetFinalStats()로 최종 능력치 즉시 계산 방식 확립
↓
나머지 8개 하위 상태는 소속 시스템 구현 일차까지 대기
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/PlayerRunState.cs
Assets/ProjectDelta/Scripts/Domain/RunContext.cs
Assets/ProjectDelta/Scripts/Domain/RunSubStates.cs
Devlogs/Day05/README.md
```

---

## 수정 파일

없음.

---

## 삭제 파일

없음.

---

## 최종 확인 항목

5일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- `RunContext`와 `RunData`가 서로 다른 네임스페이스에 있어 이름이 충돌하지 않음
- `PlayerRunState.GetFinalStats()`가 Base·Allocated·Temporary를 합산해 반환함
- `RunContext.Begin()`이 이미 진행 중인 런이 있을 때 예외를 던짐
- `RunContext.End()`가 `Current`를 정상적으로 비움

---

## 다음 개발 방향

다음 6일차에는 **SaveService 인터페이스·저장 슬롯 구조·JSON 직렬화/역직렬화·저장 버전 필드**를 구현한다.

예정 흐름:

```text
ISaveService 인터페이스 정의
↓
저장 슬롯 구조 (프로필 1개 + 현재 회차 1개, 기획서 9.1: 수동 저장/불러오기 없음)
↓
ProfileData/RunData/SettingsData 각각 독립적으로 JSON 직렬화
↓
저장 버전 필드 추가 (이후 저장 버전 변환의 기준)
↓
AppRoot의 "저장 시스템 초기화 (TODO)" 자리를 실제 구현으로 교체 준비
```
