using NUnit.Framework; // Unity EditMode 테스트 기능 사용
using ProjectDelta.Domain; // 구성요소 문자 규칙 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class DungeonMinimapContentGlyphRulesTests // 지도 구성요소 문자 테스트
    {
        [TestCase(RoomContentType.Stairs, "S")] // 계단 문자 검사
        [TestCase(RoomContentType.Chest, "C")] // 상자 문자 검사
        [TestCase(RoomContentType.SecretWall, "W")] // 비밀벽 문자 검사
        [TestCase(RoomContentType.NpcPoint, "N")] // NPC 문자 검사
        [TestCase(RoomContentType.AmbientProp, "A")] // 환경 요소 문자 검사
        [TestCase(RoomContentType.Monster, "M")] // 몬스터 문자 검사
        public void GetGlyph_KnownContentType_ReturnsExpectedGlyph( // 구성요소 문자 매핑 검사
            RoomContentType contentType, // 검사할 구성요소 종류
            string expectedGlyph) // 기대 문자
        {
            string actualGlyph = // 실제 문자 조회
                DungeonMinimapContentGlyphRules.GetGlyph( // 문자 규칙 호출
                    contentType); // 구성요소 종류 전달

            Assert.That( // 결과 검증
                actualGlyph, // 실제 문자 전달
                Is.EqualTo(expectedGlyph)); // 기대 문자와 비교
        }
    }
}
