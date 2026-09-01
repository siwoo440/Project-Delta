using System;
using System.Collections.Generic;

namespace ProjectDelta.Data
{
    // 120일차: 별도 이벤트 전투 버전의 BattleEncounterCheckpointStore(82일차). 같은 원칙을
    // 따른다 - 저장 시점의 정보만 담고, 정확한 턴 단위 재현은 시도하지 않는다. 이벤트
    // 전투는 진행 중이던 일반 전투(BattleEncounterCheckpoint가 이미 따로 저장한다) 안에서
    // 시작되므로, 복원 시점엔 이 체크포인트를 비우고 일반 전투 체크포인트가 처음부터
    // 다시 진입시키게 한다 - 회유/유혇을 다시 시도해야 하지만, 저장 직전 자원·진행도는
    // 기록에 남아 사용자가 확인할 수 있다.
    [Serializable]
    public sealed class EventBattleCheckpointData
    {
        public bool IsPending;
        public string RoomId;
        public string SourceLabel;
        public int AttemptCount;
        public int PlayerManaAtCheckpoint;
        public int PlayerStaminaAtCheckpoint;
        public List<string> TargetDefinitionIds =
            new List<string>();
        public List<int> TargetFavors =
            new List<int>();
        public List<int> TargetStages =
            new List<int>();
    }

    public static class EventBattleCheckpointStore
    {
        private static EventBattleCheckpointData pending;

        public static bool HasPending =>
            pending != null
            && pending.IsPending;

        public static EventBattleCheckpointData Pending =>
            Clone(
                pending);

        public static void Capture(
            string roomId,
            string sourceLabel,
            int attemptCount,
            int playerMana,
            int playerStamina,
            IReadOnlyList<string> targetDefinitionIds,
            IReadOnlyList<int> targetFavors,
            IReadOnlyList<int> targetStages)
        {
            if (string.IsNullOrEmpty(
                    roomId))
            {
                Clear();
                return;
            }

            EventBattleCheckpointData data =
                new EventBattleCheckpointData
                {
                    IsPending = true,
                    RoomId = roomId,
                    SourceLabel = sourceLabel,
                    AttemptCount = attemptCount,
                    PlayerManaAtCheckpoint = playerMana,
                    PlayerStaminaAtCheckpoint = playerStamina
                };

            if (targetDefinitionIds != null)
            {
                data.TargetDefinitionIds.AddRange(
                    targetDefinitionIds);
            }

            if (targetFavors != null)
            {
                data.TargetFavors.AddRange(
                    targetFavors);
            }

            if (targetStages != null)
            {
                data.TargetStages.AddRange(
                    targetStages);
            }

            pending =
                data;
        }

        // 120일차: 저장된 회차를 이어할 때 호출한다 - 이벤트 전투 자체를 재현하지 않고,
        // 있었다는 사실과 저장 직전 수치만 로그로 남긴 뒤 비운다(일반 전투 체크포인트가
        // 같은 조우를 처음부터 다시 시작시킨다).
        public static void RestoreAndClear(
            EventBattleCheckpointData saved,
            Action<string> onRecovered = null)
        {
            if (saved == null
                || !saved.IsPending)
            {
                Clear();
                return;
            }

            onRecovered?.Invoke(
                $"이벤트 전투 진행 중 저장됨 / Room {saved.RoomId} / 시도 {saved.AttemptCount}회 / 저장 시점 MP {saved.PlayerManaAtCheckpoint} 정력 {saved.PlayerStaminaAtCheckpoint} - 처음부터 다시 시도합니다.");

            Clear();
        }

        public static void ApplyTo(
            RunData runData)
        {
            if (runData == null)
            {
                return;
            }

            runData.EventBattleCheckpoint =
                HasPending
                    ? Clone(pending)
                    : new EventBattleCheckpointData();
        }

        public static void Clear()
        {
            pending =
                null;
        }

        private static EventBattleCheckpointData Clone(
            EventBattleCheckpointData source)
        {
            if (source == null)
            {
                return null;
            }

            EventBattleCheckpointData copy =
                new EventBattleCheckpointData
                {
                    IsPending = source.IsPending,
                    RoomId = source.RoomId,
                    SourceLabel = source.SourceLabel,
                    AttemptCount = source.AttemptCount,
                    PlayerManaAtCheckpoint = source.PlayerManaAtCheckpoint,
                    PlayerStaminaAtCheckpoint = source.PlayerStaminaAtCheckpoint
                };

            if (source.TargetDefinitionIds != null)
            {
                copy.TargetDefinitionIds.AddRange(
                    source.TargetDefinitionIds);
            }

            if (source.TargetFavors != null)
            {
                copy.TargetFavors.AddRange(
                    source.TargetFavors);
            }

            if (source.TargetStages != null)
            {
                copy.TargetStages.AddRange(
                    source.TargetStages);
            }

            return copy;
        }
    }
}
