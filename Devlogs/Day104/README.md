# Project Delta - 104일차 개발일지

## 작업 개요

104일차는 유물 시스템의 기반과 저주 유물, 유물 패널 UI를 만들었다.

장비와 달리 유물은 인벤토리 슬롯을 거치지 않고 "보유 목록"으로 관리되며, 획득 즉시 적용되고 해제 개념이 없다는 전제로 설계했다.

유물의 실제 패시브 효과 적용과 전투 보상 연결은 이번 범위에서 다루지 않는다. 105일차(골드 시스템+상점)로 이어간다.

---

## 1. RelicRunState - 유물 보유 목록

`Assets/ProjectDelta/Scripts/Domain/RelicRunState.cs`

`EquipmentRunState`와 같은 위치의 새 런타임 상태다. 6부위 슬롯 대신 단순 리스트(`List<RelicInstanceState>`)로 관리하며, 기본 최대 보유 수는 5개다.

- `HasRelic(relicId)` - 동일 ID 중복 보유를 막기 위한 조회.
- `AddRelic`은 `internal`로 막아뒀다 - 실제 획득 규칙(중복·용량 검사)은 반드시 `RelicService.Acquire`를 거치도록 강제한다. `EquipmentRunState.SetEquippedItem`이 `EquipmentService.Equip`을 통해서만 호출되는 것과 같은 패턴이다.
- `SetMaxCapacity`를 열어뒀다 - 139일차 예정인 "기타 영구 강화 7종"에 유물 한도 강화가 포함되어 있어, 그 때 이 메서드로 최대치를 늘릴 수 있게 미리 준비했다.
- `RestoreFrom`은 세이브/로드 복원용이며, 중복 ID나 용량 초과분은 조용히 무시한다.

---

## 2. RelicService - 획득 규칙

`Assets/ProjectDelta/Scripts/Domain/RelicService.cs`

`Acquire(relics, relicId, displayName, isCursed)` 하나로 "동일 ID 중복 금지 + 최대 보유 수" 두 규칙을 한 곳에서 처리한다. 실패 사유(`RelicAcquisitionFailureReason`)로 `AlreadyOwned`/`CapacityFull`/`InvalidState`를 구분해 반환한다. `EquipmentService`와 마찬가지로 무작위성이나 UI 의존성이 없는 순수 Domain 로직이다.

---

## 3. RelicDefinition - 별도 Definition으로 분리

`Assets/ProjectDelta/Scripts/Data/RelicDefinition.cs`

`ItemDefinition`을 확장하지 않고 새 ScriptableObject로 만들었다. 유물은 인벤토리 슬롯 점유 여부 자체가 장비·소비 아이템과 다르기 때문이다(획득 즉시 슬롯을 거치지 않고 바로 적용). 이름·설명·`IsCursed`만 가진 최소 구조로 시작했다.

103일차 저주 장비와 같은 원칙 - 저주 유물도 불리한 효과를 description에 전부 공개하는 것을 전제로 한다.

---

## 4. RunContext에 연결

`Assets/ProjectDelta/Scripts/Domain/RunContext.cs`

`Relics` 프로퍼티(`RelicRunState`)를 추가해 `Player`/`Inventory`/`Equipment`와 동급의 독립 런타임 상태로 두었다. 기존 빈 스텁이던 `RewardRunState`는 건드리지 않았다 - 유물은 보상 그 자체가 아니라 보상으로 "획득하는 대상"이라 별도 시스템으로 분리하는 게 맞다고 판단했다.

---

## 5. 유물 패널 (읽기 전용 UI)

`Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs`

98일차 장비 패널과 비슷한 위치에 5칸짜리 유물 패널을 추가했다. 유물은 장착/해제 개념이 없으므로 버튼 없이 이름 텍스트만 표시한다.

- 빈 칸은 "비어있음".
- 저주 유물은 이름 앞에 `[저주]`를 붙여 항상 저주 여부를 드러낸다.
- `RefreshInventory()`가 호출될 때마다 `RefreshEquipmentPanel()` 옆에서 `RefreshRelicPanel()`도 함께 갱신된다.

---

## 6. 테스트

- `RelicRunStateTests` - 기본 최대 5개, 용량 도달 후 추가 시도 실패, `SetMaxCapacity` 최소값(1) 보장, `RestoreFrom`이 중복·초과분을 걸러내는지, `null` 복원 시 목록이 비워지는지.
- `RelicServiceTests` - 신규 획득 성공, 중복 ID 실패(`AlreadyOwned`), 용량 초과 실패(`CapacityFull`), `null`/빈 ID 실패(`InvalidState`), 저주 여부가 그대로 저장되는지.
- `RelicDefinitionTests` - 이름·설명·저주 여부가 설정한 값 그대로 노출되는지, `IsCursed` 기본값이 `false`인지.
- `PlayerInventoryHudEquipmentUguiTests` - 유물 패널 필드(`relicPanel`, `relicSlotNameTexts`)와 `RefreshRelicPanel` 메서드가 존재하는지(리플렉션).

`RelicRunState.AddRelic`은 `internal`이라 테스트에서도 직접 호출하지 않고 전부 `RelicService.Acquire`를 통해 검증했다 - 실제 게임 코드와 같은 경로를 타도록 강제했다.

---

## 7. Unity 에디터에서 확인해야 할 사항

1. **Scene 작업이 필요하다** - 이번 일차는 유물 패널을 표시할 UI 요소 자체가 Scene에 없다. 인벤토리 HUD에 빈 패널 GameObject와 Text 5개를 만들어 `PlayerInventoryHudController`의 `relicPanel`, `relicSlotNameTexts[0]`~`[4]`에 연결해달라.
2. 유물 에셋(`RelicDefinition`)을 몇 개 만들어보고, `RelicService.Acquire`를 통해 획득시켰을 때 유물 패널에 이름이 뜨는지, 저주 유물은 `[저주]` 표시가 붙는지 확인해달라.
3. 6개째 유물을 획득 시도했을 때 실패하는지(기본 한도 5개), 이미 보유한 유물을 다시 획득 시도했을 때 실패하는지 확인해달라 - 다만 실제로 이걸 트리거할 UI/보상 연결은 아직 없으므로, 테스트 코드나 임시 디버그 호출로 확인해야 한다.
4. 새 EditMode 테스트를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
5. 유물의 실제 패시브 효과와 전투 보상 연결은 다루지 않았다 - 콘텐츠 제작 단계에서 실제 유물 데이터가 만들어질 때 함께 설계할 필요가 있다.
6. 재구성한 개발 일정 기준 다음은 105일차 - 골드 시스템 + 상점이다.
