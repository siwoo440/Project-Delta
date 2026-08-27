# Project Delta - 106일차 개발일지

## 작업 개요

106일차는 보물상자 등급 3종과 미믹 판정 로직을 만들었다.

지금까지 상자(25일차 `ChestContentMarker`)는 씬에 직접 배치한 아이템 이름 목록만 보여주는 최소 골격이었는데, 이번 일차에서 등급·잠금·강제개방·미믹이라는 새로운 규칙 계층을 추가했다.

기존 씬 상자와의 실제 연결, 미믹 전투 전환, 콘텐츠(어떤 상자가 어떤 등급인지, 미믹 몬스터 데이터)는 이번 범위에서 다루지 않는다. 107일차(이벤트 시스템)로 이어간다.

---

## 1. ChestRarity - 등급 3종과 미믹 확률

`Assets/ProjectDelta/Scripts/Domain/ChestRarity.cs`

일반·고급·희귀 3등급과, 기획서 수치 그대로 등급별 미믹 확률(8%·12%·18%)을 `ChestRarityRules`에 정의했다. `EquipmentRarityRules`(100일차)와 같은 패턴이다.

---

## 2. ChestRunState / ChestService - 상자 하나의 규칙

`Assets/ProjectDelta/Scripts/Domain/ChestRunState.cs`, `ChestService.cs`

`ChestRunState`는 상자 한 개의 상태(등급, 잠금 여부, 강제개방 시도 여부, 미믹 판정 결과, 보상 지급 여부)만 담는다. 상태를 바꾸는 메서드는 전부 `internal`로 막아뒀고, 실제 규칙 판단은 `ChestService`(같은 Domain 어셈블리)를 통해서만 이뤄진다 - 97일차 `EquipmentRunState`/`EquipmentService`와 같은 원칙이다.

`ChestService`가 제공하는 것:

- `UnlockWithKey` - 열쇠가 있으면 1개 소모하고 잠금 해제. 이미 열려 있거나 열쇠가 없으면 상태 변경 없이 실패.
- `ForceOpen` - 강제 개방은 항상 성공하지만 **상자당 딱 한 번만** 시도할 수 있다. 강제개방 이력을 잠금 상태보다 먼저 확인하도록 순서를 정했다 - 그래야 열쇠로 이미 열린 상자에 다시 강제개방을 시도해도 "아직 시도 안 한 것"처럼 보이지 않고 정확히 `AlreadyForceOpened`로 실패한다.
- `ResolveMimic` - 미믹 여부를 한 번만 확정한다. 두 번째 호출은 실패하고 첫 판정 결과가 그대로 유지된다(재판정 방지).
- `GrantReward` - 보상 지급을 한 번만 허용한다(중복 획득 방지). 실제 미믹 전투 승리 보상 로직이 이 게이트를 거치도록 나중에 연결하면 된다.

---

## 3. ChestMimicRollService - 실제 확률 굴림

`Assets/ProjectDelta/Scripts/Application/ChestMimicRollService.cs`

`RollIsMimic(rarity, random)`이 등급별 확률로 미믹 여부를 굴린다. `ChestService`(Domain)는 무작위성을 갖지 않고 이미 굴려진 `bool` 결과만 받아 확정하도록 분리했다 - 100일차 `EquipmentService`/`EquipmentRollService` 분리와 같은 원칙이다. 테스트에서 `System.Random`을 주입해 통계적 검증이 가능한 것도 이 분리 덕분이다.

---

## 4. 이번 일차에서 의도적으로 하지 않은 것

- **기존 상자 씬(`ChestContentMarker`/`RoomInstance`)과의 연결**을 하지 않았다 - 25일차부터 이어온 상자 저장/복원 로직(95일차에 남은 아이템 목록 분리 등)이 이미 복잡하고 잘 동작하고 있어서, 이번에 만든 새 규칙 계층을 섣불리 얹기보다는 별도로 완성한 뒤 신중하게 연결하는 편이 안전하다고 판단했다.
- **미믹 전투 전환**(기존 `ExplorationMonsterEncounterController`, 40~46일차)도 연결하지 않았다 - 미믹 몬스터 데이터 자체가 아직 콘텐츠로 없다.
- **"승리 시 원래 보상 + 추가 1개"의 실제 보상 내용**도 정하지 않았다 - 105일차 상점 상품 카탈로그와 같은 이유로, 실제 보상 풀이 콘텐츠 제작 단계에서 정해질 때 `ChestService.GrantReward`를 그 지점에 연결하면 된다.

---

## 5. 테스트

- `ChestRarityRulesTests` - 등급별 표시명, 미믹 확률이 기획서 수치(8/12/18)와 정확히 일치하는지.
- `ChestServiceTests` - 열쇠 개방 성공/실패(열쇠 없음·이미 열림), 강제개방 성공과 2회차 시도가 `AlreadyForceOpened`로 실패하는지, 미믹 판정이 한 번만 확정되고 재판정 시 첫 결과가 유지되는지, 보상 지급이 한 번만 허용되는지, 모든 메서드가 `null` 상자에 안전한지.
- `ChestMimicRollServiceTests` - `System.Random`의 내부 구현에 의존하지 않도록 정확한 값 대신 통계 검증. 희귀 등급이 일반 등급보다 미믹이 확실히 더 많이 나오는지, 일반 등급 다회 시행 결과가 8% 근처(±2.5%p)에 오는지.

---

## 6. Unity 에디터에서 확인해야 할 사항

1. Scene 변경 사항은 없다 - 이번 일차는 완전히 새로운 Domain/Application 로직이고, 기존 씬 상자와는 아직 연결되지 않았다.
2. 새 EditMode 테스트를 Unity Test Runner에서 실행해달라. 이 환경에는 Unity 에디터가 없어 직접 실행하지 못했다.
3. 재구성한 개발 일정 기준 다음은 107일차 - 이벤트 시스템(EventDefinition 구조 + 선택지 조건)이다.
