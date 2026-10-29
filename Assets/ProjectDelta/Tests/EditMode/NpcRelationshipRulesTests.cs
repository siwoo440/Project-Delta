using NUnit.Framework; // 독립 관계 검증 도구
using ProjectDelta.Core; // 호감도와 수령 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class NpcRelationshipRulesTests // NPC별 호감도와 중복 검증
    { // 클래스 시작
        [Test] // 기획 단계 경계 검증
        public void Stage_UsesEverySpecifiedBoundary() // 전체 호감도 단계 확인
        { // 검증 시작
            Assert.That(NpcRelationshipRules.Stage(0), Is.EqualTo(NpcRelationshipStage.Neutral)); // 기본 중립
            Assert.That(NpcRelationshipRules.Stage(33), Is.EqualTo(NpcRelationshipStage.Neutral)); // 중립 상한
            Assert.That(NpcRelationshipRules.Stage(34), Is.EqualTo(NpcRelationshipStage.Interest)); // 관심 하한
            Assert.That(NpcRelationshipRules.Stage(66), Is.EqualTo(NpcRelationshipStage.Interest)); // 관심 상한
            Assert.That(NpcRelationshipRules.Stage(67), Is.EqualTo(NpcRelationshipStage.Trust)); // 신뢰 하한
            Assert.That(NpcRelationshipRules.Stage(84), Is.EqualTo(NpcRelationshipStage.Trust)); // 신뢰 상한
            Assert.That(NpcRelationshipRules.Stage(85), Is.EqualTo(NpcRelationshipStage.Special)); // 특별 하한
            Assert.That(NpcRelationshipRules.Stage(99), Is.EqualTo(NpcRelationshipStage.Special)); // 특별 상한
            Assert.That(NpcRelationshipRules.Stage(100), Is.EqualTo(NpcRelationshipStage.EndingAvailable)); // 엔딩 가능 단계
        } // 검증 끝
        [Test] // 독립 NPC와 회차별 중복 검증
        public void Friendliness_IsOncePerNpcAndRun() // 우호 선택과 저장 복원
        { // 검증 시작
            var initial = NpcRelationshipState.Empty; // 신규 관계 상태
            Assert.That(initial.TryGrantFriendliness("run-1", "merchant", out var first), Is.True); // 첫 우호 선택
            Assert.That(first.GetAffection("merchant"), Is.EqualTo(5)); // 시험 우호 수치
            Assert.That(initial.GetAffection("merchant"), Is.Zero); // 최초 상태 불변성
            Assert.That(first.HasReceipt("run-1", "merchant"), Is.True); // 회차별 수령 기록
            Assert.That(first.TryGrantFriendliness("run-1", "merchant", out _), Is.False); // 같은 회차 재적용 차단
            Assert.That(first.TryGrantFriendliness("run-1", "other", out var other), Is.True); // 같은 회차의 다른 NPC
            Assert.That(other.GetAffection("other"), Is.EqualTo(5)); // 다른 NPC 독립 호감도
            Assert.That(other.TryGrantFriendliness("run-2", "merchant", out var next), Is.True); // 다음 회차의 같은 NPC
            Assert.That(next.GetAffection("merchant"), Is.EqualTo(10)); // 회차 간 누적 호감도
            Assert.That(NpcRelationshipState.TryRestore(next.Snapshot(), out var restored), Is.True); // 실제 수령 기록 복원
            Assert.That(restored.TryGrantFriendliness("run-1", "merchant", out _), Is.False); // 재시작 후 중복 차단
        } // 검증 끝
        [Test] // 호감도 상한과 심층 복사 검증
        public void LimitAndCopy_PreserveIndependentRecords() // 호감도 상한과 외부 변경 차단
        { // 검증 시작
            var saved = new NpcRelationshipSaveData { relationships = new[] { new NpcRelationshipRecord { npcId = "npc", affection = 98 } }, receipts = new NpcRelationshipReceipt[0] }; // 상한 근처 저장
            Assert.That(NpcRelationshipState.TryRestore(saved, out var state), Is.True); // 기존 관계 복원
            saved.relationships[0].affection = 0; // 원본 저장 데이터 변경
            Assert.That(state.GetAffection("npc"), Is.EqualTo(98)); // 복원 관계의 독립성
            Assert.That(state.TryGrantFriendliness("run", "npc", out var next), Is.True); // 상한 근처 우호 선택
            Assert.That(next.GetAffection("npc"), Is.EqualTo(100)); // 백 호감도 상한
            var snapshot = next.Snapshot(); // 독립 저장본
            snapshot.receipts[0].runId = "changed"; // 수령 저장본 변경
            snapshot.relationships[0].affection = 0; // 관계 저장본 변경
            Assert.That(next.HasReceipt("run", "npc"), Is.True); // 원본 수령 기록 보존
            Assert.That(next.GetAffection("npc"), Is.EqualTo(100)); // 원본 호감도 보존
        } // 검증 끝
        [Test] // 손상과 중복 저장 거절 검증
        public void Restore_RejectsInvalidIdsAndDuplicateRecords() // 관계와 수령 무결성 확인
        { // 검증 시작
            Assert.That(NpcRelationshipState.Empty.TryGrantFriendliness("", "npc", out _), Is.False); // 빈 회차 식별자 거절
            Assert.That(NpcRelationshipState.Empty.TryGrantFriendliness("run", "white space", out _), Is.False); // 공백 식별자 거절
            Assert.That(NpcRelationshipState.Empty.TryGrantFriendliness("run", new string('x', 129), out _), Is.False); // 과도한 식별자 거절
            var data = new NpcRelationshipSaveData { relationships = new[] { new NpcRelationshipRecord { npcId = "npc", affection = 101 } }, receipts = new NpcRelationshipReceipt[0] }; // 범위 밖 저장
            Assert.That(NpcRelationshipState.TryRestore(data, out _), Is.False); // 호감도 초과 차단
            data.relationships = new[] { new NpcRelationshipRecord { npcId = "npc", affection = 5 }, new NpcRelationshipRecord { npcId = "npc", affection = 5 } }; // 중복 관계
            Assert.That(NpcRelationshipState.TryRestore(data, out _), Is.False); // 중복 NPC 차단
            data.relationships = new[] { new NpcRelationshipRecord { npcId = "npc", affection = 5 } }; // 정상 관계 구성
            data.receipts = new[] { new NpcRelationshipReceipt { runId = "run", npcId = "npc" }, new NpcRelationshipReceipt { runId = "run", npcId = "npc" } }; // 중복 수령
            Assert.That(NpcRelationshipState.TryRestore(data, out _), Is.False); // 동일 회차 중복 차단
            data.receipts = new[] { new NpcRelationshipReceipt { runId = "run", npcId = "missing" } }; // 누락된 NPC 수령
            Assert.That(NpcRelationshipState.TryRestore(data, out _), Is.False); // 관계 없는 수령 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
