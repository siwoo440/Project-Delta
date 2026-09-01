using ProjectDelta.Data;

namespace ProjectDelta.Application
{
    // 119일차: 공통 행동 12종의 영구 숙련도(Lv.1~5) 규칙. 레벨이 오를수록 그 행동의 호감도
    // 증가량에 배율이 붙는다 - 자주 쓰는 행동일수록 오래 걸려도 결국 더 강해지는 구조다.
    public static class EventBattleProficiencyRule
    {
        public const int MaxLevel = 5;

        // 레벨업에 필요한 누적 경험치 - Lv.1→2는 20, Lv.2→3은 40 ... (레벨 * 20).
        public static int ExperienceRequiredForNextLevel(
            int currentLevel)
        {
            return currentLevel * 20;
        }

        // Lv.1은 100%, 레벨마다 10%p씩 늘어 Lv.5는 140%.
        public static float GetMultiplier(
            int level)
        {
            int clampedLevel =
                level < 1
                    ? 1
                    : level > MaxLevel
                        ? MaxLevel
                        : level;

            return 1f
                + (clampedLevel - 1)
                * 0.1f;
        }

        // 행동 성공으로 얻은 경험치를 기록에 더하고, 필요하면 레벨업까지 한 번에 처리한다.
        public static bool AddExperience(
            EventBattleActionProficiencyRecord record,
            int amount)
        {
            if (record == null
                || amount <= 0
                || record.Level >= MaxLevel)
            {
                return false;
            }

            record.Experience +=
                amount;

            bool leveledUp =
                false;

            while (record.Level < MaxLevel
                   && record.Experience
                   >= ExperienceRequiredForNextLevel(
                       record.Level))
            {
                record.Experience -=
                    ExperienceRequiredForNextLevel(
                        record.Level);

                record.Level++;

                leveledUp =
                    true;
            }

            if (record.Level >= MaxLevel)
            {
                record.Experience =
                    0;
            }

            return leveledUp;
        }
    }
}
