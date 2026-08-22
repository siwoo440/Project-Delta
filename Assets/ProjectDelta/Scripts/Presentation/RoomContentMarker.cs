using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 기획서 10.3절 "방 표현"이 나열한 콘텐츠 종류.
    // TODO: 이후 생성 시스템(Domain/Application)이 이 종류를 직접 다뤄야 하면
    // Domain으로 옮긴다. 지금은 화면에 자리만 표시하는 용도라 Presentation에 둔다.
    public enum RoomContentType // 방 콘텐츠 배치 지점 종류
    {
        Stairs, // 계단
        Chest, // 상자
        SecretWall, // 비밀 벽
        NpcPoint, // NPC 상호작용 지점
        AmbientProp // 환경 소품
    }

    // 콘텐츠가 나중에 배치될 빈 자리를 표시만 한다. 스스로 아무것도 생성하거나 판정하지 않는다.
    public sealed class RoomContentMarker : MonoBehaviour // 방 콘텐츠 배치 지점 표시
    {
        [SerializeField] private RoomContentType contentType; // 이 자리에 배치될 콘텐츠 종류

        public RoomContentType ContentType => contentType; // 콘텐츠 종류 공개
    }
}
