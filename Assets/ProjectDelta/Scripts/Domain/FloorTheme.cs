namespace ProjectDelta.Domain
{
    // 123일차: 5개 층 회차의 층별 테마. 1층·5층은 고정이고 2~4층은 이 중 나머지 셋을
    // 무작위 순서로 돌아간다(FloorThemeSchedule). 지금은 데이터로만 구분한다 - 테마별
    // 방 프리팹/텍스처 같은 실제 미술 콘텐츠는 아직 없어서, 어떤 테마인지 HUD 문구로만
    // 확인할 수 있다.
    public enum FloorTheme
    {
        Cave = 0,
        Ruins = 1,
        Forest = 2,
        Crypt = 3,
        ThroneRoom = 4
    }

    public static class FloorThemeRules
    {
        public static string GetDisplayName(
            FloorTheme theme)
        {
            switch (theme)
            {
                case FloorTheme.Ruins:
                    return "폐허";

                case FloorTheme.Forest:
                    return "숲";

                case FloorTheme.Crypt:
                    return "무덤";

                case FloorTheme.ThroneRoom:
                    return "마왕성";

                default:
                    return "동굴";
            }
        }
    }
}
