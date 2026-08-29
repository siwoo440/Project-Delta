# Project Delta - 111일차 개발일지

## 작업 개요

110일차에 만든 RoomType 골격(Trap만 실제로 동작)을 나머지 세 종류까지 마저 연결하는 날이다. Combat은 기존 몬스터 조우 시스템에, Event는 아직 아무데도 연결되지 않았던 이벤트 화면에 붙인다. 추가로 110일차에 만들어두고도 씬에 배치하지 않았던 방 상태 HUD를 실제로 화면에 올렸다.

---

## Part 1. 방 진입 신호

`Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs`

Combat/Event 모두 "플레이어가 이 방에 들어왔다"는 시점이 필요한데, 그런 신호가 기존 코드 어디에도 없었다. `event Action<RoomView, bool> RoomEntered`를 추가하고, 최초 스폰 시점(`Awake`)과 방 이동 시점(`EnterRoom`) 두 곳에서 `MarkVisited()`가 이미 계산해주는 "최초 방문 여부"와 함께 방출한다.

---

## Part 2. RoomType.Combat → 몬스터 조우 연결

`Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs`

몬스터 배치(`RoomEncounterPlacementService.BuildForFloor`)는 절차 생성 그래프(RoomNode)만 보고 방을 고르기 때문에 실제 RoomType을 몰랐다. 다행히 이 서비스는 처음부터 `excludedRoomIds` 파라미터를 받도록 설계돼 있어서, 서비스 내부를 건드리지 않고 `DungeonFloorController` 쪽에서 "Combat이 아닌 방 전부"를 제외 목록으로 넘기는 것만으로 해결됐다. 방 인스턴스 조회는 `RunContext.Current.Dungeon.TryGetRoom`을 쓴다 - 몬스터 배치 시점(`BuildEncounterLayout`)은 이미 모든 방의 `InstantiateRooms`(RoomType 배정 포함)가 끝난 뒤라 항상 값을 찾을 수 있다.

---

## Part 3. RoomType.Event → 이벤트 화면 연결

`Assets/ProjectDelta/Scripts/Domain/RoomEventService.cs`, `Assets/ProjectDelta/Scripts/Application/RoomEventPoolService.cs`, `Assets/ProjectDelta/Scripts/Presentation/RoomEventTriggerController.cs`

`EventHudController`는 108일차부터 "실제로 이벤트를 트리거하는 방이 아직 없어서 Open()을 외부에서 직접 호출한다"는 주석을 달고 있었다 - 오늘이 그 자리를 채우는 날이다. 함정 시스템과 같은 3계층 구조를 그대로 따랐다.

- `RoomEventPoolService`(Application) - 후보 EventDefinition 목록에서 하나를 무작위로 고른다.
- `RoomEventService`(Domain) - 이 방이 Event 타입이고 아직 표시된 적 없는지 판정하고, 허용되면 `RoomInstance.MarkEventTriggered()`(internal)까지 호출한다. Presentation 계층은 internal 멤버에 접근할 수 없어서 이 서비스를 거쳐야 한다.
- `RoomEventTriggerController`(Presentation) - `PlayerGridMovementController.RoomEntered`를 구독해서 위 두 서비스를 거친 뒤 `EventHudController.Open()`을 호출한다.

한 가지 주의한 점: Unity는 서로 다른 GameObject의 `Awake()` 실행 순서를 보장하지 않는다. 최초 스폰 방의 `RoomEntered`가 이 컨트롤러의 구독보다 먼저 발생했을 수 있어서, `OnEnable()`에서 구독 직후 현재 방을 한 번 더 직접 확인한다 - `RoomEventService`가 이미 멱등하게 짜여 있어 중복 호출이 안전하다.

`RoomInstance.EventTriggered`를 `TrapTriggered`와 동일한 패턴으로 추가하고, `RunData.RoomRunState`/`DungeonSaveMapper`/`RoomPassageController` 복원 지점까지 나란히 이어서 저장/불러오기 후에도 이벤트가 다시 뜨지 않게 했다.

### 이벤트 데이터 자산

`Assets/ProjectDelta/Data/Events/`

`EventDefinition` 애셋이 프로젝트에 하나도 없었다(조건/결과 로직은 107~109일차에 이미 만들었지만 실제 콘텐츠는 없었다). 테스트 가능하도록 최소한의 이벤트 2종("신비한 샘" - 체력 회복, "떠돌이 상인" - 골드 획득)을 실제 `.asset`으로 만들어 `RoomEventTriggerController`의 후보 풀에 등록했다.

---

## Part 4. RoomTypeRollService 확률 갱신

`Assets/ProjectDelta/Scripts/Application/RoomTypeRollService.cs`

Combat/Event가 실제로 연결됐으니 110일차에 Trap/Normal 둘로만 굴리던 확률을 4종으로 늘렸다: Normal 45% / Combat 25% / Trap 15% / Event 15%.

---

## Part 5. 방 상태 HUD를 실제로 화면에 배치

`Assets/ProjectDelta/Scenes/DungeonScene.unity`

110일차에 `RoomStatusHudController`를 만들어놓고 "Canvas 연결은 직접 해달라"고 미뤘던 부분을 이번엔 직접 처리했다. 기존 `PersistentPlayerHudRoot`(상시 노출되는 HUD 루트, `BattleCanvas`의 자식) 밑에 텍스트를 하나 추가하고 화면 상단 중앙에 배치했다. 같은 GameObject에 이미 `PersistentPlayerVitalsController`가 붙어있던 걸 참고해서, `RoomStatusHudController`와 `RoomEventTriggerController`도 같은 자리에 나란히 붙였다.

이벤트 화면(`EventPanel`)은 기존 `BattleCanvas`(Screen Space Overlay, 상시 최상단) 밑에 새로 만들었다 - 새 Canvas를 따로 만들지 않고 이미 검증된 설정을 그대로 재사용했다. 제목/본문/결과 텍스트, 선택지 버튼 6개, 닫기 버튼까지 `EventHudController`가 요구하는 필드를 전부 씬에서 직접 연결했다. 버튼 클릭 로직은 `EventHudController.Awake()`가 코드로 등록하기 때문에(`HookButtons()`), 씬에는 버튼 컴포넌트만 있으면 되고 Persistent Call 같은 에디터 전용 연결은 필요 없었다.

---

## 테스트

- `RoomEventServiceTests` - Event 방 판정 허용/재판정 방지/비Event 방 거부/null 방어.
- `RoomEventPoolServiceTests` - 빈 풀 처리, 단일 후보 항상 선택, 다중 후보가 풀 밖 값을 반환하지 않는지.
- `RoomEventTriggerControllerTests` - 리플렉션으로 Scene 참조 필드 구성과 핸들러 메서드 존재 확인.
- `RoomTypeRollServiceTests` - 기존 "Normal/Trap만 나온다" 테스트를 4종 분포 검증으로 교체(Normal > Combat > Trap = Event 순서 확인).

씬에 새로 배치한 UI(방 상태 텍스트, 이벤트 팝업)는 Unity 에디터가 없는 환경이라 실제 플레이로는 검증하지 못했고, 사용자가 에디터에서 직접 확인했다.
