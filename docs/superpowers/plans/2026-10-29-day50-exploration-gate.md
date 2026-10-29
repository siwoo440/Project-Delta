---
# 50일차 탐험 검증 Implementation Plan

**Goal:** 현재 2D 탐험의 주요 연결 흐름을 검증한다.

**Architecture:** 실제 씬과 격리된 저장 경로를 사용하는 통합 테스트에서 지도·비밀 통로·재시작을 연속 검사한다.

**Tech Stack:** Unity 6000.3.21f1, C#, Input System, NUnit.

**Spec:** docs/superpowers/specs/2026-10-29-day50-exploration-gate-design.md

---
## 작업

- [x] 지도에서 미발견 정보 숨김과 입력 잠금을 검사한다.
- [x] 발견·개방·비밀방 방문 후 새 실행의 이어하기를 검사한다.
- [ ] 전체 테스트와 최신 플레이 화면을 확인하고 커밋·푸시한다.
