namespace ProjectDelta.Domain
{
    // 131일차: 기획서 7.1~7.2절 - 5층 마왕전 이후 "귀환/잔류" 최종 선택에서 갈리는
    // 주요 엔딩 15종. 몬스터 개별 엔딩(20)·NPC 개별 엔딩(10)은 별도 카테고리라
    // 여기 포함하지 않는다 - 판정 시점 자체가 다르다(개별 엔딩은 마왕전 이전에 발생).
    public enum MainEndingId
    {
        None = 0,

        // 귀환 계열(1~8) - 5층 마왕전 승리 후 "현실로 돌아간다"를 선택.
        ReturnToReality = 1,
        HeroicReturn = 2,
        ReturnOfWill = 3,
        ReturnLeavingEverythingBehind = 4,
        EmptyHandedReturn = 5,
        WoundedReturn = 6,
        CursedReturn = 7,
        CompleteExplorerReturn = 8,

        // 잔류 계열(9~13) - 5층 마왕전 승리 후 "던전에 남는다"를 선택.
        KingOfTheDungeon = 9,
        KingOfConquest = 10,
        KingOfCharm = 11,
        CursedKing = 12,
        KingOfRecords = 13,

        // 선택과 무관한 특수 조건.
        MonsterHarem = 14,
        ServantOfTheDemonLord = 15
    }

    public static class MainEndingRules
    {
        // 132일차: 마왕 패배 시 패배 기록("왕 앞에 무릎 꿇다")도 함께 등록해야 하는데,
        // 그 대상 ID가 123일차 DungeonFloorController의 FinalBossMonsterId와 같아야
        // 한다 - 두 곳이 각자 문자열을 들고 있다가 어긋나지 않도록 여기를 기준으로 둔다.
        public const string DemonLordMonsterId = "MON_DEMON_LORD";

        public static string GetDisplayName(
            MainEndingId ending)
        {
            switch (ending)
            {
                case MainEndingId.ReturnToReality: return "현실로의 귀환";
                case MainEndingId.HeroicReturn: return "영웅의 귀환";
                case MainEndingId.ReturnOfWill: return "의지의 귀환";
                case MainEndingId.ReturnLeavingEverythingBehind: return "모든 것을 남겨 둔 귀환";
                case MainEndingId.EmptyHandedReturn: return "빈손의 귀환";
                case MainEndingId.WoundedReturn: return "상처뿐인 귀환";
                case MainEndingId.CursedReturn: return "저주를 품은 귀환";
                case MainEndingId.CompleteExplorerReturn: return "완전한 탐험자의 귀환";
                case MainEndingId.KingOfTheDungeon: return "던전의 왕";
                case MainEndingId.KingOfConquest: return "정복의 왕";
                case MainEndingId.KingOfCharm: return "매혹의 왕";
                case MainEndingId.CursedKing: return "저주받은 왕";
                case MainEndingId.KingOfRecords: return "기록의 왕";
                case MainEndingId.MonsterHarem: return "몬스터 하렘";
                case MainEndingId.ServantOfTheDemonLord: return "마왕의 종";
                default: return "알 수 없음";
            }
        }

        // 기획서 7.2절의 엔딩별 기억 파편 보상 - 5(기본형 3종) / 15(하렘) / 나머지 10.
        public static int GetMemoryShardReward(
            MainEndingId ending)
        {
            switch (ending)
            {
                case MainEndingId.ReturnToReality:
                case MainEndingId.KingOfTheDungeon:
                case MainEndingId.ServantOfTheDemonLord:
                    return 5;

                case MainEndingId.MonsterHarem:
                    return 15;

                case MainEndingId.None:
                    return 0;

                default:
                    return 10;
            }
        }
    }
}
