# Project Delta - 62일차 개발일지

## 작업 주제

**강화 상태 7종 StatusEffectDefinition 데이터 제작**

---

## 개발 목표

61일차에 약화 상태이상 9종을 등록한 데 이어, 전투에서 사용할 강화 상태 7종을 동일한 `StatusEffectDefinition` 구조로 추가한다.

이번 일차의 범위는 **강화 상태 정의 데이터 제작과 재적용 규칙 지정**까지다.

실제 능력치 상승, 재생 회복, 상태 성공률, 지속시간 계산, 중첩·재적용 판정은 후속 일차에서 구현한다.

---

## 주요 작업 내용

### 1. 강화 상태 7종 데이터 에셋 제작

`Assets/ProjectDelta/Data/StatusEffects/` 폴더에 다음 7종의 `StatusEffectDefinition` 에셋을 추가했다.

| ID | 에셋 | 표시 이름 |
| --- | --- | --- |
| SE010 | Regeneration | 재생 |
| SE011 | AttackUp | 공격 상승 |
| SE012 | DefenseUp | 방어 상승 |
| SE013 | SpeedUp | 속도 상승 |
| SE014 | AccuracyUp | 명중 상승 |
| SE015 | EvasionUp | 회피 상승 |
| SE016 | ResistanceUp | 저항 상승 |

61일차의 약화 상태 9종과 합쳐 현재 상태 데이터 ID 범위는 `SE001 ~ SE016`이다.

---

### 2. 강화 상태 재적용 규칙 지정

강화 상태 7종은 모두 중첩 수를 증가시키지 않고 동일 상태가 다시 적용될 때 지속시간을 갱신하는 규칙을 참조하도록 구성했다.

| 상태 | StackRule | MaxStack |
| --- | --- | ---: |
| 재생 | RefreshDuration | 1 |
| 공격 상승 | RefreshDuration | 1 |
| 방어 상승 | RefreshDuration | 1 |
| 속도 상승 | RefreshDuration | 1 |
| 명중 상승 | RefreshDuration | 1 |
| 회피 상승 | RefreshDuration | 1 |
| 저항 상승 | RefreshDuration | 1 |

현재 `StatusStackRule`에서 강화 상태는 `RefreshDuration` 규칙을 사용하는 대상으로 정의되어 있으며, 이번 일차에서는 해당 규칙을 데이터에 지정하는 것까지만 처리한다.

실제 재적용 시 기존 지속시간을 어떻게 갱신할지는 63일차 상태 성공률·지속시간·중첩 처리에서 구현한다.

---

### 3. 기존 상태 시스템 구조 재사용

이번 작업에서는 새로운 상태 시스템 코드를 추가하지 않았다.

강화 상태 7종은 기존 `StatusEffectDefinition`의 다음 필드를 그대로 사용한다.

```text
Id
DisplayName
DurationType
StackRule
MaxStack
TickTiming
RoundEndValue
```

현재 데이터 값은 다음 원칙으로 구성했다.

```text
DurationType = Rounds
StackRule = RefreshDuration
MaxStack = 1
TickTiming = RoundEnd
RoundEndValue = 0
```

`RoundEndValue`는 현재 0이므로 이번 일차에서 재생이나 능력치 상승이 실제 전투 효과로 발동하지 않는다.

---

## 변경 파일

```text
Assets/ProjectDelta/Data/StatusEffects/Regeneration.asset
Assets/ProjectDelta/Data/StatusEffects/Regeneration.asset.meta

Assets/ProjectDelta/Data/StatusEffects/AttackUp.asset
Assets/ProjectDelta/Data/StatusEffects/AttackUp.asset.meta

Assets/ProjectDelta/Data/StatusEffects/DefenseUp.asset
Assets/ProjectDelta/Data/StatusEffects/DefenseUp.asset.meta

Assets/ProjectDelta/Data/StatusEffects/SpeedUp.asset
Assets/ProjectDelta/Data/StatusEffects/SpeedUp.asset.meta

Assets/ProjectDelta/Data/StatusEffects/AccuracyUp.asset
Assets/ProjectDelta/Data/StatusEffects/AccuracyUp.asset.meta

Assets/ProjectDelta/Data/StatusEffects/EvasionUp.asset
Assets/ProjectDelta/Data/StatusEffects/EvasionUp.asset.meta

Assets/ProjectDelta/Data/StatusEffects/ResistanceUp.asset
Assets/ProjectDelta/Data/StatusEffects/ResistanceUp.asset.meta
```

기존 스크립트와 약화 상태 에셋은 수정하지 않았다.

---

## 확인 사항

최신 저장소 기준으로 다음 항목을 확인했다.

- 강화 상태 7종 에셋 존재
- ID `SE010 ~ SE016` 구성 확인
- 모든 에셋이 기존 `StatusEffectDefinition`을 참조
- 7종 모두 `RefreshDuration` 규칙 사용
- 7종 모두 `MaxStack = 1`
- `DurationType = Rounds`
- `RoundEndValue = 0`
- 신규 `.asset` 7개와 대응 `.meta` 7개만 추가
- 기존 61일차 상태이상 데이터 및 스크립트 변경 없음

GitHub 저장소에는 최신 커밋을 대상으로 실행된 CI 상태 체크가 없으므로, Unity 에디터 재컴파일 및 EditMode/PlayMode 테스트 실행 여부는 이 개발일지에서 완료로 단정하지 않는다.

---

## 이번 일차 완료 상태

62일차 목표인 **강화 상태 7종 Definition 데이터 제작 및 기본 재적용 규칙 지정**을 완료했다.

현재 상태 데이터는 약화 9종과 강화 7종을 합쳐 총 16종이 준비된 상태다.

실제 상태 부여 성공률, 지속시간 결정, 중첩 및 재적용 판정, 재생·능력치 상승 효과는 이번 일차 범위에 포함하지 않는다.

---

## 다음 단계

63일차에서는 상태이상과 강화 상태를 실제 전투 참가자에게 부여할 때 사용하는 **상태 성공률·지속시간·중첩 및 재적용 처리**를 구현한다.

기획 기준 핵심 범위는 다음과 같다.

```text
상태 성공률 5~95%
지속시간 결정
중독·출혈 최대 3중첩
NoStack / RefreshDuration / Stack 판정
상태 지속시간 표시 기준 연결
```
