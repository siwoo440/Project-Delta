# 124일차 : 5층 마왕 고정 등장 및 전투 자원 바 UI 수정

## 목표
- 5층(마지막 층)에는 반드시 정해진 마왕이 나오도록 데이터/로직 보강
- 5층 마왕 처치 후 계단 상호작용 시 나오던 경고 로그 버그 수정
- 전투/탐험 화면의 HP·MP·정력 바가 실제 자원 상태에 따라 줄어들거나 늘지 않던 문제 수정

## 구현 내용

### 1. 5층 전용 마왕 데이터 신설
- `DemonLord.asset` (MonsterDefinition): `MON_DEMON_LORD`, 표시명 "마왕", Tier=Boss, 기존 4종 보스보다 확실히 강한 임시 능력치(HP 200/공격 26 등), 3페이즈
- `DemonLord.asset` (EncounterDefinition): `minFloor/maxFloor: 5`로 지정

### 2. 5층에 반드시 이 마왕만 나오도록 배치 로직 수정
- `DungeonFloorController.CollectBossEncounters()`: `MON_DEMON_LORD`를 기존 4종 보스 순환 로스터에서 완전히 제외하고, 층이 5층(마지막 층)이면 이 마왕만 단독으로 반환하도록 분기
- 근본 원인이었던 누락도 수정: `DemonLord` 인카운터가 씬 어디에도 참조되지 않아 런타임에 로드되지 않던 문제를 `DungeonScene.unity`의 `additionalFloorEncounters` 배열에 등록해 해결

### 3. 5층 클리어 흐름 버그 수정
- 5층 보스방 계단은 마왕을 쓰러뜨려야만(RoomType.Boss 게이팅) 나타나므로, 계단에 도달했다는 것 자체가 이미 클리어했다는 뜻이다. 기존에는 `TryDescend`가 "마지막 층에서는 계단을 사용할 수 없습니다" 경고만 찍고 막았는데, 이제 `ApplicationFlow.ReturnToLobby()`를 호출해 던전 클리어 처리 후 로비로 복귀하도록 수정 (124일차 본작업인 클리어 보상/전용 연출은 추후 별도 일차에서 정식으로 만들 예정)

### 4. HP/MP/정력 바 UI 버그 수정
- `DungeonScene.unity`의 자원 바 Image 컴포넌트 11곳 점검 - 두 가지 원인 확인
  - 탐험 화면 상시 HUD(`PersistentPlayerVitalsController`)의 HP/MP/정력 바 3개는 스프라이트가 비어 있어(`m_Sprite: {fileID: 0}`) Unity가 `Type`과 무관하게 항상 꽉 찬 사각형만 그리던 상태였다 - 스프라이트를 채워 넣어 해결
  - 나머지 8개(전투 슬롯 HP 5개 + 전투 중 플레이어 자원 바 3개)는 원래부터 정상(`Type: Filled`)이었으나, 조사 과정에서 실수로 `Type`을 잘못 바꿨다가 다시 원상복구
- 최종적으로 11개 바 모두 `Type: Filled` + 스프라이트 지정 상태로 정리되어, `BattleParticipantSlotView`/`BattleHudController`/`PersistentPlayerVitalsController`가 매 프레임 계산하는 `fillAmount`가 실제로 화면에 반영된다
