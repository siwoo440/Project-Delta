# Project Delta - 110일차 개발일지

## 작업 개요

110일차는 "특수 방" 단계의 첫날이다. 방마다 고유한 종류(RoomType)를 갖고 그 종류에 맞는 이벤트(함정)가 발생하도록 하는 것이 목표였는데, 작업 중 두 가지 별도 버그도 함께 발견되어 같이 처리했다.

1. 방 종류(RoomType) 시스템 - 방마다 Normal/Combat/Event/Trap 중 하나를 배정하고, Trap 방에 실제 함정 판정 로직 연결.
2. 절차 생성 무작위성 버그 2건 - 시드가 고정돼 있던 문제, 생성 후보 방 모양이 1종류뿐이었던 문제.
3. 미로 방(Room_Maze_01~10) 10종을 실제 생성 풀에 등록.
4. 현재 방 상태를 보여주는 HUD 추가.
5. 미로 방 등록 이후 드러난 문 시각 버그 2건 수정.
6. 109일차에 남아있던 컴파일 오류(CS0165) 수정.

---

## Part 1. 방 종류(RoomType) + 함정 시스템

### 1-1. RoomType 정의

`Assets/ProjectDelta/Scripts/Domain/RoomType.cs`

`RoomType` enum(Normal/Combat/Event/Trap)과 `RoomTypeRules.GetDisplayName`을 추가했다. Combat/Event는 이번 작업 범위에는 포함하지 않았다 - 이미 존재하는 몬스터 스폰/이벤트 시스템과 방 종류를 동시에 배정하면 한 방에 두 시스템이 겹쳐 이중 처리될 위험이 있어서, 이번에는 Trap만 실제로 굴리고 나머지는 향후 연결을 위해 자리만 잡아뒀다.

### 1-2. 방 종류 판정과 함정 판정 분리

`Assets/ProjectDelta/Scripts/Application/RoomTypeRollService.cs`, `Assets/ProjectDelta/Scripts/Application/RoomTrapRollService.cs`, `Assets/ProjectDelta/Scripts/Domain/RoomTrapService.cs`

이 프로젝트에서 계속 지켜온 규칙대로 무작위 판정(`System.Random` 사용)은 Application 계층 RollService가 맡고, Domain 계층 `RoomTrapService`는 이미 굴려진 결과를 규칙대로 적용만 한다.

- `RoomTypeRollService.Roll` - 15% 확률로 Trap, 나머지는 Normal.
- `RoomTrapRollService` - 회피 확률(기본 20% + 회피 스탯, 최대 95%)과 피해량(8~15) 각각 별도로 굴린다.
- `RoomTrapService.Trigger` - 방이 실제 Trap 타입인지, 이미 발동한 적 있는지 확인 후 회피 실패 시 체력을 깎는다. 한 방에서 함정이 중복 발동하지 않도록 `RoomInstance.MarkTrapTriggered()`를 `internal`로 막아 `RoomTrapService`만 호출할 수 있게 했다.

### 1-3. 방 종류 저장·복원

`Assets/ProjectDelta/Scripts/Domain/RoomInstance.cs`, `Assets/ProjectDelta/Scripts/Data/RunData.cs`, `Assets/ProjectDelta/Scripts/Data/DungeonSaveMapper.cs`, `Assets/ProjectDelta/Scripts/Presentation/RoomPassageController.cs`

`RoomInstance`에 `RoomType`, `TrapTriggered` 상태를 추가했다. 새로 생성된 방은 `RoomPassageController.InitializeRoom`에서 저장된 상태가 없을 때만 `RoomTypeRollService.Roll()`로 종류를 확정하고, 저장된 방을 복원할 때는 그 값을 그대로 되살린다. `RunData.RoomRunState`에는 원래 있었지만 아무도 읽고 쓰지 않던 `TrapTriggered` 필드를 이번에 실제로 연결했다.

---

## Part 2. 절차 생성 무작위성 버그 수정

### 2-1. 시드 고정 문제

`Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs`

`baseSeed`가 `3600`으로 고정된 채 어디서도 바뀌지 않고 있었다. `randomizeSeedEachRun`(기본 `true`) 옵션을 추가해서, 새 게임 시작 시 `Awake()`에서 `baseSeed`를 매번 무작위로 재설정하게 했다. 이어하기(저장된 시드를 그대로 쓰는 경로)는 `baseSeed`를 아예 참조하지 않아 영향이 없다.

### 2-2. 생성 후보 방 모양이 1종류뿐이었던 문제

`Assets/ProjectDelta/Scenes/DungeonScene.unity`

시드를 고쳐도 여전히 같은 모양만 나왔다. 원인은 씬에 등록된 4개 방 바인딩(CROSS/NS/NE/T) 중 `includeInGenerationPool`이 CROSS만 `1`이고 나머지 3개는 `0`으로 꺼져 있던 것 - 생성 후보가 사실상 1종류뿐이었다. 3개를 `1`로 켰다.

---

## Part 3. 미로 방 10종 등록

`Assets/ProjectDelta/Scenes/DungeonScene.unity`, `Assets/ProjectDelta/Prefabs/Dungeon/Room_Maze_01~10.prefab`

18일차에 만들어둔 미로 방 프리팹 10종을 실제 생성 풀에 등록했다. 씬의 `roomBindings`에 10개 항목을 추가하는 것 외에, 프리팹 쪽에 두 가지가 빠져 있어서 같이 채웠다.

- `RoomView` 컴포넌트 - `DungeonRoomPrefabBinding.prefab`이 요구하는 컴포넌트인데 9종에 빠져 있었다(1종만 있었음).
- `RoomExitMarker` - 그래프가 실제로 연결할 출구 위치를 찾는 데 쓰는 컴포넌트인데 10종 전부 없었다. 미로 방은 전부 `(0,-2)/South` 위치에 문이 하나뿐이라 계산이 단순해서, 18일차 `Day31MultiExitRoomGenerator`의 좌표 공식을 그대로 가져와 10개 프리팹에 동일하게 추가했다.

이 두 가지가 없으면 그래프 연결 단계에서 "실제 프리팹 출구 마커가 그래프와 일치하지 않습니다" 오류로 생성이 아예 중단된다.

---

## Part 4. 현재 방 상태 HUD

`Assets/ProjectDelta/Scripts/Presentation/RoomStatusHudController.cs`

방 종류를 화면에서 확인할 수 있도록 상단 HUD를 추가했다. 96일차에 쓴 패턴대로 `Update()`에서 방 ID가 실제로 바뀔 때만 텍스트를 갱신한다. Trap 방이 이미 발동했으면 "(해제됨)"을 붙여 표시한다. Canvas에 붙이는 작업은 씬의 UI 계층 구조를 잘못 건드릴 위험이 있어 직접 하지 않고, 사용자가 에디터에서 직접 Text 오브젝트를 만들어 필드에 연결하도록 안내했다.

---

## Part 5. 미로 방 등록 후 드러난 문 시각 버그 2건

미로 방을 생성 풀에 추가하자 그동안 한 번도 실제로 쓰인 적 없던 문 관련 코드 두 곳의 문제가 연달아 드러났다.

### 5-1. 문 옆 틈새 (1차 수정)

미로 방의 문 칸은 완전히 뚫린 2m 폭 그대로였고, `RoomPassageController`가 런타임에 만드는 문 장식(`GeneratedDoorLintel`)은 1.8m 폭만 채운다는 걸 알게 됐다. 즉 남는 0.1m씩 양옆 틈으로 빛이 새고 있었다. 기존에 정상 동작하던 Day31 테스트 방들처럼, 문 칸 양옆에 0.1m 폭 벽(`Wall_0_-2_South_StubW`/`StubE`)을 10개 프리팹 모두에 추가했다.

### 5-2. 문이 열려도 사라지지 않는 큐브 (2차 수정, 진짜 원인)

1차 수정 후에도 "문이 열린 상태인데 큐브가 안 사라진다"는 문제가 남아있었다. 확인해보니 미로 방 프리팹 안에 예전부터 있던 `Door_0_-2_South`라는 정적 큐브(가로 1.6m)가 원인이었다 - `RoomExitMarker` 기반 동적 문 시스템과 완전히 무관한 별개 오브젝트라 항상 켜져 있었고, 문이 열리든 닫히든 절대 사라지지 않았다. 10개 프리팹에서 이 오브젝트를 완전히 제거해서, Day31 방과 동일하게 동적 문 시스템만으로 열림/닫힘이 표현되게 했다.

---

## Part 6. 109일차 컴파일 오류 수정

`Assets/ProjectDelta/Scripts/Presentation/SaveSlotHudController.cs`

`bool hasData = ... && TryGetSlotSummary(slot, out var summary)` 형태로 쓰면, `summary`를 나중의 별개 분기에서 참조할 때 컴파일러가 대입 여부를 추적하지 못해 CS0165가 난다. `summary`를 `SaveSlotSummary.Empty(slot)`으로 미리 선언한 뒤 `out summary`로 받는 방식으로 고쳤다.

---

## 테스트

Unity 에디터가 없는 환경이라 PlayMode 테스트 대신 EditMode 테스트로 로직/구조를 검증했다.

- `RoomTrapServiceTests` - 피해/회피/클램프/중복발동방지/비Trap방 등 `RoomTrapService.Trigger` 전 분기.
- `RoomTypeRollServiceTests` - Trap/Normal 확률 분포, 회피율 스탯 반영, 피해량 범위.
- `DungeonFloorControllerSeedTests` - `randomizeSeedEachRun` 필드 존재 확인(실제 무작위 동작은 PlayMode 필요라 리플렉션 검증만).
- `RoomStatusHudControllerUguiTests` - HUD 컨트롤러가 OnGUI 없이 uGUI 필드로만 구성됐는지 확인.

문 시각 버그 자체는 코드가 아니라 프리팹 데이터 문제라 별도 테스트 없이 스크린샷으로 직접 확인했다.
