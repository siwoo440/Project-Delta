using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Encounter Session 사용
using ProjectDelta.Domain; // GridPosition 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class ExplorationEncounterSessionTests
    {
        [Test]
        public void NewSession_StartsIdleWithoutContext()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession(); // 새 Session 생성

            Assert.AreEqual(
                EncounterState.Idle,
                session.State); // Idle 시작 확인

            Assert.IsFalse(
                session.IsActive); // 비활성 시작 확인

            Assert.IsNull(
                session.Context); // Context 없음 확인
        }

        [Test]
        public void TryBegin_SameRoomAndPosition_MovesIdleToStartingAndCreatesContext()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession(); // 새 Session 생성

            bool started =
                session.TryBegin(
                    "ROOM_A",
                    new GridPosition(1, 0),
                    "ROOM_A",
                    new GridPosition(1, 0),
                    "MON_TEST"); // 같은 칸 Encounter 시작

            Assert.IsTrue(
                started); // 시작 성공 확인

            Assert.AreEqual(
                EncounterState.Starting,
                session.State); // Starting 전환 확인

            Assert.IsTrue(
                session.IsActive); // 활성 상태 확인

            Assert.IsNotNull(
                session.Context); // Context 생성 확인

            Assert.AreEqual(
                "ROOM_A",
                session.Context.RoomId); // 방 ID 확인

            Assert.AreEqual(
                "MON_TEST",
                session.Context.MonsterDefinitionId); // 몬스터 ID 확인

            Assert.AreEqual(
                new GridPosition(1, 0),
                session.Context.MonsterGridPosition); // 몬스터 위치 확인
        }

        [Test]
        public void TryBegin_SameRoomAndAdjacentPosition_StartsEncounter()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession(); // 새 Session 생성

            bool started =
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    new GridPosition(1, 1),
                    "MON_TEST"); // 대각선 1칸 Encounter 시작

            Assert.IsTrue(
                started); // 8방향 포착 성공 확인

            Assert.AreEqual(
                EncounterState.Starting,
                session.State); // Starting 전환 확인
        }

        [Test]
        public void TryBegin_DifferentRoomOrOutsideCaptureRange_StaysIdle()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession(); // 새 Session 생성

            Assert.IsFalse(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_B",
                    GridPosition.Zero,
                    "MON_TEST")); // 다른 방 Encounter 거부 확인

            Assert.IsFalse(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    new GridPosition(2, 0),
                    "MON_TEST")); // 2칸 거리 Encounter 거부 확인

            Assert.AreEqual(
                EncounterState.Idle,
                session.State); // Idle 유지 확인
        }

        [Test]
        public void TryActivate_OnlyAllowsStartingToActive()
        {
            ExplorationEncounterSession session =
                CreateStartingSession(); // Starting Session 준비

            Assert.IsTrue(
                session.TryActivate()); // Active 전환 확인

            Assert.AreEqual(
                EncounterState.Active,
                session.State); // Active 상태 확인

            Assert.IsFalse(
                session.TryActivate()); // 중복 Active 전환 거부 확인
        }

        [Test]
        public void TryBeginResolve_OnlyAllowsActiveToResolving()
        {
            ExplorationEncounterSession session =
                CreateStartingSession(); // Starting Session 준비

            Assert.IsFalse(
                session.TryBeginResolve()); // Starting에서 Resolving 직행 거부 확인

            Assert.IsTrue(
                session.TryActivate()); // Active 전환

            Assert.IsTrue(
                session.TryBeginResolve()); // Resolving 전환

            Assert.AreEqual(
                EncounterState.Resolving,
                session.State); // Resolving 상태 확인
        }

        [Test]
        public void TryFinish_OnlyAllowsResolvingToFinished()
        {
            ExplorationEncounterSession session =
                CreateActiveSession(); // Active Session 준비

            Assert.IsFalse(
                session.TryFinish()); // Active에서 Finished 직행 거부 확인

            Assert.IsTrue(
                session.TryBeginResolve()); // Resolving 전환

            Assert.IsTrue(
                session.TryFinish()); // Finished 전환

            Assert.AreEqual(
                EncounterState.Finished,
                session.State); // Finished 상태 확인
        }

        [Test]
        public void TryReset_OnlyAllowsFinishedToIdleAndClearsContext()
        {
            ExplorationEncounterSession session =
                CreateActiveSession(); // Active Session 준비

            Assert.IsFalse(
                session.TryReset()); // Active에서 Reset 거부 확인

            Assert.IsTrue(
                session.TryBeginResolve()); // Resolving 전환

            Assert.IsTrue(
                session.TryFinish()); // Finished 전환

            Assert.IsTrue(
                session.TryReset()); // Idle Reset

            Assert.AreEqual(
                EncounterState.Idle,
                session.State); // Idle 상태 확인

            Assert.IsNull(
                session.Context); // Context 제거 확인

            Assert.IsNull(
                session.MonsterDefinitionId); // 몬스터 ID 제거 확인

            Assert.IsFalse(
                session.IsActive); // 비활성 상태 확인
        }

        [Test]
        public void TryBegin_WhileLifecycleIsNotIdle_BlocksDuplicateEncounter()
        {
            ExplorationEncounterSession session =
                CreateStartingSession(); // Starting Session 준비

            Assert.IsFalse(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    GridPosition.Zero,
                    "MON_TEST")); // 중복 Encounter 시작 차단 확인

            Assert.AreEqual(
                EncounterState.Starting,
                session.State); // 기존 상태 유지 확인
        }

        [Test]
        public void ForceReset_ReturnsAnyStateToIdleForControllerShutdown()
        {
            ExplorationEncounterSession session =
                CreateActiveSession(); // Active Session 준비

            session.ForceReset(); // 강제 초기화

            Assert.AreEqual(
                EncounterState.Idle,
                session.State); // Idle 복귀 확인

            Assert.IsNull(
                session.Context); // Context 제거 확인

            Assert.IsFalse(
                session.IsActive); // 비활성 상태 확인
        }

        [Test]
        public void TryBegin_MissingRequiredId_StaysIdle()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession(); // 새 Session 생성

            Assert.IsFalse(
                session.TryBegin(
                    null,
                    GridPosition.Zero,
                    "ROOM_A",
                    GridPosition.Zero,
                    "MON_TEST")); // 플레이어 방 ID 누락 차단

            Assert.IsFalse(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    GridPosition.Zero,
                    null)); // 몬스터 ID 누락 차단

            Assert.AreEqual(
                EncounterState.Idle,
                session.State); // Idle 유지 확인
        }

        private static ExplorationEncounterSession CreateStartingSession()
        {
            ExplorationEncounterSession session =
                new ExplorationEncounterSession(); // 새 Session 생성

            Assert.IsTrue(
                session.TryBegin(
                    "ROOM_A",
                    GridPosition.Zero,
                    "ROOM_A",
                    GridPosition.Zero,
                    "MON_TEST")); // 테스트 Encounter 시작

            return session; // Starting Session 반환
        }

        private static ExplorationEncounterSession CreateActiveSession()
        {
            ExplorationEncounterSession session =
                CreateStartingSession(); // Starting Session 준비

            Assert.IsTrue(
                session.TryActivate()); // Active 전환

            return session; // Active Session 반환
        }
    }
}
