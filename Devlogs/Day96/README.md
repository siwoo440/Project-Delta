# Project Delta - 96일차 개발일지

## 작업 개요

96일차는 신규 기능 추가보다 프로젝트 정리와 UI 구조 개선, 반복 조회 최적화에 집중했다.

기존 개발 과정에서 사용이 끝난 일회성 Installer를 정리하고, 전투 HUD와 인벤토리 HUD의 불필요한 반복 처리를 줄였다.  
또한 코드에서 런타임으로 생성하던 인벤토리 액션 UI와 IMGUI 기반 상자 UI를 실제 uGUI 구조로 전환해 이후 UI 디자인 작업이 쉬운 형태로 정리했다.

- 기준 커밋: `7ecc697664efb0d2a24d33cf31197e3546220f2f`
- 기존 기준: 95일차 최종 커밋 `eb0c7f7c78520e5de080ef50bf3c4ed4b06cec97`

---

## 주요 작업

### 1. 사용이 끝난 Editor Installer 정리

개발 과정에서 특정 일차의 기능을 Scene과 코드에 자동 적용하기 위해 사용했던 일회성 Installer들을 제거했다.

정리 대상에는 다음 작업용 Installer들이 포함되었다.

- Day47 전투 HUD 설치 도구
- Day71 패배 흐름 설치 도구
- Day72 전투 보상 설치 도구
- Day73 전투 의도 표시 설치 도구
- Day74 몬스터 AI 및 보정 설치 도구
- Day79 플레이어 성장 설치 도구
- Day80 전투 드롭 설치 도구
- Day81 전투 보상 설치 도구
- Day86 전투 배속 설치 도구

이미 실제 Scene 및 런타임 코드에 필요한 결과가 반영된 Installer만 제거했다.

`Day31MultiExitRoomGenerator`, `Day36ProceduralFloorSetup`처럼 향후 에셋이나 구조를 다시 생성할 가능성이 있는 Editor 도구는 보존했다.

---

### 2. 전투 HUD 몬스터 초상화 조회 최적화

`BattleHudController`에서 적 슬롯을 갱신할 때 동일한 몬스터 초상화를 반복해서 `Resources.Load<Sprite>()`로 조회하던 구조를 수정했다.

몬스터 Definition ID를 키로 사용하는 Dictionary 캐시를 추가하여:

1. 처음 등장한 몬스터만 Resource에서 초상화를 조회
2. 조회한 Sprite를 캐시에 저장
3. 이후 동일 몬스터는 캐시에서 즉시 반환

하도록 변경했다.

잘못된 ID로 인해 Sprite를 찾지 못한 경우의 `null` 결과도 캐시에 저장해 같은 실패 조회가 반복되지 않도록 했다.

---

### 3. 인벤토리 HUD Refresh 최적화

기존 `PlayerInventoryHudController`는 `Update()`에서 매 프레임 전체 인벤토리 UI를 다시 갱신하고 있었다.

이를 상태 변화 기반 Refresh 구조로 변경했다.

비교 대상에는 다음 상태가 포함된다.

- 인벤토리 슬롯 Item ID
- 수량
- Max Stack
- 선택 슬롯
- 이동 모드
- 버리기 확인창 상태
- HP / MP / 정력
- 전투 상태
- 현재 행동자
- 전투 중 플레이어 자원

상태 변화가 없으면 전체 UI Refresh를 건너뛰고, 실제 값이 변경됐을 때만 UI를 다시 갱신하도록 했다.

---

### 4. 인벤토리 액션 UI를 Scene 기반으로 전환

기존에는 `PlayerInventoryHudController`가 실행 중 다음 UI를 직접 생성했다.

- 사용 버튼
- 이동 버튼
- 버리기 버튼
- 버리기 확인 패널
- 1개 버리기
- 전부 버리기
- 취소 버튼

이 구조를 제거하고 `DungeonScene`에 실제 uGUI 오브젝트를 생성한 뒤 `[SerializeField]`로 연결하도록 변경했다.

이제 버튼 크기, 위치, 이미지, 폰트 등의 디자인을 코드 수정 없이 Unity Inspector에서 편집할 수 있다.

---

### 5. ItemDefinition 런타임 조회 캐싱

`RuntimeItemDefinitionLookup`에서 아이템 정보를 찾을 때마다 `Resources.FindObjectsOfTypeAll<ItemDefinition>()`를 실행하던 구조를 변경했다.

현재는 로드된 ItemDefinition을 한 번 조회한 뒤 다음 키들을 Dictionary에 등록한다.

- Item ID
- ScriptableObject 에셋 이름
- 표시 이름

Scene 변경 시 캐시를 무효화하고 다음 조회 때 다시 구성한다.

아이템 정의를 찾지 못한 경우 Max Stack 기본값은 기존 규칙과 동일하게 `1`을 사용한다.

---

### 6. 상자 UI를 IMGUI에서 uGUI로 전환

`ChestInteractionController`에서 사용하던 `OnGUI`, `GUI.Box`, `GUI.Button`, `GUI.Label` 기반 인터페이스를 제거했다.

새로운 `ChestInteractionView`를 추가하고 `DungeonScene`에 `ChestInteractionCanvas`를 구성했다.

현재 상자 UI에서 지원하는 흐름은 다음과 같다.

- 상자 열기 안내
- 상자 아이템 목록
- 플레이어 인벤토리 목록
- 아이템 획득
- 인벤토리 가득 참 안내
- 아이템 두고 가기
- 기존 아이템과 교체
- 교체 대상 슬롯 선택
- 취소 및 닫기

획득 및 저장 관련 기존 게임 로직은 `ChestInteractionController`에 유지하고, 표시와 입력 UI를 View 쪽으로 분리했다.

---

### 7. Debug Overlay Release Build 정리

디버그 Overlay 자체는 개발 중 문제 확인에 계속 사용할 가치가 있어 파일을 삭제하지 않았다.

대신 `DevelopmentOnlyBehaviourGate`를 추가해:

- Unity Editor: 사용 가능
- Development Build: 사용 가능
- 일반 Release Build: 대상 Debug Overlay 자동 비활성화

구조로 변경했다.

---

## Scene 변경

`DungeonScene`에 UI 및 직렬화 참조가 추가되었다.

주요 변경 내용:

- 인벤토리 Use / Move / Discard 버튼 연결
- 버리기 확인 UI 연결
- `ChestInteractionCanvas` 생성
- `ChestInteractionView` 연결
- `ChestInteractionController.interactionView` 연결
- Debug Overlay용 `DevelopmentOnlyBehaviourGate` 연결

---

## 추가된 테스트

96일차 변경에 맞춰 다음 EditMode 테스트를 추가했다.

- `BattleHudPortraitCacheTests`
- `PlayerInventoryHudRefreshOptimizationTests`
- `InventoryActionUiSerializationTests`
- `RuntimeItemDefinitionLookupCacheTests`
- `ChestInteractionUguiTests`
- `DevelopmentOnlyBehaviourGateTests`

주요 검증 대상은 초상화 캐시, 인벤토리 Refresh 조건, UI 직렬화 구조, ItemDefinition 캐시, IMGUI 제거 여부, Debug Gate 구조다.

---

## 오류 수정

통합 정리 과정에서 `RuntimeItemDefinitionLookup`이 존재하지 않는
`InventoryRunState.DefaultMaxStackSize`를 참조해 컴파일 오류가 발생했다.

기존 프로젝트의 실제 동작 규칙을 다시 확인하여, ItemDefinition을 찾지 못했을 때 Max Stack 기본값을 `1`로 반환하도록 수정했다.

---

## 최종 상태

96일차 작업을 통해 신규 기능 추가 전에 프로젝트의 UI와 런타임 구조를 정리했다.

- 불필요한 일회성 Installer 제거
- 반복 Resource 조회 감소
- 인벤토리 HUD 불필요 Refresh 감소
- 인벤토리 Action UI Scene 기반 전환
- ItemDefinition 조회 캐싱
- 상자 UI uGUI 전환
- Release Build의 Debug Overlay 차단

실제 플레이에서도 기존 탐험, 인벤토리, 상자, 전투 흐름이 정상 동작하는 것을 확인했다.

다음 일차부터는 정리된 UI 및 아이템 기반 위에서 후속 시스템 개발을 이어간다.
