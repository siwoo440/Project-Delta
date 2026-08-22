# Project Delta - 6일차 개발일지

## 개발 주제

**SaveService 인터페이스·저장 슬롯 구조·JSON 직렬화/역직렬화·저장 버전 필드**

기획서 9.5절 "저장 메타데이터"와 10.4절 "저장 서비스/저장 DTO"를 기반으로, 저장 파이프라인(`Domain State → Save Mapper → Save DTO → Serializer → File`)의 Serializer 단계를 구현했다. 4~5일차의 `RunData`/`RunContext`는 그대로 두고, 그 둘을 감싸는 저장 계층만 새로 얹었다.

---

## 개발 목표

- `ProfileData`/`RunData`/`SettingsData`를 감싸는 저장 메타데이터 봉투(`SaveEnvelope<T>`) 구현
- `ISaveService` 인터페이스로 직렬화/역직렬화 책임을 분리
- Newtonsoft.Json 기반 `SaveService` 구현
- 저장 버전 필드(`SaveVersion`) 확정
- `AppRoot`의 "Save system init skipped" 자리를 실제 서비스 등록으로 교체

---

## 구현 내용

### 1. 저장 파이프라인에서 오늘 위치

```text
Domain State(RunContext, 5일차) → Save Mapper(미구현) → Save DTO(RunData, 4일차) → Serializer(오늘) → File(7일차 이후)
```

`Save Mapper`(RunContext↔RunData 변환)와 실제 파일 I/O는 이번 일차 범위가 아니다. 오늘은 DTO를 문자열(JSON)로 왕복시키는 부분만 구현했다.

---

### 2. SaveEnvelope 구현

기획서 9.5절 저장 메타데이터 8개 필드 중 `checksum`을 제외한 7개를 반영했다.

```text
SaveEnvelope<T>
├─ SaveVersion (상수 CurrentSaveVersion = 1)
├─ GameVersion
├─ ContentVersion (TODO: 콘텐츠 버전 체계 확정 시 채움)
├─ CreatedAtIso8601 / ModifiedAtIso8601
├─ Platform
├─ SaveState (RunData 전용, Profile/Settings는 비움)
└─ Payload
```

`checksum`은 8일차(원자적 저장)에서 함께 추가한다. `CreatedAtIso8601` 보존(기존 파일의 최초 생성 시각 유지)은 실제 파일 I/O가 붙는 7일차 이후 처리한다.

---

### 3. ISaveService / SaveService 구현

```text
ISaveService
├─ SerializeProfile / DeserializeProfile
├─ SerializeRun(run, saveState) / DeserializeRun
└─ SerializeSettings / DeserializeSettings
```

Unity 기본 `JsonUtility`는 `ProfileData.PermanentRecord.NpcAffinity`(Dictionary)를 직렬화하지 못해, 3일차에 Addressables 종속 패키지로 이미 설치된 `Newtonsoft.Json`을 사용했다. 도메인 시스템이 파일을 직접 열거나 JSON을 작성하지 않는다는 10.4절 원칙에 따라, 직렬화 로직은 전부 `SaveService` 안에 캡슐화했다.

---

### 4. AppRoot 연결

`SaveService`는 비동기 초기화가 필요 없는 단순 객체라 2~3일차와 같은 방식으로 바로 등록했다.

```text
로그 → 설정(TODO) → Localization → Input → 오디오(TODO)
→ Save 서비스 등록 (오늘 구현)
→ 프로필 불러오기 (TODO, 파일 경로 붙는 이후 일차)
→ Addressables → Steam(TODO) → Cloud(TODO) → SceneLoader → TitleScene
```

---

## 적용 중 발견된 문제 및 수정

없음. Console 확인 결과 컴파일 에러 없음.

---

## 현재 6일차 전체 흐름

```text
저장 메타데이터 7개 필드 확정 (checksum 제외)
↓
SaveEnvelope<T>로 Profile/Run/Settings 공통 래핑
↓
Newtonsoft.Json으로 직렬화/역직렬화 구현
↓
ISaveService를 AppRoot 초기화 순서에 등록
↓
실제 파일 경로·저장/불러오기는 다음 일차로 이연
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Infrastructure/SaveEnvelope.cs
Assets/ProjectDelta/Scripts/Infrastructure/ISaveService.cs
Assets/ProjectDelta/Scripts/Infrastructure/SaveService.cs
Devlogs/Day06/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Infrastructure/AppRoot.cs
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

6일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음 (Console 확인 완료)
- `SaveEnvelope<T>.Wrap()`이 메타데이터 7개 필드를 채워 반환함
- `SaveService`가 Profile/Run/Settings 세 종류를 각각 직렬화·역직렬화함
- `SaveVersion`이 상수로 고정되어 있음
- `AppRoot`가 부팅 시 `ISaveService`를 정상 등록함

---

## 다음 개발 방향

다음 7일차에는 **저장 경로·파일명 규칙(프로필/런/설정 파일 분리), 수동·자동 저장 호출 지점과 공통 API**를 구현한다.

예정 흐름:

```text
저장 경로 규칙 확정 (Application.persistentDataPath 기준)
↓
프로필/런/설정 파일명 분리
↓
SaveService에 실제 파일 쓰기/읽기 추가
↓
수동·자동 저장 호출 지점 정의 (9.2절 자동 저장 시점 표 참고)
↓
AppRoot의 "프로필 불러오기 (TODO)" 자리를 실제 구현으로 교체
```
