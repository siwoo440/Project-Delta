# Project Delta - 13일차 개발일지

## 개발 주제

**마우스 자유 시점 회전과 수평 시선 방향 상태 구현**

12일차에서 구현한 시점 기준 WASD 한 칸 이동 구조에 실제 마우스 자유 시점을 연결했다. 이번 일차에서는 Player가 수평 Yaw 회전을 담당하고 Main Camera가 수직 Pitch 회전을 담당하도록 역할을 분리했으며, 현재 시선 방향을 기존 그리드 이동 시스템과 자연스럽게 연결할 수 있는 기반을 구성했다.

---

## 개발 목표

- `Exploration` Input Action Map에 `Look` 액션 추가
- Mouse Delta 기반 자유 시점 입력 연결
- Player의 Yaw와 Main Camera의 Pitch 분리
- 좌우 360° 자유 회전 구현
- 상하 시점 `-80° ~ +80°` 제한
- 플레이 중 마우스 커서 잠금 및 숨김
- 현재 Yaw/Pitch와 4방향 시선 상태 공개
- 기존 12일차 WASD 그리드 이동과 시점 방향 연결
- 위치 데이터와 시점 데이터를 독립적으로 유지

---

## 구현 내용

### 1. `Look` Input Action 추가

기존 `Exploration` Action Map에 다음 입력을 추가했다.

```text
Look
└─ <Mouse>/delta
```

`Look`은 `PassThrough` 타입으로 구성해 매 프레임 마우스 이동량을 `Vector2`로 읽을 수 있도록 했다.

기존 이동 액션은 그대로 유지된다.

```text
MoveForward  → W
MoveBackward → S
MoveLeft     → A
MoveRight    → D
Look         → Mouse Delta
```

---

### 2. `PlayerLookController` 추가

`Assets/ProjectDelta/Scripts/Presentation/PlayerLookController.cs`를 추가했다.

Player와 Main Camera의 회전 책임을 다음처럼 분리했다.

```text
Player
→ Yaw
→ 좌우 자유 회전

Main Camera
→ Pitch
→ 위아래 시점 회전
```

마우스 좌우 입력은 Player의 Y축 회전에 반영하고, 마우스 상하 입력은 Main Camera의 로컬 X축 회전에 반영한다.

이 구조를 사용하면 카메라를 위아래로 움직일 때 Player 전체가 기울어지지 않는다.

---

### 3. 상하 시점 제한

Pitch 값은 다음 범위로 제한했다.

```text
Min Pitch : -80°
Max Pitch : +80°
```

따라서 마우스를 계속 위나 아래로 움직여도 카메라가 뒤집히지 않는다.

초기 감도는 다음 값으로 설정했다.

```text
Mouse Sensitivity : 0.08
```

감도 값은 이후 설정 메뉴와 연결할 수 있도록 직렬화 필드로 유지했다.

---

### 4. 기존 카메라 회전값 정리

초기화 시 Player와 Main Camera에 남아 있을 수 있는 회전값을 분리해 정리한다.

```text
Player의 기존 Yaw
+
Main Camera의 기존 Local Yaw
↓
Player Yaw로 통합

Main Camera
↓
Pitch만 유지
```

이후에는 Player가 수평 회전만, Main Camera가 수직 회전만 담당한다.

---

### 5. 현재 시선 상태 공개

`PlayerLookController`에서 다음 값을 읽을 수 있도록 했다.

```text
YawDegrees
PitchDegrees
FacingDirection
```

`FacingDirection`은 기존 `GridMovement.GetFacingFromYaw()`를 사용해 현재 자유 Yaw를 가장 가까운 4방향으로 변환한다.

```text
North
East
South
West
```

이 값은 이후 미니맵의 플레이어 방향 화살표, 전방 상호작용, 방향 판정 등에 활용할 수 있다.

---

### 6. 12일차 이동 시스템과 연결

12일차의 `PlayerGridMovementController`는 이미 `viewTransform.eulerAngles.y`를 기준으로 이동 방향을 계산한다.

따라서 13일차에서 Player와 Main Camera의 실제 시점이 마우스 입력으로 회전하면 기존 이동 시스템도 변경된 시점 방향을 그대로 사용한다.

예시:

```text
Yaw 10°
→ North
→ W 입력 시 Z +1

Yaw 90°
→ East
→ W 입력 시 X +1
```

시점 자체는 자유롭게 회전하지만 실제 이동은 계속 4방향 한 칸 이동 구조를 유지한다.

---

### 7. 마우스 커서 잠금

탐험 시점 입력이 활성화되면 커서를 다음 상태로 변경한다.

```text
Cursor Lock State → Locked
Cursor Visible    → false
```

컴포넌트가 비활성화되면 커서를 다시 해제하고 표시한다.

향후 ESC 메뉴와 설정 UI가 추가되면 이 커서 제어를 메뉴 상태와 연결할 수 있다.

---

### 8. DungeonScene 연결

`DungeonScene`의 Player에 `PlayerLookController`를 추가했다.

현재 구조는 다음과 같다.

```text
===Dungeon===
└─ Player
   ├─ PlayerGridMovementController
   ├─ PlayerLookController
   └─ Main Camera
```

Inspector 기준 연결값은 다음과 같다.

```text
Input Actions      → Assets/InputSystem_Actions.inputactions
Camera Transform   → Main Camera
Mouse Sensitivity  → 0.08
Min Pitch          → -80
Max Pitch          → 80
```

---

## 현재 13일차 전체 흐름

```text
Exploration에 Look 액션 추가
↓
Mouse Delta 입력 수신
↓
Player Yaw와 Main Camera Pitch 분리
↓
좌우 360° 자유 회전
↓
상하 Pitch -80° ~ +80° 제한
↓
현재 Yaw를 4방향 FacingDirection으로 변환
↓
12일차 WASD 이동이 현재 시점 방향을 그대로 사용
↓
플레이 중 커서 잠금 및 숨김
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Presentation/PlayerLookController.cs
Assets/ProjectDelta/Scripts/Presentation/PlayerLookController.cs.meta
Assets/Editor.meta
Devlogs/Day13/README.md
```

`Assets/Editor.meta`는 13일차 자동 설정 스크립트를 넣기 위해 `Assets/Editor` 폴더가 생성되면서 Unity가 만든 폴더 메타 파일이다. 자동 설정 스크립트 본체는 작업 완료 후 삭제되어 최종 커밋에는 남지 않았다.

---

## 수정 파일

```text
Assets/InputSystem_Actions.inputactions
Assets/ProjectDelta/Scenes/DungeonScene.unity
Project-Delta.slnx
```

`Project-Delta.slnx` 변경은 프로젝트 목록의 순서가 Unity/IDE에 의해 다시 정렬된 수준이며 기능 변경은 없다.

---

## 삭제 파일

없음.

13일차 자동 설정에 사용한 임시 `Day13ProjectSetup.cs`는 설정 완료 후 자동 삭제되어 최종 프로젝트 파일에는 남지 않는다.

---

## 최종 확인 항목

13일차 완료 기준은 다음과 같다.

- `Exploration` Action Map에 `Look` 액션이 존재
- `Look`이 `<Mouse>/delta`에 연결됨
- `PlayerLookController`가 DungeonScene의 Player에 연결됨
- Main Camera가 `cameraTransform`으로 연결됨
- Player가 수평 Yaw 회전을 담당함
- Main Camera가 수직 Pitch 회전을 담당함
- 좌우 360° 자유 회전 가능
- Pitch가 `-80° ~ +80°` 범위로 제한됨
- 플레이 중 커서가 잠기고 숨겨짐
- `YawDegrees`, `PitchDegrees`, `FacingDirection`을 읽을 수 있음
- 시점 회전만으로 `CurrentGridPosition`은 변경되지 않음
- W/A/S/D 입력 시 현재 시점 기준 한 칸 이동 구조가 유지됨
- 기존 EditMode 테스트 11종과 PlayMode 테스트 1종의 회귀 여부 확인 필요

GitHub 변경 내역 기준으로 구조적인 충돌은 확인되지 않았다. Unity Console의 최종 컴파일 결과와 실제 Test Runner 통과 여부는 로컬 Unity Editor에서 최종 확인한다.

---

## 다음 개발 방향

14일차에는 **벽·문·통과 가능 여부 데이터와 이동 판정 구조**를 구현한다.

예정 흐름:

```text
테스트 방의 각 그리드 칸 이동 가능 정보 정의
↓
벽이 있는 방향의 이동 차단
↓
문이 있는 방향과 일반 벽 구분
↓
현재 칸과 목표 칸 사이 통과 가능 여부 검사
↓
PlayerGridMovementController의 단순 GridBounds 검사와 연결
↓
이후 문 상호작용과 방 전환 시스템이 사용할 기반 확보
```

13일차까지는 시점과 입력을 완성하고, 14일차부터 실제 던전 구조물이 플레이어의 이동 가능 여부에 영향을 주도록 확장한다.
