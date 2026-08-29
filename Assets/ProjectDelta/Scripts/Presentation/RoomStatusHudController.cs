using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectDelta.Presentation
{
    // 110일차: 화면 상단에 현재 방의 종류(RoomType)를 표시한다.
    // PlayerInventoryHudController(96일차)와 같은 패턴 - 방이 실제로 바뀔 때만
    // 텍스트를 다시 그려서 매 프레임 갱신을 피한다.
    [DisallowMultipleComponent]
    public sealed class RoomStatusHudController : MonoBehaviour
    {
        [SerializeField]
        private PlayerGridMovementController movementController;

        [SerializeField]
        private Text roomStatusText;

        private string lastRoomId;
        private bool hasLastRoomId;

        private void Awake()
        {
            if (movementController == null)
            {
                movementController =
                    FindFirstObjectByType<PlayerGridMovementController>();
            }
        }

        private void Update()
        {
            RoomInstance room =
                ResolveCurrentRoom();

            string roomId =
                room != null
                    ? room.RoomId
                    : null;

            if (hasLastRoomId
                && roomId == lastRoomId)
            {
                return;
            }

            lastRoomId =
                roomId;

            hasLastRoomId =
                true;

            Refresh(
                room);
        }

        private RoomInstance ResolveCurrentRoom()
        {
            RoomPassageController passage =
                movementController != null
                    ? movementController.CurrentPassageController
                    : null;

            return passage != null
                ? passage.CurrentInstance
                : null;
        }

        private void Refresh(
            RoomInstance room)
        {
            if (roomStatusText == null)
            {
                return;
            }

            if (room == null)
            {
                roomStatusText.text =
                    string.Empty;

                return;
            }

            string typeName =
                RoomTypeRules.GetDisplayName(
                    room.RoomType);

            // 함정 방은 이미 처리(회피/피해 적용)됐는지도 함께 보여준다.
            roomStatusText.text =
                room.RoomType == RoomType.Trap
                && room.TrapTriggered
                    ? $"현재 방: {typeName} (해제됨)"
                    : $"현재 방: {typeName}";
        }
    }
}
