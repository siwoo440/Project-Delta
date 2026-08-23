using ProjectDelta.Domain; // 도메인 방 연결 규칙 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation
{
    public sealed class TestRoomTransitionController : MonoBehaviour
    {
        [SerializeField] private RoomPassageController roomA;
        [SerializeField] private RoomPassageController roomB;
        [SerializeField] private GridFloorGuideController roomAGuide;
        [SerializeField] private GridFloorGuideController roomBGuide;
        [SerializeField] private DungeonFloorController proceduralFloorController; // 36일차 생성 던전 연결

        private RoomConnection connection;
        private bool initialized;

        private void Awake()
        {
            if (proceduralFloorController == null)
            {
                proceduralFloorController = FindFirstObjectByType<DungeonFloorController>();
            }
        }

        private void Start()
        {
            EnsureInitialized();
            SetCurrentRoomVisual(roomA != null ? roomA.RoomId : null);
        }

        public bool TryTransition(
            string currentRoomId,
            GridPosition currentPosition,
            CardinalDirection moveDirection,
            out RoomPassageController destinationRoom,
            out GridPosition destinationEntryPosition)
        {
            destinationRoom = null;
            destinationEntryPosition = GridPosition.Zero;

            // 36일차: 절차 생성 층이 활성화되어 있으면 GeneratedDungeon 그래프를 우선 사용한다.
            if (proceduralFloorController != null
                && proceduralFloorController.CurrentDungeon != null
                && proceduralFloorController.TryGetGeneratedDestination(
                    currentRoomId,
                    currentPosition,
                    moveDirection,
                    out RoomView generatedDestination,
                    out destinationEntryPosition))
            {
                destinationRoom = generatedDestination != null
                    ? generatedDestination.PassageController
                    : null;

                return destinationRoom != null;
            }

            // 생성 던전이 없으면 기존 2방 테스트 연결을 그대로 사용한다.
            EnsureInitialized();

            if (connection == null)
            {
                return false;
            }

            if (!connection.TryGetDestination(
                    currentRoomId,
                    currentPosition,
                    moveDirection,
                    out string destinationRoomId,
                    out destinationEntryPosition))
            {
                return false;
            }

            destinationRoom = GetRoomById(destinationRoomId);

            if (destinationRoom == null)
            {
                return false;
            }

            SetCurrentRoomVisual(destinationRoomId);
            return true;
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            if (roomA == null || roomB == null)
            {
                return;
            }

            GridPassage sharedDoor = roomA.BoundaryDoorPassage;

            if (sharedDoor == null)
            {
                return;
            }

            roomB.SetBoundaryDoorPassage(sharedDoor);

            RoomConnectionEnd endA = new RoomConnectionEnd(
                roomA.RoomId,
                roomA.BoundaryPosition,
                roomA.BoundaryDirection);

            RoomConnectionEnd endB = new RoomConnectionEnd(
                roomB.RoomId,
                roomB.BoundaryPosition,
                roomB.BoundaryDirection);

            connection = new RoomConnection(endA, endB);
            initialized = true;
        }

        private RoomPassageController GetRoomById(string roomId)
        {
            if (roomA != null && roomA.RoomId == roomId)
            {
                return roomA;
            }

            if (roomB != null && roomB.RoomId == roomId)
            {
                return roomB;
            }

            return null;
        }

        private void SetCurrentRoomVisual(string roomId)
        {
            if (roomAGuide != null)
            {
                roomAGuide.SetGuideVisible(roomA != null && roomA.RoomId == roomId);
            }

            if (roomBGuide != null)
            {
                roomBGuide.SetGuideVisible(roomB != null && roomB.RoomId == roomId);
            }
        }
    }
}
