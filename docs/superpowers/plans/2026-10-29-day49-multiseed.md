---
# 49일차 다중 시드 검증 Implementation Plan

**Goal:** 던전 생성 규칙의 시드별 안전성을 검증한다.

**Architecture:** 기존 구조와 시험 비밀 구조를 독립 검증하고 잘못된 연결은 생성·재개 경로에서 거절한다.

**Tech Stack:** Unity 6000.3.21f1, C#, NUnit.

**Spec:** docs/superpowers/specs/2026-10-29-day49-multiseed-design.md

---
## 작업

- [x] 다중 시드와 잘못된 비밀 구조의 테스트를 먼저 작성한다.
- [x] 생성 버전별 구조 검사를 구현하고 실제 경로에 연결한다.
- [ ] 전체 테스트와 최신 실제 플레이 화면을 확인한 뒤 커밋·푸시한다.
