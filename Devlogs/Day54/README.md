# Project Delta - 54일차 개발일지

## 작업 주제

**마나·정력 자원 도입 — 테스트 상수를 걷어내고 PlayerRunState·몬스터 정의와 연결**

---

## 개발 목표

47일차부터 전투 화면 확인용으로 남아 있던 플레이어·몬스터 테스트 스탯 상수를
실제 런타임 데이터로 교체한다.

- 플레이어: `RunContext.Current.Player` (`PlayerRunState`)의 최종 능력치와
  현재 체력·마나·정력을 그대로 이어받는다.
- 몬스터: `MonsterDefinition`에 전투 능력치 7종 + 최대 마나를 추가하고,
  그 값으로 참가자를 만든다.
- `BattleParticipant`에 마나·정력 자원(최대치·현재치)을 도입한다.
- 전투 화면 HUD의 MP·SP 표시를 실제 전투 참가자 데이터에 연결한다
  (기존에는 "전투 모델에 아직 없다"는 주석과 함께 `PlayerRunState`만 표시하고 있었다).

스킬이 마나·정력을 실제로 소모하는 처리(`UseSkillCommand`)는 66~67일차에서 다룬다.
이번 일차는 자원 그릇을 만들고 실제 데이터에 연결하는 것까지만 다룬다.

---

## 주요 작업 내용

### 1. BattleParticipant에 마나·정력 자원 추가

`MaxMana` / `CurrentMana` / `MaxStamina` / `CurrentStamina`를 `MaxHp`/`CurrentHp`와
같은 방식으로 추가했다.

생성자에 `maxMana`, `maxStamina`, `currentHp`, `currentMana`, `currentStamina`를
선택 인자로 추가했다. 기존 호출부(몬스터·테스트 코드)는 값을 생략하면 자원이
0이거나 만땅으로 시작하도록 기본값을 유지해 하위 호환을 지켰다.

`currentXxx`를 지정하지 않으면 최대치로 시작하고, 최대치를 넘는 값이 들어오면
최대치로 잘라낸다.

### 2. MonsterDefinition에 전투 능력치 필드 추가

`displayName` 하나뿐이던 `MonsterDefinition`에 `BattleParticipant`를 만드는 데
필요한 능력치를 추가했다.

```text
maxHp
maxMana
speed
attack
defense
accuracy
evasion
charm
resistance
```

층 보정·개체 등급 보정·난이도 보정·개체 편차(기획서 3.5)는 아직 적용하지 않는다.

기존 `MON_TEST` 에셋에 47일차 테스트 몬스터와 같은 수치
(체력 10 / 속도 5 / 공격 4 / 방어 2 / 명중 80 / 회피 5)를 그대로 옮겨,
전투 밸런스가 바뀌지 않게 했다.

### 3. PlayerRunState 기본 능력치 도입

`RunContext.Begin()`이 항상 빈 능력치(0)로 `PlayerRunState`를 만들고 있어,
플레이어 참가자를 여기 연결하면 체력·마나·정력이 모두 0이 되는 문제가 있었다.

`PlayerRunState.CreateDefault()`를 추가해 기획서 6.1 기본 능력치 표를 그대로
반영했다.

```text
최대 체력 100 / 최대 마나 50 / 최대 정력 100
공격력 50 / 방어력 40 / 속도 50 / 매력 50 / 회피 40 / 저항 50
```

현재 체력·마나·정력은 시작 시 최대치로 채운다. `PlayerBaseDefinition`
ScriptableObject가 아직 없어 상수로 관리했고, 나중에 정의 데이터가 생기면
`CreateDefault()` 내부만 교체하면 되도록 분리해두었다.

그 외 `new PlayerRunState()`를 쓰는 그리드 이동 테스트 등은 기존과 동일하게
빈 상태로 남긴다.

### 4. ExplorationMonsterEncounterController 테스트 상수 제거

47일차부터 있던 `TestPlayerMaxHp`, `TestPlayerSpeed` 등 플레이어·몬스터 스탯
상수를 모두 제거했다. 명중(`Accuracy`)은 기획서 6.1 기본 능력치 표에 없는
스킬 기본값 성격이라 56일차 명중 공식 정정 전까지는 임시 상수로 남겨뒀다.

`[SerializeField] private MonsterDefinition testMonsterDefinition`을 추가하고
씬에서 기존 `MON_TEST` 에셋을 연결했다. `DataRepository`로 몬스터를 ID 조회하는
방식은 아직 실제 게임 흐름에 연결되어 있지 않아(정의만 있고 생성자가 없음)
이번 일차 범위 밖으로 미뤘다.

`BeginTestBattle()`은 이제 `RunContext.Current.Player`의 최종 능력치와 현재
체력·마나·정력으로 플레이어 참가자를, `testMonsterDefinition`으로 적 참가자를
만든다.

### 5. 전투 종료 시 자원 되돌리기

기획서 4.2 "전투 후 자동 회복 없음", 3.6.2 "층 이동 시에만 회복" 규칙에 맞춰
`FinishBattle()`에서 전투 참가자가 들고 있던 현재 체력·마나·정력을
`RunContext.Current.Player`로 그대로 되돌리도록 했다.

### 6. BattleHudController MP·SP 연결

"MP·SP는 전투 모델에 아직 없으므로 런 상태를 표시만 한다"던 주석을 지우고,
HP와 같은 방식으로 MP·SP도 전투 참가자(`BattleContext.Player`) 데이터를
그대로 표시하도록 했다.

### 7. EditMode 테스트 추가

`BattleParticipantTests`에 마나·정력 관련 테스트를 추가했다.

- `maxMana`·`maxStamina`를 생략하면 0으로 시작 (기존 호출부 호환)
- 지정하면 만땅으로 시작
- `currentHp`·`currentMana`·`currentStamina`로 기존 값을 이어받음
- 최대치를 넘는 현재값은 최대치로 잘림

`PlayerRunStateTests`를 새로 추가해 `CreateDefault()`가 기획서 6.1 표와
일치하는지, 현재 자원이 최대치로 시작하는지 확인했다.

---

## 수정 파일

```text
Assets/ProjectDelta/Data/Monster/Monster Definition/MonsterDefinition.asset
Assets/ProjectDelta/Scenes/DungeonScene.unity
Assets/ProjectDelta/Scripts/Application/BattleParticipant.cs
Assets/ProjectDelta/Scripts/Data/MonsterDefinition.cs
Assets/ProjectDelta/Scripts/Domain/PlayerRunState.cs
Assets/ProjectDelta/Scripts/Domain/RunContext.cs
Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs
Assets/ProjectDelta/Tests/EditMode/BattleParticipantTests.cs
Assets/ProjectDelta/Tests/EditMode/PlayerRunStateTests.cs (신규)
```

---

## 남은 과제

- 저장 데이터(`RunData.PlayerRunStats`)에 정력(Stamina) 필드가 없고, 대신
  용도가 다른 `Arousal`(절정도) 필드만 있다. `PlayerRunState`와 저장 데이터를
  실제로 연결하는 일차에서 함께 정리해야 한다.
- 몬스터 조회를 `DataRepository` + ID 기반으로 바꾸는 작업은 아직 시작되지
  않았다. 현재는 인스펙터에 직접 연결한 `MonsterDefinition` 하나만 쓴다.
- 명중(`Accuracy`)은 여전히 테스트 상수다. 56일차 명중 공식 정정에서
  스킬 기본값 기반으로 교체한다.

Unity 에디터가 열려 있어 이번 일차 변경 사항의 배치 모드 컴파일 확인은
진행하지 못했다. 에디터에서 다시 포커스를 얻으면 자동으로 재컴파일되므로
콘솔에서 오류 여부를 확인해야 한다.

---

## 다음 단계

다음 일차부터는 피해 공식을 비율형으로 바꾸고 95~105% 편차를 적용하는 등
(55일차) 전투 정합성 회수 작업을 이어간다.
