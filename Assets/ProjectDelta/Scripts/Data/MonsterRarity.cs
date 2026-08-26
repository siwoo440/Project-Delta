namespace ProjectDelta.Data
{
    // 76일차: 몬스터 개체 등급. 한 방에 여러 몬스터가 섞여 나올 때 어떤 몬스터를 탐험 화면의
    // 대표 외형으로 보여줄지 판정하는 기준으로 쓴다 (등급이 높을수록 우선). 선언 순서가 곧
    // 우선순위이므로, 새 등급을 끼워 넣을 때는 순서에 주의한다.
    public enum MonsterRarity
    {
        Normal,
        Rare,
        Boss
    }
}
