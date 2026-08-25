using System;
using System.Collections.Generic;

namespace ProjectDelta.Application
{
    // 59일차: 전투 명령(공격·방어 등) 처리 결과. 문자열 메시지 하나로 뭉뚱그리던 49일차
    // BattleCommandResult 대신, 실제로 무엇이 바뀌었는지(변화 목록)와 로그·저장 필요 여부·
    // 전투 종료 결과를 따로 담는다 (기획서 10.3 BattleActionResult).
    //
    // ResourceChanges·StatusChanges·UpdatedIntents는 기획서 10.3에 있지만, 이를 만들어내는
    // 시스템(자원 소모 Command, 상태 이상, 몬스터 행동 예고)이 아직 없어 이번 일차에는 넣지
    // 않는다. 해당 시스템이 생기는 일차(60~65일차 상태 이상, 66일차 이후 스킬)에서 추가한다.
    //
    // IBattleCommand.Execute()는 지금처럼 더 단순한 BattleCommandResult(선언 수락 여부)를
    // 반환한다. 실제 판정(명중·피해·사망 판정)은 아직 Presentation(컨트롤러)에서 이뤄지므로,
    // 이 결과도 거기서 조립한다. Command가 판정까지 직접 마치고 BattleActionResult를 반환하는
    // 형태로 옮기는 건 스킬 Command가 여럿 생기는 66일차 이후에 재검토한다.
    public sealed class BattleActionResult
    {
        public string CommandId { get; }
        public bool Accepted { get; }
        public IReadOnlyList<string> Logs { get; }
        public IReadOnlyList<BattleDamageChange> DamageChanges { get; }
        public IReadOnlyList<BattleParticipant> RemovedParticipants { get; }
        public bool SaveRequired { get; }
        public BattleResult BattleEndResult { get; }

        private BattleActionResult(
            string commandId,
            bool accepted,
            IReadOnlyList<string> logs,
            IReadOnlyList<BattleDamageChange> damageChanges,
            IReadOnlyList<BattleParticipant> removedParticipants,
            bool saveRequired,
            BattleResult battleEndResult)
        {
            CommandId =
                commandId;

            Accepted =
                accepted;

            Logs =
                logs;

            DamageChanges =
                damageChanges;

            RemovedParticipants =
                removedParticipants;

            SaveRequired =
                saveRequired;

            BattleEndResult =
                battleEndResult;
        }

        // 명령 자체가 거부됐을 때(대상 없음 등). 실제 게임 데이터는 아무것도 바뀌지 않았으므로
        // SaveRequired는 항상 false다.
        public static BattleActionResult Reject(
            string commandId,
            string log)
        {
            return new BattleActionResult(
                commandId,
                false,
                new[] { log },
                Array.Empty<BattleDamageChange>(),
                Array.Empty<BattleParticipant>(),
                false,
                null);
        }

        public static BattleActionResult Accept(
            string commandId,
            IReadOnlyList<string> logs,
            IReadOnlyList<BattleDamageChange> damageChanges,
            IReadOnlyList<BattleParticipant> removedParticipants,
            bool saveRequired,
            BattleResult battleEndResult)
        {
            return new BattleActionResult(
                commandId,
                true,
                logs,
                damageChanges,
                removedParticipants,
                saveRequired,
                battleEndResult);
        }
    }
}
