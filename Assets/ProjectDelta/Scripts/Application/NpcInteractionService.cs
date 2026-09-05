using System.Collections.Generic;
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

        // 115일차: 아이템을 선물하고 호감도를 올린다. 인벤토리에서 실제로 아이템을
        // 빼는 것은 Presentation이 담당하고(어떤 슬롯을 골랐는지는 화면의 몫이다),
        // 여기서는 관계 변화만 처리한다.
        public NpcInteractionResult ResolveGift(
            NpcRelationshipState relationship,
            string itemDisplayName,
            int affinityGain)
        {
            if (relationship == null)
            {
                return new NpcInteractionResult(
                    NpcInteractionResultType.ContinueInteraction,
                    "NPC 정보를 불러오지 못했습니다.");
            }

            int previousAffinity =
                relationship.Affinity;

            relationship.ChangeAffinity(
                affinityGain);

            UnlockNpcCgIfThresholdCrossed(
                relationship.NpcId,
                previousAffinity,
                relationship.Affinity);

            return new NpcInteractionResult(
                NpcInteractionResultType.ContinueInteraction,
                $"{itemDisplayName}을(를) 선물했습니다. (호감도 +{affinityGain})");
        }

        // 133일차: 기획서 7.4절 "NPC 관계 이벤트 CG" - 호감도가 오를 때마다 이번에
        // 새로 넘긴 단계가 있는지 확인해 해금한다(선물·구조 등 호감도가 오르는 모든
        // 경로에서 공통으로 호출한다).
        private static void UnlockNpcCgIfThresholdCrossed(
            string npcId,
            int previousAffinity,
            int newAffinity)
        {
            if (ApplicationFlow.Current == null)
            {
                return;
            }

            List<string> newlyUnlocked =
                NpcCgRule.GetNewlyUnlockedCgIds(
                    npcId,
                    previousAffinity,
                    newAffinity);

            for (int i = 0; i < newlyUnlocked.Count; i++)
            {
                ApplicationFlow.Current.UnlockCg(
                    newlyUnlocked[i]);
            }
        }

        // 115일차: NPC 한 명당 한 번만 가능한 "구조" - 도움을 주고 큰 폭으로 호감도를 올린다.
        public NpcInteractionResult ResolveRescue(
            NpcRelationshipState relationship,
            int affinityGain)
        {
            if (relationship == null)
            {
                return new NpcInteractionResult(
                    NpcInteractionResultType.ContinueInteraction,
                    "NPC 정보를 불러오지 못했습니다.");
            }

            if (relationship.HasBeenRescued)
            {
                return new NpcInteractionResult(
                    NpcInteractionResultType.ContinueInteraction,
                    "이미 도움을 준 적이 있습니다.");
            }

            int previousAffinity =
                relationship.Affinity;

            relationship.ChangeAffinity(
                affinityGain);

            relationship.MarkRescued();

            UnlockNpcCgIfThresholdCrossed(
                relationship.NpcId,
                previousAffinity,
                relationship.Affinity);

            return new NpcInteractionResult(
                NpcInteractionResultType.ContinueInteraction,
                $"위험에서 도와주었습니다. (호감도 +{affinityGain})");
        }

        // 115일차: 공격/약탈/배신 - 전부 같은 결과(적대 전환 + 전투 시작)로 이어진다.
        public NpcInteractionResult ResolveAttack(
            NpcDefinition definition,
            NpcRelationshipState relationship)
        {
            if (definition == null
                || relationship == null
                || !definition.CanBattle)
            {
                return new NpcInteractionResult(
                    NpcInteractionResultType.ContinueInteraction,
                    "지금은 공격할 수 없습니다.");
            }

            relationship.SetHostile(
                true);

            return new NpcInteractionResult(
                NpcInteractionResultType.StartBattle,
                $"{definition.DisplayName}과(와) 적대 관계가 되었습니다.");
        }
    }
}
