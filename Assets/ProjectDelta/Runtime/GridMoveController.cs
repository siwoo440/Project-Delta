using ProjectDelta.Core; // 격자 이동 규칙
using UnityEngine; // 실제 씬 구성 요소
using UnityEngine.InputSystem; // 연속 이동 키 확인
using UnityEngine.Tilemaps; // 바닥과 장애물 타일
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class GridMoveController : MonoBehaviour // 플레이어 이동 표시
    { // 클래스 시작
        [SerializeField] private Tilemap floor; // 이동 가능한 바닥
        [SerializeField] private Tilemap obstacles; // 이동을 막는 칸
        [SerializeField] private Transform directionIndicator; // 표시 방향 화살표
        [SerializeField, Min(0.01f)] private float moveDuration = 0.12f; // 한 칸 이동 시간
        private GridMoveState state; // 순수 격자 이동 상태
        private GameInputRouter input; // 현재 탐험 입력
        private Vector3 animationStart; // 화면 이동 시작 위치
        private Vector3 animationTarget; // 화면 이동 목표 위치
        private float elapsed; // 현재 이동 경과 시간
        private int roomTransitionFrame = -1; // 방 전환 직후 입력 차단 프레임
        public event System.Action StepArrived; // 한 칸 도착 알림
        public int X => state.X; // 확정된 가로 칸
        public int Y => state.Y; // 확정된 세로 칸
        public MoveDirection Facing => state.Facing; // 현재 표시 방향
        public bool IsMoving => state.IsMoving; // 현재 이동 진행 여부
        public Bounds RoomWorldBounds // 현재 방 바닥의 월드 범위
        { // 속성 시작
            get // 월드 범위 조회
            { // 조회 시작
                if (floor == null) // 바닥 연결 검사
                { // 누락 조건 시작
                    return new Bounds(transform.position, Vector3.zero); // 현재 위치의 빈 범위
                } // 누락 조건 끝
                var cells = floor.cellBounds; // 바닥의 실제 격자 범위
                var minimum = floor.CellToWorld(new Vector3Int(cells.xMin, cells.yMin, 0)); // 격자 최소 월드 좌표
                var maximum = floor.CellToWorld(new Vector3Int(cells.xMax, cells.yMax, 0)); // 격자 최대 월드 좌표
                return new Bounds((minimum + maximum) * 0.5f, new Vector3(Mathf.Abs(maximum.x - minimum.x), Mathf.Abs(maximum.y - minimum.y), 0f)); // 실제 방 중심과 크기
            } // 조회 끝
        } // 속성 끝
        public bool IsRoomTransitionLocked // 부드러운 방 전환 중 이동 잠금
        { // 속성 시작
            get; // 현재 방 전환 잠금 조회
            private set; // 내부 방 전환 잠금 설정
        } // 속성 끝
        public void SetRoomTransitionLocked(bool locked) // 방 전환에 따른 이동 잠금 설정
        { // 설정 시작
            IsRoomTransitionLocked = locked; // 실제 이동 잠금 변경
            if (locked) // 새 방 전환 잠금 검사
            { // 잠금 조건 시작
                state?.DiscardBufferedMove(); // 전환 전 대기 이동 제거
            } // 잠금 조건 끝
        } // 설정 끝
        public void Configure(Tilemap floorMap, Tilemap obstacleMap, Transform indicator) // 씬 제작 시 구성 요소 연결
        { // 연결 시작
            floor = floorMap; // 바닥 타일맵 연결
            obstacles = obstacleMap; // 장애물 타일맵 연결
            directionIndicator = indicator; // 방향 화살표 연결
        } // 연결 끝
        public bool CanArrive(Tilemap floorMap, Tilemap obstacleMap, int x, int y) // 새 방 도착 칸의 안전성 검사
        { // 판정 시작
            var cell = new Vector3Int(x, y, 0); // 도착할 격자 칸
            return floorMap != null && obstacleMap != null && floorMap.HasTile(cell) && !obstacleMap.HasTile(cell); // 바닥과 장애물 검사
        } // 판정 끝
        public bool ChangeRoom(Tilemap floorMap, Tilemap obstacleMap, int x, int y) // 이동이 끝난 플레이어의 방 전환
        { // 전환 시작
            if (state == null || state.IsMoving || !CanArrive(floorMap, obstacleMap, x, y)) // 이동 상태와 도착 칸 검사
            { // 전환 불가 조건 시작
                return false; // 기존 플레이어 위치 유지
            } // 전환 불가 조건 끝
            var facing = state.Facing; // 이전 이동 방향 보관
            floor = floorMap; // 새 방 바닥 연결
            obstacles = obstacleMap; // 새 방 장애물 연결
            state = new GridMoveState(x, y, CanEnter, facing); // 새 방 내부 좌표와 방향 설정
            transform.position = floor.GetCellCenterWorld(new Vector3Int(x, y, 0)); // 도착 칸 중심으로 배치
            roomTransitionFrame = Time.frameCount; // 같은 프레임 이동 입력 차단
            UpdateDirection(); // 기존 방향 표시 유지
            return true; // 방 전환 성공
        } // 전환 끝
        public bool RestoreFacing(MoveDirection direction) // 저장된 방향 복원
        { // 복원 시작
            if (state == null || state.IsMoving || direction < MoveDirection.None || direction > MoveDirection.Right) // 이동과 방향 범위 검사
            { // 거절 조건 시작
                return false; // 잘못된 방향 거절
            } // 거절 조건 끝
            state = new GridMoveState(state.X, state.Y, CanEnter, direction); // 현재 위치와 저장 방향 적용
            UpdateDirection(); // 방향 이미지 갱신
            return true; // 방향 복원 성공
        } // 복원 끝
        private void Awake() // 실제 던전 상태 초기화
        { // 초기화 시작
            if (floor == null || obstacles == null || directionIndicator == null || AppRoot.Instance == null) // 필수 씬 연결 검사
            { // 설정 오류 조건 시작
                Debug.LogError("Grid movement requires the dungeon maps, direction indicator, and AppRoot."); // 누락된 연결 기록
                enabled = false; // 잘못된 이동 실행 중단
                return; // 추가 초기화 중단
            } // 설정 오류 조건 끝
            var cell = floor.WorldToCell(transform.position); // 시작 화면 위치의 격자 칸
            state = new GridMoveState(cell.x, cell.y, CanEnter); // 새 격자 상태 생성
            input = AppRoot.Instance.Input; // 실제 탐험 입력 연결
            transform.position = floor.GetCellCenterWorld(cell); // 시작 위치를 칸 중심에 정렬
            UpdateDirection(); // 최초 아래쪽 방향 표시
        } // 초기화 끝
        private void OnEnable() // 탐험 입력 구독
        { // 구독 시작
            if (input != null) // 공통 입력 준비 검사
            { // 구독 조건 시작
                input.MoveRequested += OnMoveRequested; // 한 칸 이동 요청 연결
                input.ExplorationLocked += OnExplorationLocked; // 입력 잠금 요청 연결
            } // 구독 조건 끝
        } // 구독 끝
        private void OnDisable() // 탐험 입력 연결 해제
        { // 해제 시작
            if (input != null) // 공통 입력 존재 검사
            { // 해제 조건 시작
                input.MoveRequested -= OnMoveRequested; // 이동 요청 연결 해제
                input.ExplorationLocked -= OnExplorationLocked; // 입력 잠금 연결 해제
            } // 해제 조건 끝
        } // 해제 끝
        private void Update() // 한 칸의 화면 이동 처리
        { // 갱신 시작
            if (state == null || input == null || !input.IsExplorationEnabled || !state.IsMoving) // 이동 가능 상태 검사
            { // 갱신 제외 조건 시작
                return; // 입력 잠금 또는 대기 중 위치 유지
            } // 갱신 제외 조건 끝
            elapsed += Time.deltaTime; // 현재 이동 시간 누적
            var progress = Mathf.Clamp01(elapsed / moveDuration); // 한 칸 이동 비율
            transform.position = Vector3.Lerp(animationStart, animationTarget, progress); // 실제 표시 위치 보간
            if (progress < 1f) // 현재 칸 이동 완료 검사
            { // 이동 진행 조건 시작
                return; // 다음 프레임 이동 대기
            } // 이동 진행 조건 끝
            transform.position = animationTarget; // 목표 칸 중심 확정
            StepArrived?.Invoke(); // 대기 이동 전 현재 칸 도착 알림
            if (state.FinishMove()) // 대기 입력의 다음 이동 검사
            { // 대기 이동 조건 시작
                BeginAnimation(); // 첫 대기 입력의 화면 이동 시작
            } // 대기 이동 조건 끝
            else if (input.IsExplorationEnabled && !IsRoomTransitionLocked) // 탐험과 방 전환 상태 검사
            { // 누른 키 연속 이동 시작
                var keyboard = Keyboard.current; // 현재 키보드 조회
                if (keyboard != null) // 키보드 존재 검사
                { // 키보드 방향 계산 시작
                    var x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0); // 좌우 키 합계
                    var y = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0); // 상하 키 합계
                    if (Mathf.Abs(x) + Mathf.Abs(y) == 1) // 한 방향 누름 검사
                    { // 연속 요청 시작
                        OnMoveRequested(new Vector2Int(x, y)); // 같은 방향의 다음 칸 요청
                    } // 연속 요청 끝
                } // 키보드 방향 계산 끝
            } // 누른 키 연속 이동 끝
        } // 갱신 끝
        private void OnMoveRequested(Vector2Int direction) // 입력된 한 칸 이동 요청
        { // 요청 시작
            if (state == null || input == null || !input.IsExplorationEnabled || IsRoomTransitionLocked || roomTransitionFrame == Time.frameCount) // 탐험 입력과 방 전환 검사
            { // 차단 조건 시작
                return; // 탐험 외 입력 무시
            } // 차단 조건 끝
            var result = state.RequestMove(ToMoveDirection(direction)); // 논리 위치와 대기 입력 처리
            if (result == MoveRequestResult.Started) // 실제 첫 이동 시작 검사
            { // 이동 시작 조건 시작
                BeginAnimation(); // 새 목적 칸으로 화면 이동
            } // 이동 시작 조건 끝
        } // 요청 끝
        private void OnExplorationLocked() // 입력 잠금의 대기 입력 처리
        { // 잠금 시작
            state?.DiscardBufferedMove(); // 전환 전 대기 명령 제거
        } // 잠금 끝
        private bool CanEnter(int x, int y) // 실제 타일의 이동 가능 여부
        { // 판정 시작
            var cell = new Vector3Int(x, y, 0); // 판정할 격자 칸
            return floor.HasTile(cell) && !obstacles.HasTile(cell); // 바닥 존재와 장애물 부재 검사
        } // 판정 끝
        private void BeginAnimation() // 논리 칸의 화면 이동 준비
        { // 준비 시작
            animationStart = transform.position; // 현재 화면 위치 보관
            animationTarget = floor.GetCellCenterWorld(new Vector3Int(state.X, state.Y, 0)); // 목적 칸의 화면 중심
            elapsed = 0f; // 이동 경과 시간 초기화
            UpdateDirection(); // 새 방향의 화살표 표시
        } // 준비 끝
        private void UpdateDirection() // 현재 방향의 화살표 회전
        { // 표시 시작
            var angle = 0f; // 위쪽 기준 각도
            switch (state.Facing) // 현재 바라보는 방향 검사
            { // 방향 선택 시작
                case MoveDirection.Down: // 아래쪽 방향
                    angle = 180f; // 아래쪽 회전 각도
                    break; // 방향 선택 종료
                case MoveDirection.Left: // 왼쪽 방향
                    angle = 90f; // 왼쪽 회전 각도
                    break; // 방향 선택 종료
                case MoveDirection.Right: // 오른쪽 방향
                    angle = -90f; // 오른쪽 회전 각도
                    break; // 방향 선택 종료
            } // 방향 선택 끝
            directionIndicator.localRotation = Quaternion.Euler(0f, 0f, angle); // 화살표 회전 적용
            DungeonPresentation.ApplyPlayerDirection(gameObject, state.Facing); // 실제 이동 방향의 주인공 이미지 적용
        } // 표시 끝
        private static MoveDirection ToMoveDirection(Vector2Int direction) // 화면 방향의 논리 방향 변환
        { // 변환 시작
            if (direction == Vector2Int.up) // 위쪽 벡터 검사
            { // 위쪽 조건 시작
                return MoveDirection.Up; // 위쪽 논리 방향
            } // 위쪽 조건 끝
            if (direction == Vector2Int.down) // 아래쪽 벡터 검사
            { // 아래쪽 조건 시작
                return MoveDirection.Down; // 아래쪽 논리 방향
            } // 아래쪽 조건 끝
            if (direction == Vector2Int.left) // 왼쪽 벡터 검사
            { // 왼쪽 조건 시작
                return MoveDirection.Left; // 왼쪽 논리 방향
            } // 왼쪽 조건 끝
            if (direction == Vector2Int.right) // 오른쪽 벡터 검사
            { // 오른쪽 조건 시작
                return MoveDirection.Right; // 오른쪽 논리 방향
            } // 오른쪽 조건 끝
            return MoveDirection.None; // 허용하지 않은 벡터
        } // 변환 끝
    } // 클래스 끝
} // 이름 공간 끝
