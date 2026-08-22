using ProjectDelta.Domain; // 도메인 문 규칙 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Input System 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public sealed class PlayerDoorInteractionController : MonoBehaviour // 플레이어 문 상호작용 제어
    {
        [SerializeField] private InputActionAsset inputActions; // 프로젝트 입력 액션 에셋
        [SerializeField] private Transform viewTransform; // 바라보는 방향 기준 Transform
        [SerializeField] private PlayerGridMovementController movementController; // 플레이어 이동 상태 컨트롤러
        [SerializeField] private RoomPassageController passageController; // 초기 방 통로 컨트롤러

        private InputActionMap explorationMap; // 탐험 입력 맵
        private InputAction interactAction; // 상호작용 액션
        private string promptText; // 현재 화면 안내 문구
        private GUIStyle promptStyle; // 안내 문구 GUI 스타일

        private void Awake() // 상호작용 참조 자동 연결
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

            if (passageController == null) // 초기 통로 컨트롤러 미지정 확인
            {
                passageController = FindFirstObjectByType<RoomPassageController>(); // 첫 테스트 방 통로 컨트롤러 검색
            }
        }

        private void OnEnable() // 상호작용 입력 활성화
        {
            if (inputActions == null) // 입력 에셋 미지정 확인
            {
                Debug.LogError("[Project Delta] PlayerDoorInteractionController에 Input Actions가 지정되지 않았습니다.", this); // 입력 에셋 오류 출력
                return; // 입력 연결 중단
            }

            explorationMap = inputActions.FindActionMap("Exploration", true); // 탐험 입력 맵 검색
            interactAction = explorationMap.FindAction("Interact", true); // 상호작용 액션 검색
            interactAction.performed += OnInteract; // F 입력 이벤트 연결
            explorationMap.Enable(); // 탐험 입력 맵 활성화
        }

        private void Update() // 정면 문 안내 갱신
        {
            promptText = BuildPromptText(); // 현재 문 상태 안내 문구 계산
        }

        private void OnDisable() // 상호작용 입력 해제
        {
            if (interactAction != null) // 상호작용 액션 존재 확인
            {
                interactAction.performed -= OnInteract; // F 입력 이벤트 해제
            }
        }

        private void OnInteract(InputAction.CallbackContext context) // F 상호작용 처리
        {
            PlayerRunState playerState = movementController != null ? movementController.PlayerState : null; // 현재 플레이어 상태 조회
            RoomPassageController currentPassageController = GetCurrentPassageController(); // 현재 방 통로 컨트롤러 조회

            if (playerState == null || currentPassageController == null) // 필요한 런타임 상태 확인
            {
                return; // 상호작용 중단
            }

            CardinalDirection facing = GetFacingDirection(); // 현재 바라보는 4방향 계산
            DoorOpenResult result = currentPassageController.TryOpenDoor(playerState.CurrentGridPosition, facing, playerState); // 현재 방 바로 앞 문 열기 시도

            if (result == DoorOpenResult.Opened) // 문 열기 성공 확인
            {
                Debug.Log($"[Project Delta] 문 열기 성공 / 남은 열쇠 {playerState.KeyCount}개", this); // 문 열기 성공 로그 출력
            }
            else if (result == DoorOpenResult.LockedNoKey) // 열쇠 부족 확인
            {
                Debug.Log("[Project Delta] 잠긴 문: 보유 열쇠가 없습니다.", this); // 열쇠 부족 로그 출력
            }

            promptText = BuildPromptText(); // 상호작용 후 안내 문구 즉시 갱신
        }

        private string BuildPromptText() // 정면 문 안내 문구 생성
        {
            PlayerRunState playerState = movementController != null ? movementController.PlayerState : null; // 현재 플레이어 상태 조회
            RoomPassageController currentPassageController = GetCurrentPassageController(); // 현재 방 통로 컨트롤러 조회

            if (playerState == null || currentPassageController == null) // 필요한 런타임 상태 확인
            {
                return string.Empty; // 안내 없음 반환
            }

            CardinalDirection facing = GetFacingDirection(); // 현재 바라보는 4방향 계산

            if (!currentPassageController.TryGetDoor(playerState.CurrentGridPosition, facing, out GridPassage doorPassage)) // 현재 방 바로 앞 문 존재 확인
            {
                return string.Empty; // 문 없으면 안내 숨김
            }

            if (doorPassage.IsOpen) // 열린 문 확인
            {
                return string.Empty; // 열린 문 안내 숨김
            }

            if (doorPassage.IsLocked) // 잠긴 문 확인
            {
                return $"잠김 (열쇠 : {playerState.KeyCount}개)"; // 잠김과 보유 열쇠 수 표시
            }

            return "열기 [F]"; // 일반 닫힌 문 열기 안내
        }

        private RoomPassageController GetCurrentPassageController() // 현재 이동 상태의 방 통로 조회
        {
            if (movementController != null && movementController.CurrentPassageController != null) // 이동 컨트롤러 현재 방 존재 확인
            {
                return movementController.CurrentPassageController; // 현재 방 통로 컨트롤러 반환
            }

            return passageController; // 초기 방 통로 컨트롤러 대체 반환
        }

        private CardinalDirection GetFacingDirection() // 현재 수평 시선 방향 계산
        {
            float yaw = viewTransform != null ? viewTransform.eulerAngles.y : transform.eulerAngles.y; // 현재 Yaw 읽기
            return GridMovement.GetFacingFromYaw(yaw); // Yaw를 4방향으로 변환
        }

        private void OnGUI() // 문 상호작용 안내 표시
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

            Rect promptRect = new Rect(0f, Screen.height * 0.72f, Screen.width, 40f); // 화면 하단 중앙 영역 계산
            GUI.Label(promptRect, promptText, promptStyle); // 문 상호작용 안내 표시
        }
    }
}
