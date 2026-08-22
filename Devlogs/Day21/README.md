# Project Delta - 21일차 개발일지

## 개발 주제

**방별 방문 상태·콘텐츠 처리 상태 저장 구현**

20일차에 만든 `RoomInstance.Visited`는 메모리에만 존재해 씬을 다시 열면 사라졌다. 오늘은 이걸 붙잡아둘 자리를 만들었다 — 4일차 `RunData`(저장용)와 5일차 `RunSubStates`(런타임)에 각각 남겨뒀던 빈 자리를 채웠다.

---

## 개발 목표

- `RoomInstance`에 방문 여부(`Visited`, 20일차)와 짝을 이루는 콘텐츠 처리 상태(`Completed`) 추가
- 5일차부터 빈 클래스였던 `RunSubStates.DungeonRunState`(Domain)를 방 레지스트리로 채움
- 방이 만들어질 때 실제 런이 진행 중이면 자동으로 레지스트리에 등록
- 저장 파일(`RunData.DungeonRunState.Rooms`, 4일차)로의 변환은 아직 손대지 않음

---

## 구현 내용

### 1. RoomInstance — Completed 추가

```text
RoomInstance
├─ Visited (20일차)
└─ Completed (오늘) — MarkCompleted() 호출 시 true로 전환
```

기획서 3.3.3절 *"이벤트가 완료되면 해당 방은 일반적으로 빈 방으로 전환한다"*에 대응한다. 다만 아직 이벤트/조우 판정 시스템이 없어서, `MarkCompleted()`를 실제로 호출하는 곳은 아직 없다.

---

### 2. DungeonRunState(Domain) — 5일차부터 비어있던 자리를 방 레지스트리로

```text
Before (5일차): public sealed class DungeonRunState { }
After  (오늘):  Dictionary<string, RoomInstance> 기반 레지스트리
                ├─ Register(roomInstance)
                ├─ TryGetRoom(roomId, out roomInstance)
                └─ AllRooms
```

---

### 3. RoomPassageController — 자동 등록

```text
Awake()에서 RoomInstance 생성 직후:
RunContext.Current != null 이면
→ RunContext.Current.Dungeon.Register(roomInstance)
```

테스트 씬(런 없이 여는 경우)은 이 조건이 항상 거짓이라 등록 없이 기존처럼 동작한다.

---

## 적용 중 발견된 문제 및 수정

없음. 다만 확인 과정에서 중요한 사실을 하나 확인했다: **지금 테스트 방식(DungeonScene을 직접 열기)으로는 오늘 추가한 등록 코드가 아예 실행되지 않는다.** `RunContext.Current`를 만드는 `RunContext.Begin()`을 호출하는 "새 게임" 흐름이 아직 없기 때문이다. 즉 오늘 작업은 지금 당장 눈에 보이는 변화가 없는, 나중에 새 게임 흐름이 생겼을 때를 위한 순수 배관 작업이었다. 사용자에게도 이 점을 먼저 설명하고 코드 확인만으로 진행하기로 했다.

---

## 현재 21일차 전체 흐름

```text
RoomInstance에 Completed 추가 (아직 호출부 없음)
↓
DungeonRunState(Domain)를 방 레지스트리로 구현
↓
RoomPassageController.Awake()에서 실제 런 진행 중이면 자동 등록
↓
RunData.DungeonRunState.Rooms로의 변환(Save Mapper)은 이후 일차로 계속 이월
```

---

## 생성 파일

```text
Devlogs/Day21/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Domain/RoomInstance.cs
Assets/ProjectDelta/Scripts/Domain/RunSubStates.cs
Assets/ProjectDelta/Scripts/Presentation/RoomPassageController.cs
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

21일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- `RoomInstance.Completed`가 기본값 false로 시작하고 `MarkCompleted()` 호출 시 true로 전환됨
- `DungeonRunState.Register()`/`TryGetRoom()`이 정상 동작함 (코드 검토로 확인)
- `RunContext.Current`가 없는 테스트 씬에서는 기존 동작이 그대로 유지됨 (등록 코드 미실행)

**참고**: 이번 일차 변경은 현재 테스트 환경(새 게임 흐름 없이 DungeonScene 직접 실행)에서는 실행되지 않는 코드라, 실제 동작 확인은 다음에 새 게임 흐름이 생기는 시점으로 미뤄둔다.

---

## 다음 개발 방향

다음 22일차에는 **층 입구/출구와 계단 상호작용 기본 구현**을 진행한다.

예정 흐름:

```text
계단 오브젝트/마커 정의 (19일차 RoomContentMarker의 Stairs 종류 활용)
↓
계단 상호작용 (F) — 문 상호작용(14일차)과 유사한 패턴
↓
층 이동 개념의 최소 골격 (실제 다음 층 생성은 26~35일차 던전 생성 이후)
```
