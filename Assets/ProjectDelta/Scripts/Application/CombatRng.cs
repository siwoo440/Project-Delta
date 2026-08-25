using System;

namespace ProjectDelta.Application
{
    // 59일차: 기획서 9.3 난수 분리 표 "CombatRng - 명중·피해·상태 이상, 저장" 전용 난수 발생원.
    // ExplorationMonsterEncounterController가 여기서 hitRoll·varianceRoll 등을 뽑아
    // BattleDamageCalculator에 넘기고, 더 이상 UnityEngine.Random을 핵심 판정에 직접 쓰지 않는다.
    //
    // "저장"은 아직 연결하지 않았다 - 전투 상태 자체가 RunData에 저장되지 않아
    // (9.3 자동 저장 파이프라인 미구현) 시드를 되돌려 복원할 대상이 없다. 저장이 붙는 일차에서
    // 시드를 노출하는 CaptureState 계열 메서드를 추가해야 한다.
    public sealed class CombatRng : IRandomSource
    {
        private readonly Random random;

        public CombatRng(
            int seed)
        {
            random =
                new Random(seed);
        }

        // 시드를 지정하지 않으면 매 전투가 실제로 무작위로 진행된다 (테스트가 아닌 실제 플레이용).
        public CombatRng()
            : this(
                Environment.TickCount)
        {
        }

        public int NextInt(
            int minInclusive,
            int maxExclusive)
        {
            return random.Next(
                minInclusive,
                maxExclusive);
        }
    }
}
