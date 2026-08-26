# Project Delta 84일차 개발일지

---
## 개발 정보

- 개발 일자: 2026-08-26
- 최신 커밋: `e955198437dfb9d9c167143225815512591412df`
- 기준 커밋: `2176b8e4931e44fae435f0bc9442ba845c1bf58f`
- 현재 커밋 제목: `a`
- 개발 주제: 일반 전투 HUD 완성 및 다수 대상·Intent·피해·상태이상 표시 정합

---
# 개발 목표

47~83일차에 구현한 전투 시스템을 실제 플레이 화면에서 즉시 파악할 수 있도록 전투 HUD 표시를 정리했다.

84일차에서는 새로운 전투 규칙을 추가하기보다 기존 전투 상태를 Presentation 계층에서 정확하게 보여주는 데 집중했다.

주요 목표는 다음과 같다.

- 최대 4명 적 슬롯의 현재 상태 표시
- 선택 대상과 현재 행동자 구분
- 적별 Intent 표시 정합
- 플레이어 HP·MP·SP 표시
- 상태이상 이름·중첩·남은 라운드 표시
- 피해·치명타·MISS 피드백 표시
- 기존 디버그 피해 창 기본 비활성화
- HUD 표시 문자열의 EditMode 테스트 추가

---
# 주요 개발 내용

---
## 1. 전투 HUD 갱신 흐름 정리

`BattleHudController`를 기준으로 전투 화면 갱신 흐름을 정리했다.

매 프레임 현재 `BattleContext`와 `ExplorationMonsterEncounterController`의 상태를 읽어 다음 요소를 갱신한다.

- 적 슬롯
- 플레이어 슬롯
- 현재 행동자
- 선택 대상
- 플레이어 HP·MP·SP
- 행동 버튼 활성 상태
- 최근 행동 피드백

HUD가 별도의 전투 상태를 보관하기보다 현재 전투 런타임 상태를 읽어 화면에 반영하는 구조를 유지했다.

---
## 2. 최대 4명 적 슬롯 표시 정합

기존 `BattleContext.MaxEnemySlots` 기반의 적 슬롯 구조를 HUD에서 그대로 사용한다.

각 슬롯은 현재 인덱스에 적이 존재할 때만 참가자 정보를 표시하고, 적이 존재하지 않으면 비운다.

적 슬롯에서 표시하는 주요 정보는 다음과 같다.

- 몬스터 식별 정보
- 초상화
- 현재 HP / 최대 HP
- 선택 가능 상태
- 현재 선택 대상
- 방어 상태
- 상태이상
- 현재 행동자 표시
- 피해 피드백

유효한 공격 대상 목록과 현재 선택 대상을 기준으로 슬롯의 클릭 가능 상태와 선택 강조를 갱신한다.

---
## 3. 현재 행동자 표시

플레이어와 적 슬롯에 현재 행동자인지 확인할 수 있는 표시를 추가했다.

현재 행동자인 경우 슬롯에 `행동 중` 텍스트를 표시한다.

이를 통해 전투 순서를 디버그 로그 없이 HUD에서 확인할 수 있다.

행동이 실제로 실행된 경우 기존 초상화 Bump 연출도 유지한다.

---
## 4. Intent 표시 개선

`BattleIntentHudController`를 정리하여 각 적 슬롯의 행동 예고를 개별적으로 표시한다.

표시 가능한 Intent 종류:

- 공격
- 방어
- 강화
- 약화
- 상태
- 회복
- 특수

대상이 지정된 Intent는 대상 정보도 함께 표시한다.

예시:

`[공격] 물기`
`→ 플레이어`

Intent가 취소된 경우에도 이유를 화면에 표시한다.

취소 사유 예:

- 기절
- 침묵
- 사망
- 만족 상태
- 대상 부재

---
## 5. 상태이상 HUD 표시

`BattleHudDisplayFormatter`를 새로 추가하여 상태이상 표시 문자열을 한 곳에서 생성하도록 구성했다.

기본 표시 형식:

`상태 이름 ×중첩 · 남은 라운드R`

예시:

`중독 ×2 · 3R`

현재 알려진 상태 ID는 읽기 쉬운 한글 이름으로 변환한다.

지원 예:

- POISON → 중독
- BLEED → 출혈
- REGEN → 재생
- STUN → 기절
- SILENCE → 침묵
- BIND → 구속
- CHARM → 매혹
- SLOW → 둔화

능력치 변경 상태는 대상 능력치와 적용 값을 함께 표시한다.

예:

`공격 +10 · 2R`

만료된 상태이상은 HUD에서 제외한다.

---
## 6. 피해 결과 피드백

최근 행동 결과의 `BattleDamageChange`를 실제 대상 슬롯로 전달하여 피해 결과를 표시한다.

기본 표시 규칙:

- 일반 피해 → `-9`
- 치명타 → `치명타! -17`
- 회피 / 빗나감 → `MISS`
- 적용 피해 없음 → `0`

피드백은 일정 시간 표시된 뒤 서서히 사라진다.

또한 참가자 슬롯은 이전 HP를 기억하고 동일 참가자의 HP가 변경된 경우 HP 증감값도 감지할 수 있도록 구성했다.

---
## 7. 플레이어 자원 HUD 정합

플레이어 HUD에서 다음 자원을 현재 전투 참가자 상태 기준으로 표시한다.

- HP
- MP
- SP

표시 형식:

`HP  현재 / 최대`
`MP  현재 / 최대`
`SP  현재 / 최대`

게이지 이미지가 연결되어 있는 경우 현재값과 최대값 비율로 `fillAmount`도 함께 갱신한다.

---
## 8. 행동 버튼 활성 조건 정리

공격·방어·도주 버튼의 공통 조건을 `playerCanAct` 기준으로 정리했다.

버튼 활성 기본 조건:

- 현재 전투 상태가 `AwaitingAction`
- 현재 행동자가 존재
- 현재 행동자가 플레이어 팀

공격 버튼은 추가로 선택된 대상이 존재해야 활성화된다.

이를 통해 적 행동 중 플레이어 행동 버튼이 잘못 활성화되는 상황을 방지한다.

---
## 9. 런타임 HUD 보조 텍스트 생성

`BattleParticipantSlotView`에서 84일차에 필요한 일부 UI 참조가 기존 Scene 또는 Prefab에 없는 경우 런타임에 보조 Text를 생성하도록 구성했다.

자동 생성 대상:

- 상태이상 텍스트
- 피해 피드백 텍스트
- 현재 행동자 텍스트

기존 HUD 구조를 크게 다시 제작하지 않아도 새로운 정보 표시를 사용할 수 있도록 한 호환 처리다.

---
## 10. 기존 피해 디버그 Overlay 정리

55일차 피해 공식 확인용 `BattleDamageDebugOverlay`는 기본 표시 상태를 `true`에서 `false`로 변경했다.

84일차부터 정식 전투 슬롯에서 피해 결과를 확인할 수 있기 때문에 일반 플레이에서는 디버그 창을 숨긴다.

필요한 경우 기존과 동일하게 `F9` 키로 표시 상태를 전환할 수 있다.

---
# 테스트

---
## BattleHudDisplayFormatterTests

84일차 HUD 표시 문자열을 검증하는 EditMode 테스트를 추가했다.

검증 항목:

- 상태이상이 없을 때 빈 문자열
- 중독 상태 이름 표시
- 중첩 수 표시
- 남은 라운드 표시
- 능력치 증가 상태 표시
- 만료 상태 숨김
- MISS 표시
- 치명타 피해 표시
- 일반 피해 표시

중독 테스트는 실제 출력인 다음 형식을 기준으로 한다.

`중독 ×2 · 3R`

초기 테스트에서 일반 분류명인 `지속 피해`를 기대하던 부분은 실제 상태명인 `중독`을 기대하도록 수정되었다.

---
# 변경 파일

83일차 기준 총 9개 파일이 변경되었다.

---
## 수정

- `Assets/ProjectDelta/Scripts/Presentation/BattleDamageDebugOverlay.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleHudController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleIntentHudController.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleParticipantSlotView.cs`
- `Project-Delta.slnx`

---
## 생성

- `Assets/ProjectDelta/Scripts/Presentation/BattleHudDisplayFormatter.cs`
- `Assets/ProjectDelta/Scripts/Presentation/BattleHudDisplayFormatter.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/BattleHudDisplayFormatterTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BattleHudDisplayFormatterTests.cs.meta`

삭제 파일은 없다.

`Project-Delta.slnx` 변경은 프로젝트 항목의 순서 변경이며 기능 코드 변경은 아니다.

---
# 최종 동작 흐름

---
## 전투 진입

`전투 시작 → BattleContext 생성 또는 복원 → HUD 활성화 → 플레이어/적 슬롯 표시 → Intent 표시`

---
## 플레이어 행동

`플레이어 행동 차례 → 대상 선택 → 선택 슬롯 강조 → 공격/방어/도주 버튼 활성 → 행동 확정`

---
## 행동 해결

`행동 실행 → BattleActionResult 생성 → HP/상태 변경 → HUD 재조회 → 피해 피드백 → 상태이상/자원 표시 갱신`

---
## 적 행동

`적 Intent 표시 → 해당 적 행동 차례에 행동 중 표시 → 행동 실행 → 대상 슬롯 피해 피드백 → 다음 행동자로 진행`

---
## 상태이상

`현재 BattleParticipant.StatusEffects → BattleHudDisplayFormatter → 상태 이름·중첩·라운드 문자열 → 슬롯 표시`

---
# 검증 상태

최신 `main`은 83일차 커밋 `2176b8e4931e44fae435f0bc9442ba845c1bf58f`에서 1개 커밋 앞선 상태다.

최신 커밋:

`e955198437dfb9d9c167143225815512591412df`

현재 커밋 메시지는 `a`다.

GitHub 소스 기준으로 다음 사항을 확인했다.

- 84일차 HUD 관련 변경 파일 9개 반영
- 적 슬롯 선택 상태 및 현재 행동자 표시 반영
- 플레이어 HP·MP·SP HUD 갱신 반영
- 적 Intent 표시 및 취소 사유 표시 반영
- 피해·치명타·MISS 표시 Formatter 반영
- 상태이상 이름·중첩·남은 라운드 표시 반영
- `STATUS_POISON`이 `중독`으로 표시되는 로직 확인
- 테스트 기대값도 `중독`으로 수정된 상태 확인
- 기존 피해 디버그 Overlay 기본 비활성화 확인
- `Project-Delta.slnx`는 프로젝트 항목 순서 변경만 확인

GitHub Commit Status와 연결된 자동 CI 결과는 등록되어 있지 않다.

따라서 GitHub 소스 정적 검토 기준으로 84일차를 중단해야 할 차단 문제는 확인되지 않았다.

Unity Editor의 실제 컴파일 및 전체 Test Runner 성공 여부는 GitHub에서 자동 확인할 수 없으므로 로컬 Unity 실행 결과가 최종 기준이다.

---
# 84일차 완료 요약

84일차에서는 기존 전투 시스템의 핵심 정보를 HUD에서 직접 확인할 수 있도록 Presentation 영역을 정리했다.

최종적으로 다음 정보가 전투 화면에서 표현된다.

- 최대 4명 적 상태
- 현재 선택 대상
- 현재 행동자
- 적별 Intent
- 플레이어 HP·MP·SP
- 플레이어 및 적 상태이상
- 피해량
- 치명타
- MISS

84일차의 목표였던 `디버그 로그 없이 전투 상황을 화면에서 이해할 수 있는 상태`에 맞춰 전투 HUD 표시 계층을 완성했다.
