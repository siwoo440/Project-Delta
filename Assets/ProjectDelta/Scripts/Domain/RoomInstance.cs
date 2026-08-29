using System; // 직렬화 기능 사용
using System.Collections.Generic; // 통로 목록 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    [Serializable] // RoomDefinition에서 목록으로 직렬화하기 위한 표시
    public struct PassageEntry // 방 정의가 갖는 통로 한 칸 데이터
    {
        public int X; // 통로 기준 칸 X 좌표
        public int Z; // 통로 기준 칸 Z 좌표
        public CardinalDirection Direction; // 통로 방향
        public PassageType Type; // 통로 종류 (Open/Wall/Door)
        public bool IsLocked; // 문일 때 잠김 여부
    }

    public sealed class RoomInstance // 층에 실제로 배치된 방 하나의 런타임 표현
    {
        public string RoomId { get; } // 이 방 인스턴스의 식별자
        public string DefinitionId { get; } // 원본 RoomDefinition의 Id
        public RoomGridLayout Layout { get; } // 이 방의 통로 상태
        public bool Visited { get; private set; } // 최초 방문 여부 (기획서 3.3.3절 "처음 들어온 순간 한 번만 처리")
        public bool Completed { get; private set; } // 콘텐츠 처리 상태 (기획서 3.3.3절 "이벤트가 완료되면 해당 방은 일반적으로 빈 방으로 전환한다")
        public bool ChestOpened { get; private set; } // 25일차 상자 개봉 여부 (26일차: 저장 대상에 포함)

        // 110일차: 방 종류. 생성 시 한 번 배정되고 이후에는 바뀌지 않는다.
        public RoomType RoomType { get; private set; } = RoomType.Normal;

        // 110일차: 함정 방이 이미 판정을 마쳤는지. 성공/실패(회피)와 무관하게 한 번만 처리한다.
        public bool TrapTriggered { get; private set; }

        // 111일차: 이벤트 방이 이미 이벤트 화면을 띄운 적 있는지. 재진입 시 다시 뜨지 않게 막는다.
        public bool EventTriggered { get; private set; }

        // 95일차: 상자 개봉 여부와 실제 남은 내용물을 분리해 관리한다.
        private readonly List<string> chestRemainingItems =
            new List<string>();

        public bool HasChestContentsSnapshot { get; private set; }

        public IReadOnlyList<string> ChestRemainingItems =>
            chestRemainingItems;

        private RoomInstance(string roomId, string definitionId, RoomGridLayout layout) // 방 인스턴스 생성자
        {
            RoomId = roomId; // 방 식별자 저장
            DefinitionId = definitionId; // 정의 식별자 저장
            Layout = layout; // 통로 데이터 저장
        }

        // 처음 호출될 때만 true를 반환한다. 두 번째 호출부터는 이미 방문한 상태이므로 false.
        public bool MarkVisited() // 최초 방문 처리 및 여부 반환
        {
            if (Visited) // 이미 방문한 방인지 확인
            {
                return false; // 최초 방문이 아님을 반환
            }

            Visited = true; // 방문 상태로 전환
            return true; // 최초 방문임을 반환
        }

        // TODO: 실제 이벤트/조우/함정 판정 시스템이 생기면 그 결과가 끝났을 때 호출한다.
        // 지금은 아직 그 시스템이 없어서 아무도 호출하지 않는다.
        public void MarkCompleted() // 방 콘텐츠 처리 완료로 전환
        {
            Completed = true; // 콘텐츠 처리 완료 상태로 전환
        }

        // 25일차: 상자를 열었을 때 호출한다. 저장 데이터(RoomRunState.ChestOpened)로 이어진다.
        public void MarkChestOpened()
        {
            ChestOpened = true; // 상자 개봉 상태로 전환
        }

        // 110일차: 방이 처음 만들어질 때 한 번 호출해 종류를 확정한다.
        // 저장된 방을 복원할 때는 ApplySavedState가 저장된 값을 그대로 덮어쓴다.
        public void SetRoomType(
            RoomType roomType)
        {
            RoomType = roomType;
        }

        // 110일차: 함정 판정을 한 번만 허용한다. 이미 처리된 방이면 false를 반환한다.
        // RoomTrapService를 통해서만 호출되어야 하므로 internal로 막아둔다.
        internal bool MarkTrapTriggered()
        {
            if (TrapTriggered)
            {
                return false;
            }

            TrapTriggered = true;
            return true;
        }

        // 111일차: 이벤트 표시를 한 번만 허용한다. 이미 표시된 방이면 false를 반환한다.
        // RoomEventTriggerController를 통해서만 호출되어야 하므로 internal로 막아둔다.
        internal bool MarkEventTriggered()
        {
            if (EventTriggered)
            {
                return false;
            }

            EventTriggered = true;
            return true;
        }

        // 95일차: 새 방에서 상자 원본 목록을 최초 한 번만 런타임 상태로 등록한다.
        public void InitializeChestContents(
            IEnumerable<string> itemKeys)
        {
            if (HasChestContentsSnapshot)
            {
                return;
            }

            ReplaceChestContents(
                itemKeys);
        }

        // 95일차: 저장 데이터의 남은 상자 목록을 그대로 복원한다.
        public void RestoreChestContents(
            IEnumerable<string> itemKeys)
        {
            ReplaceChestContents(
                itemKeys);
        }

        // 95일차: 실제 획득 성공 시에만 상자 런타임 상태에서 제거한다.
        public bool TryTakeChestItem(
            int index,
            out string itemKey)
        {
            if (!HasChestContentsSnapshot
                || index < 0
                || index >= chestRemainingItems.Count)
            {
                itemKey =
                    null;

                return false;
            }

            itemKey =
                chestRemainingItems[index];

            chestRemainingItems.RemoveAt(
                index);

            return true;
        }

        private void ReplaceChestContents(
            IEnumerable<string> itemKeys)
        {
            chestRemainingItems.Clear();

            if (itemKeys != null)
            {
                foreach (string itemKey in itemKeys)
                {
                    if (!string.IsNullOrEmpty(
                            itemKey))
                    {
                        chestRemainingItems.Add(
                            itemKey);
                    }
                }
            }

            HasChestContentsSnapshot =
                true;
        }

        // 26일차: 저장 데이터를 불러올 때 "최초 1회" 판정 없이 상태를 그대로 덮어씌운다.
        // MarkVisited()/MarkCompleted()와 달리 몇 번을 호출해도 부작용이 없다.
        public void ApplySavedState(bool visited, bool completed, bool chestOpened)
        {
            ApplySavedState(
                visited,
                completed,
                chestOpened,
                RoomType,
                TrapTriggered,
                EventTriggered);
        }

        // 110일차: 방 종류·함정 판정 여부까지 함께 복원하는 오버로드.
        public void ApplySavedState(
            bool visited,
            bool completed,
            bool chestOpened,
            RoomType roomType,
            bool trapTriggered)
        {
            ApplySavedState(
                visited,
                completed,
                chestOpened,
                roomType,
                trapTriggered,
                EventTriggered);
        }

        // 111일차: 이벤트 표시 여부까지 함께 복원하는 오버로드.
        public void ApplySavedState(
            bool visited,
            bool completed,
            bool chestOpened,
            RoomType roomType,
            bool trapTriggered,
            bool eventTriggered)
        {
            Visited = visited; // 저장된 방문 상태 복원
            Completed = completed; // 저장된 완료 상태 복원
            ChestOpened = chestOpened; // 저장된 상자 개봉 상태 복원
            RoomType = roomType; // 저장된 방 종류 복원
            TrapTriggered = trapTriggered; // 저장된 함정 판정 여부 복원
            EventTriggered = eventTriggered; // 저장된 이벤트 표시 여부 복원
        }

        // RoomDefinition의 정적 통로 목록으로부터 실제 방 인스턴스를 만든다.
        // Domain은 Data(RoomDefinition)를 직접 참조하지 않고, 순수 데이터 목록만 받는다.
        public static RoomInstance Create(string roomId, string definitionId, IEnumerable<PassageEntry> passages) // 정의 데이터로 방 인스턴스 생성
        {
            RoomGridLayout layout = new RoomGridLayout(); // 새 통로 데이터 생성

            if (passages != null) // 통로 목록 존재 확인
            {
                foreach (PassageEntry entry in passages) // 정의된 통로 항목 반복
                {
                    GridPassage passage = CreatePassage(entry); // 통로 항목을 실제 통로 객체로 변환
                    layout.SetPassage(new GridPosition(entry.X, entry.Z), entry.Direction, passage); // 통로 데이터 등록
                }
            }

            return new RoomInstance(roomId, definitionId, layout); // 완성된 방 인스턴스 반환
        }

        private static GridPassage CreatePassage(PassageEntry entry) // 통로 항목을 GridPassage로 변환
        {
            switch (entry.Type) // 통로 종류 분기
            {
                case PassageType.Wall: // 벽 처리
                    return GridPassage.CreateWall(); // 벽 통로 반환
                case PassageType.Door: // 문 처리
                    return GridPassage.CreateDoor(entry.IsLocked); // 문 통로 반환 (잠김 여부 포함)
                default: // 일반 통로 처리
                    return GridPassage.CreateOpen(); // 일반 통로 반환
            }
        }
    }
}
