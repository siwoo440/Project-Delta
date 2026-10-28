namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public enum MoveRequestResult // 이동 요청 결과
    { // 결과 목록 시작
        Rejected, // 이동 요청 거절
        Started, // 새 이동 시작
        Buffered // 한 칸 입력 대기
    } // 결과 목록 끝
} // 이름 공간 끝
