using System; // 고유 시험 경로 생성
using System.IO; // 시험 저장 파일 검사
using System.Linq; // 백업 파일 수 확인
using System.Reflection; // 구현 전 저장 기능 조회
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 관찰 기록
using ProjectDelta.Runtime; // 저장 구현 형식 조회
using UnityEngine; // 격리된 프로젝트 경로
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class ObservationStorageTests // 관찰 기록 저장과 안전한 복구 검증
    { // 클래스 시작
        private string root; // 테스트 전용 저장 폴더
        private Type storeType; // 관찰 저장 기능 형식
        [SetUp] // 각 검증의 독립 경로 준비
        public void Prepare() // 사용자 저장과 분리된 경로 생성
        { // 준비 시작
            root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Artifacts", "ObservationStorageTests", Guid.NewGuid().ToString("N"))); // 프로젝트 안의 고유 시험 경로
            Directory.CreateDirectory(root); // 시험 폴더 준비
            storeType = typeof(AppRoot).Assembly.GetType("ProjectDelta.Runtime.ObservationStorage"); // 실제 저장 기능 조회
            Assert.That(storeType, Is.Not.Null); // 저장 기능 제공 확인
        } // 준비 끝
        private object Store() // 독립 저장 기능 생성
        { // 생성 시작
            return Activator.CreateInstance(storeType, root); // 시험 경로를 사용하는 저장 기능
        } // 생성 끝
        private ObservationJournal Load(object store, out string warning) // 저장된 기록과 안내 조회
        { // 조회 시작
            var arguments = new object[] // 기록 조회 인수
            { // 인수 목록 시작
                null // 복구 안내 출력
            }; // 인수 목록 끝
            var journal = (ObservationJournal)storeType.GetMethod("Load").Invoke(store, arguments); // 실제 저장 기록 조회
            warning = (string)arguments[0]; // 복구 또는 오류 안내
            return journal; // 확인한 관찰 기록
        } // 조회 끝
        private bool Save(object store, ObservationJournal journal, out string error) // 관찰 기록 저장 요청
        { // 저장 시작
            var arguments = new object[] // 저장 인수
            { // 인수 목록 시작
                journal, null // 기록과 오류 안내
            }; // 인수 목록 끝
            var saved = (bool)storeType.GetMethod("TrySave").Invoke(store, arguments); // 실제 안전 저장 요청
            error = (string)arguments[1]; // 저장 실패 안내
            return saved; // 실제 저장 성공 여부
        } // 저장 끝
        [Test] // 저장과 새 인스턴스 읽기 검증
        public void Save_ReloadsRecordedObservation() // 새 실행의 관찰 정보 보존 확인
        { // 검증 시작
            var store = Store(); // 첫 저장 기능
            var journal = Load(store, out var warning); // 기존 관찰 기록 조회
            Assert.That(warning, Is.Empty); // 새 저장의 오류 없음
            Assert.That(journal.TryRecord("trial-elite"), Is.True); // 시험 대상 관찰 기록
            Assert.That(Save(store, journal, out var error), Is.True); // 실제 기록 저장
            Assert.That(error, Is.Empty); // 성공한 저장의 오류 없음
            var loaded = Load(Store(), out warning); // 새 저장 기능에서 다시 읽기
            Assert.That(warning, Is.Empty); // 정상 저장 복구 안내 없음
            Assert.That(loaded.Contains("trial-elite"), Is.True); // 재실행 후 관찰 정보 보존
        } // 검증 끝
        [Test] // 손상된 현재 파일과 백업 검증
        public void Load_CorruptCurrentUsesBackupWithoutOverwritingOriginal() // 손상 보존과 백업 읽기
        { // 검증 시작
            var store = Store(); // 첫 저장 기능
            var journal = Load(store, out _); // 빈 관찰 기록 조회
            Assert.That(journal.TryRecord("first"), Is.True); // 첫 관찰 기록
            Assert.That(Save(store, journal, out _), Is.True); // 첫 정상 저장
            Assert.That(journal.TryRecord("second"), Is.True); // 두 번째 관찰 기록
            Assert.That(Save(store, journal, out _), Is.True); // 백업을 남기는 두 번째 저장
            var current = Path.Combine(root, "observation-v1.json"); // 현재 시험 저장 파일
            File.WriteAllText(current, "damaged test data"); // 현재 파일 손상 모사
            var corrupt = File.ReadAllBytes(current); // 손상 원본 보관
            var recoveredStore = Store(); // 새 실행의 저장 기능
            var recovered = Load(recoveredStore, out var warning); // 손상 파일과 백업 읽기
            Assert.That(warning, Is.Not.Empty); // 사용자에게 복구 상태 안내
            Assert.That(recovered.Contains("first"), Is.True); // 최근 정상 백업에서 기록 복구
            Assert.That(recovered.Contains("second"), Is.False); // 백업 이후 변경은 복구 불가
            Assert.That(Save(recoveredStore, recovered, out var error), Is.False); // 손상 원본의 자동 덮어쓰기 차단
            Assert.That(error, Is.Not.Empty); // 저장 차단 이유 안내
            Assert.That(File.ReadAllBytes(current), Is.EqualTo(corrupt)); // 손상 원본 그대로 보존
        } // 검증 끝
        [Test] // 임시 파일과 최신 백업의 복구 순서 검증
        public void Load_PrefersNewestValidRecoveryCandidate() // 오래된 임시 파일보다 최신 백업 확인
        { // 검증 시작
            var store = Store(); // 시험 저장 기능
            var journal = Load(store, out _); // 빈 관찰 기록 조회
            journal.TryRecord("first"); // 첫 관찰 기록
            Assert.That(Save(store, journal, out _), Is.True); // 첫 정상 저장
            var current = Path.Combine(root, "observation-v1.json"); // 현재 시험 저장 파일
            var stale = Path.Combine(root, "observation-0000000000000000001-00000000000000000000000000000000.tmp"); // 오래된 임시 파일 이름
            File.Copy(current, stale); // 오래된 정상 임시 파일 모사
            journal.TryRecord("second"); // 두 번째 관찰 기록
            Assert.That(Save(store, journal, out _), Is.True); // 두 번째 정상 저장
            journal.TryRecord("third"); // 세 번째 관찰 기록
            Assert.That(Save(store, journal, out _), Is.True); // 최신 백업이 생기는 저장
            File.WriteAllText(current, "damaged test data"); // 현재 파일 손상 모사
            var recovered = Load(Store(), out var warning); // 복구 후보 우선순위 확인
            Assert.That(warning, Is.Not.Empty); // 복구 상태 안내
            Assert.That(recovered.Contains("second"), Is.True); // 오래된 임시 파일보다 최신 백업 사용
            Assert.That(recovered.Contains("third"), Is.False); // 손상된 최신 기록은 복구하지 않음
            Assert.That(File.Exists(stale), Is.True); // 오래된 임시 파일 보존
        } // 검증 끝
        [Test] // 같은 저장 시도의 임시 기록과 백업 비교
        public void Load_PrefersPendingRecordToBackupFromSameAttempt() // 실패한 교체의 최신 기록 우선 복구
        { // 검증 시작
            var store = Store(); // 시험 저장 기능
            var journal = Load(store, out _); // 빈 관찰 기록 조회
            journal.TryRecord("first"); // 첫 관찰 기록
            Assert.That(Save(store, journal, out _), Is.True); // 첫 정상 저장
            var current = Path.Combine(root, "observation-v1.json"); // 현재 시험 저장 파일
            var previous = File.ReadAllBytes(current); // 이전 기록의 백업 내용
            journal.TryRecord("second"); // 두 번째 관찰 기록
            Assert.That(Save(store, journal, out _), Is.True); // 두 번째 정상 저장
            var pending = File.ReadAllBytes(current); // 교체를 기다리는 최신 내용
            var transaction = "9000000000000000000"; // 같은 저장 시도의 고정 시각
            File.WriteAllBytes(Path.Combine(root, "observation-" + transaction + "-a.tmp"), pending); // 교체 전 임시 파일 모사
            File.WriteAllBytes(Path.Combine(root, "observation-backup-" + transaction + "-b.json"), previous); // 같은 시도의 이전 기록 백업
            File.WriteAllText(current, "damaged test data"); // 현재 파일 손상 모사
            var recovered = Load(Store(), out var warning); // 안전한 복구 후보 조회
            Assert.That(warning, Is.Not.Empty); // 복구 상태 안내
            Assert.That(recovered.Contains("second"), Is.True); // 같은 시도의 최신 임시 기록 우선
        } // 검증 끝
        [Test] // 자동 검증의 사용자 저장 격리 검증
        public void ResolveDirectory_BatchDefaultsToIsolatedProjectArtifacts() // 배치 기본 경로의 저장 격리 확인
        { // 검증 시작
            if (!Application.isBatchMode) // 배치 테스트 실행 여부 검사
            { // 편집기 실행 조건 시작
                Assert.Ignore("배치 경로 격리는 배치 모드에서 검증"); // 일반 편집기 실행 제외
            } // 편집기 실행 조건 끝
            var previousRoot = Environment.GetEnvironmentVariable("PROJECT_DELTA_OBSERVATION_TEST_ROOT"); // 기존 검증 환경 경로
            var overrideProperty = storeType.GetProperty("TestDirectoryOverride"); // 개별 테스트 경로 조회
            var previousOverride = overrideProperty.GetValue(null); // 기존 테스트 경로 보관
            try // 환경 변수 없이 배치 기본 경로 검사
            { // 경로 검사 시작
                Environment.SetEnvironmentVariable("PROJECT_DELTA_OBSERVATION_TEST_ROOT", null); // 명시적 검증 경로 제거
                overrideProperty.SetValue(null, null); // 개별 테스트 경로 제거
                var directory = (string)storeType.GetMethod("ResolveDirectory").Invoke(null, null); // 실제 배치 기본 경로
                var artifacts = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Artifacts")); // 현재 테스트 프로젝트의 결과 폴더
                Assert.That(Path.GetFullPath(directory), Does.StartWith(artifacts + Path.DirectorySeparatorChar)); // 프로젝트 결과 폴더 안의 격리 확인
            } // 경로 검사 끝
            finally // 원래 검증 환경 복구
            { // 환경 복구 시작
                overrideProperty.SetValue(null, previousOverride); // 기존 테스트 경로 복구
                Environment.SetEnvironmentVariable("PROJECT_DELTA_OBSERVATION_TEST_ROOT", previousRoot); // 기존 검증 환경 경로 복구
            } // 환경 복구 끝
        } // 검증 끝
        [Test] // 체크섬 손상 파일 검증
        public void Load_TamperedPayloadIsNotTrustedOrOverwritten() // 데이터 변경과 원본 보호 확인
        { // 검증 시작
            var store = Store(); // 시험 저장 기능
            var journal = Load(store, out _); // 빈 관찰 기록 조회
            journal.TryRecord("trial-elite"); // 시험 대상 관찰 기록
            Assert.That(Save(store, journal, out _), Is.True); // 정상 관찰 기록 저장
            var current = Path.Combine(root, "observation-v1.json"); // 현재 시험 저장 파일
            var text = File.ReadAllText(current).Replace("trial-elite", "other-elite"); // 체크섬을 바꾸지 않은 본문 변경
            File.WriteAllText(current, text); // 조작된 시험 파일 기록
            var original = File.ReadAllBytes(current); // 손상된 원본 보관
            var damagedStore = Store(); // 새 실행의 저장 기능
            var loaded = Load(damagedStore, out var warning); // 체크섬 검증
            Assert.That(warning, Is.Not.Empty); // 손상 안내 확인
            Assert.That(loaded.Contains("other-elite"), Is.False); // 조작된 기록을 신뢰하지 않음
            Assert.That(Save(damagedStore, loaded, out _), Is.False); // 손상 파일 덮어쓰기 차단
            Assert.That(File.ReadAllBytes(current), Is.EqualTo(original)); // 손상 원본 보존
        } // 검증 끝
        [Test] // 다른 실행의 저장 변경 검증
        public void Save_RejectsConcurrentChangesWithoutOverwriting() // 다른 저장 작업의 변경 보호
        { // 검증 시작
            var firstStore = Store(); // 첫 번째 실행
            var secondStore = Store(); // 두 번째 실행
            var first = Load(firstStore, out _); // 첫 실행의 빈 상태
            var second = Load(secondStore, out _); // 두 번째 실행의 빈 상태
            first.TryRecord("first"); // 첫 실행의 관찰
            Assert.That(Save(firstStore, first, out _), Is.True); // 첫 실행의 저장
            var current = Path.Combine(root, "observation-v1.json"); // 실제 현재 파일
            var original = File.ReadAllBytes(current); // 첫 실행의 정상 데이터 보관
            second.TryRecord("second"); // 오래된 두 번째 실행의 관찰
            Assert.That(Save(secondStore, second, out var error), Is.False); // 다른 실행의 변경 덮어쓰기 차단
            Assert.That(error, Is.Not.Empty); // 충돌 안내 확인
            Assert.That(File.ReadAllBytes(current), Is.EqualTo(original)); // 정상 파일 보존 확인
        } // 검증 끝
        [Test] // 최근 백업 보관 검증
        public void Save_KeepsThreeRecentValidBackups() // 정상 백업 회전 확인
        { // 검증 시작
            var store = Store(); // 시험 저장 기능
            var journal = Load(store, out _); // 빈 관찰 기록 조회
            for (var index = 0; index < 5; index++) // 다섯 번의 정상 저장
            { // 반복 저장 시작
                journal.TryRecord("monster-" + index); // 새로운 대상 기록
                Assert.That(Save(store, journal, out _), Is.True); // 정상 기록 저장
            } // 반복 저장 끝
            Assert.That(Directory.GetFiles(root, "observation-backup-*.json").Length, Is.EqualTo(3)); // 최근 정상 백업 세 개 유지
            var loaded = Load(Store(), out _); // 현재 기록 다시 읽기
            Assert.That(loaded.Contains("monster-4"), Is.True); // 최신 관찰 정보 유지
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
