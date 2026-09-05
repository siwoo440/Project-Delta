using System;
using System.Collections;
using ProjectDelta.Application;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectDelta.Presentation
{
    public sealed class PlayerGridMovementController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform viewTransform;
        [SerializeField] private RoomView currentRoomView;
        [SerializeField] private TestRoomTransitionController roomTransitionController;
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private float moveDuration = 0.15f;
        [SerializeField] private int minX = -2;
        [SerializeField] private int maxX = 2;
        [SerializeField] private int minZ = -2;
        [SerializeField] private int maxZ = 2;

        private PlayerRunState playerState;
        private bool isMoving;

        // 135일차: 기획서 8.1절 "입력 버퍼(이동 중 대기 행동 최대 1개)" - 이동 애니메이션이
        // 끝날 때까지 입력을 버리지 않고 딱 1개만 기억해뒀다가 이동이 끝나는 즉시 실행한다.
        // 새 입력이 또 들어오면 먼저 있던 대기 입력을 덮어쓴다(항상 최대 1개 유지).
        private GridMoveInput? pendingInput;

        private InputActionMap explorationMap;
        private InputAction moveForwardAction;
        private InputAction moveBackwardAction;
        private InputAction moveLeftAction;
        private InputAction moveRightAction;

        public PlayerRunState PlayerState =>
            playerState;

        public RoomView CurrentRoomView =>
            currentRoomView;

        public RoomPassageController CurrentPassageController =>
            currentRoomView != null
                ? currentRoomView.PassageController
                : null;

        public Transform CurrentRoomOrigin =>
            currentRoomView != null
                ? currentRoomView.transform
                : null;

        public bool IsMoving =>
            isMoving;

        public float CellSize => // 미니맵 월드 좌표 변환용 칸 크기 공개
            cellSize; // 현재 탐험 칸 크기 반환

        public bool IsInputLocked { get; set; }

        // 111일차: 방에 들어올 때마다(최초 진입인 currentRoomView, isFirstVisit)를 함께 알린다.
        // Combat/Event 방 트리거처럼 방 진입에 반응해야 하는 시스템이 여기 구독한다.
        public event Action<RoomView, bool> RoomEntered;

        private void Awake()
        {
            if (viewTransform == null)
            {
                Camera mainCamera =
                    Camera.main;

                viewTransform =
                    mainCamera != null
                        ? mainCamera.transform
                        : transform;
            }

            if (currentRoomView == null)
            {
                currentRoomView =
                    FindFirstObjectByType<RoomView>();
            }

            if (roomTransitionController == null)
            {
                roomTransitionController =
                    FindFirstObjectByType<TestRoomTransitionController>();
            }

            if (RunContext.Current != null)
            {
                playerState =
                    RunContext.Current.Player;

                if (string.IsNullOrEmpty(
                        playerState.CurrentRoomId)
                    && CurrentPassageController != null)
                {
                    playerState.CurrentRoomId =
                        CurrentPassageController.RoomId;
                }
                else if (CurrentPassageController != null
                    && playerState.CurrentRoomId
                        != CurrentPassageController.RoomId)
                {
                    DungeonFloorController floorController =
                        FindFirstObjectByType<DungeonFloorController>();

                    floorController?.EnsureCurrentFloorRoomExists();

                    RoomView restoredRoomView =
                        FindRoomViewById(
                            playerState.CurrentRoomId);

                    if (restoredRoomView != null)
                    {
                        currentRoomView =
                            restoredRoomView;
                    }
                }

                ApplyWorldPosition(
                    playerState.CurrentGridPosition);
            }
            else
            {
                playerState =
                    new PlayerRunState();

                playerState.CurrentGridPosition =
                    WorldToGridPosition(
                        transform.position);

                playerState.CurrentRoomId =
                    CurrentPassageController != null
                        ? CurrentPassageController.RoomId
                        : "TestRoom_A";

                playerState.KeyCount =
                    1;
            }

            if (currentRoomView != null
                && CurrentPassageController != null
                && CurrentPassageController.CurrentInstance != null)
            {
                bool isFirstVisit =
                    CurrentPassageController.CurrentInstance.MarkVisited();

                RoomEntered?.Invoke(
                    currentRoomView,
                    isFirstVisit);
            }
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError(
                    "[Project Delta] PlayerGridMovementController에 Input Actions가 지정되지 않았습니다.",
                    this);

                return;
            }

            inputActions.Disable();

            explorationMap =
                inputActions.FindActionMap(
                    "Exploration",
                    true);

            moveForwardAction =
                explorationMap.FindAction(
                    "MoveForward",
                    true);

            moveBackwardAction =
                explorationMap.FindAction(
                    "MoveBackward",
                    true);

            moveLeftAction =
                explorationMap.FindAction(
                    "MoveLeft",
                    true);

            moveRightAction =
                explorationMap.FindAction(
                    "MoveRight",
                    true);

            moveForwardAction.performed +=
                OnMoveForward;

            moveBackwardAction.performed +=
                OnMoveBackward;

            moveLeftAction.performed +=
                OnMoveLeft;

            moveRightAction.performed +=
                OnMoveRight;

            explorationMap.Enable();
        }

        private void OnDisable()
        {
            if (moveForwardAction != null)
            {
                moveForwardAction.performed -=
                    OnMoveForward;
            }

            if (moveBackwardAction != null)
            {
                moveBackwardAction.performed -=
                    OnMoveBackward;
            }

            if (moveLeftAction != null)
            {
                moveLeftAction.performed -=
                    OnMoveLeft;
            }

            if (moveRightAction != null)
            {
                moveRightAction.performed -=
                    OnMoveRight;
            }

            explorationMap?.Disable();

            if (inputActions != null)
            {
                InputActionMap uiMap =
                    inputActions.FindActionMap(
                        "UI",
                        false);

                uiMap?.Enable();
            }
        }

        private void OnMoveForward(
            InputAction.CallbackContext context)
        {
            TryMove(
                GridMoveInput.Forward);
        }

        private void OnMoveBackward(
            InputAction.CallbackContext context)
        {
            TryMove(
                GridMoveInput.Backward);
        }

        private void OnMoveLeft(
            InputAction.CallbackContext context)
        {
            TryMove(
                GridMoveInput.Left);
        }

        private void OnMoveRight(
            InputAction.CallbackContext context)
        {
            TryMove(
                GridMoveInput.Right);
        }

        private void TryMove(
            GridMoveInput input)
        {
            if (playerState == null
                || IsInputLocked)
            {
                // 입력이 잠긴 동안(NPC 대화·전투 진입 등) 대기 중이던 입력은 버린다 -
                // 잠금이 풀린 뒤 예전 입력이 갑자기 실행되면 안 된다.
                pendingInput =
                    null;

                return;
            }

            if (isMoving)
            {
                // 135일차: 이동 중 입력은 버리지 않고 대기시킨다 - 여러 번 눌러도 마지막
                // 입력 하나만 남는다(최대 1개 유지).
                pendingInput =
                    input;

                return;
            }

            // 83일차: 기절은 이동 입력 자체를 한 번 소비하고 지속 횟수만 1 감소시킨다.
            if (ExplorationStatusEffectService.TryConsumeStunMoveAttempt(
                    playerState))
            {
                Debug.Log(
                    "[Project Delta] 83일차 탐험 이동 차단 / 기절 상태 1회 소모",
                    this);

                return;
            }

            float yaw =
                viewTransform != null
                    ? viewTransform.eulerAngles.y
                    : transform.eulerAngles.y;

            CardinalDirection facing =
                GridMovement.GetFacingFromYaw(
                    yaw);

            CardinalDirection moveDirection =
                GridMovement.GetMoveDirection(
                    facing,
                    input);

            GridPosition delta =
                GridMovement.GetMoveDelta(
                    facing,
                    input);

            GridPosition target =
                new GridPosition(
                    playerState.CurrentGridPosition.X + delta.X,
                    playerState.CurrentGridPosition.Z + delta.Z);

            GridBounds bounds =
                new GridBounds(
                    minX,
                    maxX,
                    minZ,
                    maxZ);

            RoomPassageController passageController =
                CurrentPassageController;

            if (passageController != null
                && !passageController.CanPass(
                    playerState.CurrentGridPosition,
                    moveDirection))
            {
                Debug.Log(
                    $"[Project Delta] 이동 불가: {playerState.CurrentGridPosition} / {moveDirection} 통로 차단",
                    this);

                return;
            }

            if (bounds.Contains(
                    target))
            {
                // 113일차: 상자와 NPC처럼 한 칸을 점유하는 활성 콘텐츠는 플레이어가 통과하지 못한다.
                if (IsBlockedBySolidContent(
                        target))
                {
                    Debug.Log(
                        $"[Project Delta] 이동 불가: {target} 콘텐츠 칸 점유",
                        this);

                    return;
                }

                CommitGridMove(
                    target,
                    facing);

                return;
            }

            TryMoveAcrossRoomBoundary(
                moveDirection,
                facing);
        }

        private bool IsBlockedBySolidContent(
            GridPosition target)
        {
            if (currentRoomView == null)
            {
                return false;
            }

            return HasActiveMarkerAt(
                    RoomContentType.Chest,
                    target)
                || HasActiveMarkerAt(
                    RoomContentType.NpcPoint,
                    target);
        }

        private bool HasActiveMarkerAt(
            RoomContentType contentType,
            GridPosition target)
        {
            foreach (RoomContentMarker marker
                     in currentRoomView.GetMarkers(
                         contentType))
            {
                if (marker != null
                    && marker.gameObject.activeInHierarchy
                    && marker.GridPosition == target)
                {
                    return true;
                }
            }

            return false;
        }

        private void TryMoveAcrossRoomBoundary(
            CardinalDirection moveDirection,
            CardinalDirection facing)
        {
            RoomPassageController passageController =
                CurrentPassageController;

            if (roomTransitionController == null
                || passageController == null)
            {
                Debug.Log(
                    "[Project Delta] 이동 불가: 연결된 테스트 방 정보가 없습니다.",
                    this);

                return;
            }

            if (!roomTransitionController.TryTransition(
                    playerState.CurrentRoomId,
                    playerState.CurrentGridPosition,
                    moveDirection,
                    out RoomPassageController destinationRoom,
                    out GridPosition destinationEntryPosition))
            {
                Debug.Log(
                    $"[Project Delta] 이동 불가: {playerState.CurrentRoomId} / {moveDirection} 방향 연결 방 없음",
                    this);

                return;
            }

            RoomView destinationRoomView =
                destinationRoom.GetComponent<RoomView>();

            if (destinationRoomView == null)
            {
                Debug.LogError(
                    $"[Project Delta] {destinationRoom.RoomId}에 RoomView가 없습니다.",
                    this);

                return;
            }

            EnterRoom(
                destinationRoomView,
                destinationEntryPosition,
                facing);
        }

        public void EnterRoom(
            RoomView roomView,
            GridPosition entryPosition,
            CardinalDirection facing)
        {
            if (roomView == null
                || playerState == null)
            {
                return;
            }

            currentRoomView =
                roomView;

            playerState.CurrentRoomId =
                roomView.PassageController != null
                    ? roomView.PassageController.RoomId
                    : roomView.name;

            playerState.CurrentGridPosition =
                entryPosition;

            bool isFirstVisit =
                roomView.PassageController != null
                && roomView.PassageController.CurrentInstance != null
                && roomView.PassageController.CurrentInstance.MarkVisited();

            RoomEntered?.Invoke(
                roomView,
                isFirstVisit);

            // 83일차: 방 경계를 넘는 이동도 성공한 이동 1회로 처리한다.
            if (ApplyExplorationStatusTick())
            {
                return;
            }

            // 상태이상 틱까지 반영한 뒤 저장해 이어하기에서도 같은 상태를 복원한다.
            ApplicationFlow.Current?.SaveDungeonProgress();

            StartCoroutine(
                MoveRoutine(
                    CalculateWorldPosition(
                        entryPosition)));

            Debug.Log(
                $"[Project Delta] 방 진입: {playerState.CurrentRoomId} / Entry {entryPosition} / Facing {facing} / 최초 방문 {isFirstVisit}",
                this);
        }

        private void CommitGridMove(
            GridPosition target,
            CardinalDirection facing)
        {
            playerState.CurrentGridPosition =
                target;

            if (ApplyExplorationStatusTick())
            {
                return;
            }

            StartCoroutine(
                MoveRoutine(
                    CalculateWorldPosition(
                        target)));

            Debug.Log(
                $"[Project Delta] GridPosition {target} / Room {playerState.CurrentRoomId} / Facing {facing}",
                this);
        }

        private bool ApplyExplorationStatusTick()
        {
            bool defeated =
                ExplorationStatusEffectService.ApplyAfterSuccessfulMove(
                    playerState);

            if (!defeated)
            {
                return false;
            }

            Debug.Log(
                "[Project Delta] 83일차 탐험 상태이상으로 플레이어 HP가 0이 되었습니다.",
                this);

            ApplicationFlow.Current?.EnterDefeat();

            return true;
        }

        private RoomView FindRoomViewById(
            string roomId)
        {
            foreach (RoomView candidate
                     in FindObjectsByType<RoomView>(
                         FindObjectsSortMode.None))
            {
                if (candidate.PassageController != null
                    && candidate.PassageController.RoomId
                        == roomId)
                {
                    return candidate;
                }
            }

            return null;
        }

        private GridPosition WorldToGridPosition(
            Vector3 worldPosition)
        {
            Vector3 localPosition =
                CurrentRoomOrigin != null
                    ? CurrentRoomOrigin.InverseTransformPoint(
                        worldPosition)
                    : worldPosition;

            int gridX =
                Mathf.RoundToInt(
                    localPosition.x / cellSize);

            int gridZ =
                Mathf.RoundToInt(
                    localPosition.z / cellSize);

            return new GridPosition(
                gridX,
                gridZ);
        }

        private Vector3 CalculateWorldPosition(
            GridPosition gridPosition)
        {
            Vector3 localPosition =
                new Vector3(
                    gridPosition.X * cellSize,
                    0f,
                    gridPosition.Z * cellSize);

            Vector3 targetWorldPosition =
                CurrentRoomOrigin != null
                    ? CurrentRoomOrigin.TransformPoint(
                        localPosition)
                    : localPosition;

            targetWorldPosition.y =
                transform.position.y;

            return targetWorldPosition;
        }

        private void ApplyWorldPosition(
            GridPosition gridPosition)
        {
            transform.position =
                CalculateWorldPosition(
                    gridPosition);
        }

        private IEnumerator MoveRoutine(
            Vector3 targetWorldPosition)
        {
            isMoving =
                true;

            Vector3 startWorldPosition =
                transform.position;

            float elapsed =
                0f;

            while (elapsed < moveDuration)
            {
                elapsed +=
                    Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed / moveDuration);

                float smoothT =
                    t * t * (3f - 2f * t);

                transform.position =
                    Vector3.Lerp(
                        startWorldPosition,
                        targetWorldPosition,
                        smoothT);

                yield return null;
            }

            transform.position =
                targetWorldPosition;

            isMoving =
                false;

            // 135일차: 이동이 끝난 시점에 대기 중이던 입력이 있으면 곧바로 소비한다.
            if (pendingInput.HasValue)
            {
                GridMoveInput bufferedInput =
                    pendingInput.Value;

                pendingInput =
                    null;

                TryMove(
                    bufferedInput);
            }
        }
    }
}
