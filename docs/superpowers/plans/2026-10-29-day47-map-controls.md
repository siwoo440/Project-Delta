---
# 47일차 지도 조작 Implementation Plan

**Goal:** 발견 지도에서 이동·확대·현재 위치 복귀를 지원한다.

**Architecture:** 지도 전용 입력을 화면에 전달하고 공개된 지도 요소만 잘린 이동 영역에 배치한다.

**Tech Stack:** Unity 6000.3.21f1, C#, Input System, NUnit.

**Spec:** docs/superpowers/specs/2026-10-29-day47-map-controls-design.md

---
## 작업

- [x] 입력별 지도 이동·확대·복귀와 숨긴 방 보호 테스트를 작성한다.
- [x] 지도 전용 입력과 잘린 지도 조작 화면을 구현한다.
- [ ] 전체 테스트와 최신 실제 플레이 화면을 확인하고 커밋·푸시한다.
