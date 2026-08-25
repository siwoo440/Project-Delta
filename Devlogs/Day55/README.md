# Project Delta - 55일차 개발일지

## 작업 주제

**피해 공식 비율형 전환 + 95~105% 편차**

---

## 개발 목표

53일차부터 임시로 쓰던 `공격력 - 방어력` 뺄셈 피해 공식을 기획서 4.2가 정의하는
비율형 공식으로 바꾸고, 95~105% 무작위 편차를 추가한다.

```text
기본 피해 = 공격력 × 공격 배율 × 100 ÷ (100 + 대상 방어력)
최종 피해 = 기본 피해 × 95~105% 무작위 편차 × 치명타 배율 × 기타 보정
최종 피해가 1보다 작으면 1로 처리
```

이번 일차는 비율형 전환과 편차까지만 다룬다. "공격 배율"은 스킬 데이터
(66일차 이후)에서 오는 값이라 기본 공격은 배율 100%로 취급했고,
치명타 배율·기타 보정은 58일차 이후 별도 항목에서 곱한다.

---

## 주요 작업 내용

### 1. BattleDamageCalculator 피해 공식 교체

`CalculateDamage`의 `공격력 - 방어력` 뺄셈 공식을
`공격력 × 100 ÷ (100 + 방어력)` 비율형 공식으로 바꿨다.

```text
baseDamage = attacker.Attack * 100 / (100 + defender.Defense)
```

방어력이 클수록 피해가 완만하게 줄어드는 감쇠 곡선이 되고,
방어력 0일 때는 기존과 동일하게 공격력이 그대로 적용된다.

### 2. 95~105% 무작위 편차 추가

`varianceRoll`(0~10, 11칸) 인자를 추가해 95~105% 편차를 11단계로 매핑했다.

```text
variancePercent = 95 + Clamp(varianceRoll, 0, 10)
damage = baseDamage * variancePercent / 100
```

범위를 벗어난 `varianceRoll`이 들어오면 가장 가까운 경계(95% 또는 105%)로
고정한다.

방어 중 50% 감소(52일차)와 최소 피해 1 보장은 편차 적용 다음 순서로 유지했다.

### 3. Resolve 시그니처 확장

`Resolve(attacker, defender, roll0To99)`에 `varianceRoll` 인자를 추가했다.
명중 판정용 난수(0~99)와 편차용 난수(0~10)를 각각 밖에서 주입받는 기존
패턴을 그대로 유지했다.

### 4. ExplorationMonsterEncounterController 난수 생성 추가

`ConfirmAttack()`에서 기존 명중 난수(`hitRoll`, 0~99)에 더해 편차 난수
(`varianceRoll`, `UnityEngine.Random.Range(0, DamageVarianceRollCount)`)를
만들어 `Resolve()`에 함께 넘기도록 했다.

### 5. EditMode 테스트 갱신

`CalculateDamage`·`Resolve` 호출부가 모두 새 인자를 받도록 시그니처를
바꾸면서 기존 뺄셈 공식 기준 기대값을 전부 비율 공식 기준으로 다시 계산했다.

새로 추가한 검증 항목:

- 방어력 100일 때 비율 공식 결과 확인 (10 × 100 ÷ 200 = 5)
- 방어력 0일 때 기존과 동일하게 공격력 그대로 적용됨
- `varianceRoll` 최솟값(0) → 95%, 최댓값(10) → 105% 적용 확인
- `varianceRoll`이 범위를 벗어나면 가장 가까운 경계로 고정됨
- 매력·저항이 여전히 피해 공식에 영향을 주지 않음 (10 × 100 ÷ 104 = 9로 갱신)

### 6. 피해 공식 디버그 창 추가

편차 난수가 실제로 굴러가는지 화면에서 바로 확인할 수 있도록 디버그 전용
창을 추가했다.

`BattleDamageResult`에 편차 적용 전 기본 피해(`BaseDamage`)와 실제 적용된
편차(`VariancePercent`)를 추가하고, `BattleDamageCalculator.CalculateDamage`
내부 로직을 `CalculateBaseDamage`·`CalculateVariancePercent`로 분리해
`Resolve()`가 이 값들을 함께 반환하도록 했다.

`ExplorationMonsterEncounterController`에 `LastDamageFormulaDebugText`
속성을 추가해, 공격이 확정될 때마다 다음과 같은 형식으로 채운다.

```text
PLAYER → MON_TEST#1 / 10 × 100 ÷ (100 + 4) = 9 → × 95% = 8 (적용 8) (95%)
```

빗나가면 `"빗나감 (명중률 70%, 편차 미적용)"`으로 표시한다.

이 텍스트를 화면에 그리는 `BattleDamageDebugOverlay`(신규)를 추가했다.
`OnGUI`로 좌상단에 작은 박스를 그리는 순수 디버그 오버레이로, 정식 전투
화면(`BattleHudController`)과는 무관하다. F9 키로 껐다 켤 수 있다. 씬의
`Player` 오브젝트에 직접 연결해뒀다.

처음에는 `UnityEngine.Input.GetKeyDown`으로 F9를 읽었는데, 이 프로젝트는
Player Settings에서 Active Input Handling이 새 Input System으로 전환되어
있어 `InvalidOperationException`이 발생했다. 다른 컨트롤러들과 같은 방식인
`UnityEngine.InputSystem.Keyboard.current.f9Key.wasPressedThisFrame`으로
교체해 해결했다.

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/BattleDamageCalculator.cs
Assets/ProjectDelta/Scripts/Application/BattleDamageResult.cs
Assets/ProjectDelta/Scripts/Presentation/BattleDamageDebugOverlay.cs (신규)
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs
Assets/ProjectDelta/Scenes/DungeonScene.unity
Assets/ProjectDelta/Tests/EditMode/BattleDamageCalculatorTests.cs
```

---

## 남은 과제

- 치명타 배율·피해 유형별 방어 수치(고정 피해는 방어 무시, 상태이상은 저항)는
  58일차에서 다룬다.
- 명중 공식(스킬 기본값, 회피×0.5, 5~95%)은 56일차에서 정정한다.
- 방어 감소율 곡선(현재는 고정 50%)은 57일차에서 다룬다.
- `BattleDamageDebugOverlay`는 디버그 전용이라 정식 빌드에서 끄거나
  제거하는 절차는 아직 정하지 않았다.

Unity 에디터에서 실제 플레이로 컴파일·런타임(F9 토글 포함)까지 확인했다.

---

## 다음 단계

56일차에서는 명중 공식을 스킬 기본값 기반으로 정정하고,
회피 반영 비율을 0.5로 낮추며, 명중률 범위를 5~95%로 좁힌다.
