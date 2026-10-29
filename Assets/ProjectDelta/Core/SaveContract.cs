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
        public EquipmentVariantData equipment; // 개별 장비의 등급과 확정 옵션
        public bool IsValid() // 아이템 저장 범위 검사
        { // 검사 시작
            if (string.IsNullOrEmpty(itemId)) // 빈 슬롯 검사
            { // 빈 슬롯 시작
                return quantity == 0 && stackLimit == 1 && equipment == null; // 빈 슬롯과 장비 정보 기본값 확인
            } // 빈 슬롯 끝
            if (equipment != null && (quantity != 1 || stackLimit != 1 || equipment.definitionId != itemId || !TrialEquipmentTraits.IsValid(equipment))) // 개별 장비의 종류와 단일 수량 및 옵션 검사
            { // 개별 장비 오류 시작
                return false; // 손상된 장비 아이템 차단
            } // 개별 장비 오류 끝
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
        public EquipmentVariantData equipment; // 저장된 개별 장비 보상 후보
    } // 클래스 끝
    [Serializable] // 완료된 보상 기록 직렬화
    public sealed class SavedRewardReceipt // 확정된 전투별 보상 거래
    { // 클래스 시작
        public string transactionId; // 완료된 전투 거래 식별자
        public bool tookPotion; // 소비 아이템 획득 여부
        public bool tookEquipment; // 개별 장비 획득 여부
    } // 클래스 끝
    [Serializable] // 저장 형식 직렬화
    public sealed class RunSaveData // 현재 회차 저장 범위
    { // 클래스 시작
        public const int CurrentVersion = 17; // 시험 이벤트 전투를 포함한 저장 버전
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
        public string weaponId; // 장착한 무기 식별자
        public string armorId; // 장착한 방어구 식별자
        public string accessoryId; // 장착한 장신구 식별자
        public string bagId; // 장착한 가방 식별자
        public EquipmentVariantData weaponVariant; // 장착한 무기의 개별 특성
        public EquipmentVariantData armorVariant; // 장착한 방어구의 개별 특성
        public EquipmentVariantData accessoryVariant; // 장착한 장신구의 개별 특성
        public EquipmentVariantData bagVariant; // 장착한 가방의 개별 특성
        public string[] relicIds; // 현재 회차의 보유 유물 식별자
        public ShopStockData[] shopStocks; // 방문한 층의 확정된 상품과 재고
        public bool roomContentEnabled; // 상자와 함정 시험 배치 활성 여부
        public SavedFloorRoomContent[] roomContents; // 방문한 층의 상자와 함정 상태
        public bool trialEventsEnabled; // 시험 이벤트 배치 활성 여부
        public SavedFloorTrialEvent[] trialEvents; // 층별 시험 이벤트 기록
        public bool trialNpcEnabled; // 시험 상인 배치 활성 여부
        public bool hasTrialEventBattle; // 진행 중인 이벤트 전투 존재 여부
        public TrialEventBattleSaveData trialEventBattle; // 마지막 완료 행동의 이벤트 전투
        public bool IsValid() // 복원 전 기본 형식 검사
        { // 검사 시작
            if ((version < 1 || version > CurrentVersion) || floor < 1 || maxHealth < 1 || health < 0 || health > maxHealth || slots == null || slots.Length > (version >= 11 ? 23 : 10)) // 기존과 새 버전 및 진행 상태 범위
            { // 오류 조건 시작
                return false; // 잘못된 회차 거절
            } // 오류 조건 끝
            if (version >= 11 && (!EquipmentState.TryRestore(weaponId, armorId, accessoryId, bagId, out var equipment) || !equipment.TryCalculate(50, 40, 10, 0, 150, 10, out var equipmentStats) || slots.Length != equipmentStats.InventoryCapacity)) // 장비 부위와 가방 용량 일치 검사
            { // 장비 저장 오류 시작
                return false; // 알 수 없는 장비와 잘못된 용량 차단
            } // 장비 저장 오류 끝
            if (version >= 14 && (roomContents == null || roomContents.Length > 2 || !FloorRoomContent.HasUniqueFloors(roomContents) || !roomContentEnabled && roomContents.Length != 0)) // 층별 콘텐츠 목록과 활성 상태 검사
            { // 방 콘텐츠 오류 시작
                return false; // 누락 또는 중복된 층 상태 차단
            } // 방 콘텐츠 오류 끝
            if (version >= 17 && hasTrialEventBattle && (hasPendingReward || !TrialEventBattleState.TryRestore(trialEventBattle, out _))) // 이벤트 전투와 기존 보상 상태 검사
            { // 전투 저장 오류 시작
                return false; // 손상 또는 중복 보상 전투 차단
            } // 전투 저장 오류 끝
            if (version >= 16 && trialNpcEnabled && !trialEventsEnabled) // 상인과 기존 콘텐츠 활성 검사
            { // 오류 시작
                return false; // 필요한 기존 배치가 없는 상인 차단
            } // 오류 끝
            if (version >= 15) // 층별 시험 이벤트 검사
            { // 이벤트 검사 시작
                if (trialEvents == null || trialEvents.Length > 2 || trialEventsEnabled && !roomContentEnabled || !trialEventsEnabled && trialEvents.Length != 0) // 이벤트 목록과 활성 상태 검사
                { // 오류 시작
                    return false; // 누락 또는 비활성 상태의 이벤트 차단
                } // 오류 끝
                var eventFloors = new HashSet<int>(); // 중복 층 검사
                var completedEvents = 0; // 회차당 결정된 이벤트 수
                foreach (var record in trialEvents) // 저장된 층별 이벤트 순회
                { // 기록 검사 시작
                    if (record == null || record.floor < 1 || record.floor > floor || !eventFloors.Add(record.floor) || !TrialEventState.TryRestore(record.eventData, out var eventState) || record.eventData.eventId != "trial-event-01") // 층과 이벤트 상태 검사
                    { // 오류 시작
                        return false; // 중복 또는 손상 이벤트 차단
                    } // 오류 끝
                    if (eventState.Snapshot().phase > 0) // 선택이 확정된 이벤트 검사
                    { // 완료 검사 시작
                        completedEvents++; // 회차당 결정 수 증가
                    } // 완료 검사 끝
                } // 기록 검사 끝
                if (completedEvents > 1) // 중복 결정 검사
                { // 오류 시작
                    return false; // 같은 회차의 중복 결과 차단
                } // 오류 끝
            } // 이벤트 검사 끝
            if (version >= 13 && (!RelicState.TryRestore(relicIds, out _) || shopStocks == null || shopStocks.Length > 2)) // 유물과 현재 시험 층의 상점 수 검사
            { // 경제 저장 오류 시작
                return false; // 잘못된 유물과 상점 목록 차단
            } // 경제 저장 오류 끝
            if (version >= 13) // 층별 시험 상점 검사
            { // 상점 검사 시작
                var seenFloors = new HashSet<int>(); // 상점 층 중복 검사
                foreach (var shop in shopStocks) // 저장된 층별 상점 순회
                { // 상점 검증 시작
                    if (shop == null || shop.floor < 1 || shop.floor > floor || !seenFloors.Add(shop.floor) || !ShopState.TryRestore(shop, out _)) // 층 범위와 상품 상태 검사
                    { // 상점 오류 시작
                        return false; // 중복 또는 손상 상점 차단
                    } // 상점 오류 끝
                } // 상점 검증 끝
            } // 상점 검사 끝
            if (version >= 12) // 개별 장비 저장 검사
            { // 개별 장비 검사 시작
                if (!EquipmentState.TryRestore(weaponId, armorId, accessoryId, bagId, weaponVariant, armorVariant, accessoryVariant, bagVariant, out _)) // 부위별 개별 장비와 기본 정의 일치 검사
                { // 장착 오류 시작
                    return false; // 잘못된 장착 장비 차단
                } // 장착 오류 끝
                var seenItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // 회차 전체 장비 인스턴스 중복 검사
                foreach (var variant in new[] { weaponVariant, armorVariant, accessoryVariant, bagVariant }) // 장착 장비 순회
                { // 장착 중복 검사 시작
                    if (variant != null && !seenItems.Add(variant.instanceId)) // 동일한 장비의 중복 장착 검사
                    { // 중복 오류 시작
                        return false; // 중복 인스턴스 차단
                    } // 중복 오류 끝
                } // 장착 중복 검사 끝
                if (hasPendingReward && pendingReward != null && pendingReward.equipment != null && !seenItems.Add(pendingReward.equipment.instanceId)) // 보상 후보와 이미 보유한 장비의 중복 검사
                { // 후보 중복 오류 시작
                    return false; // 이미 지급된 장비의 후보 차단
                } // 후보 중복 오류 끝
                foreach (var slot in slots) // 보유 슬롯의 개별 장비 순회
                { // 소지 중복 검사 시작
                    if (slot != null && slot.equipment != null && !seenItems.Add(slot.equipment.instanceId)) // 보유와 장착 및 슬롯 사이 중복 검사
                    { // 중복 오류 시작
                        return false; // 같은 장비의 중복 보유 차단
                    } // 중복 오류 끝
                } // 소지 중복 검사 끝
                if (version >= 13) // 상점에 남은 개별 장비 검사
                { // 상점 중복 검사 시작
                    foreach (var shop in shopStocks) // 검증한 층별 상점 순회
                    { // 상점 상품 검사 시작
                        foreach (var offer in shop.offers) // 실제 상품 순회
                        { // 상품 중복 검사 시작
                            if (offer.remaining > 0 && offer.equipment != null && !seenItems.Add(offer.equipment.instanceId)) // 재고로 남은 개별 장비의 중복 검사
                            { // 중복 오류 시작
                                return false; // 보유 또는 다른 상품과 중복 차단
                            } // 중복 오류 끝
                        } // 상품 중복 검사 끝
                    } // 상점 상품 검사 끝
                } // 상점 중복 검사 끝
            } // 개별 장비 검사 끝
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
            if (version >= 12 && hasPendingReward && pendingReward.equipment != null && (pendingReward.potionCount != 0 || !TrialEquipmentTraits.IsValid(pendingReward.equipment))) // 개별 장비 후보와 소비품 배타성 검사
            { // 보상 장비 오류 시작
                return false; // 손상된 장비와 중복 후보 차단
            } // 보상 장비 오류 끝
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
    [Serializable] // 층별 이벤트 직렬화 형식
    public sealed class SavedFloorTrialEvent // 이벤트 방과 상태 기록
    { // 클래스 시작
        public int floor; // 이벤트 층 번호
        public int x; // 이벤트 방 가로 좌표
        public int y; // 이벤트 방 세로 좌표
        public TrialEventSaveData eventData; // 고정 선택과 결과
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
