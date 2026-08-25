# Project Delta - 63일차 개발일지

## 작업 주제

**상태 성공률·지속시간·중첩 및 재적용 처리 구현**

---

## 개발 목표

61~62일차에 제작한 약화 상태 9종과 강화 상태 7종을 실제 전투 참가자에게 적용할 수 있도록 공통 상태 적용 규칙을 구현한다.

이번 일차의 핵심 범위는 다음과 같다.

```text
상태 성공률 계산
최종 성공률 5~95% 제한
낮음 / 보통 / 높음 표시 단계 계산
상태 적용 성공·실패 판정
동일 상태 재적용 처리
중독·출혈 최대 3중첩
지속시간 갱신
기절 중첩 불가 처리
```

실제 지속 피해, 기절에 의한 행동 건너뜀, 추가 행동, 전투 종료 시 상태 정리는 64일차에서 처리한다.

---

## 주요 작업 내용

### 1. 상태 성공률 계산 서비스 구현

`StatusEffectApplicationService`를 추가하여 상태 적용 확률 계산을 한 곳에서 처리하도록 구성했다.

최종 상태 이상 성공률 공식은 다음과 같다.

```text
최종 상태 이상 성공률
= 효과 기본 확률
+ 공격자 상태 보정
- 대상 저항
+ 스킬·장비·유물 보정
```

계산 결과는 최소 `5%`, 최대 `95%`로 제한한다.

이를 통해 상태를 부여하는 스킬이나 아이템이 추가되더라도 동일한 성공률 계산 규칙을 공통으로 사용할 수 있다.

---

### 2. 성공률 표시 단계 구현

전투 UI에서 상태 적용 가능성을 표시할 수 있도록 `StatusSuccessLevel`을 추가했다.

| 최종 성공률 | 표시 |
| ---: | --- |
| 5~34% | 낮음 |
| 35~69% | 보통 |
| 70~95% | 높음 |

확률 계산과 UI 표시 기준이 서로 다른 코드에 중복되지 않도록 `StatusEffectApplicationService.GetSuccessLevel()`에서 공통 처리한다.

---

### 3. 상태 적용 결과 구조 추가

`StatusEffectApplyResult`를 추가하여 상태 적용 시 다음 결과를 한 번에 반환하도록 구성했다.

```text
FinalSuccessChance
SuccessLevel
Roll
Succeeded
ActiveStackCount
RemainingRounds
```

상태 적용 성공 여부뿐 아니라 실제 굴림값, 최종 확률, 적용 후 중첩 수와 남은 지속시간까지 확인할 수 있다.

후속 전투 로그와 UI에서도 동일한 결과 데이터를 재사용할 수 있는 구조다.

---

### 4. 상태 적용 성공·실패 판정 구현

상태 적용 시 기존 `IRandomSource` 구조를 사용하여 `1~100` 범위의 굴림값을 생성한다.

```text
Roll <= FinalSuccessChance
```

조건을 만족하면 상태 적용 성공으로 처리하고, 초과하면 상태를 추가하거나 갱신하지 않는다.

핵심 전투 판정에서 `UnityEngine.Random`을 직접 사용하지 않고 기존 `CombatRng` 계층과 호환되는 구조를 유지했다.

---

### 5. 동일 상태 재적용 및 중첩 규칙 구현

기존 `StatusStackRule`의 세 규칙을 실제 적용 로직에 연결했다.

#### NoStack

기절과 같이 중첩할 수 없는 상태에 사용한다.

이미 동일 상태가 존재하면 중첩 수와 남은 지속시간을 변경하지 않는다.

#### RefreshDuration

약화, 둔화, 혼란, 침묵과 강화 상태 등에 사용한다.

동일 상태를 다시 부여해도 중첩 수는 증가하지 않고 지속시간만 새 값으로 갱신한다.

#### Stack

중독과 출혈에 사용한다.

재적용 시 중첩 수를 증가시키며 `MaxStack`을 넘지 않는다. 중독과 출혈의 기존 데이터는 `MaxStack = 3`으로 구성되어 있으므로 최대 3중첩까지만 적용된다.

최대 중첩에 도달한 뒤 다시 적용해도 중첩 수는 증가하지 않지만 지속시간은 갱신한다.

---

### 6. StatusEffectInstance 확장

기존 `StatusEffectInstance`에 재적용 처리를 위한 API를 추가했다.

```text
RefreshDuration()
IncreaseStack()
```

`RefreshDuration()`은 동일 상태 재부여 시 남은 지속시간을 새 값으로 갱신한다.

`IncreaseStack()`은 최대 중첩 수를 넘지 않도록 현재 중첩 수를 증가시킨다.

기존 생성자와 `DecrementRemainingRounds()` 사용 방식은 유지하여 기존 상태 지속시간 감소 흐름과 호환되도록 구성했다.

---

### 7. EditMode 테스트 추가

`StatusEffectApplicationServiceTests`를 추가하여 63일차 핵심 규칙을 검증할 수 있도록 했다.

테스트 범위는 다음과 같다.

```text
상태 성공률 기본 공식
최소 5% 제한
최대 95% 제한
낮음 5~34% 경계
보통 35~69% 경계
높음 70~95% 경계
확률 실패 시 상태 미적용
최초 적용 시 1중첩 생성
RefreshDuration 재적용
Stack 최대 3중첩
최대 중첩 이후 지속시간 갱신
NoStack 재적용
대상 Resistance 반영
```

NUnit 기준 총 15개의 테스트 케이스가 정의되어 있다.

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Application/StatusEffectApplicationService.cs
Assets/ProjectDelta/Scripts/Application/StatusEffectApplicationService.cs.meta

Assets/ProjectDelta/Scripts/Application/StatusEffectApplyResult.cs
Assets/ProjectDelta/Scripts/Application/StatusEffectApplyResult.cs.meta

Assets/ProjectDelta/Scripts/Application/StatusEffectInstance.cs

Assets/ProjectDelta/Scripts/Application/StatusSuccessLevel.cs
Assets/ProjectDelta/Scripts/Application/StatusSuccessLevel.cs.meta

Assets/ProjectDelta/Tests/EditMode/StatusEffectApplicationServiceTests.cs
Assets/ProjectDelta/Tests/EditMode/StatusEffectApplicationServiceTests.cs.meta
```

기존 `StatusEffectDefinition`과 `SE001 ~ SE016` 상태 에셋은 수정하지 않았다.

---

## 확인 사항

최신 `main` 커밋 `32abb4a52240fb452376036a47cacc509f954b1b` 기준으로 다음 항목을 확인했다.

- 63일차 상태 적용 서비스 추가
- 최종 성공률 공식 구현
- 최종 성공률 `5~95%` 제한
- `낮음 / 보통 / 높음` 표시 단계 구현
- 대상 `Resistance` 반영
- 기존 `IRandomSource` 기반 확률 굴림 사용
- `NoStack / RefreshDuration / Stack` 실제 적용 로직 연결
- `StatusEffectInstance` 지속시간 갱신 API 추가
- `StatusEffectInstance` 중첩 증가 API 추가
- 중독·출혈 최대 3중첩 구조 지원
- 상태 적용 결과 데이터 구조 추가
- EditMode 테스트 15개 케이스 정의
- 상태 정의 에셋과 기존 16종 상태 데이터 변경 없음

GitHub 저장소에는 해당 커밋을 대상으로 보고된 CI 상태 체크가 없다.

따라서 저장소 diff 기준으로 명백한 코드 누락이나 충돌은 확인되지 않았지만, Unity Editor에서의 실제 스크립트 컴파일과 Test Runner 통과 여부는 별도 실행 결과가 있어야 최종적으로 확정할 수 있다.

---

## 이번 일차 완료 상태

63일차 목표인 **상태 성공률·지속시간·중첩 및 재적용 공통 처리 구조**를 구현했다.

61~62일차에 준비한 상태 정의 데이터를 실제 전투 시스템에서 적용할 수 있는 기반이 마련되었다.

이번 일차에서는 상태가 성공적으로 등록되고 유지되는 규칙까지만 담당하며, 상태가 전투 행동과 능력치에 실제 영향을 주는 처리는 다음 일차로 분리한다.

---

## 다음 단계

64일차에서는 실제 상태 효과 실행을 구현한다.

기획 기준 핵심 범위는 다음과 같다.

```text
중독·출혈 지속 피해
재생 지속 회복
기절 시 차례 건너뜀
추가 행동 처리
전투 종료 시 전투용 상태 정리
상태 효과와 라운드 파이프라인 연결
```
