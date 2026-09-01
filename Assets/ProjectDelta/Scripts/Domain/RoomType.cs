namespace ProjectDelta.Domain
{
    // 110일차: 방 종류. 114~121일차(특수 방 단계)에 걸쳐 값이 계속 늘어날 예정이다.
    // 111일차부터 Combat(몬스터 조우)·Event(이벤트 화면)·Trap(함정 판정) 모두 실제
    // 시스템에 연결되어 4종 전부 RoomTypeRollService가 굴린 대로 동작한다.
    public enum RoomType
    {
        Normal = 0,
        Combat = 1,
        Event = 2,
        Trap = 3,

        // 121일차: 상위 개체(보스) 전용 방. 층마다 하나씩 강제 배치하는 로직은 아직 없다 -
        // 122일차(보스 Context 4종 상위 개체 연결)에서 실제로 이 방에 보스를 배치한다.
        Boss = 4
    }

    public static class RoomTypeRules
    {
        public static string GetDisplayName(
            RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Combat:
                    return "전투";

                case RoomType.Event:
                    return "이벤트";

                case RoomType.Trap:
                    return "함정";

                case RoomType.Boss:
                    return "보스";

                default:
                    return "일반";
            }
        }

        // 112일차: 미니맵처럼 좁은 공간에 표시할 때 쓰는 한 글자 표기.
        public static string GetShortLabel(
            RoomType roomType)
        {
            switch (roomType)
            {
                case RoomType.Combat:
                    return "C";

                case RoomType.Event:
                    return "E";

                case RoomType.Trap:
                    return "T";

                case RoomType.Boss:
                    return "B";

                default:
                    return "N";
            }
        }
    }
}
