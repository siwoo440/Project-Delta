using System;

namespace ProjectDelta.Application
{
    // 61일차: 기획서 10.3 StatusEffectInstance — 실제로 적용된 상태 이상 한 건의 런타임 데이터.
    // 정의 데이터(StatusEffectDefinition)는 바꾸지 않고, 여기에 개별 적용 결과만 담는다.
    //
    // 문서 필드명은 "RemainingTurns"지만, 59일차에 정정한 대로 이 프로젝트에서 지속시간은
    // "라운드" 단위이므로(기획서 4.4 "약한 상태 1라운드" 등) RemainingRounds로 이름 붙였다.
    //
    // SourceInstanceId는 이 상태를 건 참가자의 InstanceId다. 지속 피해로 사망했을 때 "마지막
    // 공격자"를 기록하는 데 그대로 쓸 수 있다(기획서 4.2 "지속 피해로 체력이 0이 되면 해당
    // 상태를 부여한 캐릭터를 마지막 공격자로 기록한다") — 실제 연결은 71일차 패배 기록 작업 몫이다.
    public sealed class StatusEffectInstance
    {
        public string DefinitionId { get; }
        public string SourceInstanceId { get; }
        public int RemainingRounds { get; private set; }
        public int StackCount { get; private set; }
        public int AppliedValue { get; }

        public bool IsExpired =>
            RemainingRounds <= 0;

        public StatusEffectInstance(
            string definitionId,
            string sourceInstanceId,
            int remainingRounds,
            int stackCount,
            int appliedValue)
        {
            DefinitionId =
                definitionId;

            SourceInstanceId =
                sourceInstanceId;

            RemainingRounds =
                remainingRounds;

            StackCount =
                stackCount;

            AppliedValue =
                appliedValue;
        }

        // 60일차: 라운드 파이프라인의 "상태 지속 시간 감소" 단계에서 호출한다. 0 밑으로는
        // 내려가지 않는다.
        public void DecrementRemainingRounds()
        {
            RemainingRounds =
                Math.Max(
                    0,
                    RemainingRounds - 1);
        }
    }
}
