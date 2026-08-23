# Project Delta - 24일차 개발일지

## 개발 주제

**새 게임 흐름 최소 구현**

`RunContext.Begin()`이 5일차부터 프로젝트 전체에서 단 한 번도 호출되지 않고 있었다. 그 결과 20~23일차에 만든 방문 상태(Visited)/완료 상태(Completed)/층 번호(CurrentFloor)/미니맵 기능이 전부 "DungeonScene을 직접 열 때만 확인되는 배관"으로만 존재했다. 오늘은 TitleScene부터 시작해 실제로 새 런을 시작하고 씬 사이를 오갈 수 있는 최소 경로를 만들었다.

---

## 개발 목표

- `TitleScene`/`SettingsScene`/`LoadingScene`/`DungeonScene` 4개 씬에 임시 UI를 두고 실제로 오갈 수 있게 연결
- "새 게임" 버튼에서 `RunContext.Begin()`을 처음으로 실제 호출
- 던전에서 타이틀로 돌아갈 때 `RunContext.End()`(런 포기)도 처음으로 실제 호출
- 정식 UI(Canvas/Button)는 아직 이르므로, 기존 문/계단/미니맵과 같은 OnGUI 방식으로 최소한만 구현

---

## 구현 내용

### 1. ApplicationFlow — 씬 전환 로직의 중심

```text
ApplicationFlow (Application)
├─ EnterTitle() — 기존 (AppRoot 부팅 시 호출)
├─ StartNewGame() (오늘) — RunContext.Begin() → LoadingScene → DungeonScene
├─ OpenSettings() (오늘) — SettingsScene으로 즉시 이동 (가벼워서 로딩 화면 생략)
├─ ReturnToTitle() (오늘) — 진행 중인 런 있으면 RunContext.End() 후 TitleScene
└─ ProceedFromLoadingScreen() (오늘) — 예약해둔 목적지 씬으로 이동
```

`ApplicationFlow.Current` 정적 접근자를 새로 추가했다. `RunContext.Current`와 같은 패턴으로, Presentation 쪽 씬 UI가 Infrastructure(`AppRoot`)를 직접 참조하지 않고도 화면 전환을 요청할 수 있게 하기 위함이다 — Presentation은 Infrastructure에 의존하면 안 되는 기존 의존 방향 규칙을 유지하려면 이 방법이 필요했다. `ApplicationFlow`가 `RunContext`(Domain)를 직접 쓰게 되면서, `ProjectDelta.Application.asmdef`에 `ProjectDelta.Domain` 참조를 추가했다.

### 2. 4개 씬에 임시 OnGUI 컨트롤러 부착

```text
TitleSceneController    — "새 게임" / "설정" / "종료"
SettingsSceneController — "설정 (준비 중)" / "뒤로가기"
LoadingSceneController  — "로딩 중..." / "계속 (임시)" — 실제 진행률 대신 수동 확인용
DungeonDebugMenuController — DungeonScene 좌측 상단 "타이틀로 (임시)" 버튼, Player에 부착
```

Canvas·Button·EventSystem 없이 기존 문/계단/미니맵과 동일한 `OnGUI()` 즉시 모드 방식을 그대로 썼다. 새 Input System UI Module 설정이나 uGUI 계층 구조를 오늘 하루에 새로 들이는 대신, 이미 검증된 패턴을 재사용하는 쪽을 택했다.

### 3. 연결된 흐름

```text
TitleScene ──새 게임──▶ LoadingScene ──계속──▶ DungeonScene ──타이틀로──▶ TitleScene
    └──설정──▶ SettingsScene ──뒤로가기──▶ TitleScene
```

---

## 적용 중 발견된 문제 및 수정

없음. 다만 구현 중 두 가지를 미리 점검해서 문제로 번지기 전에 피했다.

1. **어셈블리 참조 누락**: `ApplicationFlow`(Application)가 `RunContext`(Domain)를 직접 호출해야 하는데, `ProjectDelta.Application.asmdef`는 `ProjectDelta.Data`만 참조하고 있었다. Data가 Domain을 참조한다고 해서 Application이 Domain 타입을 자동으로 쓸 수 있는 건 아니라서(비전이적 참조), asmdef에 Domain을 직접 추가했다.
2. **의존 방향 위반 가능성**: 처음엔 씬 UI 컨트롤러가 `AppRoot.Instance`(Infrastructure)를 직접 참조하게 만들려 했는데, `ProjectDelta.Presentation.asmdef`에 Infrastructure 참조가 없다는 걸 먼저 확인했다. Presentation→Infrastructure 참조를 새로 뚫는 대신, `ApplicationFlow.Current` 정적 접근자를 추가해서 기존 의존 방향(Presentation→Application)만으로 해결했다.

---

## 현재 24일차 전체 흐름

```text
ApplicationFlow에 StartNewGame/OpenSettings/ReturnToTitle/ProceedFromLoadingScreen 추가
↓
ApplicationFlow.Current 정적 접근자로 Presentation-Infrastructure 의존 방향 문제 회피
↓
Application asmdef에 Domain 참조 추가
↓
TitleScene/SettingsScene/LoadingScene에 각각 OnGUI 컨트롤러 부착
↓
DungeonScene Player에 DungeonDebugMenuController 부착
↓
BootstrapScene → TitleScene → 새 게임 → LoadingScene → DungeonScene → 타이틀로 순환 경로 완성
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Presentation/TitleSceneController.cs
Assets/ProjectDelta/Scripts/Presentation/TitleSceneController.cs.meta
Assets/ProjectDelta/Scripts/Presentation/SettingsSceneController.cs
Assets/ProjectDelta/Scripts/Presentation/SettingsSceneController.cs.meta
Assets/ProjectDelta/Scripts/Presentation/LoadingSceneController.cs
Assets/ProjectDelta/Scripts/Presentation/LoadingSceneController.cs.meta
Assets/ProjectDelta/Scripts/Presentation/DungeonDebugMenuController.cs
Assets/ProjectDelta/Scripts/Presentation/DungeonDebugMenuController.cs.meta
Devlogs/Day24/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/ApplicationFlow.cs (StartNewGame 등 추가)
Assets/ProjectDelta/Scripts/Application/ProjectDelta.Application.asmdef (Domain 참조 추가)
Assets/ProjectDelta/Scenes/TitleScene.unity (TitleSceneController 부착)
Assets/ProjectDelta/Scenes/SettingsScene.unity (SettingsSceneController 부착)
Assets/ProjectDelta/Scenes/LoadingScene.unity (LoadingSceneController 부착)
Assets/ProjectDelta/Scenes/DungeonScene.unity (DungeonDebugMenuController 부착)
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

24일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- BootstrapScene에서 Play 시 TitleScene까지 정상 진입
- "새 게임" → LoadingScene → "계속" → DungeonScene까지 정상 진입, 기존 이동/문/계단/미니맵 기능이 그대로 동작
- DungeonScene "타이틀로" 클릭 시 Console에 런 포기 로그가 뜨고 TitleScene으로 복귀
- "설정" → SettingsScene → "뒤로가기" → TitleScene 순환 확인

**참고**: 저장된 런을 이어하는 "이어하기" 흐름은 아직 없다. `SaveService.WriteRun`/`ReadRun`이 연동되는 26일차 이후 자연스럽게 필요해진다. 오늘 만든 UI는 전부 임시(OnGUI)이며, 정식 UI(Canvas/Button, 아트)는 별도 일차에서 다시 만든다.

---

## 다음 개발 방향

25일차에는 **상자·비밀벽 상호작용 최소 골격**을 진행한다. 문(14일차)/계단(22일차)과 같은 F 상호작용 패턴을 `RoomContentType.Chest`, `SecretWall`에도 적용해서, 던전 생성(28일차 이후로 예정 변경)이 배치할 콘텐츠 종류를 최소 2종 더 확보한다.
