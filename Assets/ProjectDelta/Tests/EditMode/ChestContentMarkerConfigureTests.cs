using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectDelta.Presentation;

namespace ProjectDelta.Tests.EditMode
{
    // 112일차: 절차 생성 상자가 코드로 내용물을 지정할 수 있도록 추가한 Configure 메서드의
    // 시그니처만 확인한다. 실제 동작(RoomInstance 연동)은 RoomPassageController 계층
    // 구조가 있는 실제 Scene에서만 재현 가능해 EditMode에서는 검증하지 않는다.
    public sealed class ChestContentMarkerConfigureTests
    {
        [Test]
        public void Configure_AcceptsEnumerableOfString()
        {
            MethodInfo configure =
                typeof(ChestContentMarker).GetMethod(
                    "Configure",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(IEnumerable<string>) },
                    null);

            Assert.That(
                configure,
                Is.Not.Null);
        }
    }
}
