# Project Delta - 109일차 개발일지

## 작업 개요

109일차는 두 부분으로 나뉜다.

1. 이벤트 시스템의 마지막 날 - 이벤트 플래그 저장·복원과, 1회성/재등장 가능 이벤트 구분.
2. 별도 요청으로 추가된 **다중 저장 슬롯 UI** - 여러 슬롯에 각각 저장/불러오기를 할 수 있는 화면과, 이를 뒷받침하는 저장 인프라 확장.

이벤트 시스템(97~109일차에 걸친 장비→유물→경제→상자→이벤트 순서 중 마지막)은 이걸로 마무리되고, 110일차부터는 특수 방 단계로 넘어간다. 저장 슬롯 UI는 원래 일정에는 없던 추가 작업이다.

---

## Part 1. 이벤트 저장·기록 + 통합 검증

### 1-1. 1회성 / 재등장 가능 이벤트

`Assets/ProjectDelta/Scripts/Data/EventDefinition.cs`, `Assets/ProjectDelta/Scripts/Application/EventResultService.cs`

`EventDefinition.IsRepeatable`(기본값 `false`)을 추가했다. `EventResultService.ApplyChoice`는 재등장 가능한 이벤트라면 "한 번만 확정" 게이트(`EVENT_RESOLVED_<id>` 플래그 검사)를 아예 적용하지 않고, 확정 플래그도 남기지 않는다 - 반복 이벤트가 불필요한 플래그를 계속 쌓지 않게 했다.

### 1-2. 이벤트 플래그 저장·복원

`Assets/ProjectDelta/Scripts/Data/RunData.cs`, `Assets/ProjectDelta/Scripts/Data/DungeonSaveMapper.cs`

`RunData.EventFlags`(`List<string>`)를 추가하고, `DungeonSaveMapper.BuildFromRunContext`/`ApplyBasics`에 저장·복원 두 지점만 나란히 추가했다. 107일차에 만든 `EventRunState.RestoreFrom(IEnumerable<string>)`이 이미 있어서 별도 변환 로직 없이 그대로 연결됐다. 기존 인벤토리/던전 저장 코드는 건드리지 않았다.

### 1-3. 통합 검증

실제 씬에서 방 진입→선택→판정→결과→보상→복귀를 확인하는 건 Unity 에디터가 없어 할 수 없었다. 대신 조건(107일차)→결과(108일차)→저장/복원(109일차) 전체를 잇는 EditMode 테스트로 대체했다 - 1회성 이벤트가 저장/복원 후에도 다시 실행되지 않는지, 재등장 가능 이벤트는 반대로 다시 실행되는지를 골드 지급이 정확히 몇 번 일어나는지로 검증했다.

---

## Part 2. 저장 슬롯 UI

### 확인한 현재 상태

기존 저장 시스템(`ISaveService`/`SaveService`)은 런 저장 파일이 `run.json` 하나뿐이었다 - 슬롯 개념 자체가 없었다. `SavePaths`의 `GetBackupPath(path, slot)`는 손상 복구용 백업 순환(bak1~bak3)이지, 사용자가 고르는 저장 슬롯이 아니다.

### 2-1. 저장 인프라 - 슬롯 지정 API

`Assets/ProjectDelta/Scripts/Infrastructure/SavePaths.cs`, `SaveService.cs`, `Assets/ProjectDelta/Scripts/Application/ISaveService.cs`

- `SavePaths.RunPathForSlot(slot)` - 슬롯 0은 기존 `run.json`을 그대로 쓰고(하위 호환), 슬롯 1 이상은 `run_slot{N}.json`으로 분리한다.
- `ISaveService`에 슬롯 인자를 받는 `WriteRun`/`ReadRun`/`HasRun`/`DeleteRun` 오버로드와, 슬롯 카드용 요약 정보를 반환하는 `TryGetRunSummary(slot, out SaveSlotSummary)`를 추가했다. 기존 인자 없는 메서드들은 전부 슬롯 0 호출로 위임되어 그대로 동작한다.
- `SaveSlotSummary`(신규 Data 클래스) - Slot, HasData, RunId, SavedAtIso8601(저장 파일에 실제 기록된 시각), PlaytimeSeconds.

**알아두실 제약**: `PlaytimeSeconds`는 `RunData.BasicInfo`에 필드는 있지만 이번 조사에서 실제로 값을 갱신하는 코드가 어디에도 없다는 걸 확인했다 - 즉 지금은 항상 0으로 표시된다. 플레이타임을 실제로 누적 추적하려면 별도 작업(세션 동안 경과 시간을 더하는 로직)이 필요해서 이번 범위에는 포함하지 않았다. "저장시간"(마지막으로 저장한 실제 시각)은 저장 파일의 envelope 정보를 그대로 쓰기 때문에 정확하다.

### 2-2. ApplicationFlow - 슬롯 인식

`Assets/ProjectDelta/Scripts/Application/ApplicationFlow.cs`

`ActiveSlot`(기본값 0) 상태를 추가하고, 관련 메서드에 슬롯 오버로드를 만들었다.

- `StartNewGame(slot)` / `ContinueGame(slot)` - 호출 시 `ActiveSlot`을 그 슬롯으로 바꾼다. 인자 없는 기존 버전은 `ActiveSlot`을 그대로 쓰므로 하위 호환된다.
- `SaveToSlot(slot)` - 저장 슬롯 UI의 "저장" 버튼용. 지정한 슬롯으로 `ActiveSlot`을 바꾸고 즉시 저장한다 - 이후 자동 저장도 그 슬롯을 계속 대상으로 삼는다.
- `TryGetSlotSummary(slot, out summary)` - UI가 슬롯 카드를 채울 때 쓴다.
- 자동 저장(`TryWriteDungeonProgress`)과 런 삭제(`EnterDefeat`/`ReturnToTitle`)는 전부 `ActiveSlot`을 대상으로 하도록 바꿨다.

### 2-3. SaveSlotHudController - 이미지와 같은 슬롯 목록 UI

`Assets/ProjectDelta/Scripts/Presentation/SaveSlotHudController.cs`

말씀하신 이미지대로 "저장" 탭 하나만 구현했다(게임/사운드 탭은 만들지 않음).

- 슬롯마다 `SaveSlotRowRefs`(슬롯 이름·저장시간·플레이타임·"저장 데이터 없음" 텍스트 + 저장/불러오기 버튼)를 인스펙터 배열로 연결하는 구조 - 슬롯 개수는 배열 크기로 자유롭게 조절된다(기본 2개).
- 데이터가 없는 슬롯은 저장시간·플레이타임이 비고 "저장 데이터 없음"이 표시되며 불러오기 버튼이 비활성화된다.
- 저장 버튼은 진행 중인 런이 있을 때만 활성화되고, 어떤 슬롯이든 눌러서 그 슬롯에 저장할 수 있다(빈 슬롯에 새로 저장하는 것도 포함).
- 불러오기 버튼은 `ApplicationFlow.ContinueGame(slot)`을 그대로 호출해 기존 로딩 화면 흐름을 그대로 탄다.

슬롯 0(기존 자동 저장 파일)은 이 UI에 노출하지 않는다 - 저장 슬롯 기능이 생기기 전 자동 저장과의 호환용으로만 남겨뒀다.

---

## 3. 테스트

- `EventPersistenceTests` - 이벤트 플래그 저장/복원, 1회성/재등장 이벤트가 저장·복원 전체 파이프라인에서 의도대로 동작하는지.
- `EventResultServiceTests`(추가분) - 재등장 가능 이벤트가 여러 번 적용되는지, 확정 플래그를 남기지 않는지.
- `SaveServiceTests`(추가분) - 슬롯별 저장/읽기/존재확인/삭제가 서로 독립적인지, 슬롯 없는 기존 호출이 슬롯 0 경로를 쓰는지, `TryGetRunSummary`가 있음/없음을 정확히 반환하는지.
- `SaveSlotHudControllerUguiTests` - 패널·닫기 버튼·슬롯 배열 필드, `Open`/`Close` 메서드, 슬롯 행 UI 참조 필드가 존재하는지(리플렉션).

---

## 4. Unity 에디터에서 확인해야 할 사항

1. **Scene 작업이 필요하다** - 저장 슬롯 패널 GameObject, 슬롯 행 UI(이름/저장시간/플레이타임/빈 상태 텍스트 + 저장/불러오기 버튼) 2세트 이상, 닫기 버튼을 만들어 `SaveSlotHudController`에 연결해달라.
2. 슬롯이 비어 있을 때 "저장 데이터 없음"이 뜨고 불러오기가 비활성화되는지, 저장 후에는 저장시간이 정확히 표시되는지 확인해달라.
3. 같은 런에서 슬롯 1에 저장했다가 슬롯 2에도 저장해보고, 두 슬롯이 서로 다른 데이터를 유지하는지 확인해달라.
4. 슬롯을 불러오면 실제로 그 슬롯의 진행 상황(층, 인벤토리 등)으로 이어지는지 확인해달라.
5. 새 EditMode 테스트를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
6. **플레이타임은 아직 항상 0으로 표시된다** - 실제 추적은 별도 작업이 필요하다는 점을 알아둬달라.
7. 재구성한 개발 일정 기준 다음은 110일차 - 특수 방 공통 구조 + 함정 방이다.
