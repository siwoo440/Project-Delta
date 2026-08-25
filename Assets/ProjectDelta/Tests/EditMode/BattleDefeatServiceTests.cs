using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // 패배 추적 기능 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class BattleDefeatServiceTests // 패배 추적 서비스 테스트
    {
        [SetUp]
        public void SetUp() // 각 테스트 초기화
        {
            BattleDefeatService.BeginBattle(); // 이전 패배 정보 제거
        }

        [Test]
        public void RecordAppliedDamage_PlayerDamaged_StoresAttacker() // 실제 플레이어 피해 공격자 기록 확인
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player); // 플레이어 생성

            BattleParticipant enemy =
                CreateParticipant(
                    "MON_A",
                    BattleTeam.Enemy); // 적 생성

            BattleDefeatService.RecordAppliedDamage(
                enemy,
                player,
                3); // 실제 피해 기록

            Assert.AreEqual(
                enemy.InstanceId,
                BattleDefeatService.LastAttackerInstanceId); // 마지막 공격자 확인

            Assert.AreEqual(
                enemy.DefinitionId,
                BattleDefeatService.LastAttackerDefinitionId); // 공격자 정의 확인
        }

        [Test]
        public void RecordAppliedDamage_ZeroDamage_DoesNotOverwriteAttacker() // 0 피해 덮어쓰기 방지 확인
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player); // 플레이어 생성

            BattleParticipant enemyA =
                CreateParticipant(
                    "MON_A",
                    BattleTeam.Enemy); // 첫 적 생성

            BattleParticipant enemyB =
                CreateParticipant(
                    "MON_B",
                    BattleTeam.Enemy); // 두 번째 적 생성

            BattleDefeatService.RecordAppliedDamage(
                enemyA,
                player,
                2); // 첫 실제 피해 기록

            BattleDefeatService.RecordAppliedDamage(
                enemyB,
                player,
                0); // 0 피해 기록 시도

            Assert.AreEqual(
                enemyA.InstanceId,
                BattleDefeatService.LastAttackerInstanceId); // 기존 공격자 유지 확인
        }

        [Test]
        public void RecordAppliedDamage_MultipleEnemies_StoresLatestActualAttacker() // 여러 적 마지막 공격자 확인
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player); // 플레이어 생성

            BattleParticipant enemyA =
                CreateParticipant(
                    "MON_A",
                    BattleTeam.Enemy); // 첫 적 생성

            BattleParticipant enemyB =
                CreateParticipant(
                    "MON_B",
                    BattleTeam.Enemy); // 두 번째 적 생성

            BattleDefeatService.RecordAppliedDamage(
                enemyA,
                player,
                2); // 첫 피해 기록

            BattleDefeatService.RecordAppliedDamage(
                enemyB,
                player,
                4); // 두 번째 피해 기록

            Assert.AreEqual(
                enemyB.InstanceId,
                BattleDefeatService.LastAttackerInstanceId); // 가장 최근 공격자 확인
        }

        [Test]
        public void RecordEnemyDefeat_UsesLastAttacker() // 일반 패배 기록 공격자 연결 확인
        {
            BattleParticipant player =
                CreateParticipant(
                    "PLAYER",
                    BattleTeam.Player); // 플레이어 생성

            BattleParticipant enemy =
                CreateParticipant(
                    "MON_A",
                    BattleTeam.Enemy); // 적 생성

            BattleDefeatService.RecordAppliedDamage(
                enemy,
                player,
                5); // 실제 피해 기록

            BattleDefeatRecord record =
                BattleDefeatService.RecordEnemyDefeat(
                    3); // 일반 패배 기록 생성

            Assert.AreEqual(
                BattleDefeatReason.EnemyAttack,
                record.Reason); // 일반 패배 사유 확인

            Assert.AreEqual(
                enemy.InstanceId,
                record.AttackerInstanceId); // 마지막 공격자 확인

            Assert.AreEqual(
                3,
                record.RoundNumber); // 패배 라운드 확인
        }

        [Test]
        public void RecordSurrender_HasNoAttacker() // 항복에 공격자 미지정 확인
        {
            BattleDefeatRecord record =
                BattleDefeatService.RecordSurrender(
                    2); // 항복 기록 생성

            Assert.AreEqual(
                BattleDefeatReason.Surrender,
                record.Reason); // 항복 사유 확인

            Assert.IsFalse(
                record.HasAttacker); // 공격자 없음 확인

            Assert.AreEqual(
                2,
                record.RoundNumber); // 항복 라운드 확인
        }

        private static BattleParticipant CreateParticipant( // 테스트 참가자 생성
            string id, // 참가자 ID 입력
            BattleTeam team) // 참가자 팀 입력
        {
            return new BattleParticipant(
                id,
                id,
                team,
                20,
                10,
                10,
                5,
                90,
                5); // 최소 전투 참가자 생성
        }
    }
}
