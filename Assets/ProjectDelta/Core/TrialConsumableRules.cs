using System; // 안전한 회복량 계산
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public enum TrialConsumableResource // 시험 소비품의 회복 자원
    { // 자원 종류 시작
        Health, // 체력 자원
        Mana // 마나 자원
    } // 자원 종류 끝
    public sealed class TrialConsumableDefinition // 변경 불가능한 시험 아이템 정의
    { // 정의 클래스 시작
        public string Id { get; } // 저장용 아이템 식별자
        public string Name { get; } // 아이템 표시 이름
        public TrialConsumableResource Resource { get; } // 회복할 자원 종류
        public int Recovery { get; } // 시험 회복량
        public int StackLimit => 5; // 미확정 시험 중첩 한도
        internal TrialConsumableDefinition(string id, string name, TrialConsumableResource resource, int recovery) // 시험 아이템 정의 생성
        { // 생성 시작
            Id = id; // 식별자 저장
            Name = name; // 표시 이름 저장
            Resource = resource; // 자원 종류 저장
            Recovery = recovery; // 시험 회복량 저장
        } // 생성 끝
    } // 정의 클래스 끝
    public static class TrialConsumableRules // 시험 소비품 목록과 사용 판정
    { // 규칙 클래스 시작
        private static readonly TrialConsumableDefinition small = new TrialConsumableDefinition("trial-small-potion", "소형 회복약", TrialConsumableResource.Health, 25); // 소형 체력 회복약
        private static readonly TrialConsumableDefinition large = new TrialConsumableDefinition("trial-large-potion", "대형 회복약", TrialConsumableResource.Health, 60); // 대형 체력 회복약
        private static readonly TrialConsumableDefinition mana = new TrialConsumableDefinition("trial-mana-potion", "마나 회복약", TrialConsumableResource.Mana, 30); // 마나 회복약
        public static TrialConsumableDefinition Find(string id) // 시험 아이템 식별자 조회
        { // 조회 시작
            if (id == small.Id) // 소형 회복약 식별자 검사
            { // 소형 아이템 분기 시작
                return small; // 소형 회복약 전달
            } // 소형 아이템 분기 끝
            if (id == large.Id) // 대형 회복약 식별자 검사
            { // 대형 아이템 분기 시작
                return large; // 대형 회복약 전달
            } // 대형 아이템 분기 끝
            if (id == mana.Id) // 마나 회복약 식별자 검사
            { // 마나 아이템 분기 시작
                return mana; // 마나 회복약 전달
            } // 마나 아이템 분기 끝
            return null; // 미등록 식별자 거절
        } // 조회 끝
        public static bool TryUse(InventoryState inventory, int slotIndex, int current, int maximum, out int recovered) // 선택 슬롯 소비와 대상 자원 회복
        { // 사용 시작
            recovered = current; // 실패 시 원래 자원 보존
            if (inventory == null || slotIndex < 0 || slotIndex >= inventory.Slots.Count || maximum <= 0 || current < 0 || current >= maximum) // 슬롯과 자원 범위 검사
            { // 잘못된 사용 분기 시작
                return false; // 아이템 소비 차단
            } // 잘못된 사용 분기 끝
            var item = Find(inventory.Slots[slotIndex].ItemId); // 선택된 아이템 정의 조회
            if (item == null || (item.Resource == TrialConsumableResource.Health && current == 0)) // 미등록 아이템과 체력 소진 검사
            { // 사용 불가 분기 시작
                return false; // 아이템 소비 차단
            } // 사용 불가 분기 끝
            var target = (int)Math.Min((long)maximum, (long)current + item.Recovery); // 정수 넘침 없는 회복 제한
            if (!inventory.Remove(slotIndex, 1, out var removed) || removed != 1) // 선택 슬롯의 단일 소비 검사
            { // 소비 실패 분기 시작
                return false; // 기존 자원 유지
            } // 소비 실패 분기 끝
            recovered = target; // 확인된 회복 결과 전달
            return true; // 소비와 회복 성공
        } // 사용 끝
    } // 규칙 클래스 끝
} // 이름 공간 끝
