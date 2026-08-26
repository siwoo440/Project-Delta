# Project Delta — 91일차 개발 일지

- 날짜: 2026-08-26
- 기준 브랜치: `main`
- 최신 커밋: `7efa33a936e0c35aab60d23688ee18288aaac607`
- 현재 커밋 메시지: `a`
- 비교 기준: `55e308a936f93f23aa64c45d4889deede251f79a`
  - `90일차 : 소비 아이템 중첩 및 슬롯 수량 관리 구현`

---

## 1. 오늘의 목표

91일차는 90일차에 구현한 인벤토리 Stack과 슬롯 수량 관리 구조를 기반으로
**아이템 7분류와 분류별 공통 행동 규칙**을 추가하는 작업을 진행했다.

핵심 목표는 다음과 같다.

- 실제 게임 아이템을 7개 종류로 분류
- 기존 아이템 에셋을 위한 `Uncategorized` 마이그레이션 상태 제공
- `ItemDefinition`에 Category 데이터 추가
- 사용 / 판매 / 버리기 / 장착 가능 여부를 공통 규칙으로 관리
- 조건부 행동을 표현할 수 있는 상태 추가
- 선택한 아이템의 분류를 인벤토리 UI에 표시
- 잘못 설정된 아이템 분류와 Stack 값을 찾는 Editor 검증 메뉴 추가
- 아이템 분류 규칙 EditMode 테스트 추가
- Domain / Data 어셈블리 의존 방향 정리

---

## 2. 아이템 7분류

실제 게임에서 사용하는 아이템 종류를 다음 7개로 정리했다.

| 코드 | 표시명 | 기본 용도 |
| --- | --- | --- |
| `Consumable` | 소비 아이템 | 사용 후 효과를 발생시키는 일반 소비형 아이템 |
| `ExplorationTool` | 탐험 도구 | 탐험 중 사용하는 도구 |
| `KeyItem` | 중요 아이템 | 진행과 연결되는 중요 아이템 |
| `Treasure` | 보물 | 판매와 수집을 중심으로 하는 아이템 |
| `Equipment` | 장비 | 장착하여 사용하는 아이템 |
| `Relic` | 유물 | 특수 규칙을 가지는 유물 |
| `Cursed` | 저주 | 버리기나 장착에 별도 조건이 필요한 저주 아이템 |

추가로 `Uncategorized = 0`을 두었다.

이 값은 실제 게임 분류에는 포함하지 않으며,
기존 `ItemDefinition` 에셋이 업데이트 직후 임의의 종류로 자동 지정되는 것을 막기 위한
마이그레이션용 기본 상태다.

---

## 3. ItemCategory의 Domain 배치

91일차 작업 중 아이템 분류 타입의 어셈블리 위치를 정리했다.

최종 구조는 다음과 같다.

```text
ProjectDelta.Domain
├─ ItemCategory
└─ ItemCategoryRules
        ↓
ProjectDelta.Data
└─ ItemDefinition
```

`ProjectDelta.Data`가 이미 `ProjectDelta.Domain`을 참조하는 구조이므로
`ItemCategory`와 공통 규칙을 Domain에 배치했다.

이를 통해 Domain이 Data를 역참조하는 구조를 만들지 않고
`ItemDefinition`이 Domain의 분류 타입을 사용하는 단방향 의존성을 유지한다.

초기 구현 과정에서 `ItemCategoryRules`가 Data 쪽 타입을 참조하면서 발생했던
어셈블리 컴파일 문제는 이 구조로 정리했다.

최신 `main`에서는 잘못된 `Data/ItemCategory.cs`가 남아 있지 않고
`Domain/ItemCategory.cs`를 단일 분류 정의로 사용한다.

---

## 4. ItemDefinition 확장

`ItemDefinition`에 다음 데이터가 추가됐다.

```text
Category
```

기본값은 `ItemCategory.Uncategorized`다.

기존의 다음 데이터는 그대로 유지한다.

- DisplayName
- Icon
- Description
- MaxStackSize

따라서 각 아이템 에셋은 기존 Stack 설정과 함께
아이템 종류도 직접 지정할 수 있게 됐다.

---

## 5. ItemActionAvailability

모든 행동을 단순한 `true / false`로만 처리하지 않고
다음 세 상태를 표현할 수 있도록 했다.

| 값 | 의미 |
| --- | --- |
| `Unavailable` | 해당 행동을 할 수 없음 |
| `Available` | 별도 조건 없이 행동 가능 |
| `Conditional` | 아이템 또는 상황별 추가 조건 확인 필요 |

특히 유물과 저주 아이템처럼
일반 아이템과 다른 예외 규칙을 이후 확장할 수 있도록 준비한 구조다.

---

## 6. ItemCategoryRules

아이템 종류별 공통 규칙을 `ItemCategoryRules`에 모았다.

주요 기능은 다음과 같다.

- `IsGameplayCategory()`
  - 실제 게임에서 사용하는 7분류인지 확인
- `GetDisplayName()`
  - UI용 한글 분류명 반환
- `GetUseAvailability()`
  - 사용 가능 상태 반환
- `GetSellAvailability()`
  - 판매 가능 상태 반환
- `GetDiscardAvailability()`
  - 버리기 가능 상태 반환
- `GetEquipAvailability()`
  - 장착 가능 상태 반환
- `CanUse()`
- `CanSell()`
- `CanDiscard()`
- `CanEquip()`
  - 즉시 실행 가능한 상태인지 간단히 확인

분류별 기본 정책은 다음과 같다.

| 분류 | 사용 | 판매 | 버리기 | 장착 |
| --- | --- | --- | --- | --- |
| 소비 아이템 | 가능 | 가능 | 가능 | 불가 |
| 탐험 도구 | 가능 | 가능 | 가능 | 불가 |
| 중요 아이템 | 불가 | 불가 | 불가 | 불가 |
| 보물 | 불가 | 가능 | 가능 | 불가 |
| 장비 | 불가 | 가능 | 가능 | 가능 |
| 유물 | 불가 | 조건부 | 조건부 | 불가 |
| 저주 | 불가 | 불가 | 조건부 | 조건부 |

이 규칙을 한 곳에서 관리하므로
이후 상점, 인벤토리 행동 메뉴, 장비 시스템 등이 같은 기준을 사용할 수 있다.

---

## 7. 인벤토리 UI 분류 표시

`PlayerInventoryHudController`의 선택 아이템 표시를 확장했다.

아이템 슬롯을 선택하면 설명 영역에 다음 순서로 정보가 표시된다.

```text
[아이템 분류]
보유 수량 ×N
아이템 설명
```

예:

```text
[소비 아이템]
보유 수량 ×3
HP를 회복한다.
```

분류가 지정되지 않은 기존 에셋은 `[미분류]`로 표시된다.

---

## 8. 아이템 분류 검증 메뉴

잘못된 에셋 설정을 빠르게 찾기 위해 Editor 검증 메뉴를 추가했다.

메뉴 경로:

```text
Project Delta
└─ 91일차
   └─ 아이템 분류 검증
```

검증 대상은 다음과 같다.

- `ItemDefinition.Category == Uncategorized`
  - 분류가 지정되지 않은 에셋 경고
- 장비 또는 유물의 `MaxStackSize > 1`
  - 중첩 설정을 다시 확인하도록 경고

검사가 끝나면 전체 ItemDefinition 수와
미분류 / Stack 확인 필요 항목 수를 Console에 출력한다.

---

## 9. 테스트 추가

`ItemCategoryRulesTests`를 추가했다.

검증 대상으로 다음 내용을 포함한다.

- 실제 게임 분류가 정확히 7개인지 확인
- `Uncategorized`가 실제 게임 분류에서 제외되는지 확인
- 소비 아이템 행동 규칙
- 탐험 도구 행동 규칙
- 중요 아이템 행동 규칙
- 보물 행동 규칙
- 장비 행동 규칙
- 유물 조건부 규칙
- 저주 조건부 규칙
- 7개 분류의 한글 표시명
- 새 `ItemDefinition`의 기본 분류가 `Uncategorized`인지 확인

테스트 코드는 최신 `main`에 포함되어 있다.

---

## 10. 변경 파일

90일차 커밋과 비교하여 91일차에서는 총 10개 파일이 변경됐다.

- `Assets/ProjectDelta/Scripts/Data/ItemDefinition.cs`
- `Assets/ProjectDelta/Scripts/Domain/ItemCategory.cs`
- `Assets/ProjectDelta/Scripts/Domain/ItemCategory.cs.meta`
- `Assets/ProjectDelta/Scripts/Domain/ItemCategoryRules.cs`
- `Assets/ProjectDelta/Scripts/Domain/ItemCategoryRules.cs.meta`
- `Assets/ProjectDelta/Scripts/Editor/ItemCategoryValidationMenu.cs`
- `Assets/ProjectDelta/Scripts/Editor/ItemCategoryValidationMenu.cs.meta`
- `Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs`
- `Assets/ProjectDelta/Tests/EditMode/ItemCategoryRulesTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/ItemCategoryRulesTests.cs.meta`

---

## 11. 현재 상태

91일차 기준 인벤토리 / 아이템 기반은 다음 기능을 갖는다.

- 기본 10칸 슬롯 인벤토리
- 슬롯별 아이템 수량
- 동일 아이템 Stack
- 아이템별 최대 Stack
- Stack 병합과 분할
- 슬롯 이동과 Swap
- 수량 감소
- 슬롯 위치와 수량 저장 / 복원 기반
- 아이템 7분류
- 분류별 사용 / 판매 / 버리기 / 장착 규칙
- 조건부 행동 규칙
- 선택 아이템 분류 UI 표시
- ItemDefinition 분류 설정 검증 메뉴
- 분류 규칙 EditMode 테스트 코드

이 구조를 기반으로 이후 실제 아이템 사용,
장비 장착, 상점 판매, 중요 아이템 제한 등의 동작을
동일한 분류 규칙에 연결할 수 있다.

---

## 12. 91일차 작업 중 수정한 문제

초기 아이템 분류 구현에서는
Domain에 위치한 규칙 코드가 Data 어셈블리의 타입을 참조하도록 구성되어
어셈블리 의존성 문제가 발생했다.

최종적으로 다음처럼 수정했다.

```text
잘못된 방향
Domain → Data
Data   → Domain

최종 방향
Domain
  ↓
Data
```

`ItemCategory`를 Domain으로 이동하고
`ItemDefinition`이 이를 참조하도록 수정하여
상호 참조가 필요하지 않은 구조로 정리했다.

최신 GitHub 소스에서는 이 최종 구조가 반영되어 있다.

---

## 13. 다음 일차 준비

90일차에 만든 `InventoryAddResult`와
91일차의 분류 / 행동 규칙을 이용하면
다음 단계에서 인벤토리가 가득 찼을 때의 처리나
실제 아이템 상호작용을 연결할 수 있다.

예를 들어 다음 흐름의 기반으로 사용할 수 있다.

```text
아이템 획득
→ Stack 시도
→ 빈 슬롯 확인
→ 공간 부족
→ 두고 간다 / 교체 / 취소
```

또한 소비 아이템 사용, 장비 장착, 중요 아이템 제한 등은
`ItemCategoryRules`를 공통 기준으로 연결할 수 있다.

---

## 14. 검증 메모

GitHub `main`의 최신 상태를 기준으로 확인했다.

- 최신 커밋: `7efa33a936e0c35aab60d23688ee18288aaac607`
- 최신 커밋 메시지: `a`
- 비교 기준: `55e308a936f93f23aa64c45d4889deede251f79a`
- 90일차 기준보다 1개 커밋 앞섬
- 변경 파일: 10개
- `ItemCategory`가 Domain에 존재함
- 잘못된 `Data/ItemCategory.cs`는 최신 트리에 존재하지 않음
- `ItemDefinition`이 `ProjectDelta.Domain`의 `ItemCategory`를 사용함
- `ItemCategoryRules`가 Data 어셈블리를 역참조하지 않음
- `PlayerInventoryHudController`가 선택 아이템의 분류명을 표시함
- `ItemCategoryValidationMenu` 존재 확인
- `ItemCategoryRulesTests` 존재 확인
- 최신 커밋에 연결된 GitHub commit status check 결과는 없음

현재 환경에서는 Unity Editor를 실행할 수 없으므로
**실제 Unity 컴파일 및 EditMode Test Runner 통과 여부는 검증하지 못했다.**

GitHub 최신 소스의 정적 구조를 확인한 범위에서는
91일차 종료를 막는 새로운 소스 구조 문제는 발견하지 못했다.
