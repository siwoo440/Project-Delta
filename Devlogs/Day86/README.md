# Project Delta 86일차 개발일지

---
## 개발 정보

- 개발 일자: 2026-08-26
- 최신 커밋: `b065aa9497693ae7a038063c6d381d15d5d687bb`
- 기준 커밋: `2ba1713d7605753e686ef39b0c6661445fb19ced`
- 현재 커밋 제목: `a`
- 개발 주제: 전투 1×·2× 속도 전환 및 행동·피해 연출 배속 적용

---
# 개발 목표

85일차에서 F1 디버그 전투 로그를 추가한 뒤, 86일차에서는 전투 규칙과 계산 결과를 변경하지 않고 전투의 시각적 진행 속도만 조절할 수 있는 1×·2× 전환 기능을 추가했다.

주요 목표는 다음과 같다.

- 전투 시작 시 기본 1×
- 전투 중 1× / 2× 전환
- 전투 속도 버튼 런타임 자동 생성
- 적 연속 행동 대기 시간 배속
- 행동 Bump 연출 배속
- 피해 숫자 표시 및 Fade 배속
- 명중·피해·RNG·행동 순서·보상 등 전투 계산에는 영향 없음
- Scene / Prefab 수동 연결 최소화

---
# 주요 개발 내용

---
## 1. BattleSpeedState 추가

전투 속도 상태를 전투 계산 로직과 분리하기 위해 `BattleSpeedState`를 추가했다.

지원 속도:

- Normal: `1×`
- Fast: `2×`

주요 기능:

- 현재 배속 조회
- 현재 표시 문자열 제공
- 1× / 2× 토글
- 1× 초기화
- 기본 연출 시간을 현재 배속에 맞게 변환

배속 시간 계산은 다음 구조를 사용한다.

`실제 시간 = 기본 시간 / 현재 배속`

예:

- 0.45초 / 1× = 0.45초
- 0.45초 / 2× = 0.225초

---
## 2. 전투 속도 버튼 런타임 자동 생성

`BattleSpeedRuntimeHud`를 추가했다.

Scene이나 Prefab에 별도 버튼을 직접 배치하지 않아도 런타임에 자동 생성된다.

버튼은 전투가 존재하는 동안에만 표시되며 화면 오른쪽 아래에 배치된다.

초기 표시:

`1×`

한 번 클릭:

`2×`

다시 클릭:

`1×`

게임 런타임이 새로 시작되면 기본 1×로 초기화된다.

---
## 3. 적 연속 행동 간격 배속

기존 적 행동 사이의 기본 대기 시간은 0.45초였다.

86일차부터 `BattleSpeedState.ScaleDuration()`을 적용한다.

1×:

`0.45초`

2×:

`0.225초`

행동 순서와 전투 계산은 그대로 유지되고 다음 적 행동으로 넘어가는 화면상 대기 시간만 빨라진다.

---
## 4. 캐릭터 행동 Bump 배속

84일차에 추가된 참가자 초상화 Bump 연출에도 전투 배속을 연결했다.

기본 Bump 시간:

`0.12초`

1×:

`0.12초`

2×:

`0.06초`

따라서 빠른 전투에서도 행동 연출이 전투 진행 속도와 맞게 동작한다.

---
## 5. 피해 숫자 표시 시간 배속

피해 피드백은 기존에 실시간 기준으로 표시되므로 별도 배속 보정이 필요했다.

기본 설정:

- 피해 숫자 표시: 0.65초
- Fade: 0.25초

2×에서는:

- 피해 숫자 표시: 약 0.325초
- Fade: 약 0.125초

가 되도록 `BattleSpeedState.ScaleDuration()`을 적용했다.

---
## 6. 전투 계산과 배속 분리

86일차 배속 시스템은 전투 규칙을 수정하지 않는다.

배속과 무관하게 동일해야 하는 항목:

- 명중률
- 피해량
- 치명타 및 기타 확률
- Combat RNG
- 행동 순서
- 상태이상 결과
- 라운드 진행 결과
- 도주 판정
- 보상
- 85일차 전투 로그의 행동 순서

배속은 화면상 시간과 대기 시간만 변경한다.

---
## 7. 자동 설치 스크립트 추가

`Day86BattleSpeedInstaller`를 추가해 최신 85일차 소스에 필요한 수정 지점을 자동 적용할 수 있도록 했다.

자동 수정 대상:

- `ExplorationMonsterEncounterController.cs`
- `BattleParticipantSlotView.cs`

수정 대상은 정확한 기존 코드 문자열을 확인한 후 치환하도록 구성되어 있어 예상한 최신 소스와 다르면 오류를 출력하고 임의 수정하지 않는다.

수동 메뉴도 제공한다.

`Project Delta → 86일차 → 86일차 전투 1x 2x 속도 적용`

---
## 8. Day86BattleSpeedInstaller 컴파일 오류 수정

첫 적용 후 다음 컴파일 오류를 확인했다.

`CS0234: ProjectDelta.Application 네임스페이스에 dataPath가 존재하지 않음`

원인은 `ProjectDelta.Editor` 네임스페이스에서 작성한:

`Application.dataPath`

표현이 `UnityEngine.Application`이 아닌 `ProjectDelta.Application`으로 해석된 것이었다.

수정:

`UnityEngine.Application.dataPath`

로 타입을 명확히 지정했다.

최신 86일차 커밋에는 이 수정이 반영되어 있다.

---
# 테스트

`BattleSpeedStateTests`를 추가했다.

검증 항목:

- Reset 시 1×로 복귀
- 1회 Toggle 시 2×
- 2회 Toggle 시 다시 1×
- 1×에서 기본 시간이 그대로 유지되는지
- 2×에서 시간이 절반이 되는지
- 0 이하의 시간이 안전하게 0으로 처리되는지

---
# 변경 파일

85일차 기준 86일차 변경 범위는 다음과 같다.

---
## 생성

- `Assets/ProjectDelta/Scripts/Application/BattleSpeedState.cs`
- `Assets/ProjectDelta/Scripts/Application/BattleSpeedState.cs.meta`
- `Assets/ProjectDelta/Scripts/Editor/Day86BattleSpeedInstaller.cs`
- `Assets/ProjectDelta/Scripts/Editor/Day86BattleSpeedInstaller.cs.meta`
- `Assets/ProjectDelta/Scripts/Presentation/BattleSpeedRuntimeHud.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleSpeedRuntimeHud.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/BattleSpeedStateTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleSpeedStateTests.cs.meta`

---
## 수정

- `Assets/ProjectDelta/Scripts/Presentation/ExplorationMonsterEncounterController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleParticipantSlotView.cs`

---
## 삭제

없음.

---
# 최종 동작 흐름

게임 실행

→ `BattleSpeedState` 1× 초기화

→ `BattleSpeedRuntimeHud` 자동 생성

→ 탐험 중 속도 버튼 숨김

→ 전투 시작

→ 오른쪽 아래 `1×` 버튼 표시

→ 적 행동 간격 0.45초

→ 행동 Bump 기본 속도

→ 피해 표시 기본 속도

→ `1×` 버튼 클릭

→ `2×` 표시

→ 적 행동 간격 0.225초

→ Bump 시간 절반

→ 피해 표시 및 Fade 시간 절반

→ 전투 계산 결과와 행동 순서는 동일하게 유지

---
# 검토 상태

최신 `main` 커밋:

`b065aa9497693ae7a038063c6d381d15d5d687bb`

현재 커밋 제목:

`a`

85일차 커밋:

`2ba1713d7605753e686ef39b0c6661445fb19ced`

대비 1개 커밋 앞선 상태로 확인했다.

GitHub 소스 기준으로 확인한 내용:

- 전투 속도 상태 추가
- 1× / 2× 토글 구현
- 런타임 전투 속도 버튼 추가
- 적 행동 대기 시간 배속 적용
- 참가자 Bump 연출 배속 적용
- 피해 숫자 표시 및 Fade 배속 적용
- EditMode 테스트 추가
- `UnityEngine.Application.dataPath` 네임스페이스 충돌 수정 반영
- 기존 전투 계산 시스템에 배속 수치 직접 반영 없음
- 삭제 파일 없음

GitHub Commit Status와 연결된 Workflow Run은 등록되어 있지 않다.

따라서 GitHub 소스 검토 기준으로 86일차를 중단해야 할 추가 차단 문제는 확인되지 않았다.

실제 Unity 전체 컴파일 및 Test Runner 결과는 로컬 Unity 실행 결과가 최종 기준이다.

---
# 86일차 완료 요약

86일차에서는 전투의 규칙이나 결과를 바꾸지 않고 전투 진행의 체감 속도를 바꿀 수 있는 1×·2× 전환 시스템을 구현했다.

전투 속도는 별도 상태로 관리하고 다음 화면 연출에만 적용했다.

- 적 행동 사이의 대기
- 참가자 행동 Bump
- 피해 숫자 표시
- 피해 숫자 Fade

전투 중에는 오른쪽 아래 버튼으로 즉시 1× / 2×를 변경할 수 있다.

이를 통해 다수 적과의 전투가 길어졌을 때 플레이어가 빠르게 진행할 수 있는 기반을 마련했다.
