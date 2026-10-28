---
# 프로젝트 델타

2D 탑뷰 격자 탐험과 별도 턴제 전투, 선택·관계 중심 로그라이트 프로젝트.

기존 기획의 기능과 규칙을 기준으로 코드를 새로 작성한다. 현재는 기본 실행 흐름과 2D 격자 이동·장애물 판정·두 시험 방의 왕복 이동을 제공한다.

---
## 개발 환경

- Unity 6000.3.21f1
- URP 2D, Unity Input System, uGUI
- Windows 64비트

---
## 실행

Unity Hub에서 저장소 폴더를 프로젝트로 추가하고 `Assets/ProjectDelta/Scenes/BootstrapScene.unity`를 열어 Play를 실행한다.

타이틀에서 새 게임을 선택하면 던전에 진입한다. 던전에서 WASD·방향키·게임패드 방향 입력으로 한 칸씩 이동하고, Esc 또는 게임패드의 취소 버튼으로 타이틀에 복귀한다. 흰 화살표는 바라보는 방향, 붉은 칸은 장애물, 노란 화살표는 문이다. 문 앞에서 문 방향으로 마지막 이동을 마친 뒤 E 또는 게임패드의 남쪽 버튼을 누르면 다른 방으로 이동한다. 도착 직후 돌아가려면 방 안쪽으로 한 칸 이동한 뒤 문 쪽으로 돌아와 상호작용한다. 전투·저장은 후속 작업이다.

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

PowerShell에서 `Tools/Run-DayThreeChecks.ps1 -Check EditMode`, `-Check PlayMode`, `-Check Build`로 실행한다. 편집기 경로가 다르면 `-UnityPath`로 지정한다. 실제 Windows 테스트 실행 파일 검증은 `-Check PlayerTests`를 사용한다.

기본 씬 재생성은 Unity 메뉴 `Project Delta/1일차/기본 씬 재생성`으로 실행한다. 생성기는 해당 기본 씬 세 개를 덮어쓰므로 씬 편집 후에는 변경을 먼저 보존한다.

기존 던전에 이동 요소를 다시 연결하는 메뉴는 `Project Delta/2일차/던전 이동 구성`이다. 바닥이 완전히 비어 있을 때만 시험 바닥·장애물을 생성하며, 기존 바닥의 빈 칸과 사용자 장애물은 보존한다. 이전 시험 장애물과 정확히 일치하는 배치는 밝은 전용 타일로 교체한다. 다른 열린 씬의 미저장 변경은 저장 또는 취소를 확인한다.

두 시험 방을 연결하는 메뉴는 `Project Delta/3일차/시험 방 연결`이다. 기존 객체나 문 위치의 타일과 충돌하면 구성을 중단한다. 다른 열린 씬의 미저장 변경은 저장 또는 취소를 확인한다.

검증 기록은 `Artifacts`, 3일차 Windows 빌드는 `Builds/Day03`에 생성되며 Git에서 제외한다. 기존 빌드 경로는 `Builds/Windows`와 `Builds/Day02`다.
