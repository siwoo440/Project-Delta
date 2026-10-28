---
# 프로젝트 델타

2D 탑뷰 격자 탐험과 별도 턴제 전투, 선택·관계 중심 로그라이트 프로젝트.

기존 기획의 기능과 규칙을 기준으로 코드를 새로 작성한다. 1일차 범위는 기본 실행 흐름과 입력 분리다.

---
## 개발 환경

- Unity 6000.3.21f1
- URP 2D, Unity Input System, uGUI
- Windows 64비트

---
## 실행

Unity Hub에서 저장소 폴더를 프로젝트로 추가하고 `Assets/ProjectDelta/Scenes/BootstrapScene.unity`를 열어 Play를 실행한다.

타이틀에서 새 게임을 선택하면 던전에 진입한다. 던전에서 Esc 또는 게임패드의 취소 버튼으로 타이틀에 복귀한다. 1일차에는 격자·플레이어 표시까지 제공하며 이동·전투·저장은 후속 작업이다.

---
## 코드 구성

| 폴더 | 역할 |
|---|---|
| Assets/ProjectDelta/Core | Unity 객체에 의존하지 않는 게임 상태 규칙 |
| Assets/ProjectDelta/Runtime | 초기화·씬 전환·입력·화면 표시 |
| Assets/ProjectDelta/Editor | 기본 씬 생성·Windows 빌드 |
| Assets/ProjectDelta/Tests | EditMode·PlayMode 검증 |
| docs/development | 일차별 작업과 확인 결과 |

---
## 검증과 빌드

PowerShell에서 `Tools/Run-DayOneChecks.ps1 -Check EditMode`, `-Check PlayMode`, `-Check Build`로 실행한다. 편집기 경로가 다르면 `-UnityPath`로 지정한다. 실제 Windows 테스트 실행 파일 검증은 `-Check PlayerTests`를 사용한다.

기본 씬 재생성은 Unity 메뉴 `Project Delta/1일차/기본 씬 재생성`으로 실행한다. 생성기는 해당 기본 씬 세 개를 덮어쓰므로 씬 편집 후에는 변경을 먼저 보존한다.

검증 기록은 `Artifacts`, Windows 빌드는 `Builds/Windows`에 생성되며 Git에서 제외한다.
