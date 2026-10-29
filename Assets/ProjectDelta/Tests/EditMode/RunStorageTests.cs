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
        [Test] // 이전 보상 후보의 아이템 종류 복원 검증
        public void Load_LegacyRewardCandidateKeepsSmallPotion() // 종류가 없던 이전 보상 호환
        { // 검증 시작
            var data = Snapshot(42); // 정상 이전 회차
            data.version = 9; // 소비품 종류가 없던 마지막 저장 버전
            data.generationVersion = 1; // 기존 던전 생성 규칙
            data.secretPassages = new SavedSecretPassage[0]; // 기존 구조의 빈 비밀 기록
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(DungeonGenerator.Generate(data.seed)); // 기존 던전 구조 지문
            data.mana = 50; // 현재 마나
            data.maxMana = 50; // 실제 최대 마나
            data.runId = Guid.NewGuid().ToString("N"); // 회차 식별자
            data.level = 1; // 기본 레벨
            data.statPoints = 5; // 기본 스탯 포인트
            data.hasPendingReward = true; // 미완료 보상 후보
            data.pendingReward = new SavedBattleReward { transactionId = Guid.NewGuid().ToString("N"), normalEnemies = 1, experience = 60, potionCount = 1 }; // 종류 없는 기존 소형 회복약 후보
            var store = new RunStorage(root); // 독립 회차 저장소
            store.Load(out _); // 최초 저장 상태 조회
            Assert.That(store.TrySave(data, out var error), Is.True, error); // 이전 버전의 유효한 후보 저장
            var restored = new RunStorage(root).Load(out var warning); // 새 실행의 실제 후보 읽기
            Assert.That(warning, Is.Empty); // 정상 저장 확인
            Assert.That(restored.pendingReward.itemId, Is.EqualTo("trial-small-potion")); // 기존 소형 회복약 후보 복원
        } // 검증 끝
        [Test] // 종료 회차의 백업 재등장 방지 검증
        public void EndedRun_IsNotRecoveredAndNewRunCanBeSaved() // 회차 종료와 새 저장 확인
        { // 검증 시작
            var store = new RunStorage(root); // 독립 회차 저장소
            store.Load(out _); // 최초 저장 상태 읽기
            Assert.That(store.TrySave(Snapshot(42), out _), Is.True); // 첫 회차 저장
            Assert.That(store.TrySave(Snapshot(50), out _), Is.True); // 기존 회차 백업 생성
            Assert.That(store.TryMarkEnded(store.LegacyRunId, out var error), Is.True, error); // 검증된 회차 종료 처리
            var fresh = new RunStorage(root); // 새 실행의 저장소
            Assert.That(fresh.Load(out var warning), Is.Null); // 종료된 회차 이어하기 차단
            Assert.That(warning, Is.Empty); // 정상 종료의 손상 경고 없음
            Assert.That(fresh.TrySave(Snapshot(60), out error), Is.True, error); // 새 회차 저장 가능
            File.WriteAllText(Path.Combine(root, "run-v1.json"), "corrupt"); // 새 회차 현재 저장 손상
            var recovery = new RunStorage(root); // 새 실행의 복구 저장소
            Assert.That(recovery.Load(out _), Is.Null); // 종료된 이전 회차의 백업 부활 차단
        } // 검증 끝
        [Test] // 다른 실행의 저장 변경과 회차 종료 충돌 검증
        public void EndedRun_RejectsConcurrentChangeWithoutReplacingIt() // 다른 실행의 저장 보존 확인
        { // 검증 시작
            var first = new RunStorage(root); // 첫 저장소
            first.Load(out _); // 최초 상태 읽기
            Assert.That(first.TrySave(Snapshot(42), out _), Is.True); // 첫 회차 저장
            var other = new RunStorage(root); // 다른 실행 저장소
            other.Load(out _); // 다른 실행의 현재 저장 읽기
            Assert.That(other.TrySave(Snapshot(50), out _), Is.True); // 다른 실행의 회차 변경
            Assert.That(first.TryMarkEnded("test-run", out _), Is.False); // 이전 실행의 종료 충돌 차단
            Assert.That(new RunStorage(root).Load(out _).health, Is.EqualTo(50)); // 다른 실행의 저장 보존
        } // 검증 끝
        [Test] // 다른 회차 저장 보존 확인
        public void EndedRun_PreservesDifferentSavedRun() // 저장되지 않은 다른 회차의 종료 검사
        { // 검증 시작
            var store = new RunStorage(root); // 독립 회차 저장소
            store.Load(out _); // 최초 저장 상태 조회
            Assert.That(store.TrySave(Snapshot(42), out var error), Is.True, error); // 기존 회차 저장
            var previousId = store.LegacyRunId; // 기존 회차 식별자
            Assert.That(store.TryMarkEnded(System.Guid.NewGuid().ToString("N"), out error), Is.True, error); // 다른 회차 종료 처리
            Assert.That(new RunStorage(root).Load(out _).health, Is.EqualTo(42)); // 기존 회차의 재개 상태 보존
            Assert.That(previousId, Is.Not.Null); // 기존 회차 식별자 확인
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
