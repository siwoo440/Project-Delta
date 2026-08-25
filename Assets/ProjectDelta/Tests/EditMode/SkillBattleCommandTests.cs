using System.Collections.Generic; // List 사용
using System.Reflection; // private 필드 설정용 리플렉션
using NUnit.Framework; // NUnit 테스트 사용
using ProjectDelta.Application; // SkillBattleCommand 사용
using ProjectDelta.Data; // SkillDefinition·SkillTargetType 사용
using UnityEngine; // ScriptableObject 사용
using Object = UnityEngine.Object; // Object.DestroyImmediate 명확화

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class SkillBattleCommandTests
    {
        // RoomEncounterPlacementTests와 같은 방식으로, Definition ScriptableObject는
        // 공개 생성자가 없어 CreateInstance + private 필드 리플렉션으로 테스트용 데이터를 만든다.
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
        public void Execute_EnemyTarget_ValidTargetAndEnoughResources_Accepts()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SK001",
                    "화염구",
                    SkillTargetType.Enemy,
                    manaCost: 10,
                    staminaCost: 0);

            BattleParticipant player =
                CreatePlayer(
                    maxMana: 10,
                    maxStamina: 0);

            BattleParticipant enemy =
                CreateEnemy();

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            SkillBattleCommand command =
                new SkillBattleCommand(
                    skill);

            BattleCommandResult result =
                command.Execute(
                    context,
                    player,
                    enemy);

            Assert.IsTrue(
                result.Accepted);

            Assert.AreEqual(
                "SK001",
                command.Id);
        }

        [Test]
        public void Execute_EnemyTarget_InvalidTarget_Rejects()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SK001",
                    "화염구",
                    SkillTargetType.Enemy,
                    manaCost: 0,
                    staminaCost: 0);

            BattleParticipant player =
                CreatePlayer(
                    maxMana: 0,
                    maxStamina: 0);

            BattleParticipant enemy =
                CreateEnemy();

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            SkillBattleCommand command =
                new SkillBattleCommand(
                    skill);

            // 아군(자기 자신)을 대상으로 지정 - Enemy 대상 스킬이라 거부돼야 함
            BattleCommandResult result =
                command.Execute(
                    context,
                    player,
                    player);

            Assert.IsFalse(
                result.Accepted);
        }

        [Test]
        public void Execute_EnemyTarget_NullTarget_Rejects()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SK001",
                    "화염구",
                    SkillTargetType.Enemy,
                    manaCost: 0,
                    staminaCost: 0);

            BattleParticipant player =
                CreatePlayer(
                    maxMana: 0,
                    maxStamina: 0);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            SkillBattleCommand command =
                new SkillBattleCommand(
                    skill);

            BattleCommandResult result =
                command.Execute(
                    context,
                    player,
                    null);

            Assert.IsFalse(
                result.Accepted);
        }

        [Test]
        public void Execute_InsufficientMana_Rejects()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SK001",
                    "화염구",
                    SkillTargetType.Enemy,
                    manaCost: 20,
                    staminaCost: 0);

            BattleParticipant player =
                CreatePlayer(
                    maxMana: 10, // 필요한 20보다 적음
                    maxStamina: 0);

            BattleParticipant enemy =
                CreateEnemy();

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            SkillBattleCommand command =
                new SkillBattleCommand(
                    skill);

            BattleCommandResult result =
                command.Execute(
                    context,
                    player,
                    enemy);

            Assert.IsFalse(
                result.Accepted);

            Assert.AreEqual(
                10,
                player.CurrentMana); // Execute()는 판정만 하고 실제로 깎지 않음 확인
        }

        [Test]
        public void Execute_InsufficientStamina_Rejects()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SK001",
                    "필살기",
                    SkillTargetType.Enemy,
                    manaCost: 0,
                    staminaCost: 30);

            BattleParticipant player =
                CreatePlayer(
                    maxMana: 0,
                    maxStamina: 10); // 필요한 30보다 적음

            BattleParticipant enemy =
                CreateEnemy();

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { enemy });

            SkillBattleCommand command =
                new SkillBattleCommand(
                    skill);

            BattleCommandResult result =
                command.Execute(
                    context,
                    player,
                    enemy);

            Assert.IsFalse(
                result.Accepted);
        }

        [Test]
        public void Execute_SelfTarget_NoTargetRequired_Accepts()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SK002",
                    "방어 강화",
                    SkillTargetType.Self,
                    manaCost: 5,
                    staminaCost: 0);

            BattleParticipant player =
                CreatePlayer(
                    maxMana: 5,
                    maxStamina: 0);

            BattleContext context =
                new BattleContext(
                    player,
                    new[] { CreateEnemy() });

            SkillBattleCommand command =
                new SkillBattleCommand(
                    skill);

            // Self 대상 스킬은 target 없이도(null) 수락돼야 한다 - DefendBattleCommand와 동일한 원칙
            BattleCommandResult result =
                command.Execute(
                    context,
                    player,
                    null);

            Assert.IsTrue(
                result.Accepted);
        }

        [Test]
        public void Execute_NullSkill_RejectsWithoutThrowing()
        {
            SkillBattleCommand command =
                new SkillBattleCommand(
                    null);

            BattleContext context =
                new BattleContext(
                    CreatePlayer(
                        maxMana: 0,
                        maxStamina: 0),
                    new[] { CreateEnemy() });

            Assert.DoesNotThrow(
                () =>
                {
                    BattleCommandResult result =
                        command.Execute(
                            context,
                            context.Player,
                            context.Enemies[0]);

                    Assert.IsFalse(
                        result.Accepted);
                });
        }

        [Test]
        public void Execute_NullContext_Rejects()
        {
            SkillDefinition skill =
                CreateSkill(
                    "SK001",
                    "화염구",
                    SkillTargetType.Enemy,
                    manaCost: 0,
                    staminaCost: 0);

            SkillBattleCommand command =
                new SkillBattleCommand(
                    skill);

            BattleCommandResult result =
                command.Execute(
                    null,
                    CreatePlayer(
                        maxMana: 0,
                        maxStamina: 0),
                    CreateEnemy());

            Assert.IsFalse(
                result.Accepted);
        }

        private SkillDefinition CreateSkill(
            string id,
            string displayName,
            SkillTargetType targetType,
            int manaCost,
            int staminaCost)
        {
            SkillDefinition skill =
                ScriptableObject.CreateInstance<SkillDefinition>();

            createdObjects.Add(
                skill);

            SetDefinitionId(
                skill,
                id);

            SetPrivateField(
                skill,
                "displayName",
                displayName);

            SetPrivateField(
                skill,
                "targetType",
                targetType);

            SetPrivateField(
                skill,
                "manaCost",
                manaCost);

            SetPrivateField(
                skill,
                "staminaCost",
                staminaCost);

            return skill;
        }

        private static BattleParticipant CreatePlayer(
            int maxMana,
            int maxStamina)
        {
            return new BattleParticipant(
                "PLAYER",
                "PLAYER",
                BattleTeam.Player,
                20,
                5,
                6,
                3,
                90,
                10,
                0,
                0,
                maxMana,
                maxStamina);
        }

        private static BattleParticipant CreateEnemy()
        {
            return new BattleParticipant(
                "MON_TEST",
                "MON_TEST",
                BattleTeam.Enemy,
                10,
                5,
                4,
                2,
                80,
                5,
                0);
        }

        private static void SetDefinitionId(
            ProjectDelta.Data.DefinitionBase definition,
            string id)
        {
            FieldInfo field =
                typeof(ProjectDelta.Data.DefinitionBase).GetField(
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
