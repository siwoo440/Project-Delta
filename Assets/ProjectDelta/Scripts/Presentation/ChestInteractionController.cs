using System.Collections.Generic;
using ProjectDelta.Application;
using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectDelta.Presentation
{
    // 플레이어 정면의 상자를 열고 아이템 획득 및 인벤토리 초과 선택을 처리한다.
    public sealed class ChestInteractionController : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset inputActions;

        [SerializeField]
        private Transform viewTransform;

        [SerializeField]
        private PlayerGridMovementController movementController;

        [SerializeField]
        private PlayerLookController lookController;

        private InputActionMap explorationMap;
        private InputAction interactAction;

        private string promptText;
        private GUIStyle promptStyle;
        private GUIStyle panelStyle;
        private GUIStyle slotStyle;
        private GUIStyle warningStyle;

        private InventoryRunState inventory;
        private ChestContentMarker openChest;

        private bool isPanelOpen;
        private bool isOverflowPanelOpen;
        private bool isSelectingReplacement;

        private int pendingChestIndex = -1;
        private InventoryAcquisitionPlan pendingPlan;
        private string overflowStatusText;

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

            if (movementController == null)
            {
                movementController =
                    GetComponent<PlayerGridMovementController>();
            }

            if (lookController == null)
            {
                lookController =
                    GetComponent<PlayerLookController>();
            }

            inventory =
                RunContext.Current != null
                    ? RunContext.Current.Inventory
                    : new InventoryRunState();
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogError(
                    "[Project Delta] ChestInteractionController에 Input Actions가 지정되지 않았습니다.",
                    this);

                return;
            }

            explorationMap =
                inputActions.FindActionMap(
                    "Exploration",
                    true);

            interactAction =
                explorationMap.FindAction(
                    "Interact",
                    true);

            interactAction.performed +=
                OnInteract;

            explorationMap.Enable();
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.performed -=
                    OnInteract;
            }
        }

        private void Update()
        {
            if (isPanelOpen)
            {
                if (Keyboard.current != null
                    && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    if (isOverflowPanelOpen)
                    {
                        CancelPendingAcquisition();
                    }
                    else
                    {
                        ClosePanel();
                    }
                }

                return;
            }

            promptText =
                FindChestMarkerInFront() != null
                    ? "상자 열기 [F]"
                    : string.Empty;
        }

        private void OnInteract(
            InputAction.CallbackContext context)
        {
            if (isPanelOpen)
            {
                return;
            }

            if (movementController != null
                && movementController.IsMoving)
            {
                return;
            }

            ChestContentMarker chest =
                FindChestMarkerInFront();

            if (chest == null)
            {
                return;
            }

            OpenPanel(
                chest);
        }

        private void OpenPanel(
            ChestContentMarker chest)
        {
            openChest =
                chest;

            isPanelOpen =
                true;

            promptText =
                string.Empty;

            ResetPendingAcquisition();

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    true;
            }

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(
                    true);
            }

            // 기존 26일차 저장 규칙은 유지한다.
            RoomInstance roomInstance =
                movementController != null
                && movementController.CurrentPassageController != null
                    ? movementController.CurrentPassageController.CurrentInstance
                    : null;

            if (roomInstance != null)
            {
                roomInstance.MarkChestOpened();
            }

            ApplicationFlow.Current?.SaveDungeonProgress();
        }

        private void ClosePanel()
        {
            isPanelOpen =
                false;

            openChest =
                null;

            ResetPendingAcquisition();

            if (movementController != null)
            {
                movementController.IsInputLocked =
                    false;
            }

            if (lookController != null)
            {
                lookController.SetCursorFreeForUi(
                    false);
            }
        }

        private ChestContentMarker FindChestMarkerInFront()
        {
            RoomView roomView =
                movementController != null
                    ? movementController.CurrentRoomView
                    : null;

            PlayerRunState playerState =
                movementController != null
                    ? movementController.PlayerState
                    : null;

            if (roomView == null
                || playerState == null)
            {
                return null;
            }

            float yaw =
                viewTransform != null
                    ? viewTransform.eulerAngles.y
                    : transform.eulerAngles.y;

            CardinalDirection facing =
                GridMovement.GetFacingFromYaw(
                    yaw);

            GridPosition delta =
                GridMovement.GetDirectionDelta(
                    facing);

            GridPosition frontPosition =
                new GridPosition(
                    playerState.CurrentGridPosition.X
                        + delta.X,
                    playerState.CurrentGridPosition.Z
                        + delta.Z);

            foreach (RoomContentMarker marker
                     in roomView.GetMarkers(
                         RoomContentType.Chest))
            {
                GridPosition markerPosition =
                    marker.GridPosition;

                if (markerPosition.X
                        == frontPosition.X
                    && markerPosition.Z
                        == frontPosition.Z)
                {
                    return marker.GetComponent<ChestContentMarker>();
                }
            }

            return null;
        }

        private void OnGUI()
        {
            if (isPanelOpen)
            {
                DrawPanel();

                if (isOverflowPanelOpen)
                {
                    DrawOverflowPanel();
                }

                return;
            }

            if (string.IsNullOrEmpty(
                    promptText))
            {
                return;
            }

            EnsurePromptStyle();

            Rect promptRect =
                new Rect(
                    0f,
                    Screen.height * 0.72f,
                    Screen.width,
                    40f);

            GUI.Label(
                promptRect,
                promptText,
                promptStyle);
        }

        private void DrawPanel()
        {
            EnsurePanelStyles();

            float panelWidth =
                300f;

            float panelHeight =
                360f;

            float gap =
                40f;

            float centerX =
                Screen.width / 2f;

            float top =
                (Screen.height - panelHeight)
                / 2f;

            Rect inventoryRect =
                new Rect(
                    centerX
                        - (gap / 2f)
                        - panelWidth,
                    top,
                    panelWidth,
                    panelHeight);

            Rect chestRect =
                new Rect(
                    centerX
                        + (gap / 2f),
                    top,
                    panelWidth,
                    panelHeight);

            GUI.Box(
                inventoryRect,
                isSelectingReplacement
                    ? "교체할 슬롯 선택"
                    : "인벤토리",
                panelStyle);

            GUI.Box(
                chestRect,
                "상자",
                panelStyle);

            DrawInventorySlots(
                inventoryRect);

            DrawChestSlots(
                chestRect);

            bool previousEnabled =
                GUI.enabled;

            GUI.enabled =
                !isOverflowPanelOpen;

            Rect closeRect =
                new Rect(
                    centerX - 50f,
                    top + panelHeight + 12f,
                    100f,
                    32f);

            if (GUI.Button(
                    closeRect,
                    "닫기"))
            {
                ClosePanel();
            }

            GUI.enabled =
                previousEnabled;
        }

        private void DrawInventorySlots(
            Rect panelRect)
        {
            float y =
                panelRect.y + 36f;

            int visibleSlotCount =
                Mathf.Min(
                    InventoryRunState.BaseSlotCount,
                    inventory.Slots.Count);

            for (int index = 0;
                 index < visibleSlotCount;
                 index++)
            {
                InventorySlotState slot =
                    inventory.Slots[index];

                Rect slotRect =
                    new Rect(
                        panelRect.x + 10f,
                        y,
                        panelRect.width - 20f,
                        28f);

                string slotText =
                    slot == null
                    || slot.IsEmpty
                        ? $"{index + 1}. (빈 슬롯)"
                        : $"{index + 1}. {slot.DisplayName} ×{slot.Quantity}";

                if (!isSelectingReplacement)
                {
                    GUI.Label(
                        slotRect,
                        slotText,
                        slotStyle);

                    y += 30f;
                    continue;
                }

                ItemCategory category =
                    slot == null
                    || slot.IsEmpty
                        ? ItemCategory.Uncategorized
                        : RuntimeItemDefinitionLookup.ResolveCategory(
                            slot.ItemId);

                bool canReplace =
                    slot != null
                    && !slot.IsEmpty
                    && InventoryAcquisitionService.CanReplaceTarget(
                        category);

                bool previousEnabled =
                    GUI.enabled;

                GUI.enabled =
                    canReplace;

                if (GUI.Button(
                        slotRect,
                        slotText,
                        slotStyle))
                {
                    ResolveReplacement(
                        index,
                        category);
                }

                GUI.enabled =
                    previousEnabled;

                y += 30f;
            }
        }

        private void DrawChestSlots(
            Rect panelRect)
        {
            if (openChest == null)
            {
                return;
            }

            IReadOnlyList<string> items =
                openChest.RemainingItems;

            float y =
                panelRect.y + 36f;

            if (items.Count == 0)
            {
                GUI.Label(
                    new Rect(
                        panelRect.x + 10f,
                        y,
                        panelRect.width - 20f,
                        28f),
                    "(비어있음)",
                    slotStyle);

                return;
            }

            bool previousEnabled =
                GUI.enabled;

            GUI.enabled =
                !isOverflowPanelOpen;

            for (int index = 0;
                 index < items.Count;
                 index++)
            {
                Rect slotRect =
                    new Rect(
                        panelRect.x + 10f,
                        y,
                        panelRect.width - 20f,
                        28f);

                if (GUI.Button(
                        slotRect,
                        items[index],
                        slotStyle))
                {
                    BeginChestAcquisition(
                        index,
                        items[index]);

                    break;
                }

                y += 30f;
            }

            GUI.enabled =
                previousEnabled;
        }

        private void BeginChestAcquisition(
            int chestIndex,
            string itemKey)
        {
            int maxStackSize =
                RuntimeItemDefinitionLookup.ResolveMaxStackSize(
                    itemKey);

            InventoryAcquisitionPlan plan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    itemKey,
                    itemKey,
                    1,
                    maxStackSize);

            if (!plan.RequiresDecision)
            {
                InventoryAcquisitionCommitResult result =
                    InventoryAcquisitionService.CommitLeave(
                        inventory,
                        plan);

                if (result.AddedQuantity > 0)
                {
                    CompleteChestTake(
                        chestIndex);
                }

                return;
            }

            pendingChestIndex =
                chestIndex;

            pendingPlan =
                plan;

            isOverflowPanelOpen =
                true;

            isSelectingReplacement =
                false;

            overflowStatusText =
                $"인벤토리에 공간이 없습니다.\n{plan.DisplayName} ×{plan.RemainingQuantity}";
        }

        private void DrawOverflowPanel()
        {
            if (pendingPlan == null)
            {
                return;
            }

            EnsurePanelStyles();

            float width =
                420f;

            float height =
                isSelectingReplacement
                    ? 190f
                    : 230f;

            Rect panelRect =
                new Rect(
                    (Screen.width - width) / 2f,
                    60f,
                    width,
                    height);

            GUI.Box(
                panelRect,
                "인벤토리가 가득 찼습니다",
                panelStyle);

            Rect messageRect =
                new Rect(
                    panelRect.x + 20f,
                    panelRect.y + 45f,
                    panelRect.width - 40f,
                    65f);

            GUI.Label(
                messageRect,
                overflowStatusText,
                warningStyle);

            if (isSelectingReplacement)
            {
                Rect backRect =
                    new Rect(
                        panelRect.x + 135f,
                        panelRect.y + 135f,
                        150f,
                        36f);

                if (GUI.Button(
                        backRect,
                        "선택 취소"))
                {
                    isSelectingReplacement =
                        false;

                    overflowStatusText =
                        $"인벤토리에 공간이 없습니다.\n{pendingPlan.DisplayName} ×{pendingPlan.RemainingQuantity}";
                }

                return;
            }

            float buttonY =
                panelRect.y + 150f;

            Rect leaveRect =
                new Rect(
                    panelRect.x + 15f,
                    buttonY,
                    120f,
                    38f);

            Rect replaceRect =
                new Rect(
                    panelRect.x + 150f,
                    buttonY,
                    120f,
                    38f);

            Rect cancelRect =
                new Rect(
                    panelRect.x + 285f,
                    buttonY,
                    120f,
                    38f);

            if (GUI.Button(
                    leaveRect,
                    "두고 간다"))
            {
                ResolveLeave();
            }

            if (GUI.Button(
                    replaceRect,
                    "교체"))
            {
                isSelectingReplacement =
                    true;

                overflowStatusText =
                    "왼쪽 인벤토리에서 교체할 슬롯을 선택하세요.\n중요/유물/저주/미분류 아이템은 교체할 수 없습니다.";
            }

            if (GUI.Button(
                    cancelRect,
                    "취소"))
            {
                CancelPendingAcquisition();
            }
        }

        private void ResolveLeave()
        {
            if (pendingPlan == null)
            {
                ResetPendingAcquisition();
                return;
            }

            InventoryAcquisitionCommitResult result =
                InventoryAcquisitionService.CommitLeave(
                    inventory,
                    pendingPlan);

            // 현재 상자 데이터는 한 항목당 수량 1이므로 전량 획득된 경우에만 상자에서 제거한다.
            if (result.IsComplete
                && result.AddedQuantity > 0)
            {
                CompleteChestTake(
                    pendingChestIndex);
            }
            else if (result.AddedQuantity > 0)
            {
                ApplicationFlow.Current?.SaveDungeonProgress();
            }

            ResetPendingAcquisition();
        }

        private void ResolveReplacement(
            int targetSlotIndex,
            ItemCategory targetCategory)
        {
            if (pendingPlan == null)
            {
                ResetPendingAcquisition();
                return;
            }

            InventoryAcquisitionCommitResult result =
                InventoryAcquisitionService.CommitReplace(
                    inventory,
                    pendingPlan,
                    targetSlotIndex,
                    targetCategory);

            if (!result.ReplacementSucceeded)
            {
                overflowStatusText =
                    "이 슬롯의 아이템은 교체할 수 없습니다.";

                return;
            }

            if (result.IsComplete)
            {
                CompleteChestTake(
                    pendingChestIndex);

                ResetPendingAcquisition();
                return;
            }

            ApplicationFlow.Current?.SaveDungeonProgress();

            pendingPlan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    pendingPlan.ItemId,
                    pendingPlan.DisplayName,
                    result.RemainingQuantity,
                    pendingPlan.MaxStackSize);

            isSelectingReplacement =
                false;

            overflowStatusText =
                $"아직 {pendingPlan.RemainingQuantity}개가 남았습니다.\n추가로 교체하거나 두고 갈 수 있습니다.";
        }

        private void CancelPendingAcquisition()
        {
            if (pendingPlan != null)
            {
                InventoryAcquisitionService.CommitCancel(
                    pendingPlan);
            }

            ResetPendingAcquisition();
        }

        private void CompleteChestTake(
            int chestIndex)
        {
            if (openChest == null)
            {
                return;
            }

            if (openChest.TryTake(
                    chestIndex,
                    out _))
            {
                ApplicationFlow.Current?.SaveDungeonProgress();
            }
        }

        private void ResetPendingAcquisition()
        {
            pendingChestIndex =
                -1;

            pendingPlan =
                null;

            isOverflowPanelOpen =
                false;

            isSelectingReplacement =
                false;

            overflowStatusText =
                string.Empty;
        }

        private void EnsurePromptStyle()
        {
            if (promptStyle != null)
            {
                return;
            }

            promptStyle =
                new GUIStyle(
                    GUI.skin.label);

            promptStyle.alignment =
                TextAnchor.MiddleCenter;

            promptStyle.fontSize =
                22;

            promptStyle.normal.textColor =
                Color.white;
        }

        private void EnsurePanelStyles()
        {
            if (panelStyle == null)
            {
                panelStyle =
                    new GUIStyle(
                        GUI.skin.box);

                panelStyle.fontSize =
                    18;

                panelStyle.alignment =
                    TextAnchor.UpperCenter;

                panelStyle.normal.textColor =
                    Color.white;
            }

            if (slotStyle == null)
            {
                slotStyle =
                    new GUIStyle(
                        GUI.skin.button);

                slotStyle.alignment =
                    TextAnchor.MiddleLeft;

                slotStyle.fontSize =
                    16;
            }

            if (warningStyle == null)
            {
                warningStyle =
                    new GUIStyle(
                        GUI.skin.label);

                warningStyle.alignment =
                    TextAnchor.MiddleCenter;

                warningStyle.fontSize =
                    16;

                warningStyle.wordWrap =
                    true;

                warningStyle.normal.textColor =
                    Color.white;
            }
        }
    }
}
