using System.Collections.Generic;

namespace ProjectDelta.Domain
{
    // 133일차: 기획서 7.4절 "몬스터 관계 이벤트 CG - 호감도 20~100 단계별 전용 CG".
    // 실제 CG 이미지 자산은 아직 없어서(일러스트 전부 색상 박스로 대체 중), 우선 해금
    // 여부만 정확히 추적할 수 있는 ID 체계를 만든다 - 자산이 생기면 이 ID로 그대로 불러오면 된다.
    public static class MonsterCgRule
    {
        public static readonly int[] AffinityThresholds =
        {
            20, 40, 60, 80, 100
        };

        // "{몬스터ID}_CG_{호감도 구간}" 형태의 영구 ID. ProfileData.PermanentRecord.
        // UnlockedCgIds에 이 값이 있으면 해금된 것으로 취급한다.
        public static string BuildCgId(
            string monsterDefinitionId,
            int affinityThreshold)
        {
            return $"{monsterDefinitionId}_CG_{affinityThreshold}";
        }

        // 이번 호감도 상승으로 새로 넘긴 구간이 있으면 그 구간들의 CG ID를 전부 돌려준다
        // (한 번에 여러 구간을 건너뛸 수도 있어 previousFavor~newFavor 사이를 전부 확인한다).
        public static List<string> GetNewlyUnlockedCgIds(
            string monsterDefinitionId,
            int previousFavor,
            int newFavor)
        {
            List<string> unlocked =
                new List<string>();

            for (int i = 0; i < AffinityThresholds.Length; i++)
            {
                int threshold =
                    AffinityThresholds[i];

                if (previousFavor < threshold
                    && newFavor >= threshold)
                {
                    unlocked.Add(
                        BuildCgId(
                            monsterDefinitionId,
                            threshold));
                }
            }

            return unlocked;
        }
    }
}
