using ProjectDelta.Domain; // 도메인 좌표·이동 규칙 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Input System 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 문 상호작용(14일차 PlayerDoorInteractionController)과 완전히 같은 패턴을 따른다.
    // 22일차: 처음에는 계단 칸 위에 "서서" 상호작용하는 방식이었지만, 계단 모형이 실제로
    // 벽처럼 그 칸을 막고 있어야 해서(RoomDefinition_TestRoom_B의 벽 통로 참고) 문처럼
    // "정면에서" 상호작용하는 방식으로 바꿨다. 그 칸 자체는 이동 불가능한 벽이라 밟고 올라갈 수 없다.
    public sealed class StairsInteractionController : MonoBehaviour // 플레이어 계단 상호작용 제어
    {
        [SerializeField] private InputActionAsset inputActions; // 프로젝트 입력 액션 에셋
        [SerializeField] private Transform viewTransform; // 바라보는 방향 기준 Transform
        [SerializeField] private PlayerGridMovementController movementController; // 플레이어 이동 상태 컨트롤러
        [SerializeField] private DungeonFloorController floorController; // 층 전환 컨트롤러

        private InputActionMap explorationMap; // 탐험 입력 맵
        private InputAction interactAction; // 상호작용 액션
        private string promptText; // 현재 화면 안내 문구
        private GUIStyle promptStyle; // 안내 문구 GUI 스타일

        private void Awake() // 참조 자동 연결
        {
            if (viewTransform == null) // 시점 Transform 미지정 확인
            {
                Camera mainCamera = Camera.main; // 메인 카메라 검색
                viewTransform = mainCamera != null ? mainCamera.transform : transform; // 시점 기준 자동 지정
            }

            if (movementController == null) // 이동 컨트롤러 미지정 확인
            {
                movementController = GetComponent<PlayerGridMovementController>(); // 같은 Player의 이동 컨트롤러 연결
            }

            if (floorController == null) // 층 전환 컨트롤러 미지정 확인
            {
                floorController = FindFirstObjectByType<DungeonFloorController>(); // 층 전환 컨트롤러 검색
            }
        }

        private void OnEnable() // 상호작용 입력 활성화
        {
            if (inputActions == null) // 입력 에셋 미지정 확인
            {
                Debug.LogError("[Project Delta] StairsInteractionController에 Input Actions가 지정되지 않았습니다.", this); // 입력 에셋 오류 출력
                return; // 입력 연결 중단
            }

            explorationMap = inputActions.FindActionMap("Exploration", true); // 탐험 입력 맵 검색
            interactAction = explorationMap.FindAction("Interact", true); // 상호작용 액션 검색 (문과 동일한 F 입력 공유)
            interactAction.performed += OnInteract; // F 입력 이벤트 연결
            explorationMap.Enable(); // 탐험 입력 맵 활성화
        }

        private void OnDisable() // 상호작용 입력 해제
        {
            if (interactAction != null) // 상호작용 액션 존재 확인
            {
                interactAction.performed -= OnInteract; // F 입력 이벤트 해제
            }
        }

        private void Update() // 정면 계단 안내 갱신
        {
            promptText = FindStairsMarkerInFront() != null ? "계단 내려가기 [F]" : string.Empty; // 정면 계단 여부에 따라 안내 갱신
        }

        private void OnInteract(InputAction.CallbackContext context) // F 상호작용 처리
        {
            if (movementController != null && movementController.IsMoving) // 이동 보간 중 상호작용 차단 (14일차와 동일 규칙)
            {
                return; // 이동 중 상호작용 중단
            }

            if (FindStairsMarkerInFront() == null || floorController == null) // 정면 계단과 층 전환 컨트롤러 확인
            {
                return; // 계단 없음 또는 컨트롤러 없음, 상호작용 중단
            }

            floorController.TryDescend(movementController); // 다음 층으로 이동 시도

            promptText = FindStairsMarkerInFront() != null ? "계단 내려가기 [F]" : string.Empty; // 상호작용 후 안내 문구 즉시 갱신
        }

        private RoomContentMarker FindStairsMarkerInFront() // 플레이어 정면 칸의 계단 자리 조회
        {
            RoomView roomView = movementController != null ? movementController.CurrentRoomView : null; // 현재 방 표시 진입점 조회
            PlayerRunState playerState = movementController != null ? movementController.PlayerState : null; // 현재 플레이어 상태 조회

            if (roomView == null || playerState == null) // 필요한 참조 확인
            {
                return null; // 계단 없음 반환
            }

            CardinalDirection facing = GetFacingDirection(); // 현재 바라보는 4방향 계산
            GridPosition delta = GridMovement.GetDirectionDelta(facing); // 정면 방향 변화량 계산
            GridPosition frontPosition = new GridPosition(playerState.CurrentGridPosition.X + delta.X, playerState.CurrentGridPosition.Z + delta.Z); // 정면 칸 좌표 계산

            foreach (RoomContentMarker marker in roomView.GetMarkers(RoomContentType.Stairs)) // 현재 방의 계단 자리 전체 확인
            {
                GridPosition markerPosition = marker.GridPosition; // 계단 자리 그리드 좌표 조회

                if (markerPosition.X == frontPosition.X && markerPosition.Z == frontPosition.Z) // 정면 칸과 일치 확인
                {
                    return marker; // 정면 계단 자리 반환
                }
            }

            return null; // 계단 없음 반환
        }

        private CardinalDirection GetFacingDirection() // 현재 수평 시선 방향 계산
        {
            float yaw = viewTransform != null ? viewTransform.eulerAngles.y : transform.eulerAngles.y; // 현재 Yaw 읽기
            return GridMovement.GetFacingFromYaw(yaw); // Yaw를 4방향으로 변환
        }

        private void OnGUI() // 계단 상호작용 안내 표시
        {
            if (string.IsNullOrEmpty(promptText)) // 안내 문구 존재 여부 확인
            {
                return; // GUI 표시 생략
            }

            if (promptStyle == null) // GUI 스타일 생성 여부 확인
            {
                promptStyle = new GUIStyle(GUI.skin.label); // 기본 라벨 스타일 복제
                promptStyle.alignment = TextAnchor.MiddleCenter; // 가운데 정렬 적용
                promptStyle.fontSize = 22; // 안내 글자 크기 적용
                promptStyle.normal.textColor = Color.white; // 안내 글자 흰색 적용
            }

            Rect promptRect = new Rect(0f, Screen.height * 0.72f, Screen.width, 40f); // 화면 하단 중앙 영역 계산 (문 안내와 동일 위치, 동시에 뜨지 않음)
            GUI.Label(promptRect, promptText, promptStyle); // 계단 상호작용 안내 표시
        }
    }
}
