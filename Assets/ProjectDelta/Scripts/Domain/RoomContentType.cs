namespace ProjectDelta.Domain
{
    // 기존 Presentation enum의 직렬화 값(0~4)을 유지하고 Monster를 뒤에 추가한다.
    public enum RoomContentType
    {
        Stairs = 0,
        Chest = 1,
        SecretWall = 2,
        NpcPoint = 3,
        AmbientProp = 4,
        Monster = 5
    }
}
