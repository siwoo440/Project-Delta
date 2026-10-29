---
# 46일차 던전 구조 검증 Implementation Plan

**Goal:** 저장 당시 던전 구조와 복원 구조의 불일치를 검출한다.

**Architecture:** Core의 결정적 방·문 지문을 버전 2 저장에 포함하고 재개 전에 검사한다. 기존 버전 1은 현재 구조 기준으로 읽는다.

**Tech Stack:** Unity 6000.3.21f1, C#, NUnit.

**Spec:** docs/superpowers/specs/2026-10-29-day46-layout-fingerprint-design.md

---
## 작업

- [x] 구조 지문과 변경·누락·기존 저장 호환성 검증을 먼저 작성한다.
- [x] 던전 구조 지문과 버전별 검증을 구현한다.
- [ ] 테스트와 실제 화면을 확인하고 커밋·푸시한다.
