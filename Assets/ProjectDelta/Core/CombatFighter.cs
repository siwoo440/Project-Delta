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
        public bool IsDefending // 다음 행동 전까지 유지되는 방어 상태
        { // 속성 시작
            get; // 현재 방어 상태 조회
            private set; // 내부 방어 상태 변경
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
        public bool BeginDefense() // 생존 참가자의 방어 행동 시작
        { // 방어 시작
            if (!Actor.IsAlive || Health <= 0) // 생존 상태 검사
            { // 사망 조건 시작
                return false; // 사망 참가자의 방어 차단
            } // 사망 조건 끝
            IsDefending = true; // 다음 행동까지 방어 상태 유지
            return true; // 방어 행동 성공
        } // 방어 끝
        public void EndDefense() // 새 행동 차례의 방어 종료
        { // 해제 시작
            IsDefending = false; // 이전 방어 상태 해제
        } // 해제 끝
        public bool TryHeal(int amount, out int restored) // 생존 전투자의 실제 체력 회복
        { // 회복 시작
            restored = 0; // 기본 회복량 없음
            if (!Actor.IsAlive || !PotionRecoveryRules.TryRecover(Health, MaxHealth, amount, out var recovered)) // 생존과 회복 가능 여부 검사
            { // 회복 불가 조건 시작
                return false; // 효과 없는 체력 변경 차단
            } // 회복 불가 조건 끝
            restored = recovered - Health; // 실제 회복한 체력
            Health = recovered; // 최대 체력 이내로 회복
            return true; // 실제 회복 성공
        } // 회복 끝
        public int ApplyDamage(int amount) // 체력 피해 적용
        { // 적용 시작
            if (amount <= 0 || Health <= 0) // 유효한 피해와 생존 상태 검사
            { // 피해 없음 조건 시작
                return 0; // 적용할 피해 없음
            } // 피해 없음 조건 끝
            var applied = System.Math.Min(amount, Health); // 남은 체력까지만 피해 적용
            Health -= applied; // 현재 체력 감소
            Actor.IsAlive = Health > 0; // 체력에 따른 생존 상태 갱신
            if (!Actor.IsAlive) // 체력 소진 검사
            { // 사망 조건 시작
                IsDefending = false; // 사망 참가자의 방어 종료
            } // 사망 조건 끝
            return applied; // 실제 적용된 피해 전달
        } // 적용 끝
    } // 클래스 끝
} // 이름 공간 끝
