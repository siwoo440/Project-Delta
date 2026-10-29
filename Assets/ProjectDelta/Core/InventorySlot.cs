namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class InventorySlot // 아이템 한 묶음의 슬롯
    { // 클래스 시작
        private EquipmentVariantData equipment; // 내부 개별 장비 정보
        public EquipmentVariantData Equipment // 외부 변경을 차단하는 개별 장비 조회
        { // 속성 시작
            get => EquipmentVariantRules.Copy(equipment); // 독립 장비 복사 반환
            internal set => equipment = EquipmentVariantRules.Copy(value); // 외부 입력의 독립 복사 보관
        } // 속성 끝
        public string ItemId // 아이템 식별자
        { // 속성 시작
            get; // 식별자 조회
            internal set; // 내부 식별자 변경
        } = string.Empty; // 빈 슬롯 초기화
        public string Name // 표시 이름
        { // 속성 시작
            get; // 이름 조회
            internal set; // 내부 이름 변경
        } = string.Empty; // 빈 이름 초기화
        public int Quantity // 현재 수량
        { // 속성 시작
            get; // 수량 조회
            internal set; // 내부 수량 변경
        } // 속성 끝
        public int StackLimit // 슬롯별 최대 중첩 수량
        { // 속성 시작
            get; // 최대 수량 조회
            internal set; // 내부 최대 수량 변경
        } = 1; // 기본 최대 수량
        public bool IsEmpty => string.IsNullOrEmpty(ItemId); // 빈 슬롯 여부
    } // 클래스 끝
} // 이름 공간 끝
