# Project Delta - 1일차 개발일지

## 개발 주제

**Unity 프로젝트 기반 구축 및 Git 저장소 초기화**

이번 일차에서는 실제 콘텐츠 작업에 들어가기 전, 기획서 11.4절 개발 일정의 1일차 항목인 **Unity 프로젝트·URP·플랫폼 설정**과 **Git 저장소·.gitignore·폴더 규칙·필수 패키지 구축**을 진행했다.

기존에 Unity Hub로 생성된 템플릿 상태의 프로젝트를 기준으로, 버전 관리 체계를 갖추고 기획서 10.6절에서 정의한 폴더 구조를 실제로 반영했다.

---

## 개발 목표

- Unity 에디터 버전 확인 및 고정 여부 점검
- URP를 프로젝트 기본 렌더 파이프라인으로 명시적으로 지정
- 실행 플랫폼(Windows PC) 설정 상태 확인
- Git 저장소 초기화 및 GitHub 원격 저장소 연결
- Unity 표준 .gitignore 적용
- 기획서 10.6절 폴더 구조를 Assets 하위에 실제로 생성
- 현재 설치된 패키지 점검 및 부족한 필수 패키지 확인

---

## 구현 내용

### 1. Unity 프로젝트 버전 확인

`ProjectSettings/ProjectVersion.txt` 기준 에디터 버전은 `6000.3.21f1`이다.

이번 일차에서는 버전을 변경하지 않고 현재 버전을 그대로 유지했다.

```text
m_EditorVersion: 6000.3.21f1
```

LTS 여부는 Unity Hub에서 별도로 확인이 필요하며, 확인 후 개발 도중에는 메이저 버전을 임의로 바꾸지 않는다.

---

### 2. URP 기본 렌더 파이프라인 명시 지정

기존에는 `GraphicsSettings.asset`의 `m_CustomRenderPipeline`이 비어 있었고, `QualitySettings`의 품질 등급(Mobile/PC)별 참조에만 의존하는 상태였다.

```text
품질 등급 PC (index 1)
→ customRenderPipeline: PC_RPAsset

GraphicsSettings 기본값
→ 비어 있음 (fileID: 0)
```

프로젝트 전체 기준 렌더 파이프라인을 명확히 하기 위해 `GraphicsSettings.asset`의 기본값을 `PC_RPAsset`으로 직접 지정했다.

```text
GraphicsSettings.m_CustomRenderPipeline
→ PC_RPAsset 참조로 변경
```

---

### 3. 플랫폼 설정 확인

기획서 1.1.1절 기준 실행 플랫폼은 Windows PC, 유통 플랫폼은 Steam이다.

현재 프로젝트에는 빌드 타깃을 저장하는 `EditorUserBuildSettings.asset`이 아직 생성되지 않은 상태였다. 이 파일은 에디터를 열고 빌드 설정을 한 번 거쳐야 생성되므로, 이번 일차에서는 직접 만들지 않고 확인이 필요한 항목으로 남겨두었다.

```text
File > Build Settings
→ Platform: PC, Mac & Linux Standalone / Windows
→ 확인 필요 (에디터 진입 후)
```

---

### 4. Git 저장소 초기화

프로젝트 루트에 Git 저장소를 초기화하고 기본 브랜치를 `main`으로 설정했다.

```text
git init -b main
```

원격 저장소는 기존에 생성되어 있던 GitHub 저장소로 연결했다.

```text
origin → https://github.com/siwoo440/Project-Delta.git
```

---

### 5. .gitignore 적용

Unity 공식 gitignore 템플릿을 기준으로 `.gitignore`를 작성했다.

```text
/Library/
/Temp/
/Obj/
/Build/
/Builds/
/Logs/
/UserSettings/
/MemoryCaptures/
*.csproj / *.sln / *.user 등 IDE 생성 파일
```

에디터가 생성하는 캐시성 폴더와 IDE 부산물은 저장소에서 제외하고, `ProjectSettings`, `Packages`, `Assets`는 그대로 추적한다.

---

### 6. 기획서 10.6절 폴더 구조 반영

`Assets` 하위에 기획서에서 정의한 프로젝트 전용 폴더 구조를 생성했다.

```text
Assets/ProjectDelta
├─ Art (Characters, Dungeon, CG, UI, VFX)
├─ Audio (BGM, SFX, Environment, Voice)
├─ Data (Characters, Items, Skills, Events, Endings, Balance)
├─ Prefabs (Dungeon, UI, VFX)
├─ Scenes
├─ Scripts (Presentation, Application, Domain, Infrastructure, Data, Editor)
├─ Localization
├─ AddressableAssets
└─ Tests

Assets/ThirdParty
```

Git은 빈 폴더를 추적하지 않으므로, 각 말단 폴더에 `.gitkeep`을 두어 구조 자체를 저장소에 반영했다. 에디터를 열면 각 폴더에 `.meta` 파일이 자동 생성되며, 이는 다음 커밋에서 반영한다.

서드파티 에셋은 `Assets/ProjectDelta` 코드/데이터 폴더와 섞지 않고 `Assets/ThirdParty`에 분리한다.

---

### 7. 필수 패키지 점검

현재 설치된 패키지를 기준 기술 스택(9.6절/10.5절)과 비교했다.

```text
이미 설치됨
→ Input System
→ URP (Universal Render Pipeline)
→ Test Framework
→ uGUI (TextMeshPro 포함, Unity 6부터 통합)

추가 설치 필요
→ Addressables
→ Localization
```

Addressables와 Localization은 `manifest.json`을 직접 수정하지 않았다. 에디터 없이 버전을 손으로 지정하면 `6000.3.21f1`과 실제로 호환되는 버전인지 검증할 수 없어, 패키지 해석이 깨질 위험이 있기 때문이다. 두 패키지는 Package Manager UI에서 설치하는 것으로 남겨두었다.

---

## 적용 중 발견된 문제 및 수정

### 8. 원격 저장소 인증 상태 확인

원격 연결 직후 `git push` 결과를 확인하는 과정에서, 별도 설정 없이도 기존에 구성되어 있던 자격 증명으로 푸시가 그대로 성공하는 것을 확인했다.

이 저장소는 이번 일차에 처음 생성된 단일 커밋 상태였기 때문에 결과적으로 문제는 없었으나, 공개 저장소에 대한 push는 사전에 확인을 받고 진행했어야 하는 작업이었다. 이후에는 push 여부를 먼저 확인받고 진행한다.

---

## 현재 1일차 전체 흐름

```text
Unity 템플릿 프로젝트 확인
↓
에디터 버전 확인 (변경 없음)
↓
URP 기본 파이프라인 명시 지정
↓
플랫폼 설정 상태 확인 (에디터 확인 필요 항목으로 분리)
↓
Git 저장소 초기화
↓
.gitignore 적용
↓
기획서 10.6절 폴더 구조 생성
↓
필수 패키지 점검 (Addressables·Localization은 Editor 설치 대기)
↓
초기 커밋
↓
원격 저장소 연결 및 반영
```

---

## 생성 파일

```text
.gitignore
Assets/ProjectDelta/ (하위 전체 폴더 + .gitkeep)
Assets/ThirdParty/.gitkeep
Devlogs/Day01/README.md
```

---

## 수정 파일

```text
ProjectSettings/GraphicsSettings.asset
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

1일차 완료 기준은 다음과 같다.

- Unity 에디터 버전 확인 완료
- URP가 프로젝트 기본 렌더 파이프라인으로 지정됨
- Git 저장소 초기화 및 원격 연결 완료
- .gitignore가 Library/Temp/Logs/UserSettings를 제외함
- 기획서 10.6절 폴더 구조가 Assets 하위에 존재함
- 기존 필수 패키지(Input System, URP, Test Framework) 설치 확인
- 추가 필수 패키지(Addressables, Localization) 미설치 상태 및 설치 방법 확인
- 플랫폼(Windows Standalone) 설정은 에디터에서 별도 확인 필요 상태로 남음

---

## 다음 개발 방향

다음 2일차에서는 기획서 10.1절 기준 **메인 씬 구조(Bootstrap/Title/Game/Settings/Loading)**와 **AppRoot·ServiceRegistry·SceneLoader** 기본 구조를 구현한다.

예정 흐름:

```text
BootstrapScene
↓
AppRoot 초기화
↓
ServiceRegistry 등록
↓
SceneLoader를 통한 씬 전환
↓
TitleScene 진입
```

이후에는 Input System·TMP·Localization·Addressables 초기 설정과 공통 ID 규칙·DataRepository 구축으로 이어간다.
