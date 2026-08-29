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

        // 112일차: 절차 생성으로 런타임에 만들어지는 상자용 - 인스펙터 목록 대신 코드로 내용물을 지정한다.
        public void Configure(
            IEnumerable<string> items)
        {
            itemDisplayNames.Clear();

            if (items != null)
            {
                itemDisplayNames.AddRange(
                    items);
            }
        }

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
            }
            // 새 저장 형식은 실제 남은 목록을 그대로 복원한다.
            else if (DungeonSaveMapper.TryGetRoomState(
                    roomInstance.RoomId,
                    out RoomRunState savedState)
                && savedState != null
                && savedState.HasChestContentsSnapshot)
            {
                roomInstance.RestoreChestContents(
                    savedState.ChestRemainingItems);
            }
            // 95일차 이전 저장은 남은 목록을 알 수 없다.
            // 기존 규칙과 호환되도록 이미 열린 상자는 빈 상자로 복원한다.
            else if (roomInstance.ChestOpened)
            {
                roomInstance.RestoreChestContents(
                    System.Array.Empty<string>());
            }
            else
            {
                // 새 게임/미개봉 상자는 Inspector의 원본 목록을 런타임 상태로 등록한다.
                roomInstance.InitializeChestContents(
                    itemDisplayNames);
            }

            initialized =
                true;

            HideIfEmpty();
        }

        public bool TryTake(
            int index,
            out string displayName)
        {
            EnsureInitialized();

            bool took;

            if (roomInstance != null)
            {
                took =
                    roomInstance.TryTakeChestItem(
                        index,
                        out displayName);
            }
            else if (index < 0
                || index >= fallbackRemainingItems.Count)
            {
                displayName =
                    null;

                took =
                    false;
            }
            else
            {
                displayName =
                    fallbackRemainingItems[index];

                fallbackRemainingItems.RemoveAt(
                    index);

                took =
                    true;
            }

            if (took)
            {
                HideIfEmpty();
            }

            return took;
        }

        // 112일차: 내용물을 전부 가져간 상자는 더 이상 상호작용 대상이 아니므로 감춘다.
        // 이미 비어있는 상태로 저장을 불러온 경우에도 동일하게 적용된다.
        private void HideIfEmpty()
        {
            if (RemainingItems.Count == 0)
            {
                gameObject.SetActive(
                    false);
            }
        }
    }
}
