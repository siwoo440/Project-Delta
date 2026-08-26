using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class Day83EscapeStatusPersistenceTests
    {
        [TearDown]
        public void TearDown()
        {
            if (RunContext.Current != null)
            {
                RunContext.End();
            }
        }

        [Test]
        public void EscapeCapturePreservesBattleStatusExceptExtraAction()
        {
            PlayerRunState runState =
                PlayerRunState.CreateDefault();

            BattleParticipant player =
                CreatePlayer();

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "ENEMY#1",
                    3,
                    2,
                    5,
                    StatusEffectKind.DamageOverTime));

            player.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_EXTRA",
                    "PLAYER",
                    1,
                    1,
                    0,
                    StatusEffectKind.ExtraAction));

            PersistentPlayerStatusService.CaptureFromBattleAfterEscape(
                player,
                runState);

            Assert.That(
                runState.PersistentStatusEffects.Count,
                Is.EqualTo(1));

            Assert.That(
                runState.PersistentStatusEffects[0].DefinitionId,
                Is.EqualTo("STATUS_POISON"));

            Assert.That(
                runState.PersistentStatusEffects[0].RemainingDuration,
                Is.EqualTo(3));

            Assert.That(
                runState.PersistentStatusEffects[0].StackCount,
                Is.EqualTo(2));
        }

        [Test]
        public void BeginBattleRestoresPersistentStatusAndConsumesRunCopy()
        {
            PlayerRunState runState =
                PlayerRunState.CreateDefault();

            runState.PersistentStatusEffects.Add(
                new PersistentStatusEffectState
                {
                    DefinitionId = "STATUS_POISON",
                    SourceInstanceId = "ENEMY#1",
                    RemainingDuration = 2,
                    StackCount = 1,
                    AppliedValue = 4,
                    EffectKind = (int)StatusEffectKind.DamageOverTime,
                    TargetStat = (int)BattleStatType.Attack
                });

            BattleParticipant player =
                CreatePlayer();

            PersistentPlayerStatusService.RestoreToBattleAndClear(
                runState,
                player);

            Assert.That(
                player.StatusEffects.Count,
                Is.EqualTo(1));

            Assert.That(
                player.StatusEffects[0].RemainingRounds,
                Is.EqualTo(2));

            Assert.That(
                runState.PersistentStatusEffects.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void SuccessfulMoveAppliesDamageAndConsumesDuration()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.CurrentHp =
                50;

            player.PersistentStatusEffects.Add(
                new PersistentStatusEffectState
                {
                    DefinitionId = "STATUS_POISON",
                    SourceInstanceId = "ENEMY#1",
                    RemainingDuration = 2,
                    StackCount = 2,
                    AppliedValue = 5,
                    EffectKind = (int)StatusEffectKind.DamageOverTime
                });

            bool defeated =
                ExplorationStatusEffectService.ApplyAfterSuccessfulMove(
                    player);

            Assert.That(
                defeated,
                Is.False);

            Assert.That(
                player.CurrentHp,
                Is.EqualTo(40));

            Assert.That(
                player.PersistentStatusEffects[0].RemainingDuration,
                Is.EqualTo(1));
        }

        [Test]
        public void SuccessfulMoveRemovesExpiredStatus()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.PersistentStatusEffects.Add(
                new PersistentStatusEffectState
                {
                    DefinitionId = "STATUS_REGEN",
                    SourceInstanceId = "PLAYER",
                    RemainingDuration = 1,
                    StackCount = 1,
                    AppliedValue = 3,
                    EffectKind = (int)StatusEffectKind.HealOverTime
                });

            ExplorationStatusEffectService.ApplyAfterSuccessfulMove(
                player);

            Assert.That(
                player.PersistentStatusEffects.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void StunConsumesMoveAttemptWithoutTickingPoison()
        {
            PlayerRunState player =
                PlayerRunState.CreateDefault();

            player.PersistentStatusEffects.Add(
                new PersistentStatusEffectState
                {
                    DefinitionId = "STATUS_STUN",
                    SourceInstanceId = "ENEMY#1",
                    RemainingDuration = 1,
                    StackCount = 1,
                    AppliedValue = 0,
                    EffectKind = (int)StatusEffectKind.Stun
                });

            player.PersistentStatusEffects.Add(
                new PersistentStatusEffectState
                {
                    DefinitionId = "STATUS_POISON",
                    SourceInstanceId = "ENEMY#1",
                    RemainingDuration = 3,
                    StackCount = 1,
                    AppliedValue = 5,
                    EffectKind = (int)StatusEffectKind.DamageOverTime
                });

            int hpBefore =
                player.CurrentHp;

            bool blocked =
                ExplorationStatusEffectService.TryConsumeStunMoveAttempt(
                    player);

            Assert.That(
                blocked,
                Is.True);

            Assert.That(
                player.CurrentHp,
                Is.EqualTo(hpBefore));

            Assert.That(
                player.PersistentStatusEffects.Count,
                Is.EqualTo(1));

            Assert.That(
                player.PersistentStatusEffects[0].DefinitionId,
                Is.EqualTo("STATUS_POISON"));

            Assert.That(
                player.PersistentStatusEffects[0].RemainingDuration,
                Is.EqualTo(3));
        }

        [Test]
        public void EscapedEncounterCompletesRoomAndRemovesMonster()
        {
            EncounterResult result =
                new EncounterResult(
                    "Room_01",
                    "MON_SLIME",
                    EncounterOutcome.Escaped);

            Assert.That(
                result.CompletesRoom,
                Is.True);

            Assert.That(
                result.RemovesMonster,
                Is.True);
        }

        [Test]
        public void DungeonSaveMapperPreservesResourcesAndPersistentStatus()
        {
            RunContext source =
                RunContext.Begin(
                    "DAY83_SAVE");

            source.Player.CurrentHp =
                37;

            source.Player.CurrentMana =
                21;

            source.Player.CurrentStamina =
                64;

            source.Player.PersistentStatusEffects.Add(
                new PersistentStatusEffectState
                {
                    DefinitionId = "STATUS_POISON",
                    SourceInstanceId = "ENEMY#1",
                    RemainingDuration = 2,
                    StackCount = 1,
                    AppliedValue = 4,
                    EffectKind = (int)StatusEffectKind.DamageOverTime
                });

            RunData saved =
                DungeonSaveMapper.BuildFromRunContext(
                    source);

            RunContext.End();

            RunContext restored =
                RunContext.Begin(
                    "DAY83_RESTORE");

            DungeonSaveMapper.ApplyBasics(
                restored,
                saved);

            Assert.That(
                restored.Player.CurrentHp,
                Is.EqualTo(37));

            Assert.That(
                restored.Player.CurrentMana,
                Is.EqualTo(21));

            Assert.That(
                restored.Player.CurrentStamina,
                Is.EqualTo(64));

            Assert.That(
                restored.Player.PersistentStatusEffects.Count,
                Is.EqualTo(1));

            Assert.That(
                restored.Player.PersistentStatusEffects[0].RemainingDuration,
                Is.EqualTo(2));
        }

        private static BattleParticipant CreatePlayer()
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                100,
                50,
                50,
                40,
                90,
                40,
                50,
                50,
                50,
                100,
                100,
                50,
                100);
        }
    }
}
