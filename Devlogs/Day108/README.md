# Project Delta - 108일차 개발일지

## 작업 개요

108일차는 이벤트 선택 결과 적용과 이벤트 화면을 만들어 이벤트 시스템의 두 번째 날을 마무리했다.

작업을 시작하기 전 작업 디렉터리에 `EventDefinition.cs`(107일차 파일의 108일차 확장분), `EventResultService.cs`, `EventHudController.cs`가 이미 만들어져 있는 상태였다 - 처음부터 새로 만들지 않고 기존 내용을 검토한 뒤 문제를 고치고 테스트를 추가해서 완성했다.

이벤트 저장·기록, 여러 유형의 이벤트로 전체 파이프라인을 검증하는 작업은 이번 범위에서 다루지 않는다. 109일차(저장·기록 + 통합 검증)로 이어간다.

---

## 1. EventEffect - 결과 효과 데이터

`Assets/ProjectDelta/Scripts/Data/EventDefinition.cs`

107일차에 만든 조건(`EventCondition`)과 짝을 이루는 결과 데이터를 추가했다.

- `EventEffectKind` - HP/마나/기력 회복, 골드 획득, 아이템 획득, 플래그 설정, 관계 변화(스텁) 6종.
- `EventEffect` - 조건과 달리 값에 **음수를 허용**한다. "회복"과 "피해", "획득"과 "소비"를 값 하나의 부호로 표현해, 위험한 선택지(함정 등)도 같은 구조로 만들 수 있게 했다.
- 관계(호감도) 변화는 enum 값 자리만 만들어뒀다 - NPC·관계 시스템은 재구성 일정상 113~116일차라 지금은 저장할 곳이 없다.

---

## 2. EventResultService - 결과 적용과 중복 방지

`Assets/ProjectDelta/Scripts/Application/EventResultService.cs`

`ApplyChoice(eventDefinition, choice, context)`가 선택지의 결과 목록을 순서대로 적용한다.

- HP/마나/기력은 `GetFinalStats()` 기준으로 clamp(0 이상, 최대치 이하).
- 골드는 105일차 `GoldService`를 그대로 재사용(획득은 `Earn`, 소비는 보유량을 넘지 않게 `TrySpend`).
- 아이템은 `InventoryRunState`에 직접 추가/제거.
- 플래그는 107일차 `EventRunState.SetFlag`.
- 관계 변화는 데이터만 통과시키고 실제 적용은 no-op.

**"한 번만 확정·저장"** 요구는 `EVENT_RESOLVED_<eventId>`라는 이름으로 `EventRunState`의 일반 플래그 저장소에 기록하는 방식으로 구현했다 - 별도 구조 없이 107일차에 만든 플래그 메커니즘을 그대로 재사용한 것이다. 이미 확정된 이벤트에 다시 적용을 시도하면 상태 변경 없이 `AlreadyResolved`로 실패한다.

---

## 3. EventHudController - 이벤트 화면 (버그 수정 포함)

`Assets/ProjectDelta/Scripts/Presentation/EventHudController.cs`

본문·선택지·조건 미충족 사유·결과 메시지를 표시하는 화면이다. 실제로 이벤트를 트리거하는 방(114일차 예정 특수 방)이 아직 없어서, `Open(definition)`을 외부에서 직접 호출해 연결하는 구조로 만들었다.

**검토 중 발견한 문제**: 기존 코드는 선택지를 클릭해 결과를 적용한 직후 곧바로 `Close()`를 호출해 패널을 닫아버렸다 - 결과 메시지 텍스트를 설정한 바로 다음 줄에서 패널을 숨기니, 플레이어가 결과를 읽을 틈이 전혀 없었다.

이를 고치기 위해 `closeButton`과 `isResolved` 상태를 추가했다.

- 결과를 적용하면 패널은 닫지 않고, 선택지 버튼을 전부 비활성화하며 닫기 버튼을 노출한다.
- 플레이어가 결과 메시지를 읽은 뒤 직접 닫기를 눌러야 탐험으로 복귀한다 - "결과 확정 후 탐험으로 안전하게 복귀한다"는 요구를 실제로 만족시킨다.

---

## 4. 테스트

- `EventResultServiceTests` - HP 회복이 최대치를 넘지 않게 clamp되는지, 음수 HP 효과가 0 밑으로 내려가지 않는지, 골드 획득/소비(0 밑으로 안 내려감), 아이템 획득이 인벤토리에 반영되는지, 플래그 설정이 `EventRunState`에 반영되는지, 같은 이벤트를 두 번 확정 시도하면 두 번째가 `AlreadyResolved`로 실패하고 효과가 중복 적용되지 않는지, 관계 변화가 예외 없이 no-op으로 통과하는지, `null` 컨텍스트 안전 처리.
- `EventHudControllerUguiTests` - `OnGUI` 미사용, 패널/선택지/닫기 버튼 필드가 `[SerializeField]`로 노출됐는지, `Open`/`Close` 공개 메서드가 존재하는지(리플렉션, 기존 UI 테스트 패턴과 동일).

---

## 5. Unity 에디터에서 확인해야 할 사항

1. **Scene 작업이 필요하다** - 이벤트 패널 GameObject, 제목/본문/결과 Text, 선택지 버튼 6개 + 각 버튼의 Text, 닫기 버튼을 만들어 `EventHudController`의 해당 필드에 연결해달라.
2. 조건을 만족하지 못하는 선택지가 비활성화되면서 사유가 버튼 텍스트에 함께 표시되는지 확인해달라.
3. 선택지를 확정한 뒤 결과 메시지가 화면에 남아 있고, 닫기 버튼을 눌러야 패널이 닫히는지 확인해달라(이번에 고친 버그 지점).
4. 같은 `EventDefinition`을 다시 열어 선택지를 확정하려 하면 "이미 결과가 적용된 이벤트입니다" 메시지가 뜨는지 확인해달라.
5. 새 EditMode 테스트를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
6. 재구성한 개발 일정 기준 다음은 109일차 - 저장·기록 + 통합 검증이다.
