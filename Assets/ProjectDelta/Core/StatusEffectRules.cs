using System; // 상태 값 유효성 검사
using System.Collections.Generic; // 지속 상태 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public enum RoundStatusKind // 라운드 종료 지속 피해 종류
    { // 종류 시작
        Poison, // 독 지속 피해
        Bleeding // 출혈 지속 피해
    } // 종류 끝
    public sealed class RoundStatusEffect // 라운드 종료 피해 상태
    { // 클래스 시작
        public RoundStatusKind Kind { get; } // 지속 피해 종류
        public int RemainingRounds { get; } // 남은 지속 라운드
        public int DamagePerRound { get; } // 외부에서 지정한 라운드 피해
        public RoundStatusEffect(RoundStatusKind kind, int remainingRounds, int damagePerRound) // 실제 상태 정보 생성
        { // 생성 시작
            if (kind != RoundStatusKind.Poison && kind != RoundStatusKind.Bleeding) // 지원하는 상태 종류 검사
            { // 상태 종류 오류 시작
                throw new ArgumentOutOfRangeException(nameof(kind)); // 잘못된 상태 종류 차단
            } // 상태 종류 오류 끝
            if (remainingRounds < 1) // 남은 지속 시간 검사
            { // 지속 시간 오류 시작
                throw new ArgumentOutOfRangeException(nameof(remainingRounds)); // 잘못된 지속 시간 차단
            } // 지속 시간 오류 끝
            if (damagePerRound < 0) // 지속 피해 수치 검사
            { // 피해 수치 오류 시작
                throw new ArgumentOutOfRangeException(nameof(damagePerRound)); // 잘못된 피해 수치 차단
            } // 피해 수치 오류 끝
            Kind = kind; // 실제 상태 종류 보관
            RemainingRounds = remainingRounds; // 남은 지속 시간 보관
            DamagePerRound = damagePerRound; // 전달된 지속 피해 보관
        } // 생성 끝
    } // 클래스 끝
    public sealed class StatusRoundResult // 지속 피해 처리 결과
    { // 클래스 시작
        public int Health { get; } // 피해 후 남은 체력
        public IReadOnlyList<RoundStatusEffect> Effects { get; } // 만료되지 않은 상태 목록
        internal StatusRoundResult(int health, List<RoundStatusEffect> effects) // 새 결과 생성
        { // 생성 시작
            Health = health; // 결과 체력 보관
            Effects = effects.AsReadOnly(); // 결과 목록 외부 변경 차단
        } // 생성 끝
    } // 클래스 끝
    public static class StatusEffectRules // 상태 성공률과 라운드 종료 규칙
    { // 클래스 시작
        public static int SuccessChance(int baseChance, int attackerModifier, int targetResistance, int otherModifier) // 상태 적용 성공률 계산
        { // 계산 시작
            var raw = (long)baseChance + attackerModifier - targetResistance + otherModifier; // 큰 정수 범위의 보정 계산
            return (int)Math.Max(5L, Math.Min(95L, raw)); // 기획의 5~95 성공률 제한
        } // 계산 끝
        public static StatusRoundResult ResolveRoundEnd(int health, IReadOnlyList<RoundStatusEffect> effects) // 독과 출혈의 라운드 종료 처리
        { // 처리 시작
            if (health < 0 || effects == null) // 체력과 상태 목록 검사
            { // 유효성 오류 시작
                throw new ArgumentException("Invalid health or effects."); // 잘못된 입력 차단
            } // 유효성 오류 끝
            var remaining = new List<RoundStatusEffect>(); // 만료되지 않은 상태 보관
            var currentHealth = health; // 피해를 적용할 현재 체력
            foreach (var effect in effects) // 현재 적용된 상태 순회
            { // 순회 시작
                if (effect == null) // 누락된 상태 검사
                { // 상태 누락 시작
                    throw new ArgumentException("Effects cannot contain null."); // 잘못된 상태 목록 차단
                } // 상태 누락 끝
                currentHealth = (int)Math.Max(0L, (long)currentHealth - effect.DamagePerRound); // 지속 피해의 체력 하한 적용
                var nextRounds = effect.RemainingRounds - 1; // 피해 적용 후 지속 시간 감소
                if (nextRounds > 0) // 만료되지 않은 상태 검사
                { // 상태 유지 시작
                    remaining.Add(new RoundStatusEffect(effect.Kind, nextRounds, effect.DamagePerRound)); // 변경하지 않은 새 상태 보관
                } // 상태 유지 끝
            } // 순회 끝
            return new StatusRoundResult(currentHealth, remaining); // 피해와 잔여 상태 전달
        } // 처리 끝
    } // 클래스 끝
} // 이름 공간 끝
