using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class RoomEncounterPlacementTests
    {
        private readonly List<Object> createdObjects =
            new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void Build_ExcludesEntryAndStairs_AndAssignsAtMostOneEncounterPerRoom()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(5);

            EncounterDefinition encounter =
                CreateEncounter(
                    "ENC_TEST",
                    "MON_TEST",
                    1f,
                    true);

            DungeonEncounterLayout layout =
                new RoomEncounterPlacementService().Build(
                    dungeon,
                    40001,
                    encounter);

            Assert.AreEqual(3, layout.Assignments.Count);
            Assert.IsFalse(
                layout.TryGet(
                    dungeon.EntryRoom.RoomId,
                    out _));
            Assert.IsFalse(
                layout.TryGet(
                    dungeon.StairsRoom.RoomId,
                    out _));

            foreach (RoomEncounterAssignment assignment
                     in layout.Assignments)
            {
                Assert.AreEqual(
                    RoomContentType.Monster,
                    assignment.ContentType);

                Assert.AreEqual(
                    "ENC_TEST",
                    assignment.EncounterDefinitionId);

                Assert.AreEqual(
                    "MON_TEST",
                    assignment.MonsterDefinitionId);
            }

            Assert.AreEqual(
                layout.Assignments.Count,
                layout.Assignments
                    .Select(item => item.RoomId)
                    .Distinct()
                    .Count());
        }

        [Test]
        public void Build_SpecialRoomCandidate_IsReservedFromMonsterEncounter()
        {
            DungeonLayoutGraph graph =
                new DungeonLayoutGraph();

            RoomNode[] rooms =
                new RoomNode[5];

            for (int i = 0; i < rooms.Length; i++)
            {
                rooms[i] =
                    graph.AddRoom(
                        $"ROOM_{i:00}",
                        "ROOM_TEST",
                        new GridPosition(i, 0));

                if (i > 0)
                {
                    graph.Connect(
                        rooms[i - 1],
                        CardinalDirection.East,
                        rooms[i]);
                }
            }

            GeneratedDungeon dungeon =
                new GeneratedDungeon(
                    graph,
                    rooms[0],
                    rooms[4],
                    new RoomNode[0],
                    new RoomNode[0],
                    new RoomNode[0],
                    new[]
                    {
                        rooms[2]
                    },
                    0,
                    5,
                    null);

            EncounterDefinition encounter =
                CreateEncounter(
                    "ENC_TEST",
                    "MON_TEST",
                    1f,
                    true);

            DungeonEncounterLayout layout =
                new RoomEncounterPlacementService().Build(
                    dungeon,
                    40001,
                    encounter);

            Assert.IsFalse(
                layout.TryGet(
                    rooms[2].RoomId,
                    out _));

            Assert.AreEqual(
                2,
                layout.Assignments.Count);
        }

        [Test]
        public void Build_ExplicitExcludedRoom_IsNotAssigned()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(5);

            EncounterDefinition encounter =
                CreateEncounter(
                    "ENC_TEST",
                    "MON_TEST",
                    1f,
                    true);

            string excludedRoomId =
                dungeon.Layout.AllRooms
                    .First(room =>
                        room.RoomId != dungeon.EntryRoom.RoomId
                        && room.RoomId != dungeon.StairsRoom.RoomId)
                    .RoomId;

            DungeonEncounterLayout layout =
                new RoomEncounterPlacementService().Build(
                    dungeon,
                    40001,
                    encounter,
                    new[]
                    {
                        excludedRoomId
                    });

            Assert.IsFalse(
                layout.TryGet(
                    excludedRoomId,
                    out _));

            Assert.AreEqual(2, layout.Assignments.Count);
        }

        [Test]
        public void Build_SameSeedAndSameRooms_ProducesSameAssignments()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(12);

            EncounterDefinition encounter =
                CreateEncounter(
                    "ENC_TEST",
                    "MON_TEST",
                    0.5f,
                    true);

            RoomEncounterPlacementService service =
                new RoomEncounterPlacementService();

            string[] first =
                service.Build(
                        dungeon,
                        40123,
                        encounter)
                    .Assignments
                    .Select(item => item.RoomId)
                    .OrderBy(roomId => roomId)
                    .ToArray();

            string[] second =
                service.Build(
                        dungeon,
                        40123,
                        encounter)
                    .Assignments
                    .Select(item => item.RoomId)
                    .OrderBy(roomId => roomId)
                    .ToArray();

            CollectionAssert.AreEqual(
                first,
                second);
        }

        [Test]
        public void Build_ZeroChanceOrDisabledDefinition_ProducesNoAssignments()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(6);

            EncounterDefinition zeroChance =
                CreateEncounter(
                    "ENC_ZERO",
                    "MON_TEST",
                    0f,
                    true);

            EncounterDefinition disabled =
                CreateEncounter(
                    "ENC_DISABLED",
                    "MON_TEST",
                    1f,
                    false);

            RoomEncounterPlacementService service =
                new RoomEncounterPlacementService();

            Assert.AreEqual(
                0,
                service.Build(
                        dungeon,
                        40001,
                        zeroChance)
                    .Assignments
                    .Count);

            Assert.AreEqual(
                0,
                service.Build(
                        dungeon,
                        40001,
                        disabled)
                    .Assignments
                    .Count);
        }

        [Test]
        public void Build_NullOrInvalidEncounter_ProducesNoAssignments()
        {
            GeneratedDungeon dungeon =
                CreateLinearDungeon(6);

            EncounterDefinition missingMonster =
                ScriptableObject.CreateInstance<EncounterDefinition>();

            createdObjects.Add(missingMonster);

            SetDefinitionId(
                missingMonster,
                "ENC_INVALID");

            SetPrivateField(
                missingMonster,
                "roomSpawnChance",
                1f);

            SetPrivateField(
                missingMonster,
                "enabled",
                true);

            RoomEncounterPlacementService service =
                new RoomEncounterPlacementService();

            Assert.AreEqual(
                0,
                service.Build(
                        dungeon,
                        40001,
                        null)
                    .Assignments
                    .Count);

            Assert.AreEqual(
                0,
                service.Build(
                        dungeon,
                        40001,
                        missingMonster)
                    .Assignments
                    .Count);
        }

        [Test]
        public void Build_FixedGroupSize_FillsMonsterDefinitionIdsForEverySlot()
        {
            // 76일차: 그룹 마리 수 설정이 실제 배치 결과(MonsterDefinitionIds)에 반영되는지 확인한다.
            GeneratedDungeon dungeon =
                CreateLinearDungeon(5);

            EncounterDefinition encounter =
                CreateEncounter(
                    "ENC_TEST",
                    "MON_TEST",
                    1f,
                    true);

            SetPrivateField(
                encounter,
                "minGroupSize",
                3);

            SetPrivateField(
                encounter,
                "maxGroupSize",
                3);

            DungeonEncounterLayout layout =
                new RoomEncounterPlacementService().Build(
                    dungeon,
                    40001,
                    encounter);

            Assert.Greater(
                layout.Assignments.Count,
                0);

            foreach (RoomEncounterAssignment assignment
                     in layout.Assignments)
            {
                Assert.AreEqual(
                    3,
                    assignment.MonsterDefinitionIds.Count);

                foreach (string monsterDefinitionId in assignment.MonsterDefinitionIds)
                {
                    Assert.AreEqual(
                        "MON_TEST",
                        monsterDefinitionId); // 추가 후보 풀이 없으면 전부 같은 종으로 채워짐

                }

                Assert.AreEqual(
                    "MON_TEST",
                    assignment.MonsterDefinitionId); // 대표도 같은 종
            }
        }

        [Test]
        public void BuildForFloor_EncounterOutsideFloorRange_IsExcluded()
        {
            // 78일차: minFloor·maxFloor로 층을 제한한 인카운터는 그 범위 밖 층에서 아예
            // 배정 대상에서 빠져야 한다 - 스폰 확률 1이어도 마찬가지다.
            GeneratedDungeon dungeon =
                CreateLinearDungeon(5);

            EncounterDefinition floorTwoOnly =
                CreateEncounter(
                    "ENC_FLOOR_TWO",
                    "MON_TEST",
                    1f,
                    true);

            SetPrivateField(
                floorTwoOnly,
                "minFloor",
                2);

            SetPrivateField(
                floorTwoOnly,
                "maxFloor",
                2);

            DungeonEncounterLayout layoutOnFloorOne =
                new RoomEncounterPlacementService().BuildForFloor(
                    dungeon,
                    40001,
                    1,
                    new[] { floorTwoOnly });

            Assert.AreEqual(
                0,
                layoutOnFloorOne.Assignments.Count);

            DungeonEncounterLayout layoutOnFloorTwo =
                new RoomEncounterPlacementService().BuildForFloor(
                    dungeon,
                    40001,
                    2,
                    new[] { floorTwoOnly });

            Assert.Greater(
                layoutOnFloorTwo.Assignments.Count,
                0);
        }

        [Test]
        public void BuildForFloor_DefaultFloorRange_AllowsEveryFloor()
        {
            // 78일차: 배치가 정해지지 않은 몬스터(기본값 minFloor=1, maxFloor=-1)는
            // 모든 층에서 나와야 한다.
            GeneratedDungeon dungeon =
                CreateLinearDungeon(5);

            EncounterDefinition unrestricted =
                CreateEncounter(
                    "ENC_ANY_FLOOR",
                    "MON_TEST",
                    1f,
                    true);

            foreach (int floor in new[] { 1, 2, 3, 4, 99 })
            {
                DungeonEncounterLayout layout =
                    new RoomEncounterPlacementService().BuildForFloor(
                        dungeon,
                        40001,
                        floor,
                        new[] { unrestricted });

                Assert.Greater(
                    layout.Assignments.Count,
                    0,
                    $"floor {floor}");
            }
        }

        [Test]
        public void BuildForFloor_MultipleEncounters_NeverAssignSameRoomTwice()
        {
            // 78일차: 여러 인카운터가 같은 방을 놓고 경쟁하면, 먼저 처리된(Id 순서) 인카운터만
            // 그 방을 가져가고 나머지는 건너뛴다 - 한 방에 두 인카운터가 겹쳐 배정되면 안 된다.
            GeneratedDungeon dungeon =
                CreateLinearDungeon(8);

            EncounterDefinition first =
                CreateEncounter(
                    "ENC_A",
                    "MON_TEST",
                    1f,
                    true);

            EncounterDefinition second =
                CreateEncounter(
                    "ENC_B",
                    "MON_TEST",
                    1f,
                    true);

            DungeonEncounterLayout layout =
                new RoomEncounterPlacementService().BuildForFloor(
                    dungeon,
                    40001,
                    1,
                    new[] { first, second });

            HashSet<string> seenRoomIds =
                new HashSet<string>();

            foreach (RoomEncounterAssignment assignment in layout.Assignments)
            {
                Assert.IsTrue(
                    seenRoomIds.Add(assignment.RoomId)); // 중복 배정이면 Add가 false를 반환
            }
        }

        [Test]
        public void IsAllowedOnFloor_DefaultValues_AllowsAllFloorsFromOne()
        {
            EncounterDefinition encounter =
                CreateEncounter(
                    "ENC_DEFAULT",
                    "MON_TEST",
                    1f,
                    true);

            Assert.IsTrue(
                encounter.IsAllowedOnFloor(1));

            Assert.IsTrue(
                encounter.IsAllowedOnFloor(50));
        }

        [Test]
        public void IsAllowedOnFloor_RestrictedRange_RejectsOutsideRange()
        {
            EncounterDefinition encounter =
                CreateEncounter(
                    "ENC_RANGE",
                    "MON_TEST",
                    1f,
                    true);

            SetPrivateField(
                encounter,
                "minFloor",
                3);

            SetPrivateField(
                encounter,
                "maxFloor",
                4);

            Assert.IsFalse(
                encounter.IsAllowedOnFloor(2));

            Assert.IsTrue(
                encounter.IsAllowedOnFloor(3));

            Assert.IsTrue(
                encounter.IsAllowedOnFloor(4));

            Assert.IsFalse(
                encounter.IsAllowedOnFloor(5));
        }

        private EncounterDefinition CreateEncounter(
            string encounterId,
            string monsterId,
            float chance,
            bool enabled)
        {
            MonsterDefinition monster =
                ScriptableObject.CreateInstance<MonsterDefinition>();

            EncounterDefinition encounter =
                ScriptableObject.CreateInstance<EncounterDefinition>();

            createdObjects.Add(monster);
            createdObjects.Add(encounter);

            SetDefinitionId(
                monster,
                monsterId);

            SetDefinitionId(
                encounter,
                encounterId);

            SetPrivateField(
                encounter,
                "monster",
                monster);

            SetPrivateField(
                encounter,
                "roomSpawnChance",
                chance);

            SetPrivateField(
                encounter,
                "enabled",
                enabled);

            return encounter;
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

            Assert.IsNotNull(field);
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

        private static GeneratedDungeon CreateLinearDungeon(
            int roomCount)
        {
            DungeonLayoutGraph graph =
                new DungeonLayoutGraph();

            RoomNode[] rooms =
                new RoomNode[roomCount];

            for (int i = 0; i < roomCount; i++)
            {
                rooms[i] =
                    graph.AddRoom(
                        $"ROOM_{i:00}",
                        "ROOM_TEST",
                        new GridPosition(i, 0));

                if (i > 0)
                {
                    graph.Connect(
                        rooms[i - 1],
                        CardinalDirection.East,
                        rooms[i]);
                }
            }

            return new GeneratedDungeon(
                graph,
                rooms[0],
                rooms[roomCount - 1]);
        }
    }
}
