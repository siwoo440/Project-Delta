# Project Delta - 87일차 개발 일지

- 개발일: 2026-08-26
- 최신 커밋: `1bfc73c1111e7932f5aa5d191276e51867ec193c`
- 기준 커밋: `99cf47737a9ab3b5976ec1d751c8cc974e4a6ba9`
- 현재 커밋 메시지: `a`
- 개발 주제: ScriptableObject 기반 Balance Editor 제작 및 데이터 검증 기능 추가

---

## 1. 개발 목표

이번 일차의 목표는 Project Delta의 밸런스 데이터를 Unity Inspector 여러 곳에서 개별적으로 찾지 않고 하나의 Editor Window에서 관리할 수 있도록 만드는 것이다.

기존 런타임 전투 구조와 ScriptableObject 데이터 원본은 유지하고, Unity Editor 전용 기능만 추가하는 방향으로 구현했다.

주요 목표는 다음과 같다.

- Monster / Skill / Status / Growth / Drop 데이터를 하나의 창에서 관리
- Project 내부 ScriptableObject 자동 검색
- 에셋 이름, ID, Display Name 검색
- 선택한 원본 ScriptableObject 직접 수정
- 명백하게 잘못된 밸런스 값 경고
- Growth 데이터의 레벨별 필요 경험치 및 누적 경험치 확인
- Unity Undo 지원
- Asset 저장 기능 제공
- 별도 Scene / Prefab / Hierarchy 연결 없이 사용
- Editor 기능에 대한 EditMode 테스트 추가

---

## 2. Balance Editor Window 구현

신규 파일:

`Assets/ProjectDelta/Scripts/Editor/ProjectDeltaBalanceEditorWindow.cs`

Unity 상단 메뉴에 다음 항목을 추가했다.

`Project Delta → Balance Editor`

Balance Editor는 다음 5개 탭으로 구성된다.

1. Monster
2. Skill
3. Status
4. Growth
5. Drop

각 탭은 `AssetDatabase.FindAssets()`를 사용해 현재 프로젝트에 존재하는 해당 타입의 ScriptableObject를 자동으로 수집한다.

에셋을 새로 만들거나 삭제한 경우에도 창 포커스 또는 Project 변경 시 목록을 다시 불러오도록 구성했다.

---

## 3. 원본 ScriptableObject 직접 편집

Balance Editor는 별도의 밸런스 데이터 사본을 만들지 않는다.

선택한 ScriptableObject를 `SerializedObject`로 감싸 Unity가 직렬화하는 실제 필드를 그대로 표시한다.

따라서 Balance Editor에서 수정한 값은 기존 Inspector에서 수정한 값과 동일한 원본 Asset에 적용된다.

`m_Script` 필드는 수정하지 못하도록 비활성화하고 나머지 SerializedProperty는 기존 Inspector 방식으로 표시한다.

변경 사항은 `ApplyModifiedProperties()`를 통해 적용되므로 Unity Undo 흐름을 사용할 수 있다.

---

## 4. 데이터 검색 기능

신규 파일:

`Assets/ProjectDelta/Scripts/Editor/BalanceEditorUtility.cs`

검색은 다음 값을 대상으로 한다.

- Asset 이름
- Definition ID
- Display Name
- 데이터 타입 이름

검색은 대소문자를 구분하지 않는다.

검색어가 비어 있으면 현재 탭의 모든 데이터를 표시한다.

목록에서는 가능한 경우 다음 형식으로 데이터를 표시한다.

`ID | Display Name`

ID 또는 Display Name이 없는 데이터는 사용 가능한 이름으로 자동 대체한다.

---

## 5. 밸런스 데이터 기본 검증

Balance Editor는 잘못된 값을 자동 수정하지 않는다.

원본 값을 그대로 유지한 상태에서 명확하게 잘못된 값만 경고 HelpBox로 표시한다.

### Monster

- 최대 HP가 1 미만인지 확인
- 최대 MP가 음수인지 확인
- 경험치 보상이 음수인지 확인

### Skill

- 마나 비용 음수 확인
- 정력 비용 음수 확인
- 피해 배율 확인
- 치명타 확률 0~100 범위 확인
- 치명타 확률과 치명타 배율 조합 확인
- 상태이상 기본 확률 0~100 범위 확인
- 상태이상 부여 시 지속 라운드 확인

### Status

- 최대 중첩 수 확인
- 라운드 종료 적용 절대값 확인

### Growth

- 최대 레벨 확인
- 레벨당 스탯 포인트 확인
- 최대 레벨과 경험치 배열 길이 일치 여부 확인
- 각 레벨 필요 경험치가 1 이상인지 확인

### Drop

- 최소/최대 골드 범위 확인
- 최대 골드가 최소 골드보다 작은지 확인
- 드롭 Item 누락 확인
- 드롭 확률 Basis Point 범위 확인
- 최소/최대 수량 확인
- 최대 수량이 최소 수량보다 작은지 확인

---

## 6. Growth 경험치 요약

Growth 탭에서는 기존 `experienceToNextLevel` 배열을 수정할 수 있으며, 그 아래에 계산용 요약 표를 추가했다.

표에는 다음 정보가 표시된다.

- 현재 레벨
- 다음 레벨
- 해당 구간 필요 EXP
- 해당 레벨까지의 누적 EXP

누적 경험치는 Editor에서 확인하기 위한 계산값이며 새로운 런타임 데이터로 저장하지 않는다.

예시:

| 구간 | 필요 EXP | 누적 EXP |
| --- | ---: | ---: |
| Lv.1 → Lv.2 | 100 | 100 |
| Lv.2 → Lv.3 | 150 | 250 |
| Lv.3 → Lv.4 | 220 | 470 |

---

## 7. 편집 편의 기능

Balance Editor에 다음 기능을 추가했다.

- 검색창
- 검색 초기화
- Asset 목록 새로고침
- 선택 Asset을 Project 창에서 Ping
- 전체 Asset 저장 버튼
- 현재 검색 결과 수 표시
- 선택 Asset 변경 시 Inspector 스크롤 초기화
- 기본 검증 결과 표시

저장 버튼은 `AssetDatabase.SaveAssets()`를 사용한다.

일반 Unity 작업과 동일하게 `Ctrl + S`를 사용해도 저장할 수 있다.

---

## 8. EditMode 테스트 추가

신규 파일:

`Assets/ProjectDelta/Tests/EditMode/BalanceEditorUtilityTests.cs`

다음 항목을 테스트하도록 구성했다.

1. 검색어가 비어 있으면 Asset이 검색되는지 확인
2. ID 및 Display Name 검색이 대소문자를 구분하지 않는지 확인
3. 목록 표시명이 ID와 Display Name을 포함하는지 확인
4. Monster의 잘못된 HP 값 경고 확인
5. Drop의 최소/최대 골드 역전 경고 확인
6. Growth 누적 경험치 계산 확인

Editor Utility를 테스트할 수 있도록 기존 EditMode 테스트 asmdef에 다음 참조를 추가했다.

`ProjectDelta.Editor`

---

## 9. 변경 파일

이번 커밋은 86일차 기준 커밋보다 정확히 1커밋 앞서 있으며 다음 파일이 변경되었다.

### 생성

- `Assets/ProjectDelta/Scripts/Editor/BalanceEditorUtility.cs`
- `Assets/ProjectDelta/Scripts/Editor/BalanceEditorUtility.cs.meta`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaBalanceEditorWindow.cs`
- `Assets/ProjectDelta/Scripts/Editor/ProjectDeltaBalanceEditorWindow.cs.meta`
- `Assets/ProjectDelta/Tests/EditMode/BalanceEditorUtilityTests.cs`
- `Assets/ProjectDelta/Tests/EditMode/BalanceEditorUtilityTests.cs.meta`

### 수정

- `Assets/ProjectDelta/Tests/EditMode/ProjectDelta.Tests.EditMode.asmdef`
- `Project-Delta.slnx`

### 삭제

- 없음

`Project-Delta.slnx`는 `ProjectDelta.Editor.csproj` 항목의 순서만 이동했으며 프로젝트 항목 자체의 추가/삭제는 없다. 기능 코드에는 영향을 주지 않는 IDE/Unity 생성 파일 정렬 변경이다.

---

## 10. 최종 사용 흐름

1. Unity 실행
2. 상단 메뉴에서 `Project Delta → Balance Editor` 실행
3. Monster / Skill / Status / Growth / Drop 탭 선택
4. 왼쪽 목록에서 데이터 선택
5. 오른쪽에서 원본 ScriptableObject 수정
6. 하단 경고 확인
7. 필요하면 Growth 누적 EXP 확인
8. `Ctrl + S` 또는 `모든 수정 저장` 실행
9. 필요 시 EditMode Test Runner 실행

별도의 Scene, Prefab, GameObject, Hierarchy, Inspector 연결 작업은 필요하지 않는다.

---

## 11. 검증 결과

최신 GitHub 커밋:

`1bfc73c1111e7932f5aa5d191276e51867ec193c`

기준 커밋:

`99cf47737a9ab3b5976ec1d751c8cc974e4a6ba9`

두 커밋 비교 결과:

- 상태: ahead
- 커밋 차이: 1
- 변경 파일: 8개
- 삭제 파일: 없음

Balance Editor 핵심 파일 3개와 EditMode asmdef의 Git blob SHA를 직전 생성본과 비교했으며 모두 최신 커밋과 일치했다.

정적 확인:

- C# 신규 파일 중괄호 균형 확인
- EditMode asmdef JSON 구문 확인
- `ProjectDelta.Editor` 어셈블리의 Data/Application/Infrastructure/Domain/Presentation 참조 확인
- EditMode 테스트 어셈블리의 `ProjectDelta.Editor` 참조 확인
- Skill / Monster / Growth 데이터의 실제 SerializedField 이름과 검증 코드의 필드 이름 대조
- `Project-Delta.slnx` 변경 내용 확인

GitHub에는 해당 커밋에 등록된 CI status가 없고 관련 workflow run도 없다.

현재 작업 환경에는 Unity 실행 파일, `dotnet`, `csc`, `mcs`가 없어 실제 Unity 컴파일과 Test Runner 실행 결과는 확인할 수 없다.

따라서 소스 및 저장소 구조 기준으로 진행을 막는 문제는 발견되지 않았으며, 최종 런타임 검증은 로컬 Unity의 Console과 EditMode Test Runner에서 확인한다.

---

## 12. 87일차 완료 내용

87일차에서는 기존 ScriptableObject 밸런스 데이터를 유지하면서 Unity Editor에서 한 번에 확인하고 수정할 수 있는 Balance Editor를 추가했다.

몬스터, 스킬, 상태이상, 성장, 드롭 데이터를 탭으로 분리하고 검색, 직접 편집, 데이터 검증, Growth 누적 경험치 확인, Asset 저장 기능을 제공한다.

또한 검색·경고·성장 계산을 별도 Utility로 분리하고 EditMode 테스트를 추가해 이후 밸런스 데이터 종류와 검증 규칙을 확장하기 쉬운 기반을 마련했다.
