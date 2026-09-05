using ProjectDelta.Domain; // 도메인 좌표·이동 규칙 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Input System 사용
using UnityEngine.UI; // UGUI 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 문 상호작용과 같은 탐험 안내 UI 패턴을 유지한다.
    public sealed class StairsInteractionController : MonoBehaviour // 플레이어 계단 상호작용 제어
    {
        [SerializeField] private InputActionAsset inputActions; // 프로젝트 입력 액션 에셋
        [SerializeField] private Transform viewTransform; // 바라보는 방향 기준 Transform
        [SerializeField] private PlayerGridMovementController movementController; // 플레이어 이동 상태 컨트롤러
        [SerializeField] private DungeonFloorController floorController; // 층 전환 컨트롤러

        private InputActionMap explorationMap; // 탐험 입력 맵
        private InputAction interactAction; // 상호작용 액션
        private string promptText; // 현재 화면 안내 문구
        private string lastPromptText = string.Empty; // 마지막 UGUI 안내 문구
        private bool awaitingConfirmation; // 확인 대기 상태
        private GameObject promptCanvasObject; // 안내 Canvas 오브젝트
        private GameObject promptRootObject; // 안내 패널 오브젝트
        private Text promptLabel; // 안내 Text

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

            BuildPromptUi(); // 계단 안내 UGUI 생성
        }

        private void OnEnable() // 상호작용 입력 활성화
        {
            if (inputActions == null) // 입력 에셋 미지정 확인
            {
                Debug.LogError("[Project Delta] StairsInteractionController에 Input Actions가 지정되지 않았습니다.", this); // 입력 에셋 오류 출력
                return; // 입력 연결 중단
            }

            explorationMap = inputActions.FindActionMap("Exploration", true); // 탐험 입력 맵 검색
            interactAction = explorationMap.FindAction("Interact", true); // 상호작용 액션 검색
            interactAction.performed += OnInteract; // F 입력 이벤트 연결
            explorationMap.Enable(); // 탐험 입력 맵 활성화
        }

        private void OnDisable() // 상호작용 입력 해제
        {
            if (interactAction != null) // 상호작용 액션 존재 확인
            {
                interactAction.performed -= OnInteract; // 입력 이벤트 해제
            }

            awaitingConfirmation = false; // 확인 대기 상태 초기화
            promptText = string.Empty; // 안내 문구 제거
            RefreshPromptUi(); // 비활성화 상태 즉시 반영
        }

        private void Update() // 정면 계단 안내 갱신
        {
            bool hasStairsInFront = FindStairsMarkerInFront() != null; // 정면 계단 존재 확인

            if (!hasStairsInFront) // 계단 정면 이탈 확인
            {
                awaitingConfirmation = false; // 확인 대기 상태 취소
            }

            if (awaitingConfirmation) // 확인 대기 상태 확인
            {
                promptText = "이전 층으로 돌아갈 수 없습니다. 내려가시겠습니까? [F] 확인 / [Esc] 취소"; // 확인 안내 표시
            }
            else
            {
                promptText = hasStairsInFront ? "계단 내려가기 [F]" : string.Empty; // 일반 계단 안내 표시
            }

            if (awaitingConfirmation && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) // Esc 취소 입력 확인
            {
                awaitingConfirmation = false; // 확인 대기 취소
                promptText = hasStairsInFront ? "계단 내려가기 [F]" : string.Empty; // 취소 후 일반 안내 복원
            }

            RefreshPromptUi(); // 현재 안내 UGUI 반영
        }

        private void OnInteract(InputAction.CallbackContext context) // F 상호작용 처리
        {
            if (movementController != null && movementController.IsMoving) // 이동 보간 중 상호작용 차단
            {
                return; // 이동 중 상호작용 중단
            }

            if (FindStairsMarkerInFront() == null || floorController == null) // 정면 계단과 층 컨트롤러 확인
            {
                return; // 상호작용 중단
            }

            if (!awaitingConfirmation) // 첫 번째 F 입력 확인
            {
                awaitingConfirmation = true; // 확인 대기 상태 진입
                promptText = "이전 층으로 돌아갈 수 없습니다. 내려가시겠습니까? [F] 확인 / [Esc] 취소"; // 확인 안내 즉시 표시
                RefreshPromptUi(); // 확인 상태 UGUI 반영
                return; // 실제 층 이동 보류
            }

            awaitingConfirmation = false; // 확인 대기 상태 해제
            floorController.TryDescend(movementController); // 기존 다음 층 이동 시도 실행
            promptText = FindStairsMarkerInFront() != null ? "계단 내려가기 [F]" : string.Empty; // 상호작용 후 안내 갱신
            RefreshPromptUi(); // 이동 후 안내 UGUI 반영
        }

        private RoomContentMarker FindStairsMarkerInFront() // 플레이어 정면 칸 계단 조회
        {
            RoomView roomView = movementController != null ? movementController.CurrentRoomView : null; // 현재 방 뷰 조회
            PlayerRunState playerState = movementController != null ? movementController.PlayerState : null; // 현재 플레이어 상태 조회

            if (roomView == null || playerState == null) // 필요한 참조 확인
            {
                return null; // 계단 없음 반환
            }

            CardinalDirection facing = GetFacingDirection(); // 현재 바라보는 4방향 계산
            GridPosition delta = GridMovement.GetDirectionDelta(facing); // 정면 방향 변화량 계산
            GridPosition frontPosition = new GridPosition(playerState.CurrentGridPosition.X + delta.X, playerState.CurrentGridPosition.Z + delta.Z); // 정면 칸 좌표 계산

            foreach (RoomContentMarker marker in roomView.GetMarkers(RoomContentType.Stairs)) // 현재 방 계단 전체 확인
            {
                GridPosition markerPosition = marker.GridPosition; // 계단 좌표 조회

                if (markerPosition.X == frontPosition.X && markerPosition.Z == frontPosition.Z) // 정면 칸 일치 확인
                {
                    return marker; // 정면 계단 반환
                }
            }

            return null; // 계단 없음 반환
        }

        private CardinalDirection GetFacingDirection() // 현재 수평 시선 방향 계산
        {
            float yaw = viewTransform != null ? viewTransform.eulerAngles.y : transform.eulerAngles.y; // 현재 Yaw 읽기
            return GridMovement.GetFacingFromYaw(yaw); // Yaw를 4방향으로 변환
        }

        private void BuildPromptUi() // 계단 안내 UGUI 생성
        {
            if (promptCanvasObject != null) // 기존 Canvas 확인
            {
                return; // 중복 생성 방지
            }

            RuntimeUiFactory.EnsureEventSystem(); // 공용 EventSystem 준비

            promptCanvasObject = new GameObject( // 안내 Canvas 생성
                "StairsPromptCanvas", // Canvas 이름 지정
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
            promptRect.sizeDelta = new Vector2(1040f, 52f); // 긴 확인 문구 대응

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

        private void RefreshPromptUi() // 계단 안내 UGUI 갱신
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
