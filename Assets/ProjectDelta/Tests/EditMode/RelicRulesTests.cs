using NUnit.Framework; // 유물 규칙 검증 도구
using ProjectDelta.Core; // 순수 유물 상태
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class RelicRulesTests // 유물 보유와 효과 검증
    { // 검증 클래스 시작
        [Test] // 기본 한도와 중복 검증
        public void Acquire_RejectsDuplicateAndFullCapacity() // 다섯 개 유물과 중복 제한 확인
        { // 검증 시작
            var state = RelicState.Empty; // 새 회차의 빈 유물
            foreach (var id in TrialRelicCatalog.Ids()) // 등록된 시험 유물 순회
            { // 획득 시도 시작
                if (state.Count == 5) // 기본 한도 도달 검사
                { // 획득 중단 시작
                    break; // 다음 유물 획득 보류
                } // 획득 중단 끝
                Assert.That(state.TryAcquire(id, out var next), Is.True); // 실제 유물 획득
                state = next; // 획득된 독립 상태 적용
            } // 획득 시도 끝
            Assert.That(state.Count, Is.EqualTo(5)); // 기본 유물 한도 확인
            Assert.That(state.TryAcquire(TrialRelicCatalog.AttackId, out _), Is.False); // 동일 유물 중복 차단
            Assert.That(state.TryAcquire(TrialRelicCatalog.SpeedId, out _), Is.False); // 한도 초과 획득 차단
            Assert.That(RelicState.Empty.Count, Is.Zero); // 새 회차 초기화 확인
        } // 검증 끝
        [Test] // 한도 교체와 기존 상태 보존 검증
        public void Replace_RequiresDistinctKnownRelicAndPreservesOriginal() // 명시 교체와 포기 확인
        { // 검증 시작
            var ids = TrialRelicCatalog.Ids(); // 시험 유물 목록
            Assert.That(RelicState.TryRestore(new[] { ids[0], ids[1], ids[2], ids[3], ids[4] }, out var state), Is.True); // 가득 찬 유물 복원
            Assert.That(state.TryReplace(ids[0], ids[5], out var replaced), Is.True); // 명시한 기존 유물 교체
            Assert.That(replaced.Contains(ids[5]), Is.True); // 새 유물 획득 확인
            Assert.That(state.Contains(ids[0]), Is.True); // 기존 독립 상태 보존
            Assert.That(state.TryReplace(ids[0], ids[1], out _), Is.False); // 보유 유물 중복 교체 차단
            Assert.That(state.TryReplace("missing", ids[5], out _), Is.False); // 미보유 대상 교체 차단
            Assert.That(state.TryReplace(ids[0], "unknown", out _), Is.False); // 미등록 유물 교체 차단
            Assert.That(state.Count, Is.EqualTo(5)); // 실패와 포기 시 보유 보존
        } // 검증 끝
        [Test] // 저장 배열과 효과 독립성 검증
        public void Restore_CopiesIdsAndRejectsCorruption() // 저장 복원과 효과 확인
        { // 검증 시작
            var saved = new[] { TrialRelicCatalog.AttackId }; // 시험 공격 유물 저장본
            Assert.That(RelicState.TryRestore(saved, out var state), Is.True); // 정상 유물 복원
            saved[0] = "unknown"; // 외부 저장 배열 변경
            Assert.That(state.Contains(TrialRelicCatalog.AttackId), Is.True); // 복원 상태 독립성
            var copy = state.SaveIds(); // 독립 저장 배열 생성
            copy[0] = "unknown"; // 반환된 저장 배열 변경
            Assert.That(state.Contains(TrialRelicCatalog.AttackId), Is.True); // 내부 유물 독립성
            Assert.That(state.AttackBonus, Is.EqualTo(TrialRelicCatalog.Find(TrialRelicCatalog.AttackId).AttackBonus)); // 획득 즉시 시험 효과 확인
            Assert.That(RelicState.TryRestore(new[] { TrialRelicCatalog.AttackId, TrialRelicCatalog.AttackId }, out _), Is.False); // 중복 저장 거절
            Assert.That(RelicState.TryRestore(new[] { "unknown" }, out _), Is.False); // 미등록 유물 거절
            Assert.That(RelicState.TryRestore(null, out _), Is.False); // 누락 저장 거절
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
