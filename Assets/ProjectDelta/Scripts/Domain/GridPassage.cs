namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public enum PassageType // 그리드 통로 종류
    {
        Open, // 일반 통로
        Wall, // 벽 통로
        Door // 문 통로
    }

    public enum DoorOpenResult // 문 열기 결과
    {
        NotDoor, // 문 아님
        AlreadyOpen, // 이미 열린 문
        Opened, // 문 열기 성공
        LockedNoKey // 열쇠 부족
    }

    public sealed class GridPassage // 두 그리드 칸 사이 통로 상태
    {
        public PassageType Type { get; } // 통로 종류
        public bool IsLocked { get; private set; } // 문 잠금 여부
        public bool IsOpen { get; private set; } // 문 열림 여부

        private GridPassage(PassageType type, bool isLocked, bool isOpen) // 통로 생성자
        {
            Type = type; // 통로 종류 저장
            IsLocked = isLocked; // 잠금 상태 저장
            IsOpen = isOpen; // 열림 상태 저장
        }

        public static GridPassage CreateOpen() // 일반 통로 생성
        {
            return new GridPassage(PassageType.Open, false, true); // 열린 통로 반환
        }

        public static GridPassage CreateWall() // 벽 통로 생성
        {
            return new GridPassage(PassageType.Wall, false, false); // 막힌 벽 반환
        }

        public static GridPassage CreateDoor(bool isLocked) // 문 통로 생성
        {
            return new GridPassage(PassageType.Door, isLocked, false); // 닫힌 문 반환
        }

        public bool CanPass() // 현재 통과 가능 여부 검사
        {
            if (Type == PassageType.Open) // 일반 통로 확인
            {
                return true; // 일반 통로 통과 허용
            }

            if (Type == PassageType.Door && IsOpen) // 열린 문 확인
            {
                return true; // 열린 문 통과 허용
            }

            return false; // 벽 또는 닫힌 문 통과 차단
        }

        public DoorOpenResult TryOpenDoor(PlayerRunState playerState) // 문 열기 시도
        {
            if (Type != PassageType.Door) // 문 종류 확인
            {
                return DoorOpenResult.NotDoor; // 문 아님 반환
            }

            if (IsOpen) // 이미 열린 문 확인
            {
                return DoorOpenResult.AlreadyOpen; // 이미 열림 반환
            }

            if (IsLocked) // 잠긴 문 확인
            {
                if (playerState == null || playerState.KeyCount <= 0) // 보유 열쇠 확인
                {
                    return DoorOpenResult.LockedNoKey; // 열쇠 부족 반환
                }

                playerState.KeyCount -= 1; // 열쇠 한 개 소모
                IsLocked = false; // 문 잠금 해제
            }

            IsOpen = true; // 문 열림 상태 적용
            return DoorOpenResult.Opened; // 문 열기 성공 반환
        }
    }
}
