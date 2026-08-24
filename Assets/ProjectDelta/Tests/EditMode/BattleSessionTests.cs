using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Session 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleSessionTests
    {
        [Test]
        public void NewSession_StartsIdleWithoutContext()
        {
            BattleSession session =
                new BattleSession(); // 새 Session 생성

            Assert.AreEqual(
                BattleState.Idle,
                session.State); // Idle 시작 확인

            Assert.IsFalse(
                session.IsActive); // 비활성 시작 확인

            Assert.IsNull(
                session.Context); // Context 없음 확인

            Assert.AreEqual(
                0,
                session.TurnNumber); // 턴 번호 0 확인
        }

        [Test]
        public void TryBeginBattle_WithValidContext_MovesIdleToStarting()
        {
            BattleSession session =
                new BattleSession(); // 새 Session 생성

            Assert.IsTrue(
                session.TryBeginBattle(
                    CreateContext())); // Battle 시작

            Assert.AreEqual(
                BattleState.Starting,
                session.State); // Starting 전환 확인

            Assert.IsTrue(
                session.IsActive); // 활성 상태 확인

            Assert.IsNotNull(
                session.Context); // Context 저장 확인
        }

        [Test]
        public void TryBeginBattle_WithoutEnemies_StaysIdle()
        {
            BattleSession session =
                new BattleSession(); // 새 Session 생성

            BattleContext context =
                new BattleContext(
                    CreatePlayer(),
                    new BattleParticipant[0]); // 적이 없는 Context

            Assert.IsFalse(
                session.TryBeginBattle(
                    context)); // 시작 거부 확인

            Assert.AreEqual(
                BattleState.Idle,
                session.State); // Idle 유지 확인
        }

        [Test]
        public void TryBeginBattle_WhileAlreadyActive_BlocksDuplicateBattle()
        {
            BattleSession session =
                CreateStartingSession(); // Starting Session 준비

            Assert.IsFalse(
                session.TryBeginBattle(
                    CreateContext())); // 중복 시작 거부 확인

            Assert.AreEqual(
                BattleState.Starting,
                session.State); // 기존 상태 유지 확인
        }

        [Test]
        public void TryStartTurn_FromStarting_EntersTurnStartAndIncrementsTurn()
        {
            BattleSession session =
                CreateStartingSession(); // Starting Session 준비

            Assert.IsTrue(
                session.TryStartTurn()); // TurnStart 전환

            Assert.AreEqual(
                BattleState.TurnStart,
                session.State); // TurnStart 상태 확인

            Assert.AreEqual(
                1,
                session.TurnNumber); // 첫 턴 번호 확인
        }

        [Test]
        public void TryEnterAwaitingAction_OnlyFromTurnStartWithValidActor()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비

            Assert.IsFalse(
                session.TryEnterAwaitingAction(
                    null)); // 행동자 없음 거부 확인

            BattleParticipant actor =
                session.Context.Player;

            Assert.IsTrue(
                session.TryEnterAwaitingAction(
                    actor)); // AwaitingAction 전환

            Assert.AreEqual(
                BattleState.AwaitingAction,
                session.State); // AwaitingAction 상태 확인

            Assert.AreSame(
                actor,
                session.CurrentActor); // 행동 대상 저장 확인
        }

        [Test]
        public void TryEnterAwaitingAction_WithParticipantOutsideContext_Rejected()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비

            BattleParticipant outsider =
                new BattleParticipant(
                    "OUTSIDER",
                    "OUTSIDER",
                    BattleTeam.Enemy,
                    5,
                    5); // Context에 속하지 않은 참가자

            Assert.IsFalse(
                session.TryEnterAwaitingAction(
                    outsider)); // 소속 불일치 거부 확인

            Assert.AreEqual(
                BattleState.TurnStart,
                session.State); // 상태 유지 확인
        }

        [Test]
        public void TurnLifecycle_CanLoopThroughMultipleTurns()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비

            BattleParticipant actor =
                session.Context.Player;

            Assert.IsTrue(
                session.TryEnterAwaitingAction(
                    actor)); // AwaitingAction 전환

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction 전환

            Assert.AreEqual(
                BattleState.ResolvingAction,
                session.State); // ResolvingAction 상태 확인

            Assert.IsTrue(
                session.TryEndTurn()); // TurnEnd 전환

            Assert.AreEqual(
                BattleState.TurnEnd,
                session.State); // TurnEnd 상태 확인

            Assert.IsNull(
                session.CurrentActor); // 행동 대상 초기화 확인

            Assert.IsTrue(
                session.TryStartTurn()); // 다음 턴 시작

            Assert.AreEqual(
                BattleState.TurnStart,
                session.State); // TurnStart 상태 확인

            Assert.AreEqual(
                2,
                session.TurnNumber); // 두 번째 턴 확인
        }

        [Test]
        public void TryFinishBattle_FromAnyActiveState_SetsResultAndFinished()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비

            Assert.IsTrue(
                session.TryFinishBattle(
                    BattleOutcome.Victory)); // Finished 전환

            Assert.AreEqual(
                BattleState.Finished,
                session.State); // Finished 상태 확인

            Assert.IsNotNull(
                session.Result); // 결과 저장 확인

            Assert.AreEqual(
                BattleOutcome.Victory,
                session.Result.Outcome); // 결과 종류 확인

            Assert.AreEqual(
                1,
                session.Result.TurnCount); // 종료 시점 턴 번호 확인
        }

        [Test]
        public void TryFinishBattle_FromIdleOrFinished_Rejected()
        {
            BattleSession session =
                new BattleSession(); // 새 Session 생성

            Assert.IsFalse(
                session.TryFinishBattle(
                    BattleOutcome.Victory)); // Idle에서 거부 확인

            BattleSession finished =
                CreateTurnStartSession(); // TurnStart Session 준비

            Assert.IsTrue(
                finished.TryFinishBattle(
                    BattleOutcome.Defeat)); // Finished 전환

            Assert.IsFalse(
                finished.TryFinishBattle(
                    BattleOutcome.Victory)); // 중복 종료 거부 확인
        }

        [Test]
        public void TryReset_OnlyFromFinished_ClearsState()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비

            Assert.IsFalse(
                session.TryReset()); // TurnStart에서 Reset 거부 확인

            Assert.IsTrue(
                session.TryFinishBattle(
                    BattleOutcome.Victory)); // Finished 전환

            Assert.IsTrue(
                session.TryReset()); // Idle Reset

            Assert.AreEqual(
                BattleState.Idle,
                session.State); // Idle 상태 확인

            Assert.IsNull(
                session.Context); // Context 제거 확인

            Assert.IsNull(
                session.Result); // 결과 제거 확인

            Assert.AreEqual(
                0,
                session.TurnNumber); // 턴 번호 초기화 확인
        }

        [Test]
        public void ForceReset_ReturnsAnyStateToIdle()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비

            session.ForceReset(); // 강제 초기화

            Assert.AreEqual(
                BattleState.Idle,
                session.State); // Idle 복귀 확인

            Assert.IsNull(
                session.Context); // Context 제거 확인

            Assert.IsFalse(
                session.IsActive); // 비활성 상태 확인
        }

        private static BattleParticipant CreatePlayer()
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                20,
                5); // 테스트용 플레이어 참가자
        }

        private static BattleParticipant CreateEnemy()
        {
            return new BattleParticipant(
                "MON_TEST",
                "MON_TEST",
                BattleTeam.Enemy,
                10,
                5); // 테스트용 몬스터 참가자
        }

        private static BattleContext CreateContext()
        {
            return new BattleContext(
                CreatePlayer(),
                new[] { CreateEnemy() }); // 테스트용 Battle Context
        }

        private static BattleSession CreateStartingSession()
        {
            BattleSession session =
                new BattleSession(); // 새 Session 생성

            Assert.IsTrue(
                session.TryBeginBattle(
                    CreateContext())); // 테스트 Battle 시작

            return session; // Starting Session 반환
        }

        private static BattleSession CreateTurnStartSession()
        {
            BattleSession session =
                CreateStartingSession(); // Starting Session 준비

            Assert.IsTrue(
                session.TryStartTurn()); // TurnStart 전환

            return session; // TurnStart Session 반환
        }
    }
}
