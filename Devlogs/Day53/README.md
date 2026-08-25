# Project Delta - 53일차 개발일지

## 작업 주제

**전투 능력치 7종 정정 및 관통 제거·매력·저항 추가**

---

## 개발 목표

기존 전투 능력치에 포함되어 있던 `Penetration(관통)`을 제거하고,
기획 기준에 맞춰 전투 능력치를 다음 7종으로 정리한다.

- 공격력 `Attack`
- 방어력 `Defense`
- 속도 `Speed`
- 명중 `Accuracy`
- 회피 `Evasion`
- 매력 `Charm`
- 저항 `Resistance`

이번 일차에서는 능력치 구조 정정까지만 진행하고,
매력과 저항의 실제 판정식 적용은 이후 개발 단계에서 처리한다.

---

## 주요 작업 내용

### 1. BattleParticipant 전투 능력치 구조 정정

`BattleParticipant`에서 기존 `Penetration`을 제거했다.

대신 다음 두 능력치를 추가했다.

```text
Charm
Resistance
```

생성자도 새로운 능력치 구조에 맞춰 수정했다.

최종 전투 능력치 구조:

```text
Speed
Attack
Defense
Accuracy
Evasion
Charm
Resistance
```

HP는 전투 능력치 7종과 별개의 전투 자원으로 유지한다.

---

### 2. 관통 능력치 제거

기존 코드에 존재하던 다음 항목을 제거했다.

```text
Penetration
penetration
TestPlayerPenetration
TestEnemyPenetration
```

전투 참가자 데이터, 피해 계산식, 테스트 코드, 테스트 전투 생성 코드에서
관통 관련 참조를 모두 정리했다.

---

### 3. 피해 계산식 임시 정정

기존 피해 공식:

```text
Attack + Penetration - Defense
```

53일차 수정 후:

```text
Attack - Defense
```

최소 피해 1과 52일차에 구현한 방어 행동의 50% 피해 감소 구조는 그대로 유지했다.

비율형 피해 공식과 피해 편차는 이후 전투 공식 정비 단계에서 별도로 구현한다.

---

### 4. 테스트 전투 능력치 정정

`ExplorationMonsterEncounterController`의 테스트용 전투 스탯도
새 능력치 구조에 맞춰 수정했다.

플레이어:

```text
Attack 6
Defense 3
Accuracy 90
Evasion 10
Charm 0
Resistance 0
```

몬스터:

```text
Attack 4
Defense 2
Accuracy 80
Evasion 5
Charm 0
Resistance 0
```

`BattleParticipant` 생성 시 매력과 저항이 각각 올바른 위치로 전달되도록 수정했다.

---

### 5. EditMode 테스트 정정

기존 관통 기반 피해 테스트를 제거하고
현재 피해 공식에 맞춰 테스트를 수정했다.

추가 및 정정된 주요 검증 항목:

- 공격력 - 방어력 피해 계산
- 최소 피해 1 유지
- 방어 중 50% 피해 감소 유지
- 방어 적용 후에도 최소 피해 유지
- 매력과 저항이 현재 피해 공식에 영향을 주지 않음
- `BattleParticipant`가 전투 능력치 7종을 올바르게 저장함

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/BattleDamageCalculator.cs
Assets/ProjectDelta/Scripts/Application/BattleParticipant.cs
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs
Assets/ProjectDelta/Tests/EditMode/BattleDamageCalculatorTests.cs
Assets/ProjectDelta/Tests/EditMode/BattleParticipantTests.cs
```

---

## 검토 결과

최신 `main` 커밋 기준으로 다음 사항을 확인했다.

- `BattleParticipant`에서 `Penetration` 제거
- `Charm`, `Resistance` 추가
- 피해 계산식에서 관통 제거
- 테스트 전투의 플레이어·몬스터 능력치 전달 구조 정정
- 방어 행동의 기존 피해 감소 구조 유지
- 저장소 코드 검색 기준 `Penetration` 잔여 참조 0건

GitHub에 연결된 CI 상태 체크는 존재하지 않아
Unity Test Runner의 실제 실행 결과는 커밋 정보만으로 확인할 수 없다.

---

## 완료 결과

53일차를 통해 전투 능력치 구조를 기획 기준에 맞게 다시 정리했다.

이제 전투 시스템은 다음 7종 능력치를 기준으로 확장할 수 있다.

```text
공격력
방어력
속도
명중
회피
매력
저항
```

다음 단계에서는 마나·정력 자원을 도입하고
현재 테스트 상수 중심의 전투 데이터를 실제 런타임 데이터와 연결한다.

---

## 기준 커밋

```text
86dd77c279bb12d86d6b3cb70662faff8f6051f4
```

```text
53일차 : 전투 능력치 7종 정정 및 관통 제거·매력·저항 추가
```
