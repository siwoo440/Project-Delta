# Project Delta - 45일차 개발일지

## 개발 목표

44일차에 구현한 Encounter UI와 전투·회피 Command 구조를 실제 플레이 흐름에서 안전하게 사용할 수 있도록 행동 선택 조건, 중복 입력 방지, 탐험 이동 잠금 복구를 보강한다.

추가로 기존에는 플레이어와 몬스터가 정확히 같은 GridPosition에 겹쳐야 Encounter가 발생하던 구조를 같은 방 안의 몬스터 주변 8방향 1칸 범위까지 확장한다.

또한 이후 실제 몬스터 일러스트를 사용할 수 있도록 3D 판정 Root와 2D 외형을 분리하고, Sprite가 플레이어 카메라를 향하는 Billboard 표시 기반을 준비한다.

이번 일차에서는 실제 몬스터 이미지 리소스는 추가하지 않는다. 이미지가 없는 동안에는 기존 Capsule 외형을 fallback으로 유지한다.

---

## 구현 내용

### 1. Encounter 행동 선택 가능 여부 구조

`EncounterActionAvailability`를 추가해 행동 선택 가능 여부와 선택 불가 사유를 함께 전달하도록 구성했다.

보관 정보:

```text
CanSelect
Reason
```

이제 UI는 단순히 Command 호출 성공 여부만 확인하는 것이 아니라 현재 Encounter에서 행동을 선택할 수 있는지를 먼저 판단할 수 있다.

### 2. EncounterActionSelectionGate 추가

한 Encounter에서 전투 또는 회피 행동이 한 번만 확정되도록 `EncounterActionSelectionGate`를 추가했다.

기본 흐름:

```text
Active + Context 존재 + 미선택
→ 행동 선택 가능

첫 Command Accept
→ 선택 확정

두 번째 Command 입력
→ 선택 거부
→ 이미 행동을 선택했습니다.
```

선택한 Command ID도 보관한다.

```text
HasSelection
SelectedCommandId
```

Encounter 종료 또는 새 Encounter 시작 시 Gate를 Reset해 다음 Encounter에서는 다시 행동을 선택할 수 있다.

### 3. 전투·회피 버튼 중복 입력 방지

`ExplorationMonsterEncounterController`가 행동 실행 전에 `EncounterActionSelectionGate`를 확인하도록 변경했다.

정상적인 첫 행동만 확정하며, 이미 선택된 상태에서는 추가 Command 실행을 막는다.

```text
전투 클릭
→ Battle Command 실행
→ 선택 확정

이후 전투 클릭
→ 거부

이후 회피 클릭
→ 거부
```

실제 전투 계산이나 회피 판정은 아직 실행하지 않는다.

### 4. 행동 버튼 활성 조건과 선택 불가 사유 UI

`EncounterPanelController`에서 현재 행동 선택 가능 여부에 따라 전투·회피 버튼의 `interactable` 상태를 갱신하도록 변경했다.

```text
행동 선택 가능
→ BattleButton 활성
→ EscapeButton 활성

행동 선택 완료 또는 선택 불가
→ BattleButton 비활성
→ EscapeButton 비활성
```

기존 `ResultText`를 재사용해 Command 결과와 선택 불가 사유를 표시한다.

예시:

```text
선택 : 전투 선택 / Target MON_TEST
이미 행동을 선택했습니다.
```

새로운 UI 오브젝트는 추가하지 않았다.

### 5. 탐험 이동 잠금 안전화

Encounter가 탐험 이동 잠금을 직접 소유하는지 기록하도록 보강했다.

Encounter 시작 전의 `IsInputLocked` 값을 저장하고, Encounter 종료 시 무조건 `false`로 만드는 대신 이전 상태를 복원한다.

```text
Encounter 시작 전
IsInputLocked = false
↓
Encounter 시작
IsInputLocked = true
↓
Encounter 종료
IsInputLocked = false 복원
```

다른 UI나 시스템이 이미 이동을 잠근 상태였다면 해당 잠금 상태를 유지할 수 있도록 구성했다.

### 6. 8방향 1칸 Encounter 포착 규칙

`EncounterRangeRule`을 추가했다.

기존 판정:

```text
플레이어 GridPosition == 몬스터 GridPosition
→ Encounter
```

변경 판정:

```text
같은 RoomId
+
X축 거리 <= 1
+
Z축 거리 <= 1
→ Encounter
```

몬스터 기준 포착 범위:

```text
X X X
X M X
X X X
```

`M`은 몬스터이며 주변 상·하·좌·우와 대각선 4방향을 포함한다.

기존처럼 같은 칸에 겹치는 경우도 계속 Encounter가 발생한다.

2칸 이상 떨어진 위치 또는 다른 방에서는 Encounter가 발생하지 않는다.

현재 단계에서는 벽이나 시야 차단 여부를 검사하지 않는다. 이후 몬스터 탐지·시야 시스템에서 확장할 수 있도록 거리 규칙을 별도 클래스로 분리했다.

### 7. ExplorationEncounterSession 범위 판정 변경

`ExplorationEncounterSession.TryBegin()`의 기존 정확한 위치 일치 조건을 `EncounterRangeRule` 호출로 교체했다.

상태 머신 자체는 유지한다.

```text
Idle
↓
Starting
↓
Active
↓
Resolving
↓
Finished
↓
Idle
```

따라서 Encounter 발생 거리만 확장되고 43~44일차에서 구현한 생명주기 구조는 그대로 사용한다.

### 8. 2D 몬스터 Billboard 외형 기반

이후 몬스터를 3D 모델 대신 일러스트 이미지로 표시할 수 있도록 `MonsterBillboardView`를 추가했다.

몬스터 구조는 논리 Root와 외형 View를 분리한다.

```text
Monster Root
├─ ExplorationMonsterMarker
├─ 기존 Trigger / Grid / Encounter 판정
└─ MonsterBillboardVisual
   ├─ SpriteRenderer
   └─ MonsterBillboardView
```

Billboard는 `Camera.main`을 대상으로 하며 Y축 방향만 회전한다.

따라서 카메라의 높낮이에 따라 이미지가 눕지 않고, 수평 방향으로 플레이어 카메라를 바라보는 형태를 유지한다.

### 9. MonsterDefinitionId 기반 Sprite 자동 로딩

몬스터 외형 Sprite는 다음 경로에서 `MonsterDefinitionId`와 같은 이름으로 찾는다.

```text
Assets/ProjectDelta/Resources/MonsterSprites/
```

예시:

```text
MonsterDefinitionId = MON_TEST
↓
Assets/ProjectDelta/Resources/MonsterSprites/MON_TEST.png
```

이번 일차에는 실제 이미지 파일을 넣지 않았다.

다음 일차 이후 실제 일러스트를 확보한 뒤 ID와 같은 파일 이름으로 추가해 검증할 예정이다.

### 10. 이미지가 없을 때 Capsule fallback 유지

현재 `MonsterSprites` 폴더에 이미지가 없더라도 몬스터가 보이지 않는 상태가 되지 않도록 fallback을 유지한다.

```text
Sprite 발견
→ SpriteRenderer 사용
→ 기존 Capsule Renderer 숨김

Sprite 없음
→ Billboard Sprite 없음
→ 기존 Capsule Renderer 유지
```

따라서 현재 단계에서는 기존 Capsule 테스트 몬스터로 계속 탐험과 Encounter를 검증할 수 있다.

### 11. MonsterSpriteImporter 추가

이후 `MonsterSprites` 폴더에 이미지 파일을 넣었을 때 자동으로 Sprite 리소스로 가져오도록 Editor용 `MonsterSpriteImporter`를 추가했다.

자동 설정:

```text
Texture Type = Sprite
Sprite Mode = Single
Alpha Is Transparency = 활성
Mip Maps = 비활성
```

실제 이미지 추가 작업은 다음 일차 이후 진행한다.

### 12. EditMode 테스트 추가·확장

45일차 기능 검증을 위해 다음 테스트를 추가했다.

#### EncounterActionSelectionGateTests

- Active + Context 상태 행동 선택 가능
- Active가 아닌 상태 선택 차단
- Context 누락 상태 선택 차단
- 첫 행동 선택 성공
- 두 번째 행동 선택 거부
- Reset 후 다음 Encounter에서 다시 선택 가능

#### EncounterRangeRuleTests

- 같은 위치 허용
- 상·하·좌·우 및 대각선 8방향 허용
- 1칸 범위 밖 거부
- 잘못된 음수 범위 거부

#### ExplorationEncounterSessionTests

기존 같은 위치 테스트에 더해 대각선 1칸 위치에서 Encounter가 시작되는 테스트와 2칸 거리에서 시작되지 않는 테스트를 반영했다.

#### Billboard 관련 테스트

- MonsterDefinitionId 기반 Resources 경로 생성
- Billboard가 카메라 높이를 무시하고 Y축만 회전하는지 확인
- 같은 수평 위치일 때 기존 회전값 유지
- Sprite가 없을 때 Billboard 자식은 준비하지만 Capsule fallback Renderer를 유지하는지 확인

---

## 45일차 전체 동작 흐름

```text
플레이어 이동 완료
↓
현재 방의 몬스터 조회
↓
같은 RoomId 확인
↓
몬스터 주변 8방향 1칸 범위 확인
↓
Encounter Starting
↓
탐험 이동 잠금
↓
Active
↓
EncounterPanel 표시
↓
행동 선택 가능 조건 확인
↓
전투 / 회피 활성
↓
첫 행동 선택
↓
Command 실행
↓
행동 선택 Gate 확정
↓
전투 / 회피 비활성
↓
중복 입력 차단
↓
TestEnd
↓
Resolving → Finished → Idle
↓
기존 탐험 입력 잠금 상태 복원
↓
다음 Encounter에서 행동 선택 상태 Reset
```

몬스터 외형은 현재 실제 이미지가 없으므로 다음처럼 동작한다.

```text
MonsterDefinitionId로 Sprite 검색
↓
현재 이미지 없음
↓
기존 Capsule 외형 유지
↓
다음 일차 이후 이미지 추가 시 Billboard Sprite 표시 가능
```

---

## 이번 일차에서 제외한 내용

다음 내용은 이번 45일차에서 구현하지 않는다.

- 실제 전투 턴 진행
- 플레이어·몬스터 데미지 계산
- 실제 회피 성공률 판정
- 전투 승리·패배 결과 처리
- 몬스터 처치 결과의 방 상태 반영
- 저장 데이터에 Encounter 결과 반영
- 벽을 고려한 몬스터 감지
- Line of Sight 기반 시야 판정
- 실제 몬스터 이동 AI
- 실제 몬스터 일러스트 이미지 리소스
- 일러스트별 개별 크기·위치 밸런싱
- 피격 플래시·애니메이션 등 외형 연출

실제 몬스터 일러스트 리소스는 다음 일차 이후 별도로 추가해 화면 표시를 검증한다.

---

## 변경 파일

44일차 완료 커밋과 비교해 최신 커밋에서 총 27개 파일이 추가·수정·삭제되었다.

### 주요 생성

- `Assets/ProjectDelta/Scripts/Application/EncounterActionAvailability.cs`
- `Assets/ProjectDelta/Scripts/Application/EncounterActionSelectionGate.cs`
- `Assets/ProjectDelta/Scripts/Application/EncounterRangeRule.cs`
- `Assets/ProjectDelta/Scripts/Editor/MonsterSpriteImporter.cs`
- `Assets/ProjectDelta/Scripts/Presentation/MonsterBillboardView.cs`
- `Assets/ProjectDelta/Tests/EditMode/EncounterActionSelectionGateTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/EncounterRangeRuleTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/ExplorationMonsterMarkerBillboardTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/MonsterBillboardViewTests.cs`
- `Assets/ProjectDelta/Resources/MonsterSprites/` 리소스 폴더

### 주요 수정

- `Assets/ProjectDelta/Scripts/Application/ExplorationEncounterSession.cs`
- `Assets/ProjectDelta/Scripts/Presentation/EncounterPanelController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterMarker.cs`
- `Assets/ProjectDelta/Tests/EditMode/ExplorationEncounterSessionTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/ProjectDelta.Tests.EditMode.asmdef`

### 삭제

- `_Apply_Day44_AssemblyFix.bat`

Day44 임시 Assembly Fix 배치 파일은 최신 구조에서 제거되었다.

---

## 최신 커밋 확인

확인한 최신 커밋:

- SHA: `e2afc9ece1f2275cadbf8264db5ba63256f86e0d`
- 현재 커밋 메시지: `a`
- 이전 커밋: `37aec519fdc3dffb77de3354fb1573bf4cedb093`
- 이전 커밋 메시지: `44일차 : 인카운터 UI 및 전투·회피 공통 Command 구조 구현`

최신 커밋은 44일차 완료 커밋보다 정확히 1개 앞선 상태다.

저장소 정적 검토에서 다음 내용을 확인했다.

- Encounter 행동 선택 Gate가 추가되어 최초 행동만 확정하도록 구성됨
- Encounter 선택 불가 사유를 UI에 표시하도록 연결됨
- Battle / Escape 버튼의 interactable 상태 제어가 추가됨
- Encounter 이동 잠금 상태를 시작 전 값으로 복원하도록 보강됨
- 같은 방의 몬스터 주변 8방향 1칸 Encounter 판정이 적용됨
- 몬스터 Billboard View와 Resources 기반 Sprite 로딩 구조가 추가됨
- 실제 Sprite가 없을 때 기존 Capsule Renderer를 유지하는 fallback이 구현됨
- 실제 몬스터 이미지 파일은 아직 추가되지 않음

GitHub Combined Status에는 등록된 CI 상태가 없다. 따라서 저장소 코드 정적 검토만으로 Unity Editor 실제 컴파일 성공과 EditMode Test Runner 전체 통과를 확정할 수는 없다.

---

## 45일차 결과

44일차의 Encounter UI와 Command 구조 위에 행동 선택 조건과 중복 입력 방지, 탐험 이동 잠금 복구를 추가했다.

몬스터 접촉 조건도 정확히 같은 칸에서 주변 8방향 1칸 범위로 확장되어 플레이어가 몬스터 바로 근처에 접근하면 Encounter를 시작할 수 있는 구조가 되었다.

또한 향후 3D 모델 대신 2D 일러스트를 몬스터 외형으로 사용할 수 있도록 Billboard View, SpriteRenderer 자식 구조, MonsterDefinitionId 기반 자동 Sprite 로딩과 Importer를 준비했다.

현재 실제 몬스터 일러스트는 포함하지 않는다. 이미지가 없는 상태에서는 기존 Capsule 테스트 외형을 계속 사용하며, 다음 일차 이후 일러스트를 추가해 실제 Billboard 표시 크기와 위치를 검증한다.
