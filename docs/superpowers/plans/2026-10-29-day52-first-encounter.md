---
# 52일차 첫 일반 조우 Implementation Plan

**Goal:** 첫 슬라임 조우의 대상별 선택과 저장 재개를 연결한다.

**Architecture:** 새 던전 생성 버전에서 첫 연결 방의 일반 역할을 보장하고 조우 대상과 완료 상태를 Core·Runtime에 연결한다.

**Tech Stack:** Unity 6000.3.21f1, C#, Input System, NUnit.

**Spec:** docs/superpowers/specs/2026-10-29-day52-first-encounter-design.md

---
## 작업

- [ ] 여러 시드의 첫 일반 방과 이전 생성 버전 보존 테스트를 작성한다.
- [ ] 첫 슬라임 조우·대상별 선택 및 저장 재개를 구현한다.
- [ ] 기존 상위 조우와 첫 일반 조우의 실제 선택을 검증한다.
- [ ] 전체 테스트와 최신 플레이 화면을 확인하고 커밋·푸시한다.
