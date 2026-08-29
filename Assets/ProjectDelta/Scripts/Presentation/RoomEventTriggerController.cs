using ProjectDelta.Application;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 111일차: RoomType.Event 방에 처음 들어왔을 때 EventHudController를 자동으로 연다.
    // 실제 표시할 EventDefinition을 고르는 무작위 판정은 Application 계층
    // (RoomEventPoolService)이 담당하고, 여기서는 "이 방이 Event 타입이고 아직
    // 이벤트를 띄운 적 없는지"만 판단한다.
    [DisallowMultipleComponent]
    public sealed class RoomEventTriggerController : MonoBehaviour
    {
        [SerializeField] private PlayerGridMovementController movementController;
        [SerializeField] private EventHudController eventHudController;
        [SerializeField] private EventDefinition[] eventPool;

        private void Awake()
        {
            if (movementController == null)
            {
                movementController =
                    FindFirstObjectByType<PlayerGridMovementController>();
            }

            if (eventHudController == null)
            {
                eventHudController =
                    FindFirstObjectByType<EventHudController>();
            }
        }

        private void OnEnable()
        {
            if (movementController == null)
            {
                return;
            }

            movementController.RoomEntered +=
                HandleRoomEntered;

            // Awake() 실행 순서는 스크립트 간에 보장되지 않는다. 최초 진입 방의
            // RoomEntered가 이 구독보다 먼저 발생했을 수 있으므로, 구독 시점에
            // 현재 방을 한 번 더 직접 확인해 놓친 최초 진입을 보정한다.
            if (movementController.CurrentRoomView != null)
            {
                HandleRoomEntered(
                    movementController.CurrentRoomView,
                    true);
            }
        }

        private void OnDisable()
        {
            if (movementController != null)
            {
                movementController.RoomEntered -=
                    HandleRoomEntered;
            }
        }

        private void HandleRoomEntered(
            RoomView roomView,
            bool isFirstVisit)
        {
            if (!isFirstVisit
                || eventHudController == null
                || roomView == null
                || roomView.PassageController == null)
            {
                return;
            }

            RoomInstance room =
                roomView.PassageController.CurrentInstance;

            if (room == null
                || room.RoomType != RoomType.Event)
            {
                return;
            }

            EventDefinition definition =
                RoomEventPoolService.Pick(
                    eventPool);

            if (definition == null
                || !RoomEventService.TryMarkTriggered(
                    room))
            {
                return;
            }

            eventHudController.Open(
                definition);
        }
    }
}
