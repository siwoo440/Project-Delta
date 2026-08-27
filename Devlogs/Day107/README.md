# Project Delta - 107일차 개발일지

## 작업 개요

107일차는 이벤트 시스템의 첫 날로, 이벤트 데이터 구조와 선택지 조건 판정을 만들었다.

지금까지 이벤트 관련 코드는 전혀 없었고 `RunContext.Events`도 빈 스텁이었다. 이번 일차에서 데이터 정의부터 조건 검사까지 기반을 세웠다.

판정·결과 적용·보상은 이번 범위에서 다루지 않는다. 108일차(결과 처리 + 이벤트 화면)로 이어간다.

---

## 1. EventDefinition - 데이터 구조

`Assets/ProjectDelta/Scripts/Data/EventDefinition.cs`

93일차 `ItemUseEffectDefinition`과 같은 패턴 - `[Serializable]` 일반 클래스를 ScriptableObject 안에 배열로 담는다.

- `EventDefinition`(ScriptableObject) - Id(`DefinitionBase` 상속)·제목·본문·선택지 배열(`EventChoiceDefinition[]`).
- `EventChoiceDefinition` - 선택지 텍스트 + 조건 목록(`EventCondition[]`).
- `EventCondition` - 조건 하나. `{Kind, TargetId, RequiredValue}` 범용 구조로 통일해 능력치·아이템·골드·플래그 4종을 전부 표현한다.
- `EventStatType` - `StatBlock`의 9개 필드와 이름이 대응하는 별도 enum. Data 계층이 Domain의 `StatBlock`을 직접 참조하지 않기 위해 분리했다.

`EventCondition`/`EventChoiceDefinition`에는 테스트와 콘텐츠 코드에서 바로 쓸 수 있도록 공개 생성자를 추가했다(93일차 `ItemUseEffectDefinition`과 동일한 방식).

---

## 2. EventRunState - 이벤트 플래그 저장소

`Assets/ProjectDelta/Scripts/Domain/EventRunState.cs`

지금까지 `RunSubStates.cs` 안에 `public sealed class EventRunState { }`로 비어 있던 걸 실제로 채웠다. `HasFlag`/`SetFlag`/`RestoreFrom`(세이브 복원용)만 가진 단순한 `HashSet<string>` 저장소다. 타입 이름과 네임스페이스를 그대로 유지했기 때문에 `RunContext.Events`를 포함한 다른 코드는 전혀 손대지 않아도 됐다.

---

## 3. EventConditionService - 선택지 조건 판정

`Assets/ProjectDelta/Scripts/Application/EventConditionService.cs`

선택지 하나의 조건을 전부 검사해 `EventChoiceAvailabilityResult`(`Available`/`Unavailable` + 실패 사유 문자열)를 반환한다.

- `Stat` - `PlayerRunState.GetFinalStats()`의 해당 스탯이 요구치 이상인지.
- `Item` - 인벤토리 전체 슬롯에서 해당 ID의 보유 수량 합이 요구치 이상인지.
- `Gold` - `PlayerRunState.Gold`가 요구치 이상인지.
- `Flag` - `EventRunState.HasFlag`가 요구한 참/거짓과 일치하는지(특정 조건이 "없어야" 통과하는 경우도 표현 가능).

여러 조건이 걸려 있으면 첫 번째로 실패한 조건의 사유를 그대로 반환한다 - "사용할 수 없는 선택지도 숨기지 않고 비활성 상태와 사유를 표시한다"는 요구를 그대로 구현한 것이다.

기존 `ItemActionAvailability`(장착/사용/판매/버리기용 3단계 열거값)를 재사용하지 않고 이벤트 전용 `EventChoiceAvailability`를 새로 만들었다 - 실패 사유 텍스트를 담을 자리가 필요했기 때문이다.

---

## 4. 테스트

- `EventRunStateTests` - 플래그 설정/해제/빈 이름 무시, `RestoreFrom`이 기존 플래그를 교체하는지, `null` 복원 시 전부 비워지는지.
- `EventConditionServiceTests` - 조건 없음(항상 통과), 능력치·아이템·골드·플래그 조건 각각의 통과/실패, 플래그가 "없어야" 통과하는 역방향 케이스, 여러 조건 중 하나라도 실패하면 그 사유가 반환되는지, 모든 조건을 만족하면 통과하는지, `null` 컨텍스트/선택지 안전 처리.

---

## 5. Unity 에디터에서 확인해야 할 사항

1. Scene 변경 사항은 없다 - 이번 일차는 데이터 구조와 조건 판정 로직까지다.
2. `EventDefinition` 에셋을 하나 만들어보고, 인스펙터에서 제목·본문·선택지·조건이 정상적으로 입력되는지 확인해달라.
3. 새 EditMode 테스트를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
4. 재구성한 개발 일정 기준 다음은 108일차 - 결과 처리 + 이벤트 화면이다.
