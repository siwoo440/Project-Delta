# Project Delta — 95일차 개발 일지

- 날짜: 2026-08-27
- 기준 브랜치: `main`
- 최신 커밋: `b5b15a7cf364282d8a0d110565682a6e237570e4`
- 현재 커밋 메시지: `a`
- 비교 기준: `2dfe836029cd3b1970fa29a228c2dd2b32c7c05d`
  - `94일차 : 인벤토리 이동·교환 및 버리기 상호작용 구현`

---

## 1. 오늘의 목표

95일차는 89~94일차에서 구현한 인벤토리·아이템 시스템을 마무리하는 단계로,
**인벤토리 Stack 정보와 상자 남은 아이템 상태가 저장·이어하기 과정에서도 정확히 유지되도록 저장/복원 구조를 안정화**했다.

핵심 목표는 다음과 같다.

- 인벤토리 슬롯의 `MaxStackSize` 저장
- HUD 초기화 이전에도 Stack 수량을 정확히 복원
- 95일차 이전 저장 데이터와의 하위 호환 유지
- 상자 개봉 여부와 남은 아이템 목록을 분리
- 상자 부분 획득 상태 저장
- 비어 있는 상자 상태를 명확하게 저장
- 상자 남은 아이템 순서 복원
- Scene 재구성 시 저장된 상자 상태 연결
- 저장/복원 회귀 테스트 추가
- 기존 인벤토리 제거 테스트의 실행 순서 오류 수정

---

## 2. 인벤토리 MaxStackSize 저장

기존 인벤토리 저장 데이터는 슬롯마다 다음 정보만 저장했다.

```text
ItemId
DisplayName
Quantity
```

그러나 이어하기는 Dungeon Scene과 HUD가 만들어지기 전에 실행될 수 있고,
기존 `MaxStackResolver`는 HUD 초기화 과정에서 설정되고 있었다.

이 때문에 저장된 수량이 다음과 같이 잘릴 가능성이 있었다.

```text
Potion ×4 / MaxStack 5 저장
↓
게임 재실행
↓
HUD 초기화 전 이어하기
↓
MaxStack 기본값 1
↓
Potion ×1로 복원될 가능성
```

95일차부터 슬롯 저장 데이터에 다음 값을 추가했다.

```text
MaxStackSize
```

새 저장 흐름:

```text
ItemId
DisplayName
Quantity
MaxStackSize
↓
저장
↓
이어하기
↓
저장된 MaxStackSize를 직접 사용
```

따라서 UI 초기화 순서에 의존하지 않고 Stack 수량을 복원할 수 있게 됐다.

---

## 3. 이전 저장 데이터 하위 호환

95일차 이전 저장에는 `MaxStackSize` 값이 존재하지 않는다.

이를 구분하기 위해:

```text
MaxStackSize > 0
→ 95일차 이후 저장
→ 저장된 MaxStackSize 직접 사용

MaxStackSize <= 0
→ 기존 저장
→ 기존 MaxStackResolver 경로 사용
```

으로 처리했다.

기존 세이브 파일을 바로 폐기하지 않고 이전 복원 흐름을 유지한다.

---

## 4. 상자 개봉 상태와 내용물 상태 분리

기존 상자는 다음 정보만 저장했다.

```text
ChestOpened
```

이 구조에서는 상자를 한 번 열기만 해도 이어하기 시 상자 안의 남은 아이템을 알 수 없었다.

예:

```text
Potion
Sword
Treasure
↓
상자 개봉
↓
Potion만 획득
↓
저장 / 종료
↓
이어하기
```

기존 구조에서는 `Sword`, `Treasure`가 남아 있다는 정보를 저장할 수 없었다.

95일차부터 다음 구조를 사용한다.

```text
ChestOpened
HasChestContentsSnapshot
ChestRemainingItems[]
```

`ChestOpened`는 상자의 개봉 상태만 담당하고,
`ChestRemainingItems`는 실제로 상자에 남아 있는 아이템을 담당한다.

---

## 5. RoomInstance에 상자 런타임 상태 추가

실제 플레이 중 상자의 남은 내용물을 `RoomInstance`가 관리하도록 확장했다.

추가된 주요 상태:

```text
HasChestContentsSnapshot
ChestRemainingItems
```

추가된 주요 동작:

```text
InitializeChestContents()
RestoreChestContents()
TryTakeChestItem()
```

### InitializeChestContents

새 방의 상자 원본 목록을 최초 한 번 등록한다.

### RestoreChestContents

저장 데이터에 있던 남은 아이템 목록을 그대로 복원한다.

### TryTakeChestItem

실제 아이템 획득이 성공했을 때만 해당 아이템을 상자 런타임 상태에서 제거한다.

---

## 6. ChestContentMarker 저장/복원 연결

기존 `ChestContentMarker` 자체가 별도의 `remainingItems` 목록을 관리하던 구조를
`RoomInstance`의 상자 상태와 연결했다.

새 게임:

```text
Inspector 상자 목록
↓
RoomInstance.InitializeChestContents()
```

95일차 이후 저장 이어하기:

```text
RoomRunState.ChestRemainingItems
↓
RoomInstance.RestoreChestContents()
↓
ChestContentMarker.RemainingItems
```

95일차 이전 저장에서 이미 열린 상자는 남은 아이템 목록을 알 수 없으므로,
기존 동작과의 호환을 위해 빈 상자로 복원한다.

---

## 7. 상자 부분 획득 저장

상자에서 일부 아이템만 가져간 상태도 저장할 수 있게 됐다.

예:

```text
최초
Potion
Sword
Treasure

↓ Potion 획득

Sword
Treasure

↓ 저장
↓ 종료
↓ 이어하기

Sword
Treasure
```

아이템이 전부 사라진 경우도:

```text
HasChestContentsSnapshot = true
ChestRemainingItems = Empty
```

로 저장한다.

이를 통해:

```text
이전 저장이라 목록 정보가 없음
```

과:

```text
새 저장이며 실제로 상자가 비어 있음
```

을 구분할 수 있다.

---

## 8. DungeonSaveMapper 확장

인벤토리 저장 시 슬롯별 `MaxStackSize`를 추가로 기록한다.

```text
ItemId
DisplayName
Quantity
MaxStackSize
```

복원 시에는 저장된 MaxStack 값이 있으면 직접 전달한다.

상자 저장에서는 `RoomInstance`에 Snapshot이 존재하는 경우:

```text
HasChestContentsSnapshot = true
ChestRemainingItems 복사
```

를 수행한다.

이 처리는:

```text
생성된 던전 방 저장
기존 Legacy 방 저장
```

두 흐름 모두에 적용했다.

---

## 9. 저장/복원 테스트 추가

`ItemSystemPersistenceTests`를 추가하여 다음 흐름을 검증할 수 있도록 했다.

- Resolver가 없어도 Quantity와 MaxStackSize가 보존되는지
- 95일차 이전 저장이 기존 Resolver를 사용하는지
- 상자 일부 획득 후 남은 아이템만 저장되는지
- 상자가 실제로 비어 있는 Snapshot을 구분하는지
- 상자 남은 아이템 순서가 유지되는지
- `BeginRestore()` 이후 Scene 재구성 코드가 상자 Snapshot을 조회할 수 있는지

이를 통해 89~94일차에서 구현된 아이템 기능을 저장/복원 계층까지 연결했다.

---

## 10. 기존 인벤토리 테스트 오류 수정

전체 EditMode 테스트 실행 중 다음 테스트에서 실패가 확인됐다.

```text
TryRemoveQuantityAt_DecreasesAndClearsSlot

Expected: 1
But was: 0
```

원인을 확인한 결과 `TryRemoveQuantityAt()` 구현 문제가 아니라
테스트 내부의 실행 순서 문제였다.

기존 테스트는:

```text
수량 2
↓
첫 번째 1개 제거
↓
두 번째 1개 제거
↓
수량이 1인지 검사
```

순서여서 검사 시점에는 이미 수량이 0이었다.

다음과 같이 보정했다.

```text
수량 2
↓
첫 번째 1개 제거
↓
수량 1 확인
↓
두 번째 1개 제거
↓
빈 슬롯 확인
```

프로덕션의 `TryRemoveQuantityAt()` 동작은 변경하지 않았다.

---

## 11. 94일차 대비 변경 범위

### 생성

```text
Assets/ProjectDelta/Tests/EditMode/ItemSystemPersistenceTests.cs
Assets/ProjectDelta/Tests/EditMode/ItemSystemPersistenceTests.cs.meta
```

### 수정

```text
Assets/ProjectDelta/Scripts/Data/DungeonSaveMapper.cs
Assets/ProjectDelta/Scripts/Data/RunData.cs
Assets/ProjectDelta/Scripts/Domain/RoomInstance.cs
Assets/ProjectDelta/Scripts/Presentation/ChestContentMarker.cs
Assets/ProjectDelta/Tests/EditMode/RunInventoryStateTests.cs
```

---

## 12. 현재 아이템 시스템 흐름

95일차 기준으로 89~95일차 아이템 시스템은 다음 흐름까지 연결됐다.

```text
상자 생성
↓
아이템 획득
↓
인벤토리 10칸 슬롯
↓
Stack
↓
가득 참 / 교체 선택
↓
아이템 분류
↓
사용
↓
이동 / 병합 / 교환
↓
버리기
↓
인벤토리 저장
↓
상자 남은 아이템 저장
↓
게임 종료
↓
이어하기
↓
인벤토리 / 상자 상태 복원
```

이로써 인벤토리·아이템 구간의 기본 플레이 흐름을 저장 시스템까지 연결했다.

---

## 13. 검증 메모

GitHub `main`의 최신 상태를 기준으로 확인했다.

- 최신 커밋: `b5b15a7cf364282d8a0d110565682a6e237570e4`
- 현재 커밋 메시지: `a`
- 94일차 기준보다 1개 커밋 앞섬
- `RunInventorySlotData.MaxStackSize` 추가 확인
- 저장 시 `MaxStackSize` 기록 확인
- 복원 시 새 저장/기존 저장 분기 확인
- `RoomRunState.HasChestContentsSnapshot` 추가 확인
- `ChestRemainingItems` 저장 구조 확인
- `RoomInstance`의 상자 런타임 상태 추가 확인
- `ChestContentMarker`의 저장 Snapshot 복원 연결 확인
- `ItemSystemPersistenceTests` 추가 확인
- `TryRemoveQuantityAt_DecreasesAndClearsSlot`의 검사 순서 수정이 최신 저장소에 반영된 것을 확인
- 최신 커밋에 연결된 GitHub status check 결과는 없음

사용자 환경에서 앞서 확인된 단일 실패 테스트의 원인은 수정되어 최신 저장소에 반영돼 있다.

다만 현재 환경에서는 Unity Editor를 직접 실행할 수 없고
GitHub에도 자동 CI status가 없으므로,
**최신 커밋 기준 전체 Unity Test Runner의 최종 0 Failure 상태는 독립적으로 재실행해 확인하지 못했다.**

---

## 14. 다음 단계

95일차에서 인벤토리·아이템 시스템의 저장/복원까지 마무리했으므로,
다음 개발 구간에서는 장비 시스템으로 넘어갈 수 있다.

다음 단계의 주요 방향:

```text
Equipment 데이터
↓
장비 슬롯
↓
장착 / 해제
↓
능력치 적용
↓
가방 장비
↓
인벤토리 슬롯 확장 연결
```

89~95일차에서 만든 ItemCategory, Inventory, 저장 시스템을
장비 시스템에서도 그대로 재사용한다.
