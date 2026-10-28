namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class InventoryAddResult // 아이템 획득 결과
    { // 클래스 시작
        public int Added // 실제 추가 수량
        { // 속성 시작
            get; // 추가 수량 조회
        } // 속성 끝
        public int Remaining // 넣지 못한 수량
        { // 속성 시작
            get; // 남은 수량 조회
        } // 속성 끝
        public InventoryAddResult(int added, int remaining) // 획득 결과 생성
        { // 생성 시작
            Added = added; // 실제 추가 수량 보관
            Remaining = remaining; // 남은 수량 보관
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
