# Project Delta - 8일차 개발일지

## 개발 주제

**임시 파일 후 교체하는 원자적 저장 방식과 체크섬 검증·손상 파일 감지 구현**

7일차까지의 `File.WriteAllText` 직접 덮어쓰기 방식을 기획서 9.5절 "안전한 파일 쓰기" 절차로 교체했다. 저장 도중 프로세스가 죽어도 기존 정상 파일이 유지되고, 손상된 파일은 읽는 시점에 바로 감지된다.

---

## 개발 목표

- 저장 시 기존 파일을 직접 덮어쓰지 않고 임시 파일 → 검증 → 교체 절차 적용
- 체크섬 생성 및 검증으로 손상 파일 감지
- 재직렬화로 인한 체크섬 오탐 가능성 제거
- 더 이상 쓰지 않는 6일차 API 정리

---

## 구현 내용

### 1. SaveEnvelope 재설계 — Payload를 문자열로

6일차 `SaveEnvelope<T>`는 `Payload`를 객체로 가지고 있었다. 이 상태로 체크섬을 검증하려면 "읽은 객체를 다시 직렬화해서 비교"해야 하는데, JSON 재직렬화 과정에서 필드 순서나 포맷이 미세하게 달라질 수 있어 멀쩡한 파일도 손상으로 오판할 위험이 있었다.

```text
Before (6일차)          After (8일차)
SaveEnvelope<T>          SaveEnvelope (non-generic)
└─ Payload: T            ├─ PayloadJson: string
                          └─ Checksum: string
```

Payload를 저장 시점의 JSON 문자열 그대로 봉투에 넣어, 체크섬을 그 문자열 자체에 대해 계산·검증하도록 바꿨다. 재직렬화가 필요 없어 오탐 가능성이 사라졌다.

---

### 2. 체크섬

새 패키지 없이 .NET 기본 `System.Security.Cryptography.SHA256`을 사용했다.

```text
저장: payloadJson → SHA256 → Checksum 필드에 기록
읽기: 저장된 PayloadJson으로 SHA256 재계산 → Checksum과 비교
  일치 → 정상
  불일치 → 손상 파일로 판정, 예외 발생
```

---

### 3. 안전한 쓰기 절차 (9.5절)

```text
새 데이터 생성
↓
임시 파일(.tmp) 기록
↓
임시 파일 다시 읽기 + 체크섬 검증
↓ (검증 실패 시 .tmp 삭제하고 예외, 원본은 그대로 안전)
기존 파일 존재?
├─ 있음 → File.Replace(tmp, target, target+".bak") — 백업 이동 + 교체를 원자적으로 한 번에
└─ 없음 → File.Move(tmp, target)
```

`File.Replace`는 .NET이 제공하는 원자적 파일 교체 함수로, "기존 파일을 백업으로 이동"과 "임시 파일을 현재 파일로 교체"를 한 번의 호출로 처리한다. 두 단계로 나눠 직접 구현하는 것보다 중간에 깨질 여지가 없다.

백업은 오늘은 슬롯 1개(`.bak`)까지만 유지한다. 최근 3개를 순환 보관하는 정책은 9일차에서 확장한다.

---

### 4. 낡은 API 정리

6일차의 `SerializeProfile`/`DeserializeProfile`/`SerializeRun`/`DeserializeRun`/`SerializeSettings`/`DeserializeSettings`는 새 봉투 포맷과 더 이상 맞지 않고, 다른 곳에서도 쓰이지 않는 걸 확인한 뒤 제거했다. `WriteProfile`/`ReadProfile` 등 7일차부터 쓰던 공개 API 이름은 그대로 유지해 `AppRoot.cs`는 수정할 필요가 없었다.

---

## 적용 중 발견된 문제 및 수정

없음. Console 확인 결과 컴파일 에러 없음.

---

## 현재 8일차 전체 흐름

```text
Payload를 객체 대신 JSON 문자열로 봉투에 저장 (오탐 방지)
↓
SHA256으로 Checksum 계산 및 검증
↓
쓰기: 임시 파일 → 재검증 → File.Replace/Move로 원자적 교체
↓
읽기: 체크섬 불일치 시 손상 파일로 판정
↓
6일차의 낡은 Serialize/Deserialize API 제거
```

---

## 생성 파일

```text
Devlogs/Day08/README.md
```

---

## 수정 파일

```text
Assets/ProjectDelta/Scripts/Infrastructure/SaveEnvelope.cs
Assets/ProjectDelta/Scripts/Infrastructure/ISaveService.cs
Assets/ProjectDelta/Scripts/Infrastructure/SaveService.cs
```

---

## 삭제 파일

없음.

---

## 최종 확인 항목

8일차 완료 기준은 다음과 같다.

- Unity 컴파일 오류 없음
- 저장 시 기존 파일을 직접 덮어쓰지 않고 임시 파일을 거침
- 임시 파일 검증에 실패하면 원본 파일이 그대로 유지됨
- 체크섬이 불일치하는 파일을 읽으면 손상 파일로 판정하고 예외를 던짐
- 정상 저장 시 `.bak` 백업 파일이 생성됨

---

## 다음 개발 방향

다음 9일차에는 **최근 3개 백업 보존·복원 로직과 강제 종료·비정상 종료 후 런 복구 흐름**을 구현한다.

예정 흐름:

```text
.bak 슬롯 1개 → Backup1/Backup2/Backup3 3단계 순환으로 확장
↓
현재 파일 손상 시: Backup1 확인 → 손상 시 Backup2 → 손상 시 Backup3
↓
모든 백업 손상 시 복구 실패 처리
↓
강제 종료 후 재실행 시 저장 상태 확인 흐름 구현 (임시 파일 잔존 여부 등)
↓
복구 성공/실패를 AppRoot 부팅 로그에 반영
```
