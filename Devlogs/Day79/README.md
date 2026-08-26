# Project Delta - 79일차 개발일지

## 작업 주제

**전투 승리 경험치·레벨업·스탯 포인트 지급 및 성장 상태 저장·복원**

---

## 개발 목표

78일차까지 던전에서 실제 몬스터 20종이 인카운터와 전투에 연결됐지만, 전투 승리 후 플레이어가 성장하는 구조는 아직 실제 전투 흐름에 연결되지 않았다.

79일차는 각 몬스터가 경험치 보상값을 가지게 하고, 전투 승리 시 참가한 적들의 경험치를 합산하여 플레이어에게 지급한 뒤 레벨업과 스탯 포인트 지급까지 처리하는 성장 루프를 구현한다.

또한 레벨·현재 경험치·미사용 스탯 포인트를 기존 런 저장 데이터에 연결해 이어하기에서도 성장 상태가 유지되도록 한다.

```text
전투 승리
→ 전투에 참가한 적 전체 확인
→ 몬스터별 경험치 합산
→ 플레이어 경험치 지급
→ 필요 경험치 확인
→ 레벨업
→ 스탯 포인트 지급
→ 기존 승리 보상 선택 흐름
→ 탐험 복귀
```

---

## 1. MonsterDefinition 경험치 보상 추가

`MonsterDefinition`에 `experienceReward`를 추가했다.

```csharp
[Header("79일차 성장 보상")]
[Min(0)]
[SerializeField] private int experienceReward = 20;

public int ExperienceReward =>
    Mathf.Max(
        0,
        experienceReward);
```

기본 경험치 규칙은 몬스터 등급을 기준으로 설정했다.

```text
Normal : 20 EXP
Rare   : 50 EXP
Boss   : 120 EXP
```

79일차 Installer가 기존 `MonsterDefinition` 에셋을 순회하여 이 규칙에 맞는 경험치 값을 기록한다.

---

## 2. PlayerGrowthDefinition 성장표 구현

성장 수치를 코드에 흩어놓지 않고 `PlayerGrowthDefinition` ScriptableObject로 분리했다.

현재 최대 레벨은 Lv.10이며, 레벨업 1회마다 미사용 스탯 포인트 1개를 지급한다.

```text
Lv.1 → 2   : 100 EXP
Lv.2 → 3   : 150 EXP
Lv.3 → 4   : 220 EXP
Lv.4 → 5   : 300 EXP
Lv.5 → 6   : 400 EXP
Lv.6 → 7   : 520 EXP
Lv.7 → 8   : 660 EXP
Lv.8 → 9   : 820 EXP
Lv.9 → 10  : 1000 EXP
```

런타임에서는 `Resources/PlayerGrowthDefinition.asset`을 읽어 같은 규칙을 사용하며, 에셋을 찾지 못하는 테스트 상황에서는 동일한 기본 성장표를 런타임 임시 정의로 생성할 수 있게 했다.

---

## 3. PlayerGrowthService 구현

경험치 합산과 실제 레벨업 계산은 `PlayerGrowthService`가 담당한다.

주요 책임은 다음과 같다.

```text
- 쓰러뜨린 몬스터들의 ExperienceReward 합산
- 음수 경험치 입력 방지
- 현재 경험치 + 획득 경험치 계산
- 한 번의 전투에서 여러 레벨이 오르는 경우 반복 처리
- 레벨업마다 UnusedStatPoints 지급
- Lv.10 상한 적용
- 최대 레벨 도달 후 남는 경험치 제거
- 계산 결과를 BattleGrowthResult로 반환
```

예를 들어 Lv.1 / 0 EXP 상태에서 300 EXP를 얻으면 다음처럼 처리된다.

```text
Lv.1 / 0
+300 EXP

Lv.1 → Lv.2 : 100 사용
남은 EXP 200

Lv.2 → Lv.3 : 150 사용
남은 EXP 50

최종
Lv.3 / 50 EXP
스탯 포인트 +2
```

---

## 4. 실제 전투 승리 흐름 연결

`ExplorationMonsterEncounterController`에 최근 성장 결과를 보관하는 `LastBattleGrowthResult`를 추가했다.

전투 승리가 확정되면 `BattleContext.Enemies`에 실제로 참가한 적들의 `DefinitionId`를 이용해 `MonsterDefinition`을 다시 찾고, 해당 전투의 경험치를 계산한다.

```text
BattleOutcome.Victory
→ ApplyVictoryGrowth()
→ 실제 Enemy 구성 조회
→ MonsterDefinition 경험치 합산
→ RunContext.Current.Player에 경험치 적용
→ 레벨업 결과를 LastBattleGrowthResult에 보존
→ 기존 72일차 승리 보상 처리 계속 진행
```

성장 결과는 기존 승리 보상 선택 구조를 대체하지 않는다. 이후 정식 보상 UI를 확장할 때 `LastBattleGrowthResult`를 이용해 획득 경험치와 레벨업 결과를 표시할 수 있도록 분리해 두었다.

---

## 5. 성장 상태 저장·복원 연결

기존 `RunData.PlayerStats`에 준비되어 있던 다음 값을 실제 `PlayerRunState`와 연결했다.

```text
Level
Experience
UnspentStatPoints
```

`DungeonSaveMapper.BuildFromRunContext()`에서 저장하고, `ApplyBasics()`에서 이어하기 시 복원한다.

구버전 저장 데이터의 레벨 값이 0인 경우에도 최소 Lv.1로 복구하도록 방어 처리를 넣었으며, 저장 데이터가 비정상적인 음수 경험치·포인트를 가지고 있어도 0 이상으로 보정한다.

---

## 6. EditMode 테스트 코드 추가

`PlayerGrowthServiceTests`에 다음 6가지 테스트를 추가했다.

```text
1. 요구 경험치 미만에서는 레벨업하지 않음
2. 정확한 요구 경험치 도달 시 레벨업 + 스탯 포인트 지급
3. 큰 경험치 보상으로 여러 레벨 연속 상승
4. Lv.10 상한 및 이후 추가 성장 차단
5. 여러 몬스터 경험치 보상 합산
6. Level / Experience / UnspentStatPoints 저장·복원
```

테스트 코드는 성장 계산과 저장 연결의 회귀를 확인하기 위한 용도다.

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Data/MonsterDefinition.cs
Assets/ProjectDelta/Scripts/Data/PlayerGrowthDefinition.cs
Assets/ProjectDelta/Scripts/Application/BattleGrowthResult.cs
Assets/ProjectDelta/Scripts/Application/PlayerGrowthService.cs
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs
Assets/ProjectDelta/Scripts/Data/DungeonSaveMapper.cs
Assets/ProjectDelta/Scripts/Editor/Day79PlayerGrowthInstaller.cs
Assets/ProjectDelta/Tests/EditMode/PlayerGrowthServiceTests.cs

Assets/ProjectDelta/Resources/PlayerGrowthDefinition.asset
Assets/ProjectDelta/Data/Monster/Monster Definition/*.asset
```

---

## 확인 사항

최신 `main`의 79일차 커밋 `91245fa8e08f04ad911a7860b82e54b4cfd4b149`을 기준으로 변경 내용을 다시 확인했다.

- `MonsterDefinition`에 경험치 보상 필드와 안전한 조회 Property가 존재한다.
- 성장표는 Lv.10 상한과 9개 구간의 필요 경험치를 가지고 있다.
- 전투에 참가한 적들의 경험치를 합산하는 로직이 존재한다.
- 한 번에 여러 레벨이 상승할 수 있다.
- 레벨업마다 미사용 스탯 포인트를 지급한다.
- 승리 확정 시 성장 계산이 실제 전투 흐름에 연결되어 있다.
- 레벨·경험치·미사용 스탯 포인트가 `DungeonSaveMapper`를 통해 저장·복원된다.
- 성장 관련 EditMode 테스트 코드 6종이 저장소에 포함되어 있다.
- `PlayerGrowthDefinition.asset`에 저장된 경험치 배열도 100/150/220/300/400/520/660/820/1000으로 확인했다.

GitHub에 연결된 CI Status 및 Workflow Run은 현재 없으므로, **Unity Editor의 실제 컴파일 성공과 Test Runner 통과 여부는 저장소만으로 확정하지 않는다.** 최종 실행 검증은 로컬 Unity Editor에서 수행해야 한다.

79일차 커밋에는 빈 자리표시자 폴더였던 `Assets/ThirdParty/.gitkeep` 제거가 함께 잡혀 있다. 현재 해당 폴더에 실제 에셋은 없었기 때문에 79일차 성장 기능에는 영향을 주지 않는 비차단 변경으로 판단했다. 폴더 구조 자체를 유지하고 싶다면 추후 `.gitkeep`만 복원하면 된다.

---

## 이번 일차 완료 상태

79일차 목표인 **몬스터 경험치 보상 → 전투 승리 경험치 획득 → 레벨업 → 스탯 포인트 지급 → 성장 상태 저장·복원** 흐름이 코드와 데이터 기준으로 연결됐다.

정적 검토에서 79일차 진행을 막는 문제는 발견하지 못했다.

---

## 다음 단계

다음 일차에서는 전투 결과와 몬스터 정의를 기반으로 **아이템 드롭 판정 구조**를 구현하는 방향으로 이어갈 수 있다.

경험치와 드롭을 서로 분리해 두면 이후 정식 보상 화면에서 다음과 같이 한 번에 표시할 수 있다.

```text
전투 승리
→ 경험치 / 레벨업
→ 골드
→ 아이템 드롭
→ 보상 선택
→ 탐험 복귀
```
