# Project Delta - 4일차 개발일지

## 개발 주제

**ProfileData · RunData · SettingsData 구조 정의**

이번 일차부터 새 일정(371일 → 363일 통합)의 저장·런타임 구간이 시작된다. 기획서 9.1절이 정의하는 저장 데이터 4종 중, Steam 연동이 필요한 `AchievementCache`를 제외한 3종의 구조를 정의했다.

---

## 개발 목표

- 기획서 9.1절 "저장 데이터 구성" 표에 맞춰 `ProfileData`/`RunData`/`SettingsData`를 서로 독립된 클래스로 정의
- `RunData` 손상이 `ProfileData`/`SettingsData`에 전파되지 않는 구조 확보
- 아직 존재하지 않는 시스템(인벤토리·장비·유물·던전 생성)에 의존하지 않는 최소 표현으로 자리만 확보
- 이후(새 6일차) JSON 직렬화 작업을 바로 이어갈 수 있도록 `[Serializable]` 적용

---

## 구현 내용

### 1. 세 데이터의 독립성 확보

기획서 9.1절 원칙을 그대로 반영했다.

```text
ProfileData  — 회차 종료 후에도 유지
RunData      — 회차 종료 시 삭제
SettingsData — 회차와 무관, 유지
```

셋은 서로를 참조하지 않는 완전히 분리된 클래스로 만들어, 셋 중 하나가 손상되어도 나머지 저장 파일이 함께 깨지지 않는 구조로 시작했다.

---

### 2. ProfileData 구조

```text
ProfileData
├─ PermanentGrowth (영구 성장)
│  ├─ 기억 파편 보유량 / 누적 획득량
│  ├─ 시작 골드, 시작 소비 아이템
│  ├─ 해금된 스킬, 추가 시작 스킬 후보
│  └─ TODO: 영구 능력치 강화 등 6.6절, 성인 이벤트 행동 숙련도
├─ PermanentRecord (영구 기록)
│  ├─ 몬스터 도감, NPC 호감도·관계 진행
│  ├─ 엔딩(주요/몬스터/NPC), 패배 기록
│  └─ CG·이벤트 다시보기, 도전과제, 스토리 플래그
└─ LifetimeStats (누적 통계, 13종)
   └─ 플레이 시간, 완료 회차, 캐릭터 엔딩, 게임 오버 등
```

---

### 3. RunData 구조

가장 많은 항목을 가진 데이터라 다섯 개의 하위 클래스로 나눴다.

```text
RunData
├─ RunBasicInfo   — 회차 ID, 난이도, 현재 층/방 좌표, 던전 시드
├─ PlayerRunStats — 레벨·경험치·스탯 포인트, 체력·마나·정력, 6대 능력치, 스킬
├─ RunInventory   — 인벤토리·소비 아이템·장비·유물 (모두 ID 리스트, TODO 6.4절)
├─ DungeonRunState
│  └─ RoomRunState — 방별 방문·발견·완료·함정·상자·계단 상태
└─ CharacterRunState — 몬스터/NPC 개체별 체력·회차 한정 호감도·생존 상태
```

기획서에서 특히 강조한 규칙을 그대로 반영했다: 몬스터 **개체** 호감도(`CharacterRunState.RunAffinity`)는 RunData에 저장해 회차 종료 시 삭제되고, NPC 호감도는 ProfileData의 `PermanentRecord.NpcAffinity`에 저장해 영구 유지된다.

---

### 4. SettingsData 구조

기획서 9.1절 9개 분류를 그대로 하위 클래스로 나눴다.

```text
SettingsData
├─ DisplaySettings      (해상도, 창 모드, 수직 동기화, 프레임 제한)
├─ GraphicsSettingsData (품질, 그림자, 효과 품질)
├─ UiSettings           (UI 크기, 피해 숫자, HUD)
├─ TextSettings         (출력 속도, 자동 진행, 글자 크기)
├─ AudioSettingsData    (전체·BGM·효과음·환경음·UI·음성)
├─ AccessibilitySettings(섬광, 흔들림, 모노 오디오, 효과음 자막)
├─ KeyBindingEntry 리스트 (키보드/게임패드 조작)
├─ Language
└─ StreamingModeEnabled
```

---

## 적용 중 발견된 문제 및 수정

없음. 세 파일 모두 다른 시스템에 의존하지 않는 순수 데이터 클래스라 컴파일 문제 없이 `.meta`가 정상 생성되었다.

---

## 현재 4일차 전체 흐름

```text
저장 데이터 4종 확인 (기획서 9.1)
↓
AchievementCache 제외 (Steam 연동 이후 일차)
↓
ProfileData / RunData / SettingsData 구조 정의
↓
아직 없는 시스템은 ID 리스트 placeholder + TODO 주석으로 자리만 확보
↓
[Serializable] 적용 — 직렬화 준비 완료
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Data/ProfileData.cs
Assets/ProjectDelta/Scripts/Data/RunData.cs
Assets/ProjectDelta/Scripts/Data/SettingsData.cs
Devlogs/Day04/README.md
```

---

## 수정 파일

없음.

---

## 삭제 파일

없음.

---

## 최종 확인 항목

4일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- `ProfileData`가 회차 종료 후 유지되는 항목만 포함함
- `RunData`가 회차 종료 시 삭제되는 항목만 포함함
- 몬스터 개체 호감도는 RunData, NPC 호감도는 ProfileData에 위치함
- `SettingsData`가 런과 무관하게 독립적으로 저장 가능한 구조임
- 세 클래스가 서로를 참조하지 않음

---

## 다음 개발 방향

다음 5일차에는 **RunContext와 현재 런 생명주기, PlayerRunState 플레이어 런타임 능력치 상태**를 구현한다.

예정 흐름:

```text
RunContext — 현재 RunData를 들고 있는 런타임 컨테이너
↓
런 생명주기: 시작 → 진행 → 종료(정상/포기/게임오버)
↓
PlayerRunState — RunData.PlayerStats를 실제 전투/탐험 코드가 읽고 쓰는 런타임 형태로 감싸기
↓
AppRoot의 "저장 시스템 초기화 (TODO)"·"프로필 불러오기 (TODO)" 자리와 연결 준비
```
