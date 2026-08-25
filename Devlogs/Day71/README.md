# Project Delta - 71일차 개발일지

## 작업 주제

**항복 UI 전투 HUD 통합 + 패배 결과 화면 및 정식 런 종료 흐름 구현**

---

## 개발 목표

70일차에서는 항복과 일반 패배를 구분하고, 마지막 실제 공격자와 패배 라운드를 기록할 수 있는 기반을 구축했다.

하지만 패배가 발생하면 기록을 실제 화면에서 확인하지 못한 채 곧바로 타이틀 화면으로 돌아가는 임시 구조였고, 항복 UI 역시 전투 HUD와 별개의 `BattleSurrenderCanvas`에 존재했다.

71일차에서는 다음 두 부분을 정리했다.

```text
1. 별도 BattleSurrenderCanvas 제거
2. 항복 버튼과 확인창을 기존 전투 HUD 내부로 통합
3. 패배 정보를 DefeatScene으로 전달
4. 패배 원인·도달 층·패배 라운드·마지막 공격자 표시
5. 패배 정보 확보 후 현재 런 종료
6. DefeatScene에서 타이틀 화면으로 복귀
```

이번 일차를 통해 70일차에서 만든 패배 기록 데이터를 실제 게임 오버 흐름에서 사용할 수 있게 했다.

---

## 주요 작업 내용

### 1. 기존 BattleSurrenderCanvas 제거

70일차에서 항복 기능 확인을 위해 별도로 생성했던:

```text
BattleSurrenderCanvas
├─ SurrenderButton
└─ SurrenderConfirmation
```

구조를 제거했다.

항복 UI는 전투에 종속된 기능이므로 별도의 Canvas로 유지하지 않고 기존 전투 HUD 안에서 함께 관리하도록 구조를 정리했다.

기존 70일차 설치용 `Day70SurrenderInstaller.cs`도 제거했다.

---

### 2. 기존 전투 HUD에 항복 UI 통합

`DungeonScene`의 기존 `BattleHudController`가 사용하는 HUD Root 아래에 항복 UI를 추가했다.

구조는 다음과 같다.

```text
Battle HUD
├─ 기존 전투 UI
├─ SurrenderButton
└─ SurrenderConfirmation
   ├─ MessageText
   ├─ ConfirmButton
   └─ CancelButton
```

기존 `BattleSurrenderController`는 유지하고, 새로 생성된 항복 버튼과 확인창을 연결해 기존 70일차 항복 판정 로직을 그대로 재사용한다.

따라서 항복의 핵심 규칙은 변경하지 않았다.

```text
플레이어 차례
BattleState.AwaitingAction
플레이어 Actor 생존
→ 항복 가능
```

항복 확정 시 `SurrenderBattleCommand`와 `BattleDefeatService.RecordSurrender()`를 거쳐 패배 처리 흐름으로 진입한다.

---

### 3. DefeatSceneState 추가

패배가 발생한 직후 `RunContext`를 종료하면 현재 층과 패배 정보를 더 이상 안전하게 읽기 어렵기 때문에, 패배 결과 화면으로 전달할 전용 상태를 추가했다.

`RunDefeatSummary`에는 다음 정보를 보관한다.

```text
Reason
AttackerInstanceId
AttackerDefinitionId
RoundNumber
FloorNumber
HasAttacker
```

`DefeatSceneState.Capture()`는 `BattleDefeatRecord`와 현재 층을 복사해 패배 화면에서 사용할 수 있는 결과 데이터로 만든다.

`DefeatSceneState.Clear()`는 새 게임 시작, 이어하기, 타이틀 복귀 시 이전 패배 정보가 다음 플레이에 남지 않도록 정리한다.

---

### 4. BattleDefeatService 패배 종료 흐름 변경

70일차까지 패배 흐름은 다음과 같았다.

```text
패배 발생
→ BattleDefeatRecord 생성
→ ReturnToTitle()
```

71일차부터는 다음 흐름을 사용한다.

```text
패배 발생
→ 일반 패배 또는 항복 기록 확정
→ 현재 Dungeon Floor 확인
→ DefeatSceneState.Capture()
→ ApplicationFlow.EnterDefeat()
→ DefeatScene
```

일반 패배라면 마지막으로 플레이어에게 실제 피해를 준 공격자가 결과에 포함된다.

항복이라면 공격자 없이 `Surrender` 사유가 그대로 전달된다.

---

### 5. ApplicationFlow에 패배 Scene 진입 흐름 추가

`ApplicationFlow`에 `EnterDefeat()`를 추가했다.

패배 데이터가 `DefeatSceneState`에 먼저 저장된 뒤 다음 순서로 런을 정리한다.

```text
RunContext.End()
→ 진행 중인 Run Save 삭제
→ Pending Restore 정보 정리
→ DefeatScene 로드
```

기존 `ReturnToTitle()`은 그대로 타이틀 복귀용으로 사용한다.

타이틀로 돌아갈 때는 `DefeatSceneState.Clear()`도 함께 호출해 이전 결과 데이터를 제거한다.

새 게임과 이어하기를 시작할 때도 패배 결과 상태를 초기화한다.

---

### 6. DefeatScene 추가

새로운 게임 오버 전용 씬:

```text
Assets/ProjectDelta/Scenes/DefeatScene.unity
```

을 추가했다.

패배 화면에서는 다음 정보를 표시한다.

```text
패배 원인
도달 층
패배 라운드
마지막 공격자
```

일반 패배 예시:

```text
패배

패배 원인 : 전투 패배
도달 층 : 3층
패배 라운드 : 4 Round
마지막 공격자 : MON_TEST
```

항복 예시:

```text
패배

패배 원인 : 항복
도달 층 : 3층
패배 라운드 : 2 Round
마지막 공격자 : 없음
```

---

### 7. DefeatSceneController 구현

`DefeatSceneController`가 `DefeatSceneState.Current`를 읽어 패배 화면 UI에 결과를 표시한다.

패배 정보가 없는 상태에서 Scene이 직접 실행된 경우에도 예외를 발생시키지 않고 다음처럼 대체 표시한다.

```text
패배 정보를 찾을 수 없습니다.
도달 층 : -
패배 라운드 : -
마지막 공격자 : -
```

`타이틀로 돌아가기` 버튼을 누르면:

```text
ApplicationFlow.Current?.ReturnToTitle()
```

을 호출해 TitleScene으로 복귀한다.

---

### 8. SceneNames 및 Build Settings 확장

`SceneNames`에 다음 Scene 상수를 추가했다.

```text
DefeatScene
```

또한 `ProjectSettings/EditorBuildSettings.asset`에 `DefeatScene.unity`를 등록해 런타임 SceneLoader에서 정상적으로 불러올 수 있도록 했다.

---

### 9. 71일차 Editor 설치 도구 추가

`Day71DefeatFlowInstaller`를 추가했다.

Unity Editor 메뉴:

```text
Project Delta
→ 71일차
→ 71일차 전체 적용
```

을 실행하면 다음 작업을 자동 수행하도록 구성했다.

```text
BootstrapScene의 BattleSurrenderCanvas 제거
DungeonScene 기존 전투 HUD에 항복 UI 생성
BattleSurrenderController 참조 연결
DefeatScene 생성
DefeatScene UI 생성
DefeatSceneController 참조 연결
Build Settings에 DefeatScene 등록
70일차 설치용 Editor Script 제거
```

실제 적용 후 Scene 파일이 저장소에 반영되었다.

---

## EditMode 테스트 추가

### DefeatSceneStateTests

다음 동작을 검증하는 테스트를 추가했다.

```text
BattleDefeatRecord의 패배 원인 복사
공격자 Instance ID 복사
공격자 Definition ID 복사
패배 라운드 복사
현재 층 복사
항복 시 공격자 없음 유지
Clear 호출 시 현재 결과 제거
```

---

## 변경 파일

```text
Assets/ProjectDelta/Editor/Day70SurrenderInstaller.cs (삭제)

Assets/ProjectDelta/Scenes/BootstrapScene.unity
Assets/ProjectDelta/Scenes/DungeonScene.unity
Assets/ProjectDelta/Scenes/DefeatScene.unity (신규)

Assets/ProjectDelta/Scripts/Application/ApplicationFlow.cs
Assets/ProjectDelta/Scripts/Application/BattleDefeatService.cs
Assets/ProjectDelta/Scripts/Application/DefeatSceneState.cs (신규)
Assets/ProjectDelta/Scripts/Application/SceneNames.cs

Assets/ProjectDelta/Scripts/Editor/Day71DefeatFlowInstaller.cs (신규)

Assets/ProjectDelta/Scripts/Presentation/DefeatSceneController.cs (신규)

Assets/ProjectDelta/Tests/EditMode/DefeatSceneStateTests.cs (신규)

ProjectSettings/EditorBuildSettings.asset
Project-Delta.slnx
```

Unity가 생성한 `.meta` 파일도 신규 Scene과 Script에 함께 추가되었다.

---

## 확인 사항

- 기존 별도 `BattleSurrenderCanvas` 제거 확인
- `DungeonScene` 기존 전투 HUD 내부에 `SurrenderButton` 생성 확인
- 기존 `BattleSurrenderController` 항복 로직 재사용
- 일반 패배와 항복 패배 정보 전달 유지
- `BattleDefeatRecord`에서 `RunDefeatSummary`로 결과 복사
- 현재 Dungeon Floor를 패배 결과에 포함
- 패배 결과 확보 후 `RunContext` 종료
- 패배 시 기존 즉시 타이틀 복귀 대신 `DefeatScene` 진입
- `DefeatSceneController`가 결과 데이터를 UI에 표시
- 패배 결과가 없을 때 안전한 대체 문구 표시
- 타이틀 복귀 시 이전 `DefeatSceneState` 제거
- 새 게임·이어하기 시작 시 이전 패배 상태 제거
- `DefeatScene` Build Settings 등록
- `DefeatSceneStateTests` 3종 추가
- 70일차 설치용 Editor Script 제거

GitHub 최신 커밋에는 별도의 CI 또는 Unity Test Runner 결과가 등록되어 있지 않다.

따라서 저장소의 변경 파일과 API 연결을 정적으로 점검한 기준에서는 추가적인 명백한 컴파일 차단 문제를 확인하지 못했지만, Unity Editor 실제 컴파일 및 EditMode Test Runner 실행 결과는 로컬 Unity에서 최종 확인해야 한다.

---

## 이번 일차 완료 상태

71일차 목표인 **항복 UI 전투 HUD 통합과 패배 결과 화면 기반**을 구현했다.

이제 패배 흐름은 다음과 같이 정리된다.

```text
전투 패배 또는 항복
→ 패배 원인 및 마지막 공격자 기록
→ 현재 층과 패배 라운드 보존
→ 현재 런 종료
→ DefeatScene 진입
→ 패배 결과 표시
→ 타이틀로 돌아가기
```

70일차의 패배 기록 기반이 실제 게임 오버 화면과 연결되었으며, 별도로 존재하던 항복 Canvas도 기존 전투 HUD에 통합되어 전투 UI 구조가 단순해졌다.

---

## 다음 단계

다음 일차에서는 현재까지 완성된 전투 종료 흐름을 기반으로 이후 전투 시스템 또는 기획서 개발 일정의 다음 항목을 이어서 구현한다.
