---
# 1일차 기본 실행 구조 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 새 코드로 시작 흐름과 상태별 입력 분리를 완성하고 검증 후 커밋·푸시.

**Architecture:** 순수 상태 모델이 전환을 승인하고 AppRoot가 씬 로더와 입력 라우터를 조정한다. 화면은 표시와 요청만 처리한다.

**Tech Stack:** Unity 6000.3.21f1, URP 2D, C#, Input System, uGUI, Unity Test Framework.

**Spec:** docs/superpowers/specs/2026-10-26-day01-foundation-design.md

---
## Global Constraints

- C# Allman 스타일과 각 비어 있지 않은 줄의 명사형 한글 주석.
- 기존 기능·규칙 유지, C# 신규 작성.
- 프로젝트·엔진·패키지 기준 보존, 생성 캐시와 빌드 제외.
- 사용자 승인에 따라 구현 후 지정 GitHub 저장소에 커밋·푸시.

---
## Review Focus

- 전환 도중 새 게임 연타: 단일 던전 로드 보장.
- 반복 Bootstrap 진입: AppRoot·EventSystem 각 하나 유지.
- UI에서 탐험으로 전환: 이전 입력 맵 비활성화.
- 던전에서 타이틀로 복귀: 새 게임 버튼 포커스 복구.
- 씬 로드 실패: 이전 상태·입력 복구와 오류 기록.

---
## Task 1: 상태 전환 모델

**Files:** Assets/ProjectDelta/Core/GamePhase.cs, GameFlowState.cs, Tests/EditMode/GameFlowStateTests.cs.

**Interfaces:** GameFlowState.Phase, TryBeginTransition(GamePhase), CompleteTransition(), CancelTransition().

- [x] 상태·중복 요청·실패 복구 테스트와 최소 실패 구현 작성.
- [x] Unity EditMode 테스트의 의도된 실패 확인.
- [x] 합법적인 전환과 완료·취소 구현 후 전체 EditMode 통과.

---
## Task 2: Unity 실행 흐름

**Files:** Assets/ProjectDelta/Runtime의 AppRoot·SceneLoader·GameInputRouter·TitleScreenView·DungeonScreenView·UiFactory, Editor/DayOneProjectSetup.cs, Tests/PlayMode/StartupFlowTests.cs.

**Interfaces:** AppRoot.Instance, Flow, Input, StartNewGame(), ReturnToTitle(); SceneLoader.LoadScene(string); GameInputRouter.SetPhase(GamePhase).

- [x] 씬 연결·중복 초기화·입력·복귀 통합 테스트 작성 및 배경 클릭 오류 재현.
- [x] 씬 생성 도구와 실행 코드 구현, Bootstrap·Title·Dungeon 생성.
- [x] EditMode 6개·PlayMode 4개·Windows 실행 파일 4개 통과 및 일반 빌드·타이틀 확인.

---
## Task 3: 검토와 배포

**Files:** README.md, docs/development/day-001.md, 전체 변경.

- [x] 실제 결과와 실행 방법 기록, 변경·주석·누락 파일 점검.
- [x] 별도 검토자의 전체 코드 검토와 배경 클릭 오류 수정.
- [x] `1일차 : 2D 프로젝트 기본 실행 구조 구축` 제목과 명사형 작업 본문 준비.
- [x] 빈 원격 main에 첫 커밋을 푸시하고 SHA를 대조하는 배포 절차 확정.

커밋·푸시의 실제 결과는 로컬 Git 이력과 원격 main의 SHA로 확인한다.
