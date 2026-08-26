# Project Delta - 74일차 개발일지

## 작업 주제

**몬스터 AI 행동 선택 및 BattleIntent 실제 실행 연동**

---

## 개발 목표

73일차에서 몬스터의 다음 행동을 미리 저장하고 HUD에 표시하는 `BattleIntent` 기반 행동 예고 시스템을 구축했다.

74일차에서는 이 구조 위에 실제 몬스터 AI를 연결하여, 몬스터가 전투 상황을 기준으로 다음 행동을 선택하고 선택한 행동을 Intent로 예고한 뒤 자신의 차례에 동일한 행동을 실행하도록 확장했다.

핵심 흐름은 다음과 같다.

```text
몬스터 상태 확인
→ 사용 가능한 행동 후보 생성
→ 가중치 기반 행동 선택
→ BattleIntent 저장
→ HUD에 행동 예고
→ 몬스터 차례
→ 저장된 Intent 그대로 실행
→ Intent 소비
→ 다음 행동 준비
```

---

## 1. MonsterAiProfile 추가

몬스터마다 AI 성향을 데이터로 설정할 수 있도록 `MonsterAiProfile` ScriptableObject를 추가했다.

주요 설정값은 다음과 같다.

```text
기본 공격 가중치
방어 가중치
저체력 기준 %
저체력 상태에서 추가되는 방어 가중치
사용 가능한 스킬 목록
각 스킬의 선택 가중치
```

AI 판단 수치를 코드에 직접 고정하지 않고 데이터로 분리했기 때문에 이후 몬스터별로 서로 다른 전투 성향을 만들 수 있다.

예:

```text
공격형 몬스터
→ 공격 가중치 높음

방어형 몬스터
→ 방어 가중치 높음

스킬형 몬스터
→ 스킬 가중치 높음

위기 대응형 몬스터
→ HP가 낮을수록 방어 확률 증가
```

---

## 2. MonsterDefinition에 AI Profile 연결

기존 `MonsterDefinition`에 `MonsterAiProfile` 참조를 추가했다.

```text
MonsterDefinition
├─ 기존 전투 능력치
└─ AiProfile
```

이를 통해 몬스터 정의 데이터와 행동 성향 데이터를 연결할 수 있게 됐다.

현재 테스트 몬스터에는 기본 AI Profile이 연결되어 있다.

---

## 3. 테스트 AI Profile 구성

테스트용 몬스터 AI는 다음 초기 가중치로 구성했다.

```text
기본 공격 : 55
방어      : 25
강공격    : 20
```

HP가 40% 이하라면:

```text
방어 가중치 +30
```

을 적용한다.

따라서 HP가 충분할 때는 공격 성향이 강하지만 체력이 낮아지면 방어 행동의 선택 가능성이 높아진다.

---

## 4. MonsterAiDecisionService 구현

실제 AI 행동 선택을 담당하는 `MonsterAiDecisionService`를 추가했다.

AI는 먼저 사용할 수 있는 행동 후보를 만든다.

```text
Attack
Defend
Skill
```

후보를 만든 뒤 현재 상태에서 사용할 수 없는 행동을 제거하고 남은 행동들의 가중치를 합산한다.

이후 `IRandomSource`를 사용해 최종 행동 하나를 선택한다.

```text
행동 후보 생성
→ 사용할 수 없는 후보 제거
→ 가중치 계산
→ 난수 판정
→ 최종 행동 선택
```

---

## 5. 자원 부족 스킬 후보 제외

AI가 사용할 수 없는 스킬을 선택한 뒤 실행에 실패하지 않도록, 행동 결정 단계에서 먼저 자원을 확인한다.

검사 자원:

```text
CurrentMana
CurrentStamina
```

예:

```text
스킬 요구 Mana = 5
현재 Mana = 0

→ Skill 후보 제외
→ Attack / Defend 중 선택
```

스킬 사용 가능 여부를 Intent 생성 전에 확인하므로 불필요한 실패 행동이 발생하지 않는다.

---

## 6. 대상이 없는 행동 후보 제외

Enemy 대상 스킬은 유효한 대상이 있을 때만 AI 후보에 포함된다.

```text
Player 생존
→ Enemy Target Skill 후보 사용 가능

Player 없음 또는 사망
→ Enemy Target Skill 후보 제외
```

대상이 필요한 기본 공격 역시 유효한 대상이 없으면 후보로 추가되지 않는다.

---

## 7. 저체력 방어 가중치

현재 HP 비율을 계산하여 설정된 저체력 기준 이하라면 방어 가중치를 추가한다.

테스트 설정:

```text
저체력 기준 : 40%
기본 방어 가중치 : 25
저체력 방어 보너스 : +30
```

따라서 HP 40% 이하에서는:

```text
방어 가중치 = 55
```

가 된다.

이 구조를 사용하면 이후 몬스터별로 위기 상황 행동 패턴을 다르게 설정할 수 있다.

---

## 8. BattleIntent 확장

기존 `BattleIntent`를 다음 행동들을 표현할 수 있도록 확장했다.

```text
Attack
Defend
Skill
```

추가된 생성 함수:

```text
CreateBasicAttack()
CreateDefend()
CreateSkill()
```

Skill Intent에는 기존 ID뿐 아니라 실제 `SkillDefinition` 참조도 저장한다.

따라서 몬스터 차례가 왔을 때 AI를 다시 실행하지 않고 미리 예고해 둔 스킬 데이터를 그대로 실행할 수 있다.

---

## 9. Intent와 실제 몬스터 행동 연결

기존 Enemy 자동 행동은 항상 기본 공격을 실행했다.

기존 흐름:

```text
Enemy 차례
→ Player 선택
→ ConfirmAttack()
```

74일차에서는 다음 구조로 변경했다.

```text
Enemy 차례
→ BattleIntent 조회

Attack
→ ConfirmAttack()

Defend
→ ConfirmDefend()

Skill
→ ConfirmSkill()

→ 행동 결과 처리
→ Intent 소비
```

따라서 HUD에서 예고한 행동과 실제 몬스터 행동이 일치한다.

예:

```text
HUD
[DEF] 방어

실제 차례
→ ConfirmDefend()
```

또는:

```text
HUD
[ATK] 강공격

실제 차례
→ ConfirmSkill(TestMonsterHeavyAttack)
```

---

## 10. 테스트용 강공격 스킬 추가

AI Skill 동작을 확인하기 위한 테스트용 `강공격` SkillDefinition을 추가했다.

```text
ID : SKILL_MON_HEAVY_ATTACK
이름 : 강공격
대상 : Enemy
Mana 비용 : 0
Stamina 비용 : 0
피해 배율 : 140%
```

현재 테스트 목적의 데이터이며 이후 실제 몬스터 스킬 데이터로 교체할 수 있다.

---

## 11. BattleIntentRuntimeController AI 연동

73일차 `BattleIntentRuntimeController`가 더 이상 모든 적에게 기본 공격만 등록하지 않고 `MonsterAiDecisionService`를 사용하도록 수정했다.

현재 처리 흐름:

```text
Enemy 확인
→ 현재 HP 확인
→ 상태이상 확인
→ 현재 자원 확인
→ AI Profile 조회
→ 행동 선택
→ BattleIntent 생성
→ BattleIntentService 등록
```

이미 등록된 Intent가 있다면 AI가 다시 판단하지 않는다.

즉 플레이어가 행동을 결정하는 동안 예고 행동이 임의로 바뀌지 않는다.

---

## 12. 침묵 상태의 Skill 후보 차단

침묵 상태의 몬스터는 AI 행동 후보 생성 단계에서 Skill을 선택하지 않도록 했다.

```text
침묵 없음
→ Attack / Defend / Skill

침묵 상태
→ Attack / Defend
```

일반 공격과 방어는 침묵 상태에서도 사용할 수 있다.

---

## 13. 침묵 상태 공통 판별 정책 추가

침묵 판별 코드가 여러 위치에 흩어지는 것을 막기 위해 `BattleStatusRestrictionPolicy`를 추가했다.

현재 활성 상태이상의 Definition ID에서:

```text
SILENCE
침묵
```

를 확인해 침묵 상태를 판별한다.

이 정책은:

```text
AI 후보 생성
Intent 실행 검증
SkillBattleCommand
```

에서 공통으로 사용할 수 있다.

---

## 14. SkillBattleCommand 실행 시 침묵 재검증

초기 74일차 점검 과정에서 다음 예외를 확인했다.

```text
몬스터가 Skill Intent 예고
→ 같은 프레임에 침묵 적용
→ BattleIntentRuntimeController.Update 이전에 Enemy 차례 실행
→ 기존 Skill Intent 실행 가능
```

이를 방지하기 위해 `SkillBattleCommand.Execute()`에서도 침묵 여부를 다시 확인하도록 수정했다.

현재는:

```text
침묵 상태
→ SkillBattleCommand.Execute()
→ Skill 사용 거부
```

가 된다.

따라서 Intent 시스템의 Update 실행 순서에 의존하지 않고 실제 Command 계층에서도 스킬 사용을 차단한다.

---

## 15. BattleIntentExecutionPolicy 추가

Intent 실행 직전 현재 전투 상태를 다시 검사하기 위한 `BattleIntentExecutionPolicy`를 추가했다.

검사 항목:

```text
기절
침묵
사망
만족 상태
대상 부재
```

몬스터 차례가 시작되면 저장된 Intent를 즉시 실행하기 전에 현재 상태를 다시 평가한다.

이로 인해 예고 이후 상태가 변경된 경우에도 기획서의 Intent 취소 규칙을 적용할 수 있다.

---

## 16. Intent 실행 직전 재검증

실제 `ExecuteEnemyIntent()` 흐름에 실행 직전 검증을 추가했다.

```text
저장된 Intent 조회
↓
현재 상태 재검사
↓
취소 조건 없음
→ 예고 행동 실행

취소 조건 발생
→ Intent 취소
→ 예고 행동 실행하지 않음
```

예:

```text
[강공격] 예고
↓
플레이어가 침묵 적용
↓
Enemy 차례
↓
Silenced 확인
↓
강공격 취소
```

---

## 17. 취소된 Intent를 일반 공격으로 대체하지 않도록 수정

Intent가 취소된 뒤 기존 Enemy fallback 코드가 기본 공격을 실행할 가능성을 차단했다.

기획 원칙:

```text
예고된 행동은 반드시 사용
단, 취소 조건 발생 시 해당 행동 취소
취소된 행동을 다른 행동으로 즉석 교체하지 않음
```

따라서:

```text
[강공격] 취소
→ 기본 공격으로 변경
```

하지 않는다.

대신:

```text
[강공격] 취소
→ 해당 Enemy의 현재 차례 소비
→ 다음 행동자로 진행
```

하도록 처리한다.

---

## 18. 취소 Intent 대기 상태 추가

추가 점검 과정에서 다음 예외를 확인했다.

```text
Skill Intent 취소
↓
BattleIntentService에서 Intent 제거
↓
다음 Runtime Update
↓
Intent가 없으므로 AI가 새 행동 생성
↓
공격 또는 방어로 교체될 가능성
```

이를 방지하기 위해 기존 취소 사유 기록을 `Pending Cancellation` 상태로 활용하도록 수정했다.

현재 흐름:

```text
Intent 취소
↓
취소 사유 저장
↓
HasPendingCancellation = true
↓
새 Intent 등록 금지
↓
해당 Enemy 차례까지 대기
↓
취소된 차례 소비
↓
TryConsumeCancellation()
↓
Pending Cancellation 해제
```

---

## 19. BattleIntentService 재등록 방지 강화

`BattleIntentService.TryRegister()`에서 해당 Actor에게 Pending Cancellation이 존재하면 새 Intent 등록을 거부한다.

따라서 Runtime 외 다른 코드가 새 Intent 등록을 시도하더라도 취소된 행동을 즉시 교체할 수 없다.

```text
Pending Cancellation 존재
→ TryRegister() 실패
```

취소된 차례가 실제로 소비된 이후:

```text
TryConsumeCancellation()
→ Pending 상태 제거
→ 다음 행동 Intent 등록 가능
```

이 된다.

---

## 20. Runtime의 취소 대기 Enemy 재선택 차단

`BattleIntentRuntimeController`에서도 Pending Cancellation 상태를 검사한다.

```text
Enemy에게 취소 대기 상태 있음
→ AI 행동 선택 건너뜀
→ 새로운 Intent 생성하지 않음
```

따라서 취소와 AI 선택의 실행 순서가 달라져도 동일한 규칙을 유지한다.

---

## 21. Enemy 차례에서 취소 대기 상태 우선 처리

`ExecuteEnemyIntent()`에서 Intent가 없을 경우 즉시 AI를 다시 실행하기 전에 Pending Cancellation을 먼저 확인하도록 수정했다.

```text
Intent 없음
↓
Pending Cancellation 확인

있음
→ 취소된 차례 소비

없음
→ AI를 통해 새 Intent 준비
```

이로써 취소된 행동을 새로운 행동으로 교체하는 마지막 예외를 차단했다.

---

## 22. 취소된 차례 소비 후 정상 복구

취소된 Enemy의 차례를 소비할 때:

```text
LastActingParticipant 갱신
LastActionSequence 증가
BattleActionResult 기록
```

을 수행한다.

Runtime은 해당 행동 Sequence 변경을 확인한 후:

```text
TryConsumeCancellation()
```

을 호출한다.

이후 다음 라운드부터는 정상적으로 새 Intent를 생성할 수 있다.

최종 흐름:

```text
Skill Intent 예고
↓
침묵
↓
Intent 취소
↓
새 행동 생성 금지
↓
Enemy 차례
↓
취소된 차례 소비
↓
Pending Cancellation 제거
↓
다음 라운드
↓
새 AI Intent 생성
```

---

## 23. AI 관련 EditMode 테스트

`MonsterAiDecisionServiceTests`를 추가해 다음 항목을 검증하도록 구성했다.

```text
AI Profile이 없을 때 기본 공격 fallback
저체력 상태에서 방어 가중치 증가
Mana 부족 스킬 후보 제외
자원이 충분한 스킬 선택 가능
침묵 상태에서 Skill 후보 제외
살아 있는 대상이 없으면 Enemy 대상 Skill 사용 불가
```

---

## 24. 침묵 Intent 회귀 테스트

`Day74SilenceIntentRegressionTests`를 추가했다.

검증 내용:

```text
이미 준비된 Skill Intent가 침묵 후 취소되는가
침묵이 일반 공격 Intent까지 취소하지 않는가
SkillBattleCommand가 침묵 Actor의 Skill을 직접 거부하는가
```

---

## 25. 취소 Intent 재생성 회귀 테스트

`Day74CancelledIntentHoldRegressionTests`를 추가했다.

검증 내용:

```text
취소된 Intent가 있는 동안 새 Intent 등록이 차단되는가
취소 사유가 해당 차례 소비 전까지 유지되는가
취소 상태를 소비한 뒤 새 Intent 등록이 다시 가능한가
```

---

## 주요 변경 파일

### 신규

```text
Assets/ProjectDelta/Scripts/Data/MonsterAiProfile.cs

Assets/ProjectDelta/Scripts/Application/MonsterAiDecisionService.cs
Assets/ProjectDelta/Scripts/Application/BattleStatusRestrictionPolicy.cs
Assets/ProjectDelta/Scripts/Application/BattleIntentExecutionPolicy.cs

Assets/ProjectDelta/Scripts/Editor/Day74MonsterAiInstaller.cs
Assets/ProjectDelta/Scripts/Editor/Day74SilenceIntentFixInstaller.cs
Assets/ProjectDelta/Scripts/Editor/Day74CancelledIntentHoldFixInstaller.cs

Assets/ProjectDelta/Tests/EditMode/MonsterAiDecisionServiceTests.cs
Assets/ProjectDelta/Tests/EditMode/Day74SilenceIntentRegressionTests.cs
Assets/ProjectDelta/Tests/EditMode/Day74CancelledIntentHoldRegressionTests.cs
```

### 수정

```text
Assets/ProjectDelta/Scripts/Data/MonsterDefinition.cs
Assets/ProjectDelta/Scripts/Application/BattleIntent.cs
Assets/ProjectDelta/Scripts/Application/BattleIntentService.cs
Assets/ProjectDelta/Scripts/Application/SkillBattleCommand.cs
Assets/ProjectDelta/Scripts/Presentation/BattleIntentRuntimeController.cs
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs
Assets/ProjectDelta/Scenes/DungeonScene.unity
```

### 데이터 추가 및 수정

```text
DefaultTestMonsterAiProfile.asset
TestMonsterHeavyAttack.asset
MonsterDefinition.asset의 AI Profile 연결
```

### 삭제

```text
없음
```

---

## 74일차 완료 기준

- MonsterAiProfile 데이터 구조 구현
- MonsterDefinition과 AI Profile 연결
- Attack / Defend / Skill AI 후보 지원
- AI 행동 가중치 선택 구현
- 저체력 방어 가중치 적용
- Mana 부족 Skill 후보 제외
- Stamina 부족 Skill 후보 제외
- 대상 부재 Skill 후보 제외
- 침묵 상태 Skill 후보 제외
- AI가 선택한 행동을 BattleIntent로 저장
- Attack Intent 실제 공격 실행
- Defend Intent 실제 방어 실행
- Skill Intent 실제 Skill 실행
- 예고된 Intent를 적 차례까지 고정
- 행동 실행 직전 Intent 취소 조건 재검증
- SkillBattleCommand 침묵 직접 차단
- 취소된 Intent를 다른 행동으로 교체하지 않음
- 취소된 Enemy의 차례 정상 소비
- Pending Cancellation 동안 새 Intent 생성 차단
- 차례 소비 후 Pending Cancellation 해제
- AI 기본 테스트 추가
- 침묵 회귀 테스트 추가
- 취소 Intent 재생성 회귀 테스트 추가

---

## 저장소 점검

74일차 최신 커밋 기준으로 AI Profile, AI 행동 선택, Intent 실행 연결, 침묵 재검증, 취소 행동의 차례 소비, Pending Cancellation 기반의 Intent 재생성 차단이 반영된 것을 확인했다.

정적 코드 흐름 기준으로 기존에 발견했던 다음 두 예외를 모두 보완했다.

```text
1. Skill Intent가 같은 프레임의 침묵을 무시하고 실행될 가능성

2. 취소된 Skill Intent 직후 AI가 공격/방어 Intent를 다시 생성해
   취소 행동을 다른 행동으로 교체할 가능성
```

현재 두 경우 모두 실제 행동 실행 전에 차단되도록 구성되어 있다.

GitHub 저장소에는 최신 커밋에 연결된 CI/Unity Test Runner 실행 기록이 없다.

따라서 저장소 코드 정적 검증과 별도로 로컬 Unity Editor에서 다음을 최종 확인한다.

```text
Unity Script Compile 성공
MonsterAiDecisionServiceTests 통과
Day74SilenceIntentRegressionTests 통과
Day74CancelledIntentHoldRegressionTests 통과

전투 중 Attack 예고와 실제 공격 일치
Defend 예고와 실제 방어 일치
강공격 예고와 실제 Skill 사용 일치
침묵 시 예고 Skill 취소
취소된 Skill이 일반 공격으로 교체되지 않음
취소된 차례 이후 다음 라운드 Intent 정상 생성
```

---

## 이번 일차 결과

73일차에서는 몬스터의 다음 행동을 단순히 저장하고 보여주는 기반을 만들었다.

74일차에서는 이 구조에 실제 AI 판단을 연결하여:

```text
전투 상황 분석
→ 행동 선택
→ 행동 예고
→ 예고 고정
→ 실제 차례에서 동일 행동 실행
```

이라는 전투 AI의 기본 흐름을 완성했다.

또한 상태 변화로 예고 행동을 실행할 수 없게 된 경우:

```text
예고 취소
→ 다른 행동으로 교체하지 않음
→ 해당 차례 소비
→ 다음 라운드부터 정상 AI 판단
```

이라는 예외 처리까지 정리했다.

이 구조를 바탕으로 다음 일차부터는 다수 Enemy 전투 규칙과 몬스터별 AI 데이터를 확장할 수 있다.

---

## 다음 단계

75일차에서는 **적 최대 3명 정합 및 몬스터 간 비공격 규칙**을 구현한다.

현재 과거 테스트 구조에는 적 슬롯 4칸을 사용하던 흔적이 남아 있으므로 기획 기준인 최대 3명으로 통일한다.

다음 목표:

```text
Enemy 최대 수 3명으로 통일
전투 생성 시 최대 3명 제한
HUD Enemy Slot 최대 3개 정합
AI Targeting에서 같은 Monster Team 대상 제외
몬스터끼리 서로 공격하지 못하도록 규칙 고정
관련 EditMode 테스트 추가
```
