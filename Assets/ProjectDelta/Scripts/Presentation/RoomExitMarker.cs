using ProjectDelta.Domain; // 출구 좌표·방향 데이터 사용
using UnityEngine; // MonoBehaviour 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    [DisallowMultipleComponent] // 한 문 위치에 마커 중복 방지
    public sealed class RoomExitMarker : MonoBehaviour // RoomView 프리팹의 실제 경계 출구 위치 표시
    {
        [SerializeField] private GridPosition localPosition; // RoomDefinition 기준 출구 칸 좌표
        [SerializeField] private CardinalDirection direction; // 방 바깥으로 나가는 방향

        public GridPosition LocalPosition => localPosition; // 출구 로컬 좌표 공개
        public CardinalDirection Direction => direction; // 출구 방향 공개
        public RoomExit Exit => new RoomExit(localPosition, direction); // Domain 출구 값으로 변환

        public void Configure(GridPosition position, CardinalDirection exitDirection) // Editor 생성기용 설정
        {
            localPosition = position; // 출구 좌표 저장
            direction = exitDirection; // 출구 방향 저장
        }
    }
}
