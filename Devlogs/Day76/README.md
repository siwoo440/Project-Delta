# Project Delta - 76일차 개발일지

## 작업 주제

**몬스터 그룹 구성 시스템 — 등급 기반 대표 외형 판정 및 실제 전투 연결**

---

## 개발 목표

던전을 돌아다니는 몬스터 하나가 실제로는 **여러 마리로 구성된 그룹**을 대표하도록 만든다.

```text
일반 · 희귀 · 보스 등급이 있고, 그룹 안에 더 높은 등급이 섞여 있으면 그 몬스터를
탐험 화면의 대표 외형으로 보여준다. 등급이 같으면 가장 앞 자리(1번) 몬스터가 대표다.
각 몬스터 마커는 "몇 번 자리에 어떤 몬스터가 있는지" 데이터를 들고 있고,
플레이어와 마주치면 그 데이터를 그대로 불러와 전투를 시작한다.
```

작업 전 세 가지를 먼저 정했다.

```text
그룹 구성 결정 시점: 던전 생성 시 확정 (시드 기반 결정론적, 47~75일차와 동일 원칙)
그룹 구성: 여러 종·여러 등급이 섞일 수 있음
데이터 위치: MonsterDefinition에 등급 필드, EncounterDefinition에 마리 수 범위
```

47일차부터 `BeginTestBattle()`은 접촉한 몬스터를 4번 복제해서 적을 채우는 플레이스홀더였다("실제 적 구성은 DataRepository가 도입되는 이후 일차에서 교체한다"는 주석이 그대로 남아 있었다). 76일차에 이 플레이스홀더를 실제로 걷어낸다.

---

## 주요 작업 내용

### 1. MonsterRarity — 몬스터 등급

`ProjectDelta.Data`에 `Normal < Rare < Boss` 3단계 enum을 추가하고 `MonsterDefinition`에 `Rarity` 필드로 연결했다. 선언 순서 자체가 우선순위라서(C# enum은 기본적으로 정수 비교가 되므로) 대표 판정에 `candidate.Rarity > representative.Rarity` 비교 하나로 등급 우선순위를 그대로 쓸 수 있다.

### 2. EncounterDefinition 확장 — 그룹 후보 풀·마리 수 범위

```csharp
[SerializeField] private EncounterMonsterEntry[] additionalMonsterPool; // 2번 자리부터 뽑을 후보
[SerializeField] private int minGroupSize = 1;
[SerializeField] private int maxGroupSize = 1;
```

`EncounterMonsterEntry`(신규, 몬스터 + 가중치)는 그룹의 2번째 자리부터 채울 후보다. 기존 `Monster` 필드(1번 자리, 항상 존재)는 그대로 뒀다. 기본값(1~1, 빈 풀)은 "몬스터 1마리"라는 기존 동작과 완전히 같아서, 기존에 만들어둔 `EncounterDefinition` 에셋이나 테스트는 아무것도 안 건드려도 그대로 통과한다.

### 3. MonsterGroupCompositionService — 결정론적 그룹 뽑기

새 서비스가 "이 방에 실제로 몇 마리의 어떤 몬스터가 나오는가"를 계산한다.

```text
그룹 마리 수 = [minGroupSize, maxGroupSize] 범위에서 Seed·RoomId·EncounterId 기반으로 뽑음
                (75일차에 확정한 적 최대 인원 4명을 넘지 않게 고정)
1번 자리 = 항상 EncounterDefinition.Monster
2번 자리부터 = additionalMonsterPool에서 가중치 기반으로 뽑음 (풀이 비어 있으면 1번과 같은 종)
대표 외형 = 그룹 중 등급이 가장 높은 몬스터, 동률이면 가장 앞 자리
```

40일차 `RoomEncounterPlacementService.CalculateStableRoll()`과 같은 FNV-1a 해시 방식을 쓴다 — 같은 Seed·방·인카운터면 던전을 다시 불러와도 항상 같은 구성이 나오므로, **그룹 구성 자체를 저장할 필요가 없다.** 해시 혼합 로직은 두 곳(방 배치 굴림, 그룹 구성 굴림)에서 쓰게 돼서 `DeterministicRollHash`라는 공용 유틸로 뽑아냈다 — `RoomEncounterPlacementService.CalculateStableRoll()`은 결과값 변경 없이 이 유틸을 호출하도록 리팩터링했다.

### 4. 데이터 흐름 연결

```text
RoomEncounterPlacementService.Build()
  → MonsterGroupCompositionService로 방마다 그룹 구성·대표를 뽑는다
  → RoomEncounterAssignment(그룹 전체 ID 목록 + 대표 ID)로 저장

DungeonFloorController.CreateRuntimeMonster()
  → ExplorationMonsterMarker.Configure()에 그룹 전체 ID 목록을 함께 넘긴다
  → 마커는 대표 ID로 빌보드 스프라이트를(대표 외형), 그룹 전체를 별도로 들고 있는다

ExplorationMonsterEncounterController.TryBeginEncounterAtCurrentPosition()
  → session.TryBegin()에 마커의 그룹 전체 ID 목록을 함께 넘긴다
  → EncounterContext가 대표 ID + 그룹 전체 ID 목록을 모두 보관

BeginTestBattle()
  → 더 이상 4마리 복제가 아니라, EncounterContext의 그룹 전체 ID 목록을 그대로 순회해
    자리마다 실제 다른 몬스터로 BattleParticipant를 만든다
```

`RoomEncounterAssignment`(Domain 계층)은 `MonsterDefinition`(Data 계층) 객체를 직접 들 수 없다 — asmdef 참조 방향이 Application → Data이지 그 반대가 아니기 때문이다(66일차 스킬 enum과 같은 제약). 그래서 Domain 계층은 끝까지 문자열 ID만 들고 다니고, 실제 `MonsterDefinition` 에셋으로 되돌리는 조회는 Presentation 계층(`DungeonFloorController.TryFindMonsterDefinition`)에서 한다. 지금은 던전 전체가 `defaultMonsterEncounter` 하나만 쓰므로 그 안의 기본 몬스터 + 추가 후보 풀만 뒤지면 충분하고, 여러 `EncounterDefinition`을 쓰게 되면 이미 자리만 만들어져 있는 `DataRepository` 기반 조회로 교체하면 된다.

### 5. AI Profile도 실제 종별로 분리

작업 도중 `BattleIntentRuntimeController` 쪽 AI 판단 코드가 몬스터 종류와 무관하게 항상 `testMonsterDefinition.AiProfile`을 쓰고 있는 걸 발견했다(47일차 4마리 복제 시절에는 다 같은 몬스터였으니 문제가 안 됐다). 그룹에 여러 종이 섞이는 지금은 각 적이 자기 자신의 `MonsterDefinition`을 기준으로 AI Profile을 찾도록 고쳤다 — 안 고치면 희귀·보스 몬스터가 섞여도 전부 테스트 몬스터처럼 행동하는 문제가 생긴다.

### 6. EditMode 테스트 추가

```text
MonsterGroupCompositionServiceTests (신규)
  - 추가 후보 풀이 없으면 모든 자리가 1번 몬스터로 채워짐
  - 그룹 마리 수가 적 최대 인원(4명)을 넘지 않음
  - 같은 Seed·Room·Encounter면 항상 같은 구성 (결정론 확인)
  - 등급이 가장 높은 몬스터가 자리와 무관하게 대표로 선택됨
  - 등급이 같으면 더 앞 자리가 대표로 선택됨
  - 빈/null 그룹이면 대표 없음(null)

RoomEncounterPlacementTests (확장)
  - 그룹 마리 수를 3으로 고정하면 실제 배치 결과의 MonsterDefinitionIds가 3개로 채워짐
```

기존 `RoomEncounterPlacementTests`의 다른 테스트들은 그룹 설정을 건드리지 않아 기본값(1~1)을 그대로 쓰므로 전부 수정 없이 통과해야 한다.

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Data/MonsterRarity.cs (신규)
Assets/ProjectDelta/Scripts/Data/EncounterMonsterEntry.cs (신규)
Assets/ProjectDelta/Scripts/Data/MonsterDefinition.cs
Assets/ProjectDelta/Scripts/Data/EncounterDefinition.cs

Assets/ProjectDelta/Scripts/Domain/DungeonEncounterLayout.cs

Assets/ProjectDelta/Scripts/Application/MonsterGroupCompositionService.cs (신규)
Assets/ProjectDelta/Scripts/Application/DeterministicRollHash.cs (신규)
Assets/ProjectDelta/Scripts/Application/RoomEncounterPlacementService.cs
Assets/ProjectDelta/Scripts/Application/EncounterContext.cs
Assets/ProjectDelta/Scripts/Application/ExplorationEncounterSession.cs

Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterMarker.cs
Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs

Assets/ProjectDelta/Tests/EditMode/MonsterGroupCompositionServiceTests.cs (신규)
Assets/ProjectDelta/Tests/EditMode/RoomEncounterPlacementTests.cs
```

---

## 확인 사항

- `MonsterRarity`(일반/희귀/보스)를 `MonsterDefinition`에 연결
- `EncounterDefinition`에 그룹 후보 풀·마리 수 범위 추가, 기본값은 기존 "1마리" 동작과 동일해 하위 호환
- `MonsterGroupCompositionService`가 Seed 기반으로 그룹 구성과 대표 외형을 결정론적으로 계산 (별도 저장 불필요)
- 대표 판정 규칙: 등급 최우선, 동률이면 앞 자리 우선 — 자리와 무관하게 검증하는 테스트로 확인
- 그룹 마리 수는 75일차에 확정한 적 최대 인원(4명)을 넘지 않게 고정
- `RoomEncounterAssignment`(Domain)는 문자열 ID만 보관 — asmdef 참조 방향 준수, `MonsterDefinition` 실물 조회는 Presentation 계층에서
- `BeginTestBattle()`이 더 이상 4마리 복제가 아니라 실제 그룹 구성으로 적을 만듦
- 몬스터별 AI Profile이 실제 종을 기준으로 선택되도록 부수적으로 수정 (기존엔 테스트 몬스터 고정)
- 기존 `RoomEncounterPlacementTests`는 수정 없이 그대로 통과 (그룹 설정 기본값이 이전 동작과 동일)
- 새 EditMode 테스트로 결정론·대표 판정·최대 인원 제한·실제 배치 반영을 검증

Unity Editor에서의 실제 스크립트 컴파일과 Test Runner 통과 여부, 그리고 실제 던전에서 여러 종이 섞인 몬스터 그룹이 의도대로 나오는지는 이 저장소 diff만으로는 확정할 수 없으므로, Unity Editor에서 직접 최종 확인이 필요하다. 특히 실제 `EncounterDefinition` 에셋에 `additionalMonsterPool`·마리 수 범위를 채워 넣어야 그룹이 2마리 이상으로 나오는 걸 눈으로 확인할 수 있다(지금 기본값은 1마리).

---

## 이번 일차 완료 상태

76일차 목표인 **몬스터 그룹 구성 시스템**을 구현했다. 47일차부터 남아 있던 "4마리 복제" 플레이스홀더가 사라지고, 던전의 몬스터 마커가 처음으로 진짜 그룹 데이터를 들고 다니게 됐다.

---

## 다음 단계

실제 `EncounterDefinition` 에셋(들)에 몬스터 등급·후보 풀·마리 수 범위를 채워 콘텐츠로 확정한다. 던전 전체가 `defaultMonsterEncounter` 하나만 쓰는 구조도, 방마다 다른 인카운터를 배정할 수 있게 확장하는 게 자연스러운 다음 단계다.
