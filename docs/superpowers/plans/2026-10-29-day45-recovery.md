---
# 45일차 저장 복구 Implementation Plan

**Goal:** 손상되거나 누락된 회차 저장의 안전한 읽기 복구.

**Architecture:** 기존 회차 저장 처리에서 백업과 임시 파일을 최신순으로 검증하며 원본 교체 없이 읽는다.

**Tech Stack:** Unity 6000.3.21f1, C#, NUnit.

**Spec:** docs/superpowers/specs/2026-10-29-day45-recovery-design.md

---
## 작업

- [x] 손상·누락·복구 후보 우선순위 및 원본 보존 검증을 추가하고 실패를 확인한다.
- [x] 검증 가능한 최신 후보를 읽고 저장을 차단하는 복구를 구현한다.
- [ ] 단위 및 플레이 회귀 검증과 실제 화면 갱신 후 커밋·푸시한다.
