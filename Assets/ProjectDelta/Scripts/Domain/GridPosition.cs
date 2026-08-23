using System; // 직렬화 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    [Serializable] // 좌표 데이터 직렬화 지원
    public struct GridPosition : IEquatable<GridPosition> // 플레이어 그리드 좌표 값 (28일차: Dictionary 키로 쓰기 위해 IEquatable 추가)
    {
        public int X; // 가로 그리드 좌표
        public int Z; // 세로 그리드 좌표

        public static GridPosition Zero => new GridPosition(0, 0); // 원점 좌표

        public GridPosition(int x, int z) // 좌표 생성자
        {
            X = x; // 가로 좌표 저장
            Z = z; // 세로 좌표 저장
        }

        public override string ToString() // 디버그 문자열 변환
        {
            return $"({X}, {Z})"; // 좌표 문자열 반환
        }

        public bool Equals(GridPosition other) // 좌표값 동등 비교 (28일차)
        {
            return X == other.X && Z == other.Z; // X·Z 모두 같은지 반환
        }

        public override bool Equals(object obj) // 객체 기반 동등 비교
        {
            return obj is GridPosition other && Equals(other); // 좌표 비교 결과 반환
        }

        public override int GetHashCode() // 해시 계산 (28일차)
        {
            unchecked // 정수 오버플로 허용
            {
                return (X * 397) ^ Z; // X·Z 조합 해시 반환
            }
        }

        public static bool operator ==(GridPosition left, GridPosition right) // 동등 연산자
        {
            return left.Equals(right); // 동등 비교 결과 반환
        }

        public static bool operator !=(GridPosition left, GridPosition right) // 부등 연산자
        {
            return !left.Equals(right); // 부등 비교 결과 반환
        }
    }
}
