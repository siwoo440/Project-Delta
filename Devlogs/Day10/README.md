# Project Delta - 10일차 개발일지

## 개발 주제

**Save Inspector 디버그 메뉴, 저장/불러오기 EditMode·PlayMode 테스트와 회귀 테스트 기준 작성**

저장·런타임 구간(4~10일차)의 마지막 일차. 지금까지 만든 저장 시스템을 검증하는 도구와 자동화 테스트를 갖췄고, 그 과정에서 실제 버그 하나를 발견해 고쳤다.

---

## 개발 목표

- Save Inspector 에디터 창으로 저장 파일 내용을 직접 확인
- EditMode 테스트로 저장 시스템 핵심 동작(왕복, 백업 순환, 손상 복구) 검증
- PlayMode 테스트로 Bootstrap→Title 부팅 흐름 검증
- 기획서 10.6절이 요구하는 Assembly Definition 8종 구성
- 이 테스트들을 4~10일차의 회귀 테스트 기준으로 확정

---

## 구현 내용

### 1. 숨어있던 순환 참조 발견과 해소

Assembly Definition을 나누려는 과정에서, `AppRoot`(Infrastructure)가 `ApplicationFlow`(Application)를 생성하고 `ApplicationFlow`는 반대로 `ILogService`/`ISceneLoaderService`(당시 Infrastructure 소속)를 참조하는 **양방향 순환**이 있다는 걸 발견했다. 지금까지는 모든 스크립트가 하나의 Assembly-CSharp로 컴파일돼 문제가 드러나지 않았다.

```text
서비스 인터페이스 6개 이동: Infrastructure → Application
ILogService, ISceneLoaderService, ILocalizationService,
IAddressableService, IInputService, ISaveService
```

구현체(`LogService` 등)는 Infrastructure에 남기고 `using ProjectDelta.Application;`만 추가했다. 기획서 10.1절 "Infrastructure → Application 또는 인터페이스 구현" 원칙과 일치하는 구조가 됐다. `AppRoot.cs`는 이미 `using ProjectDelta.Application;`을 갖고 있어 수정이 필요 없었다.

---

### 2. Assembly Definition 8종 구성 (기획서 10.6절)

```text
ProjectDelta.Domain          (독립)
ProjectDelta.Data            (독립)
ProjectDelta.Application     → Data
ProjectDelta.Infrastructure  → Application, Data
ProjectDelta.Presentation    → Application, Data
ProjectDelta.Editor          → Application, Infrastructure, Data (Editor 전용)
ProjectDelta.Tests.EditMode  → 위 전체 + Unity Test Framework (Editor 전용)
ProjectDelta.Tests.PlayMode  → 위 전체 + Unity Test Framework
```

의존 방향이 한쪽으로만 흐르도록 구성해 순환을 원천적으로 막았다.

---

### 3. 패키지 참조 오류 두 차례 수정

Assembly-CSharp에서는 모든 패키지가 자동으로 보였지만, 독립 asmdef는 필요한 패키지를 직접 나열해야 했다.

```text
1차: Unity.InputSystem, Unity.Addressables, Unity.Localization 추가
2차: Unity.ResourceManager 추가
  → Addressables.InitializeAsync()/LocalizationSettings.InitializationOperation이
    반환하는 AsyncOperationHandle<T>가 이 어셈블리에 있음.
    asmdef 참조는 컴파일 순서만 전이되고, 실제 사용하는 타입은 그 타입의
    소속 어셈블리를 직접 참조해야 한다.
```

---

### 4. Save Inspector (기획서 10.6절 에디터 도구)

`Window > Project Delta > Save Inspector`에서 `profile.json`/`run.json`/`settings.json` 내용을 그대로 표시하고, "파일 손상시키기" 버튼으로 복구 로직을 손으로 검증할 수 있게 했다. 경로는 `SavePaths`(실제 도메인 코드)를 그대로 사용해, 기획서 원칙("Editor 도구는 런타임 게임 규칙을 복사하지 않고 실제 도메인 코드를 사용한다")을 지켰다.

---

### 5. EditMode 테스트 5종

```text
WriteThenReadProfile_ReturnsEquivalentData   — 왕복 저장
SecondWrite_CreatesBackup1                   — 백업 1개 생성
FourWrites_RotatesThreeBackups               — 백업 3단계 순환
CorruptedCurrentFile_RecoversFromBackup1     — 손상 시 자동 복구
AllCandidatesCorrupted_ThrowsInvalidDataException — 전부 손상 시 예외
```

---

### 6. PlayMode 테스트 1종

```text
Bootstrap_ReachesTitleScene
BootstrapScene 로드 → AppRoot 초기화 대기 → TitleScene 도달 확인
```

---

## 적용 중 발견된 문제 및 수정

### 7. 컴파일 에러 2차례 (asmdef 패키지 참조 누락)

위 3절에 정리한 대로, `Unity.InputSystem`/`Unity.Addressables`/`Unity.Localization`, 이어서 `Unity.ResourceManager`를 `ProjectDelta.Infrastructure.asmdef`에 추가해 해결했다.

### 8. 실제 버그: 손상 파일 파싱 실패 시 예외가 복구 순회를 중단시킴

Test Runner에서 EditMode 테스트 2개가 실패했다.

```text
AllCandidatesCorrupted_ThrowsInvalidDataException — 실패
CorruptedCurrentFile_RecoversFromBackup1 — 실패
```

원인: 테스트가 파일을 일반 텍스트(`"corrupted"`)로 덮어썼는데, `TryReadEnvelope`가 `JsonConvert.DeserializeObject`를 호출할 때 **JSON으로 파싱조차 안 되는 경우**(체크섬 불일치가 아니라 파싱 자체 실패)를 처리하지 않고 있었다. 이 경우 예외가 그대로 `ReadFileWithRecovery`의 순회 루프 밖으로 튀어나가, "다음 백업 후보로 넘어가기"가 실행되지 못하고 전체가 실패했다.

8~9일차에 만든 복구 순회 로직 자체는 맞았지만, "파싱 실패도 손상으로 취급한다"는 조건 하나가 빠져 있었다. `TryReadEnvelope`에 `try/catch(JsonException)`을 추가해 파싱 실패도 "이 후보는 못 씀"으로 처리하도록 수정했다. 재실행 결과 5개 테스트 전부 통과했다.

이 버그는 손으로 하나씩 확인했다면 놓쳤을 가능성이 높다 — 자동화 테스트를 만든 첫날 바로 실제 버그를 잡아낸 사례다.

---

## 현재 10일차 전체 흐름

```text
Assembly Definition 도입 → 숨어있던 Application↔Infrastructure 순환 참조 발견 및 해소
↓
8개 asmdef 구성, 패키지 참조 오류 2차례 수정
↓
Save Inspector 에디터 도구 구현
↓
EditMode 테스트 5종 작성 → 2개 실패 → 손상 파싱 처리 누락 버그 발견 및 수정 → 5개 전부 통과
↓
PlayMode 테스트 1종 작성
↓
4~10일차 저장·런타임 구간 회귀 테스트 기준 확정
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Domain/ProjectDelta.Domain.asmdef
Assets/ProjectDelta/Scripts/Data/ProjectDelta.Data.asmdef
Assets/ProjectDelta/Scripts/Application/ProjectDelta.Application.asmdef
Assets/ProjectDelta/Scripts/Infrastructure/ProjectDelta.Infrastructure.asmdef
Assets/ProjectDelta/Scripts/Presentation/ProjectDelta.Presentation.asmdef
Assets/ProjectDelta/Scripts/Editor/ProjectDelta.Editor.asmdef
Assets/ProjectDelta/Scripts/Editor/SaveInspectorWindow.cs
Assets/ProjectDelta/Tests/EditMode/ProjectDelta.Tests.EditMode.asmdef
Assets/ProjectDelta/Tests/EditMode/SaveServiceTests.cs
Assets/ProjectDelta/Tests/PlayMode/ProjectDelta.Tests.PlayMode.asmdef
Assets/ProjectDelta/Tests/PlayMode/AppRootBootTests.cs
Devlogs/Day10/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Application/ApplicationFlow.cs (Infrastructure 참조 제거)
Assets/ProjectDelta/Scripts/Infrastructure/LogService.cs
Assets/ProjectDelta/Scripts/Infrastructure/SceneLoaderService.cs
Assets/ProjectDelta/Scripts/Infrastructure/LocalizationService.cs
Assets/ProjectDelta/Scripts/Infrastructure/AddressableService.cs
Assets/ProjectDelta/Scripts/Infrastructure/InputService.cs
Assets/ProjectDelta/Scripts/Infrastructure/SaveService.cs (using 추가 + 손상 파싱 처리 버그 수정)
```

## 이동 파일 (Infrastructure → Application)

```text
ILogService.cs, ISceneLoaderService.cs, ILocalizationService.cs,
IAddressableService.cs, IInputService.cs, ISaveService.cs
```

## 삭제 파일

없음 (빈 폴더 `.gitkeep` 3개는 실제 내용으로 자연 대체됨).

---

## 최종 확인 항목

10일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- Assembly Definition 8종이 순환 없이 한 방향으로만 참조함
- Save Inspector로 저장 파일 3종 내용을 확인할 수 있음
- EditMode 테스트 5개 전부 통과
- PlayMode 테스트 1개 통과
- 4~10일차(저장·런타임) 전체 기능이 테스트로 뒷받침됨

**이로써 4~10일차 저장·런타임 구간이 종료된다.**

---

## 다음 개발 방향

다음 11일차부터 **던전 탐험** 구간(11~25일차)이 시작된다. 11일차에는 **단일 테스트 방과 플레이어 그리드 위치 데이터**를 구현한다.

예정 흐름:

```text
단일 테스트 방(RoomView 프리팹 이전의 최소 형태) 배치
↓
플레이어 그리드 좌표 데이터 정의 (Vector2Int 기반)
↓
DungeonScene에서 플레이어 위치 표현
↓
이후 12일차 WASD 이동 입력과 연결 준비
```

RunContext.Dungeon(5일차 placeholder), RunData.DungeonState(4일차 placeholder)가 이 시점부터 실제 구현으로 채워지기 시작한다.
