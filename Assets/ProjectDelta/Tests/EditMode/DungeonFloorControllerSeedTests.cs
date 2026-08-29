using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Presentation;
using UnityEngine;

namespace ProjectDelta.Tests.EditMode
{
    // 110일차: 새 게임마다 던전 Seed를 무작위로 다시 뽑는 스위치가 존재하는지 확인한다.
    // Awake()가 실제로 baseSeed를 무작위로 바꾸는지는 절차 생성 방 바인딩 등
    // 여러 SerializeField가 함께 필요해 PlayMode/통합 테스트 영역이라 여기서는 다루지 않는다.
    public sealed class DungeonFloorControllerSeedTests
    {
        [Test]
        public void RandomizeSeedEachRun_FieldExistsAsSerializedBool()
        {
            FieldInfo field =
                typeof(DungeonFloorController).GetField(
                    "randomizeSeedEachRun",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                "randomizeSeedEachRun 필드를 찾을 수 없습니다.");

            Assert.That(
                field.FieldType,
                Is.EqualTo(
                    typeof(bool)));

            Assert.That(
                field.GetCustomAttribute<SerializeField>(),
                Is.Not.Null,
                "randomizeSeedEachRun 필드가 [SerializeField]로 노출되어 있지 않습니다.");
        }

        [Test]
        public void BaseSeed_FieldStillExistsAsSerializedInt()
        {
            FieldInfo field =
                typeof(DungeonFloorController).GetField(
                    "baseSeed",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null);

            Assert.That(
                field.FieldType,
                Is.EqualTo(
                    typeof(int)));
        }
    }
}
