# Project Delta - 72일차 개발일지

## 작업 주제

**전투 승리 보상 선택 및 탐험 복귀 흐름 구현**

---

## 개발 목표

71일차까지 패배 시 `DefeatScene`으로 이동하는 정식 패배 흐름을 구현했지만, 승리 시에는 전투가 끝난 직후 바로 Encounter가 완료되고 탐험으로 돌아가는 구조였다.

72일차에서는 전투 승리 후 즉시 탐험으로 복귀하지 않고 다음 흐름을 거치도록 변경했다.

```text
전투 승리
→ 보상 후보 생성
→ 보상 선택 UI 표시
→ 보상 하나 선택
→ 플레이어 런 상태에 보상 적용
→ Encounter 완료
→ 진행 상태 저장
→ 탐험 복귀
```

이번 일차에서는 향후 아이템·장비·스킬·유물 보상으로 확장할 수 있는 기본 보상 선택 구조를 먼저 구축했다.

---

## 주요 작업 내용

### 1. BattleRewardState 추가

전투 승리 후 표시할 보상 후보와 선택 상태를 관리하는 `BattleRewardState`를 추가했다.

현재 테스트용 기본 보상은 다음 3종이다.

```text
REWARD_GOLD_100  → 골드 +100
REWARD_HEAL_10   → HP +10
REWARD_MANA_5    → MP +5
```

보상 후보는 `BattleRewardOption`으로 표현하며 다음 정보를 가진다.

```text
Id
DisplayName
Type
Amount
```

보상 종류는 현재 다음 세 가지로 구성했다.

```text
Gold
Health
Mana
```

---

### 2. 보상 선택 및 지급 처리 구현

`BattleRewardState.TryClaim()`에서 선택한 보상을 실제 `PlayerRunState`에 적용하도록 했다.

골드 보상:

```text
PlayerRunState.Gold 증가
```

HP 보상:

```text
CurrentHp 회복
최대 체력 초과 방지
```

MP 보상:

```text
CurrentMana 회복
최대 마나 초과 방지
```

보상을 한 번 선택하면 `IsPending`을 false로 변경해 같은 전투에서 두 번째 보상을 받을 수 없도록 했다.

---

### 3. 전투 승리 직후 Encounter 종료 보류

기존 승리 흐름에서는 `FinishBattle(BattleOutcome.Victory)` 내부에서 즉시:

```text
FinalizeActiveEncounter()
→ BattleSession Reset
→ 탐험 복귀
```

가 진행됐다.

72일차에서는 승리 결과를 `pendingVictoryEncounterResult`에 임시 보관한 뒤 보상 선택 상태로 전환하도록 수정했다.

```text
BattleOutcome.Victory
→ EncounterResult 생성
→ pendingVictoryEncounterResult 저장
→ BattleRewardState.BeginDefaultRewards()
→ 보상 선택 대기
```

이 시점에서는 Encounter를 완료하지 않기 때문에 플레이어는 보상을 선택하기 전까지 탐험으로 복귀하지 않는다.

---

### 4. ConfirmBattleReward 구현

`ExplorationMonsterEncounterController`에 보상 선택 완료용 `ConfirmBattleReward()`를 추가했다.

보상이 정상적으로 선택되면 다음 순서로 처리한다.

```text
보상 지급
→ pendingVictoryEncounterResult 제거
→ FinalizeActiveEncounter()
→ 방 Encounter 완료 처리
→ 진행 상태 저장
→ BattleSession Reset
→ 탐험 조작 복구
```

보상 선택을 Encounter 완료보다 먼저 처리하기 때문에 보상 결과도 이후 런 저장에 함께 반영된다.

---

### 5. 보상 상태 초기화 처리

이전 전투의 보상 상태가 다음 Encounter에 남지 않도록 다음 시점에 `BattleRewardState.Clear()`를 호출하도록 정리했다.

```text
새 Battle 시작
Encounter 강제 중단
ExplorationMonsterEncounterController 비활성화
```

`pendingVictoryEncounterResult`도 동일한 시점에 초기화한다.

---

### 6. BattleRewardPanelController 추가

전투 승리 후 보상 UI를 표시하고 버튼 입력을 처리하는 `BattleRewardPanelController`를 추가했다.

컨트롤러는 다음 조건을 확인한다.

```text
ExplorationMonsterEncounterController 존재
IsBattleRewardPending == true
BattleRewardState.IsPending == true
```

조건을 만족하면 보상 패널을 표시한다.

각 버튼에는 현재 `BattleRewardState.CurrentOptions`의 보상 이름을 연결하고, 클릭 시 해당 보상 ID를 `ConfirmBattleReward()`에 전달한다.

---

### 7. 기존 전투 HUD에 BattleRewardPanel 추가

별도의 Scene으로 이동하지 않고 `DungeonScene`의 기존 전투 HUD 내부에 보상 패널을 추가했다.

기본 구조:

```text
Battle HUD
└─ BattleRewardPanel
   ├─ TitleText
   ├─ GuideText
   ├─ RewardButton_Gold
   │  └─ Label
   ├─ RewardButton_Health
   │  └─ Label
   └─ RewardButton_Mana
      └─ Label
```

표시 문구는 다음과 같다.

```text
전투 승리

보상 하나를 선택하세요.

[골드 +100] [HP +10] [MP +5]
```

평상시에는 패널을 비활성화하고, 승리 보상 선택 상태일 때만 표시한다.

---

### 8. Day72BattleRewardInstaller 추가

Unity Editor에서 보상 UI를 자동 설치할 수 있도록 `Day72BattleRewardInstaller`를 추가했다.

메뉴:

```text
Project Delta
→ 72일차
→ 72일차 보상 UI 적용
```

실행 시 `DungeonScene`의 기존 Battle HUD를 찾아 `BattleRewardPanel`을 생성하고, `BattleRewardPanelController`의 버튼·텍스트·Encounter Controller 참조를 자동으로 연결한다.

---

## EditMode 테스트 추가

### BattleRewardStateTests

다음 5가지 동작을 검사하는 테스트를 추가했다.

```text
기본 보상 후보 3개 생성
골드 +100 적용 및 선택 종료
HP 회복 시 최대 체력 초과 방지
MP +5 적용
첫 보상 수령 이후 두 번째 보상 수령 거부
```

---

## 변경 파일

```text
Assets/ProjectDelta/Scenes/DungeonScene.unity

Assets/ProjectDelta/Scripts/Application/BattleRewardState.cs (신규)

Assets/ProjectDelta/Scripts/Editor/Day72BattleRewardInstaller.cs (신규)

Assets/ProjectDelta/Scripts/Presentation/BattleRewardPanelController.cs (신규)
Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs

Assets/ProjectDelta/Tests/EditMode/BattleRewardStateTests.cs (신규)
```

신규 Script에 대응하는 `.meta` 파일도 함께 추가되었다.

---

## 확인 사항

- 전투 승리 시 즉시 Encounter가 종료되지 않도록 변경
- 승리 시 테스트 보상 후보 3종 생성
- 보상 선택 전 탐험 복귀 보류
- 골드 +100 적용
- HP +10 적용 및 최대 체력 초과 방지
- MP +5 적용 및 최대 마나 초과 방지
- 한 전투에서 보상 1개만 수령 가능
- 보상 선택 후 기존 Encounter 완료 처리 재사용
- Encounter 완료 후 기존 진행 상태 저장 흐름 유지
- 보상 처리 후 BattleSession 초기화
- 탐험 조작 정상 복구 구조 연결
- DungeonScene 기존 전투 HUD 내부에 보상 패널 추가
- 보상 버튼 3개와 `BattleRewardPanelController` 참조 연결
- EditMode 테스트 5종 추가

최신 72일차 커밋의 변경 파일과 코드 연결을 정적으로 점검한 기준에서는 추가적인 명백한 컴파일 차단 문제를 확인하지 못했다.

다만 GitHub 최신 커밋에는 CI 또는 Unity Test Runner 실행 결과가 등록되어 있지 않으므로, Unity Editor 실제 컴파일과 EditMode Test Runner 통과 여부는 로컬 환경에서 최종 확인해야 한다.

---

## 이번 일차 완료 상태

72일차 목표인 **전투 승리 후 보상 선택 → 보상 지급 → Encounter 완료 → 탐험 복귀**의 기본 흐름을 구현했다.

현재 보상은 테스트용으로 골드·HP·MP 3종만 사용하지만, 이후 동일한 선택 흐름에 아이템·장비·스킬·유물 등의 실제 게임 보상 데이터를 연결할 수 있는 기반이 마련됐다.

---

## 다음 단계

다음 일차에서는 현재 보상 선택 구조를 기반으로 기획서 개발 일정의 다음 항목을 확인하고 실제 보상 데이터 확장 또는 다음 전투 시스템을 이어서 구현한다.
