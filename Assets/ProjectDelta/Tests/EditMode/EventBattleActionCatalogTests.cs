using System.Collections.Generic;
using NUnit.Framework;
using ProjectDelta.Application;

namespace ProjectDelta.Tests.EditMode
{
    public sealed class EventBattleActionCatalogTests
    {
        [Test]
        public void All_ContainsTwelveActions()
        {
            Assert.AreEqual(
                12,
                EventBattleActionCatalog.All.Count);
        }

        [Test]
        public void All_EveryActionHasUniqueId()
        {
            HashSet<string> ids =
                new HashSet<string>();

            foreach (IEventBattleCommand action
                     in EventBattleActionCatalog.All)
            {
                Assert.IsFalse(
                    string.IsNullOrEmpty(
                        action.Id));

                Assert.IsTrue(
                    ids.Add(
                        action.Id),
                    $"중복된 Id: {action.Id}");
            }
        }

        [Test]
        public void All_EveryActionCostsExactlyOneResourceType()
        {
            // 118일차: Court/Soothe(117일차)를 포함해 모든 행동은 마나 또는 정력 중
            // 하나만 소모한다 - 동시 소모(원자적 처리 필요)를 피하기 위한 설계 제약.
            foreach (IEventBattleCommand action
                     in EventBattleActionCatalog.All)
            {
                bool exactlyOne =
                    (action.ManaCost > 0)
                    != (action.StaminaCost > 0);

                Assert.IsTrue(
                    exactlyOne,
                    $"{action.Id}는 마나/정력 중 정확히 하나만 소모해야 합니다.");
            }
        }
    }
}
