namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public enum DungeonRoomRole // 던전 생성 단계에서 사용하는 방 역할
    {
        MainPath, // 시작→계단 메인 경로
        Branch, // 가지 경로 중간 방
        DeadEndCandidate, // 일반 막다른 방 후보
        SpecialCandidate // 상점·휴식·이벤트 등에 사용할 특수 방 후보
    }
}
