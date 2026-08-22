using System; // 값 비교 기능 사용
using System.Collections.Generic; // 통로 사전 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public sealed class RoomGridLayout // 방 내부 방향별 통로 데이터
    {
        private readonly Dictionary<PassageKey, GridPassage> passages = new Dictionary<PassageKey, GridPassage>(); // 방향별 통로 저장소

        public void SetPassage(GridPosition position, CardinalDirection direction, GridPassage passage) // 양방향 통로 설정
        {
            PassageKey forwardKey = new PassageKey(position, direction); // 현재 칸 통로 키 생성
            passages[forwardKey] = passage; // 현재 칸 통로 저장
            GridPosition delta = GridMovement.GetDirectionDelta(direction); // 인접 칸 변화량 계산
            GridPosition neighbor = new GridPosition(position.X + delta.X, position.Z + delta.Z); // 인접 칸 좌표 계산
            CardinalDirection opposite = GetOpposite(direction); // 반대 방향 계산
            PassageKey backwardKey = new PassageKey(neighbor, opposite); // 인접 칸 반대 통로 키 생성
            passages[backwardKey] = passage; // 동일 통로 객체 공유
        }

        public GridPassage GetPassage(GridPosition position, CardinalDirection direction) // 방향 통로 조회
        {
            PassageKey key = new PassageKey(position, direction); // 통로 조회 키 생성

            if (passages.TryGetValue(key, out GridPassage passage)) // 등록 통로 확인
            {
                return passage; // 등록 통로 반환
            }

            return GridPassage.CreateOpen(); // 미등록 내부 통로 기본 허용
        }

        public bool CanPass(GridPosition position, CardinalDirection direction) // 방향 통과 가능 여부 검사
        {
            GridPassage passage = GetPassage(position, direction); // 방향 통로 조회
            return passage.CanPass(); // 통과 가능 여부 반환
        }

        public static CardinalDirection GetOpposite(CardinalDirection direction) // 반대 방향 계산
        {
            switch (direction) // 방향 분기
            {
                case CardinalDirection.North: // 북쪽 처리
                    return CardinalDirection.South; // 남쪽 반환
                case CardinalDirection.East: // 동쪽 처리
                    return CardinalDirection.West; // 서쪽 반환
                case CardinalDirection.South: // 남쪽 처리
                    return CardinalDirection.North; // 북쪽 반환
                case CardinalDirection.West: // 서쪽 처리
                    return CardinalDirection.East; // 동쪽 반환
                default: // 예외 방향 처리
                    return CardinalDirection.North; // 기본 북쪽 반환
            }
        }

        private readonly struct PassageKey : IEquatable<PassageKey> // 통로 사전 키
        {
            private readonly int x; // 기준 X 좌표
            private readonly int z; // 기준 Z 좌표
            private readonly CardinalDirection direction; // 기준 방향

            public PassageKey(GridPosition position, CardinalDirection passageDirection) // 통로 키 생성자
            {
                x = position.X; // X 좌표 저장
                z = position.Z; // Z 좌표 저장
                direction = passageDirection; // 방향 저장
            }

            public bool Equals(PassageKey other) // 통로 키 값 비교
            {
                return x == other.x && z == other.z && direction == other.direction; // 동일 키 여부 반환
            }

            public override bool Equals(object obj) // 객체 기반 값 비교
            {
                return obj is PassageKey other && Equals(other); // 통로 키 비교 결과 반환
            }

            public override int GetHashCode() // 통로 키 해시 계산
            {
                unchecked // 정수 오버플로 허용
                {
                    int hash = 17; // 초기 해시 값
                    hash = (hash * 31) + x; // X 좌표 해시 반영
                    hash = (hash * 31) + z; // Z 좌표 해시 반영
                    hash = (hash * 31) + (int)direction; // 방향 해시 반영
                    return hash; // 최종 해시 반환
                }
            }
        }
    }
}
