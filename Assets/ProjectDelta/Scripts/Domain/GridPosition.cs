using System; // 직렬화 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    [Serializable] // 좌표 데이터 직렬화 지원
    public struct GridPosition // 플레이어 그리드 좌표 값
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
    }
}
