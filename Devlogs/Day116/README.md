# Project Delta - 116일차 개발일지

## 작업 개요

기획서 개발 일정상 116일차는 "Day88의 몬스터 접촉 즉시 전투 흐름을 되돌려 전투·도망·회유·유혹·아이템·관찰 6가지를 다시 제공한다"였다. 처음엔 이걸 문자 그대로 "조우 시작 전 선택 화면"으로 구현했는데, 사용자가 원한 건 그게 아니라 **이미 전투 화면에 있던 행동 버튼 줄에 회유·유혹·관찰을 추가하는 것**이었다. 첫 시도를 전부 되돌리고 방향을 다시 잡았다.

핵심은 세 가지다.

1. 전투 중 행동으로 회유·유혇·관찰을 추가한다 - 기존 공격·방어·도주와 같은 자리에서 고를 수 있게.
2. 씬에 이미 있던 8개 자리(공격·행동·방어·아이템·도주·유혇 + 신규 회유·관찰) 중 실제로 연결 안 되어 있던 유혇을 살리고, 회유·관찰 버튼 2개를 새로 만든다.
3. 사용자 피드백에 맞춰 버튼 배치·크기, 인벤토리 칸 크기를 반복 조정한다.

---

## Part 1. 방향 전환 - 조우 선택 화면이 아니라 전투 내부 버튼

처음엔 44~46일차에 있던 `IEncounterCommand`/`EncounterActionSelectionGate` 프레임워크를 확장해서 "몬스터와 접촉하면 전투 시작 전에 6지선다 화면을 보여준다"는 식으로 만들었다. 새 `EncounterSelectionHudController`까지 만들어 붙였는데, 사용자가 "내가 원한건 이게 아니야. 위 기능을 전투시의 버튼들에 추가해줘"라고 정정했다.

전부 되돌렸다. `git status`가 이번 세션 시작(115일차 커밋) 이후 아무것도 커밋되지 않은 상태였던 덕분에 `git checkout --`으로 수정한 파일들을 원래대로, 새로 만든 파일들은 삭제로 깔끔하게 정리할 수 있었다.

전투는 원래대로 몬스터 접촉 즉시 시작하고(88일차 자동 진입 그대로), 회유·유혇·관찰은 **전투가 시작된 뒤** 공격·방어·도주와 나란히 고르는 행동으로 다시 만들었다.

---

## Part 2. 전투 내부 행동 - 회유·유혇·관찰

`Assets/ProjectDelta/Scripts/Application/PersuadeBattleCommand.cs`, `SeduceBattleCommand.cs`, `ObserveBattleCommand.cs`, `EncounterPersuasionRule.cs`, `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`

- 세 `IBattleCommand`는 `AttackBattleCommand`(49일차)와 같은 원칙으로 대상 유효성만 확인한다. 실제 판정은 Presentation에서 한다.
- `EncounterPersuasionRule` - 성공률 = 기준값 + (플레이어 매력 − 대상 저항), 5~95%로 clamp. 회유 기준 50%, 유혇 기준 35% - 능력치를 새로 늘리지 않고 "유혇이 더 위험한 시도"라는 차이를 기준값 차이만으로 표현했다.
- `ConfirmPersuade()`/`ConfirmSeduce()` - `ConfirmFlee()`(69일차 도주)와 완전히 같은 구조다. 성공하면 `FinishBattle(BattleOutcome.Escaped)`로 전투를 평화롭게 끝내고, 실패하면 도주 실패와 똑같이 로그만 남기고 턴을 소모한다.
- `ConfirmObserve()` - `ConfirmDefend()`(52일차 방어)와 같은 구조로 턴을 소모하지만, 상태 변화 대신 선택된 대상의 HP/공격/방어/속도/매력/저항을 `LastObservationText`에 담는다.

**알려진 한계**: 유혇 성공 시 "전용 이벤트 전투로 분기"하는 부분은 아직 그 이벤트 전투 시스템 자체가 없어서 117일차 이후 과제로 남기고, 지금은 회유와 같은 결과(전투 종료)로 처리했다.

---

## Part 3. 씬의 행동 버튼 8개 연결·배치

`Assets/ProjectDelta/Scenes/DungeonScene.unity`, `Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs`

씬을 직접 뒤져보니 `ActionButtonPanel` 안에 공격·행동·방어·아이템·도주·유혇 6개 버튼이 이미 있었는데, 공격·방어만 스크립트에 연결돼 있고 유혇은 계속 비활성 상태였다(도주는 `BattleHudActionButtonResolver`가 이름으로 찾아서 연결, 아이템은 인벤토리 패널의 "사용" 버튼으로 이미 동작 중이라 손대지 않았다).

- `BattleHudController`에 `persuadeButton`/`seduceButton`/`observeButton` 필드를 추가하고, 대상이 선택됐을 때만 눌리도록 기존 공격 버튼과 같은 조건(`playerCanAct && SelectedBattleTarget != null`)을 걸었다.
- 씬에 회유·관찰 버튼 2개를 유혇 버튼과 동일한 구조(RectTransform + Image + Button + Label Text)로 복제해 새로 만들고, `BattleHudController`의 세 필드에 연결했다.
- 8개 버튼을 4×2 직사각형 배치로 재배치하고 "Lv." 행 바로 아래로 옮겼다.

이후 사용자 피드백에 따라 두 차례 더 조정했다.

- 버튼 크기: 110×40 → 130×46 (패널 464×88 → 544×100).
- 인벤토리 칸: 처음 요청대로 78×66 → 117×99로 키웠더니 패널(204 높이) 아래로 넘쳐서, 104×88로 다시 줄여 패널(552×184)이 넘치지 않게 맞췄다.

---

## 테스트

- `EncounterPersuasionRuleTests` - 매력/저항 차이에 따른 성공률 계산, clamp, RNG 굴림 성공/실패 판정.
- `PersuadeSeduceObserveBattleCommandTests` - 세 Command의 Id/DisplayName, 유효한 대상에서 Accept, 대상 없음/Context 없음에서 Reject.

씬 UI 배치·클릭 반응은 Unity 에디터가 없는 환경이라 실제 플레이로 확인하지 못했다. 사용자가 에디터에서 스크린샷으로 확인하며 배치를 두 차례 피드백해줬고, 이번 커밋 기준으로는 "살짝 안 맞지만 일단 넘어가자"는 선에서 마무리했다 - 정확한 픽셀 정렬은 다음에 에디터에서 직접 조정이 필요할 수 있다.
