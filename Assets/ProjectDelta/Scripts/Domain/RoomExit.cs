using System; // 값 비교 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 30일차: 던전 생성에서 사용할 방 경계 출구 정보.
    // 기존 CardinalDirection만 보관하던 구조에서 출구의 실제 방 내부 좌표까지 함께 보관한다.
    public readonly struct RoomExit : IEquatable<RoomExit> // 방 경계 출구 값
    {
        public GridPosition LocalPosition { get; } // RoomDefinition 기준 방 내부 출구 좌표
        public CardinalDirection Direction { get; } // 방 바깥으로 나가는 방향

        public RoomExit(GridPosition localPosition, CardinalDirection direction) // 출구 정보 생성자
        {
            LocalPosition = localPosition; // 출구 좌표 저장
            Direction = direction; // 출구 방향 저장
        }

        // 방향이 반대이고, 서로 맞닿는 축의 위치가 같은지 확인한다.
        // North/South는 X가 같아야 하고 East/West는 Z가 같아야 한다.
        // 실제 방 프리팹 배치·문 정렬 검증은 이후 일차에서 이 규칙을 사용한다.
        public bool CanConnectTo(RoomExit other) // 다른 출구와 연결 가능한지 확인
        {
            if (RoomGridLayout.GetOpposite(Direction) != other.Direction) // 서로 반대 방향인지 확인
            {
                return false; // 반대 방향이 아니면 연결 불가
            }

            switch (Direction) // 현재 출구 방향에 따라 맞춰야 하는 축 선택
            {
                case CardinalDirection.North:
                case CardinalDirection.South:
                    return LocalPosition.X == other.LocalPosition.X; // 북/남 연결은 X 위치 일치 필요

                case CardinalDirection.East:
                case CardinalDirection.West:
                    return LocalPosition.Z == other.LocalPosition.Z; // 동/서 연결은 Z 위치 일치 필요

                default:
                    return false; // 정의되지 않은 방향은 연결 불가
            }
        }

        public bool Equals(RoomExit other) // 출구 값 비교
        {
            return LocalPosition.Equals(other.LocalPosition) && Direction == other.Direction; // 좌표와 방향이 모두 같은지 확인
        }

        public override bool Equals(object obj) // 객체 기반 값 비교
        {
            return obj is RoomExit other && Equals(other); // RoomExit인지 확인 후 값 비교
        }

        public override int GetHashCode() // 해시 계산
        {
            unchecked
            {
                return (LocalPosition.GetHashCode() * 397) ^ (int)Direction; // 좌표와 방향 조합
            }
        }

        public static bool operator ==(RoomExit left, RoomExit right) // 동등 연산자
        {
            return left.Equals(right); // 동등 비교 결과 반환
        }

        public static bool operator !=(RoomExit left, RoomExit right) // 부등 연산자
        {
            return !left.Equals(right); // 부등 비교 결과 반환
        }

        public override string ToString() // 디버그 문자열
        {
            return $"{LocalPosition} / {Direction}"; // 좌표와 방향 표시
        }
    }
}
