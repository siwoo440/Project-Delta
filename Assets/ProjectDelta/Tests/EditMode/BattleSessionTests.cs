using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Session 사용
using ProjectDelta.Data; // StatusEffectKind 사용

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
                session.RoundNumber); // 라운드 번호 0 확인

            Assert.IsFalse(
                session.HasPendingActorsThisRound); // 남은 행동자 없음 확인
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
        public void TryBeginBattle_WithMaxEnemySlotEnemies_Starts()
        {
            // 75일차: 적 최대 인원 4명은 정상적으로 시작돼야 한다.
            BattleSession session =
                new BattleSession(); // 새 Session 생성

            BattleParticipant[] enemies =
                new BattleParticipant[BattleContext.MaxEnemySlots];

            for (int index = 0; index < enemies.Length; index++)
            {
                enemies[index] =
                    CreateEnemy();
            }

            BattleContext context =
                new BattleContext(
                    CreatePlayer(),
                    enemies);

            Assert.IsTrue(
                session.TryBeginBattle(
                    context));

            Assert.AreEqual(
                BattleState.Starting,
                session.State);
        }

        [Test]
        public void TryBeginBattle_WithMoreThanMaxEnemySlotEnemies_StaysIdle()
        {
            // 75일차: 적 최대 인원(4명)을 넘기면 거부해야 한다.
            BattleSession session =
                new BattleSession(); // 새 Session 생성

            BattleParticipant[] enemies =
                new BattleParticipant[BattleContext.MaxEnemySlots + 1];

            for (int index = 0; index < enemies.Length; index++)
            {
                enemies[index] =
                    CreateEnemy();
            }

            BattleContext context =
                new BattleContext(
                    CreatePlayer(),
                    enemies);

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
        public void TryStartRound_FromStarting_EntersRoundStartAndBuildsOrderQueue()
        {
            BattleSession session =
                CreateStartingSession(); // Starting Session 준비

            Assert.IsTrue(
                session.TryStartRound()); // RoundStart 전환

            Assert.AreEqual(
                BattleState.RoundStart,
                session.State); // RoundStart 상태 확인

            Assert.AreEqual(
                1,
                session.RoundNumber); // 첫 라운드 번호 확인

            Assert.IsTrue(
                session.HasPendingActorsThisRound); // 행동 순서 큐 생성 확인 (Player + Enemy)

            Assert.AreEqual(
                2,
                session.PendingActorsThisRound.Count); // Player 1명 + Enemy 1명 확인
        }

        [Test]
        public void TryEnterAwaitingAction_OnlyFromRoundStartOrResolvingAction_PopsNextActor()
        {
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비

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
                session.PendingActorsThisRound.Count); // 남은 행동자 1명 확인
        }

        [Test]
        public void TryEnterAwaitingAction_FromAwaitingAction_Rejected()
        {
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 첫 행동자 진행

            Assert.IsFalse(
                session.TryEnterAwaitingAction()); // AwaitingAction 상태에서 중복 호출 거부 확인
        }

        [Test]
        public void TryEnterAwaitingAction_WithEmptyQueue_Rejected()
        {
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비 (Player + Enemy 1명씩)

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째 행동자 (Player)

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction 전환

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 2번째 행동자 (Enemy)

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction 전환

            Assert.IsFalse(
                session.HasPendingActorsThisRound); // 이번 라운드 행동자 모두 소진 확인

            Assert.IsFalse(
                session.TryEnterAwaitingAction()); // 남은 행동자 없을 때 거부 확인
        }

        [Test]
        public void TryEnterAwaitingAction_SkipsActorThatDiedMidRound_AndContinuesToNextAlive()
        {
            // Speed 내림차순: ENEMY_FAST(20) → PLAYER(5) → ENEMY_SLOW(1)
            BattleParticipant player =
                CreatePlayer();

            BattleParticipant fastEnemy =
                new BattleParticipant(
                    "ENEMY_FAST",
                    "ENEMY_FAST",
                    BattleTeam.Enemy,
                    10,
                    20,
                    4,
                    2,
                    80,
                    5,
                    0);

            BattleParticipant slowEnemy =
                new BattleParticipant(
                    "ENEMY_SLOW",
                    "ENEMY_SLOW",
                    BattleTeam.Enemy,
                    10,
                    1,
                    4,
                    2,
                    80,
                    5,
                    0);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { fastEnemy, slowEnemy });

            BattleSession session =
                new BattleSession();

            Assert.IsTrue(
                session.TryBeginBattle(
                    context));

            Assert.IsTrue(
                session.TryStartRound());

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째: ENEMY_FAST

            Assert.AreEqual(
                "ENEMY_FAST",
                session.CurrentActor.InstanceId);

            Assert.IsTrue(
                session.TryBeginResolveAction());

            // ENEMY_FAST의 행동으로 아직 순서가 오지 않은 ENEMY_SLOW가 죽었다고 가정 (전투 이탈 상황 재현)
            slowEnemy.ApplyDamage(
                999);

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 2번째: PLAYER (ENEMY_SLOW는 아직 큐에 남아있음)

            Assert.AreEqual(
                "PLAYER",
                session.CurrentActor.InstanceId);

            Assert.IsTrue(
                session.TryBeginResolveAction());

            // 마지막 남은 ENEMY_SLOW는 이미 죽어있으므로 건너뛰고 큐가 소진된다
            Assert.IsFalse(
                session.TryEnterAwaitingAction()); // 죽은 참가자만 남아 진행 실패 확인

            Assert.IsFalse(
                session.HasPendingActorsThisRound); // 큐 소진 확인

            Assert.IsTrue(
                session.TryEndRound()); // 남은 행동자가 없으므로 RoundEnd 허용 확인
        }

        [Test]
        public void TryEndRound_WithPendingActors_Rejected()
        {
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비 (Player + Enemy)

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째 행동자만 진행

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction 전환

            Assert.IsTrue(
                session.HasPendingActorsThisRound); // 아직 Enemy가 남음

            Assert.IsFalse(
                session.TryEndRound()); // 남은 행동자가 있으면 RoundEnd 거부 확인
        }

        [Test]
        public void RoundLifecycle_AllActorsActBeforeRoundEndsThenNextRoundRebuildsQueue()
        {
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비 (Player + Enemy)

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째 행동자

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction

            Assert.IsFalse(
                session.TryEndRound()); // 아직 2번째 행동자가 남아 거부

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 2번째 행동자로 직접 전환 (ResolvingAction → AwaitingAction)

            Assert.AreEqual(
                BattleState.AwaitingAction,
                session.State); // AwaitingAction 재전환 확인

            Assert.IsTrue(
                session.TryBeginResolveAction()); // ResolvingAction

            Assert.IsFalse(
                session.HasPendingActorsThisRound); // 전원 행동 완료 확인

            Assert.IsTrue(
                session.TryEndRound()); // RoundEnd 전환

            Assert.AreEqual(
                BattleState.RoundEnd,
                session.State); // RoundEnd 상태 확인

            Assert.IsTrue(
                session.TryStartRound()); // 다음 라운드 시작

            Assert.AreEqual(
                2,
                session.RoundNumber); // 두 번째 라운드 확인

            Assert.AreEqual(
                2,
                session.PendingActorsThisRound.Count); // 다음 라운드 순서 큐가 다시 채워짐 확인
        }

        [Test]
        public void TryEnterAwaitingAction_ClearsActorsOwnDefendingStateWhenTheirRoundComesAgain()
        {
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비 (Player + Enemy, 동률로 Player 우선)

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째: Player

            BattleParticipant player =
                session.CurrentActor;

            player.SetDefending(
                true); // 방어 선택

            Assert.IsTrue(
                session.TryBeginResolveAction());

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 2번째: Enemy

            Assert.IsTrue(
                player.IsDefending); // 아직 Player 차례가 안 돌아왔으므로 방어 유지 확인

            Assert.IsTrue(
                session.TryBeginResolveAction());

            Assert.IsTrue(
                session.TryEndRound());

            Assert.IsTrue(
                session.TryStartRound()); // 다음 라운드

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // Player 차례가 다시 돌아옴

            Assert.AreSame(
                player,
                session.CurrentActor);

            Assert.IsFalse(
                player.IsDefending); // 자기 차례가 돌아오며 방어 해제 확인
        }

        [Test]
        public void TrySelectTarget_OnlyDuringAwaitingAction_WithValidTarget()
        {
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비

            BattleParticipant enemy =
                session.Context.Enemies[0];

            Assert.IsFalse(
                session.TrySelectTarget(
                    enemy)); // RoundStart에서는 대상 선택 거부 확인

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
                CreateRoundStartSession(); // RoundStart Session 준비

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
                CreateRoundStartSession(); // RoundStart Session 준비 (Player + Enemy)

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
                CreateRoundStartSession(); // RoundStart Session 준비

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
                session.Result.RoundCount); // 종료 시점 라운드 번호 확인
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
                CreateRoundStartSession(); // RoundStart Session 준비

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
                CreateRoundStartSession(); // RoundStart Session 준비

            Assert.IsFalse(
                session.TryReset()); // RoundStart에서 Reset 거부 확인

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
                session.RoundNumber); // 라운드 번호 초기화 확인

            Assert.IsFalse(
                session.HasPendingActorsThisRound); // 행동 순서 큐 정리 확인
        }

        [Test]
        public void ForceReset_ReturnsAnyStateToIdle()
        {
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비

            session.ForceReset(); // 강제 초기화

            Assert.AreEqual(
                BattleState.Idle,
                session.State); // Idle 복귀 확인

            Assert.IsNull(
                session.Context); // Context 제거 확인

            Assert.IsFalse(
                session.IsActive); // 비활성 상태 확인

            Assert.IsFalse(
                session.HasPendingActorsThisRound); // 행동 순서 큐 정리 확인
        }

        [Test]
        public void TryEndRound_AppliesStatusEffectDamageAndDecrementsDuration()
        {
            // 60일차: 라운드 파이프라인에 끼워 넣은 "지속 피해와 회복 적용 → 상태 지속 시간
            // 감소" 단계가 TryEndRound()에서 실제로 호출되는지 확인한다.
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비 (Player + Enemy)

            BattleParticipant player =
                session.Context.Player;

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    2,
                    1,
                    5,
                    StatusEffectKind.DamageOverTime)); // 중독: 라운드 종료 시 5 피해, 남은 2라운드

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째 행동자

            Assert.IsTrue(
                session.TryBeginResolveAction());

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 2번째 행동자

            Assert.IsTrue(
                session.TryBeginResolveAction());

            Assert.IsTrue(
                session.TryEndRound());

            Assert.AreEqual(
                15,
                player.CurrentHp); // 20 - 5(중독) 확인

            Assert.AreEqual(
                1,
                player.StatusEffects[0].RemainingRounds); // 2 → 1로 감소, 아직 만료 아님
        }

        [Test]
        public void TryEnterAwaitingAction_SkipsStunnedActor_AndConsumesTurnWithoutAwaitingInput()
        {
            // 64일차: 기절한 참가자는 입력을 요구하지 않고 차례만 소비한 뒤 다음 행동자로 넘어간다.
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비 (Player + Enemy, 동률로 Player 우선)

            BattleParticipant player =
                session.Context.Player;

            StatusEffectInstance stun =
                new StatusEffectInstance(
                    "STATUS_STUN",
                    "MON_TEST",
                    1,
                    1,
                    0,
                    StatusEffectKind.Stun);

            player.AddStatusEffect(
                stun);

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // Player는 기절이라 건너뛰고 Enemy가 곧바로 선출됨

            Assert.AreEqual(
                "MON_TEST",
                session.CurrentActor.InstanceId); // 기절 건너뜀 뒤 다음 참가자 정상 진행 확인

            Assert.IsFalse(
                session.HasPendingActorsThisRound); // Player·Enemy 모두 큐에서 빠졌으므로 없음

            Assert.AreEqual(
                1,
                stun.RemainingRounds); // 기절 차례를 건너뛰어도 지속시간은 여기서 감소하지 않음

            Assert.IsTrue(
                session.TryBeginResolveAction());

            Assert.IsTrue(
                session.TryEndRound()); // 남은 행동자 없이 정상적으로 라운드 종료

            Assert.AreEqual(
                0,
                stun.RemainingRounds); // 라운드 종료 지속시간 감소 단계에서만 1 감소
        }

        [Test]
        public void TryGrantExtraAction_InsertsActorAtFrontOfQueue_ThenResumesNormalOrder()
        {
            // 64일차: 추가 행동을 소비한 A가 한 번 더 행동한 뒤 정상 순서(B)로 복귀해야 한다.
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비 (Player + Enemy, 동률로 Player 우선)

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 1번째: Player

            BattleParticipant player =
                session.CurrentActor;

            Assert.AreEqual(
                "PLAYER",
                player.InstanceId);

            Assert.IsTrue(
                session.TryGrantExtraAction(
                    player)); // Player에게 추가 행동 부여

            Assert.IsTrue(
                session.TryBeginResolveAction());

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 추가 행동 소비: Player가 한 번 더 행동

            Assert.AreSame(
                player,
                session.CurrentActor);

            Assert.IsTrue(
                session.TryBeginResolveAction());

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // 정상 순서 복귀: Enemy

            Assert.AreEqual(
                "MON_TEST",
                session.CurrentActor.InstanceId);

            Assert.IsTrue(
                session.TryBeginResolveAction());

            Assert.IsTrue(
                session.TryEndRound()); // 전원 행동 완료로 라운드 정상 종료
        }

        [Test]
        public void TryGrantExtraAction_SameActorTwiceInSameRound_SecondGrantRejected()
        {
            // 64일차: 추가 행동이 다시 추가 행동을 무한 생성하지 않도록 라운드당 한 번만 허용한다.
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비

            Assert.IsTrue(
                session.TryEnterAwaitingAction()); // Player

            BattleParticipant player =
                session.CurrentActor;

            Assert.IsTrue(
                session.TryGrantExtraAction(
                    player)); // 1차 부여 성공

            Assert.IsFalse(
                session.TryGrantExtraAction(
                    player)); // 같은 라운드 2차 부여 거부
        }

        [Test]
        public void TryGrantExtraAction_ActorNotInContext_Rejected()
        {
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비

            Assert.IsTrue(
                session.TryEnterAwaitingAction());

            BattleParticipant outsider =
                CreatePlayer(); // Context에 속하지 않은 별개 참가자

            Assert.IsFalse(
                session.TryGrantExtraAction(
                    outsider));
        }

        [Test]
        public void TryFinishBattle_ClearsAllParticipantStatusEffects()
        {
            // 64일차: 전투가 끝나면 전투 한정 상태(중독·기절 등)가 다음 전투까지 남지 않아야 한다.
            BattleSession session =
                CreateRoundStartSession(); // RoundStart Session 준비 (Player + Enemy)

            BattleParticipant player =
                session.Context.Player;

            BattleParticipant enemy =
                session.Context.Enemies[0];

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    2,
                    1,
                    5,
                    StatusEffectKind.DamageOverTime));

            enemy.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_STUN",
                    "PLAYER",
                    99,
                    1,
                    0,
                    StatusEffectKind.Stun)); // UntilCombatEnd 여부와 무관하게 전투 종료 시 제거되어야 함

            Assert.IsTrue(
                session.TryFinishBattle(
                    BattleOutcome.Victory));

            Assert.AreEqual(
                0,
                player.StatusEffects.Count);

            Assert.AreEqual(
                0,
                enemy.StatusEffects.Count);
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

        private static BattleSession CreateRoundStartSession()
        {
            BattleSession session =
                CreateStartingSession(); // Starting Session 준비

            Assert.IsTrue(
                session.TryStartRound()); // RoundStart 전환

            return session; // RoundStart Session 반환
        }
    }
}
