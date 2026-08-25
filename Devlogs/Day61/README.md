# Project Delta - 61일차 개발일지

## 작업 주제

**약화 상태이상 9종 StatusEffectDefinition 데이터 제작**

---

## 개발 목표

60일차에 마련한 공통 상태이상 구조를 기반으로 실제 전투에서 사용할 약화 상태이상 9종을 데이터 에셋으로 등록한다.

이번 일차의 범위는 **상태이상 정의 데이터 제작과 중첩 규칙 지정**까지다.

상태이상 적용 성공률, 지속시간 계산, 재적용 처리, 실제 행동 제한·지속 피해·능력치 변화 등의 전투 로직은 후속 일차에서 구현한다.

---

## 주요 작업 내용

### 1. 약화 상태이상 9종 데이터 에셋 제작

`Assets/ProjectDelta/Data/StatusEffects/` 폴더에 다음 9종의 `StatusEffectDefinition` 에셋을 추가했다.

| ID | 에셋 | 표시 이름 |
| --- | --- | --- |
| SE001 | Poison | 중독 |
| SE002 | Bleeding | 출혈 |
| SE003 | Weakness | 약화 |
| SE004 | Slow | 둔화 |
| SE005 | Stun | 기절 |
| SE006 | Confusion | 혼란 |
| SE007 | Silence | 침묵 |
| SE008 | Bind | 구속 |
| SE009 | Charm | 매혹 |

각 에셋은 공통 `StatusEffectDefinition` 구조를 사용한다.

```text
Id
DisplayName
DurationType
StackRule
MaxStack
TickTiming
RoundEndValue
```

---

### 2. 상태이상별 중첩 규칙 지정

상태이상 데이터가 이후 전투 시스템에서 동일한 규칙을 참조할 수 있도록 중첩 방식을 지정했다.

| 상태이상 | StackRule | MaxStack |
| --- | --- | ---: |
| 중독 | Stack | 3 |
| 출혈 | Stack | 3 |
| 약화 | NoStack | 1 |
| 둔화 | RefreshDuration | 1 |
| 기절 | NoStack | 1 |
| 혼란 | RefreshDuration | 1 |
| 침묵 | RefreshDuration | 1 |
| 구속 | NoStack | 1 |
| 매혹 | NoStack | 1 |

중독과 출혈은 최대 3중첩 데이터를 갖는다.

둔화·혼란·침묵은 동일 상태가 다시 적용될 때 중첩 수를 증가시키지 않고 지속시간을 갱신하는 규칙을 참조한다.

이번 일차에서는 규칙을 **데이터로 지정하는 것까지만** 처리하며, 실제 중첩·갱신 판정은 후속 전투 로직에서 구현한다.

---

### 3. StatusEffectDefinition 데이터 구조 정리

`StatusEffectDefinition`은 상태이상 에셋에서 필요한 표시 정보와 지속·중첩·라운드 처리 값을 노출하도록 유지했다.

```text
DisplayName
DurationType
StackRule
MaxStack
TickTiming
RoundEndValue
```

기존 `DefinitionBase`의 `Id`를 그대로 사용하며, 9개 상태이상 에셋은 동일한 ScriptableObject 정의를 참조한다.

---

## 변경 파일

```text
Assets/ProjectDelta/Data/StatusEffects.meta

Assets/ProjectDelta/Data/StatusEffects/Poison.asset
Assets/ProjectDelta/Data/StatusEffects/Bleeding.asset
Assets/ProjectDelta/Data/StatusEffects/Weakness.asset
Assets/ProjectDelta/Data/StatusEffects/Slow.asset
Assets/ProjectDelta/Data/StatusEffects/Stun.asset
Assets/ProjectDelta/Data/StatusEffects/Confusion.asset
Assets/ProjectDelta/Data/StatusEffects/Silence.asset
Assets/ProjectDelta/Data/StatusEffects/Bind.asset
Assets/ProjectDelta/Data/StatusEffects/Charm.asset

각 에셋의 .meta 파일

Assets/ProjectDelta/Scripts/Data/StatusEffectDefinition.cs
```

---

## 확인 사항

최신 저장소 기준으로 다음 항목을 확인했다.

- 약화 상태이상 9종 에셋 존재
- ID `SE001` ~ `SE009` 구성 확인
- 모든 에셋이 `StatusEffectDefinition`을 참조
- 중독·출혈 `Stack / MaxStack 3` 확인
- 둔화·혼란·침묵 `RefreshDuration` 확인
- 나머지 상태이상 `NoStack` 확인
- `StatusDurationType`, `StatusStackRule`, `StatusTickTiming`의 현재 enum 정의와 직렬화 값이 충돌하지 않음
- 이전에 발생했던 존재하지 않는 enum 멤버 참조 문제 없음

GitHub 저장소에는 현재 이 커밋을 대상으로 실행된 CI 체크가 없으므로, Unity 에디터 자체 재컴파일 및 EditMode/PlayMode 테스트 실행 여부는 이 개발일지에서 완료로 단정하지 않는다.

---

## 이번 일차 완료 상태

61일차 목표인 **약화 상태이상 9종 Definition 데이터 제작과 기본 중첩 규칙 설정**을 완료했다.

실제 상태이상 성공 판정, 지속시간 처리, 재적용, 행동 제한, 지속 피해 및 능력치 변화는 이번 일차 범위에 포함하지 않는다.

---

## 다음 단계

상태이상 데이터가 준비되었으므로 후속 일차에서는 이 Definition을 실제 전투 참가자에게 적용하는 과정과 지속시간·재적용·효과 처리 로직을 연결한다.
