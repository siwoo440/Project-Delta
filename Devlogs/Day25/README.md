# Project Delta - 25일차 개발일지

## 개발 주제

**상자·비밀벽 상호작용 최소 골격**

`RoomContentType`(19일차)에 정의만 되어있던 5종 중 `Stairs`(22일차)에 이어 `Chest`, `SecretWall`을 실제로 상호작용 가능하게 만들었다. 인벤토리(6.4절)가 정식으로 생기기 전이라 아이템은 문자열 이름 수준의 자리표시자로 다뤘다.

---

## 개발 목표

- 상자: 계단처럼 벽으로 막힌 칸 + 문처럼 정면에서 F로 열기 + 인벤토리·상자 두 패널을 마우스로 클릭해 아이템 이동
- 비밀벽: 겉보기엔 벽이지만 실제로는 통로가 막혀있지 않아 WASD로 그냥 통과 가능 (구분용으로 회색 표시)
- `InventoryRunState`(5일차부터 빈 껍데기)를 실제 슬롯 목록으로 최초 구현

---

## 구현 내용

### 1. InventoryRunState — 빈 껍데기를 최소 슬롯 목록으로

```text
InventoryRunState (Domain, 25일차)
├─ InventoryItemStack — ItemId/DisplayName 문자열 쌍 (정식 아이템 정의 전 자리표시자)
└─ Items / Add() — 슬롯 목록 조회·추가
```

### 2. 상자 — 계단과 같은 "벽으로 막기", 문과 같은 "정면 상호작용"

```text
RoomDefinition_TestRoom_A — 상자 칸(그리드 1,-1) 사방을 벽 통로로 봉쇄 (22일차 계단과 동일 방식)
↓
ChestContentMarker(신규, Presentation) — 상자 하나의 실제 아이템 목록과 "몇 개 남았는지" 보유
    RoomContentMarker(빈 자리 표시 전용, 19일차 원칙)와 분리해서 같은 오브젝트에 나란히 부착
↓
ChestInteractionController(신규) — 정면 감지 + F (계단과 같은 패턴)
    F → 좌측 "인벤토리"(읽기 전용) / 우측 "상자"(클릭 가능) 두 패널 표시
    상자 쪽 아이템 클릭 → 상자에서 제거 + 인벤토리에 추가
    Esc → 패널 닫기
```

패널이 열려있는 동안 `PlayerGridMovementController.IsInputLocked`를 켜서 WASD 이동을 막는다 (기존 `isMoving` 잠금과 같은 자리에 조건 추가).

### 3. 비밀벽 — 코드 변경 없이 "모양만 벽"

이동 판정이 물리 충돌이 아니라 그리드 논리(`RoomGridLayout.CanPass`)만 보는 이 프로젝트의 특성을 그대로 활용했다. 벽처럼 보이는 Cube 메시 하나만 놓고, 그 자리의 통로 데이터는 아예 건드리지 않았다 — 그 결과 시각적으로는 벽이지만 WASD로 그냥 통과된다. 구분용으로 `SecretWall_Gray.mat`(기존 `Door_Gray`보다 살짝 밝고 푸른 회색)을 새로 만들어 입혔다.

### 4. 마우스 커서 문제 수정

상자 패널을 실제로 열어보니 `PlayerLookController`가 항상 커서를 잠그고 숨기고 있어서 패널을 클릭할 수 없었다. `PlayerLookController.SetCursorFreeForUi(bool)`을 추가해서, 패널이 열리면 커서를 풀고 마우스로 인한 시점 회전도 같이 멈추도록(안 그러면 아이템을 클릭하려고 마우스를 움직일 때 카메라가 같이 돔) 했다.

---

## 적용 중 발견된 문제 및 수정

**마우스 커서가 잠겨 상자 패널을 조작할 수 없었음.** 14~22일차 동안 만든 상호작용은 전부 키보드(F/WASD/Esc)만 써서 커서 잠금이 문제가 된 적이 없었는데, 오늘 처음으로 마우스 클릭이 필요한 UI를 만들면서 드러났다. `PlayerLookController`에 UI 전용 커서 해제 메서드를 추가하고 `ChestInteractionController`가 패널을 열고 닫을 때 호출하도록 연결해서 해결했다.

---

## 현재 25일차 전체 흐름

```text
InventoryRunState를 실제 슬롯 목록으로 구현
↓
RoomDefinition_TestRoom_A에 상자 칸 벽 봉쇄 추가
↓
ChestContentMarker + ChestInteractionController로 상자 상호작용(정면 F, 클릭식 아이템 이동) 구현
↓
PlayerGridMovementController.IsInputLocked로 패널 열린 동안 이동 차단
↓
(패널 클릭이 안 되는 문제 발견) PlayerLookController.SetCursorFreeForUi로 커서 잠금 해제 연동
↓
비밀벽은 회색 Cube 시각 오브젝트만 추가, 통로 데이터는 그대로 둬서 통과 가능하게 유지
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Presentation/ChestContentMarker.cs
Assets/ProjectDelta/Scripts/Presentation/ChestContentMarker.cs.meta
Assets/ProjectDelta/Scripts/Presentation/ChestInteractionController.cs
Assets/ProjectDelta/Scripts/Presentation/ChestInteractionController.cs.meta
Assets/ProjectDelta/Materials/Chest_Gold.mat
Assets/ProjectDelta/Materials/Chest_Gold.mat.meta
Assets/ProjectDelta/Materials/SecretWall_Gray.mat
Assets/ProjectDelta/Materials/SecretWall_Gray.mat.meta
Devlogs/Day25/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Domain/RunSubStates.cs (InventoryRunState 실제 구현)
Assets/ProjectDelta/Scripts/Presentation/PlayerGridMovementController.cs (IsInputLocked 추가)
Assets/ProjectDelta/Scripts/Presentation/PlayerLookController.cs (SetCursorFreeForUi 추가)
Assets/ProjectDelta/Data/Rooms/RoomDefinition_TestRoom_A.asset (상자 칸 벽 통로 추가)
Assets/ProjectDelta/Scenes/DungeonScene.unity (상자·비밀벽 배치, Player에 ChestInteractionController 부착)
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

25일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- TestRoom_A의 상자 칸(1,-1)에 걸어 들어갈 수 없음 (벽과 동일하게 막힘)
- 상자 정면에서 F → 좌우 패널이 뜨고, 마우스 커서가 보이며 클릭이 가능함
- 상자 쪽 아이템 클릭 시 인벤토리 쪽에 나타나고 상자에서 사라짐
- Esc로 패널을 닫으면 이동과 시점 회전이 정상적으로 복귀됨
- 비밀벽(SecretWall_Gray 표시) 앞에서 WASD로 그냥 통과됨

**참고**: 오늘 만든 상자 상호작용은 인벤토리 정식 시스템(6.4절) 없이 이름 문자열 수준으로만 동작하는 자리표시자다. 비밀벽도 "조사/발견 판정" 없이 처음부터 통과 가능한 최소 버전이다.

---

## 다음 개발 방향

26일차에는 **던전 진행 상태 저장 연동**을 진행한다. `RoomInstance` ↔ `RunData.RoomRunState` 매핑을 완성하고 `SaveService.WriteRun`/`ReadRun`을 실제로 연동해서, 저장 DTO가 이미 기대하고 있던 필드들과 런타임 모델 사이의 간극을 해소한다.
