using System; // 저장 형식과 거래 식별자
namespace ProjectDelta.Core // 독립 게임 규칙 이름 공간
{ // 이름 공간 시작
    [Serializable] // 이벤트의 직렬화 형식
    public sealed class TrialEventSaveData // 시험 이벤트 진행 기록
    { // 클래스 시작
        public string eventId; // 고유 이벤트 식별자
        public int[] choices; // 저장된 행동과 떠나기 순서
        public int phase; // 선택 영과 결과 일 및 완료 이
        public string outcomeText; // 저장된 결과 안내
        public int choice; // 미선택 영과 행동 일 및 떠나기 이
        public int availableGold; // 선택 당시 골드
        public int cost; // 확정된 골드 비용
        public int attack; // 장비 보정을 제외한 이벤트 공격력
        public int modifier; // 확정된 시험 보정
        public int difficulty; // 확정된 시험 난이도
        public int playerRoll; // 플레이어 주사위
        public int eventRoll; // 이벤트 주사위
        public string transactionId; // 고유 결과 거래 식별자
        public string result; // 성공과 실패 및 떠나기 결과
        public int rewardGold; // 확정된 시험 골드 보상
        public bool completed; // 결과 확인 완료
    } // 클래스 끝
    public static class TrialEventRules // 정식 수치 미정의 시험 이벤트 정의
    { // 클래스 시작
        public const string ActionChoice = "행동"; // 첫 번째 행동 선택지
        public const string LeaveChoice = "떠나기"; // 두 번째 떠나기 선택지
        public const int TrialCost = 10; // 미확정 시험 골드 비용
        public const int TrialDifficulty = 50; // 미확정 시험 이벤트 난이도
        public const int TrialModifier = 0; // 미확정 시험 능력치 보정
        public const int TrialSuccessRewardGold = 20; // 미확정 시험 성공 보상
        public const string Success = "success"; // 행동 성공 결과
        public const string Failure = "failure"; // 행동 실패 결과
        public const string Left = "left"; // 떠나기 결과
        public static int SuccessCount(int attack) // 기획된 사백 조합의 성공 수
        { // 계산 시작
            return EventCheck.SuccessCount(attack, TrialModifier, TrialDifficulty); // 기존 주사위 판정 활용
        } // 계산 끝
        public static bool CanChoose(int index, int gold, out string reason) // 선택 가능 여부와 비활성 이유
        { // 검사 시작
            reason = string.Empty; // 기본 비활성 이유 없음
            if (index < 0 || index > 1 || gold < 0) // 선택 번호와 재화 검사
            { // 잘못된 선택 시작
                reason = "선택 또는 골드 정보가 올바르지 않습니다."; // 선택 오류 안내
                return false; // 잘못된 입력 차단
            } // 잘못된 선택 끝
            if (index == 0 && gold < TrialCost) // 행동 비용 부족 검사
            { // 비용 부족 시작
                reason = "골드가 부족합니다. (시험 비용 10)"; // 실제 행동 불가 이유
                return false; // 비용 없는 행동 차단
            } // 비용 부족 끝
            return true; // 유효한 선택 가능
        } // 검사 끝
    } // 클래스 끝
    public sealed class TrialEventState // 변경 불가능한 시험 이벤트 상태
    { // 클래스 시작
        private readonly TrialEventSaveData data; // 독립 진행 기록
        private TrialEventState(TrialEventSaveData value) // 검증된 상태 생성
        { // 생성 시작
            data = Copy(value); // 독립 저장본 복사
        } // 생성 끝
        public static bool TryCreate(string eventId, out TrialEventState state) // 기본 순서의 시험 이벤트 생성
        { // 생성 시작
            return TryCreate(eventId, 0, out state); // 행동과 떠나기 순서 지정
        } // 생성 끝
        public static bool TryCreate(string eventId, int firstChoice, out TrialEventState state) // 지정된 선택 순서의 이벤트 생성
        { // 생성 시작
            state = null; // 기본 생성 실패
            if (string.IsNullOrWhiteSpace(eventId) || eventId.Length > 128 || firstChoice < 0 || firstChoice > 1) // 고유 식별자와 순서 검사
            { // 오류 시작
                return false; // 빈 식별자 차단
            } // 오류 끝
            state = new TrialEventState(new TrialEventSaveData { eventId = eventId, choices = new[] { firstChoice, 1 - firstChoice } }); // 고정 선택지 초기 상태
            return true; // 시험 이벤트 생성 성공
        } // 생성 끝
        public static bool TryRestore(TrialEventSaveData saved, out TrialEventState state) // 저장된 진행과 판정 검증
        { // 복원 시작
            state = null; // 기본 복원 실패
            if (saved == null || string.IsNullOrWhiteSpace(saved.eventId) || saved.eventId.Length > 128 || saved.choices == null || saved.choices.Length != 2 || saved.choices[0] < 0 || saved.choices[0] > 1 || saved.choices[1] != 1 - saved.choices[0] || saved.choice < 0 || saved.choice > 2 || saved.phase < 0 || saved.phase > 2 || saved.phase != (saved.choice == 0 ? 0 : saved.completed ? 2 : 1) || saved.availableGold < 0) // 식별자와 고정 순서 검사
            { // 오류 시작
                return false; // 손상된 이벤트 차단
            } // 오류 끝
            if (saved.choice == 0) // 미선택 상태 검사
            { // 미선택 시작
                if (saved.completed || saved.availableGold != 0 || saved.cost != 0 || saved.attack != 0 || saved.modifier != 0 || saved.difficulty != 0 || saved.playerRoll != 0 || saved.eventRoll != 0 || !string.IsNullOrEmpty(saved.transactionId) || !string.IsNullOrEmpty(saved.result) || !string.IsNullOrEmpty(saved.outcomeText) || saved.rewardGold != 0) // 미선택 결과 존재 검사
                { // 오류 시작
                    return false; // 미선택 상태의 결과 차단
                } // 오류 끝
            } // 미선택 끝
            else // 선택된 이벤트 검사
            { // 선택 검사 시작
                if (!Guid.TryParseExact(saved.transactionId, "N", out _) || saved.attack < 0) // 거래 식별자와 공격력 검사
                { // 거래 오류 시작
                    return false; // 누락된 거래와 음수 공격 차단
                } // 거래 오류 끝
                if (saved.choice == 2) // 떠나기 결과 검사
                { // 떠나기 시작
                    if (saved.cost != 0 || saved.attack != 0 || saved.modifier != 0 || saved.difficulty != 0 || saved.playerRoll != 0 || saved.eventRoll != 0 || saved.rewardGold != 0 || saved.result != TrialEventRules.Left || saved.outcomeText != "이벤트를 떠났습니다.") // 판정 없는 떠나기 검사
                    { // 오류 시작
                        return false; // 떠나기의 비용과 판정 차단
                    } // 오류 끝
                } // 떠나기 끝
                else // 행동 결과 검사
                { // 행동 시작
                    if (saved.cost != TrialEventRules.TrialCost || saved.availableGold < saved.cost || saved.modifier != TrialEventRules.TrialModifier || saved.difficulty != TrialEventRules.TrialDifficulty || !EventCheck.TryResolve(saved.attack, saved.modifier, saved.difficulty, saved.playerRoll, saved.eventRoll, out var success) || saved.result != (success ? TrialEventRules.Success : TrialEventRules.Failure) || saved.rewardGold != (success ? TrialEventRules.TrialSuccessRewardGold : 0) || saved.outcomeText != (success ? "행동 성공 · 시험 골드 20 획득" : "행동 실패")) // 비용과 주사위 및 결과 대조
                    { // 결과 오류 시작
                        return false; // 임의 결과와 손상된 비용 차단
                    } // 결과 오류 끝
                } // 행동 끝
            } // 선택 검사 끝
            state = new TrialEventState(saved); // 독립 이벤트 복원
            return true; // 유효한 상태 복원
        } // 복원 끝
        public TrialEventSaveData Snapshot() // 직렬화할 독립 상태 조회
        { // 조회 시작
            return Copy(data); // 선택지 배열을 포함한 심층 복사
        } // 조회 끝
        public bool TryChoose(int index, int availableGold, int eventAttack, int playerRoll, int eventRoll, string transactionId, out TrialEventState state, out string reason) // 선택과 비용 및 고정 판정 준비
        { // 선택 시작
            state = null; // 기본 선택 실패
            reason = string.Empty; // 기본 실패 안내 없음
            if (data.choice != 0 || data.completed) // 이미 결정된 선택 검사
            { // 중복 오류 시작
                reason = "이미 선택한 이벤트입니다."; // 중복 선택 안내
                return false; // 주사위 재추첨 차단
            } // 중복 오류 끝
            if (index < 0 || index >= data.choices.Length) // 표시 선택지 범위 검사
            { // 범위 오류 시작
                reason = "선택 정보가 올바르지 않습니다."; // 선택 오류 안내
                return false; // 잘못된 표시 번호 차단
            } // 범위 오류 끝
            var action = data.choices[index]; // 고정 순서의 행동 종류
            if (!TrialEventRules.CanChoose(action, availableGold, out reason)) // 선택 조건과 비용 검사
            { // 조건 오류 시작
                return false; // 조건 미충족 차단
            } // 조건 오류 끝
            if (!Guid.TryParseExact(transactionId, "N", out _) || eventAttack < 0 || (action == 1 && (eventAttack != 0 || playerRoll != 0 || eventRoll != 0))) // 거래와 입력 검사
            { // 입력 오류 시작
                reason = "이벤트 입력이 올바르지 않습니다."; // 잘못된 입력 안내
                return false; // 비정상 선택 차단
            } // 입력 오류 끝
            var next = Copy(data); // 기존 상태를 보존한 복사
            next.choice = action + 1; // 선택한 행동 종류 기록
            next.phase = 1; // 결정된 결과 표시 단계
            next.availableGold = availableGold; // 선택 당시 소지 골드 기록
            next.transactionId = transactionId; // 고유 결과 거래 기록
            if (action == 0) // 행동 선택 검사
            { // 행동 시작
                if (!EventCheck.TryResolve(eventAttack, TrialEventRules.TrialModifier, TrialEventRules.TrialDifficulty, playerRoll, eventRoll, out var success)) // 주사위 범위와 동점 성공 판정
                { // 주사위 오류 시작
                    reason = "주사위 값이 올바르지 않습니다."; // 잘못된 주사위 안내
                    return false; // 잘못된 주사위 결과 차단
                } // 주사위 오류 끝
                next.cost = TrialEventRules.TrialCost; // 실제 시험 비용 기록
                next.attack = eventAttack; // 이벤트 기본 공격력 기록
                next.modifier = TrialEventRules.TrialModifier; // 시험 보정 기록
                next.difficulty = TrialEventRules.TrialDifficulty; // 시험 난이도 기록
                next.playerRoll = playerRoll; // 플레이어 주사위 기록
                next.eventRoll = eventRoll; // 이벤트 주사위 기록
                next.result = success ? TrialEventRules.Success : TrialEventRules.Failure; // 확정 판정 결과
                next.rewardGold = success ? TrialEventRules.TrialSuccessRewardGold : 0; // 성공 시 시험 보상
                next.outcomeText = success ? "행동 성공 · 시험 골드 20 획득" : "행동 실패"; // 확정된 결과 안내
            } // 행동 끝
            else // 떠나기 선택
            { // 떠나기 시작
                next.result = TrialEventRules.Left; // 판정 없는 떠나기 기록
                next.outcomeText = "이벤트를 떠났습니다."; // 떠나기 결과 안내
            } // 떠나기 끝
            state = new TrialEventState(next); // 결과를 표시하기 전 저장할 상태
            return true; // 선택과 고정 판정 성공
        } // 선택 끝
        public bool TryComplete(out TrialEventState state) // 확인된 결과의 완료 상태
        { // 완료 시작
            state = null; // 기본 완료 실패
            if (data.choice == 0 || data.completed) // 미선택과 중복 완료 검사
            { // 완료 오류 시작
                return false; // 미결정 결과와 중복 지급 차단
            } // 완료 오류 끝
            var next = Copy(data); // 원본을 보존한 독립 상태
            next.completed = true; // 결과 확인 완료 표시
            next.phase = 2; // 완료된 이벤트 단계
            state = new TrialEventState(next); // 결과 확인 후 저장할 상태
            return true; // 유효한 완료 상태 준비
        } // 완료 끝
        private static TrialEventSaveData Copy(TrialEventSaveData source) // 선택지와 결과의 독립 복사
        { // 복사 시작
            return new TrialEventSaveData { eventId = source.eventId, choices = source.choices == null ? null : (int[])source.choices.Clone(), phase = source.phase, outcomeText = source.outcomeText, choice = source.choice, availableGold = source.availableGold, cost = source.cost, attack = source.attack, modifier = source.modifier, difficulty = source.difficulty, playerRoll = source.playerRoll, eventRoll = source.eventRoll, transactionId = source.transactionId, result = source.result, rewardGold = source.rewardGold, completed = source.completed }; // 저장 필드와 선택지 복사
        } // 복사 끝
    } // 클래스 끝
} // 이름 공간 끝
