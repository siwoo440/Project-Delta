using System;
using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    public sealed class RelicInstanceState
    {
        public string RelicId { get; }

        public string DisplayName { get; }

        // 104일차: 저주 유물 여부. 효과 설명과 함께 UI에 항상 그대로 공개한다.
        public bool IsCursed { get; }

        public RelicInstanceState(
            string relicId,
            string displayName,
            bool isCursed)
        {
            RelicId =
                relicId
                ?? string.Empty;

            DisplayName =
                string.IsNullOrEmpty(
                    displayName)
                    ? RelicId
                    : displayName;

            IsCursed =
                isCursed;
        }
    }

    // 104일차: 유물은 장비처럼 슬롯에 장착하는 게 아니라 "보유 목록"으로 관리되고,
    // 획득 즉시 패시브가 적용된다는 전제로 설계했다 - 그래서 해제 개념이 없다.
    // 인벤토리와도 분리된, 장비 슬롯과 독립된 별도 보유 구조다.
    public sealed class RelicRunState
    {
        public const int DefaultMaxCapacity = 5;

        private readonly List<RelicInstanceState> relics =
            new List<RelicInstanceState>();

        public int MaxCapacity { get; private set; } =
            DefaultMaxCapacity;

        public IReadOnlyList<RelicInstanceState> Relics =>
            relics;

        public bool IsFull =>
            relics.Count >= MaxCapacity;

        public bool HasRelic(
            string relicId)
        {
            if (string.IsNullOrEmpty(
                    relicId))
            {
                return false;
            }

            foreach (RelicInstanceState relic in relics)
            {
                if (relic != null
                    && relic.RelicId == relicId)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool AddRelic(
            RelicInstanceState relic)
        {
            if (relic == null
                || IsFull
                || HasRelic(
                    relic.RelicId))
            {
                return false;
            }

            relics.Add(
                relic);

            return true;
        }

        // 기타 영구 강화(139일차 예정)로 최대 보유 수가 늘어날 수 있어 미리 열어둔다.
        public void SetMaxCapacity(
            int maxCapacity)
        {
            MaxCapacity =
                Math.Max(
                    1,
                    maxCapacity);
        }

        // 세이브/로드 복원용. 중복 ID·용량 초과분은 조용히 무시한다.
        public void RestoreFrom(
            IEnumerable<RelicInstanceState> restored)
        {
            relics.Clear();

            if (restored == null)
            {
                return;
            }

            foreach (RelicInstanceState relic in restored)
            {
                if (relic != null
                    && !HasRelic(
                        relic.RelicId)
                    && relics.Count < MaxCapacity)
                {
                    relics.Add(
                        relic);
                }
            }
        }
    }
}
