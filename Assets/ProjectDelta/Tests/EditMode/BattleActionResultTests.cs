using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // BattleActionResult 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleActionResultTests
    {
        [Test]
        public void Reject_SetsAcceptedFalseAndSaveRequiredFalse()
        {
            BattleActionResult result =
                BattleActionResult.Reject(
                    "Attack",
                    "대상을 선택할 수 없습니다.");

            Assert.IsFalse(
                result.Accepted);

            Assert.IsFalse(
                result.SaveRequired); // 59일차: 거부된 행동은 게임 데이터를 바꾸지 않으므로 저장 불필요

            Assert.AreEqual(
                1,
                result.Logs.Count);

            Assert.AreEqual(
                "대상을 선택할 수 없습니다.",
                result.Logs[0]);

            Assert.AreEqual(
                0,
                result.DamageChanges.Count);

            Assert.AreEqual(
                0,
                result.RemovedParticipants.Count);

            Assert.IsNull(
                result.BattleEndResult);
        }

        [Test]
        public void Accept_StoresAllProvidedData()
        {
            BattleParticipant attacker =
                CreateParticipant(
                    "PLAYER");

            BattleParticipant target =
                CreateParticipant(
                    "MON_TEST");

            BattleDamageResult damageResult =
                BattleDamageResult.Hit(
                    9,
                    70,
                    9,
                    100,
                    false);

            BattleDamageChange damageChange =
                new BattleDamageChange(
                    attacker,
                    target,
                    damageResult,
                    9);

            BattleResult battleEndResult =
                new BattleResult(
                    BattleOutcome.Victory,
                    3);

            BattleActionResult result =
                BattleActionResult.Accept(
                    "Attack",
                    new[] { "공격 적중" },
                    new[] { damageChange },
                    new[] { target },
                    true,
                    battleEndResult);

            Assert.IsTrue(
                result.Accepted);

            Assert.IsTrue(
                result.SaveRequired);

            Assert.AreEqual(
                1,
                result.Logs.Count);

            Assert.AreEqual(
                1,
                result.DamageChanges.Count);

            Assert.AreSame(
                damageChange,
                result.DamageChanges[0]);

            Assert.AreEqual(
                1,
                result.RemovedParticipants.Count);

            Assert.AreSame(
                target,
                result.RemovedParticipants[0]);

            Assert.AreSame(
                battleEndResult,
                result.BattleEndResult);
        }

        [Test]
        public void Accept_WithoutBattleEndResult_LeavesItNull()
        {
            BattleActionResult result =
                BattleActionResult.Accept(
                    "Defend",
                    new[] { "방어 확정" },
                    new BattleDamageChange[0],
                    new BattleParticipant[0],
                    true,
                    null);

            Assert.IsNull(
                result.BattleEndResult);
        }

        private static BattleParticipant CreateParticipant(
            string instanceId)
        {
            return new BattleParticipant(
                instanceId,
                instanceId,
                BattleTeam.Player,
                20,
                5,
                6,
                3,
                90,
                10,
                0);
        }
    }
}
