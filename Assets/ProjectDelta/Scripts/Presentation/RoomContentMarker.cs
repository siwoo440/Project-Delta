using ProjectDelta.Domain; // 그리드 좌표 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation
{
    public enum RoomContentType
    {
        Stairs,
        Chest,
        SecretWall,
        NpcPoint,
        AmbientProp
    }

    public sealed class RoomContentMarker : MonoBehaviour
    {
        [SerializeField] private RoomContentType contentType;
        [SerializeField] private int gridX;
        [SerializeField] private int gridZ;

        public RoomContentType ContentType => contentType;
        public GridPosition GridPosition => new GridPosition(gridX, gridZ);

        // 36일차: 절차 생성된 방에 런타임 콘텐츠 마커를 만들 때 사용한다.
        public void Configure(RoomContentType type, GridPosition position)
        {
            contentType = type;
            gridX = position.X;
            gridZ = position.Z;
        }
    }
}
