using System.Collections.Generic; // List 사용
using System.Reflection; // private 필드 설정용 리플렉션
using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // MonsterGroupCompositionService·BattleContext 사용
using ProjectDelta.Data; // MonsterDefinition·EncounterDefinition·MonsterRarity 사용
using UnityEngine; // ScriptableObject 사용
using Object = UnityEngine.Object; // Object.DestroyImmediate 명확화

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class MonsterGroupCompositionServiceTests
    {
        // RoomEncounterPlacementTests와 같은 방식으로, Definition ScriptableObject는 공개
        // 생성자가 없어 CreateInstance + private 필드 리플렉션으로 테스트용 데이터를 만든다.
        private readonly List<Object> createdObjects =
            new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < createdObjects.Count; index++)
            {
                if (createdObjects[index] != null)
                {
                    Object.DestroyImmediate(
                        createdObjects[index]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void Build_FixedGroupSizeWithoutPool_FillsAllSlotsWithPrimaryMonster()
        {
            MonsterDefinition primary =
                CreateMonster(
                    "MON_GOBLIN",
                    MonsterRarity.Normal);

            EncounterDefinition encounter =
                CreateEncounter(
                    "ENC_GOBLIN",
                    primary,
                    additionalPool: null,
                    minGroupSize: 3,
                    maxGroupSize: 3);

            MonsterGroupCompositionService.Result result =
                MonsterGroupCompositionService.Build(
                    encounter,
                    12345,
                    "ROOM_A");

            Assert.AreEqual(
                3,
                result.Slots.Count);

            for (int index = 0; index < result.Slots.Count; index++)
            {
                Assert.AreSame(
                    primary,
                    result.Slots[index]); // 고블린이 있으면 나머지 자리도 고블린으로 채워짐 확인
            }

            Assert.AreSame(
                primary,
                result.Representative);
        }

        [Test]
        public void Build_GroupSizeNeverExceedsMaxEnemySlots()
        {
            MonsterDefinition primary =
                CreateMonster(
                    "MON_SWARM",
                    MonsterRarity.Normal);

            EncounterDefinition encounter =
                CreateEncounter(
                    "ENC_SWARM",
                    primary,
                    additionalPool: null,
                    minGroupSize: 10,
                    maxGroupSize: 10);

            MonsterGroupCompositionService.Result result =
                MonsterGroupCompositionService.Build(
                    encounter,
                    1,
                    "ROOM_A");

            Assert.AreEqual(
                BattleContext.MaxEnemySlots,
                result.Slots.Count); // 75일차에 확정한 최대 인원(4명)을 넘지 않음 확인
        }

        [Test]
        public void Build_SameSeedRoomAndEncounter_ProducesIdenticalComposition()
        {
            MonsterDefinition primary =
                CreateMonster(
                    "MON_GOBLIN",
                    MonsterRarity.Normal);

            MonsterDefinition rareCandidate =
                CreateMonster(
                    "MON_HOBGOBLIN",
                    MonsterRarity.Rare);

            EncounterDefinition encounter =
                CreateEncounter(
                    "ENC_GOBLIN",
                    primary,
                    additionalPool: new[] { (rareCandidate, 1) },
                    minGroupSize: 1,
                    maxGroupSize: 4);

            MonsterGroupCompositionService.Result first =
                MonsterGroupCompositionService.Build(
                    encounter,
                    777,
                    "ROOM_B");

            MonsterGroupCompositionService.Result second =
                MonsterGroupCompositionService.Build(
                    encounter,
                    777,
                    "ROOM_B");

            Assert.AreEqual(
                first.Slots.Count,
                second.Slots.Count);

            for (int index = 0; index < first.Slots.Count; index++)
            {
                Assert.AreSame(
                    first.Slots[index],
                    second.Slots[index]); // 같은 Seed·Room·Encounter면 항상 같은 구성 확인
            }
        }

        [Test]
        public void SelectRepresentative_HigherRarityWinsRegardlessOfSlotPosition()
        {
            MonsterDefinition normal =
                CreateMonster(
                    "MON_NORMAL",
                    MonsterRarity.Normal);

            MonsterDefinition boss =
                CreateMonster(
                    "MON_BOSS",
                    MonsterRarity.Boss);

            MonsterDefinition rare =
                CreateMonster(
                    "MON_RARE",
                    MonsterRarity.Rare);

            MonsterDefinition representative =
                MonsterGroupCompositionService.SelectRepresentative(
                    new[] { normal, boss, rare }); // 보스가 2번 자리여도 대표가 돼야 함

            Assert.AreSame(
                boss,
                representative);
        }

        [Test]
        public void SelectRepresentative_SameRarityTie_PicksEarliestSlot()
        {
            MonsterDefinition first =
                CreateMonster(
                    "MON_A",
                    MonsterRarity.Normal);

            MonsterDefinition second =
                CreateMonster(
                    "MON_B",
                    MonsterRarity.Normal);

            MonsterDefinition representative =
                MonsterGroupCompositionService.SelectRepresentative(
                    new[] { first, second });

            Assert.AreSame(
                first,
                representative); // 동률이면 1번 자리가 대표
        }

        [Test]
        public void SelectRepresentative_EmptyOrNullSlots_ReturnsNull()
        {
            Assert.IsNull(
                MonsterGroupCompositionService.SelectRepresentative(
                    null));

            Assert.IsNull(
                MonsterGroupCompositionService.SelectRepresentative(
                    new MonsterDefinition[0]));
        }

        private MonsterDefinition CreateMonster(
            string id,
            MonsterRarity rarity)
        {
            MonsterDefinition monster =
                ScriptableObject.CreateInstance<MonsterDefinition>();

            createdObjects.Add(
                monster);

            SetDefinitionId(
                monster,
                id);

            SetPrivateField(
                monster,
                "rarity",
                rarity);

            SetPrivateField(
                monster,
                "maxHp",
                10);

            return monster;
        }

        private EncounterDefinition CreateEncounter(
            string id,
            MonsterDefinition primaryMonster,
            (MonsterDefinition monster, int weight)[] additionalPool,
            int minGroupSize,
            int maxGroupSize)
        {
            EncounterDefinition encounter =
                ScriptableObject.CreateInstance<EncounterDefinition>();

            createdObjects.Add(
                encounter);

            SetDefinitionId(
                encounter,
                id);

            SetPrivateField(
                encounter,
                "monster",
                primaryMonster);

            SetPrivateField(
                encounter,
                "minGroupSize",
                minGroupSize);

            SetPrivateField(
                encounter,
                "maxGroupSize",
                maxGroupSize);

            if (additionalPool != null)
            {
                EncounterMonsterEntry[] entries =
                    new EncounterMonsterEntry[additionalPool.Length];

                for (int index = 0; index < additionalPool.Length; index++)
                {
                    entries[index] =
                        CreateMonsterEntry(
                            additionalPool[index].monster,
                            additionalPool[index].weight);
                }

                SetPrivateField(
                    encounter,
                    "additionalMonsterPool",
                    entries);
            }

            return encounter;
        }

        private static EncounterMonsterEntry CreateMonsterEntry(
            MonsterDefinition monster,
            int weight)
        {
            EncounterMonsterEntry entry =
                new EncounterMonsterEntry(); // 기본 public 생성자 사용 (일반 C# 클래스, ScriptableObject 아님)

            SetPrivateField(
                entry,
                "monster",
                monster);

            SetPrivateField(
                entry,
                "weight",
                weight);

            return entry;
        }

        private static void SetDefinitionId(
            DefinitionBase definition,
            string id)
        {
            FieldInfo field =
                typeof(DefinitionBase).GetField(
                    "id",
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.IsNotNull(
                field);

            field.SetValue(
                definition,
                id);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            Assert.IsNotNull(
                field,
                fieldName);

            field.SetValue(
                target,
                value);
        }
    }
}
