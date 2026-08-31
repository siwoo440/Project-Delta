# Project Delta - 118일차 개발일지

## 작업 개요

117일차에 만든 별도 이벤트 전투(구애·달래기 2개 행동, 플레이어만 계속 누르는 구조)를 기획서가 요구하는 형태로 채우는 날이다. 핵심은 세 가지다.

1. 행동을 2개에서 공통 12종으로 늘리되, 전부 같은 규격을 따르게 한다.
2. "누가 다음에 행동하는가"(주도권)를 실제로 계산해, 몬스터도 차례를 가져가 저항하게 한다.
3. 종족별로 같은 행동이라도 효과가 달라지는 상성 체계의 자리를 만든다.

여기에 사용자가 추가로 요청한 "행동할 때 플레이어 칸 아래에 지금 상태를 글자로 보여주기"도 함께 넣었다.

---

## Part 1. 공통 행동 12종

`Assets/ProjectDelta/Scripts/Application/IEventBattleCommand.cs`, `EventBattleActionCatalog.cs`, 그리고 10개의 새 Command 클래스

- `IEventBattleCommand`에 `InitiativeModifier`(이 행동을 쓰면 다음 주도권 굴림에서 받는 보정치)를 추가했다.
- 117일차의 구애·달래기(마나/정력 소모, 매력 기반/안정 기반)와 같은 패턴으로 10개를 더 만들었다 - 칭찬·놀리기·고백·노래·포옹·속삭임(마나, 매력 기반) / 선물·경청·춤·윙크(정력, 안정적). 고백은 마나 15에 기본 +20으로 가장 화끈하지만 주도권을 -2 잃고, 윙크는 정력 4에 기본 +4로 가장 값싸지만 주도권을 +3 얻는 식으로 "화끈함↔주도권" 트레이드오프를 넣었다.
- `EventBattleActionCatalog.All`이 12개를 한 곳에 모은다 - 플레이어의 선택지와 몬스터의 저항 행동이 이 카탈로그 하나를 같이 쓴다("공통"의 의미 그대로).
- 각 행동은 마나 또는 정력 중 하나만 쓰도록 설계를 제한했다(둘 다 쓰면 부분 소모 후 실패하는 원자성 문제가 생기기 때문) - `EventBattleActionCatalogTests`가 이 제약을 테스트로 고정해뒀다.

---

## Part 2. 주도권

`Assets/ProjectDelta/Scripts/Application/EventBattleInitiativeHolder.cs`, `EventBattleInitiativeRule.cs`, `EventBattleContext.cs`, `EventBattleController.cs`

- 기획서 그대로: "플레이어 매력+행동 보정+d20" vs "몬스터 매력+행동 보정+d20"을 비교해 다음 차례를 정하고, 동점이면 지금 차례를 유지한다.
- `EventBattleContext.InitiativeHolder`가 지금 누구 차례인지 담는다. `Begin()` 직후에는 항상 플레이어부터 시작한다.
- 몬스터가 차례를 가져가면(`EventBattleController.ResolveTargetTurn`) 12종 카탈로그 중 하나를 무작위로 골라 "저항" 명목으로 호감도를 깎는다 - 마나·정력을 쓰지 않으므로 항상 행동할 수 있다. 이후 다시 주도권을 굴려 플레이어 차례로 돌아올 때까지 반복한다.
- 117일차엔 플레이어가 자원만 있으면 언제든 눌렀지만, 이제 자기 차례가 아니면 버튼 자체가 비활성화된다.

---

## Part 3. 종족별 상성

`Assets/ProjectDelta/Scripts/Data/MonsterDefinition.cs`, `Assets/ProjectDelta/Scripts/Application/EventBattleAffinityRule.cs`

- 기획서의 "강점4·보통4·약점4의 50%·100%·150% 배율"을 그대로 구현했다 - 강점 행동에 당하면 50%만, 약점 행동에 당하면 150%나 반응한다.
- `MonsterDefinition`에 `eventBattleStrongActionIds`/`eventBattleWeakActionIds` 필드를 추가했지만, 실제 몬스터별 상성 값은 채우지 않았다 - 133~135일차(몬스터 콘텐츠 완성)에서 진짜 데이터가 들어갈 자리이고, 지금은 시스템만 만드는 날이라 지정되지 않은 종족은 전부 보통(100%)으로 동작한다. 80일차 `ITEM_DAY80_TEST_DROP`부터 이어온 "콘텐츠 없으면 시스템만 만들고 비워둔다" 원칙을 그대로 따랐다.

---

## Part 4. 상태 텍스트

`Assets/ProjectDelta/Scripts/Presentation/EventBattleController.cs`

사용자 요청대로 플레이어 정보 줄(MP/정력) 바로 아래에 "상태: ..." 줄을 추가했다. "당신의 차례입니다" / "{몬스터}이(가) 칭찬에 저항했다! 호감도 -6"처럼 지금 상황이 항상 텍스트로 보인다. 내 차례일 땐 파란빛, 상대 차례일 땐 붉은빛으로 색도 구분했다.

버튼 배치도 12개에 맞춰 4×3 그리드로 바꿨다 - 자기 차례가 아니거나 그 행동을 쓸 자원이 없으면 자동으로 비활성화된다.

---

## 테스트

- `EventBattleInitiativeRuleTests` - 플레이어/몬스터 중 높은 굴림이 이기는지, 동점은 유지되는지.
- `EventBattleAffinityRuleTests` - 강점/약점/보통/빈 목록 배율.
- `EventBattleActionCatalogTests` - 12개 존재, ID 중복 없음, 자원 하나만 소모.
- 기존 `EventBattleContextTests`의 `PlayerCanAct` 테스트 2개 - 117일차엔 비용 두 개를 직접 받았지만 118일차부터 카탈로그 전체를 훑는 시그니처로 바뀌어서 같이 고쳤다.

씬 UI(4×3 버튼 그리드, 몬스터 차례 자동 진행, 상태 텍스트)는 Unity 에디터가 없는 환경이라 실제 플레이로 확인하지 못했다.
