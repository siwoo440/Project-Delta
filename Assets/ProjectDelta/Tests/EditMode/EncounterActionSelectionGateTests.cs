using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Encounter 선택 Gate 사용
using ProjectDelta.Domain; // GridPosition 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class EncounterActionSelectionGateTests
    {
        [Test]
        public void Evaluate_ActiveWithContext_AllowsSelection()
        {
            EncounterActionSelectionGate gate =
                new EncounterActionSelectionGate(); // 새 Gate 생성

            EncounterActionAvailability availability =
                gate.Evaluate(
                    EncounterState.Active,
                    CreateContext()); // Active 상태 선택 가능 여부 계산

            Assert.IsTrue(
                availability.CanSelect); // 선택 가능 확인

            Assert.IsTrue(
                string.IsNullOrEmpty(availability.Reason)); // 불가 사유 없음 확인
        }

        [Test]
        public void Evaluate_NotActive_BlocksSelectionWithReason()
        {
            EncounterActionSelectionGate gate =
                new EncounterActionSelectionGate(); // 새 Gate 생성

            EncounterActionAvailability availability =
                gate.Evaluate(
                    EncounterState.Starting,
                    CreateContext()); // Starting 상태 선택 가능 여부 계산

            Assert.IsFalse(
                availability.CanSelect); // 선택 차단 확인

            Assert.IsFalse(
                string.IsNullOrEmpty(availability.Reason)); // 차단 사유 확인
        }

        [Test]
        public void Evaluate_MissingContext_BlocksSelectionWithReason()
        {
            EncounterActionSelectionGate gate =
                new EncounterActionSelectionGate(); // 새 Gate 생성

            EncounterActionAvailability availability =
                gate.Evaluate(
                    EncounterState.Active,
                    null); // Context 누락 상태 계산

            Assert.IsFalse(
                availability.CanSelect); // 선택 차단 확인

            Assert.IsFalse(
                string.IsNullOrEmpty(availability.Reason)); // 차단 사유 확인
        }

        [Test]
        public void TryCommit_FirstSelectionSucceedsAndSecondSelectionFails()
        {
            EncounterActionSelectionGate gate =
                new EncounterActionSelectionGate(); // 새 Gate 생성

            Assert.IsTrue(
                gate.TryCommit(
                    "Battle")); // 첫 전투 선택 확정 확인

            Assert.IsFalse(
                gate.TryCommit(
                    "Escape")); // 두 번째 회피 선택 차단 확인

            Assert.IsTrue(
                gate.HasSelection); // 선택 확정 상태 확인

            Assert.AreEqual(
                "Battle",
                gate.SelectedCommandId); // 최초 선택 유지 확인
        }

        [Test]
        public void Evaluate_AfterCommit_BlocksSelectionAsDuplicate()
        {
            EncounterActionSelectionGate gate =
                new EncounterActionSelectionGate(); // 새 Gate 생성

            Assert.IsTrue(
                gate.TryCommit(
                    "Battle")); // 첫 행동 확정

            EncounterActionAvailability availability =
                gate.Evaluate(
                    EncounterState.Active,
                    CreateContext()); // 확정 후 선택 가능 여부 계산

            Assert.IsFalse(
                availability.CanSelect); // 중복 선택 차단 확인

            Assert.AreEqual(
                "이미 행동을 선택했습니다.",
                availability.Reason); // 중복 선택 사유 확인
        }

        [Test]
        public void Reset_ClearsSelectionForNextEncounter()
        {
            EncounterActionSelectionGate gate =
                new EncounterActionSelectionGate(); // 새 Gate 생성

            Assert.IsTrue(
                gate.TryCommit(
                    "Battle")); // 첫 행동 확정

            gate.Reset(); // 다음 Encounter 준비 초기화

            Assert.IsFalse(
                gate.HasSelection); // 선택 상태 초기화 확인

            Assert.IsNull(
                gate.SelectedCommandId); // Command ID 초기화 확인

            Assert.IsTrue(
                gate.Evaluate(
                    EncounterState.Active,
                    CreateContext()).CanSelect); // 다음 Encounter 선택 가능 확인
        }

        private static EncounterContext CreateContext()
        {
            return new EncounterContext(
                "ROOM_A",
                "MON_TEST",
                GridPosition.Zero); // 테스트 Context 생성
        }
    }
}
