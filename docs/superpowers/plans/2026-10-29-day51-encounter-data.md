---
# 51일차 조우 데이터 Implementation Plan

**Goal:** 층별 몬스터 정의와 고정 조우 배치 규칙을 추가한다.

**Architecture:** Core에서 몬스터 ID·종류·층과 방별 조우를 검증하며 실행 화면과 무작위 발생은 후속 작업으로 분리한다.

**Tech Stack:** Unity 6000.3.21f1, C#, NUnit.

**Spec:** docs/superpowers/specs/2026-10-29-day51-encounter-data-design.md

---
## 작업

- [x] 층별 대상·같은 종족·3명 상한·첫 슬라임과 방 종류 테스트를 작성한다.
- [x] 검증된 몬스터 정의와 방 고정 조우 데이터를 구현한다.
- [ ] 테스트와 최신 플레이 화면 확인 후 커밋·푸시한다.
