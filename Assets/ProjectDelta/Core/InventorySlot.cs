namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class InventorySlot // 아이템 한 묶음의 슬롯
    { // 클래스 시작
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
