using System;
using System.Collections.Generic;
using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // 76일차: 인카운터 하나가 실제로 몇 마리의 어떤 몬스터로 구성되는지 결정론적으로 뽑는다.
    // 40일차 방 배치 굴림과 같은 원칙 - 같은 Seed·RoomId·EncounterId면 항상 같은 구성이
    // 나오므로, 던전을 다시 불러와도(저장/복원) 별도 데이터 저장 없이 그대로 재현된다.
    //
    // 대표 외형 판정 규칙(기획): 그룹 중 등급(Rarity)이 가장 높은 몬스터를 대표로 삼는다.
    // 등급이 같으면 더 앞 자리(슬롯 인덱스가 작은 쪽)를 대표로 삼는다.
    public static class MonsterGroupCompositionService
    {
        public sealed class Result
        {
            // 슬롯 0 = 1번 자리. encounter.Monster가 항상 0번에 들어간다.
            public IReadOnlyList<MonsterDefinition> Slots { get; }
            public MonsterDefinition Representative { get; }

            public Result(
                IReadOnlyList<MonsterDefinition> slots,
                MonsterDefinition representative)
            {
                Slots = slots;
                Representative = representative;
            }
        }

        public static Result Build(
            EncounterDefinition encounter,
            int dungeonSeed,
            string roomId)
        {
            if (encounter == null
                || encounter.Monster == null)
            {
                return new Result(
                    Array.Empty<MonsterDefinition>(),
                    null);
            }

            int groupSize =
                RollGroupSize(
                    encounter,
                    dungeonSeed,
                    roomId);

            List<MonsterDefinition> slots =
                new List<MonsterDefinition>(groupSize)
                {
                    encounter.Monster // 0번 슬롯은 항상 이 인카운터의 기본 몬스터
                };

            for (int slotIndex = 1; slotIndex < groupSize; slotIndex++)
            {
                slots.Add(
                    RollSlotMonster(
                        encounter,
                        dungeonSeed,
                        roomId,
                        slotIndex));
            }

            return new Result(
                slots,
                SelectRepresentative(
                    slots));
        }

        // 등급이 가장 높은 슬롯을 고른다. 동률이면 먼저 찾은(더 앞 자리) 슬롯을 그대로 유지한다 -
        // List를 앞에서부터 순회하므로 자연히 "동률 시 앞 자리 우선" 규칙이 지켜진다.
        public static MonsterDefinition SelectRepresentative(
            IReadOnlyList<MonsterDefinition> slots)
        {
            MonsterDefinition representative =
                null;

            if (slots == null)
            {
                return null;
            }

            for (int index = 0; index < slots.Count; index++)
            {
                MonsterDefinition candidate =
                    slots[index];

                if (candidate == null)
                {
                    continue;
                }

                if (representative == null
                    || candidate.Rarity > representative.Rarity)
                {
                    representative =
                        candidate;
                }
            }

            return representative;
        }

        private static int RollGroupSize(
            EncounterDefinition encounter,
            int dungeonSeed,
            string roomId)
        {
            int min =
                Math.Min(
                    encounter.MinGroupSize,
                    BattleContext.MaxEnemySlots);

            int max =
                Math.Min(
                    encounter.MaxGroupSize,
                    BattleContext.MaxEnemySlots); // 75일차에 확정한 적 최대 인원을 넘지 않게 고정

            if (max <= min)
            {
                return min; // 76일차 기본값(1~1)은 항상 1마리 - 기존 동작과 동일
            }

            int range =
                max - min + 1;

            uint hash =
                DeterministicRollHash.Compute(
                    dungeonSeed,
                    roomId,
                    encounter.Id,
                    "GroupSize");

            return min + (int)(hash % (uint)range);
        }

        private static MonsterDefinition RollSlotMonster(
            EncounterDefinition encounter,
            int dungeonSeed,
            string roomId,
            int slotIndex)
        {
            EncounterMonsterEntry[] pool =
                encounter.AdditionalMonsterPool;

            int totalWeight =
                0;

            if (pool != null)
            {
                for (int index = 0; index < pool.Length; index++)
                {
                    if (pool[index] != null
                        && pool[index].Monster != null)
                    {
                        totalWeight +=
                            pool[index].Weight;
                    }
                }
            }

            if (totalWeight <= 0)
            {
                return encounter.Monster; // 추가 후보가 없으면 같은 종으로 채운다 (기본 예시와 동일)
            }

            uint hash =
                DeterministicRollHash.Compute(
                    dungeonSeed,
                    roomId,
                    encounter.Id,
                    "Slot" + slotIndex.ToString());

            int roll =
                (int)(hash % (uint)totalWeight);

            int cumulativeWeight =
                0;

            for (int index = 0; index < pool.Length; index++)
            {
                if (pool[index] == null
                    || pool[index].Monster == null)
                {
                    continue;
                }

                cumulativeWeight +=
                    pool[index].Weight;

                if (roll < cumulativeWeight)
                {
                    return pool[index].Monster;
                }
            }

            return encounter.Monster; // 가중치 합산 오차 등 예외 상황을 위한 안전망
        }
    }
}
