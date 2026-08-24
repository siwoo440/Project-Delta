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

            Assert.IsFalse(
                session.HasPendingActorsThisTurn); // 남은 행동자 없음 확인
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
        public void TryStartTurn_FromStarting_EntersTurnStartAndBuildsOrderQueue()
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

            Assert.IsTrue(
                session.HasPendingActorsThisTurn); // 행동 순서 큐 생성 확인 (Player + Enemy)

            Assert.AreEqual(
                2,
                session.PendingActorsThisTurn.Count); // Player 1명 + Enemy 1명 확인
        }

        [Test]
        public void TryEnterAwaitingAction_OnlyFromTurnStartOrResolvingAction_PopsNextActor()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // AwaitingAction 전환 (첫 행동자)

            Assert.AreEqual(
                BattleState.AwaitingAction,
                session.State); // AwaitingAction 상태 확인

            Assert.IsNotNull(
                session.CurrentActor); // 행동 대상 선출 확인

            // Player와 Enemy가 같은 Speed이므로 동률 우선순위에 따라 Player가 먼저 나온다.
            Assert.AreEqual(
                "PLAYER",
                session.CurrentActor.InstanceId); // Player 우선 확인

            Assert.AreEqual(
                1,
                session.PendingActorsThisTurn.Count); // 남은 행동자 1명 확인
        }

        [Test]
        public void TryEnterAwaitingAction_FromAwaitingAction_Rejected()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 첫 행동자 진행

            Assert.IsFalse(
                session.TryEnterAwaitingAction()); // AwaitingAction 상태에서 중복 호출 거부 확인
        }

        [Test]
        public void TryEnterAwaitingAction_WithEmptyQueue_Rejected()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비 (Player + Enemy 1명씩)

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째 행동자 (Player)

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction 전환

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 2번째 행동자 (Enemy)

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction 전환

            Assert.IsFalse(
                session.HasPendingActorsThisTurn); // 이번 턴 행동자 모두 소진 확인

            Assert.IsFalse(
                session.TryEnterAwaitingAction()); // 남은 행동자 없을 때 거부 확인
        }

        [Test]
        public void TryEndTurn_WithPendingActors_Rejected()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비 (Player + Enemy)

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째 행동자만 진행

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction 전환

            Assert.IsTrue(
                session.HasPendingActorsThisTurn); // 아직 Enemy가 남음

            Assert.IsFalse(
                session.TryEndTurn()); // 남은 행동자가 있으면 TurnEnd 거부 확인
        }

        [Test]
        public void TurnLifecycle_AllActorsActBeforeTurnEndsThenNextTurnRebuildsQueue()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비 (Player + Enemy)

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째 행동자

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction

            Assert.IsFalse(
                session.TryEndTurn()); // 아직 2번째 행동자가 남아 거부

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 2번째 행동자로 직접 전환 (ResolvingAction → AwaitingAction)

            Assert.AreEqual(
                BattleState.AwaitingAction,
                session.State); // AwaitingAction 재전환 확인

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction

            Assert.IsFalse(
                session.HasPendingActorsThisTurn); // 전원 행동 완료 확인

            Assert.IsTrue(
                session.TryEndTurn()); // TurnEnd 전환

            Assert.AreEqual(
                BattleState.TurnEnd,
                session.State); // TurnEnd 상태 확인

            Assert.IsTrue(
                session.TryStartTurn()); // 다음 턴 시작

            Assert.AreEqual(
                2,
                session.TurnNumber); // 두 번째 턴 확인

            Assert.AreEqual(
                2,
                session.PendingActorsThisTurn.Count); // 다음 턴 순서 큐가 다시 채워짐 확인
        }

        [Test]
        public void TrySelectTarget_OnlyDuringAwaitingAction_WithValidTarget()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비

            BattleParticipant enemy =
                session.Context.Enemies[0];

            Assert.IsFalse(
                session.TrySelectTarget(
                    enemy)); // TurnStart에서는 대상 선택 거부 확인

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // Player 행동자 선출 (동률 우선순위)

            Assert.IsTrue(
                session.TrySelectTarget(
                    enemy)); // AwaitingAction에서는 선택 성공 확인

            Assert.AreSame(
                enemy,
                session.SelectedTarget); // 선택된 대상 확인
        }

        [Test]
        public void TrySelectTarget_CalledAgain_ReplacesPreviousSelection()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비

            BattleParticipant enemy =
                session.Context.Enemies[0];

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // Player 행동자 선출

            Assert.IsTrue(
                session.TrySelectTarget(
                    enemy)); // 1차 선택

            Assert.IsFalse(
                session.TrySelectTarget(
                    session.CurrentActor)); // 아군(자기 자신) 재선택 거부 확인

            Assert.AreSame(
                enemy,
                session.SelectedTarget); // 잘못된 재선택은 기존 선택을 바꾸지 않음 확인
        }

        [Test]
        public void TryEnterAwaitingAction_ForNextActor_ClearsPreviousSelectedTarget()
        {
            BattleSession session =
                CreateTurnStartSession(); // TurnStart Session 준비 (Player + Enemy)

            BattleParticipant enemy =
                session.Context.Enemies[0];

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째 행동자 (Player)

            Assert.IsTrue(
                session.TrySelectTarget(
                    enemy)); // 대상 선택

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction 전환

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 2번째 행동자 (Enemy)로 전환

            Assert.IsNull(
                session.SelectedTarget); // 새 행동자로 넘어가며 이전 선택 초기화 확인
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

            Assert.IsFalse(
                session.HasPendingActorsThisTurn); // 행동 순서 큐 정리 확인
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

            Assert.IsFalse(
                session.HasPendingActorsThisTurn); // 행동 순서 큐 정리 확인
        }

        private static BattleParticipant CreatePlayer()
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                20,
                5,
                6,
                3,
                90,
                10,
                0); // 테스트용 플레이어 참가자
        }

        private static BattleParticipant CreateEnemy()
        {
            return new BattleParticipant(
                "MON_TEST",
                "MON_TEST",
                BattleTeam.Enemy,
                10,
                5,
                4,
                2,
                80,
                5,
                0); // 테스트용 몬스터 참가자
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
