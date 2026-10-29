---
# 43일차 저장 계약 Implementation Plan

**Goal:** 회차와 영구 프로필의 저장 범위와 버전 검증을 정의한다.

**Architecture:** Core에 Unity 직렬화 가능한 데이터와 독립 검증을 두고 기존 관찰 저장과 분리한다.

**Tech Stack:** Unity 6000.3.21f1, C#, NUnit.

**Spec:** docs/superpowers/specs/2026-10-29-day43-save-contract-design.md

---
## 검증과 구현

- [x] 버전·체력·아이템·관찰 기록의 정상과 오류 검증 작성 및 미구현 컴파일 오류 확인.
- [x] 저장 데이터와 유효성 검사 구현.
- [x] EditMode·PlayMode와 실제 플레이 캡처 검증.
- [ ] 실행 기록과 README를 갱신하고 43일차 커밋을 푸시.
