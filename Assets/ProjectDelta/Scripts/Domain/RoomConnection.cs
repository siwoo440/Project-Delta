namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public readonly struct RoomConnectionEnd // 방 연결 한쪽 끝 정보
    {
        public string RoomId { get; } // 방 식별자
        public GridPosition BoundaryPosition { get; } // 방 경계 안쪽 칸
        public CardinalDirection ExitDirection { get; } // 방에서 나가는 방향

        public RoomConnectionEnd(string roomId, GridPosition boundaryPosition, CardinalDirection exitDirection) // 연결 끝 생성자
        {
            RoomId = roomId; // 방 식별자 저장
            BoundaryPosition = boundaryPosition; // 경계 칸 저장
            ExitDirection = exitDirection; // 출구 방향 저장
        }
    }

    public sealed class RoomConnection // 두 테스트 방 사이 연결 규칙
    {
        public RoomConnectionEnd EndA { get; } // 첫 번째 방 연결 끝
        public RoomConnectionEnd EndB { get; } // 두 번째 방 연결 끝

        public RoomConnection(RoomConnectionEnd endA, RoomConnectionEnd endB) // 양방향 방 연결 생성자
        {
            EndA = endA; // 첫 번째 연결 끝 저장
            EndB = endB; // 두 번째 연결 끝 저장
        }

        public bool TryGetDestination(string currentRoomId, GridPosition currentPosition, CardinalDirection moveDirection, out string destinationRoomId, out GridPosition destinationEntryPosition) // 현재 방 경계 이동 대상 조회
        {
            if (Matches(EndA, currentRoomId, currentPosition, moveDirection)) // A방 출구 조건 확인
            {
                destinationRoomId = EndB.RoomId; // B방 식별자 반환
                destinationEntryPosition = EndB.BoundaryPosition; // B방 입구 칸 반환
                return true; // 연결 성공 반환
            }

            if (Matches(EndB, currentRoomId, currentPosition, moveDirection)) // B방 출구 조건 확인
            {
                destinationRoomId = EndA.RoomId; // A방 식별자 반환
                destinationEntryPosition = EndA.BoundaryPosition; // A방 입구 칸 반환
                return true; // 연결 성공 반환
            }

            destinationRoomId = null; // 목적 방 없음 처리
            destinationEntryPosition = GridPosition.Zero; // 목적 위치 초기화
            return false; // 연결 실패 반환
        }

        private static bool Matches(RoomConnectionEnd end, string roomId, GridPosition position, CardinalDirection direction) // 연결 끝 조건 일치 검사
        {
            return end.RoomId == roomId && end.BoundaryPosition.X == position.X && end.BoundaryPosition.Z == position.Z && end.ExitDirection == direction; // 방·위치·방향 일치 결과 반환
        }
    }
}
