using ProjectDelta.Data;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 113일차: NPC 메뉴 선택을 화면 코드와 분리해 결과→복귀 흐름을 공통 결과로 만든다.
    public sealed class NpcInteractionService
    {
        public NpcInteractionResult Resolve(
            NpcDefinition definition,
            NpcRelationshipState relationship,
            NpcInteractionCommand command)
        {
            if (definition == null
                || relationship == null)
            {
                return new NpcInteractionResult(
                    NpcInteractionResultType.ReturnToExploration,
                    "NPC 정보를 불러오지 못했습니다.");
            }

            switch (command)
            {
                case NpcInteractionCommand.Talk:
                    return new NpcInteractionResult(
                        NpcInteractionResultType.ContinueInteraction,
                        $"{definition.DisplayName}과(와) 짧게 대화했습니다. 관계 단계: {relationship.Stage}");

                case NpcInteractionCommand.Service:
                    if (definition.ServiceTypes == NpcServiceType.None)
                    {
                        return new NpcInteractionResult(
                            NpcInteractionResultType.ContinueInteraction,
                            "현재 제공 가능한 서비스가 없습니다.");
                    }

                    return new NpcInteractionResult(
                        NpcInteractionResultType.OpenService,
                        "이용할 서비스를 선택하세요.");

                case NpcInteractionCommand.Leave:
                default:
                    return new NpcInteractionResult(
                        NpcInteractionResultType.ReturnToExploration,
                        "NPC 상호작용을 종료하고 탐험으로 돌아갑니다.");
            }
        }
    }
}
