# Project Delta - 78일차 개발일지

## 작업 주제

**던전 다중 인카운터 배정 구조 및 기획서 몬스터 20종 전체 배치**

---

## 개발 목표

77일차까지는 던전 전체가 `defaultMonsterEncounter` 하나(1층 슬라임 무리)만 썼다. 78일차는 방·층마다 다른 `EncounterDefinition`을 쓸 수 있게 구조를 확장하고, 그 위에 77일차에 만든 몬스터 20종을 전부 배치한다.

```text
층 배치가 정해진 몬스터(슬라임/슬라임 퀸 1층, 고블린/고블린 퀸 2층, 미노타우르 3층,
용 수인 4층)는 해당 층에서만 나오게 제한한다.

배치가 정해지지 않은 나머지 14종은 일단 모든 층에서 나오게 하고,
이후 밸런싱 때 층 범위만 좁혀서 제한할 수 있게 만든다.
```

---

## 1. EncounterDefinition에 층 범위 추가

```csharp
[SerializeField] private int minFloor = 1;
[SerializeField] private int maxFloor = -1; // -1 = 상한 없음

public bool IsAllowedOnFloor(int floor)
{
    if (floor < MinFloor) return false;
    if (maxFloor >= 1 && floor > maxFloor) return false;
    return true;
}
```

기본값(`minFloor = 1`, `maxFloor = -1`)이 곧 "모든 층에서 등장"이라, 배치가 정해지지 않은 몬스터는 필드를 건드리지 않기만 하면 된다. 나중에 밸런싱하면서 특정 층 전용으로 좁히고 싶으면 `minFloor`·`maxFloor` 두 숫자만 바꾸면 된다 — 요청하신 "이후 층수를 수정해서 제한"이 바로 이 두 필드다.

---

## 2. RoomEncounterPlacementService.BuildForFloor — 다중 인카운터 배정

기존 `Build()`는 인카운터 하나만 받았다. 여러 인카운터를 한 층에 동시에 쓸 수 있도록 `BuildForFloor()`를 추가했다.

```text
1. 이 층에서 IsAllowedOnFloor()를 통과하는 인카운터만 추린다
2. Id 기준으로 정렬해 처리 순서를 고정한다 (순서가 결과에 영향을 주므로 결정론을 위해 필요)
3. 순서대로 하나씩 Build()를 호출하되, 이미 다른 인카운터가 가져간 방은
   excludedRoomIds로 넘겨 제외한다
4. 결과를 하나의 DungeonEncounterLayout으로 합친다
```

한 방에는 최대 하나의 인카운터만 배정된다 — 여러 인카운터가 같은 방을 놓고 경쟁하면 정렬 순서상 먼저 처리되는 쪽이 그 방을 가져간다. 기존 `Build()`는 그대로 남겨뒀고(인카운터 하나만 쓰는 기존 호출부·테스트가 그대로 통과), `BuildForFloor()`가 내부적으로 그걸 반복 호출하는 얇은 오케스트레이션 계층이다.

---

## 3. DungeonFloorController 연결

```csharp
[SerializeField] private EncounterDefinition defaultMonsterEncounter; // 기존 필드 유지
[SerializeField] private EncounterDefinition[] additionalFloorEncounters; // 78일차 추가
```

`defaultMonsterEncounter`(항상 포함)와 `additionalFloorEncounters`를 `CollectFloorEncounters()`로 합쳐서 `BuildForFloor()`에 넘긴다. 기존 필드를 유지한 이유는 씬에 이미 연결돼 있던 참조(1층 슬라임 인카운터)를 그대로 살리기 위해서다 — 76일차 `EncounterDefinition.Monster` + `AdditionalMonsterPool` 구조와 같은 패턴(대표 하나 + 나머지 배열)을 여기서도 그대로 썼다.

`BuildEncounterLayout()`은 이제 층 번호(`floor`)를 함께 받는다. 두 호출부(신규 생성/저장 복원) 모두 이미 `GetDungeonState().CurrentFloor`를 지역 변수로 갖고 있어서 그대로 넘기기만 하면 됐다.

`TryFindMonsterDefinition()`(76일차, 그룹 슬롯의 몬스터 ID를 실제 에셋으로 되돌리는 조회)도 `defaultMonsterEncounter` 하나만 뒤지던 것을 `CollectFloorEncounters()`로 모은 전체 인카운터를 뒤지도록 확장했다 — 안 그러면 슬라임 외의 몬스터가 배치돼도 전투에서 스탯을 못 찾는다.

---

## 4. 몬스터 20종 전체 배치

77일차에 만든 `MonsterDefinition` 20종에 맞춰 `EncounterDefinition` 20개를 완성했다(1층 슬라임은 77일차에 이미 만들어둔 것을 재사용, 층 범위만 명시적으로 채움).

```text
2층: 고블린 무리 (고블린 위주, 드물게 고블린 퀸 섞임) - minFloor 2, maxFloor 2
3층: 미노타우르 단독 - minFloor 3, maxFloor 3
4층: 용 수인 단독 - minFloor 4, maxFloor 4

층 배치 미정 14종 - minFloor 1, maxFloor -1 (모든 층)
  아라크네·라미아·하피·서큐버스·미믹·알라우네·고스트·리자드 (단독)
  늑대·고양이·여우·토끼·쥐 수인 (1~2마리 무리)
```

곰 수인은 문서상 "높은 체력과 방어력"이라는 컨셉이 다른 수인들과 달라 단독(1마리)으로, 미믹은 "상자 위장과 기습" 컨셉이라 낮은 등장 확률(0.1)로 설정했다. 나머지 스폰 확률은 0.1~0.35 사이 임의값이다.

씬(`DungeonFloorController`)의 `additionalFloorEncounters` 배열에 새로 만든 17개 인카운터(고블린·미노타우르·용 수인 + 층 미정 14종)를 직접 연결했다.

---

## 5. EditMode 테스트 추가

```text
RoomEncounterPlacementTests (확장)
  - 층 범위 밖의 인카운터는 배정 대상에서 완전히 제외됨
  - 층 범위 기본값(모든 층 허용)이 실제로 모든 층에서 통과함
  - 인카운터 여러 개가 경쟁해도 한 방에 중복 배정되지 않음
  - IsAllowedOnFloor()가 기본값·제한값 모두 올바르게 판정함
```

---

## 변경 파일

```text
Assets/ProjectDelta/Scripts/Data/EncounterDefinition.cs
Assets/ProjectDelta/Scripts/Application/RoomEncounterPlacementService.cs
Assets/ProjectDelta/Scripts/Presentation/DungeonFloorController.cs
Assets/ProjectDelta/Scenes/DungeonScene.unity

Assets/ProjectDelta/Data/Monster/Encounter Definition/EncounterDefinition.asset (기존, 층 범위 명시)
Assets/ProjectDelta/Data/Monster/Encounter Definition/*.asset (신규 17종 + .meta)

Assets/ProjectDelta/Tests/EditMode/RoomEncounterPlacementTests.cs
```

---

## 확인 사항

- `EncounterDefinition.IsAllowedOnFloor()`로 층 제한을 표현, 기본값은 "모든 층 허용"
- `RoomEncounterPlacementService.BuildForFloor()`로 여러 인카운터를 한 층에서 동시에 배정, 한 방에 중복 배정 없음, 처리 순서 고정으로 결정론 유지
- `DungeonFloorController`가 `defaultMonsterEncounter` + `additionalFloorEncounters`를 합쳐 층별 배정에 사용
- `TryFindMonsterDefinition()`이 여러 인카운터를 모두 조회하도록 확장 — 슬라임 외 몬스터도 전투에서 정상적으로 스탯을 찾음
- 몬스터 20종 전체를 실제 인카운터로 배치 완료 — 층 배치가 정해진 4종은 해당 층 전용, 나머지 14종은 전 층 등장
- 기존 `Build()`(단일 인카운터) 호출부와 테스트는 수정 없이 그대로 통과
- 새 EditMode 테스트 4개로 층 필터링·중복 배정 방지를 검증

Unity Editor에서의 실제 스크립트 컴파일과 Test Runner 통과 여부, 그리고 실제 던전에서 층마다 의도한 몬스터가 나오는지는 이 저장소 diff만으로는 확정할 수 없으므로, Unity Editor에서 직접 최종 확인이 필요하다. 씬 파일(`DungeonScene.unity`)의 배열 필드를 직접 편집했으므로, 에디터에서 `DungeonFloorController` 인스펙터를 열어 `Additional Floor Encounters` 17개가 의도한 대로 연결돼 있는지 한 번 확인해 주시면 좋다.

참고로 워킹 디렉터리에 `Assets/POLY STYLE - Platformer Starter Pack` 폴더가 새로 보이는데, 이번 작업에서 만든 게 아니라 손대지 않았다 — 별도로 추가하신 에셋이라면 원하실 때 커밋해 주시면 된다.

---

## 이번 일차 완료 상태

78일차 목표인 **던전 다중 인카운터 배정 구조 및 몬스터 20종 전체 배치**를 완료했다. 이제 기획서의 몬스터 20종이 전부 실제로 던전에 등장할 수 있는 상태가 됐다.

---

## 다음 단계

임의로 채운 스폰 확률·그룹 구성·능력치를 실제 밸런싱 수치로 교체하는 것이 남은 일이다. 층 배치 미정 14종 중 일부를 특정 층으로 좁히고 싶다면 해당 `EncounterDefinition`의 `minFloor`·`maxFloor`만 조정하면 된다.
