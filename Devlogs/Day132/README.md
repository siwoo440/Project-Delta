# 132일차 : 캐릭터 엔딩/패배 기록 연동 (기획서 7.3절)

## 목표
- 기획서 7.3절을 확인해, 몬스터 개별 엔딩(20)·NPC 개별 엔딩(10)·패배 기록(31)의 발동
  조건과 판정 로직을 구현 - 131일차와 같은 원칙으로 구조 먼저, 재조우/선택 UI는 이후 일차로 미룸

## 확인된 기획 내용
- 몬스터 개별 엔딩: 이번 회차 호감도 100(성인 이벤트 전투 승리만 인정) 또는 종족 도감
  100% - 단, 몬스터 하렘은 도감으로 대체 불가. 호감도는 이번 회차에만 유지
- NPC 개별 엔딩: 호감도 100(회차를 넘어 유지) + 핵심 선택 이벤트 완료
- 패배 기록 31개(몬스터 20+NPC 10+마왕 1): 일반 전투 체력 0 / 이벤트 전투 정력 0 / 항복
  세 상황 모두 같은 상대면 "하나의 공통 기록"
- 마왕 패배는 특수: "마왕의 종"(주요 엔딩)과 "왕 앞에 무릎 꿇다"(패배 기록)가 동시 등록

## 구현 내용

### 1. 몬스터 호감도 회차 추적
- `RunSubStates.cs`의 빈 껍데기였던 `CharacterRunState`에 실제 필드 채움 - 이번 회차에
  호감도 100을 찍은 종족 ID 목록(`MonsterAffinityMaxedIds`)
- `EventBattleParticipantState`의 기존 `HasWon = true` 지점(Favor가 `FavorToWin`=100
  도달)에 훅을 걸어 자동 기록 - 몬스터 호감도는 NPC와 달리 프로필에 영구 저장하지 않고
  회차 상태로만 둠(기획서 차이 반영)

### 2. NPC 개별 엔딩 조건
- `NpcRelationshipState`에 `HasCompletedKeyEvent`/`MarkKeyEventCompleted()` 추가 - 실제
  핵심 선택 이벤트 콘텐츠는 아직 없어 항상 `false`, 콘텐츠가 생기면 호출만 하면 되는 자리

### 3. 판정 규칙 (Domain)
- `CharacterEndingRule` 신설 - 몬스터는 "회차 호감도 100 또는 종족 도감 100%", NPC는
  "호감도 100 그리고 핵심 이벤트 완료"

### 4. 패배 기록
- `ApplicationFlow.RecordDefeat(opponentDefinitionId)` 신설 - 같은 상대는 한 기록으로
  중복 방지하며 `ProfileData.PermanentRecord.DefeatRecordIds`에 저장
- `BattleDefeatService.ReturnToTitleAfterDefeat`에 연결(일반 전투 체력 0/항복) - 마지막
  공격자가 없으면(피해 없이 곧바로 항복한 경우) 현재 첫 생존 적으로 대체
- `EventBattleController.Finish(Lost)`에 연결(이벤트 전투 정력 0) - 그 순간 선택돼있던 대상 기준

### 5. 마왕 패배 특수 처리
- `ApplicationFlow.TryFinalizeMainEnding()`에서 `ServantOfTheDemonLord` 확정 시 마왕
  패배 기록도 동시 등록
- 던전 층 컨트롤러(`DungeonFloorController.FinalBossMonsterId`)와 패배 기록 양쪽이
  서로 다른 문자열을 들고 있다가 어긋나지 않도록 `MainEndingRules.DemonLordMonsterId`로
  단일화

## 다음 일차로 넘긴 부분
- 몬스터/NPC 재조우 후 "함께 남는다" 선택 UI, 핵심 선택 이벤트 콘텐츠 자체는 아직 없음
- 몬스터 도감 완성 여부(`HasFullMonsterDex`)는 여전히 항상 `false`
