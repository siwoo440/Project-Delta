# Project Delta — 94일차 개발 일지

- 날짜: 2026-08-26
- 기준 브랜치: `main`
- 최신 커밋: `df552b18dd838adcd7720b4869edd64ba36b2ca9`
- 현재 커밋 메시지: `a`
- 비교 기준: `95814ba311643bb2733a69f2bf55d62c24cee097`
  - `93일차 : 소비 아이템 사용 효과 및 전투·탐험 사용 처리 구현`

---

## 1. 오늘의 목표

94일차는 93일차에서 구현한 아이템 사용 기능을 기반으로
**인벤토리 슬롯을 직접 조작하는 정식 상호작용 UI**를 확장했다.

이번 일차의 핵심 목표는 다음과 같다.

- 인벤토리 슬롯 이동
- 같은 아이템 Stack 병합
- 서로 다른 아이템 자리 교환
- 아이템 1개 버리기
- 아이템 전부 버리기
- 아이템 분류 규칙에 따른 버리기 제한
- 선택 아이템 패널에 `사용 / 이동 / 버리기` 액션 통합
- 이동·버리기 성공 후 저장
- 기존 `TryMoveOrSwap()` 교환 버그 수정
- 관련 EditMode 테스트 추가 및 기존 Stack 테스트 보정

---

## 2. InventoryInteractionService 추가

인벤토리 UI가 슬롯 조작 규칙을 직접 구현하지 않도록
Application 계층에 `InventoryInteractionService`를 추가했다.

주요 API:

```text
Move()
DiscardOne()
DiscardAll()
```

각 동작은 `InventoryInteractionResult`를 반환하여
성공 여부와 실패 이유를 UI에 전달한다.

실패 이유는 다음과 같이 구분한다.

```text
InvalidInventory
InvalidSourceSlot
InvalidDestinationSlot
SameSlot
MoveFailed
DiscardNotAllowed
RemoveFailed
```

---

## 3. 슬롯 이동

인벤토리에서 아이템을 선택한 뒤 `이동` 버튼을 누르면
이동 대상 슬롯 선택 모드로 전환된다.

흐름:

```text
아이템 선택
→ [이동]
→ 이동할 슬롯 선택
```

이후 대상 슬롯 상태에 따라 다음과 같이 처리한다.

### 빈 슬롯

```text
Source 슬롯
→ Destination 슬롯
```

전체 Stack을 그대로 이동한다.

### 같은 아이템

남은 Stack 공간만큼 자동 병합한다.

예:

```text
Slot 1 : Potion ×3 / 5
Slot 2 : Potion ×4 / 5

Slot 2 → Slot 1

결과
Slot 1 : Potion ×5
Slot 2 : Potion ×2
```

### 서로 다른 아이템

두 슬롯의 아이템 정보를 서로 교환한다.

이동이 성공하면 이동한 원래 아이템을 계속 추적할 수 있도록
Destination 슬롯을 새 선택 슬롯으로 유지한다.

---

## 4. TryMoveOrSwap 교환 버그 수정

기존 `InventoryRunState.TryMoveOrSwap()`의
서로 다른 아이템 교환 분기에는 Destination 데이터를 먼저 보관하지 않고
Source 데이터로 덮어쓴 뒤 다시 읽는 문제가 있었다.

기존 위험 구조:

```text
Destination ← Source
Source ← 이미 덮어써진 Destination
```

94일차에서는 Destination의 다음 데이터를 먼저 백업하도록 수정했다.

```text
ItemId
DisplayName
Quantity
MaxStackSize
```

정상 교환 구조:

```text
Source 백업
Destination 백업
Destination ← Source 백업
Source ← Destination 백업
```

이제 서로 다른 두 아이템을 교환해도
양쪽 아이템의 ID, 이름, 수량, 최대 Stack 수치가 모두 보존된다.

---

## 5. 기존 Stack 테스트 보정

기존 `MoveSameItem_MergesStacks` 테스트는
첫 번째 Stack이 이미 최대치인 상태에서 추가 병합을 시도하는 구조라
실제 테스트 의도와 상태가 맞지 않았다.

94일차에서는 다음과 같이 보정했다.

```text
Potion ×5
Potion ×4

첫 번째 Stack에서 2개 제거

Potion ×3
Potion ×4
```

이후 두 번째 슬롯을 첫 번째 슬롯으로 이동하여:

```text
Potion ×5
Potion ×2
```

가 되는지를 검증하도록 수정했다.

---

## 6. 버리기 기능

선택 아이템 패널에 `버리기` 버튼을 추가했다.

버리기 버튼을 누르면 확인 패널이 표시된다.

```text
아이템 이름 ×수량
버릴 수량을 선택하세요.

[1개 버리기]
[전부 버리기]
[취소]
```

### 1개 버리기

```text
Potion ×5
→ Potion ×4
```

### 전부 버리기

```text
Potion ×5
→ 빈 슬롯
```

마지막 1개를 버려 슬롯이 비면
선택 상태와 상세 패널도 함께 정리된다.

---

## 7. 아이템 분류별 버리기 제한

91일차에서 만든 `ItemCategoryRules.CanDiscard()`를 그대로 사용한다.

현재 즉시 버리기 허용:

```text
Consumable
ExplorationTool
Treasure
Equipment
```

현재 버리기 차단:

```text
KeyItem
Relic
Cursed
Uncategorized
```

유물과 저주는 조건부 버리기 규칙이지만
해당 조건 시스템이 아직 구현되지 않았으므로
94일차에서는 안전하게 버리기를 차단한다.

중요 아이템도 버릴 수 없지만 슬롯 이동은 가능하다.

---

## 8. 선택 아이템 액션 UI 확장

93일차의 단일 `사용` 버튼 구조를 다음 형태로 확장했다.

```text
SelectedItemPanel
├ [사용]
├ [이동]
└ [버리기]
```

기존 Scene을 직접 수정하지 않아도
필요한 버튼과 버리기 확인 패널을 런타임에 자동 생성한다.

### 사용

93일차 ItemUseService 흐름을 그대로 유지한다.

### 이동

이동 모드로 전환한다.

이동 중에는 빈 슬롯도 Destination으로 선택할 수 있다.

### 버리기

아이템 분류 규칙을 확인한 뒤
허용된 아이템만 확인 패널을 열 수 있다.

---

## 9. 이동 모드 상태 표시

이동 모드에서는 Source 슬롯 번호 앞에 표시를 추가한다.

예:

```text
▶ 3
```

또한 이동 버튼 문구도:

```text
이동
→ 이동 취소
```

로 변경된다.

같은 Source 슬롯을 다시 누르거나
`이동 취소`를 선택하면 이동 상태를 종료한다.

---

## 10. 저장 처리

인벤토리 상태가 실제로 변경되는 다음 행동에서
기존 던전 저장 흐름을 호출한다.

```text
아이템 사용
아이템 이동
Stack 병합
자리 교환
1개 버리기
전부 버리기
```

실패한 이동이나 버리기 요청에서는
인벤토리 상태를 변경하거나 저장하지 않는다.

---

## 11. EditMode 테스트 추가

`InventoryInteractionServiceTests`를 추가해
다음 동작을 검증할 수 있도록 했다.

- 서로 다른 아이템 교환 시 양쪽 Stack 정보 보존
- 빈 슬롯으로 전체 Stack 이동
- 같은 아이템 병합 시 남은 공간만큼만 이동
- 1개 버리기
- 전부 버리기
- 중요 아이템 버리기 차단
- 유물 버리기 차단
- 저주 아이템 버리기 차단
- 미분류 아이템 버리기 차단
- 중요 아이템 이동 허용
- 기존 `TryMoveOrSwap()` 직접 호출 시에도 원래 Destination 데이터 보존

---

## 12. 93일차 대비 변경 범위

### 생성

```text
Assets/ProjectDelta/Scripts/Application/InventoryInteractionService.cs
Assets/ProjectDelta/Scripts/Application/InventoryInteractionService.cs.meta
Assets/ProjectDelta/Tests/EditMode/InventoryInteractionServiceTests.cs
Assets/ProjectDelta/Tests/EditMode/InventoryInteractionServiceTests.cs.meta
```

### 수정

```text
Assets/ProjectDelta/Scripts/Domain/RunSubStates.cs
Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs
Assets/ProjectDelta/Tests/EditMode/RunInventoryStateTests.cs
```

자동 보정용 Editor Installer는 적용 과정에서 역할을 완료한 뒤
최종 최신 커밋에는 남아 있지 않는다.

---

## 13. 현재 상태

94일차 기준 인벤토리 상호작용 흐름은 다음까지 연결됐다.

```text
아이템 획득
→ Stack / 슬롯 저장
→ 아이템 선택
→ 사용
→ 이동
→ Stack 병합
→ 자리 교환
→ 버리기
→ 저장
```

94일차 범위에서는 다음 기능은 다루지 않았다.

```text
장착
판매
정렬
Drag & Drop
유물 조건부 버리기
저주 조건부 버리기
```

이 기능들은 이후 장비·상점·인벤토리 확장 단계에서 처리한다.

---

## 14. 검증 메모

GitHub `main`의 최신 상태를 기준으로 확인했다.

- 최신 커밋: `df552b18dd838adcd7720b4869edd64ba36b2ca9`
- 현재 커밋 메시지: `a`
- 93일차 기준보다 1개 커밋 앞섬
- `InventoryInteractionService` 추가 확인
- `RunSubStates.TryMoveOrSwap()`의 Destination 백업 수정 확인
- `PlayerInventoryHudController`의 이동·버리기 UI 확장 확인
- `InventoryInteractionServiceTests` 추가 확인
- 기존 `MoveSameItem_MergesStacks` 테스트 상태 보정 확인
- 최신 커밋에 연결된 GitHub status check 결과는 없음

현재 환경에서는 Unity Editor를 실행할 수 없으므로
**실제 Unity 컴파일 및 EditMode Test Runner 통과 여부는 검증하지 못했다.**

GitHub 최신 소스의 정적 구조와 변경 내용을 확인한 범위에서는
94일차 개발일지 작성을 막을 만한 새로운 명백한 코드 불일치는 확인되지 않았다.
