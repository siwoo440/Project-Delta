namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 기존 Presentation enum의 직렬화 값(0~4)을 유지하고 Monster를 뒤에 추가한다.
    public enum RoomContentType // 방 안에 배치되는 콘텐츠 종류
    {
        Stairs = 0, // 다음 층 계단
        Chest = 1, // 보물 상자
        SecretWall = 2, // 비밀 벽
        NpcPoint = 3, // NPC 배치 지점
        AmbientProp = 4, // 분위기용 장식 소품
        Monster = 5 // 몬스터
    }
}
