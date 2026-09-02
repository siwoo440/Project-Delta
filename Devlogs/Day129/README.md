# 129일차 : 최적화 및 버그 수정 - 미사용 파일 정리 및 줄별 주석 시작

## 목표
- 전체 코드베이스(307파일·약 48,000줄) 대상 미사용 파일 삭제 후보 스캔
- 줄별(각 줄) 주석 추가 작업 시작 - 규모가 커서 여러 일차에 걸쳐 진행, 129일차는 Domain
  레이어 중 가장 작은 파일들부터 배치 1 완료

## 구현 내용

### 1. 미사용 파일 스캔 및 삭제
- 전체 `.cs` 파일의 타입명을 다른 코드 파일·씬·프리팹·에셋(GUID 기준) 어디서도 참조하지
  않는지 스캔 (Unity `RuntimeInitializeOnLoadMethod`/`MenuItem` 리플렉션 자동 호출 패턴은
  정상 사용으로 별도 분류)
- **삭제**: `DataValidator.cs`, `IDataValidator.cs` (3일차 데이터 검증 도구 - 어디서도
  생성/호출되지 않음, Tests 폴더까지 재검증 완료)
- **삭제 시도 후 복구**: `SaveSlotHudController.cs` - 1차 스캔이 `Assets/ProjectDelta/Tests`
  폴더를 검사 범위에서 빠뜨려 실제로는 `SaveSlotHudControllerUguiTests.cs`가 참조하고
  있다는 걸 놓쳤다. 사용자가 컴파일 오류(CS0246)로 알려줘서 즉시 `git checkout`으로 복구
- **삭제하지 않고 보류**: `RelicDefinition.cs`, `RelicService.cs`, `RoomTrapService.cs` -
  콘텐츠가 아직 없어 미사용이지만, 유물/함정 시스템의 향후 콘텐츠용 스캐폴딩이라 유지

### 2. 줄별 주석 - Domain 레이어 배치 1/N
- Domain 레이어(62파일·8,055줄)를 파일 크기순으로 나눠 가장 작은 13개 파일(262줄) 완료:
  `NpcInteractionResultType`, `NpcRelationshipStage`, `EquipmentSlotType`, `RoomContentType`,
  `NpcInteractionCommand`, `NpcServiceRunState`, `NpcInteractionResult`, `RoomEventService`,
  `ItemCategory`, `ArmorWeightClass`, `NpcRelationshipRules`
  (`DungeonRoomRole`, `DungeonMinimapContentGlyphRules`는 이미 매줄 주석이 있어 스킵)
- `{`/`}`만 있는 줄, 빈 줄에는 주석을 달지 않음 - 나머지 모든 코드 줄에 트레일링 `//` 주석 추가
- 배치 완료 후 전수 검사(빈 줄·중괄호 단독 줄 제외 나머지 줄에 `//` 존재 여부)로 누락 확인 -
  `NpcInteractionResult.cs`의 두 줄짜리 대입문 중 두 번째 줄 하나가 누락돼있던 것을 찾아 수정

## 남은 작업 및 교훈
- Domain 레이어 남은 49개 파일(~7,800줄), Data(3,239줄)·Application(11,497줄)·
  Presentation(23,015줄)이 아직 남아있어 다음 일차부터 계속 이어간다
- 미사용 파일 판정 시 `Assets/ProjectDelta/Tests` 폴더도 반드시 검사 범위에 포함해야 한다는
  교훈을 얻음 - 앞으로의 스캔에 반영
