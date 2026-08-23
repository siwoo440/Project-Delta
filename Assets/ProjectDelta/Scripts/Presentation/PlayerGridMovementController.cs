using System.Collections; // 이동 보간 코루틴 사용
using ProjectDelta.Domain; // 도메인 이동 규칙 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Input System 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public sealed class PlayerGridMovementController : MonoBehaviour // 플레이어 한 칸 이동 제어
    {
        [SerializeField] private InputActionAsset inputActions; // 프로젝트 입력 액션 에셋
        [SerializeField] private Transform viewTransform; // 바라보는 방향 기준 Transform
        [SerializeField] private RoomView currentRoomView; // 현재 방 표시 진입점 (20일차: passageController+roomOrigin 대체)
        [SerializeField] private TestRoomTransitionController roomTransitionController; // 테스트 방 경계 이동 컨트롤러
        [SerializeField] private float cellSize = 2f; // 한 칸 월드 크기
        [SerializeField] private float moveDuration = 0.15f; // 한 칸 이동 보간 시간(초)
        [SerializeField] private int minX = -2; // 테스트 방 최소 X
        [SerializeField] private int maxX = 2; // 테스트 방 최대 X
        [SerializeField] private int minZ = -2; // 테스트 방 최소 Z
        [SerializeField] private int maxZ = 2; // 테스트 방 최대 Z

        private PlayerRunState playerState; // 현재 플레이어 런타임 상태
        private bool isMoving; // 이동 보간 진행 중 여부 (16일차 이동 잠금)
        private InputActionMap explorationMap; // 탐험 입력 맵
        private InputAction moveForwardAction; // 전진 액션
        private InputAction moveBackwardAction; // 후진 액션
        private InputAction moveLeftAction; // 좌측 액션
        private InputAction moveRightAction; // 우측 액션

        public PlayerRunState PlayerState => playerState; // 다른 탐험 기능용 플레이어 상태 공개
        public RoomView CurrentRoomView => currentRoomView; // 현재 방 표시 진입점 공개
        public RoomPassageController CurrentPassageController => currentRoomView != null ? currentRoomView.PassageController : null; // 현재 방 통로 컨트롤러 공개
        public Transform CurrentRoomOrigin => currentRoomView != null ? currentRoomView.transform : null; // 현재 방 월드 원점 공개
        public bool IsMoving => isMoving; // 이동 잠금 상태 공개 (다른 입력 컨트롤러가 참조)
        public bool IsInputLocked { get; set; } // 25일차: 상자 패널 등 모달 UI가 열려있는 동안 이동 차단

        private void Awake() // 초기 상태 연결
        {
            if (viewTransform == null) // 시점 Transform 미지정 확인
            {
                Camera mainCamera = Camera.main; // 메인 카메라 검색
                viewTransform = mainCamera != null ? mainCamera.transform : transform; // 시점 기준 자동 지정
            }

            if (currentRoomView == null) // 현재 방 표시 진입점 미지정 확인
            {
                currentRoomView = FindFirstObjectByType<RoomView>(); // 현재 방 RoomView 자동 검색
            }

            if (roomTransitionController == null) // 방 전환 컨트롤러 미지정 확인
            {
                roomTransitionController = FindFirstObjectByType<TestRoomTransitionController>(); // 테스트 방 전환 컨트롤러 자동 검색
            }

            if (RunContext.Current != null) // 실제 런 진행 여부 확인
            {
                playerState = RunContext.Current.Player; // 실제 플레이어 상태 연결

                if (string.IsNullOrEmpty(playerState.CurrentRoomId) && CurrentPassageController != null) // 현재 방 식별자 미설정 확인
                {
                    playerState.CurrentRoomId = CurrentPassageController.RoomId; // 첫 테스트 방 식별자 초기화
                }

                ApplyWorldPosition(playerState.CurrentGridPosition); // 저장된 논리 위치를 현재 방 월드 위치에 반영
            }
            else // 테스트 씬 상태 처리
            {
                playerState = new PlayerRunState(); // 테스트용 런타임 상태 생성
                playerState.CurrentGridPosition = WorldToGridPosition(transform.position); // 현재 월드 위치를 논리 좌표로 변환
                playerState.CurrentRoomId = CurrentPassageController != null ? CurrentPassageController.RoomId : "TestRoom_A"; // 테스트 시작 방 식별자 저장
                playerState.KeyCount = 1; // 잠긴 문 검증용 테스트 열쇠 한 개 지급
            }

            // 시작 방도 최초 진입으로 취급한다 (기획서 3.3.1절 진입 순서, 20일차).
            if (currentRoomView != null && CurrentPassageController != null && CurrentPassageController.CurrentInstance != null) // 방문 상태 확인에 필요한 참조 확인
            {
                CurrentPassageController.CurrentInstance.MarkVisited(); // 시작 방 최초 방문 처리
            }
        }

        private void OnEnable() // 탐험 입력 활성화
        {
            if (inputActions == null) // 입력 에셋 미지정 확인
            {
                Debug.LogError("[Project Delta] PlayerGridMovementController에 Input Actions가 지정되지 않았습니다.", this); // 입력 에셋 오류 출력
                return; // 입력 등록 중단
            }

            inputActions.Disable(); // 기존 입력 맵 전체 비활성화
            explorationMap = inputActions.FindActionMap("Exploration", true); // 탐험 입력 맵 검색
            moveForwardAction = explorationMap.FindAction("MoveForward", true); // 전진 액션 검색
            moveBackwardAction = explorationMap.FindAction("MoveBackward", true); // 후진 액션 검색
            moveLeftAction = explorationMap.FindAction("MoveLeft", true); // 좌측 액션 검색
            moveRightAction = explorationMap.FindAction("MoveRight", true); // 우측 액션 검색
            moveForwardAction.performed += OnMoveForward; // 전진 입력 이벤트 연결
            moveBackwardAction.performed += OnMoveBackward; // 후진 입력 이벤트 연결
            moveLeftAction.performed += OnMoveLeft; // 좌측 입력 이벤트 연결
            moveRightAction.performed += OnMoveRight; // 우측 입력 이벤트 연결
            explorationMap.Enable(); // 탐험 입력 맵 활성화
        }

        private void OnDisable() // 탐험 입력 해제
        {
            if (moveForwardAction != null) // 전진 액션 존재 확인
            {
                moveForwardAction.performed -= OnMoveForward; // 전진 입력 이벤트 해제
            }

            if (moveBackwardAction != null) // 후진 액션 존재 확인
            {
                moveBackwardAction.performed -= OnMoveBackward; // 후진 입력 이벤트 해제
            }

            if (moveLeftAction != null) // 좌측 입력 액션 존재 확인
            {
                moveLeftAction.performed -= OnMoveLeft; // 좌측 입력 이벤트 해제
            }

            if (moveRightAction != null) // 우측 입력 액션 존재 확인
            {
                moveRightAction.performed -= OnMoveRight; // 우측 입력 이벤트 해제
            }

            explorationMap?.Disable(); // 탐험 입력 맵 비활성화

            if (inputActions != null) // 입력 에셋 존재 확인
            {
                InputActionMap uiMap = inputActions.FindActionMap("UI", false); // UI 입력 맵 검색
                uiMap?.Enable(); // UI 입력 맵 복구
            }
        }

        private void OnMoveForward(InputAction.CallbackContext context) // W 입력 처리
        {
            TryMove(GridMoveInput.Forward); // 한 칸 전진 시도
        }

        private void OnMoveBackward(InputAction.CallbackContext context) // S 입력 처리
        {
            TryMove(GridMoveInput.Backward); // 한 칸 후진 시도
        }

        private void OnMoveLeft(InputAction.CallbackContext context) // A 입력 처리
        {
            TryMove(GridMoveInput.Left); // 한 칸 좌측 이동 시도
        }

        private void OnMoveRight(InputAction.CallbackContext context) // D 입력 처리
        {
            TryMove(GridMoveInput.Right); // 한 칸 우측 이동 시도
        }

        private void TryMove(GridMoveInput input) // 한 칸 이동 또는 방 경계 이동 처리
        {
            if (playerState == null || isMoving || IsInputLocked) // 런타임 상태와 이동 잠금 확인 (16일차 중복 이동 방지, 25일차 모달 UI 잠금)
            {
                return; // 이동 처리 중단
            }

            float yaw = viewTransform != null ? viewTransform.eulerAngles.y : transform.eulerAngles.y; // 현재 수평 시점 각도 읽기
            CardinalDirection facing = GridMovement.GetFacingFromYaw(yaw); // 시점 각도를 4방향으로 변환
            CardinalDirection moveDirection = GridMovement.GetMoveDirection(facing, input); // 상대 입력을 절대 이동 방향으로 변환
            GridPosition delta = GridMovement.GetMoveDelta(facing, input); // 이동 변화량 계산
            GridPosition target = new GridPosition(playerState.CurrentGridPosition.X + delta.X, playerState.CurrentGridPosition.Z + delta.Z); // 목표 논리 좌표 계산
            GridBounds bounds = new GridBounds(minX, maxX, minZ, maxZ); // 현재 방 이동 범위 생성

            RoomPassageController passageController = CurrentPassageController; // 현재 방 통로 컨트롤러 조회

            if (passageController != null && !passageController.CanPass(playerState.CurrentGridPosition, moveDirection)) // 벽 또는 닫힌 문 통과 여부 확인
            {
                Debug.Log($"[Project Delta] 이동 불가: {playerState.CurrentGridPosition} / {moveDirection} 통로 차단", this); // 통로 차단 로그 출력
                return; // 막힌 통로 이동 중단
            }

            if (bounds.Contains(target)) // 현재 방 내부 목표 칸 확인
            {
                CommitGridMove(target, facing); // 현재 방 안에서 한 칸 이동 확정
                return; // 이동 처리 완료
            }

            TryMoveAcrossRoomBoundary(moveDirection, facing); // 현재 방 밖 이동이면 연결 방 이동 시도
        }

        private void TryMoveAcrossRoomBoundary(CardinalDirection moveDirection, CardinalDirection facing) // 열린 문을 통한 인접 방 이동 처리
        {
            RoomPassageController passageController = CurrentPassageController; // 현재 방 통로 컨트롤러 조회

            if (roomTransitionController == null || passageController == null) // 방 전환에 필요한 참조 확인
            {
                Debug.Log("[Project Delta] 이동 불가: 연결된 테스트 방 정보가 없습니다.", this); // 연결 방 없음 로그 출력
                return; // 방 경계 이동 중단
            }

            if (!roomTransitionController.TryTransition(playerState.CurrentRoomId, playerState.CurrentGridPosition, moveDirection, out RoomPassageController destinationRoom, out GridPosition destinationEntryPosition)) // 현재 경계에 연결된 방 조회
            {
                Debug.Log($"[Project Delta] 이동 불가: {playerState.CurrentRoomId} / {moveDirection} 방향 연결 방 없음", this); // 연결 조건 실패 로그 출력
                return; // 방 경계 이동 중단
            }

            RoomView destinationRoomView = destinationRoom.GetComponent<RoomView>(); // 목적 방의 RoomView 조회

            if (destinationRoomView == null) // 목적 방 RoomView 존재 확인
            {
                Debug.LogError($"[Project Delta] {destinationRoom.RoomId}에 RoomView가 없습니다.", this); // RoomView 누락 경고 출력
                return; // 방 경계 이동 중단
            }

            EnterRoom(destinationRoomView, destinationEntryPosition, facing); // 정식 방 진입 절차 실행
        }

        // 기획서 3.3.1절 방 진입 순서: 이동 가능 여부 확인(TryMove) -> 문 상태 확인(TryMove)
        // -> 방 이동 -> 현재 위치 갱신 -> 지도 갱신 -> 최초 방문 여부 확인 -> 방 이벤트 처리 -> 자동 저장 요청.
        // 앞 두 단계는 호출부(TryMove)에서 이미 끝난 상태로 여기 들어온다.
        // 22일차: 계단으로 다음 층에 들어갈 때도 같은 진입 순서를 그대로 따르므로 DungeonFloorController에도 공개한다.
        public void EnterRoom(RoomView roomView, GridPosition entryPosition, CardinalDirection facing) // 정식 방 진입 절차 (20일차)
        {
            currentRoomView = roomView; // 방 이동 -> 현재 방 갱신

            playerState.CurrentRoomId = roomView.PassageController != null ? roomView.PassageController.RoomId : roomView.name; // 현재 위치 갱신: 방 식별자
            playerState.CurrentGridPosition = entryPosition; // 현재 위치 갱신: 그리드 좌표

            // TODO: 지도 갱신 - 지도 시스템이 생기는 일차에 연결한다.

            bool isFirstVisit = roomView.PassageController != null
                && roomView.PassageController.CurrentInstance != null
                && roomView.PassageController.CurrentInstance.MarkVisited(); // 최초 방문 여부 확인 및 방문 처리

            // TODO: 방 이벤트 처리 - isFirstVisit를 기준으로 이벤트 시스템이 생기는 일차(99~108일차)에 연결한다.
            // TODO: 자동 저장 요청 - SaveService/RunContext가 실제로 연결되는 일차에 호출한다.

            StartCoroutine(MoveRoutine(CalculateWorldPosition(entryPosition))); // 목적 방 입구까지 부드럽게 이동

            Debug.Log($"[Project Delta] 방 진입: {playerState.CurrentRoomId} / Entry {entryPosition} / Facing {facing} / 최초 방문 {isFirstVisit}", this); // 방 진입 결과 출력
        }

        private void CommitGridMove(GridPosition target, CardinalDirection facing) // 현재 방 내부 한 칸 이동 확정
        {
            playerState.CurrentGridPosition = target; // 논리 그리드 위치 즉시 갱신 (17일차: 화면 위치와 분리)
            StartCoroutine(MoveRoutine(CalculateWorldPosition(target))); // 실제 Player 위치를 목표 칸까지 부드럽게 이동
            Debug.Log($"[Project Delta] GridPosition {target} / Room {playerState.CurrentRoomId} / Facing {facing}", this); // 현재 좌표와 방 로그 출력
        }

        private GridPosition WorldToGridPosition(Vector3 worldPosition) // 월드 위치를 현재 방 논리 좌표로 변환
        {
            Vector3 localPosition = CurrentRoomOrigin != null ? CurrentRoomOrigin.InverseTransformPoint(worldPosition) : worldPosition; // 현재 방 원점 기준 로컬 위치 계산
            int gridX = Mathf.RoundToInt(localPosition.x / cellSize); // X 그리드 좌표 계산
            int gridZ = Mathf.RoundToInt(localPosition.z / cellSize); // Z 그리드 좌표 계산
            return new GridPosition(gridX, gridZ); // 논리 좌표 반환
        }

        private Vector3 CalculateWorldPosition(GridPosition gridPosition) // 논리 좌표를 현재 방 월드 위치로 계산
        {
            Vector3 localPosition = new Vector3(gridPosition.X * cellSize, 0f, gridPosition.Z * cellSize); // 현재 방 기준 목표 로컬 위치 계산
            Vector3 targetWorldPosition = CurrentRoomOrigin != null ? CurrentRoomOrigin.TransformPoint(localPosition) : localPosition; // 현재 방 원점을 이용해 월드 위치 계산
            targetWorldPosition.y = transform.position.y; // Player 현재 높이 유지
            return targetWorldPosition; // 계산된 목표 월드 위치 반환
        }

        private void ApplyWorldPosition(GridPosition gridPosition) // 논리 좌표를 현재 방 월드 위치로 즉시 반영 (Awake 초기 배치 전용)
        {
            transform.position = CalculateWorldPosition(gridPosition); // Player 실제 월드 위치 즉시 적용
        }

        private IEnumerator MoveRoutine(Vector3 targetWorldPosition) // 한 칸 또는 방 경계 이동을 부드럽게 보간 (16~17일차)
        {
            isMoving = true; // 이동 잠금 시작 - 보간이 끝날 때까지 다른 입력 차단

            Vector3 startWorldPosition = transform.position; // 보간 시작 위치 저장
            float elapsed = 0f; // 경과 시간 초기화

            while (elapsed < moveDuration) // 지정한 이동 시간 동안 반복
            {
                elapsed += Time.deltaTime; // 경과 시간 누적
                float t = Mathf.Clamp01(elapsed / moveDuration); // 진행 비율 계산
                float smoothT = t * t * (3f - 2f * t); // SmoothStep 완화 곡선 적용 (느리게 출발 - 빠르게 - 느리게 정지)
                transform.position = Vector3.Lerp(startWorldPosition, targetWorldPosition, smoothT); // 보간 위치 적용
                yield return null; // 다음 프레임까지 대기
            }

            transform.position = targetWorldPosition; // 목표 위치로 정확히 고정 (논리 위치와 최종 일치)
            isMoving = false; // 이동 잠금 해제 - 다음 입력 허용
        }
    }
}
