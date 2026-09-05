using ProjectDelta.Domain; // 도메인 문 규칙 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Input System 사용
using UnityEngine.UI; // UGUI 기능 사용

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
        private string lastPromptText = string.Empty; // 마지막 UGUI 안내 문구
        private GameObject promptCanvasObject; // 안내 Canvas 오브젝트
        private GameObject promptRootObject; // 안내 패널 오브젝트
        private Text promptLabel; // 안내 Text

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

            BuildPromptUi(); // 문 안내 UGUI 생성
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
            RefreshPromptUi(); // 상태 변경 시 UGUI 갱신
        }

        private void OnDisable() // 상호작용 입력 해제
        {
            if (interactAction != null) // 상호작용 액션 존재 확인
            {
                interactAction.performed -= OnInteract; // 입력 이벤트 해제
            }

            promptText = string.Empty; // 비활성화 시 안내 제거
            RefreshPromptUi(); // 비활성화 상태 즉시 반영
        }

        private void OnInteract(InputAction.CallbackContext context) // F 상호작용 처리
        {
            if (movementController != null && movementController.IsMoving) // 이동 보간 중 상호작용 차단
            {
                return; // 이동 중 상호작용 중단
            }

            PlayerRunState playerState = movementController != null ? movementController.PlayerState : null; // 현재 플레이어 상태 조회
            RoomPassageController currentPassageController = GetCurrentPassageController(); // 현재 방 통로 컨트롤러 조회

            if (playerState == null || currentPassageController == null) // 필요한 런타임 상태 확인
            {
                return; // 상호작용 중단
            }

            CardinalDirection facing = GetFacingDirection(); // 현재 바라보는 4방향 계산
            DoorOpenResult result = currentPassageController.TryOpenDoor(playerState.CurrentGridPosition, facing, playerState); // 기존 문 열기 판정 실행

            if (result == DoorOpenResult.Opened) // 문 열기 성공 확인
            {
                Debug.Log($"[Project Delta] 문 열기 성공 / 남은 열쇠 {playerState.KeyCount}개", this); // 문 열기 성공 로그 출력
            }
            else if (result == DoorOpenResult.LockedNoKey) // 열쇠 부족 확인
            {
                Debug.Log("[Project Delta] 잠긴 문: 보유 열쇠가 없습니다.", this); // 열쇠 부족 로그 출력
            }

            promptText = BuildPromptText(); // 상호작용 후 안내 문구 즉시 갱신
            RefreshPromptUi(); // 변경된 문 상태 UGUI 반영
        }

        private string BuildPromptText() // 정면 문 안내 문구 생성
        {
            PlayerRunState playerState = movementController != null ? movementController.PlayerState : null; // 현재 플레이어 상태 조회
            RoomPassageController currentPassageController = GetCurrentPassageController(); // 현재 방 통로 컨트롤러 조회

            if (playerState == null || currentPassageController == null) // 필요한 참조 확인
            {
                return string.Empty; // 안내 없음 반환
            }

            CardinalDirection facing = GetFacingDirection(); // 현재 수평 방향 계산

            if (!currentPassageController.TryGetDoor(playerState.CurrentGridPosition, facing, out GridPassage doorPassage)) // 현재 정면 문 조회
            {
                return string.Empty; // 문 없으면 안내 숨김
            }

            if (doorPassage.IsOpen) // 열린 문 확인
            {
                return string.Empty; // 열린 문 안내 숨김
            }

            if (doorPassage.IsLocked) // 잠긴 문 확인
            {
                return $"잠김 (열쇠 : {playerState.KeyCount}개)"; // 잠김과 열쇠 수 표시
            }

            return "열기 [F]"; // 일반 닫힌 문 안내 반환
        }

        private RoomPassageController GetCurrentPassageController() // 현재 방 통로 조회
        {
            if (movementController != null && movementController.CurrentPassageController != null) // 이동 컨트롤러 현재 방 확인
            {
                return movementController.CurrentPassageController; // 현재 방 통로 반환
            }

            return passageController; // 초기 통로 대체 반환
        }

        private CardinalDirection GetFacingDirection() // 현재 수평 시선 방향 계산
        {
            float yaw = viewTransform != null ? viewTransform.eulerAngles.y : transform.eulerAngles.y; // 현재 Yaw 읽기
            return GridMovement.GetFacingFromYaw(yaw); // Yaw를 4방향으로 변환
        }

        private void BuildPromptUi() // 문 안내 UGUI 생성
        {
            if (promptCanvasObject != null) // 기존 Canvas 확인
            {
                return; // 중복 생성 방지
            }

            RuntimeUiFactory.EnsureEventSystem(); // 공용 EventSystem 준비

            promptCanvasObject = new GameObject( // 안내 Canvas 생성
                "DoorPromptCanvas", // Canvas 이름 지정
                typeof(RectTransform), // RectTransform 추가
                typeof(Canvas), // Canvas 추가
                typeof(CanvasScaler), // CanvasScaler 추가
                typeof(GraphicRaycaster)); // GraphicRaycaster 추가

            promptCanvasObject.transform.SetParent( // 컨트롤러 하위 배치
                transform, // 현재 Transform 사용
                false); // 로컬 Transform 유지

            Canvas canvas = promptCanvasObject.GetComponent<Canvas>(); // Canvas 참조 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 사용
            canvas.sortingOrder = 2400; // 일반 탐험 화면 위에 표시

            CanvasScaler scaler = promptCanvasObject.GetComponent<CanvasScaler>(); // CanvasScaler 참조 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 사용
            UiScaleSettings.Refresh(); // 현재 UI 배율 갱신
            UiScaleSettings.ApplyToCanvasScaler(scaler, new Vector2(1920f, 1080f)); // 프로젝트 UI 배율 적용

            GraphicRaycaster raycaster = promptCanvasObject.GetComponent<GraphicRaycaster>(); // 레이캐스터 참조 조회
            raycaster.enabled = false; // 안내 UI의 탐험 입력 방해 방지

            RectTransform promptRect = RuntimeUiFactory.CreateUiObject("PromptRoot", promptCanvasObject.transform); // 안내 패널 생성
            promptRect.anchorMin = new Vector2(0.5f, 0.28f); // 기존 OnGUI 72% 지점 대응
            promptRect.anchorMax = new Vector2(0.5f, 0.28f); // 기존 OnGUI 72% 지점 대응
            promptRect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 사용
            promptRect.anchoredPosition = Vector2.zero; // 앵커 기준 중앙 배치
            promptRect.sizeDelta = new Vector2(900f, 52f); // 안내 패널 크기 지정

            Image background = promptRect.gameObject.AddComponent<Image>(); // 안내 배경 추가
            background.color = new Color(0f, 0f, 0f, 0.62f); // 반투명 검정 배경 적용
            background.raycastTarget = false; // 입력 방해 방지

            RectTransform labelRect = RuntimeUiFactory.CreateStretchedRect("PromptLabel", promptRect); // 안내 Text 영역 생성
            promptLabel = labelRect.gameObject.AddComponent<Text>(); // 안내 Text 추가
            RuntimeUiFactory.ConfigureText(promptLabel, string.Empty, 22, FontStyle.Normal, TextAnchor.MiddleCenter); // 기존 안내 스타일 적용
            promptLabel.raycastTarget = false; // 입력 방해 방지

            promptRootObject = promptRect.gameObject; // 안내 패널 참조 저장
            promptRootObject.SetActive(false); // 초기 안내 숨김
        }

        private void RefreshPromptUi() // 문 안내 UGUI 갱신
        {
            if (promptRootObject == null || promptLabel == null) // UI 생성 여부 확인
            {
                BuildPromptUi(); // 누락 시 UI 생성
            }

            string currentText = promptText ?? string.Empty; // null 안내 문자열 정리

            if (currentText == lastPromptText && promptRootObject.activeSelf == !string.IsNullOrEmpty(currentText)) // 화면 상태 변경 여부 확인
            {
                return; // 변경 없음 종료
            }

            lastPromptText = currentText; // 마지막 안내 문구 저장
            promptLabel.text = currentText; // 현재 안내 문구 적용
            promptRootObject.SetActive(!string.IsNullOrEmpty(currentText)); // 안내 존재 여부에 따라 표시
        }
    }
}
