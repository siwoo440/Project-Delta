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

        [Header("UI")]
        [SerializeField]
        private ChestInteractionView interactionView;

        private bool interactionViewHooked;

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

            ResolveInteractionView();
            HookInteractionView();

            interactionView?.SetPanelVisible(
                false);
        }

        private void OnEnable()
        {
            ResolveInteractionView();
            HookInteractionView();

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

            UnhookInteractionView();
        }

        private void Update()
        {
            ResolveInteractionView();
            HookInteractionView();

            if (isPanelOpen)
            {
                if (Keyboard.current != null
                    && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    if (isOverflowPanelOpen)
                    {
                        CancelPendingAcquisition();
                        RefreshInteractionView();
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

            interactionView?.SetInteractionPrompt(
                promptText);
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

            interactionView?.SetInteractionPrompt(
                string.Empty);

            interactionView?.SetPanelVisible(
                true);

            RefreshInteractionView();
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

            interactionView?.SetPanelVisible(
                false);
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
                // 112일차: 내용물을 다 가져가 감춰진(SetActive(false)) 상자는
                // 더 이상 상호작용 대상이 아니다.
                if (marker == null
                    || !marker.gameObject.activeInHierarchy)
                {
                    continue;
                }

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

        private void ResolveInteractionView()
        {
            if (interactionView != null)
            {
                return;
            }

            interactionView =
                FindFirstObjectByType<ChestInteractionView>();
        }

        private void HookInteractionView()
        {
            if (interactionView == null
                || interactionViewHooked)
            {
                return;
            }

            interactionView.ChestItemClicked += HandleChestItemClicked;
            interactionView.ReplacementSlotClicked += HandleReplacementSlotClicked;
            interactionView.CloseRequested += ClosePanel;
            interactionView.LeaveRequested += HandleLeaveRequested;
            interactionView.BeginReplacementRequested += HandleBeginReplacementRequested;
            interactionView.CancelAcquisitionRequested += HandleCancelAcquisitionRequested;
            interactionView.BackReplacementRequested += HandleBackReplacementRequested;

            interactionViewHooked = true;
        }

        private void UnhookInteractionView()
        {
            if (interactionView == null
                || !interactionViewHooked)
            {
                return;
            }

            interactionView.ChestItemClicked -= HandleChestItemClicked;
            interactionView.ReplacementSlotClicked -= HandleReplacementSlotClicked;
            interactionView.CloseRequested -= ClosePanel;
            interactionView.LeaveRequested -= HandleLeaveRequested;
            interactionView.BeginReplacementRequested -= HandleBeginReplacementRequested;
            interactionView.CancelAcquisitionRequested -= HandleCancelAcquisitionRequested;
            interactionView.BackReplacementRequested -= HandleBackReplacementRequested;

            interactionViewHooked = false;
        }

        private void RefreshInteractionView()
        {
            if (interactionView == null)
            {
                return;
            }

            interactionView.SetInteractionPrompt(string.Empty);

            if (!isPanelOpen)
            {
                interactionView.SetPanelVisible(false);
                return;
            }

            interactionView.Render(
                openChest != null
                    ? openChest.RemainingItems
                    : null,
                inventory,
                isOverflowPanelOpen,
                isSelectingReplacement,
                overflowStatusText);
        }

        private void HandleChestItemClicked(int chestIndex)
        {
            if (isOverflowPanelOpen
                || openChest == null)
            {
                return;
            }

            IReadOnlyList<string> items =
                openChest.RemainingItems;

            if (items == null
                || chestIndex < 0
                || chestIndex >= items.Count)
            {
                return;
            }

            BeginChestAcquisition(
                chestIndex,
                items[chestIndex]);

            RefreshInteractionView();
        }

        private void HandleReplacementSlotClicked(int targetSlotIndex)
        {
            if (!isOverflowPanelOpen
                || !isSelectingReplacement
                || pendingPlan == null
                || inventory == null
                || !inventory.TryGetSlot(
                    targetSlotIndex,
                    out InventorySlotState slot)
                || slot == null
                || slot.IsEmpty)
            {
                return;
            }

            ItemCategory category =
                RuntimeItemDefinitionLookup.ResolveCategory(
                    slot.ItemId);

            if (!InventoryAcquisitionService.CanReplaceTarget(category))
            {
                return;
            }

            ResolveReplacement(
                targetSlotIndex,
                category);

            RefreshInteractionView();
        }

        private void HandleLeaveRequested()
        {
            ResolveLeave();
            RefreshInteractionView();
        }

        private void HandleBeginReplacementRequested()
        {
            if (pendingPlan == null)
            {
                return;
            }

            isSelectingReplacement = true;

            overflowStatusText =
                "교체할 인벤토리 슬롯을 선택하세요.\n중요/유물/저주/미분류 아이템은 교체할 수 없습니다.";

            RefreshInteractionView();
        }

        private void HandleCancelAcquisitionRequested()
        {
            CancelPendingAcquisition();
            RefreshInteractionView();
        }

        private void HandleBackReplacementRequested()
        {
            if (pendingPlan == null)
            {
                return;
            }

            isSelectingReplacement = false;

            overflowStatusText =
                $"인벤토리에 공간이 없습니다.\n{pendingPlan.DisplayName} ×{pendingPlan.RemainingQuantity}";

            RefreshInteractionView();
        }

        private void BeginChestAcquisition(
            int chestIndex,
            string itemKey)
        {
            string canonicalItemId =
                RuntimeItemDefinitionLookup.ResolveCanonicalItemId(
                    itemKey);

            string displayName =
                RuntimeItemDefinitionLookup.ResolveDisplayName(
                    itemKey);

            int maxStackSize =
                RuntimeItemDefinitionLookup.ResolveMaxStackSize(
                    itemKey);

            InventoryAcquisitionPlan plan =
                InventoryAcquisitionService.Preview(
                    inventory,
                    canonicalItemId,
                    displayName,
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


    }
}
