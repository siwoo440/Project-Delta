# Project Delta - 2일차 개발일지

## 개발 주제

**메인 씬 구조 및 AppRoot · ServiceRegistry · SceneLoader 기본 구조 구현**

이번 일차에서는 기획서 10.1절 기준 메인 씬 구성(Bootstrap/Title/Game/Settings/Loading)을 실제로 만들고, 씬 전환에 관계없이 유지되는 `AppRoot`와 그 하위 서비스 골격(`ServiceRegistry`, `LogService`, `SceneLoaderService`)을 구현했다.

---

## 개발 목표

- 기획서 10.1절 씬 구성 중 1차 골격 5개(Bootstrap/Title/Dungeon/Settings/Loading) 생성
- 모든 씬이 프로젝트 기본 렌더 파이프라인(URP)으로 구성되도록 함
- 프로젝트 전역에서 유일한 `DontDestroyOnLoad` 루트인 `AppRoot` 구현
- 타입 기반으로 서비스를 등록/조회하는 `ServiceRegistry` 구현
- 로그, 씬 로드/언로드를 담당하는 최소 서비스(`LogService`, `SceneLoaderService`) 구현
- 화면 흐름 판단을 `AppRoot`가 아닌 `ApplicationFlow`(Application 계층)로 분리
- 씬 이름을 상수로 관리해 오탈자 방지
- 서비스 초기화 순서(로그→설정→로컬라이징→입력→오디오→저장→프로필→Addressables→Steam→Cloud→Title)의 뼈대를 코드에 명시
- 기존 Unity 템플릿 잔재(`SampleScene`) 정리

---

## 구현 내용

### 1. ServiceRegistry 추가

타입을 키로 서비스 인스턴스를 등록/조회하는 최소 서비스 로케이터를 구현했다.

```text
ServiceRegistry
├─ Register<TService>(instance)
├─ Get<TService>()
└─ TryGet<TService>(out service)
```

인터페이스 기반으로 서비스를 조회하도록 하여, 이후 각 서비스 구현이 바뀌어도 사용하는 쪽 코드가 영향받지 않게 했다.

---

### 2. LogService 추가

```text
ILogService
├─ Info(message)
├─ Warn(message)
└─ Error(message)
```

`LogService`는 `Debug.Log` 계열을 `[ProjectDelta]` 접두어로 감싸는 최소 구현이다. AppRoot 초기화 순서의 첫 단계로 등록된다.

---

### 3. SceneLoaderService 추가

```text
ISceneLoaderService
├─ LoadSingle(sceneName, onComplete)
├─ LoadAdditive(sceneName, onComplete)
└─ UnloadAdditive(sceneName, onComplete)
```

`SceneManager.LoadSceneAsync` / `UnloadSceneAsync`를 코루틴으로 감싸 비동기 완료 콜백을 제공한다. `SettingsScene`처럼 Additive로 열고 닫아야 하는 씬을 고려해 Single/Additive를 분리했다.

---

### 4. AppRoot 구현

```text
AppRoot (DontDestroyOnLoad, 유일 인스턴스)
├─ Awake: 중복 인스턴스 방지, DontDestroyOnLoad
├─ Start: 서비스 초기화 → ApplicationFlow.EnterTitle()
└─ Services: ServiceRegistry
```

서비스 초기화 순서는 기획서 10.1절의 순서를 그대로 코드에 남겨두었다.

```text
로그 초기화 (구현됨)
↓
설정 불러오기 (TODO, 3일차 이후)
↓
로컬라이징 초기화 (TODO)
↓
입력 초기화 (TODO)
↓
오디오 초기화 (TODO)
↓
저장 시스템 초기화 (TODO, 4~18일차)
↓
프로필 불러오기 (TODO)
↓
Addressables 초기화 (TODO, 3일차)
↓
Steam 초기화 (TODO)
↓
Cloud 상태 확인 (TODO)
↓
SceneLoaderService 등록 (구현됨)
↓
ApplicationFlow.EnterTitle()
```

아직 구현되지 않은 단계는 로그만 출력하는 자리로 남겨, 이후 일차에서 실제 구현으로 하나씩 채워나가는 구조로 만들었다.

---

### 5. ApplicationFlow 분리

`AppRoot`가 씬 전환 판단까지 직접 하지 않도록, Application 계층에 `ApplicationFlow`를 두었다.

```text
ApplicationFlow
└─ EnterTitle(): 로그 출력 → SceneLoaderService.LoadSingle(TitleScene)
```

`AppRoot`(Infrastructure 계층)는 서비스만 준비하고, 화면 흐름 결정은 `ApplicationFlow`(Application 계층)가 맡도록 계층 의존 방향(Presentation → Application → Domain)을 지켰다.

---

### 6. SceneNames 상수화

```text
SceneNames
├─ Bootstrap
├─ Title
├─ Prologue
├─ Dungeon
├─ Ending
├─ Settings
└─ Loading
```

기획서 10.1절의 전체 씬 목록을 미리 상수로 정의해, 이후 일차에서 Prologue/Ending 씬을 추가할 때도 이름을 새로 정의하지 않도록 했다.

---

### 7. URP 씬 5개 생성

`Assets/ProjectDelta/Scenes` 하위에 1차 골격 씬 5개를 생성했다.

```text
BootstrapScene.unity
TitleScene.unity
DungeonScene.unity
SettingsScene.unity
LoadingScene.unity
```

모든 씬은 프로젝트에 이미 설치된 URP 구성 요소를 그대로 사용한다.

```text
Main Camera
└─ UniversalAdditionalCameraData

Directional Light
└─ UniversalAdditionalLightData

Global Volume
└─ 기본 Post-processing Profile
```

`BootstrapScene`에는 추가로 `AppRoot` GameObject를 배치하고 `AppRoot.cs`를 연결했다.

---

### 8. Build Settings 등록

5개 씬을 Build Settings에 등록했다. `BootstrapScene`이 항상 0번(시작 씬)이 되도록 순서를 고정했다.

```text
0. BootstrapScene
1. TitleScene
2. DungeonScene
3. SettingsScene
4. LoadingScene
```

---

## 적용 중 발견된 문제 및 수정

### 9. Unity Hub 템플릿 잔재 정리

`Assets/Scenes/SampleScene.unity`는 Unity Hub가 프로젝트 생성 시 자동으로 넣어준 샘플 씬으로, 기획서의 씬 구성과 무관했다.

```text
삭제
→ Assets/Scenes/SampleScene.unity
→ Assets/Scenes/SampleScene.unity.meta
→ Assets/Scenes.meta (빈 폴더 정리)

수정
→ EditorBuildSettings.asset의 SampleScene 참조 제거
→ 이후 새 5개 씬으로 교체 등록
```

다만 이 씬은 Unity가 직접 생성한 검증된 URP 씬이었기 때문에, 삭제 전 커밋에서 원본 내용을 그대로 가져와 새 5개 씬의 기준 템플릿으로 재사용했다. 카메라·라이트의 URP 전용 컴포넌트(`UniversalAdditionalCameraData`, `UniversalAdditionalLightData`) GUID를 새로 추측하지 않고, 실제 설치된 패키지(`Library/PackageCache`)에서 직접 확인한 값을 그대로 사용해 참조 오류 위험을 없앴다.

---

## 현재 2일차 전체 흐름

```text
BootstrapScene 진입
↓
AppRoot.Awake (DontDestroyOnLoad)
↓
AppRoot.Start → 서비스 초기화 순서 실행
↓
LogService 등록
↓
(설정/로컬라이징/입력/오디오/저장/프로필/Addressables/Steam/Cloud — 자리만 존재)
↓
SceneLoaderService 등록
↓
ApplicationFlow.EnterTitle()
↓
TitleScene(빈 화면)으로 전환
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Infrastructure/ServiceRegistry.cs
Assets/ProjectDelta/Scripts/Infrastructure/ILogService.cs
Assets/ProjectDelta/Scripts/Infrastructure/LogService.cs
Assets/ProjectDelta/Scripts/Infrastructure/ISceneLoaderService.cs
Assets/ProjectDelta/Scripts/Infrastructure/SceneLoaderService.cs
Assets/ProjectDelta/Scripts/Infrastructure/AppRoot.cs
Assets/ProjectDelta/Scripts/Application/SceneNames.cs
Assets/ProjectDelta/Scripts/Application/ApplicationFlow.cs
Assets/ProjectDelta/Scenes/BootstrapScene.unity
Assets/ProjectDelta/Scenes/TitleScene.unity
Assets/ProjectDelta/Scenes/DungeonScene.unity
Assets/ProjectDelta/Scenes/SettingsScene.unity
Assets/ProjectDelta/Scenes/LoadingScene.unity
Devlogs/Day02/README.md
```

---

## 수정 파일

```text
ProjectSettings/EditorBuildSettings.asset
```

---

## 삭제 파일

```text
Assets/Scenes/SampleScene.unity
Assets/Scenes/SampleScene.unity.meta
Assets/Scenes.meta
```

---

## 최종 확인 항목

2일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- BootstrapScene 실행 시 Console에 초기화 로그가 순서대로 출력됨
- 초기화 완료 후 TitleScene으로 자동 전환됨
- 5개 씬 모두 URP 구성 요소(Camera/Light/Volume)를 가짐
- AppRoot가 씬 전환 후에도 파괴되지 않음(DontDestroyOnLoad)
- Build Settings에 BootstrapScene이 0번으로 등록됨
- 기존 SampleScene 관련 참조가 프로젝트에 남아있지 않음

---

## 다음 개발 방향

다음 3일차에서는 기획서 11.4절 일정표 기준 **Input System·TMP·Localization·Addressables 초기 설정**과 **공통 ID 규칙·Definition·DataRepository·Validator 구축**을 진행한다.

예정 흐름:

```text
Input System 액션 맵 정리 (Exploration/UI/Battle/AdultBattle/Map/Debug)
↓
TextMeshPro 기본 폰트·리소스 설정
↓
Localization 패키지 설치 및 기본 언어 테이블 구성
↓
Addressables 패키지 설치 및 그룹 초기 설정
↓
공통 데이터 ID 규칙 확정
↓
DataRepository 뼈대 구현
↓
데이터 Validator 뼈대 구현
↓
기준 빌드로 컴파일·실행 확인
```

이 시점까지 완료되면 AppRoot의 초기화 순서에서 "TODO"로 남아있던 로컬라이징·입력·Addressables 항목을 실제 구현으로 교체한다.
