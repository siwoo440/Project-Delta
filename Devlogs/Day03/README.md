# Project Delta - 3일차 개발일지

## 개발 주제

**Input System · TMP · Localization · Addressables 초기 설정 및 공통 데이터 계층 구축**

이번 일차에서는 371일 일정표 3일차 항목대로, 남아있던 패키지(Addressables, Localization)를 설치하고 Input Action Map을 기획서 10.1절 구성으로 재정리했다. 동시에 10.2절 기준 공통 ID 규칙과 `DataRepository`·`Validator` 뼈대를 구현해, 이후 일차부터 실제 콘텐츠 데이터를 붙일 수 있는 기반을 마련했다.

---

## 개발 목표

- Addressables, Localization 패키지 설치 (버전은 설치된 에디터가 권장하는 값으로 고정)
- Input Action Map을 기획서 10.1절 6종(Exploration/UI/Battle/AdultBattle/Map/Debug)으로 재구성
- TextMeshPro 기본 리소스 임포트
- `AppRoot` 초기화 순서에서 Localization·Input·Addressables를 실제 구현으로 교체
- 공통 ID 규칙 정리 및 최소 표본 Definition(Monster/Item) 구현
- `DataRepository`로 ID 기반 조회, 없으면 명확한 오류 발생
- `DataValidator`로 중복/빈 ID 검사
- 기준 빌드(BootstrapScene → TitleScene 정상 전환) 확인

---

## 구현 내용

### 1. Addressables·Localization 패키지 설치

`manifest.json`에 두 패키지를 추가했다.

```text
com.unity.addressables: 2.9.1
com.unity.localization: 1.5.8
```

버전을 임의로 추측하지 않고, 로컬에 설치된 Unity 에디터(`6000.3.21f1`)가 내부적으로 가진 "이 에디터 빌드의 권장 패키지 버전" 정보에서 직접 확인한 값을 사용했다. `packages-lock.json`에 두 패키지와 하위 의존성(`com.unity.nuget.newtonsoft-json` 등)이 정상적으로 해석되어 기록된 것으로 설치를 확인했다.

TextMeshPro는 별도 패키지로 설치하지 않았다. Unity 6부터 `com.unity.ugui`에 통합되어 있고, 별도 패키지로 추가하면 deprecated 경고만 발생하기 때문이다.

---

### 2. Input Action Map 6종 재구성

`Assets/InputSystem_Actions.inputactions`의 템플릿 기본 구성(`Player`, `UI`)을 기획서 10.1절 구성으로 교체했다.

```text
Exploration (신규, 빈 맵)
UI (기존 바인딩 유지: Navigate/Submit/Cancel 등)
Battle (신규, 빈 맵)
AdultBattle (신규, 빈 맵)
Map (신규, 빈 맵)
Debug (신규, 빈 맵)
```

실제 이동·전투 키 바인딩은 371일 표 기준 20일차 이후(WASD 이동, Q/E 회전 등)에 채워지므로, 이번 일차에는 맵 구조만 먼저 만들었다.

---

### 3. TextMeshPro 리소스 임포트

`Window > TextMeshPro > Import TMP Essential Resources`로 기본 폰트·리소스를 가져왔다.

---

### 4. IInputService 구현

```text
IInputService
└─ SetActiveMap(mapName)
```

이전에 활성화된 맵을 비활성화한 뒤 새 맵을 활성화하는 방식으로, 탐험과 전투 입력이 동시에 켜지는 상황을 코드 수준에서 막았다 (기획서 10.1절 "탐험과 전투 입력이 동시에 활성화되지 않게 한다").

---

### 5. ILocalizationService / IAddressableService 구현

```text
ILocalizationService.InitializeRoutine()
→ LocalizationSettings.InitializationOperation 대기

IAddressableService.InitializeRoutine()
→ Addressables.InitializeAsync() 대기
```

둘 다 `IEnumerator`를 직접 반환하는 방식으로 만들어, `AppRoot`의 초기화 코루틴 안에서 `yield return`으로 그대로 이어붙일 수 있게 했다.

---

### 6. AppRoot 초기화 순서 갱신

2일차에 로그만 찍던 자리 중 3개를 실제 구현으로 교체했다.

```text
로그 초기화 (구현됨)
↓
설정 불러오기 (TODO, 4일차 이후)
↓
Localization 초기화 (구현됨)
↓
Input 초기화 (구현됨, UI 맵으로 시작)
↓
오디오 초기화 (TODO)
↓
저장 시스템 초기화 (TODO, 4~18일차)
↓
프로필 불러오기 (TODO)
↓
Addressables 초기화 (구현됨)
↓
Steam 초기화 (TODO)
↓
Cloud 상태 확인 (TODO)
↓
SceneLoader 등록 (구현됨)
↓
ApplicationFlow.EnterTitle()
```

`AppRoot`에 `InputActionAsset` 참조 필드를 추가하고, `BootstrapScene`의 AppRoot 컴포넌트에 기존 `InputSystem_Actions` 에셋을 연결했다.

---

### 7. 공통 ID 규칙 정리

```text
<카테고리>_<이름>
예: MON_SLIME, ITEM_HEAL_SMALL
```

표시 이름과 분리하고, 출시 후 표시 이름이 바뀌어도 ID는 유지한다는 원칙을 `DefinitionBase`에 주석으로 남겨두었다.

---

### 8. Definition·DataRepository·Validator 뼈대

기획서 10.2절이 정의하는 18종 Definition을 한 번에 다 만들지 않고, 파이프라인이 실제로 도는지 검증할 최소 표본 2종만 먼저 구현했다.

```text
DefinitionBase (abstract ScriptableObject)
├─ MonsterDefinition
└─ ItemDefinition

DataRepository
├─ Monsters: DefinitionTable<MonsterDefinition>
├─ Items: DefinitionTable<ItemDefinition>
├─ GetMonster(id)
└─ GetItem(id)

DataValidator
└─ 중복 ID / 빈 ID 검사
```

`DataRepository.GetMonster("MON_SLIME")` 형태로 조회하며, 존재하지 않는 ID를 조회하면 임의 기본값 대신 예외를 던지도록 했다 (기획서: "데이터가 없으면 임의 기본값을 반환하지 않고 명확한 오류를 발생시킨다").

누락된 로컬라이징 키, 확률 합계 오류 등 나머지 검증 항목은 해당 데이터(스킬, 이벤트, 보상 테이블)가 실제로 생기는 이후 일차에 `DataValidator`에 추가한다.

---

## 적용 중 발견된 문제 및 수정

이번 일차에는 별도로 발견된 오류는 없었다. Addressables/Localization 설정 마법사(Addressables Groups, Project Settings > Localization)는 Editor GUI에서 직접 실행해, 자동 생성되는 `Assets/AddressableAssetsData`와 `Assets/Localization Settings.asset`이 프로젝트에 정상적으로 반영된 것을 확인했다.

---

## 현재 3일차 전체 흐름

```text
BootstrapScene 진입
↓
AppRoot.Awake (DontDestroyOnLoad)
↓
로그 서비스 준비
↓
Localization 초기화 대기
↓
Input 서비스 준비 (UI 맵 활성화)
↓
(오디오/저장/프로필 — 자리만 존재)
↓
Addressables 초기화 대기
↓
(Steam/Cloud — 자리만 존재)
↓
SceneLoader 등록
↓
TitleScene으로 전환
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Infrastructure/ILocalizationService.cs
Assets/ProjectDelta/Scripts/Infrastructure/LocalizationService.cs
Assets/ProjectDelta/Scripts/Infrastructure/IAddressableService.cs
Assets/ProjectDelta/Scripts/Infrastructure/AddressableService.cs
Assets/ProjectDelta/Scripts/Infrastructure/IInputService.cs
Assets/ProjectDelta/Scripts/Infrastructure/InputService.cs
Assets/ProjectDelta/Scripts/Application/InputMapNames.cs
Assets/ProjectDelta/Scripts/Data/DefinitionBase.cs
Assets/ProjectDelta/Scripts/Data/MonsterDefinition.cs
Assets/ProjectDelta/Scripts/Data/ItemDefinition.cs
Assets/ProjectDelta/Scripts/Data/DefinitionTable.cs
Assets/ProjectDelta/Scripts/Data/DataRepository.cs
Assets/ProjectDelta/Scripts/Data/IDataValidator.cs
Assets/ProjectDelta/Scripts/Data/DataValidator.cs
Assets/ProjectDelta/Scripts/Data/DataValidationReport.cs
Devlogs/Day03/README.md

(Editor가 자동 생성)
Assets/AddressableAssetsData/
Assets/Localization Settings.asset
Assets/TextMesh Pro/
```

---

## 수정 파일

```text
Packages/manifest.json
Assets/InputSystem_Actions.inputactions
Assets/ProjectDelta/Scripts/Infrastructure/AppRoot.cs
Assets/ProjectDelta/Scenes/BootstrapScene.unity
ProjectSettings/EditorBuildSettings.asset (Editor가 Addressables/Localization 설정 참조 자동 반영)
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

3일차 완료 기준은 다음과 같다.

- Addressables, Localization 패키지가 `packages-lock.json`에 정상 해석됨
- Input Action Asset이 Exploration/UI/Battle/AdultBattle/Map/Debug 6개 맵으로 구성됨
- TMP Essential Resources 임포트 완료
- Unity 컴파일 오류 없음
- BootstrapScene 실행 시 Localization·Input·Addressables 초기화 로그가 순서대로 출력됨
- 초기화 완료 후 TitleScene으로 정상 전환됨
- `DataRepository.GetMonster` / `GetItem`이 존재하지 않는 ID에 대해 예외를 던짐
- `DataValidator`가 중복/빈 ID를 검출함

---

## 다음 개발 방향

다음 4일차부터는 371일 표 기준 **저장·런타임** 구간(4~18일차)이 시작된다. 4일차에는 **ProfileData 구조와 신규 프로필 생성 흐름**을 구현한다.

예정 흐름:

```text
ProfileData 구조 정의 (영구 프로필 DTO)
↓
신규 프로필 생성 흐름
↓
프로필 기본값 초기화
↓
AppRoot의 "프로필 불러오기 (TODO)" 자리를 실제 구현으로 교체 준비
```

이후 5~18일차에서 RunData, SettingsData, SaveService, 저장 슬롯, 자동/수동 저장, 백업·복구까지 저장 시스템 전체를 순서대로 채운다.
