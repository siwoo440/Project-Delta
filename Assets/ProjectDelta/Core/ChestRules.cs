using System; // 저장 형식과 거래 식별자
using System.Collections.Generic; // 고유 보상 후보 검사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public enum ChestGrade // 기획된 보물상자 등급
    { // 등급 목록 시작
        Common, // 일반 상자
        Advanced, // 고급 상자
        Rare // 희귀 상자
    } // 등급 목록 끝
    [Serializable] // 고정 보상 후보의 저장 형식
    public sealed class ChestRewardCandidateData // 상자에서 확정한 개별 후보
    { // 클래스 시작
        public string candidateId; // 보상 화면 안의 고유 후보 식별자
        public string itemId; // 아이템 또는 유물과 재화 식별자
        public int quantity; // 확정된 획득 수량
        public EquipmentVariantData equipment; // 선택적인 개별 장비와 공개 옵션
    } // 클래스 끝
    [Serializable] // 상자 판정과 보상 단계의 저장 형식
    public sealed class ChestSaveData // 특정 상자의 독립 진행 상태
    { // 클래스 시작
        public string chestId; // 같은 회차의 고유 상자 식별자
        public int grade; // 기획된 상자 등급
        public bool wasLocked; // 처음 생성한 잠금 여부
        public bool unlocked; // 현재 잠금 해제 여부
        public bool forceAttempted; // 이미 사용한 강제 개방 시도
        public int mimicState; // 미확정 0과 일반 1 및 미믹 2
        public bool mimicDefeated; // 미믹의 실제 승리 여부
        public int mimicHealth; // 생존 미믹의 저장 체력과 미저장 영
        public int baseRewardCount; // 등급 범위에서 확정된 기본 보상 수
        public int experience; // 상자 또는 실제 미믹 전투 경험치
        public string transactionId; // 보상 중복을 막는 고유 거래 식별자
        public ChestRewardCandidateData[] rewards; // 미리 확정한 실제 보상 후보
        public bool completed; // 지급 완료 상태
    } // 클래스 끝
    public static class ChestRules // 등급별 확률과 수량 및 경험치
    { // 클래스 시작
        public static int MimicChance(ChestGrade grade) // 등급별 미믹 판정 확률
        { // 조회 시작
            switch (grade) // 상자 등급 구분
            { // 등급 선택 시작
                case ChestGrade.Common: return 8; // 일반 상자의 8퍼센트
                case ChestGrade.Advanced: return 12; // 고급 상자의 12퍼센트
                case ChestGrade.Rare: return 18; // 희귀 상자의 18퍼센트
                default: return -1; // 미등록 상자 등급
            } // 등급 선택 끝
        } // 조회 끝
        public static int NormalExperience(ChestGrade grade) // 일반 상자 개방 경험치
        { // 조회 시작
            switch (grade) // 상자 등급 구분
            { // 등급 선택 시작
                case ChestGrade.Common: return 10; // 일반 상자의 경험치
                case ChestGrade.Advanced: return 20; // 고급 상자의 경험치
                case ChestGrade.Rare: return 40; // 희귀 상자의 경험치
                default: return -1; // 미등록 상자 등급
            } // 등급 선택 끝
        } // 조회 끝
        public static bool TryRewardCount(ChestGrade grade, int baseCount, bool wasLocked, bool mimic, out int count) // 잠금과 미믹 보상 수 계산
        { // 계산 시작
            count = 0; // 기본 보상 수 없음
            var minimum = grade == ChestGrade.Common ? 1 : grade == ChestGrade.Advanced ? 2 : grade == ChestGrade.Rare ? 3 : -1; // 등급별 기본 최소 수량
            if (minimum < 0 || baseCount < minimum || baseCount > minimum + 1) // 기획된 등급별 기본 범위 검사
            { // 범위 오류 시작
                return false; // 잘못된 기본 보상 수 차단
            } // 범위 오류 끝
            count = baseCount + (wasLocked ? 1 : 0) + (mimic ? 1 : 0); // 잠긴 상자와 미믹의 별도 추가 보상
            return true; // 유효한 총 보상 수
        } // 계산 끝
    } // 클래스 끝
    public sealed class ChestState // 외부 변경을 막는 개별 상자 상태
    { // 클래스 시작
        private readonly ChestSaveData data; // 고정된 판정과 보상 데이터
        private ChestState(ChestSaveData data) // 검증된 독립 상자 구성
        { // 구성 시작
            this.data = Copy(data); // 외부 변경을 막는 심층 복사
        } // 구성 끝
        public static bool TryCreate(string chestId, ChestGrade grade, bool locked, out ChestState state) // 최초 상자의 고유 상태 생성
        { // 생성 시작
            state = null; // 기본 생성 결과 없음
            if (!ValidId(chestId) || ChestRules.MimicChance(grade) < 0) // 실제 상자와 등급 검사
            { // 생성 거절 시작
                return false; // 잘못된 상자 차단
            } // 생성 거절 끝
            state = new ChestState(new ChestSaveData { chestId = chestId, grade = (int)grade, wasLocked = locked, unlocked = !locked, rewards = new ChestRewardCandidateData[0] }); // 미판정 상자의 독립 상태
            return true; // 유효한 상자 생성 성공
        } // 생성 끝
        public static bool TryRestore(ChestSaveData saved, out ChestState state) // 저장된 상자 상태의 일관성 검사
        { // 복원 시작
            state = null; // 기본 복원 결과 없음
            if (saved == null || !ValidId(saved.chestId) || ChestRules.MimicChance((ChestGrade)saved.grade) < 0 || (!saved.wasLocked && (!saved.unlocked || saved.forceAttempted)) || saved.mimicState < 0 || saved.mimicState > 2) // 기본 상태와 잠금 및 결과 범위 검사
            { // 기본 오류 시작
                return false; // 잘못된 상자 상태 차단
            } // 기본 오류 끝
            if (saved.mimicHealth < 0 || saved.mimicHealth > 70 || ((saved.mimicState != 2 || saved.mimicDefeated) && saved.mimicHealth != 0)) // 미믹 상태와 체력 일관성 검사
            { // 체력 오류 시작
                return false; // 음수와 무관한 상자의 체력 차단
            } // 체력 오류 끝
            if (saved.mimicState == 0) // 아직 열리지 않은 상자 검사
            { // 미확정 검사 시작
                if (saved.mimicDefeated || saved.completed || saved.experience != 0 || saved.baseRewardCount != 0 || !string.IsNullOrEmpty(saved.transactionId) || (saved.rewards != null && saved.rewards.Length != 0)) // 미판정 상태의 불필요한 결과 검사
                { // 상태 충돌 시작
                    return false; // 확정되지 않은 보상과 완료 차단
                } // 상태 충돌 끝
            } // 미확정 검사 끝
            else // 판정 완료 상자 분기
            { // 확정 검사 시작
                var mimic = saved.mimicState == 2; // 실제 미믹 결과
                if (!saved.unlocked || !Guid.TryParseExact(saved.transactionId, "N", out _) || !ChestRules.TryRewardCount((ChestGrade)saved.grade, saved.baseRewardCount, saved.wasLocked, mimic, out var count) || !ValidRewards(saved.rewards, count)) // 잠금과 거래 및 고정 후보 검사
                { // 확정 오류 시작
                    return false; // 손상된 보상과 거래 차단
                } // 확정 오류 끝
                if ((!mimic && (saved.mimicDefeated || saved.experience != ChestRules.NormalExperience((ChestGrade)saved.grade))) || (mimic && (saved.experience < 0 || (!saved.mimicDefeated && (saved.completed || saved.experience != 0))))) // 일반 경험치와 미믹 승리 상태 검사
                { // 승리 오류 시작
                    return false; // 서로 모순된 승패와 경험치 차단
                } // 승리 오류 끝
            } // 확정 검사 끝
            state = new ChestState(saved); // 검증된 독립 상자 복원
            return true; // 정상 저장 복원 성공
        } // 복원 끝
        public ChestSaveData Snapshot() // 현재 상자의 독립 저장본
        { // 저장 시작
            return Copy(data); // 보상 후보와 장비를 포함한 심층 복사
        } // 저장 끝
        public bool TryUnlockWithKey(int keys, out ChestState state, out int remainingKeys) // 실제 열쇠 한 개의 잠금 해제
        { // 해제 시작
            state = null; // 기본 해제 결과 없음
            remainingKeys = keys; // 실패 시 기존 열쇠 보존
            if (!data.wasLocked || data.unlocked || data.mimicState != 0 || keys <= 0) // 잠긴 미개봉 상자와 열쇠 검사
            { // 해제 거절 시작
                return false; // 잘못된 열쇠 소비 차단
            } // 해제 거절 끝
            var next = Copy(data); // 원본을 보존한 독립 상태
            next.unlocked = true; // 열쇠로 잠금 해제
            state = new ChestState(next); // 확정 전 독립 상태 전달
            remainingKeys = keys - 1; // 실제 열쇠 한 개 소비
            return true; // 유효한 열쇠 개방 성공
        } // 해제 끝
        public bool TryForce(int attack, int difficulty, int playerRoll, int chestRoll, out ChestState state, out bool success) // 이벤트 공격력의 단일 강제 개방 판정
        { // 판정 시작
            state = null; // 기본 판정 결과 없음
            success = false; // 기본 개방 실패
            if (!data.wasLocked || data.unlocked || data.forceAttempted || data.mimicState != 0 || attack < 0 || difficulty < 0 || playerRoll < 1 || playerRoll > 20 || chestRoll < 1 || chestRoll > 20) // 잠금과 단일 시도 및 주사위 검사
            { // 판정 거절 시작
                return false; // 유효하지 않은 시도 차단
            } // 판정 거절 끝
            success = (long)attack + playerRoll >= (long)difficulty + chestRoll; // 기획의 동점 성공과 정수 범위 보호
            var next = Copy(data); // 원본을 보존한 판정 상태
            next.forceAttempted = true; // 성공 또는 실패의 단일 시도 기록
            next.unlocked = success; // 성공한 경우에만 잠금 해제
            state = new ChestState(next); // 확인된 독립 판정 상태
            return true; // 유효한 단일 판정 완료
        } // 판정 끝
        public bool TryReveal(int mimicRoll, int baseRewardCount, string transactionId, ChestRewardCandidateData[] rewards, out ChestState state) // 확정된 미믹과 보상 후보 저장 준비
        { // 확정 시작
            state = null; // 기본 확정 결과 없음
            if (!data.unlocked || data.mimicState != 0 || mimicRoll < 0 || mimicRoll >= 100 || !Guid.TryParseExact(transactionId, "N", out _)) // 잠금과 미판정 및 난수와 거래 검사
            { // 확정 거절 시작
                return false; // 중복 판정과 잘못된 입력 차단
            } // 확정 거절 끝
            var mimic = mimicRoll < ChestRules.MimicChance((ChestGrade)data.grade); // 기획 확률의 미믹 판정
            if (!ChestRules.TryRewardCount((ChestGrade)data.grade, baseRewardCount, data.wasLocked, mimic, out var count) || !ValidRewards(rewards, count)) // 기본 수량과 실제 고정 후보 검사
            { // 후보 오류 시작
                return false; // 미정 후보와 잘못된 수량 차단
            } // 후보 오류 끝
            var next = Copy(data); // 원본을 보존한 독립 상자
            next.mimicState = mimic ? 2 : 1; // 확정된 미믹 또는 일반 결과
            next.baseRewardCount = baseRewardCount; // 실제 기본 보상 수 보관
            next.experience = mimic ? 0 : ChestRules.NormalExperience((ChestGrade)data.grade); // 일반 경험치 또는 전투 결과 대기
            next.transactionId = transactionId; // 고유 거래 식별자 보관
            next.rewards = CopyRewards(rewards); // 확정 후보의 독립 복사
            state = new ChestState(next); // 검증된 저장 전 결과
            return true; // 상자 판정과 후보 확정 성공
        } // 확정 끝
        public bool TryStoreMimicHealth(int health, out ChestState state) // 생존 미믹의 현재 체력 저장 준비
        { // 저장 시작
            state = null; // 기본 저장 결과 없음
            if (data.mimicState != 2 || data.mimicDefeated || data.completed || health <= 0 || health > 70) // 생존 미믹과 양수 체력 검사
            { // 저장 거절 시작
                return false; // 일반 상자와 사망 및 잘못된 체력 차단
            } // 저장 거절 끝
            var next = Copy(data); // 원본을 보존한 독립 상태
            next.mimicHealth = health; // 실제 전투 잔여 체력 보관
            state = new ChestState(next); // 독립 저장 상태 전달
            return true; // 생존 미믹 체력 저장 성공
        } // 저장 끝
        public bool TryWinMimic(int battleExperience, out ChestState state) // 실제 미믹 전투 승리 확정
        { // 승리 시작
            state = null; // 기본 승리 결과 없음
            if (data.mimicState != 2 || data.mimicDefeated || data.completed || battleExperience < 0) // 미믹과 미확정 승리 및 경험치 검사
            { // 승리 거절 시작
                return false; // 일반 상자와 중복 승리 차단
            } // 승리 거절 끝
            var next = Copy(data); // 원본을 보존한 독립 상태
            next.mimicDefeated = true; // 실제 미믹 승리 기록
            next.mimicHealth = 0; // 처치한 미믹의 저장 체력 초기화
            next.experience = battleExperience; // 외부 전투에서 확정한 경험치
            state = new ChestState(next); // 보상 지급 전 독립 상태
            return true; // 유효한 미믹 승리 성공
        } // 승리 끝
        public bool TryComplete(out ChestState state) // 보상 지급과 함께 원자적으로 저장할 완료 상태
        { // 완료 시작
            state = null; // 기본 완료 결과 없음
            if (data.mimicState == 0 || data.completed || (data.mimicState == 2 && !data.mimicDefeated)) // 판정과 미지급 및 미믹 승리 검사
            { // 완료 거절 시작
                return false; // 미확정 또는 중복 지급 차단
            } // 완료 거절 끝
            var next = Copy(data); // 원본을 보존한 완료 상태
            next.completed = true; // 보상 지급 완료 표시
            state = new ChestState(next); // 지급과 같은 저장에 사용할 결과
            return true; // 유효한 완료 상태 준비 성공
        } // 완료 끝
        private static bool ValidRewards(ChestRewardCandidateData[] rewards, int count) // 고유 후보와 개별 장비 검증
        { // 후보 검사 시작
            if (rewards == null || rewards.Length != count) // 확정된 총 보상 수 검사
            { // 수량 오류 시작
                return false; // 누락되거나 초과한 후보 차단
            } // 수량 오류 끝
            var candidates = new HashSet<string>(StringComparer.Ordinal); // 고유 보상 후보 식별자
            var instances = new HashSet<Guid>(); // 고유 장비 인스턴스 식별자
            foreach (var reward in rewards) // 실제 후보 순회
            { // 후보 검사 시작
                if (reward == null || !ValidId(reward.candidateId) || !candidates.Add(reward.candidateId) || !ValidId(reward.itemId) || reward.quantity <= 0) // 식별자와 실제 수량 검사
                { // 후보 오류 시작
                    return false; // 중복과 잘못된 후보 차단
                } // 후보 오류 끝
                if (reward.equipment != null && (!TrialEquipmentTraits.IsValid(reward.equipment) || reward.quantity != 1 || reward.equipment.definitionId != reward.itemId || !Guid.TryParseExact(reward.equipment.instanceId, "N", out var id) || !instances.Add(id))) // 개별 장비의 품목과 단일 인스턴스 검사
                { // 장비 오류 시작
                    return false; // 손상되거나 중복된 개별 장비 차단
                } // 장비 오류 끝
            } // 후보 검사 끝
            return true; // 검증된 보상 후보 목록
        } // 후보 검사 끝
        private static bool ValidId(string id) // 저장 식별자의 기본 범위 검사
        { // 검사 시작
            return !string.IsNullOrWhiteSpace(id) && id.Length <= 128; // 공백과 과도한 식별자 차단
        } // 검사 끝
        private static ChestSaveData Copy(ChestSaveData source) // 상자와 후보를 포함한 독립 복사
        { // 복사 시작
            return new ChestSaveData { chestId = source.chestId, grade = source.grade, wasLocked = source.wasLocked, unlocked = source.unlocked, forceAttempted = source.forceAttempted, mimicState = source.mimicState, mimicDefeated = source.mimicDefeated, mimicHealth = source.mimicHealth, baseRewardCount = source.baseRewardCount, experience = source.experience, transactionId = source.transactionId, rewards = CopyRewards(source.rewards), completed = source.completed }; // 상자 상태와 후보 복사
        } // 복사 끝
        private static ChestRewardCandidateData[] CopyRewards(ChestRewardCandidateData[] rewards) // 확정 후보와 장비의 심층 복사
        { // 복사 시작
            if (rewards == null) // 누락된 후보 목록 검사
            { // 누락 시작
                return null; // 누락 상태 보존
            } // 누락 끝
            var copy = new ChestRewardCandidateData[rewards.Length]; // 후보별 독립 배열
            for (var index = 0; index < rewards.Length; index++) // 확정 후보 순회
            { // 후보 복사 시작
                var item = rewards[index]; // 현재 보상 후보
                copy[index] = item == null ? null : new ChestRewardCandidateData { candidateId = item.candidateId, itemId = item.itemId, quantity = item.quantity, equipment = EquipmentVariantRules.Copy(item.equipment) }; // 식별자와 수량 및 장비의 독립 복사
            } // 후보 복사 끝
            return copy; // 독립 보상 후보 목록
        } // 복사 끝
    } // 클래스 끝
} // 이름 공간 끝
