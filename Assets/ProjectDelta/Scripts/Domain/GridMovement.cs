namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public enum GridMoveInput // 상대 이동 입력 종류
    {
        Forward, // 전진 입력
        Backward, // 후진 입력
        Left, // 좌측 이동 입력
        Right // 우측 이동 입력
    }

    public enum CardinalDirection // 그리드 기준 바라보는 방향
    {
        North, // 북쪽 방향
        East, // 동쪽 방향
        South, // 남쪽 방향
        West // 서쪽 방향
    }

    public readonly struct GridBounds // 이동 가능한 그리드 범위
    {
        public int MinX { get; } // 최소 X 좌표
        public int MaxX { get; } // 최대 X 좌표
        public int MinZ { get; } // 최소 Z 좌표
        public int MaxZ { get; } // 최대 Z 좌표

        public GridBounds(int minX, int maxX, int minZ, int maxZ) // 범위 생성자
        {
            MinX = minX; // 최소 X 저장
            MaxX = maxX; // 최대 X 저장
            MinZ = minZ; // 최소 Z 저장
            MaxZ = maxZ; // 최대 Z 저장
        }

        public bool Contains(GridPosition position) // 좌표 포함 여부 검사
        {
            return position.X >= MinX && position.X <= MaxX && position.Z >= MinZ && position.Z <= MaxZ; // 범위 포함 결과 반환
        }
    }

    public static class GridMovement // 그리드 이동 규칙
    {
        public static CardinalDirection GetFacingFromYaw(float yawDegrees) // 시점 각도를 4방향으로 변환
        {
            float normalizedYaw = yawDegrees % 360f; // 각도 360도 범위 축소

            if (normalizedYaw < 0f) // 음수 각도 확인
            {
                normalizedYaw += 360f; // 양수 각도로 보정
            }

            if (normalizedYaw >= 315f || normalizedYaw < 45f) // 북쪽 구간 확인
            {
                return CardinalDirection.North; // 북쪽 반환
            }

            if (normalizedYaw < 135f) // 동쪽 구간 확인
            {
                return CardinalDirection.East; // 동쪽 반환
            }

            if (normalizedYaw < 225f) // 남쪽 구간 확인
            {
                return CardinalDirection.South; // 남쪽 반환
            }

            return CardinalDirection.West; // 서쪽 반환
        }

        public static GridPosition GetMoveDelta(CardinalDirection facing, GridMoveInput input) // 상대 입력을 좌표 변화량으로 변환
        {
            GridPosition forward = GetDirectionDelta(facing); // 정면 방향 변화량 계산
            GridPosition right = new GridPosition(forward.Z, -forward.X); // 우측 방향 변화량 계산

            switch (input) // 입력 종류 분기
            {
                case GridMoveInput.Forward: // 전진 입력 처리
                    return forward; // 정면 변화량 반환
                case GridMoveInput.Backward: // 후진 입력 처리
                    return new GridPosition(-forward.X, -forward.Z); // 반대 변화량 반환
                case GridMoveInput.Left: // 좌측 입력 처리
                    return new GridPosition(-right.X, -right.Z); // 좌측 변화량 반환
                case GridMoveInput.Right: // 우측 입력 처리
                    return right; // 우측 변화량 반환
                default: // 예외 입력 처리
                    return GridPosition.Zero; // 변화 없음 반환
            }
        }

        public static CardinalDirection GetMoveDirection(CardinalDirection facing, GridMoveInput input) // 상대 입력을 절대 방향으로 변환
        {
            GridPosition delta = GetMoveDelta(facing, input); // 입력 이동 변화량 계산

            if (delta.Z > 0) // 북쪽 변화량 확인
            {
                return CardinalDirection.North; // 북쪽 반환
            }

            if (delta.X > 0) // 동쪽 변화량 확인
            {
                return CardinalDirection.East; // 동쪽 반환
            }

            if (delta.Z < 0) // 남쪽 변화량 확인
            {
                return CardinalDirection.South; // 남쪽 반환
            }

            return CardinalDirection.West; // 서쪽 반환
        }

        public static GridPosition GetDirectionDelta(CardinalDirection direction) // 절대 방향 변화량 계산
        {
            switch (direction) // 방향 분기
            {
                case CardinalDirection.North: // 북쪽 처리
                    return new GridPosition(0, 1); // 북쪽 변화량 반환
                case CardinalDirection.East: // 동쪽 처리
                    return new GridPosition(1, 0); // 동쪽 변화량 반환
                case CardinalDirection.South: // 남쪽 처리
                    return new GridPosition(0, -1); // 남쪽 변화량 반환
                case CardinalDirection.West: // 서쪽 처리
                    return new GridPosition(-1, 0); // 서쪽 변화량 반환
                default: // 예외 방향 처리
                    return GridPosition.Zero; // 변화 없음 반환
            }
        }

        public static bool TryGetTarget(GridPosition current, CardinalDirection facing, GridMoveInput input, GridBounds bounds, out GridPosition target) // 목표 칸 이동 가능 여부 계산
        {
            GridPosition delta = GetMoveDelta(facing, input); // 이동 변화량 계산
            target = new GridPosition(current.X + delta.X, current.Z + delta.Z); // 목표 좌표 계산
            return bounds.Contains(target); // 이동 가능 여부 반환
        }
    }
}
