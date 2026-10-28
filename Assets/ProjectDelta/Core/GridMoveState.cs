using System; // 이동 가능 여부 함수
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class GridMoveState // 격자 이동 상태
    { // 클래스 시작
        private readonly Func<int, int, bool> canEnter; // 목적 칸 통행 판정
        private MoveDirection bufferedDirection; // 대기 중인 한 방향
        public int X // 현재 가로 칸
        { // 속성 시작
            get; // 현재 값 조회
            private set; // 내부 위치 변경
        } // 속성 끝
        public int Y // 현재 세로 칸
        { // 속성 시작
            get; // 현재 값 조회
            private set; // 내부 위치 변경
        } // 속성 끝
        public MoveDirection Facing // 현재 바라보는 방향
        { // 속성 시작
            get; // 방향 조회
            private set; // 내부 방향 변경
        } = MoveDirection.Down; // 최초 아래쪽 방향
        public bool IsMoving // 화면 이동 진행 여부
        { // 속성 시작
            get; // 이동 상태 조회
            private set; // 내부 이동 상태 변경
        } // 속성 끝
        public GridMoveState(int x, int y, Func<int, int, bool> canEnter, MoveDirection facing = MoveDirection.Down) // 시작 칸과 통행 규칙
        { // 생성 시작
            X = x; // 시작 가로 칸
            Y = y; // 시작 세로 칸
            Facing = facing; // 시작 방향 설정
            this.canEnter = canEnter ?? throw new ArgumentNullException(nameof(canEnter)); // 유효한 통행 판정 보관
        } // 생성 끝
        public MoveRequestResult RequestMove(MoveDirection direction) // 이동 요청
        { // 요청 시작
            if (direction != MoveDirection.Up && direction != MoveDirection.Down && direction != MoveDirection.Left && direction != MoveDirection.Right) // 네 방향 유효성 검사
            { // 잘못된 방향 조건 시작
                return MoveRequestResult.Rejected; // 잘못된 입력 거절
            } // 잘못된 방향 조건 끝
            if (IsMoving) // 현재 화면 이동 검사
            { // 이동 중 조건 시작
                if (bufferedDirection != MoveDirection.None) // 이미 대기 중인 입력 검사
                { // 대기 가득 참 조건 시작
                    return MoveRequestResult.Rejected; // 추가 대기 입력 거절
                } // 대기 가득 참 조건 끝
                bufferedDirection = direction; // 첫 방향 하나만 보관
                return MoveRequestResult.Buffered; // 대기 입력 접수
            } // 이동 중 조건 끝
            return StartMove(direction) ? MoveRequestResult.Started : MoveRequestResult.Rejected; // 통행 여부에 따른 시작 결과
        } // 요청 끝
        public bool FinishMove() // 현재 이동 완료
        { // 완료 시작
            if (!IsMoving) // 진행 중인 이동 검사
            { // 대기 상태 조건 시작
                return false; // 완료할 이동 없음
            } // 대기 상태 조건 끝
            IsMoving = false; // 현재 이동 완료 처리
            var nextDirection = bufferedDirection; // 대기 입력 보관
            bufferedDirection = MoveDirection.None; // 대기 입력 슬롯 비우기
            return nextDirection != MoveDirection.None && StartMove(nextDirection); // 대기 이동 가능 여부와 시작
        } // 완료 끝
        public void DiscardBufferedMove() // 대기 입력 제거
        { // 제거 시작
            bufferedDirection = MoveDirection.None; // 대기 중인 입력 제거
        } // 제거 끝
        private bool StartMove(MoveDirection direction) // 목적 칸 검사와 이동 확정
        { // 시작 처리 시작
            var nextX = X; // 현재 가로 칸 복사
            var nextY = Y; // 현재 세로 칸 복사
            switch (direction) // 방향별 목적 칸 계산
            { // 방향 선택 시작
                case MoveDirection.Up: // 위쪽 이동 선택
                    nextY++; // 세로 칸 증가
                    break; // 방향 계산 종료
                case MoveDirection.Down: // 아래쪽 이동 선택
                    nextY--; // 세로 칸 감소
                    break; // 방향 계산 종료
                case MoveDirection.Left: // 왼쪽 이동 선택
                    nextX--; // 가로 칸 감소
                    break; // 방향 계산 종료
                case MoveDirection.Right: // 오른쪽 이동 선택
                    nextX++; // 가로 칸 증가
                    break; // 방향 계산 종료
                default: // 허용하지 않은 방향
                    return false; // 이동 거절
            } // 방향 선택 끝
            if (!canEnter(nextX, nextY)) // 벽과 장애물 검사
            { // 이동 불가 조건 시작
                return false; // 기존 위치와 방향 유지
            } // 이동 불가 조건 끝
            X = nextX; // 가로 목적 칸 확정
            Y = nextY; // 세로 목적 칸 확정
            Facing = direction; // 표시 방향 확정
            IsMoving = true; // 화면 이동 시작
            return true; // 이동 성공 전달
        } // 시작 처리 끝
    } // 클래스 끝
} // 이름 공간 끝
