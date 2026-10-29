using System; // 독립 실행 식별자
using System.IO; // 실제 파일과 충돌 검증
using NUnit.Framework; // 저장 규칙 검증
using ProjectDelta.Core; // 확인된 결말과 파편
using ProjectDelta.Runtime; // 결말 영구 기록 저장소
using UnityEngine; // 독립 시험 경로
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class EndingHistoryStorageTests // 결말 기록과 최초 해금 검증
    { // 클래스 시작
        private string root; // 독립 시험 폴더
        private string firstTransaction; // 첫 마왕 거래 식별자
        private string secondTransaction; // 다른 마왕 거래 식별자
        [SetUp] // 독립 경로 준비
        public void Prepare() // 실제 파일 검증 준비
        { // 준비 시작
            root = Path.Combine(Application.dataPath, "..", "Artifacts", "EndingStorageTests", Guid.NewGuid().ToString("N")); // 개별 검증 폴더
            firstTransaction = Guid.NewGuid().ToString("N"); // 첫 마왕 결과 식별자
            secondTransaction = Guid.NewGuid().ToString("N"); // 별도 마왕 결과 식별자
        } // 준비 끝
        [Test] // 최초 해금과 재실행 검증
        public void Record_FirstUnlockPaysOnceAndRestoresReceipts() // 저장된 거래와 파편 조회
        { // 검증 시작
            var store = new EndingHistoryStorage(root); // 첫 실행 저장소
            Assert.That(store.Load(out var warning).Count, Is.Zero); // 새 기록의 빈 목록
            Assert.That(warning, Is.Empty); // 정상 최초 조회 안내
            Assert.That(store.TryRecord("run-001", firstTransaction, EndingRules.ReturnBasic, out var first, out var shards, out var error), Is.True, error); // 첫 결말의 실제 저장
            Assert.That(first, Is.True); // 최초 해금 확인
            Assert.That(shards, Is.EqualTo(5)); // 최초 오 파편 지급
            Assert.That(store.TotalShards, Is.EqualTo(5)); // 실제 누적 파편 확인
            Assert.That(store.IsUnlocked(EndingRules.ReturnBasic), Is.True); // 결말 해금 조회
            Assert.That(store.TryGetRun("run-001", out var receipt), Is.True); // 회차 영수증 조회
            Assert.That(receipt.TransactionId, Is.EqualTo(firstTransaction)); // 원래 마왕 거래 식별자
            Assert.That(receipt.EndingId, Is.EqualTo(EndingRules.ReturnBasic)); // 확정 결말 식별자
            Assert.That(receipt.GrantedShards, Is.EqualTo(5)); // 복구용 최초 지급 수량
            var path = Path.Combine(root, "ending-history-v1.json"); // 실제 결말 기록 파일
            var original = File.ReadAllBytes(path); // 원래 기록 바이트
            var next = new EndingHistoryStorage(root); // 실제 새 실행 저장소
            Assert.That(next.Load(out warning).Count, Is.EqualTo(1)); // 재실행 후 영수증 복원
            Assert.That(warning, Is.Empty); // 정상 재실행 안내
            Assert.That(next.TryRecord("run-001", firstTransaction, EndingRules.ReturnBasic, out first, out shards, out error), Is.True, error); // 같은 결과 저장 재시도
            Assert.That(first, Is.False); // 같은 거래의 재해금 차단
            Assert.That(shards, Is.Zero); // 같은 거래의 재지급 차단
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original)); // 중복 요청의 파일 불변
            Assert.That(next.TryRecord("run-002", secondTransaction, EndingRules.ReturnBasic, out first, out shards, out error), Is.True, error); // 다른 회차의 같은 결말
            Assert.That(first, Is.False); // 같은 결말의 재해금 차단
            Assert.That(shards, Is.Zero); // 다른 회차의 재지급 차단
            Assert.That(next.TotalShards, Is.EqualTo(5)); // 실제 누적 파편 유지
            Assert.That(next.TryRecord("run-003", Guid.NewGuid().ToString("N"), EndingRules.StayBasic, out first, out shards, out error), Is.True, error); // 새 결말의 최초 기록
            Assert.That(first, Is.True); // 다른 결말 최초 해금
            Assert.That(shards, Is.EqualTo(5)); // 다른 결말의 최초 보상
            Assert.That(next.TotalShards, Is.EqualTo(10)); // 서로 다른 결말의 누적 합계
        } // 검증 끝
        [Test] // 거래 식별과 결말 충돌 검증
        public void Conflict_SameRunOrTransactionCannotChangeOutcome() // 회차와 거래의 단일 결과
        { // 검증 시작
            var store = new EndingHistoryStorage(root); // 독립 저장소
            store.Load(out _); // 최초 상태 조회
            Assert.That(store.TryRecord("run-001", firstTransaction, EndingRules.ReturnBasic, out _, out _, out _), Is.True); // 첫 결과 기록
            var path = Path.Combine(root, "ending-history-v1.json"); // 현재 결말 파일
            var original = File.ReadAllBytes(path); // 충돌 전 파일 바이트
            Assert.That(store.TryRecord("run-001", secondTransaction, EndingRules.StayBasic, out _, out _, out _), Is.False); // 같은 회차의 다른 거래 차단
            Assert.That(store.TryRecord("run-002", firstTransaction, EndingRules.StayBasic, out _, out _, out _), Is.False); // 같은 거래의 다른 회차 차단
            Assert.That(store.TryRecord("run-001", firstTransaction, EndingRules.StayBasic, out _, out _, out _), Is.False); // 같은 거래의 다른 결말 차단
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original)); // 모든 충돌의 원본 보존
        } // 검증 끝
        [Test] // 잘못된 결말과 읽기 상태 검증
        public void InvalidRecord_IsRejectedBeforeWriting() // 저장 전 읽기와 식별자 검사
        { // 검증 시작
            var store = new EndingHistoryStorage(root); // 독립 저장소
            Assert.That(store.TryRecord("run-001", firstTransaction, EndingRules.ReturnBasic, out _, out _, out _), Is.False); // 최초 읽기 없는 기록 차단
            store.Load(out _); // 최초 상태 조회
            Assert.That(store.TryRecord("", firstTransaction, EndingRules.ReturnBasic, out _, out _, out _), Is.False); // 빈 회차 식별 차단
            Assert.That(store.TryRecord("run-001", "invalid", EndingRules.ReturnBasic, out _, out _, out _), Is.False); // 손상된 거래 식별 차단
            Assert.That(store.TryRecord("run-001", firstTransaction, "unknown", out _, out _, out _), Is.False); // 미등록 결말 차단
            Assert.That(File.Exists(Path.Combine(root, "ending-history-v1.json")), Is.False); // 잘못된 요청의 파일 생성 차단
        } // 검증 끝
        [Test] // 다른 실행과 손상 파일 검증
        public void ConcurrentChange_DoesNotOverwriteOtherRun() // 오래된 실행의 기록 차단
        { // 검증 시작
            var first = new EndingHistoryStorage(root); // 첫 실행 저장소
            var second = new EndingHistoryStorage(root); // 두 번째 실행 저장소
            first.Load(out _); // 첫 실행의 빈 상태
            second.Load(out _); // 두 번째 실행의 빈 상태
            Assert.That(first.TryRecord("run-001", firstTransaction, EndingRules.ReturnBasic, out _, out _, out _), Is.True); // 첫 실행의 실제 저장
            Assert.That(second.TryRecord("run-002", secondTransaction, EndingRules.StayBasic, out _, out _, out _), Is.False); // 오래된 실행의 저장 차단
            Assert.That(second.IsBlocked, Is.True); // 동시 변경 보호 상태
            var reread = new EndingHistoryStorage(root); // 실제 저장 재조회
            Assert.That(reread.Load(out _).Count, Is.EqualTo(1)); // 기존 기록 유지
            Assert.That(reread.TotalShards, Is.EqualTo(5)); // 기존 해금 파편 유지
        } // 검증 끝
        [Test] // 손상된 원본과 백업 복원 검증
        public void Corruption_OffersBackupButPreservesOriginalAndBlocksWrites() // 읽기 전용 복구와 파일 보호
        { // 검증 시작
            var store = new EndingHistoryStorage(root); // 독립 저장소
            store.Load(out _); // 최초 상태 읽기
            Assert.That(store.TryRecord("run-001", firstTransaction, EndingRules.ReturnBasic, out _, out _, out _), Is.True); // 첫 정상 기록
            Assert.That(store.TryRecord("run-002", secondTransaction, EndingRules.StayBasic, out _, out _, out _), Is.True); // 두 번째 기록과 백업
            var path = Path.Combine(root, "ending-history-v1.json"); // 현재 결말 파일
            File.WriteAllText(path, "damaged"); // 손상 원본 모사
            var recovery = new EndingHistoryStorage(root); // 새 실행의 저장소
            Assert.That(recovery.Load(out var warning).Count, Is.EqualTo(1)); // 검증된 이전 백업 읽기
            Assert.That(warning, Is.Not.Empty); // 실제 복구 안내
            Assert.That(recovery.IsBlocked, Is.True); // 원본 확인 전 저장 차단
            Assert.That(recovery.TryRecord("run-003", Guid.NewGuid().ToString("N"), EndingRules.DemonServant, out _, out _, out _), Is.False); // 손상 원본 덮어쓰기 차단
            Assert.That(File.ReadAllText(path), Is.EqualTo("damaged")); // 손상 원본 보존
        } // 검증 끝
        [Test] // 저장 잠금 실패와 재시도 검증
        public void LockFailure_PreservesStateAndAllowsRetry() // 독점 잠금의 안전한 거절
        { // 검증 시작
            var store = new EndingHistoryStorage(root); // 독립 저장소
            store.Load(out _); // 최초 빈 상태 조회
            Directory.CreateDirectory(root); // 잠금 파일 폴더 생성
            using (var gate = new FileStream(Path.Combine(root, "ending-history.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)) // 다른 저장 작업의 독점 잠금
            { // 잠금 상태 시작
                Assert.That(store.TryRecord("run-001", firstTransaction, EndingRules.ReturnBasic, out _, out _, out _), Is.False); // 잠금 중 실제 쓰기 차단
                Assert.That(store.TotalShards, Is.Zero); // 저장 실패 시 파편 미지급
            } // 잠금 상태 끝
            Assert.That(store.TryRecord("run-001", firstTransaction, EndingRules.ReturnBasic, out var first, out var shards, out _), Is.True); // 잠금 해제 후 재시도
            Assert.That(first, Is.True); // 실제 최초 해금
            Assert.That(shards, Is.EqualTo(5)); // 실제 최초 파편 지급
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
