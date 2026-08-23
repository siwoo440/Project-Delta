using System.Collections.Generic; // 목록 기능 사용
using ProjectDelta.Domain; // 도메인 좌표·인벤토리 규칙 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Input System 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 25일차: 문(14일차)/계단(22일차)과 같은 "정면 감지 + F" 패턴으로 상자를 연다.
    // 상자 칸 자체는 계단처럼 벽으로 막혀 있어 밟고 들어갈 수 없다 (RoomDefinition 참고).
    // 인벤토리(6.4절)가 정식으로 생기기 전이라, 아이템은 이름 문자열 수준의 자리표시자다.
    public sealed class ChestInteractionController : MonoBehaviour // 플레이어 상자 상호작용 제어
    {
        [SerializeField] private InputActionAsset inputActions; // 프로젝트 입력 액션 에셋
        [SerializeField] private Transform viewTransform; // 바라보는 방향 기준 Transform
        [SerializeField] private PlayerGridMovementController movementController; // 플레이어 이동 상태 컨트롤러
        [SerializeField] private PlayerLookController lookController; // 플레이어 시점 컨트롤러 (패널 여는 동안 커서 해제용)

        private InputActionMap explorationMap; // 탐험 입력 맵
        private InputAction interactAction; // 상호작용 액션
        private string promptText; // 현재 화면 안내 문구
        private GUIStyle promptStyle; // 안내 문구 GUI 스타일
        private GUIStyle panelStyle; // 패널 제목 스타일
        private GUIStyle slotStyle; // 아이템 칸 스타일

        private InventoryRunState inventory; // 실제 런이 없는 테스트 씬에서도 동작하도록 하는 로컬 인벤토리 (20~22일차와 동일 패턴)
        private ChestContentMarker openChest; // 현재 열려있는 상자
        private bool isPanelOpen; // 상자 패널 표시 여부

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

            if (lookController == null) // 시점 컨트롤러 미지정 확인
            {
                lookController = GetComponent<PlayerLookController>(); // 같은 Player의 시점 컨트롤러 연결
            }

            // 실제 런이 있으면 그 인벤토리를, 없으면(테스트 씬) 로컬 인벤토리를 사용한다.
            inventory = RunContext.Current != null ? RunContext.Current.Inventory : new InventoryRunState();
        }

        private void OnEnable() // 상호작용 입력 활성화
        {
            if (inputActions == null) // 입력 에셋 미지정 확인
            {
                Debug.LogError("[Project Delta] ChestInteractionController에 Input Actions가 지정되지 않았습니다.", this); // 입력 에셋 오류 출력
                return; // 입력 연결 중단
            }

            explorationMap = inputActions.FindActionMap("Exploration", true); // 탐험 입력 맵 검색
            interactAction = explorationMap.FindAction("Interact", true); // 상호작용 액션 검색 (문/계단과 동일한 F 입력 공유)
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

        private void Update() // 정면 상자 안내 갱신 및 패널 닫기 입력 확인
        {
            if (isPanelOpen) // 패널이 열려있는지 확인
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) // Esc 입력 확인
                {
                    ClosePanel(); // 패널 닫기
                }

                return; // 패널이 열린 동안은 정면 안내 갱신 생략
            }

            promptText = FindChestMarkerInFront() != null ? "상자 열기 [F]" : string.Empty; // 정면 상자 여부에 따라 안내 갱신
        }

        private void OnInteract(InputAction.CallbackContext context) // F 상호작용 처리
        {
            if (isPanelOpen) // 이미 패널이 열려있는지 확인
            {
                return; // 중복 열기 방지 (닫기는 Esc 전용)
            }

            if (movementController != null && movementController.IsMoving) // 이동 보간 중 상호작용 차단 (14일차와 동일 규칙)
            {
                return; // 이동 중 상호작용 중단
            }

            ChestContentMarker chest = FindChestMarkerInFront(); // 정면 상자 조회

            if (chest == null) // 정면 상자 존재 확인
            {
                return; // 상자 없음, 상호작용 중단
            }

            OpenPanel(chest); // 상자 패널 열기
        }

        private void OpenPanel(ChestContentMarker chest) // 상자 패널 열기
        {
            openChest = chest; // 대상 상자 기록
            isPanelOpen = true; // 패널 표시 시작
            promptText = string.Empty; // 정면 안내 문구 숨김

            if (movementController != null) // 이동 컨트롤러 존재 확인
            {
                movementController.IsInputLocked = true; // 패널이 열린 동안 이동 차단
            }

            if (lookController != null) // 시점 컨트롤러 존재 확인
            {
                lookController.SetCursorFreeForUi(true); // 커서 해제 및 시점 회전 중단, 클릭 가능하게 전환
            }
        }

        private void ClosePanel() // 상자 패널 닫기
        {
            isPanelOpen = false; // 패널 표시 종료
            openChest = null; // 대상 상자 해제

            if (movementController != null) // 이동 컨트롤러 존재 확인
            {
                movementController.IsInputLocked = false; // 이동 차단 해제
            }

            if (lookController != null) // 시점 컨트롤러 존재 확인
            {
                lookController.SetCursorFreeForUi(false); // 커서 다시 잠그고 시점 회전 재개
            }
        }

        private ChestContentMarker FindChestMarkerInFront() // 플레이어 정면 칸의 상자 자리 조회
        {
            RoomView roomView = movementController != null ? movementController.CurrentRoomView : null; // 현재 방 표시 진입점 조회
            PlayerRunState playerState = movementController != null ? movementController.PlayerState : null; // 현재 플레이어 상태 조회

            if (roomView == null || playerState == null) // 필요한 참조 확인
            {
                return null; // 상자 없음 반환
            }

            float yaw = viewTransform != null ? viewTransform.eulerAngles.y : transform.eulerAngles.y; // 현재 수평 시선 각도 조회
            CardinalDirection facing = GridMovement.GetFacingFromYaw(yaw); // 현재 바라보는 4방향 계산
            GridPosition delta = GridMovement.GetDirectionDelta(facing); // 정면 방향 변화량 계산
            GridPosition frontPosition = new GridPosition(playerState.CurrentGridPosition.X + delta.X, playerState.CurrentGridPosition.Z + delta.Z); // 정면 칸 좌표 계산

            foreach (RoomContentMarker marker in roomView.GetMarkers(RoomContentType.Chest)) // 현재 방의 상자 자리 전체 확인
            {
                GridPosition markerPosition = marker.GridPosition; // 상자 자리 그리드 좌표 조회

                if (markerPosition.X == frontPosition.X && markerPosition.Z == frontPosition.Z) // 정면 칸과 일치 확인
                {
                    return marker.GetComponent<ChestContentMarker>(); // 같은 오브젝트의 상자 내용물 컴포넌트 반환
                }
            }

            return null; // 상자 없음 반환
        }

        private void OnGUI() // 안내 문구 또는 상자 패널 표시
        {
            if (isPanelOpen) // 패널 표시 여부 확인
            {
                DrawPanel(); // 상자 패널 표시
                return; // 정면 안내는 패널이 열린 동안 생략
            }

            if (string.IsNullOrEmpty(promptText)) // 안내 문구 존재 여부 확인
            {
                return; // GUI 표시 생략
            }

            EnsurePromptStyle(); // 안내 스타일 준비
            Rect promptRect = new Rect(0f, Screen.height * 0.72f, Screen.width, 40f); // 화면 하단 중앙 영역 계산 (문/계단 안내와 동일 위치)
            GUI.Label(promptRect, promptText, promptStyle); // 상자 상호작용 안내 표시
        }

        private void DrawPanel() // 인벤토리·상자 두 칸 패널 표시
        {
            EnsurePanelStyles(); // 패널 스타일 준비

            float panelWidth = 260f; // 패널 한 칸 가로 크기
            float panelHeight = 320f; // 패널 세로 크기
            float gap = 40f; // 두 패널 사이 간격
            float centerX = Screen.width / 2f; // 화면 가로 중앙 좌표
            float top = (Screen.height - panelHeight) / 2f; // 패널 세로 시작 좌표

            Rect inventoryRect = new Rect(centerX - (gap / 2f) - panelWidth, top, panelWidth, panelHeight); // 좌측 인벤토리 패널 영역
            Rect chestRect = new Rect(centerX + (gap / 2f), top, panelWidth, panelHeight); // 우측 상자 패널 영역

            GUI.Box(inventoryRect, "인벤토리", panelStyle); // 인벤토리 패널 배경/제목
            GUI.Box(chestRect, "상자", panelStyle); // 상자 패널 배경/제목

            DrawInventorySlots(inventoryRect); // 인벤토리 내용물 표시
            DrawChestSlots(chestRect); // 상자 내용물 표시 (클릭 시 가져오기)

            Rect closeRect = new Rect(centerX - 50f, top + panelHeight + 12f, 100f, 32f); // 닫기 버튼 영역
            if (GUI.Button(closeRect, "닫기")) // 닫기 버튼
            {
                ClosePanel(); // 패널 닫기
            }
        }

        private void DrawInventorySlots(Rect panelRect) // 인벤토리 아이템 목록 표시 (읽기 전용)
        {
            float y = panelRect.y + 36f; // 첫 항목 세로 시작 좌표

            for (int i = 0; i < inventory.Items.Count; i++) // 보유 아이템 전체 반복
            {
                Rect slotRect = new Rect(panelRect.x + 10f, y, panelRect.width - 20f, 28f); // 항목 표시 영역 계산
                GUI.Label(slotRect, inventory.Items[i].DisplayName, slotStyle); // 아이템 이름 표시
                y += 30f; // 다음 항목 위치로 이동
            }
        }

        private void DrawChestSlots(Rect panelRect) // 상자 아이템 목록 표시 (클릭하면 인벤토리로 이동)
        {
            if (openChest == null) // 대상 상자 존재 확인
            {
                return; // 표시할 내용 없음
            }

            IReadOnlyList<string> items = openChest.RemainingItems; // 남은 아이템 목록 조회
            float y = panelRect.y + 36f; // 첫 항목 세로 시작 좌표

            if (items.Count == 0) // 상자가 비어있는지 확인
            {
                GUI.Label(new Rect(panelRect.x + 10f, y, panelRect.width - 20f, 28f), "(비어있음)", slotStyle); // 빈 상자 안내 표시
                return; // 항목 없음, 표시 종료
            }

            for (int i = 0; i < items.Count; i++) // 남은 아이템 전체 반복
            {
                Rect slotRect = new Rect(panelRect.x + 10f, y, panelRect.width - 20f, 28f); // 항목 버튼 영역 계산

                if (GUI.Button(slotRect, items[i], slotStyle)) // 아이템 클릭 확인
                {
                    if (openChest.TryTake(i, out string takenName)) // 상자에서 꺼내기 시도
                    {
                        inventory.Add(new InventoryItemStack(takenName, takenName)); // 인벤토리에 추가
                    }

                    break; // 이번 프레임엔 목록이 바뀌었으므로 여기서 멈춤
                }

                y += 30f; // 다음 항목 위치로 이동
            }
        }

        private void EnsurePromptStyle() // 안내 문구 GUI 스타일 최초 1회 생성
        {
            if (promptStyle == null) // 안내 스타일 존재 확인
            {
                promptStyle = new GUIStyle(GUI.skin.label); // 기본 라벨 스타일 복제
                promptStyle.alignment = TextAnchor.MiddleCenter; // 가운데 정렬 적용
                promptStyle.fontSize = 22; // 안내 글자 크기 적용
                promptStyle.normal.textColor = Color.white; // 안내 글자 흰색 적용
            }
        }

        private void EnsurePanelStyles() // 패널 GUI 스타일 최초 1회 생성
        {
            if (panelStyle == null) // 패널 스타일 존재 확인
            {
                panelStyle = new GUIStyle(GUI.skin.box); // 기본 박스 스타일 복제
                panelStyle.fontSize = 18; // 제목 글자 크기 적용
                panelStyle.alignment = TextAnchor.UpperCenter; // 위쪽 가운데 정렬 적용
                panelStyle.normal.textColor = Color.white; // 제목 글자 흰색 적용
            }

            if (slotStyle == null) // 항목 스타일 존재 확인
            {
                slotStyle = new GUIStyle(GUI.skin.button); // 기본 버튼 스타일 복제
                slotStyle.alignment = TextAnchor.MiddleLeft; // 왼쪽 정렬 적용
                slotStyle.fontSize = 16; // 항목 글자 크기 적용
            }
        }
    }
}
