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
