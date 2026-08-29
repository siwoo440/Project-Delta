namespace ProjectDelta.Domain // 도메인 규칙 네임스페이스
{
    public static class DungeonMinimapContentGlyphRules // 지도 구성요소 문자 규칙
    {
        public static string GetGlyph( // 구성요소 종류별 지도 문자 반환
            RoomContentType contentType) // 구성요소 종류
        {
            switch (contentType) // 구성요소 종류 분기
            {
                case RoomContentType.Stairs: // 계단 종류 확인
                    return "S"; // 계단 문자 반환

                case RoomContentType.Chest: // 상자 종류 확인
                    return "C"; // 상자 문자 반환

                case RoomContentType.SecretWall: // 비밀벽 종류 확인
                    return "W"; // 비밀벽 문자 반환

                case RoomContentType.NpcPoint: // NPC 종류 확인
                    return "N"; // NPC 문자 반환

                case RoomContentType.AmbientProp: // 환경 요소 종류 확인
                    return "A"; // 환경 요소 문자 반환

                case RoomContentType.Monster: // 몬스터 종류 확인
                    return "M"; // 몬스터 문자 반환

                default: // 알 수 없는 종류 처리
                    return "?"; // 미정 문자 반환
            }
        }
    }
}
