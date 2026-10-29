using System; // 고유 시험 경로
using System.IO; // 저장 파일 검사
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 회차 데이터
using ProjectDelta.Runtime; // 회차 저장 기능
using UnityEngine; // 독립 시험 경로
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class RunStorageTests // 회차 저장 검증
    { // 클래스 시작
        private string root; // 독립 저장 경로
        [SetUp] // 각 검증의 독립 경로
        public void Prepare() // 시험 경로 준비
        { // 준비 시작
            root = Path.Combine(Application.dataPath, "..", "Artifacts", "RunStorageTests", Guid.NewGuid().ToString("N")); // 고유 시험 폴더
        } // 준비 끝
        private static RunSaveData Snapshot(int hp) // 유효한 시험 회차 데이터
        { // 생성 시작
            var layout = DungeonGenerator.Generate(123); // 시험 던전 구조
            return new RunSaveData { version = 1, seed = 123, floor = 1, health = hp, maxHealth = 100, slots = new InventoryState().SaveSlots(), visitedRooms = new[] { new SavedRoom() }, doorLock = new DoorLockState(layout).Save(), history = new string[0] }; // 복원 가능한 시험 회차
        } // 생성 끝
        [Test] // 저장과 재읽기 검증
        public void Save_ReloadsRunFromIndependentInstance() // 새 저장 객체의 회차 읽기
        { // 검증 시작
            var store = new RunStorage(root); // 독립 저장 기능
            Assert.That(store.Load(out _), Is.Null); // 최초 저장 없음
            Assert.That(store.TrySave(Snapshot(42), out _), Is.True); // 회차 저장 성공
            var loaded = new RunStorage(root).Load(out var warning); // 새 실행의 저장 조회
            Assert.That(warning, Is.Empty); // 정상 저장 안내 없음
            Assert.That(loaded.health, Is.EqualTo(42)); // 체력 복원 확인
        } // 검증 끝
        [Test] // 손상된 원본 보존 검증
        public void CorruptSave_IsNotOverwritten() // 손상 저장 덮어쓰기 거절
        { // 검증 시작
            var store = new RunStorage(root); // 독립 저장 기능
            store.Load(out _); // 최초 저장 조회
            Assert.That(store.TrySave(Snapshot(42), out _), Is.True); // 정상 회차 저장
            var path = Path.Combine(root, "run-v1.json"); // 현재 회차 저장 경로
            File.WriteAllText(path, "corrupt"); // 손상 데이터 모사
            var fresh = new RunStorage(root); // 새 저장 기능
            Assert.That(fresh.Load(out var warning), Is.Null); // 복구 후보 없는 손상 파일 거절
            Assert.That(warning, Is.Not.Empty); // 오류 안내 확인
            Assert.That(fresh.TrySave(Snapshot(50), out _), Is.False); // 손상 원본 덮어쓰기 차단
            Assert.That(File.ReadAllText(path), Is.EqualTo("corrupt")); // 원본 유지 확인
        } // 검증 끝
        [Test] // 손상 파일의 이전 백업 복구 검증
        public void CorruptCurrent_RecoversBackupWithoutOverwriting() // 백업 읽기와 원본 보존
        { // 검증 시작
            var store = new RunStorage(root); // 독립 저장 기능
            store.Load(out _); // 최초 상태 읽기
            Assert.That(store.TrySave(Snapshot(42), out _), Is.True); // 첫 저장
            Assert.That(store.TrySave(Snapshot(50), out _), Is.True); // 첫 저장의 백업 생성
            var path = Path.Combine(root, "run-v1.json"); // 현재 저장 파일
            File.WriteAllText(path, "corrupt"); // 손상 원본 모사
            var recovery = new RunStorage(root); // 복구용 저장 기능
            var loaded = recovery.Load(out var warning); // 검증된 백업 읽기
            Assert.That(loaded, Is.Not.Null); // 복구 가능한 회차 확인
            Assert.That(loaded.health, Is.EqualTo(42)); // 이전 백업의 체력 확인
            Assert.That(warning, Is.Not.Empty); // 복구 상태 안내 확인
            Assert.That(recovery.TrySave(Snapshot(60), out _), Is.False); // 복구 중 자동 덮어쓰기 차단
            Assert.That(File.ReadAllText(path), Is.EqualTo("corrupt")); // 손상 원본 보존
        } // 검증 끝
        [Test] // 사라진 현재 파일의 임시 기록 복구
        public void MissingCurrent_RecoversPendingFileAndBlocksWrites() // 중단된 저장 읽기
        { // 검증 시작
            var store = new RunStorage(root); // 독립 저장 기능
            store.Load(out _); // 최초 상태 읽기
            Assert.That(store.TrySave(Snapshot(42), out _), Is.True); // 정상 회차 저장
            var path = Path.Combine(root, "run-v1.json"); // 현재 저장 파일
            var pending = Path.Combine(root, "run-0000000000000000001-test.tmp"); // 중단된 저장 후보
            File.Copy(path, pending); // 임시 후보 모사
            File.Delete(path); // 누락된 현재 파일 모사
            var recovery = new RunStorage(root); // 복구용 저장 기능
            Assert.That(recovery.Load(out var warning).health, Is.EqualTo(42)); // 임시 기록에서 복원
            Assert.That(warning, Is.Not.Empty); // 복구 상태 안내 확인
            Assert.That(recovery.TrySave(Snapshot(50), out _), Is.False); // 복구 상태 저장 차단
            Assert.That(File.Exists(pending), Is.True); // 임시 파일 보존
            Assert.That(File.Exists(path), Is.False); // 현재 파일 자동 생성 차단
        } // 검증 끝
        [Test] // 손상된 최신 후보와 유효한 이전 후보 검증
        public void Recovery_SkipsInvalidNewestCandidate() // 검증 가능한 이전 후보 선택
        { // 검증 시작
            var store = new RunStorage(root); // 독립 저장 기능
            store.Load(out _); // 최초 상태 읽기
            Assert.That(store.TrySave(Snapshot(42), out _), Is.True); // 첫 정상 저장
            Assert.That(store.TrySave(Snapshot(50), out _), Is.True); // 첫 백업 생성
            var current = Path.Combine(root, "run-v1.json"); // 현재 저장 파일
            File.WriteAllText(current, "corrupt"); // 현재 파일 손상 모사
            var invalid = Path.Combine(root, "run-8999999999999999999-invalid.tmp"); // 더 최신으로 정렬되는 손상 후보
            File.WriteAllText(invalid, "invalid"); // 손상 임시 파일 생성
            var recovered = new RunStorage(root).Load(out var warning); // 모든 후보 검사
            Assert.That(recovered, Is.Not.Null); // 유효한 이전 백업 확인
            Assert.That(recovered.health, Is.EqualTo(42)); // 정상 백업 체력 확인
            Assert.That(warning, Is.Not.Empty); // 복구 안내 확인
            Assert.That(File.ReadAllText(invalid), Is.EqualTo("invalid")); // 손상 후보 보존
        } // 검증 끝
        [Test] // 이전 백업 형식의 저장 시각 순서 검증
        public void Recovery_UsesNewestLegacyBackupByTransactionTime() // 이전 백업의 최신 순서 확인
        { // 검증 시작
            var store = new RunStorage(root); // 독립 저장 기능
            store.Load(out _); // 최초 상태 조회
            Assert.That(store.TrySave(Snapshot(42), out _), Is.True); // 첫 회차 저장
            var path = Path.Combine(root, "run-v1.json"); // 현재 회차 파일
            var old = Path.Combine(root, "run-backup-638000000000000000-old.json"); // 이전 형식의 오래된 백업
            File.Copy(path, old); // 오래된 백업 모사
            Assert.That(store.TrySave(Snapshot(50), out _), Is.True); // 두 번째 회차 저장
            var recent = Path.Combine(root, "run-backup-638000000000000001-new.json"); // 이전 형식의 최신 백업
            File.Copy(path, recent); // 최신 백업 모사
            File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddMinutes(1)); // 오래된 백업의 더 늦은 파일 시각
            File.SetLastWriteTimeUtc(recent, DateTime.UtcNow.AddMinutes(-1)); // 최신 백업의 더 이른 파일 시각
            foreach (var candidate in Directory.GetFiles(root, "run-backup-*.json")) // 자동 생성 백업 순회
            { // 불필요한 시험 후보 검사
                if (candidate != old && candidate != recent) // 직접 구성한 후보 외 검사
                { // 자동 백업 조건 시작
                    File.Delete(candidate); // 독립 시험 경로의 자동 백업 제거
                } // 자동 백업 조건 끝
            } // 후보 검사 끝
            File.WriteAllText(path, "corrupt"); // 현재 파일 손상 모사
            Assert.That(new RunStorage(root).Load(out _).health, Is.EqualTo(50)); // 파일 수정 시각이 아닌 저장 시각으로 선택
        } // 검증 끝
        [Test] // 동시 저장 충돌 검증
        public void ConcurrentChange_IsNotOverwritten() // 다른 실행의 변경 보존
        { // 검증 시작
            var first = new RunStorage(root); // 첫 실행 저장 기능
            var second = new RunStorage(root); // 두 번째 실행 저장 기능
            first.Load(out _); // 최초 상태 조회
            second.Load(out _); // 같은 최초 상태 조회
            Assert.That(first.TrySave(Snapshot(42), out _), Is.True); // 첫 실행의 저장
            Assert.That(second.TrySave(Snapshot(50), out _), Is.False); // 오래된 상태의 저장 거절
            Assert.That(new RunStorage(root).Load(out _).health, Is.EqualTo(42)); // 첫 실행의 기록 보존
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
