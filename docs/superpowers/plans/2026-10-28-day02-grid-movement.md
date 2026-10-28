---
# 2일차 격자 이동 Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans with test-driven-development.

**Goal:** 격자 이동·충돌·방향 표시·단일 입력 대기를 구현한다.

**Architecture:** Core의 GridMoveState가 논리 좌표와 입력 대기를 관리하고, Runtime의 GridMoveController가 실제 입력과 Tilemap·화면 위치를 연결한다.

**Tech Stack:** Unity 6000.3.21f1, Input System, 2D Tilemap, C#, Unity Test Framework.

**Spec:** docs/superpowers/specs/2026-10-28-day02-grid-movement-design.md

---
## 전역 조건

- 모든 비어 있지 않은 C# 줄에 짧은 명사형 한글 주석, 중괄호는 Allman 스타일.
- 기존 씬·메타 파일·설정을 보존하고 명시한 작업만 변경.
- 1일차 실행과 타이틀·던전 입력 분리 유지.
- 입력 이벤트는 네 방향만 허용하고 대기 입력은 최대 한 개.
- C#은 기존 코드를 이식하지 않고 새로 작성.

---
## 검토 집중 사항

- 연타·방향 전환·장애물 차단 시 위치 중복, 대기 입력 누락·중복 방지.
- 장치가 동시에 두 축을 입력할 때 대각선 이동 방지.
- 씬 전환과 입력 잠금 중의 대기 입력 제거와 시각적 이동 정지.
- Tilemap 실제 저장·재개방, 격자 중심과 플레이어 표시의 일치.
- 기존 씬 재생성 도구가 2일차 구성과 완료 흐름을 유지.

---
## 작업 1: 논리 이동 규칙

**Files:** Core/MoveDirection.cs, MoveRequestResult.cs, GridMoveState.cs, Tests/EditMode/GridMoveStateTests.cs.

**Interfaces:** GridMoveState(int, int, Func<int,int,bool>), X, Y, Facing, IsMoving, RequestMove, FinishMove, DiscardBufferedMove.

- [x] 네 방향, 막힌 칸, 유효하지 않은 명령, 단일 대기와 취소의 실패 테스트 작성.
- [x] Unity EditMode에서 의도된 실패 확인.
- [x] 순수 이동 상태 구현과 EditMode 전체 통과.

---
## 작업 2: 실제 입력과 2D 씬

**Files:** Runtime/GameInputRouter.cs, Runtime/GridMoveController.cs, Runtime/DungeonScreenView.cs, Editor/DayTwoProjectSetup.cs, Editor/DayOneProjectSetup.cs, Scenes/DungeonScene.unity, Tests/PlayMode/GridMovementTests.cs.

- [x] 실제 키보드·게임패드·타일맵 입력, 경계·장애물, 입력 잠금·대기의 통합 테스트 작성.
- [x] 실제 DungeonScene을 수정해 저장하고 다시 열어 바닥·장애물·방향 표시를 확인.
- [x] PlayMode와 Windows 실행 파일 테스트 전체 통과.

---
## 작업 3: 검토·빌드·푸시

- [x] UI 안내·실행 문서·2일차 기록 갱신.
- [x] 독립 코드 검토와 사용자 요청별 검증.
- [x] Windows 일반 빌드와 전체 테스트 통과.
- 커밋 제목은 `2일차 : 2D 격자 이동 및 충돌 구현`으로 작성.
- 원격 main의 기준 커밋을 재확인한 뒤 푸시하고 SHA를 대조.
