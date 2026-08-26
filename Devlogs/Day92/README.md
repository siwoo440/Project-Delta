# Day92 - 인벤토리 가득 참 처리 및 아이템 교체 선택

## 개발 목표

인벤토리에 새 아이템이 전부 들어가지 않는 상황에서 아이템이 사라지거나 강제로 덮어써지지 않도록 획득 과정을 `Preview → 선택 → Commit` 구조로 분리한다.

공간이 부족할 경우 플레이어가 직접 `두고 간다`, `교체`, `취소` 중 하나를 선택할 수 있도록 상자 획득 흐름을 확장한다.

---

## 구현 내용

### 1. 아이템 획득 Preview 구조 추가

`InventoryAcquisitionService`를 추가하여 실제 인벤토리를 변경하기 전에 다음 정보를 계산하도록 구현했다.

- 요청한 아이템 수량
- 현재 인벤토리에 추가 가능한 수량
- 공간 부족으로 남는 수량
- 추가 선택이 필요한지 여부

Preview 단계에서는 인벤토리 상태를 변경하지 않는다.

### 2. 아이템 획득 Commit 처리 추가

획득 결과를 실제 인벤토리에 반영하는 동작을 선택별로 분리했다.

- `CommitLeave`
  - 현재 들어갈 수 있는 수량만 획득한다.
  - 들어가지 못한 수량은 남겨둔다.

- `CommitReplace`
  - 플레이어가 지정한 기존 슬롯을 비운다.
  - 기존 동일 아이템 Stack을 먼저 채운 뒤 새 아이템을 추가한다.
  - 교체가 허용되지 않은 아이템 종류는 차단한다.

- `CommitCancel`
  - Preview 결과를 폐기한다.
  - 인벤토리를 변경하지 않는다.

### 3. 91일차 아이템 분류 규칙과 교체 제한 연결

교체 가능 여부를 별도 중복 규칙으로 만들지 않고 기존 `ItemCategoryRules.GetDiscardAvailability()`를 사용하도록 연결했다.

즉시 교체 가능한 종류:

- 소비 아이템
- 탐험 도구
- 보물
- 장비

즉시 교체할 수 없는 종류:

- 중요 아이템
- 유물
- 저주
- 미분류

유물과 저주는 기존 91일차 규칙에서 조건부 버리기로 정의되어 있으므로 이번 일차에서는 안전하게 교체 대상에서 제외했다.

### 4. 상자 아이템 획득 흐름 연결

기존 `ChestInteractionController`의 직접 인벤토리 추가 방식을 새 획득 서비스 구조로 변경했다.

상자 아이템 선택 시:

```text
아이템 선택
↓
Preview
↓
전부 들어감?
├─ 예 → 즉시 획득
└─ 아니오 → 공간 부족 선택 UI
             ├─ 두고 간다
             ├─ 교체
             └─ 취소
```

공간 부족 UI가 열려 있는 동안 기존 상자 아이템 버튼과 닫기 동작이 중복 실행되지 않도록 입력 상태도 함께 제어한다.

### 5. 기존 인벤토리 슬롯을 교체 선택 UI로 재사용

교체를 선택하면 새로운 인벤토리 UI를 중복 생성하지 않고 상자 패널의 기존 인벤토리 슬롯 목록을 교체 선택 상태로 사용한다.

각 슬롯은 다음 내용을 표시한다.

```text
슬롯 번호. 아이템 이름 ×수량
```

교체가 금지된 아이템은 선택할 수 없도록 비활성 처리한다.

### 6. ItemDefinition 런타임 연결

기존 상자 데이터가 문자열 기반으로 저장되어 있으므로 `RuntimeItemDefinitionLookup`을 추가했다.

상자 아이템 문자열을 다음 정보와 비교하여 ItemDefinition을 찾는다.

- ItemDefinition 에셋 이름
- DisplayName
- 사용 가능한 ID 속성 또는 필드

정의가 확인되면 다음 값을 획득 처리에 사용한다.

- `MaxStackSize`
- `Category`

정의를 찾지 못한 아이템은 안전하게 다음 값으로 처리한다.

```text
MaxStackSize = 1
Category = Uncategorized
```

### 7. EditMode 테스트 추가

`InventoryAcquisitionServiceTests`를 추가하여 다음 규칙을 검증할 수 있도록 구성했다.

- Preview가 실제 인벤토리를 수정하지 않는지
- 가득 찬 인벤토리에서 남은 수량을 정확히 계산하는지
- 취소 시 인벤토리가 변경되지 않는지
- 두고 가기에서 들어갈 수 있는 수량만 추가되는지
- 허용된 종류의 슬롯을 교체할 수 있는지
- 중요 아이템 교체가 차단되는지
- 유물·저주·미분류 교체가 차단되는지
- 교체 전에 기존 동일 Stack을 우선 채우는지

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/InventoryAcquisitionService.cs
Assets/ProjectDelta/Scripts/Domain/InventoryAcquisitionService.cs.meta
Assets/ProjectDelta/Scripts/Presentation/RuntimeItemDefinitionLookup.cs
Assets/ProjectDelta/Scripts/Presentation/RuntimeItemDefinitionLookup.cs.meta
Assets/ProjectDelta/Tests/EditMode/InventoryAcquisitionServiceTests.cs
Assets/ProjectDelta/Tests/EditMode/InventoryAcquisitionServiceTests.cs.meta
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Presentation/ChestInteractionController.cs
```

---

## 검토 내용

- 90일차 인벤토리의 `TryAddDetailed`, Stack 수량, `RestoreSlot` 구조와 연결되는 것을 확인했다.
- 91일차 `ItemDefinition.Category`, `MaxStackSize`와 연결되는 것을 확인했다.
- 교체 가능 여부는 91일차 공통 행동 규칙을 재사용하도록 구성했다.
- Preview와 Commit을 분리하여 취소 전에는 인벤토리를 수정하지 않도록 구성했다.
- 실제 상자 아이템 획득 경로가 새 처리 구조를 사용하도록 변경했다.

현재 저장소에는 별도의 GitHub CI 상태 검사가 등록되어 있지 않으므로 Unity Editor에서의 실제 컴파일 및 EditMode Test Runner 통과 여부는 로컬 Unity에서 최종 확인한다.

---

## 완료 결과

92일차에서는 인벤토리가 가득 찼을 때 아이템 획득이 즉시 실패하거나 조용히 사라지는 대신 플레이어가 처리 방법을 선택할 수 있는 기본 구조를 완성했다.

이 구조를 기반으로 이후 아이템 사용, 버리기, 장착 등의 실제 행동 시스템에서도 동일한 아이템 분류 규칙과 슬롯 상태를 재사용할 수 있다.
