using ProjectDelta.Domain; // 도메인 이동 규칙 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Input System 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public sealed class PlayerGridMovementController : MonoBehaviour // 플레이어 한 칸 이동 제어
    {
        [SerializeField] private InputActionAsset inputActions; // 프로젝트 입력 액션 에셋
        [SerializeField] private Transform viewTransform; // 바라보는 방향 기준 Transform
        [SerializeField] private float cellSize = 2f; // 한 칸 월드 크기
        [SerializeField] private int minX = -2; // 테스트 방 최소 X
        [SerializeField] private int maxX = 2; // 테스트 방 최대 X
        [SerializeField] private int minZ = -2; // 테스트 방 최소 Z
        [SerializeField] private int maxZ = 2; // 테스트 방 최대 Z

        private PlayerRunState playerState; // 현재 플레이어 런타임 상태
        private InputActionMap explorationMap; // 탐험 입력 맵
        private InputAction moveForwardAction; // 전진 액션
        private InputAction moveBackwardAction; // 후진 액션
        private InputAction moveLeftAction; // 좌측 액션
        private InputAction moveRightAction; // 우측 액션

        private void Awake() // 초기 상태 연결
        {
            if (viewTransform == null) // 시점 Transform 미지정 확인
            {
                Camera mainCamera = Camera.main; // 메인 카메라 검색
                viewTransform = mainCamera != null ? mainCamera.transform : transform; // 시점 기준 자동 지정
            }

            if (RunContext.Current != null) // 실제 런 진행 여부 확인
            {
                playerState = RunContext.Current.Player; // 실제 플레이어 상태 연결
                ApplyWorldPosition(playerState.CurrentGridPosition); // 저장된 논리 위치를 월드에 반영
            }
            else // 테스트 씬 상태 처리
            {
                playerState = new PlayerRunState(); // 테스트용 런타임 상태 생성
                playerState.CurrentGridPosition = WorldToGridPosition(transform.position); // 현재 월드 위치를 논리 좌표로 변환
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

            if (moveLeftAction != null) // 좌측 액션 존재 확인
            {
                moveLeftAction.performed -= OnMoveLeft; // 좌측 입력 이벤트 해제
            }

            if (moveRightAction != null) // 우측 액션 존재 확인
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

        private void TryMove(GridMoveInput input) // 한 칸 이동 처리
        {
            if (playerState == null) // 런타임 상태 존재 확인
            {
                return; // 이동 처리 중단
            }

            float yaw = viewTransform != null ? viewTransform.eulerAngles.y : transform.eulerAngles.y; // 현재 수평 시점 각도 읽기
            CardinalDirection facing = GridMovement.GetFacingFromYaw(yaw); // 시점 각도를 4방향으로 변환
            GridBounds bounds = new GridBounds(minX, maxX, minZ, maxZ); // 테스트 방 이동 범위 생성

            if (!GridMovement.TryGetTarget(playerState.CurrentGridPosition, facing, input, bounds, out GridPosition target)) // 목표 칸 이동 가능 여부 확인
            {
                Debug.Log($"[Project Delta] 이동 불가: {playerState.CurrentGridPosition} -> 범위 밖", this); // 이동 거부 로그 출력
                return; // 범위 밖 이동 중단
            }

            playerState.CurrentGridPosition = target; // 논리 그리드 위치 갱신
            ApplyWorldPosition(target); // 실제 Player 위치 갱신
            Debug.Log($"[Project Delta] GridPosition {target} / Facing {facing}", this); // 현재 좌표 로그 출력
        }

        private GridPosition WorldToGridPosition(Vector3 worldPosition) // 월드 위치를 논리 좌표로 변환
        {
            int gridX = Mathf.RoundToInt(worldPosition.x / cellSize); // X 그리드 좌표 계산
            int gridZ = Mathf.RoundToInt(worldPosition.z / cellSize); // Z 그리드 좌표 계산
            return new GridPosition(gridX, gridZ); // 논리 좌표 반환
        }

        private void ApplyWorldPosition(GridPosition gridPosition) // 논리 좌표를 월드 위치로 반영
        {
            Vector3 currentPosition = transform.position; // 현재 월드 위치 저장
            transform.position = new Vector3(gridPosition.X * cellSize, currentPosition.y, gridPosition.Z * cellSize); // 한 칸 단위 월드 위치 적용
        }
    }
}
