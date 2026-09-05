namespace ProjectDelta.Domain
{
    // 131일차: 5층 마왕전이 어떻게 끝났는지 - 최종 선택 화면 표시 여부와
    // MainEndingId.ServantOfTheDemonLord 즉시 판정 여부를 가른다.
    public enum MainBossOutcome
    {
        None = 0,

        // 일반 전투로 마왕을 쓰러뜨렸다.
        NormalVictory = 1,

        // 성인 이벤트 전투(유혹 등)로 마왕을 제압했다.
        EventVictory = 2,

        // 마왕에게 패배했다.
        Defeat = 3,

        // 스스로 항복했다.
        Surrender = 4
    }

    // 131일차: 마왕 승리 후 최종 선택 화면의 선택지 - 아직 선택 전이면 None.
    public enum MainEndingChoice
    {
        None = 0,
        ReturnToReality,
        StayInDungeon
    }

    // 131일차: MainEndingRule이 판정에 필요한 값만 모은 순수 데이터 - RunContext 전체를
    // 넘기지 않고 이 DTO만 넘겨서, 규칙 자체는 RunContext/Data를 몰라도 되게 한다
    // (MainEndingConditionsBuilder(Application)가 RunContext에서 이 값을 채운다).
    public sealed class MainEndingConditions
    {
        public MainBossOutcome BossOutcome { get; set; }

        public MainEndingChoice Choice { get; set; }

        // "빈손의 귀환" - 장착 장비 + 보유 유물 합계.
        public int EquippedAndRelicCount { get; set; }

        // "저주를 품은 귀환"/"저주받은 왕" - 저주 장비 + 저주 유물 합계.
        public int CursedItemCount { get; set; }

        // "상처뿐인 귀환" - 0~1 비율.
        public float HpRatio { get; set; }

        public float StaminaRatio { get; set; }

        // "완전한 탐험자의 귀환" - 5개 층 전체를 100% 탐색했는지.
        public bool FloorExplorationComplete { get; set; }

        // "기록의 왕" - 몬스터 도감 100%인지. 도감 시스템이 생기기 전까지는 항상 false다.
        public bool MonsterDexComplete { get; set; }

        // "모든 것을 남겨 둔 귀환" - 개별 엔딩(몬스터 20+NPC 10) 조건을 몇 개나 만족했는지.
        // 개별 엔딩 판정 로직이 생기기 전까지는 항상 0이다.
        public int IndividualEndingConditionsMetCount { get; set; }

        // "몬스터 하렘" - 관계 대상 20종 호감도가 모두 100인지. 몬스터 호감도 추적이
        // 다 갖춰지기 전까지는 항상 false다.
        public bool AllRelationshipsMaxed { get; set; }
    }
}
