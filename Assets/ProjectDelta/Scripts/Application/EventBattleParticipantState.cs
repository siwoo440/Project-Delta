using System.Collections.Generic;
using ProjectDelta.Domain;

namespace ProjectDelta.Application
{
    // 119일차: "다수 참가자(최대 3명), 개별 게이지" 요구사항 - 예전엔 EventBattleContext가
    // 대상 하나(Target)와 호감도 하나를 직접 들고 있었지만, 이제 대상마다 이 상태 객체를
    // 하나씩 갖는다. 상위 개체(보스) 2단계 게이지도 같은 구조로 표현한다 - StageCount가
    // 2 이상이면 한 단계를 다 채운 뒤 게이지가 비워지고 다음 단계로 넘어간다.
    public sealed class EventBattleParticipantState
    {
        public BattleParticipant Participant { get; }

        // 119일차: 만족 이탈 판정 기준 - 이 값 이상이면 그 대상의 턴마다 만족하고 떠날 수 있다.
        public const int SatisfiedFavorThreshold = 60;

        public int StageCount { get; }

        public int CurrentStage { get; private set; } =
            1;

        public int Favor { get; private set; }

        public bool HasLeftSatisfied { get; private set; }

        public bool HasWon { get; private set; }

        public EventBattleParticipantState(
            BattleParticipant participant,
            int stageCount = 1)
        {
            Participant =
                participant;

            StageCount =
                stageCount < 1
                    ? 1
                    : stageCount;
        }

        // 119일차: 만족해서 떠났거나 이미 승리(공략 완료)한 대상은 더 이상 행동의 대상이 될 수 없다.
        public bool IsActive =>
            !HasLeftSatisfied
            && !HasWon;

        public void AddFavor(
            int amount)
        {
            if (!IsActive)
            {
                return;
            }

            int previousFavor =
                Favor;

            int newFavor =
                Favor
                + amount;

            if (newFavor < 0)
            {
                newFavor =
                    0;
            }

            if (newFavor >= EventBattleContext.FavorToWin)
            {
                if (CurrentStage < StageCount)
                {
                    // 118일차 EventBattleContext.FavorToWin 100 도달 - 다음 단계로 넘어가고
                    // 게이지를 다시 채운다(보스 2단계 게이지).
                    CheckCgUnlocks(
                        previousFavor,
                        EventBattleContext.FavorToWin);

                    CurrentStage++;

                    Favor =
                        0;

                    return;
                }

                Favor =
                    EventBattleContext.FavorToWin;

                HasWon =
                    true;

                CheckCgUnlocks(
                    previousFavor,
                    Favor);

                // 132일차: 기획서 7.3절 "몬스터 개별 엔딩" - 이번 회차에 이 종족과
                // 호감도 100(FavorToWin)을 찍었다는 사실을 기록해둔다. 몬스터 호감도는
                // 이번 회차에만 유지되므로(NPC와 달리 프로필에 영구 저장하지 않는다),
                // RunContext(회차 상태)에만 남긴다.
                RunContext.Current?.Characters.MarkMonsterAffinityMaxed(
                    Participant.DefinitionId);

                return;
            }

            Favor =
                newFavor;

            CheckCgUnlocks(
                previousFavor,
                Favor);
        }

        // 133일차: 기획서 7.4절 "몬스터 관계 이벤트 CG - 호감도 20~100 단계별" - 이번
        // 상승으로 새로 넘긴 구간이 있으면 영구 기록에 해금 표시한다.
        private void CheckCgUnlocks(
            int previousFavor,
            int newFavor)
        {
            if (ApplicationFlow.Current == null)
            {
                return;
            }

            List<string> newlyUnlocked =
                MonsterCgRule.GetNewlyUnlockedCgIds(
                    Participant.DefinitionId,
                    previousFavor,
                    newFavor);

            for (int i = 0; i < newlyUnlocked.Count; i++)
            {
                ApplicationFlow.Current.UnlockCg(
                    newlyUnlocked[i]);
            }
        }

        public void MarkSatisfiedDeparture()
        {
            HasLeftSatisfied =
                true;
        }
    }
}
