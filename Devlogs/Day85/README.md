# Project Delta 85일차 개발일지

---
## 개발 정보

- 개발 일자: 2026-08-26
- 최신 커밋: `dbd83148c1347432b5b8e45b8d2db3a03c302788`
- 기준 커밋: `7e1c529fa74f784e17143fb6f4f0846c9d93ea62`
- 현재 커밋 제목: `a`
- 개발 주제: F1 디버그 전투 로그 및 런타임 Canvas 오버레이 구현

---
# 개발 목표

84일차에서 전투 HUD의 현재 상태 표시를 정리한 뒤, 85일차에서는 전투 진행 과정을 확인할 수 있는 디버그 전투 로그를 추가했다.

정식 플레이 UI가 아니라 개발·검증용 기능으로 구성하며 다음 조건을 목표로 했다.

- 게임 시작 시 로그 창은 기본 OFF
- F1 키로 로그 창 표시 / 숨김
- 화면 오른쪽 위에 표시
- Canvas 기반 UI 사용
- Scene / Prefab 수동 연결 없이 런타임 자동 생성
- 실제 전투 행동 결과 로그 누적
- 같은 행동의 프레임 중복 기록 방지
- 새 전투 시작 시 이전 로그 초기화
- 지나치게 긴 전투에서 로그 무한 증가 방지

---
# 주요 개발 내용

---
## 1. BattleDebugLogBuffer 추가

전투 로그의 저장과 표시 문자열 생성을 담당하는 `BattleDebugLogBuffer`를 추가했다.

주요 역할:

- 로그 줄 누적
- 현재 행동 Sequence 기억
- 같은 Sequence 중복 추가 차단
- 라운드 변경 시 구분선 추가
- 행동자와 Command 정보 포함
- 최대 로그 수 제한
- 화면에 표시할 최근 로그 문자열 생성

기본 최대 보관 줄 수는 200줄이다.

---
## 2. 행동 Sequence 기반 중복 방지

전투 HUD는 매 프레임 갱신되므로 단순히 최근 결과를 읽으면 같은 행동이 반복 기록될 수 있다.

이를 방지하기 위해 `LastActionSequence`를 기준으로 이미 기록한 행동인지 판별한다.

동일한 Sequence는 다시 입력되어도 로그에 추가되지 않는다.

예:

`Sequence 3 → 기록`

같은 프레임 이후:

`Sequence 3 → 무시`

다음 행동:

`Sequence 4 → 기록`

---
## 3. 라운드 구분 표시

새로운 라운드의 첫 행동이 기록될 때 라운드 구분선을 자동 추가한다.

예:

`--- Round 1 ---`

`[R1] [PLAYER] [Attack] ...`

`--- Round 2 ---`

`[R2] [ENEMY] [Attack] ...`

현재 라운드를 로그에서 빠르게 구분할 수 있도록 구성했다.

---
## 4. 새 전투 로그 초기화

새 `BattleContext`가 감지되면 이전 전투 로그를 제거하고 새 전투 기록을 시작한다.

새 전투 시작 시:

`=== Battle Start ===`

문구를 추가한다.

이전 전투의 로그가 다음 전투에 섞이지 않도록 처리했다.

---
## 5. 시작 프레임 첫 행동 보존 처리

새 전투가 시작되는 같은 프레임에 적이 먼저 행동하는 경우 첫 행동이 누락될 수 있는 상황을 고려했다.

현재 `LastActingParticipant`가 새 `BattleContext`에 실제로 포함된 참가자인지 확인해:

- 현재 전투의 첫 행동이면 기록
- 이전 전투에 남아 있던 결과이면 무시

하도록 분기했다.

이를 통해 새 전투 시작 직후 행동과 이전 전투 잔여 결과를 구분한다.

---
## 6. BattleDebugLogOverlay 추가

전투 로그를 화면에 표시하는 `BattleDebugLogOverlay`를 추가했다.

이 컴포넌트는 Scene 또는 Prefab에 미리 배치하지 않아도 된다.

`RuntimeInitializeOnLoadMethod`를 사용하여 첫 Scene 로드 이후 자동으로 생성된다.

생성된 오브젝트는 `DontDestroyOnLoad`로 유지된다.

---
## 7. 런타임 Canvas 자동 생성

오버레이가 실행될 때 다음 UI를 코드에서 자동 생성한다.

`BattleDebugLogCanvas`

`└─ BattleDebugLogPanel`

`   └─ BattleDebugLogText`

Canvas 설정:

- Render Mode: Screen Space - Overlay
- Canvas Scaler: Scale With Screen Size
- Reference Resolution: 1920 × 1080
- Match Width Or Height: 0.5
- Sorting Order: 10000

따라서 기존 Scene의 Canvas나 Inspector 참조를 수정하지 않아도 된다.

---
## 8. 오른쪽 위 로그 패널

로그 패널은 화면 오른쪽 위에 고정된다.

기본 크기:

- Width: 620
- Height: 420
- Margin: 20

패널은 반투명 검정 배경을 사용하며 마우스 입력을 가로채지 않도록 Raycast를 비활성화했다.

최근 로그는 최대 18줄을 화면에 표시한다.

---
## 9. F1 토글 기능

Unity 새 Input System의 `Keyboard.current.f1Key`를 사용한다.

게임 시작 시:

`isVisible = false`

상태이므로 로그 창은 보이지 않는다.

F1 입력:

`OFF → ON`

다시 F1 입력:

`ON → OFF`

형태로 동작한다.

---
## 10. 기존 전투 결과 재사용

85일차 로그 시스템은 피해량이나 명중 결과를 다시 계산하지 않는다.

기존 `ExplorationMonsterEncounterController`가 제공하는:

- `LastActionSequence`
- `LastActingParticipant`
- `LastBattleActionResult`
- `BattleRoundNumber`

정보를 읽어 기록한다.

`BattleActionResult.Logs`의 실제 전투 결과 문자열을 그대로 누적하기 때문에 기존 전투 계산 로직과 별도의 판정이 발생하지 않는다.

---
# 테스트

---
## BattleDebugLogBufferTests

EditMode 테스트를 추가했다.

검증 항목:

- 첫 행동이 정상적으로 기록되는지
- 첫 행동에서 라운드 구분선이 추가되는지
- 같은 Sequence가 중복 기록되지 않는지
- 새 라운드에서 새 구분선이 추가되는지
- 최대 줄 수 초과 시 오래된 로그가 제거되는지
- 전투 시작과 같은 프레임의 첫 행동이 보존되는지
- 이전 전투의 잔여 Sequence가 무시되는지
- 새 전투 시작 시 이전 로그가 초기화되는지

---
# 변경 파일

84일차 기준 총 6개 파일이 새로 추가되었다.

---
## 생성

- `Assets/ProjectDelta/Scripts/Presentation/BattleDebugLogBuffer.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleDebugLogBuffer.cs.meta`
- `Assets/ProjectDelta/Scripts/Presentation/BattleDebugLogOverlay.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleDebugLogOverlay.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/BattleDebugLogBufferTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleDebugLogBufferTests.cs.meta`

---
## 수정

없음.

---
## 삭제

없음.

---
# 최종 동작 흐름

게임 실행

→ `BattleDebugLogOverlay` 자동 생성

→ 런타임 Canvas 생성

→ 로그 패널 기본 OFF

→ 전투 시작 감지

→ 이전 로그 초기화

→ `=== Battle Start ===` 기록

→ `LastActionSequence` 변경 감지

→ `BattleActionResult.Logs` 누적

→ 새 라운드 구분선 추가

→ F1 입력

→ 오른쪽 위 로그 패널 표시

→ 다시 F1 입력

→ 로그 패널 숨김

---
# 검토 상태

최신 `main` 커밋은:

`dbd83148c1347432b5b8e45b8d2db3a03c302788`

이며 현재 커밋 메시지는 `a`다.

84일차 커밋:

`7e1c529fa74f784e17143fb6f4f0846c9d93ea62`

바로 다음 1개 커밋으로 확인했다.

GitHub 소스 기준으로 다음 내용을 확인했다.

- 85일차 신규 파일 6개 추가
- 기존 파일 수정 없음
- 기존 파일 삭제 없음
- F1 토글 입력 반영
- 초기 표시 상태 OFF
- Screen Space Overlay Canvas 자동 생성
- 오른쪽 위 패널 배치
- Scene / Prefab 수동 연결 불필요
- `LastActionSequence` 기반 중복 방지
- 새 전투 로그 초기화
- 시작 프레임 첫 행동 보존 처리
- 최대 로그 줄 수 제한
- EditMode 테스트 추가

GitHub Commit Status와 연결된 자동 CI 결과 및 Workflow Run은 등록되어 있지 않다.

따라서 GitHub 소스 검토 기준으로 85일차를 중단해야 할 차단 문제는 확인되지 않았다.

실제 Unity 컴파일과 Test Runner 결과는 로컬 Unity 실행 결과가 최종 기준이다.

---
# 85일차 완료 요약

85일차에서는 기존 전투 결과를 다시 계산하지 않고 실제 `BattleActionResult`를 읽어 누적하는 개발용 전투 로그 시스템을 추가했다.

로그 UI는 정식 HUD와 분리된 디버그 요소이며:

- 시작 시 숨김
- F1 토글
- 화면 오른쪽 위
- Canvas 자동 생성
- 전투별 로그 초기화
- 행동 중복 방지
- 최대 200줄 유지

구조로 동작한다.

이를 통해 Unity Console을 열지 않고도 게임 화면에서 최근 전투 행동 흐름을 빠르게 확인할 수 있는 기반을 마련했다.
