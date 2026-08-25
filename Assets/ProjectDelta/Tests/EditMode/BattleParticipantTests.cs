using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // Battle Participant 사용
using ProjectDelta.Data; // StatusEffectKind 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleParticipantTests
    {
        [Test]
        public void Constructor_StoresSevenCombatStats()
        {
            BattleParticipant participant =
                new BattleParticipant(
                    "PLAYER",
                    "PLAYER",
                    BattleTeam.Player,
                    20,
                    7,
                    8,
                    9,
                    10,
                    11,
                    12,
                    13);

            Assert.AreEqual(
                7,
                participant.Speed);

            Assert.AreEqual(
                8,
                participant.Attack);

            Assert.AreEqual(
                9,
                participant.Defense);

            Assert.AreEqual(
                10,
                participant.Accuracy);

            Assert.AreEqual(
                11,
                participant.Evasion);

            Assert.AreEqual(
                12,
                participant.Charm);

            Assert.AreEqual(
                13,
                participant.Resistance);
        }

        [Test]
        public void Constructor_WithoutMaxManaOrStamina_DefaultsToZero()
        {
            // 54일차: maxMana·maxStamina를 생략하는 기존 호출부(몬스터 등)와의 호환성 확인.
            BattleParticipant participant =
                CreateParticipant(
                    20);

            Assert.AreEqual(
                0,
                participant.MaxMana);

            Assert.AreEqual(
                0,
                participant.CurrentMana);

            Assert.AreEqual(
                0,
                participant.MaxStamina);

            Assert.AreEqual(
                0,
                participant.CurrentStamina);
        }

        [Test]
        public void Constructor_WithMaxManaAndStamina_StartsFull()
        {
            BattleParticipant participant =
                new BattleParticipant(
                    "PLAYER",
                    "PLAYER",
                    BattleTeam.Player,
                    20,
                    5,
                    6,
                    3,
                    90,
                    10,
                    0,
                    0,
                    50,
                    100);

            Assert.AreEqual(
                50,
                participant.MaxMana);

            Assert.AreEqual(
                50,
                participant.CurrentMana);

            Assert.AreEqual(
                100,
                participant.MaxStamina);

            Assert.AreEqual(
                100,
                participant.CurrentStamina);
        }

        [Test]
        public void Constructor_WithCurrentValues_CarriesOverFromRunState()
        {
            // 54일차: 전투 진입 시 PlayerRunState의 현재 체력·마나·정력을 그대로 이어받는 경로.
            BattleParticipant participant =
                new BattleParticipant(
                    "PLAYER",
                    "PLAYER",
                    BattleTeam.Player,
                    20,
                    5,
                    6,
                    3,
                    90,
                    10,
                    0,
                    0,
                    50,
                    100,
                    currentHp: 12,
                    currentMana: 30,
                    currentStamina: 40);

            Assert.AreEqual(
                12,
                participant.CurrentHp);

            Assert.AreEqual(
                30,
                participant.CurrentMana);

            Assert.AreEqual(
                40,
                participant.CurrentStamina);
        }

        [Test]
        public void Constructor_WithCurrentValueAboveMax_ClampsToMax()
        {
            BattleParticipant participant =
                new BattleParticipant(
                    "PLAYER",
                    "PLAYER",
                    BattleTeam.Player,
                    20,
                    5,
                    6,
                    3,
                    90,
                    10,
                    0,
                    0,
                    50,
                    100,
                    currentHp: 999,
                    currentMana: 999,
                    currentStamina: 999);

            Assert.AreEqual(
                20,
                participant.CurrentHp);

            Assert.AreEqual(
                50,
                participant.CurrentMana);

            Assert.AreEqual(
                100,
                participant.CurrentStamina);
        }

        [Test]
        public void ApplyDamage_ReducesCurrentHpByAmount()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20); // MaxHp 20

            int appliedDamage =
                participant.ApplyDamage(
                    7);

            Assert.AreEqual(
                7,
                appliedDamage); // 실제 적용된 피해량 확인

            Assert.AreEqual(
                13,
                participant.CurrentHp); // HP 감소 확인

            Assert.IsTrue(
                participant.IsAlive); // 생존 유지 확인
        }

        [Test]
        public void ApplyDamage_ExceedingCurrentHp_ClampsAtZeroAndReturnsActualDamage()
        {
            BattleParticipant participant =
                CreateParticipant(
                    10); // MaxHp 10

            int appliedDamage =
                participant.ApplyDamage(
                    999); // 남은 HP보다 훨씬 큰 피해

            Assert.AreEqual(
                10,
                appliedDamage); // 실제로는 남은 HP만큼만 적용 확인

            Assert.AreEqual(
                0,
                participant.CurrentHp); // 0 이하로 내려가지 않음 확인

            Assert.IsFalse(
                participant.IsAlive); // 사망 판정 확인
        }

        [Test]
        public void ApplyDamage_ZeroOrNegativeAmount_DoesNothing()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            Assert.AreEqual(
                0,
                participant.ApplyDamage(
                    0)); // 0 피해 무시 확인

            Assert.AreEqual(
                0,
                participant.ApplyDamage(
                    -5)); // 음수 피해 무시 확인

            Assert.AreEqual(
                20,
                participant.CurrentHp); // HP 변화 없음 확인
        }

        [Test]
        public void ApplyDamage_MultipleHits_AccumulatesDamage()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            participant.ApplyDamage(
                5);

            participant.ApplyDamage(
                6);

            Assert.AreEqual(
                9,
                participant.CurrentHp); // 20 - 5 - 6 = 9 확인
        }

        [Test]
        public void SetDefending_TogglesIsDefending()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            Assert.IsFalse(
                participant.IsDefending); // 기본값 false 확인

            participant.SetDefending(
                true);

            Assert.IsTrue(
                participant.IsDefending);

            participant.SetDefending(
                false);

            Assert.IsFalse(
                participant.IsDefending);
        }

        [Test]
        public void Heal_IncreasesCurrentHpByAmount()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            participant.ApplyDamage(
                15); // 20 → 5

            int appliedHeal =
                participant.Heal(
                    3);

            Assert.AreEqual(
                3,
                appliedHeal);

            Assert.AreEqual(
                8,
                participant.CurrentHp);
        }

        [Test]
        public void Heal_ExceedingMaxHp_ClampsAtMaxAndReturnsActualHeal()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            participant.ApplyDamage(
                5); // 20 → 15

            int appliedHeal =
                participant.Heal(
                    999);

            Assert.AreEqual(
                5,
                appliedHeal); // 최대 HP까지만 회복

            Assert.AreEqual(
                20,
                participant.CurrentHp);
        }

        [Test]
        public void Heal_ZeroOrNegativeAmount_DoesNothing()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            participant.ApplyDamage(
                10);

            Assert.AreEqual(
                0,
                participant.Heal(
                    0));

            Assert.AreEqual(
                0,
                participant.Heal(
                    -5));

            Assert.AreEqual(
                10,
                participant.CurrentHp);
        }

        [Test]
        public void StatusEffects_StartsEmpty()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            Assert.AreEqual(
                0,
                participant.StatusEffects.Count);
        }

        [Test]
        public void AddStatusEffect_AddsToStatusEffectsList()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            StatusEffectInstance status =
                new StatusEffectInstance(
                    "STATUS_TEST",
                    "MON_TEST",
                    2,
                    1,
                    -5,
                    StatusEffectKind.DamageOverTime);

            participant.AddStatusEffect(
                status);

            Assert.AreEqual(
                1,
                participant.StatusEffects.Count);

            Assert.AreSame(
                status,
                participant.StatusEffects[0]);
        }

        [Test]
        public void RemoveExpiredStatusEffects_RemovesOnlyExpiredEntries()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            StatusEffectInstance expired =
                new StatusEffectInstance(
                    "STATUS_EXPIRED",
                    "MON_TEST",
                    0,
                    1,
                    -5,
                    StatusEffectKind.DamageOverTime);

            StatusEffectInstance active =
                new StatusEffectInstance(
                    "STATUS_ACTIVE",
                    "MON_TEST",
                    2,
                    1,
                    -5,
                    StatusEffectKind.DamageOverTime);

            participant.AddStatusEffect(
                expired);

            participant.AddStatusEffect(
                active);

            participant.RemoveExpiredStatusEffects();

            Assert.AreEqual(
                1,
                participant.StatusEffects.Count);

            Assert.AreSame(
                active,
                participant.StatusEffects[0]);
        }

        [Test]
        public void HasActiveStatusEffectOfKind_TrueOnlyWhileMatchingKindIsActive()
        {
            // 64일차: BattleSession이 기절 판정에 사용하는 조회 API를 검증한다.
            BattleParticipant participant =
                CreateParticipant(
                    20);

            Assert.IsFalse(
                participant.HasActiveStatusEffectOfKind(
                    StatusEffectKind.Stun)); // 상태 없으면 false

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_STUN",
                    "MON_TEST",
                    1,
                    1,
                    0,
                    StatusEffectKind.Stun));

            Assert.IsTrue(
                participant.HasActiveStatusEffectOfKind(
                    StatusEffectKind.Stun)); // 기절 상태 존재하면 true

            Assert.IsFalse(
                participant.HasActiveStatusEffectOfKind(
                    StatusEffectKind.DamageOverTime)); // 다른 종류는 여전히 false
        }

        [Test]
        public void HasActiveStatusEffectOfKind_IgnoresExpiredEntries()
        {
            BattleParticipant participant =
                CreateParticipant(
                    20);

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_STUN",
                    "MON_TEST",
                    0,
                    1,
                    0,
                    StatusEffectKind.Stun)); // 이미 만료된 상태

            Assert.IsFalse(
                participant.HasActiveStatusEffectOfKind(
                    StatusEffectKind.Stun));
        }

        [Test]
        public void RemoveAllStatusEffects_ClearsEveryEntryRegardlessOfDurationType()
        {
            // 64일차: 전투 종료 정리 대상 확인 (Rounds·UntilCombatEnd 구분 없이 전체 제거).
            BattleParticipant participant =
                CreateParticipant(
                    20);

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_POISON",
                    "MON_TEST",
                    2,
                    1,
                    -5,
                    StatusEffectKind.DamageOverTime));

            participant.AddStatusEffect(
                new StatusEffectInstance(
                    "STATUS_STUN",
                    "MON_TEST",
                    1,
                    1,
                    0,
                    StatusEffectKind.Stun));

            participant.RemoveAllStatusEffects();

            Assert.AreEqual(
                0,
                participant.StatusEffects.Count);
        }

        [Test]
        public void TrySpendMana_SufficientMana_DeductsAndReturnsTrue()
        {
            // 66일차: 스킬 자원 소모의 첫 API. 충분하면 전액 차감하고 성공한다.
            BattleParticipant participant =
                CreateParticipantWithResources(
                    maxMana: 50,
                    maxStamina: 100);

            bool succeeded =
                participant.TrySpendMana(
                    20);

            Assert.IsTrue(
                succeeded);

            Assert.AreEqual(
                30,
                participant.CurrentMana);
        }

        [Test]
        public void TrySpendMana_InsufficientMana_ReturnsFalseAndDoesNotChange()
        {
            BattleParticipant participant =
                CreateParticipantWithResources(
                    maxMana: 10,
                    maxStamina: 0);

            bool succeeded =
                participant.TrySpendMana(
                    11);

            Assert.IsFalse(
                succeeded);

            Assert.AreEqual(
                10,
                participant.CurrentMana); // 실패 시 변화 없음 확인
        }

        [Test]
        public void TrySpendMana_ZeroOrNegativeAmount_AlwaysSucceedsWithoutChange()
        {
            BattleParticipant participant =
                CreateParticipantWithResources(
                    maxMana: 10,
                    maxStamina: 0);

            Assert.IsTrue(
                participant.TrySpendMana(
                    0));

            Assert.IsTrue(
                participant.TrySpendMana(
                    -5));

            Assert.AreEqual(
                10,
                participant.CurrentMana);
        }

        [Test]
        public void TrySpendMana_ExactRemainingAmount_SucceedsAndReachesZero()
        {
            BattleParticipant participant =
                CreateParticipantWithResources(
                    maxMana: 10,
                    maxStamina: 0);

            Assert.IsTrue(
                participant.TrySpendMana(
                    10));

            Assert.AreEqual(
                0,
                participant.CurrentMana);
        }

        [Test]
        public void TrySpendStamina_SufficientStamina_DeductsAndReturnsTrue()
        {
            BattleParticipant participant =
                CreateParticipantWithResources(
                    maxMana: 0,
                    maxStamina: 40);

            bool succeeded =
                participant.TrySpendStamina(
                    15);

            Assert.IsTrue(
                succeeded);

            Assert.AreEqual(
                25,
                participant.CurrentStamina);
        }

        [Test]
        public void TrySpendStamina_InsufficientStamina_ReturnsFalseAndDoesNotChange()
        {
            BattleParticipant participant =
                CreateParticipantWithResources(
                    maxMana: 0,
                    maxStamina: 5);

            bool succeeded =
                participant.TrySpendStamina(
                    6);

            Assert.IsFalse(
                succeeded);

            Assert.AreEqual(
                5,
                participant.CurrentStamina);
        }

        private static BattleParticipant CreateParticipantWithResources(
            int maxMana,
            int maxStamina)
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
                0,
                0,
                maxMana,
                maxStamina);
        }

        private static BattleParticipant CreateParticipant(
            int maxHp)
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                maxHp,
                5,
                6,
                3,
                90,
                10,
                0,
                0);
        }
    }
}
