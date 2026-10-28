namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class CombatFighter // 전투 참가자의 체력과 기본 능력치
    { // 클래스 시작
        public BattleActor Actor // 행동 순서 참가자
        { // 속성 시작
            get; // 참가자 조회
        } // 속성 끝
        public int MaxHealth // 최대 체력
        { // 속성 시작
            get; // 최대 체력 조회
        } // 속성 끝
        public int Health // 현재 체력
        { // 속성 시작
            get; // 체력 조회
            private set; // 내부 체력 변경
        } // 속성 끝
        public int Attack // 기본 공격력
        { // 속성 시작
            get; // 공격력 조회
        } // 속성 끝
        public int Defense // 기본 방어력
        { // 속성 시작
            get; // 방어력 조회
        } // 속성 끝
        public int Accuracy // 기본 명중
        { // 속성 시작
            get; // 명중 조회
        } // 속성 끝
        public int Evasion // 기본 회피
        { // 속성 시작
            get; // 회피 조회
        } // 속성 끝
        public CombatFighter(BattleActor actor, int health, int attack, int defense, int accuracy, int evasion, int currentHealth = -1) // 전투 능력치 초기화
        { // 생성 시작
            Actor = actor ?? throw new System.ArgumentNullException(nameof(actor)); // 유효한 행동 참가자 연결
            MaxHealth = System.Math.Max(0, health); // 음수를 제외한 최대 체력
            Health = currentHealth < 0 ? MaxHealth : System.Math.Min(MaxHealth, currentHealth); // 기존 체력 또는 최대 체력 설정
            Attack = System.Math.Max(0, attack); // 음수를 제외한 공격력
            Defense = System.Math.Max(0, defense); // 음수를 제외한 방어력
            Accuracy = System.Math.Max(0, accuracy); // 음수를 제외한 명중
            Evasion = System.Math.Max(0, evasion); // 음수를 제외한 회피
            Actor.IsAlive = Health > 0; // 초기 체력에 따른 생존 상태
        } // 생성 끝
        public int ApplyDamage(int amount) // 체력 피해 적용
        { // 적용 시작
            if (amount <= 0 || Health <= 0) // 유효한 피해와 생존 상태 검사
            { // 피해 없음 조건 시작
                return 0; // 적용할 피해 없음
            } // 피해 없음 조건 끝
            var applied = System.Math.Min(amount, Health); // 남은 체력까지만 피해 적용
            Health -= applied; // 현재 체력 감소
            Actor.IsAlive = Health > 0; // 체력에 따른 생존 상태 갱신
            return applied; // 실제 적용된 피해 전달
        } // 적용 끝
    } // 클래스 끝
} // 이름 공간 끝
