# Project Delta - 7일차 개발일지

## 개발 주제

**저장 경로·파일명 규칙(프로필/런/설정 파일 분리), 수동·자동 저장 호출 지점과 공통 API 구현**

6일차까지 만든 JSON 직렬화 위에, 실제로 디스크에 파일을 쓰고 읽는 계층을 얹었다. 기획서 9.6절 저장 위치 규칙과 10.4절 "도메인 시스템이 파일을 직접 열지 않는다" 원칙을 반영했다.

---

## 개발 목표

- 저장 위치 규칙(설치/문서 폴더 금지, `persistentDataPath` 사용) 반영
- 프로필/런/설정 파일을 물리적으로 분리
- `ISaveService`에 실제 파일 읽기/쓰기 메서드 추가
- 도메인 시스템이 파일을 직접 다루지 않도록 공통 API로 캡슐화
- `AppRoot`의 "프로필 불러오기" TODO를 실제 구현으로 교체

---

## 구현 내용

### 1. SavePaths — 저장 위치 규칙

```text
{Application.persistentDataPath}/Saves/
├─ profile.json
├─ run.json
└─ settings.json
```

기획서 9.6절 원칙(*"게임 설치 폴더나 사용자 문서 폴더에 직접 저장하지 않는다"*)에 따라 플랫폼별 영구 데이터 경로만 사용했다. 4일차에 세 데이터를 독립 클래스로 분리한 것을 파일 단위까지 그대로 이어갔다.

---

### 2. ISaveService / SaveService — 실제 파일 I/O 추가

```text
WriteProfile / ReadProfile / HasProfile
WriteRun / ReadRun / HasRun / DeleteRun
WriteSettings / ReadSettings / HasSettings
```

`DeleteRun`은 9.2절 "새 회차 시작 시 기존 RunData만 삭제한다" 규칙을 위해 미리 만들어뒀다 (아직 호출하는 새 게임 흐름은 없음).

---

### 3. 공통 API로 캡슐화

10.4절 원칙(*"도메인 시스템이 파일을 직접 열거나 JSON을 작성하지 않는다"*)에 따라, 파일 경로·직렬화 방식은 `SaveService` 내부에만 있고 바깥에서는 이 메서드들만 호출한다. 9.2절 자동 저장 시점 표의 실제 호출 지점(이벤트 선택, 전투 종료 등 20여 개)은 해당 시스템이 생기는 이후 일차에 하나씩 연결한다 — 오늘은 API만 준비했다.

---

### 4. AppRoot 연결

```text
프로필 파일 있음 → 불러오기
프로필 파일 없음 → 새 ProfileData 생성 후 즉시 저장
```

다만 불러온 프로필을 아직 들고 있을 곳(ProfileContext 등)이 없어 지금은 값을 버린다 — 실제로 필요한 화면(타이틀 등)이 생기는 일차에 보관 지점을 만들 예정이며, 코드에 TODO로 남겨뒀다.

---

## 적용 중 발견된 문제 및 수정

### 5. `ProjectDelta.Application`과 `UnityEngine.Application` 네임스페이스 충돌

`SavePaths.cs`에서 `Application.persistentDataPath`를 썼다가 다음 컴파일 에러가 발생했다.

```text
error CS0234: The type or namespace name 'persistentDataPath' does not exist
in the namespace 'ProjectDelta.Application'
```

원인: 2일차에 화면 흐름용으로 만든 `ProjectDelta.Application` 네임스페이스가 `UnityEngine.Application`과 이름이 같다. `ProjectDelta.Infrastructure` 안에서 `Application`을 짧게 쓰면, C#이 `using UnityEngine;`보다 같은 상위 네임스페이스(`ProjectDelta`)의 형제 네임스페이스를 먼저 찾아 `ProjectDelta.Application`으로 해석해버린다.

`SaveEnvelope.cs`는 처음부터 `UnityEngine.Application.version`처럼 완전한 이름을 썼기 때문에 문제가 없었다. `SavePaths.cs`도 `UnityEngine.Application.persistentDataPath`로 완전한 이름을 명시해 해결했다. 앞으로 `ProjectDelta.*` 네임스페이스 안에서 `Application`을 쓸 때는 항상 `UnityEngine.Application`으로 완전히 적어야 한다.

---

## 현재 7일차 전체 흐름

```text
저장 위치 규칙 확정 (persistentDataPath/Saves)
↓
프로필/런/설정 파일 물리적 분리
↓
SaveService에 실제 파일 읽기/쓰기 추가
↓
AppRoot 부팅 시 프로필 로드-또는-생성
↓
Application 네임스페이스 충돌 발견 및 수정
```

---

## 생성 파일

```text
Assets/ProjectDelta/Scripts/Infrastructure/SavePaths.cs
Devlogs/Day07/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Infrastructure/ISaveService.cs
Assets/ProjectDelta/Scripts/Infrastructure/SaveService.cs
Assets/ProjectDelta/Scripts/Infrastructure/AppRoot.cs
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

7일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음 (네임스페이스 충돌 수정 후 확인 완료)
- 저장 파일이 `persistentDataPath` 하위에만 생성됨
- 프로필/런/설정 파일이 서로 다른 파일로 분리됨
- `SaveService`가 파일 존재 여부에 따라 로드/신규 생성을 올바르게 분기함
- `DeleteRun()`이 준비되어 있음 (호출 지점은 이후 일차)

---

## 다음 개발 방향

다음 8일차에는 **임시 파일 후 교체하는 원자적 저장 방식과 체크섬 검증·손상 파일 감지**를 구현한다.

예정 흐름:

```text
새 데이터 → 임시 파일 기록 → 기록 완료 확인
↓
체크섬 생성 → 임시 파일 다시 읽기 → 데이터 검증
↓
기존 파일 → 임시 파일로 교체 (원자적 교체)
↓
SaveEnvelope에 남겨뒀던 checksum 필드 실제 구현
↓
로드 시 체크섬 불일치 → 손상 파일로 판정
```
