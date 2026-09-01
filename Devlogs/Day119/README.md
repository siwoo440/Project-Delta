# Project Delta - 119일차 개발일지

## 작업 개요

118일차에 만든 "공통 행동 12종 + 주도권 + 종족 상성" 틀에 세 가지를 더 채워 넣는 날이다.

1. 행동별 영구 숙련도(Lv.1~5) - 오래 쓸수록 그 행동이 강해진다.
2. 완전 무작위였던 몬스터 저항을 조금 더 "지켜보는" AI로.
3. 대상을 1명에서 최대 3명으로 늘리고, 개별 게이지·만족 이탈·보스 다단계 게이지 구조를 만든다.

이번 일차는 사용자가 "직접 수정할 테니 zip으로 달라"고 해서, 실제 파일은 평소처럼 라이브 프로젝트에 그대로 적용하고 같은 파일들을 zip으로도 묶어 전달했다. 이후 `ProjectDelta.Presentation` 어셈블리가 `ProjectDelta.Infrastructure`를 참조하지 않는다는 걸 놓쳐 CS0234가 났고, `ApplicationFlow`를 경유하도록 고쳐서 해결했다.

---

## Part 1. 행동별 영구 숙련도

`Assets/ProjectDelta/Scripts/Data/ProfileData.cs`, `Assets/ProjectDelta/Scripts/Application/EventBattleProficiencyRule.cs`

- `ProfileData.PermanentGrowth`에 이미 있던 TODO 주석("AdultActionDefinition 구현 시 추가: 성인 이벤트 행동 숙련도와 경험치") 자리를 그대로 채웠다 - 행동 ID를 키로 쓰는 `Dictionary<string, EventBattleActionProficiencyRecord>`.
- Lv.1은 100%, 레벨마다 10%p씩 늘어 Lv.5는 140% - `EventBattleProficiencyRule.GetMultiplier`.
- 레벨업 필요 경험치는 `레벨 × 20`(Lv1→2는 20, Lv2→3은 40 ...). `AddExperience`가 한 번에 여러 레벨을 올릴 수도 있고, 최대 레벨(5)에서는 경험치를 더 받지 않는다.

---

## Part 2. 몬스터 저항 AI

`Assets/ProjectDelta/Scripts/Application/EventBattleMonsterAiRule.cs`

118일차엔 12종 중 완전 무작위로 골랐는데, 이제 직전에 자기가 썼던 행동은 되도록 피한다(같은 저항을 두 번 연달아 쓰지 않음). 완전한 예측형 AI는 아니지만 "매번 순수 무작위"보다는 지켜보고 있다는 인상을 준다.

---

## Part 3. 다수 참가자 (최대 3명)

`Assets/ProjectDelta/Scripts/Application/EventBattleParticipantState.cs`(신규), `EventBattleContext.cs`, `EventBattleEntryService.cs`, `EventBattleSession.cs`, 공통 행동 12개 파일 전부, `Assets/ProjectDelta/Scripts/Presentation/EventBattleController.cs`

- `EventBattleContext`가 대상 하나(`Target`)를 직접 들고 있던 구조에서, 최대 3명의 `EventBattleParticipantState`(개별 호감도·만족 이탈 여부·보스 단계) 목록을 갖는 구조로 바뀌었다. 플레이어는 `SelectedTargetIndex`로 지금 누구에게 행동할지 고른다(일반 전투의 `SelectedTarget`과 같은 개념).
- **만족 이탈** - 호감도 60 이상인 대상은 자기 차례가 될 때마다 15% 확률로 만족하고 떠난다(`LifetimeStats.MonstersSatisfiedAway`에 이미 있던 필드를 그대로 썼다).
- **보스 다단계 게이지** - `EventBattleParticipantState.StageCount`가 2 이상이면 한 단계를 다 채운 뒤 게이지가 비워지고 다음 단계로 넘어간다. 구조만 만들어뒀고 실제로 2단계 보스를 진입시키는 콘텐츠는 없다 - 121~122일차(상위 개체·보스) 몫으로 남긴다.
- 공통 행동 12개 파일은 전부 `context.Target` → `context.SelectedTarget.Participant`, `context.AddFavor(...)` → `context.SelectedTarget.AddFavor(...)`로 기계적으로 고쳤다.
- 117일차부터 있던 1명짜리 `Begin(...)`/`TryEnter(...)` 오버로드는 그대로 남겨뒀다 - 유혇 성공 경로(`ExplorationMonsterEncounterController`)는 수정 없이 그대로 컴파일된다.

---

## 알려진 한계 / 다음 과제

- `EventBattleController`가 프로필을 읽고 쓸 때, `ApplicationFlow`(Application 어셈블리에 있으면서 SaveService를 들고 있음)를 거친다 - `Infrastructure/AppRoot.cs`에 있는 "로드한 프로필을 들고 있을 곳이 없다"는 TODO를 그대로 우회한 것이다. 나중에 정식 보관 지점(ProfileContext 등)이 생기면 `ApplicationFlow.ReadOrCreateProfile`/`WriteProfile` 두 메서드만 바꾸면 된다.
- 보스 2단계 게이지·다수 참가자 진입은 아직 실제로 트리거하는 콘텐츠가 없다 - 지금은 유혇 성공(대상 1명)만 실제로 쓰인다.

---

## 오류 수정

`ProjectDelta.Presentation` 어셈블리가 `ProjectDelta.Infrastructure`를 참조하지 않아 `AppRoot`/`ISaveService`를 직접 쓴 코드가 CS0234로 막혔다. `ApplicationFlow`(Application 어셈블리, 이미 SaveService를 들고 있음)에 `ReadOrCreateProfile()`/`WriteProfile(profile)`를 추가해 우회했다 - Run 저장을 이미 이 클래스가 중계하고 있어서 같은 패턴을 따랐다.

---

## 테스트

- `EventBattleProficiencyRuleTests` - 레벨별 배율, 경험치 누적/레벨업/이월, 최대 레벨에서 정지.
- `EventBattleParticipantStateTests` - 단일/2단계 게이지 승리 판정, 만족 이탈 후 행동 무시.
- 기존 `EventBattleContextTests`/`EventBattleSessionTests`/`EventBattleEntryServiceTests`/`CourtSootheEventBattleCommandTests` - 새 다중 대상 구조에 맞춰 생성자·API 호출부를 고쳤다.

씬 UI(대상 탭 전환, 만족 이탈 문구, 숙련도 반영)는 Unity 에디터가 없는 환경이라 실제 플레이로 확인하지 못했다. 사용자가 에디터에서 컴파일 오류를 확인하며 한 차례 피드백을 줬고, 이번 커밋 기준으로는 정상 컴파일된다.
