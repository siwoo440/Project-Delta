# Project Delta — 89일차 개발 일지

- 날짜: 2026-08-26
- 기준 브랜치: `main`
- 최신 커밋: `312df1b2d4661e7ffb9642de98402594e90ce034`
- 현재 커밋 메시지: `a`
- 비교 기준: `bc5a97774f494e220f631b415a93dfcceaa17fbb` (88일차)

---

## 1. 오늘의 목표

89일차는 기존의 단순 아이템 ID 목록을 실제 **10칸 슬롯 인벤토리**로 확장하고,
탐험과 전투에서 계속 확인할 수 있는 플레이어 정보/인벤토리 HUD의 기반을 만드는 작업을 진행했다.

핵심 목표는 다음과 같다.

- 기본 10칸 슬롯 인벤토리 구현
- 아이템 획득 시 첫 빈 슬롯 사용
- 슬롯 제거·이동·교환
- 인벤토리 슬롯 위치 저장/복원
- 향후 영구 강화와 가방을 위한 슬롯 확장 구조
- 아이템 아이콘·설명 데이터 확장
- 5×2 인벤토리 HUD 구성
- 탐험/전투에서 플레이어 HP·MP·정력 정보 유지
- 선택 아이템 정보 표시 기반 구성

---

## 2. 인벤토리 데이터 구조 확장

기존 `InventoryRunState`는 아이템을 단순 목록 형태로 보관하고 있었지만,
89일차부터 실제 슬롯 위치를 유지하는 구조로 확장했다.

### 기본 규칙

- 기본 슬롯 수: 10칸
- 슬롯 번호: 1~10
- 내부 인덱스: 0~9
- 아이템 획득 시 첫 번째 빈 슬롯에 배치
- 아이템 제거 시 해당 슬롯만 비움
- 빈 슬롯으로 이동 가능
- 아이템이 존재하는 슬롯끼리는 교환 가능

`InventorySlotState`를 추가하여 각 슬롯이 다음 상태를 가지도록 구성했다.

- ItemId
- DisplayName
- Quantity
- IsEmpty

기존 상자 및 아이템 획득 코드와의 호환성을 위해
기존 `InventoryItemStack`, `Items`, `Add()` 흐름은 유지하면서
실제 저장 기준은 슬롯 구조가 되도록 변경했다.

---

## 3. 인벤토리 최대 슬롯 계산

향후 시스템을 위해 기본 슬롯 수를 단순 하드코딩하지 않고
확장 수치를 별도로 합산하도록 준비했다.

최종 슬롯 수는 다음 구조를 사용한다.

`기본 10칸 + 영구 강화 슬롯 보너스 + 장착 가방 슬롯 보너스`

현재 89일차에서는 두 보너스가 기본적으로 0이므로 10칸을 사용하며,
이후 영구 성장 및 가방 장비 시스템에서 그대로 확장할 수 있다.

---

## 4. 저장 / 불러오기

`RunData.Inventory`에 슬롯 저장 데이터를 추가했다.

저장되는 주요 정보:

- 슬롯별 ItemId
- 표시 이름
- 수량
- 빈 슬롯의 위치
- 영구 슬롯 보너스
- 가방 슬롯 보너스

저장 시 현재 슬롯 순서를 그대로 기록하고,
불러오기 시 동일한 슬롯 위치로 복원한다.

89일차 이전 저장 데이터의 `InventoryItemIds`도 계속 읽을 수 있도록
호환 복원 경로를 유지했다.

---

## 5. 아이템 데이터 확장

`ItemDefinition`에 HUD 표시를 위한 정보를 추가했다.

- `Icon`
- `Description`

이를 통해 이후 각 아이템 ScriptableObject에 실제 Sprite와 설명을 연결하면
인벤토리 슬롯과 선택 아이템 정보 영역에서 사용할 수 있다.

---

## 6. 플레이어 / 인벤토리 HUD

DungeonScene에 플레이어 정보와 인벤토리를 확인할 수 있는 HUD를 구성했다.

### 플레이어 정보

탐험과 전투 모두에서 현재 플레이어 상태를 읽을 수 있는
`PersistentPlayerVitalsController`를 추가했다.

표시 대상:

- 레벨
- HP
- MP
- 정력
- ATK
- DEF
- SPD

정력 표기는 기존 `SP`에서 `정력`으로 통일했다.

### 인벤토리

인벤토리는 5×2 형태로 총 10칸을 표시한다.

배치 순서:

`1 2 3 4 5`
`6 7 8 9 10`

각 슬롯에는 아이템 아이콘을 표시할 수 있으며,
오른쪽 아래에는 슬롯 번호가 표시된다.

아이템 슬롯을 선택하면 선택 아이템의 아이콘·이름·설명을 표시할 수 있는
정보 영역도 추가했다.

---

## 7. 전투 UI와 상시 HUD 분리

플레이어의 기본 정보와 인벤토리는 전투 여부와 상관없이 사용할 수 있도록
상시 HUD 구조를 추가했다.

반대로 전투 행동 버튼은 전투가 활성화된 경우에만 표시하도록
상태 판정을 분리했다.

전투 중에는 BattleContext의 현재 HP·MP·정력을 표시하고,
탐험 중에는 RunContext의 값을 사용하도록 구성했다.

---

## 8. 전투 전환 경고 정리

88일차 `BattleTransitionController`의 `HoldBlack()`에서
`BlackHoldSeconds`가 0.10초의 상수임에도 0 이하인지 검사하던 분기로 인해
CS0162 `Unreachable code detected` 경고가 발생했다.

불가능한 조건 분기를 제거하고 다음과 같이 단순화했다.

- 검은 화면 유지 시간: 0.10초
- `WaitForSecondsRealtime`을 그대로 사용
- 전환 동작 자체는 변경하지 않음

---

## 9. 테스트 추가

### RunInventoryStateTests

다음 항목을 검증하는 EditMode 테스트를 추가했다.

- 새 인벤토리는 10개의 빈 슬롯을 가진다.
- 아이템은 첫 빈 슬롯에 들어간다.
- 제거 후 다른 슬롯 위치가 당겨지지 않는다.
- 빈 슬롯 이동이 가능하다.
- 차 있는 슬롯끼리 교환 가능하다.
- 영구/가방 보너스가 최대 슬롯 수에 합산된다.
- 저장 후 동일 슬롯 위치로 복원된다.

### PersistentPlayerVitalsControllerTests

다음 HUD 관련 순수 로직 테스트를 추가했다.

- 행동 버튼은 전투 중에만 표시
- 정력 표기는 한글 `정력` 사용
- 자원 바 비율은 0~1 범위로 제한

---

## 10. 변경 파일

88일차 커밋과 비교해 총 15개 파일이 변경되었다.

### 수정

- `Assets/ProjectDelta/Scenes/DungeonScene.unity`
- `Assets/ProjectDelta/Scripts/Data/DungeonSaveMapper.cs`
- `Assets/ProjectDelta/Scripts/Data/ItemDefinition.cs`
- `Assets/ProjectDelta/Scripts/Data/RunData.cs`
- `Assets/ProjectDelta/Scripts/Domain/RunSubStates.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleTransitionController.cs`

### 추가

- `Assets/ProjectDelta/Scripts/Presentation/PersistentPlayerVitalsController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/PersistentPlayerVitalsController.cs.meta`
- `Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/PlayerInventoryHudController.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/PersistentPlayerVitalsControllerTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/PersistentPlayerVitalsControllerTests.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/RunInventoryStateTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/RunInventoryStateTests.cs.meta`

---

## 11. 현재 상태 및 남은 정리

89일차의 시스템 기반은 다음 일차에서 사용할 수 있는 형태로 구성되어 있다.

- 10칸 슬롯 인벤토리
- 슬롯 위치 유지
- 아이템 획득·제거·이동·교환
- 슬롯 저장/복원
- 향후 슬롯 확장 기반
- 아이템 Icon/Description 필드
- 5×2 인벤토리 HUD
- 탐험/전투 플레이어 자원 표시
- 선택 아이템 정보 표시 기반

다만 HUD의 세부 위치, 크기, 초상화와 버튼 배치 등 **시각적 마감은 추후 별도로 수정하기로 했다.**
따라서 89일차에서는 시스템 연결과 기본 UI 구조를 기준으로 마무리하고 다음 개발 일정으로 넘어간다.

---

## 12. 검증 메모

GitHub 최신 커밋 기준으로 소스와 변경 내역을 정적으로 확인했다.

- 최신 커밋: `312df1b2d4661e7ffb9642de98402594e90ce034`
- 88일차 대비 1개 커밋 앞섬
- 변경 파일 15개
- `BattleTransitionController`의 CS0162 원인 분기 제거 확인
- 인벤토리 저장/복원 로직 존재 확인
- EditMode 테스트 코드 존재 확인
- GitHub Actions workflow run 없음
- Commit status check 없음

현재 환경에서는 Unity Editor를 실행할 수 없어
**실제 Unity 컴파일 및 EditMode Test Runner 통과 여부는 검증하지 못했다.**
