using System; // 시험 고유 식별자
using System.IO; // 독립 저장 파일 검증
using NUnit.Framework; // 저장 규칙 검증 도구
using ProjectDelta.Core; // 패배 원인 정의
using ProjectDelta.Runtime; // 독립 패배 기록 저장소
using UnityEngine; // 독립 시험 경로
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class DefeatHistoryStorageTests // 패배 기록 저장 안전성 검증
    { // 클래스 시작
        private string root; // 독립 시험 폴더
        [SetUp] // 독립 저장 경로 준비
        public void Prepare() // 개별 시험 경로 생성
        { // 준비 시작
            root = Path.Combine(Application.dataPath, "..", "Artifacts", "DefeatStorageTests", Guid.NewGuid().ToString("N")); // 다른 회차와 분리된 경로
        } // 준비 끝
        private DefeatHistoryStorage NewStore() // 허용된 시험 대상의 저장소 생성
        { // 생성 시작
            return new DefeatHistoryStorage(root, id => id == "MON_SLIME" || id == "MON_GOBLIN"); // 명시된 대상만 유효한 저장소
        } // 생성 끝
        [Test] // 실제 저장과 중복 방지 검증
        public void Append_ReloadsAndKeepsSameTransactionIdempotent() // 새로운 실행의 중복 기록 방지 확인
        { // 검증 시작
            var first = NewStore(); // 첫 번째 저장소
            Assert.That(first.Load(out var warning).Count, Is.EqualTo(0)); // 새 기록의 빈 상태
            Assert.That(warning, Is.Empty); // 정상 저장 경로 안내 없음
            Assert.That(first.TryAppend("run-001", "MON_SLIME", BattleDefeatReason.Surrender, out var duplicate, out _), Is.True); // 첫 패배 기록 저장
            Assert.That(duplicate, Is.False); // 신규 기록 처리 확인
            var path = Path.Combine(root, "defeat-v1.json"); // 패배 전용 현재 파일
            var original = File.ReadAllBytes(path); // 첫 저장의 실제 파일
            var next = NewStore(); // 새 실행 저장소
            var loaded = next.Load(out warning); // 검증된 패배 기록 읽기
            Assert.That(loaded.Count, Is.EqualTo(1)); // 실제 저장 기록 수 확인
            Assert.That(loaded[0].TargetId, Is.EqualTo("MON_SLIME")); // 저장한 상대 확인
            Assert.That(loaded[0].Reason, Is.EqualTo(BattleDefeatReason.Surrender)); // 저장한 원인 확인
            Assert.That(next.TryAppend("run-001", "MON_SLIME", BattleDefeatReason.Surrender, out duplicate, out _), Is.True); // 같은 종료의 재시도
            Assert.That(duplicate, Is.True); // 중복 기록 인식 확인
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original)); // 중복 요청의 파일 불변 확인
            Assert.That(next.TryAppend("run-001", "MON_GOBLIN", BattleDefeatReason.HealthZero, out _, out _), Is.False); // 같은 종료 식별자의 다른 결과 차단
        } // 검증 끝
        [Test] // 잘못된 원인과 상대 검증
        public void InvalidRecord_IsRejectedBeforeWriting() // 미등록 대상과 잘못된 원인 보호
        { // 검증 시작
            var store = NewStore(); // 독립 저장소
            Assert.That(store.TryAppend("run-001", "MON_SLIME", BattleDefeatReason.Surrender, out _, out _), Is.False); // 첫 읽기 없는 저장 차단
            store.Load(out _); // 최초 상태 읽기
            Assert.That(store.TryAppend("", "MON_SLIME", BattleDefeatReason.Surrender, out _, out _), Is.False); // 빈 종료 식별자 차단
            Assert.That(store.TryAppend("run-001", "UNKNOWN", BattleDefeatReason.Surrender, out _, out _), Is.False); // 등록되지 않은 상대 차단
            Assert.That(store.TryAppend("run-001", "MON_SLIME", (BattleDefeatReason)99, out _, out _), Is.False); // 알 수 없는 패배 원인 차단
            Assert.That(File.Exists(Path.Combine(root, "defeat-v1.json")), Is.False); // 잘못된 기록의 파일 생성 방지
        } // 검증 끝
        [Test] // 두 실행의 저장 충돌 검증
        public void ConcurrentChange_IsRejectedWithoutOverwriting() // 다른 실행이 쓴 기록 보존
        { // 검증 시작
            var first = NewStore(); // 첫 번째 실행
            var second = NewStore(); // 두 번째 실행
            first.Load(out _); // 첫 실행의 초기 상태 읽기
            second.Load(out _); // 두 번째 실행의 초기 상태 읽기
            Assert.That(first.TryAppend("run-001", "MON_SLIME", BattleDefeatReason.HealthZero, out _, out _), Is.True); // 첫 실행의 실제 저장
            Assert.That(second.TryAppend("run-002", "MON_GOBLIN", BattleDefeatReason.Surrender, out _, out _), Is.False); // 오래된 상태의 기록 차단
            var saved = NewStore().Load(out _); // 새 실행의 실제 파일 읽기
            Assert.That(saved.Count, Is.EqualTo(1)); // 첫 실행의 기록 보존 확인
            Assert.That(saved[0].TransactionId, Is.EqualTo("run-001")); // 기존 기록 식별자 확인
        } // 검증 끝
        [Test] // 손상된 원본 보호 검증
        public void CorruptCurrent_IsNotOverwritten() // 손상 파일과 저장 차단 확인
        { // 검증 시작
            var store = NewStore(); // 첫 번째 저장소
            store.Load(out _); // 최초 빈 기록 확인
            Assert.That(store.TryAppend("run-001", "MON_SLIME", BattleDefeatReason.HealthZero, out _, out _), Is.True); // 정상 기록 저장
            var path = Path.Combine(root, "defeat-v1.json"); // 패배 기록 파일
            File.WriteAllText(path, "damaged"); // 손상된 원본 모사
            var next = NewStore(); // 새 실행 저장소
            Assert.That(next.Load(out var warning).Count, Is.EqualTo(0)); // 손상 파일의 기록 거절
            Assert.That(warning, Is.Not.Empty); // 사용자 오류 안내 확인
            Assert.That(next.IsBlocked, Is.True); // 손상 원본의 저장 차단 확인
            Assert.That(next.TryAppend("run-002", "MON_GOBLIN", BattleDefeatReason.Surrender, out _, out _), Is.False); // 손상 원본 덮어쓰기 차단
            Assert.That(File.ReadAllText(path), Is.EqualTo("damaged")); // 손상 원본 보존 확인
        } // 검증 끝
        [Test] // 손상된 원본과 백업의 읽기 전용 복구 검증
        public void CorruptCurrent_OffersBackupButBlocksWrites() // 검증된 백업과 원본 보호 확인
        { // 검증 시작
            var store = NewStore(); // 독립 저장소
            store.Load(out _); // 최초 빈 저장 상태
            Assert.That(store.TryAppend("run-001", "MON_SLIME", BattleDefeatReason.HealthZero, out _, out _), Is.True); // 첫 번째 정상 기록
            Assert.That(store.TryAppend("run-002", "MON_GOBLIN", BattleDefeatReason.Surrender, out _, out _), Is.True); // 두 번째 기록과 백업 생성
            var path = Path.Combine(root, "defeat-v1.json"); // 현재 기록 파일
            File.WriteAllText(path, "damaged"); // 손상된 현재 파일 모사
            var recovery = NewStore(); // 새 실행 복구 저장소
            var loaded = recovery.Load(out var warning); // 검증된 이전 백업 읽기
            Assert.That(loaded.Count, Is.EqualTo(1)); // 이전 백업의 기록 수 확인
            Assert.That(loaded[0].TransactionId, Is.EqualTo("run-001")); // 이전 백업의 기록 확인
            Assert.That(warning, Is.Not.Empty); // 복구 상태 안내 확인
            Assert.That(recovery.IsBlocked, Is.True); // 원본 확인 전 쓰기 차단
            Assert.That(recovery.TryAppend("run-003", "MON_SLIME", BattleDefeatReason.Surrender, out _, out _), Is.False); // 복구 상태 쓰기 차단 확인
            Assert.That(File.ReadAllText(path), Is.EqualTo("damaged")); // 손상 원본 유지 확인
        } // 검증 끝
        [Test] // 저장 잠금과 재시도 검증
        public void LockedStore_DoesNotOverwriteAndCanRetry() // 독점 잠금의 충돌 처리 확인
        { // 검증 시작
            var store = NewStore(); // 독립 저장소
            store.Load(out _); // 최초 상태 조회
            Directory.CreateDirectory(root); // 잠금 파일 저장 폴더 준비
            using (var gate = new FileStream(Path.Combine(root, "defeat.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) // 다른 실행의 독점 잠금 모사
            { // 잠금 중 검증 시작
                Assert.That(store.TryAppend("run-001", "MON_SLIME", BattleDefeatReason.HealthZero, out _, out _), Is.False); // 잠금 중 쓰기 차단
            } // 잠금 중 검증 끝
            Assert.That(store.TryAppend("run-001", "MON_SLIME", BattleDefeatReason.HealthZero, out _, out _), Is.True); // 잠금 해제 후 안전한 재시도
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
