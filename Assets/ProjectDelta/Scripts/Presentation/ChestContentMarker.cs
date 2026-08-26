using System.Collections.Generic;
using ProjectDelta.Data;
using ProjectDelta.Domain;
using UnityEngine;

namespace ProjectDelta.Presentation
{
    // 25일차: 상자 하나가 실제로 담고 있는 아이템 목록과 개봉 상태를 보유한다.
    // 95일차: 실제 남은 목록은 RoomInstance에도 동기화하여 저장·복원한다.
    public sealed class ChestContentMarker : MonoBehaviour
    {
        [SerializeField]
        private List<string> itemDisplayNames =
            new List<string>();

        // RoomInstance가 없는 테스트/예외 상황에서만 사용하는 호환 목록.
        private readonly List<string> fallbackRemainingItems =
            new List<string>();

        private bool initialized;
        private RoomInstance roomInstance;

        public IReadOnlyList<string> RemainingItems
        {
            get
            {
                EnsureInitialized();

                return roomInstance != null
                    ? roomInstance.ChestRemainingItems
                    : fallbackRemainingItems;
            }
        }

        // RoomPassageController.Awake()가 저장 상태를 먼저 복원한 뒤 Start()에서 상자 목록을 연결한다.
        private void Start()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            RoomPassageController passageController =
                GetComponentInParent<RoomPassageController>();

            roomInstance =
                passageController != null
                    ? passageController.CurrentInstance
                    : null;

            if (roomInstance == null)
            {
                fallbackRemainingItems.Clear();
                fallbackRemainingItems.AddRange(
                    itemDisplayNames);

                initialized =
                    true;

                return;
            }

            // 새 저장 형식은 실제 남은 목록을 그대로 복원한다.
            if (DungeonSaveMapper.TryGetRoomState(
                    roomInstance.RoomId,
                    out RoomRunState savedState)
                && savedState != null
                && savedState.HasChestContentsSnapshot)
            {
                roomInstance.RestoreChestContents(
                    savedState.ChestRemainingItems);

                initialized =
                    true;

                return;
            }

            // 95일차 이전 저장은 남은 목록을 알 수 없다.
            // 기존 규칙과 호환되도록 이미 열린 상자는 빈 상자로 복원한다.
            if (roomInstance.ChestOpened)
            {
                roomInstance.RestoreChestContents(
                    System.Array.Empty<string>());

                initialized =
                    true;

                return;
            }

            // 새 게임/미개봉 상자는 Inspector의 원본 목록을 런타임 상태로 등록한다.
            roomInstance.InitializeChestContents(
                itemDisplayNames);

            initialized =
                true;
        }

        public bool TryTake(
            int index,
            out string displayName)
        {
            EnsureInitialized();

            if (roomInstance != null)
            {
                return roomInstance.TryTakeChestItem(
                    index,
                    out displayName);
            }

            if (index < 0
                || index >= fallbackRemainingItems.Count)
            {
                displayName =
                    null;

                return false;
            }

            displayName =
                fallbackRemainingItems[index];

            fallbackRemainingItems.RemoveAt(
                index);

            return true;
        }
    }
}
