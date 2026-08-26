---
# Project Delta - 81일차 개발 일지

**개발일:** 2026-08-26  
**기준 커밋:** `f163b729cad30f4578f581b65f9fc16122c03da8`  
**기준 이전 커밋:** `2428cdd3d281db49932ff353c2af9eb6b88e9650` (80일차)

---
## 1. 개발 목표

Google Drive 개발 일정의 81일차 목표인 **전투 보상 화면 정식화**를 진행했다.

- 전투 승리 후 경험치 결과 표시
- 레벨 변화 및 스탯 포인트 획득 결과 표시
- 획득 골드 표시
- 획득 아이템 표시
- 아이템이 없을 때도 `획득 아이템 없음` 표시
- 기존 추가 보상 3종 선택 흐름 유지
- 전투 드롭 결과를 실제 런 상태에 지급
- 지급된 골드가 저장·불러오기에 유지되도록 연동

---
## 2. 전투 보상 화면 정식화

기존 전투 승리 보상 UI를 하나의 `BattleRewardPanel` 안에서 두 영역으로 분리했다.

```text
BattleRewardPanel
├─ BattleResultPanel
│  └─ Day81RewardSummary
│     ├─ 전투 승리
│     ├─ 경험치 획득 결과
│     ├─ 레벨 변화
│     ├─ 스탯 포인트 획득
│     ├─ 획득 골드
│     └─ 획득 아이템
│
└─ BonusRewardPanel
   ├─ BonusRewardGuide
   ├─ RewardButton_Gold
   ├─ RewardButton_Health
   └─ RewardButton_Mana
```

상단 `BattleResultPanel`은 전투에서 이미 결정된 성장·드롭 결과만 표시한다. 하단 `BonusRewardPanel`은 플레이어가 직접 선택하는 추가 보상만 담당하도록 역할을 분리했다.

`추가 보상 하나를 선택하세요.` 안내 문구는 기존 24px 기준의 1.5배인 **36px**로 확대하고 **Bold** 스타일을 적용했다. 추가 보상 버튼 3개는 하단에 동일한 높이의 사각형 버튼으로 가로 배치했다.

---
## 3. 성장·드롭 결과 표시

`BattleRewardSummaryFormatter`를 추가하여 79일차 성장 결과와 80일차 드롭 결과를 하나의 전투 승리 요약으로 구성한다.

표시 항목은 다음과 같다.

- 획득 경험치
- 레벨 변화 또는 변화 없음
- 획득 스탯 포인트
- 획득 골드
- 획득 아이템
- 아이템 미획득 상태

아이템은 최대 5종까지 결과 화면에 표시하고, 그 이상이면 남은 종류 수를 별도로 표시한다.

포맷터가 생성하는 문자열에 포함된 추가 보상 안내 문구는 `BattleRewardPanelController`에서 제거하여 상단 결과 패널에 중복 출력되지 않도록 처리했다. 추가 보상 안내는 하단 `BonusRewardGuide`에서만 표시한다.

---
## 4. 전투 드롭 실제 지급

`BattleRewardPayoutService`를 추가하여 80일차에서 이미 한 번 결정된 드롭 결과를 실제 런 상태에 적용하도록 연결했다.

- 드롭 골드를 `PlayerRunState.Gold`에 추가
- 골드 오버플로를 방지하고 `int.MaxValue`에서 포화 처리
- 드롭 아이템을 현재 최소 인벤토리 구조에 추가
- 드롭 결과가 없거나 잘못된 경우 안전하게 무시

전투 결과 화면을 다시 표시하거나 추가 보상 버튼을 중복 클릭하더라도 자동 드롭이 다시 지급되지 않도록 `hasAppliedBattleDropRewards` 상태를 사용한다.

추가 보상 선택이 정상 확정된 뒤에만 자동 드롭을 실제 런 상태에 반영하고 전투 종료 흐름을 이어간다.

---
## 5. 골드 저장·복원 연동

`DungeonSaveMapper`에 플레이어 골드 저장·복원 처리를 추가했다.

저장 시 현재 `RunContext.Player.Gold`를 `RunData.PlayerStats.Gold`에 기록하고, 불러오기 시 저장된 값을 다시 `PlayerRunState.Gold`로 복원한다.

이를 통해 전투 드롭 골드와 기존 선택형 골드 보상이 런 저장 이후에도 유지된다.

---
## 6. UI 자동 구성 도구

`Day81BattleRewardInstaller`를 추가하여 `DungeonScene`의 전투 보상 화면을 자동으로 정식 구조로 구성할 수 있도록 했다.

에디터 메뉴:

```text
Project Delta
└─ 81일차
   └─ 81일차 정식 전투 보상 화면 적용
```

설치 도구는 기존 `BattleRewardPanel`을 유지하면서 내부 자식을 정리하고 다음 요소를 다시 생성한다.

- `BattleResultPanel`
- `Day81RewardSummary`
- `BonusRewardPanel`
- `BonusRewardGuide`
- 추가 보상 버튼 3개

생성된 UI 참조는 `BattleRewardPanelController`의 `summaryText`, `rewardButtons`, `rewardTexts`에 다시 연결하고 수정된 `DungeonScene`을 저장한다.

---
## 7. 테스트 추가

`BattleRewardSummaryTests`에 81일차 보상 기능을 검증하는 EditMode 테스트 8개를 추가했다.

1. 성장 결과와 드롭 골드가 요약에 포함되는지 확인
2. 아이템 미획득 문구 표시 확인
3. 획득 아이템 목록 표시 확인
4. 드롭 골드 실제 지급 확인
5. 골드 `int.MaxValue` 포화 처리 확인
6. null 드롭 결과에서 골드가 변경되지 않는지 확인
7. 드롭 아이템의 현재 최소 인벤토리 추가 확인
8. `DungeonSaveMapper` 골드 저장·복원 확인

GitHub 커밋에는 CI 상태가 등록되어 있지 않아 원격에서 Unity Test Runner의 실제 실행 성공 여부는 확인할 수 없다. 코드 및 씬 구조에 대한 정적 검토에서는 81일차 진행을 막는 문제를 확인하지 못했다.

---
## 8. 주요 변경 파일

| 파일 | 내용 |
| --- | --- |
| `Assets/ProjectDelta/Scenes/DungeonScene.unity` | 정식 전투 보상 UI와 두 하위 패널 구성 |
| `Scripts/Application/BattleRewardPayoutService.cs` | 골드·아이템 드롭 실제 지급 |
| `Scripts/Application/BattleRewardSummaryFormatter.cs` | 성장·드롭 결과 문자열 구성 |
| `Scripts/Data/DungeonSaveMapper.cs` | 골드 저장·복원 연동 |
| `Scripts/Editor/Day81BattleRewardInstaller.cs` | 81일차 보상 UI 자동 설치 |
| `Scripts/Presentation/BattleRewardPanelController.cs` | 두 패널 UI 갱신 및 추가 보상 버튼 제어 |
| `Scripts/Presentation/ExplorationMonsterEncounterController.cs` | 승리 드롭 실제 지급 및 중복 지급 방지 |
| `Tests/EditMode/BattleRewardSummaryTests.cs` | 보상 요약·지급·저장 EditMode 테스트 |

위 코드 파일의 `.meta` 파일도 함께 추가됐다.

---
## 9. 81일차 완료 결과

전투 승리 후 플레이어는 하나의 정식 보상 화면에서 다음 순서로 결과를 확인할 수 있다.

```text
전투 승리
    ↓
경험치·레벨·스탯 포인트 결과 확인
    ↓
골드·아이템 드롭 결과 확인
    ↓
추가 보상 3종 중 하나 선택
    ↓
자동 드롭 및 선택 보상 실제 적용
    ↓
전투 종료 및 탐험 복귀
```

81일차 작업으로 **전투 성장 결과 표시 → 드롭 결과 표시 → 실제 보상 지급 → 추가 보상 선택 → 저장 데이터 반영** 흐름이 하나의 전투 보상 화면으로 연결됐다.
