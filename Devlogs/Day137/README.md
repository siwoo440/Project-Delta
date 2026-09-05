# 137일차 : 키 리매핑 구현

## 목표
- 기획서 8.1절 "UI·그래픽 기본 방향" 중 "키보드/마우스/게임패드 리매핑" 구현

## 구현 내용

### 1. 입력 서비스 확장
- `IInputService`/`InputService` - `GetBindingDisplayString()`(현재 바인딩을 사람이
  읽을 수 있는 문자열로), `StartRebind()`(Unity Input System의
  `PerformInteractiveRebinding` 활용 - 장치를 가리지 않고 다음 입력을 그대로 새
  바인딩으로 받음, 마우스 포인터 위치/이동 같은 잡음성 컨트롤만 제외), `ApplyBindingOverride(s)`
  (앱 시작 시 저장된 리매핑을 실제 액션에 반영)

### 2. ApplicationFlow 중계
- Presentation은 Infrastructure(InputService)를 직접 참조하지 않는다는 기존 원칙(119일차)에
  맞춰, 프로필·설정과 같은 방식으로 `ApplicationFlow`가 중계
- `GetKeyBindingDisplayString()`/`StartKeyRebind()`/`SaveKeyBinding()`/
  `ApplyKeyBindingsFromSettings()` 추가
- 저장은 `SettingsData.KeyBindings`(이미 있던 `KeyBindingEntry` 리스트)에
  `"Exploration/MoveForward"` 형식 ID로 upsert - 재설정할 때마다 즉시 저장
- `AppRoot`가 게임 시작 시 저장된 리매핑을 적용한 뒤 타이틀로 진입

### 3. 키 설정 화면
- `SettingsSceneController`에 "키 설정" 패널 추가 - 이동 4방향 + 상호작용, 총 5개
  액션을 나열
- 각 항목의 버튼이 현재 바인딩을 표시하고, 누르면 "입력 대기 중..."으로 바뀌며 다음
  입력(키보드/마우스/게임패드 상관없이)을 그대로 새 바인딩으로 저장

## 범위 참고
- `Exploration` 맵은 원래 게임패드용 이동 바인딩이 없어서(게임패드는 UI 탐색에만
  매핑), 오늘은 "장치 상관없이 하나의 바인딩을 덮어쓰는" 방식까지만 다뤘다.
  키보드와 게임패드를 별도 컨트롤 스킴으로 동시에 유지하는 진짜 다중 스킴 지원은
  이후 과제로 남긴다
