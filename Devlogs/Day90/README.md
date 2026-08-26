# Project Delta — 90일차 개발 일지

- 날짜: 2026-08-26
- 기준 브랜치: `main`
- 최신 커밋: `c72220ca7a86d4b2c4ed6b4df4fb1085a1a76703`
- 현재 커밋 메시지: `a`
- 비교 기준: `c69d73e2906525b558ef0843043fb934a8e3e384`
  - `89일차 : 10칸 슬롯 인벤토리 및 상시 플레이어 HUD 구현`

---

## 1. 오늘의 목표

90일차는 89일차에 만든 10칸 슬롯 인벤토리를 기반으로
**동일 아이템 중첩(Stack)과 슬롯 수량 관리**를 구현하는 작업을 진행했다.

핵심 목표는 다음과 같다.

- ItemDefinition에 최대 중첩 수 추가
- 같은 Item ID를 기존 슬롯에 우선 중첩
- 최대 중첩 초과 시 다음 빈 슬롯 사용
- 인벤토리 공간 부족 시 남은 수량 반환
- 같은 아이템 슬롯끼리 이동할 때 Stack 합치기
- 슬롯 단위 수량 감소
- 슬롯 번호를 왼쪽 위에 표시
- 아이템 수량을 오른쪽 아래에 표시
- 선택 아이템 정보에 보유 수량 표시
- Stack 상태 저장/복원 테스트 확장

---

## 2. ItemDefinition 최대 중첩 수

`ItemDefinition`에 `maxStackSize`를 추가했다.

기본값은 1이며 Inspector에서 아이템마다 최대 중첩 수를 지정할 수 있다.

예시:

- 중첩 불가 아이템: 1
- 일반 소비 아이템: 3~5
- 열쇠/도구 등: 기획서 기준 값 적용

`MaxStackSize`는 최소 1 이상이 되도록 제한한다.

---

## 3. InventorySlotState 확장

기존 슬롯 데이터에 최대 중첩 정보를 추가했다.

각 슬롯은 다음 값을 가진다.

- ItemId
- DisplayName
- Quantity
- MaxStackSize
- IsEmpty
- CanStackMore

추가된 핵심 동작:

- `AddQuantity()`
  - 현재 Stack의 남은 공간만큼 수량 추가
  - 들어가지 못한 수량 반환

- `RemoveQuantity()`
  - 지정 수량 감소
  - 수량이 0이 되면 슬롯 자동 초기화

---

## 4. 아이템 중첩 획득

아이템 획득 순서를 다음처럼 변경했다.

1. 동일한 Item ID를 가진 기존 슬롯 검색
2. 최대 중첩에 도달하지 않은 슬롯부터 채움
3. 아직 남은 수량이 있으면 빈 슬롯 검색
4. 새 Stack 생성
5. 인벤토리 공간이 부족하면 남은 수량 반환

예:

`Potion ×4 / MaxStack 5`

상태에서 Potion ×3 획득 시:

`Potion ×5`
`Potion ×2`

형태로 나뉘어 저장된다.

---

## 5. InventoryAddResult

부분 획득 상황을 처리하기 위해 `InventoryAddResult`를 추가했다.

주요 정보:

- RequestedQuantity
- AddedQuantity
- RemainingQuantity
- FirstChangedSlotIndex
- IsComplete

이를 통해 이후 92일차의
`인벤토리 가득 참 → 두고 간다 / 교체 / 취소`
처리에서 남은 아이템 수량을 그대로 사용할 수 있다.

---

## 6. 슬롯 이동 및 Stack 병합

기존 슬롯 이동/교환 규칙을 확장했다.

### 다른 아이템

기존과 동일하게 두 슬롯을 Swap한다.

### 같은 아이템

두 슬롯을 바로 교환하지 않고 먼저 Stack 병합을 시도한다.

예:

`Potion ×4`
`Potion ×3`

최대 Stack 5일 때 합치면:

`Potion ×5`
`Potion ×2`

가 된다.

---

## 7. 슬롯 수량 감소

`TryRemoveQuantityAt()`을 추가했다.

예:

`Potion ×3`

에서 1개 감소:

`Potion ×2`

마지막 1개까지 감소하면 해당 슬롯은 Empty 상태로 돌아간다.

이 기능은 이후 실제 소비 아이템 사용 시스템에서 사용할 기반이다.

---

## 8. 인벤토리 UI 표시 변경

90일차부터 슬롯 내부 숫자의 의미를 분리했다.

### 슬롯 번호

- 위치: **왼쪽 위**
- 표시: `1 ~ 10`

### 아이템 수량

- 위치: **오른쪽 아래**
- 표시: `×2`, `×3`, `×5` 형태
- 수량이 1이면 별도 수량 텍스트를 표시하지 않음

따라서 슬롯 번호와 실제 아이템 보유 수량을 동시에 확인할 수 있게 됐다.

---

## 9. 선택 아이템 정보

아이템 슬롯을 선택했을 때 표시되는 정보에
현재 Stack의 보유 수량을 추가했다.

예:

`소형 회복약`
`보유 수량 ×4`
`HP를 회복한다.`

아이콘과 설명은 기존 `ItemDefinition`의 데이터를 계속 사용한다.

---

## 10. UI Scene 변경

`DungeonScene`의 기존 5×2 인벤토리 슬롯에
각각 `SlotQuantityText`를 추가했다.

또한 기존 `SlotNumberText`의 RectTransform을 수정하여

- SlotNumberText → 왼쪽 위
- SlotQuantityText → 오른쪽 아래

구조로 통일했다.

---

## 11. 테스트 확장

`RunInventoryStateTests`에 90일차 기능을 검증하는 테스트를 추가/확장했다.

검증 대상:

- 새 인벤토리 10칸 유지
- 첫 빈 슬롯 획득
- 기존 Stack 우선 채우기
- MaxStack 1 아이템 중첩 금지
- 슬롯 위치 보존
- 같은 아이템 이동 시 Stack 병합
- 수량 감소 및 슬롯 자동 비우기
- 공간 부족 시 RemainingQuantity 반환
- 영구/가방 슬롯 보너스 유지
- 저장/복원 후 Stack 수량과 슬롯 위치 유지

---

## 12. 테스트 컴파일 오류 수정

90일차 작업 중 다음 오류가 발생했다.

`Assets\ProjectDelta\Tests\EditMode\RunInventoryStateTests.cs`
`CS0103: The name 'DungeonSaveMapper' does not exist in the current context`

원인은 테스트 파일에서 저장 생성 호출에는

`ProjectDelta.Data.DungeonSaveMapper.BuildFromRunContext(...)`

를 사용했지만 복원 호출에는 네임스페이스 없이

`DungeonSaveMapper.ApplyBasics(...)`

를 사용한 것이었다.

최종적으로 다음과 같이 통일했다.

`ProjectDelta.Data.DungeonSaveMapper.ApplyBasics(...)`

최신 GitHub 소스에서 해당 수정이 반영된 것을 확인했다.

---

## 13. 변경 파일

89일차 커밋과 비교하여 90일차에서는 총 5개 파일이 변경됐다.

- `Assets/ProjectDelta/Scenes/DungeonScene.unity`
- `Assets/ProjectDelta/Scripts/Data/ItemDefinition.cs`
- `Assets/ProjectDelta/Scripts/Domain/RunSubStates.cs`
- `Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs`
- `Assets/ProjectDelta/Tests/EditMode/RunInventoryStateTests.cs`

---

## 14. 현재 상태

90일차 기준으로 인벤토리는 다음 기능을 갖는다.

- 10칸 슬롯
- 동일 아이템 Stack
- 아이템별 최대 Stack
- 초과 수량 분할
- 부분 획득 결과
- 슬롯 이동
- 다른 아이템 Swap
- 같은 아이템 Stack 병합
- 수량 감소
- 슬롯 번호/수량 분리 표시
- 선택 아이템 보유 수량 표시
- 슬롯 위치와 Quantity 저장/복원 기반

다음 91일차에서는 이 구조 위에
**아이템 7분류 시스템**을 추가할 예정이다.

---

## 15. 검증 메모

GitHub 최신 상태를 기준으로 다음 항목을 확인했다.

- 최신 커밋: `c72220ca7a86d4b2c4ed6b4df4fb1085a1a76703`
- 89일차 기준 커밋보다 1개 커밋 앞섬
- 변경 파일 5개
- `ItemDefinition.MaxStackSize` 추가 확인
- Stack 관련 `InventoryRunState` 확장 확인
- `SlotQuantityText`가 DungeonScene에 추가된 변경 확인
- `RunInventoryStateTests`의 `DungeonSaveMapper.ApplyBasics` 네임스페이스 수정 확인
- GitHub commit status check 없음
- GitHub Actions workflow run 없음

현재 환경에서는 Unity Editor를 실행할 수 없으므로
**실제 Unity 컴파일과 EditMode Test Runner 통과 여부는 검증하지 못했다.**

GitHub 소스의 정적 확인 기준으로는 다음 일차 진행을 막는 새 소스 구조 문제는 발견하지 못했다.
