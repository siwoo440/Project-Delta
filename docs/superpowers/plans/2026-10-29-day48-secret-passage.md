---
# 48일차 비밀 통로 Implementation Plan

**Goal:** 시험 비밀방의 발견·개방·출입·저장 재개를 구현한다.

**Architecture:** Core에 버전별 생성과 독립적인 비밀 연결 상태를 두고 Runtime에서 벽과 실제 조작을 연결한다.

**Tech Stack:** Unity 6000.3.21f1, C#, Input System, NUnit.

**Spec:** docs/superpowers/specs/2026-10-29-day48-secret-passage-design.md

---
## 작업

- [x] 기존 생성 규칙 보존과 비밀방 생성 및 발견·개방 순서 테스트를 작성한다.
- [x] 새로운 생성 버전과 비밀 연결 상태, 안전한 저장·복원을 구현한다.
- [x] 실제 벽 조사·개방·출입과 화면 공개를 구현하고 통합 검증한다.
- [ ] 테스트와 최신 플레이 화면을 확인하고 커밋·푸시한다.
