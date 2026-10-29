using System; // 직렬화 표시
using System.Collections.Generic; // 관찰 식별자 중복 검사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    [Serializable] // 방 좌표 직렬화
    public sealed class SavedRoom // 저장한 방문 방 좌표
    { // 클래스 시작
        public int x; // 방 가로 좌표
        public int y; // 방 세로 좌표
    } // 클래스 끝
    [Serializable] // 문 상태 직렬화
    public sealed class SavedDoorLock // 저장한 시험 문 상태
    { // 클래스 시작
        public bool locked; // 현재 잠금 여부
        public bool forceAttempted; // 강제 개방 시도 여부
    } // 클래스 끝
    [Serializable] // 비밀 연결 직렬화
    public sealed class SavedSecretPassage // 발견한 비밀 통로 상태
    { // 클래스 시작
        public int fromX; // 출발 방 가로 좌표
        public int fromY; // 출발 방 세로 좌표
        public int toX; // 도착 방 가로 좌표
        public int toY; // 도착 방 세로 좌표
        public bool opened; // 실제 개방 여부
    } // 클래스 끝
    [Serializable] // 저장 형식 직렬화
    public sealed class SaveItemSlot // 저장할 아이템 한 칸
    { // 클래스 시작
        public string itemId; // 아이템 식별자
        public string name; // 아이템 표시 이름
        public int quantity; // 아이템 수량
        public int stackLimit; // 최대 중첩 수량
        public bool IsValid() // 아이템 저장 범위 검사
        { // 검사 시작
            if (string.IsNullOrEmpty(itemId)) // 빈 슬롯 검사
            { // 빈 슬롯 시작
                return quantity == 0 && stackLimit == 1; // 빈 슬롯 기본값 확인
            } // 빈 슬롯 끝
            return !string.IsNullOrWhiteSpace(itemId) && itemId.Length <= 128 && quantity > 0 && stackLimit > 0 && quantity <= stackLimit; // 유효한 아이템과 수량
        } // 검사 끝
    } // 클래스 끝
    [Serializable] // 승리 보상 후보 직렬화
    public sealed class SavedBattleReward // 확인과 지급을 기다리는 보상 후보
    { // 클래스 시작
        public string transactionId; // 개별 전투 거래 식별자
        public int normalEnemies; // 승리한 일반 적 수
        public int eliteEnemies; // 승리한 상위 적 수
        public int experience; // 확인된 경험치
        public int gold; // 확인된 시험 골드
        public int potionCount; // 확인된 시험 회복약 수량
        public string itemId; // 확인된 시험 소비품 식별자
    } // 클래스 끝
    [Serializable] // 완료된 보상 기록 직렬화
    public sealed class SavedRewardReceipt // 확정된 전투별 보상 거래
    { // 클래스 시작
        public string transactionId; // 완료된 전투 거래 식별자
        public bool tookPotion; // 소비 아이템 획득 여부
    } // 클래스 끝
    [Serializable] // 저장 형식 직렬화
    public sealed class RunSaveData // 현재 회차 저장 범위
    { // 클래스 시작
        public const int CurrentVersion = 10; // 보상 소비품 종류를 포함한 저장 버전
        public int version; // 회차 저장 버전
        public string runId; // 중복 종료를 막는 회차 식별자
        public int level; // 현재 회차 레벨
        public int experience; // 현재 레벨의 진행 경험치
        public int statPoints; // 사용 가능한 스탯 포인트
        public int gold; // 현재 회차 골드
        public bool hasPendingReward; // 미완료 보상 후보 존재 여부
        public SavedBattleReward pendingReward; // 확인과 지급을 기다리는 보상
        public SavedRewardReceipt[] rewardReceipts; // 획득 또는 포기한 거래 기록
        public int seed; // 회차 최초 던전 시드
        public int generationVersion; // 던전 생성 규칙 버전
        public bool tutorialCompleted; // 첫 슬라임 조우 완료 여부
        public int tutorialHealth; // 도망 후 남은 첫 슬라임 체력
        public bool groupEncounterEnabled; // 시험 다수 조우 활성 여부
        public bool groupGoblin; // 시험 다수 조우의 고블린 종족 여부
        public bool groupEncounterCompleted; // 시험 다수 조우 완료 여부
        public int[] groupEnemyHealth; // 도망한 다수 적의 개별 체력
        public SavedSecretPassage[] secretPassages; // 발견한 비밀 통로 목록
        public string layoutFingerprint; // 저장 당시 현재 층 구조 지문
        public int floor; // 현재 층 번호
        public int roomX; // 현재 방 가로 좌표
        public int roomY; // 현재 방 세로 좌표
        public int playerX; // 플레이어 가로 칸
        public int playerY; // 플레이어 세로 칸
        public int health; // 현재 체력
        public int maxHealth; // 최대 체력
        public int mana; // 현재 마나
        public int maxMana; // 최대 마나
        public int facing; // 플레이어 방향
        public int keys; // 시험 열쇠 수량
        public bool hasPreviousRoom; // 이전 방 연결 여부
        public int previousRoomX; // 이전 방 가로 좌표
        public int previousRoomY; // 이전 방 세로 좌표
        public bool encounterCompleted; // 현재 층 조우 완료 여부
        public int encounterHealth; // 후퇴한 시험 적의 체력
        public SavedRoom[] visitedRooms; // 현재 층 방문 방 목록
        public SavedDoorLock doorLock; // 현재 층 시험 문 상태
        public string[] history; // 현재 회차 탐험 기록
        public SaveItemSlot[] slots; // 인벤토리 슬롯 목록
        public bool IsValid() // 복원 전 기본 형식 검사
        { // 검사 시작
            if ((version < 1 || version > CurrentVersion) || floor < 1 || maxHealth < 1 || health < 0 || health > maxHealth || slots == null || slots.Length > 10) // 기존과 새 버전 및 진행 상태 범위
            { // 오류 조건 시작
                return false; // 잘못된 회차 거절
            } // 오류 조건 끝
            if (version >= 5 && (maxMana < 1 || mana < 0 || mana > maxMana)) // 새 저장의 마나 범위 검사
            { // 마나 오류 시작
                return false; // 잘못된 마나 저장 거절
            } // 마나 오류 끝
            if (version >= 8 && (gold < 0 || statPoints < 0 || statPoints > 18 || !ExperienceRules.TryAdvance(level, experience, 0, out _))) // 성장과 재화 범위 검사
            { // 성장 오류 시작
                return false; // 잘못된 성장 저장 거절
            } // 성장 오류 끝
            if (version >= 8 && hasPendingReward && (pendingReward == null || !Guid.TryParseExact(pendingReward.transactionId, "N", out _) || !ExperienceRules.TryCalculateVictory(floor, pendingReward.normalEnemies, pendingReward.eliteEnemies, out var expectedExperience) || pendingReward.experience != expectedExperience || pendingReward.gold < 0 || pendingReward.potionCount < 0 || pendingReward.potionCount > pendingReward.normalEnemies + pendingReward.eliteEnemies)) // 미확정 보상 후보 검사
            { // 후보 오류 시작
                return false; // 잘못된 보상 저장 거절
            } // 후보 오류 끝
            if (version >= 10 && hasPendingReward && pendingReward.potionCount > 0 && TrialConsumableRules.Find(pendingReward.itemId) == null) // 새 보상 후보의 실제 소비품 종류 검사
            { // 보상 종류 오류 시작
                return false; // 알 수 없는 소비품 지급 차단
            } // 보상 종류 오류 끝
            if (version >= 8 && rewardReceipts != null) // 완료된 보상 거래 목록 검사
            { // 거래 검사 시작
                if (rewardReceipts.Length > 10000) // 보상 기록 수 범위 검사
                { // 기록 수 오류 시작
                    return false; // 지나치게 많은 거래 차단
                } // 기록 수 오류 끝
                var seenRewards = new HashSet<string>(StringComparer.Ordinal); // 보상 거래 식별자 중복 검사
                foreach (var receipt in rewardReceipts) // 완료된 거래 순회
                { // 거래 검사 시작
                    if (receipt == null || !Guid.TryParseExact(receipt.transactionId, "N", out _) || !seenRewards.Add(receipt.transactionId) || (hasPendingReward && receipt.transactionId == pendingReward.transactionId)) // 거래 식별과 미완료 후보 충돌 검사
                    { // 거래 오류 시작
                        return false; // 중복 또는 잘못된 보상 기록 차단
                    } // 거래 오류 끝
                } // 거래 검사 끝
            } // 거래 검사 끝
            foreach (var slot in slots) // 저장 슬롯 순회
            { // 순회 시작
                if (slot == null || !slot.IsValid()) // 누락 또는 잘못된 슬롯 검사
                { // 오류 조건 시작
                    return false; // 잘못된 슬롯 거절
                } // 오류 조건 끝
            } // 순회 끝
            return true; // 기본 형식 검사 통과
        } // 검사 끝
    } // 클래스 끝
    [Serializable] // 저장 형식 직렬화
    public sealed class ProfileSaveData // 회차 간 영구 기록 범위
    { // 클래스 시작
        public const int CurrentVersion = 1; // 지원하는 저장 버전
        public int version; // 프로필 저장 버전
        public string[] observationIds; // 관찰 대상 식별자 목록
        public bool IsValid() // 복원 전 영구 기록 검사
        { // 검사 시작
            if (version != CurrentVersion || observationIds == null || observationIds.Length > 1024) // 버전과 목록 크기 검사
            { // 오류 조건 시작
                return false; // 잘못된 프로필 거절
            } // 오류 조건 끝
            var seen = new HashSet<string>(StringComparer.Ordinal); // 중복 식별자 검사 집합
            foreach (var id in observationIds) // 저장된 관찰 기록 순회
            { // 순회 시작
                if (string.IsNullOrWhiteSpace(id) || id.Length > 128 || !seen.Add(id)) // 빈 값과 중복 및 길이 검사
                { // 오류 조건 시작
                    return false; // 잘못된 관찰 기록 거절
                } // 오류 조건 끝
            } // 순회 끝
            return true; // 영구 기록 검사 통과
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
