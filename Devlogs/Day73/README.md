# Project Delta - 73일차 개발일지

## 작업 주제

**몬스터 행동 예고(Intent) 시스템 구현 및 전투 UI 정리**

---

## 개발 목표

72일차까지 전투 승리 보상 선택과 탐험 복귀 흐름을 구현했다.

73일차에서는 다음 단계인 몬스터 AI 구현에 앞서, 몬스터가 다음에 사용할 행동을 미리 저장하고 플레이어에게 보여줄 수 있는 `BattleIntent` 기반 행동 예고 구조를 구축했다.

또한 실제 전투 화면을 확인하면서 다음 UI 문제를 함께 정리했다.

```text
행동 예고가 적 슬롯과 떨어져 표시됨
항복 버튼이 다른 전투 명령 버튼과 분리됨
전투 중에도 미니맵이 표시됨
좌측 상단 임시 저장/불러오기/타이틀 디버그 버튼이 남아 있음
```

73일차에서는 행동 예고 시스템과 함께 위 UI 요소를 현재 전투 구조에 맞게 정리했다.

---

## 주요 작업 내용

### 1. BattleIntent 데이터 구조 추가

몬스터의 다음 행동을 표현하는 `BattleIntent` 구조를 추가했다.

Intent는 다음 정보를 가진다.

```text
ActorInstanceId
TargetInstanceId
CommandId
DisplayName
SkillId
IconType
IsSilenceSensitive
```

이를 통해 단순히 "공격 예정"이라는 문자열만 표시하는 것이 아니라:

```text
누가 행동하는지
누구를 대상으로 하는지
어떤 Command를 실행하는지
어떤 Skill을 사용할지
HUD에는 어떤 분류로 표시할지
침묵에 영향을 받는 행동인지
```

를 하나의 데이터로 관리할 수 있게 했다.

현재 73일차에서는 기존 몬스터 행동이 기본 공격이므로 `CreateBasicAttack()`을 통해 기본 공격 Intent를 생성한다.

---

### 2. 행동 예고 아이콘 유형 7종 정의

향후 몬스터 AI와 HUD가 같은 분류를 사용할 수 있도록 `BattleIntentIconType`을 추가했다.

현재 분류는 다음 7종이다.

```text
Attack
Defend
Buff
Debuff
Status
Heal
Special
```

현재 실제 전투에서는 기본 공격만 사용하므로 우선 다음과 같이 표시된다.

```text
[ATK] 공격
```

74일차 이후 몬스터 AI가 방어·강화·약화·상태이상·회복·특수 행동을 선택하면 같은 Intent 구조에 해당 아이콘 유형을 연결할 수 있다.

---

### 3. BattleIntentService 구현

전투 중 생성된 행동 예고를 관리하는 `BattleIntentService`를 추가했다.

주요 기능은 다음과 같다.

```text
Intent 등록
Intent 조회
Intent 소비
Intent 취소
마지막 취소 사유 조회
전체 Intent 초기화
```

동일한 Actor에 이미 Intent가 존재하면 새 Intent를 다시 등록할 수 없도록 했다.

즉:

```text
예고 생성
→ Intent 고정
→ 중간 변경 방지
→ 실제 행동 완료
→ Intent 소비
→ 다음 Intent 생성
```

흐름을 유지한다.

이를 통해 이후 AI가 추가되더라도 플레이어에게 보여준 행동과 실제 실행 행동이 임의로 달라지는 문제를 방지할 수 있는 기반을 만들었다.

---

### 4. 행동 예고 취소 사유 5종 구현

`BattleIntentCancelReason`을 추가하고 행동 예고를 취소할 수 있는 사유를 구분했다.

```text
Stunned
Silenced
ActorDefeated
Satisfied
TargetUnavailable
```

게임 표현으로는:

```text
기절
침묵
사망
만족 상태
대상 부재
```

에 해당한다.

기본적으로 예고된 행동은 유지하며, 해당 조건이 발생한 경우에만 Intent를 취소할 수 있도록 했다.

침묵의 경우 모든 행동을 막지 않고 `IsSilenceSensitive`인 Intent에만 적용한다.

---

### 5. BattleIntentRuntimeController 추가

현재 BattleContext와 Intent 시스템을 연결하는 `BattleIntentRuntimeController`를 추가했다.

전투가 시작되면 현재 Enemy 목록을 확인하고 각 살아 있는 적에게 기본 공격 Intent를 등록한다.

현재 흐름:

```text
Battle 시작
→ Enemy 확인
→ Player를 Target으로 설정
→ 기본 공격 Intent 생성
→ BattleIntentService 등록
```

Enemy가 실제 행동을 완료하면 `LastActionSequence`와 `LastActingParticipant`를 확인해 해당 적의 Intent를 소비한다.

이후 다음 Update에서 새로운 Intent가 등록된다.

```text
Enemy 행동 완료
→ 기존 Intent 소비
→ 다음 행동 Intent 준비
```

73일차에서는 AI 행동 선택을 구현하지 않고 기존 자동 기본 공격과 동일한 Intent를 생성한다.

실제 AI 판단은 74일차에서 이 구조에 연결한다.

---

### 6. 상태이상 기반 Intent 취소 연결

현재 상태이상 시스템과 행동 예고 취소 조건을 연결했다.

기절은 기존:

```text
StatusEffectKind.Stun
```

을 직접 사용한다.

침묵은 현재 별도의 StatusEffectKind로 구현되어 있지 않으므로 활성 상태이상의 `DefinitionId`에 다음 값이 포함되는지 확인하는 방식으로 연결했다.

```text
SILENCE
침묵
```

만족 상태는 아직 해당 전투 시스템이 구현되지 않았기 때문에 취소 규칙에는 포함하지만 현재 런타임 값은 false로 유지한다.

향후 만족 상태 시스템이 추가되면 해당 값만 실제 상태와 연결할 수 있다.

---

### 7. BattleIntentHudController 추가

각 Enemy의 Intent를 전투 HUD에 출력하기 위한 `BattleIntentHudController`를 추가했다.

표시 예:

```text
[ATK] 공격
→ PLAYER
```

Intent가 취소된 경우에는 마지막 취소 사유를 표시할 수 있도록 했다.

예:

```text
[취소] 기절
```

Enemy가 없거나 사망한 슬롯은 행동 예고를 표시하지 않는다.

---

### 8. 행동 예고 UI 위치 수정

최초 구현에서는 행동 예고가 전투 화면 위쪽 별도 패널에 표시되어 각 Enemy와 연결 관계가 직관적이지 않았다.

실제 전투 화면 확인 후 별도 `BattleIntentPanel`을 제거했다.

이제 행동 예고는 각 Enemy Slot 내부에서:

```text
몬스터 이미지
몬스터 이름
HP Bar
HP 10 / 10
[ATK] 공격
→ PLAYER
```

순서로 표시된다.

즉 행동 예고가 해당 개체의 HP 텍스트 바로 아래에 위치한다.

Enemy마다 각각 독립된 `BattleIntentText`를 가진다.

---

### 9. 항복 버튼 전투 명령 행에 통합

기존 항복 버튼은 화면 좌측 아래에 별도로 위치해 있었다.

이를 기존 전투 명령 버튼과 같은 부모 아래로 옮기고 `유혹` 버튼 오른쪽에 배치했다.

전투 명령 UI는 현재 다음 형태로 정리됐다.

```text
[공격] [행동] [방어] [아이템] [도주] [유혹] [항복]
```

항복 버튼은 유혹 버튼과 동일한 크기·Anchor·Pivot·Scale을 사용하도록 맞췄다.

기존 `BattleSurrenderController`의 항복 확인 및 패배 처리 로직은 그대로 유지한다.

---

### 10. 전투 중 미니맵 숨김

탐험 화면에서 사용하던 `DungeonMinimapController`가 전투 HUD 위에도 계속 표시되는 문제를 정리했다.

`BattleExplorationUiVisibilityController`를 추가해 다음 규칙을 적용했다.

```text
탐험 중
→ DungeonMinimapController 활성

전투 중
→ DungeonMinimapController 비활성

전투 종료
→ DungeonMinimapController 다시 활성
```

미니맵 기능 자체를 삭제하지 않고 전투 상태에서만 일시적으로 비활성화한다.

---

### 11. 임시 Dungeon 디버그 버튼 완전 제거

초기 개발 과정에서 사용하던 좌측 상단의:

```text
저장하기 (임시)
불러오기 (임시)
타이틀로 (임시)
```

버튼을 제거했다.

해당 UI는 `DungeonDebugMenuController.OnGUI()`에서 직접 생성되고 있었으며, 이제 사용하지 않기 때문에 단순 비활성화가 아니라 다음 항목을 완전히 삭제했다.

```text
DungeonScene의 DungeonDebugMenuController Component
DungeonDebugMenuController.cs
DungeonDebugMenuController.cs.meta
```

저장 및 타이틀 이동 기능 자체가 삭제된 것이 아니라, 해당 기능을 직접 호출하던 개발용 임시 버튼만 제거한 것이다.

---

### 12. Day73BattleIntentInstaller 정리

73일차 Editor Installer를 확장해 UI 수정 작업을 자동 적용할 수 있도록 했다.

메뉴:

```text
Project Delta
→ 73일차
→ 73일차 행동 예고 UI 적용
```

실행 시 다음 작업을 수행한다.

```text
기존 상단 BattleIntentPanel 제거
각 Enemy HP 아래 BattleIntentText 생성
Intent Runtime Controller 연결
Intent HUD Controller 연결
항복 버튼을 유혹 오른쪽으로 이동
항복 버튼 크기를 기존 행동 버튼과 통일
BattleExplorationUiVisibilityController 연결
DungeonDebugMenuController Component 제거
DungeonDebugMenuController Script 삭제
DungeonScene 저장
```

---

## EditMode 테스트 추가

### BattleIntentServiceTests

73일차 Intent 시스템의 핵심 규칙을 검증하는 테스트를 추가했다.

현재 테스트 항목:

```text
Intent Icon Type이 정확히 7종인지 확인
등록된 Intent가 소비 또는 취소 전까지 변경되지 않는지 확인
Intent 소비 후 정상적으로 제거되는지 확인
사망 시 취소
기절 시 취소
침묵 시 취소
만족 상태 시 취소
대상 부재 시 취소
침묵 비대상 행동은 유지되는지 확인
취소 사유가 기록되고 Intent가 제거되는지 확인
```

---

## 변경 파일

### 신규

```text
Assets/ProjectDelta/Scripts/Application/BattleIntent.cs
Assets/ProjectDelta/Scripts/Application/BattleIntentCancelReason.cs
Assets/ProjectDelta/Scripts/Application/BattleIntentIconType.cs
Assets/ProjectDelta/Scripts/Application/BattleIntentService.cs

Assets/ProjectDelta/Scripts/Presentation/BattleIntentRuntimeController.cs
Assets/ProjectDelta/Scripts/Presentation/BattleIntentHudController.cs
Assets/ProjectDelta/Scripts/Presentation/BattleExplorationUiVisibilityController.cs

Assets/ProjectDelta/Scripts/Editor/Day73BattleIntentInstaller.cs

Assets/ProjectDelta/Tests/EditMode/BattleIntentServiceTests.cs
```

각 신규 Script의 `.meta` 파일도 함께 추가됐다.

### 수정

```text
Assets/ProjectDelta/Scenes/DungeonScene.unity
```

### 삭제

```text
Assets/ProjectDelta/Scripts/Presentation/DungeonDebugMenuController.cs
Assets/ProjectDelta/Scripts/Presentation/DungeonDebugMenuController.cs.meta
```

---

## 확인 사항

- BattleIntent 데이터 구조 추가
- 행동 예고 아이콘 유형 7종 정의
- Actor별 Intent 등록 및 조회
- 기존 Intent 중간 덮어쓰기 방지
- 행동 완료 후 Intent 소비
- Intent 취소 사유 5종 구현
- 기절 상태와 기존 전투 상태이상 연결
- 침묵 상태 Definition ID 기반 연결
- 만족 상태의 향후 연결 지점 확보
- 각 Enemy에게 현재 기본 공격 Intent 생성
- Enemy 행동 후 다음 Intent 재생성 기반 구축
- Enemy Slot별 행동 예고 표시
- 행동 예고를 HP 텍스트 바로 아래로 이동
- 기존 상단 행동 예고 패널 제거
- 항복 버튼을 유혹 버튼 오른쪽에 배치
- 항복 버튼 크기와 기존 행동 버튼 크기 통일
- 전투 중 미니맵 숨김
- 전투 종료 후 미니맵 복구
- 임시 저장/불러오기/타이틀 버튼 제거
- DungeonDebugMenuController Script 및 meta 삭제
- BattleIntentService EditMode 테스트 추가

---

## 저장소 점검

최신 73일차 커밋의 코드와 Scene 변경을 확인했다.

정적 점검 기준으로는 Intent 등록·고정·소비·취소 구조와 UI 상태 제어 사이에 추가적인 명백한 충돌을 확인하지 못했다.

또한 최신 커밋에서 `DungeonDebugMenuController.cs`와 `.meta`가 실제로 삭제되고 DungeonScene의 해당 Component 참조도 제거된 것을 확인했다.

다만 GitHub 최신 커밋에는 CI 상태 또는 Unity Test Runner 실행 결과가 등록되어 있지 않다.

따라서 다음 항목은 로컬 Unity Editor에서 최종 확인이 필요하다.

```text
Unity Script Compile 성공 여부
BattleIntentServiceTests 전체 통과 여부
실제 해상도에서 Enemy HP 아래 Intent 위치
항복 버튼 한 줄 정렬 상태
전투 시작/종료 시 미니맵 표시 전환
```

---

## 이번 일차 완료 상태

73일차에서는 몬스터 AI보다 먼저 필요한 **행동 예고(Intent) 기반 시스템**을 구축했다.

현재 몬스터는 기존 전투 로직과 동일하게 기본 공격을 사용하지만, 이제 다음 행동을 별도 Intent 데이터로 저장하고 HUD에서 확인할 수 있다.

또한 플레이 화면 점검을 통해 전투 중 불필요한 탐험 UI와 임시 디버그 UI를 제거하고, 행동 예고와 항복 버튼을 전투 HUD 안에 자연스럽게 배치했다.

현재 구조는 다음과 같다.

```text
몬스터 다음 행동 준비
→ BattleIntent 생성
→ Enemy Slot에 행동 예고 표시
→ 플레이어 행동
→ Enemy 차례
→ 현재 기본 공격 실행
→ 사용된 Intent 소비
→ 다음 Intent 생성
```

이 기반을 사용하면 74일차부터 몬스터 AI가 실제 상황에 따라 서로 다른 Intent를 선택하도록 확장할 수 있다.

---

## 다음 단계

74일차에서는 몬스터의 보유 행동, 현재 HP, 상태이상, 플레이어 상태 등을 기준으로 실제 다음 행동을 선택하는 **몬스터 AI 행동 선택 시스템**을 구현한다.

AI가 선택한 결과를 이번 73일차의 `BattleIntent`에 등록해:

```text
AI 판단
→ Intent 생성
→ 플레이어에게 예고
→ 예고된 행동 실제 실행
```

흐름을 완성하는 것이 다음 목표다.
