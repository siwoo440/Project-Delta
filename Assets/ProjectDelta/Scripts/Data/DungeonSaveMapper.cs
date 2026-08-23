using System.Collections.Generic; // 사전 기능 사용
using ProjectDelta.Domain; // 런타임 던전 상태 사용
using UnityEngine; // Vector2Int 사용

namespace ProjectDelta.Data // 데이터 네임스페이스
{
    // 26일차: RunContext(Domain, 런타임)와 RunData(Data, 저장 DTO) 사이를 오가는 변환기.
    // RunContext.cs의 오래된 주석("a later SaveService flattens it into RunData")이 예고했던
    // 그 "나중"이 오늘이다.
    //
    // 지금은 방-방 연결 그래프가 없어서(28일차 이후 던전 생성 예정) 이어하기 위치를
    // RoomId 하나로만 찾는 테스트용 방식이다. 실제 좌표/연결 데이터가 생기면
    // Coordinate/ConnectedDirections 기반 매핑으로 교체한다.
    public static class DungeonSaveMapper // 던전 진행 상태 저장·복원 변환
    {
        private static Dictionary<string, RoomRunState> pendingRoomStates; // 씬 로드 중 각 방이 소비할 복원 대상

        // 현재 런 상태를 저장용 DTO로 변환한다.
        public static RunData BuildFromRunContext(RunContext context) // 저장 데이터 생성
        {
            RunData data = new RunData(); // 새 저장 데이터 생성
            data.BasicInfo.RunId = context.Metadata.RunId; // 런 식별자 저장
            data.BasicInfo.StartedAtIso8601 = context.Metadata.StartedAtIso8601; // 런 시작 시각 저장
            data.BasicInfo.CurrentFloor = context.Dungeon.CurrentFloor; // 현재 층 번호 저장
            data.BasicInfo.CurrentRoomId = context.Player.CurrentRoomId; // 현재 방 식별자 저장 (테스트용)
            data.BasicInfo.CurrentGridPositionInRoom = new Vector2Int(context.Player.CurrentGridPosition.X, context.Player.CurrentGridPosition.Z); // 방 안 정확한 칸 저장

            foreach (RoomInstance room in context.Dungeon.AllRooms) // 등록된 모든 방 반복
            {
                data.DungeonState.Rooms.Add(new RoomRunState // 방별 저장 항목 추가
                {
                    RoomId = room.RoomId, // 방 식별자
                    Visited = room.Visited, // 방문 여부
                    Completed = room.Completed, // 완료 여부
                    ChestOpened = room.ChestOpened // 상자 개봉 여부
                    // Coordinate/ConnectedDirections/Discovered 등은 해당 시스템이 생기기 전까지 기본값 유지
                });
            }

            foreach (InventoryItemStack item in context.Inventory.Items) // 보유 아이템 전체 반복
            {
                data.Inventory.InventoryItemIds.Add(item.ItemId); // 26일차: 아이템 식별자만 저장 (정식 인벤토리 전 자리표시자 수준)
            }

            return data; // 완성된 저장 데이터 반환
        }

        // 저장 데이터의 기본 정보(층 번호, 현재 방·칸, 인벤토리)를 갓 시작한 RunContext에 되돌려준다.
        public static void ApplyBasics(RunContext context, RunData savedRun) // 기본 정보 복원
        {
            int savedFloor = savedRun.BasicInfo.CurrentFloor > 0 ? savedRun.BasicInfo.CurrentFloor : 1; // 저장된 층 번호 확인 (0 이하 방지)
            context.Dungeon.SetFloor(savedFloor); // 층 번호 복원
            context.Player.CurrentRoomId = savedRun.BasicInfo.CurrentRoomId; // 현재 방 식별자 복원

            Vector2Int savedGridPosition = savedRun.BasicInfo.CurrentGridPositionInRoom; // 저장된 방 안 칸 좌표 조회
            context.Player.CurrentGridPosition = new GridPosition(savedGridPosition.x, savedGridPosition.y); // 방 안 정확한 칸 복원

            foreach (string itemId in savedRun.Inventory.InventoryItemIds) // 저장된 아이템 식별자 전체 반복
            {
                context.Inventory.Add(new InventoryItemStack(itemId, itemId)); // 인벤토리에 복원 (자리표시자 수준: 식별자와 표시 이름이 같음)
            }
        }

        // DungeonScene이 로드되는 동안 각 방(RoomPassageController)이 자신의 저장 상태를 찾아갈 수 있도록 준비한다.
        public static void BeginRestore(RunData savedRun) // 복원 대상 목록 준비
        {
            pendingRoomStates = new Dictionary<string, RoomRunState>(); // 새 복원 사전 생성

            foreach (RoomRunState room in savedRun.DungeonState.Rooms) // 저장된 방 목록 반복
            {
                if (!string.IsNullOrEmpty(room.RoomId)) // 방 식별자 존재 확인
                {
                    pendingRoomStates[room.RoomId] = room; // 식별자 기준으로 등록
                }
            }
        }

        // 방 하나가 자신의 저장 상태를 조회한다. 여러 방이 반복해서 조회해도 안전하다(소비되지 않음).
        public static bool TryGetRoomState(string roomId, out RoomRunState state) // 방별 저장 상태 조회
        {
            if (pendingRoomStates != null && pendingRoomStates.TryGetValue(roomId, out state)) // 복원 사전에 등록되어 있는지 확인
            {
                return true; // 조회 성공 반환
            }

            state = null; // 결과 초기화
            return false; // 조회 실패 반환
        }

        // 새 게임을 시작하거나 런을 포기할 때 호출해서, 다음 씬 로드가 이전 복원 데이터를 잘못 주워가지 않게 한다.
        public static void ClearPendingRestore() // 복원 대상 목록 비우기
        {
            pendingRoomStates = null; // 복원 사전 해제
        }
    }
}
