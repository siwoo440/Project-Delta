using System; // 저장 형식과 방 식별자
using System.Collections.Generic; // 층과 상자 중복 검사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    [Serializable] // 상자 좌표와 상태의 저장 형식
    public sealed class SavedRoomChest // 특정 방의 상자 저장 데이터
    { // 저장 클래스 시작
        public int x; // 상자 방 가로 좌표
        public int y; // 상자 방 세로 좌표
        public ChestSaveData chest; // 고정 후보를 포함한 상자 상태
    } // 저장 클래스 끝
    [Serializable] // 한 층의 콘텐츠 저장 형식
    public sealed class SavedFloorRoomContent // 한 층의 상자와 함정 상태
    { // 저장 클래스 시작
        public int floor; // 실제 던전 층
        public int placementVersion; // 결정적 배치 규칙 버전
        public SavedRoomChest[] chests; // 위치별 상자 상태
        public int trapX; // 함정 방 가로 좌표
        public int trapY; // 함정 방 세로 좌표
        public bool trapCompleted; // 첫 진입 함정 완료 여부
    } // 저장 클래스 끝
    public sealed class FloorRoomContent // 검증된 층별 콘텐츠의 불변 상태
    { // 상태 클래스 시작
        public const int CurrentPlacementVersion = 1; // 현재 시험 배치 규칙 버전
        private readonly SavedFloorRoomContent data; // 외부 변경을 차단한 독립 저장 데이터
        private FloorRoomContent(SavedFloorRoomContent saved) // 검증된 층별 상태 생성
        { // 생성 시작
            data = Copy(saved); // 상자 후보를 포함한 독립 상태 복사
        } // 생성 끝
        public int Floor => data.floor; // 실제 던전 층
        public DungeonRoom TrapRoom => new DungeonRoom(data.trapX, data.trapY); // 독립 함정 방 좌표
        public SavedFloorRoomContent Snapshot() // 현재 층의 독립 저장본
        { // 저장 시작
            return Copy(data); // 방별 상자와 후보의 심층 복사
        } // 저장 끝
        public static bool TryCreate(int floor, DungeonLayout layout, int seed, DungeonRoom[] excluded, out FloorRoomContent state) // 현재 층의 결정적 시험 콘텐츠 생성
        { // 생성 시작
            state = null; // 기본 실패 결과
            if (floor < 1 || !TrialRoomContentPlacement.TryCreate(layout, seed, excluded, out var placement)) // 유효한 층과 충분한 일반 방 검사
            { // 생성 오류 시작
                return false; // 누락되거나 부족한 콘텐츠 방 차단
            } // 생성 오류 끝
            var rooms = placement.ChestRooms; // 확정된 상자 방 목록
            var chests = new SavedRoomChest[rooms.Length]; // 독립 상자 저장 목록
            for (var index = 0; index < rooms.Length; index++) // 확정된 상자 방 순회
            { // 상자 생성 시작
                var room = rooms[index]; // 실제 상자 방
                if (!ChestState.TryCreate(ChestId(floor, room.X, room.Y), TrialGrade(index), TrialLocked(index), out var chest)) // 미확정 분포의 등급과 잠금 시험 상태 생성
                { // 상자 오류 시작
                    return false; // 잘못된 상자 생성 차단
                } // 상자 오류 끝
                chests[index] = new SavedRoomChest { x = room.X, y = room.Y, chest = chest.Snapshot() }; // 방별 상자와 식별자 보관
            } // 상자 생성 끝
            state = new FloorRoomContent(new SavedFloorRoomContent { floor = floor, placementVersion = CurrentPlacementVersion, chests = chests, trapX = placement.TrapRoom.X, trapY = placement.TrapRoom.Y }); // 상자와 별도 시험 함정 구성
            return true; // 정상 생성 성공
        } // 생성 끝
        public static bool TryRestore(SavedFloorRoomContent saved, DungeonLayout layout, int seed, DungeonRoom[] excluded, out FloorRoomContent state) // 저장된 배치와 각 상자 상태 검증
        { // 복원 시작
            state = null; // 기본 실패 결과
            if (saved == null || saved.placementVersion != CurrentPlacementVersion || saved.floor < 1 || !TrialRoomContentPlacement.TryCreate(layout, seed, excluded, out var placement)) // 저장 버전과 현재 층 배치 검사
            { // 기본 오류 시작
                return false; // 알 수 없는 버전과 배치 실패 차단
            } // 기본 오류 끝
            var rooms = placement.ChestRooms; // 현재 씨드의 결정적 상자 방
            if (saved.chests == null || saved.chests.Length != rooms.Length || saved.trapX != placement.TrapRoom.X || saved.trapY != placement.TrapRoom.Y) // 상자 수와 함정 방 일치 검사
            { // 배치 오류 시작
                return false; // 누락되거나 변경된 배치 차단
            } // 배치 오류 끝
            var seen = new HashSet<string>(StringComparer.Ordinal); // 상자 식별자 중복 검사
            for (var index = 0; index < rooms.Length; index++) // 결정된 상자 방 순회
            { // 상자 검사 시작
                var room = rooms[index]; // 결정된 상자 방
                var entry = saved.chests[index]; // 저장된 동일 순서 상자
                if (entry == null || entry.x != room.X || entry.y != room.Y || entry.chest == null || entry.chest.chestId != ChestId(saved.floor, room.X, room.Y) || !seen.Add(entry.chest.chestId) || entry.chest.grade != (int)TrialGrade(index) || entry.chest.wasLocked != TrialLocked(index) || !ChestState.TryRestore(entry.chest, out _)) // 좌표와 시험 정의 및 상자 상태 검사
                { // 상자 오류 시작
                    return false; // 중복·손상·다른 배치의 상자 차단
                } // 상자 오류 끝
            } // 상자 검사 끝
            state = new FloorRoomContent(saved); // 검증된 독립 층별 상태
            return true; // 정상 저장 복원 성공
        } // 복원 끝
        public bool TryGetChest(int x, int y, out ChestState chest) // 실제 상자 방의 독립 상태 조회
        { // 조회 시작
            chest = null; // 기본 조회 실패
            foreach (var entry in data.chests) // 저장된 상자 방 순회
            { // 좌표 검사 시작
                if (entry.x == x && entry.y == y) // 현재 방 좌표 일치 검사
                { // 일치 시작
                    return ChestState.TryRestore(entry.chest, out chest); // 검증된 독립 상자 상태 전달
                } // 일치 끝
            } // 좌표 검사 끝
            return false; // 상자가 없는 방 차단
        } // 조회 끝
        public bool TrySetChest(int x, int y, ChestState chest, out FloorRoomContent state) // 기존 상자의 불변 상태 전이
        { // 변경 시작
            state = null; // 기본 변경 실패
            if (chest == null) // 전달된 상자 상태 검사
            { // 상자 오류 시작
                return false; // 누락된 상태 차단
            } // 상자 오류 끝
            var next = chest.Snapshot(); // 실제 상자의 독립 저장본
            for (var index = 0; index < data.chests.Length; index++) // 층별 상자 순회
            { // 대상 검사 시작
                var entry = data.chests[index]; // 기존 상자 좌표와 상태
                if (entry.x != x || entry.y != y) // 다른 방 검사
                { // 다른 방 시작
                    continue; // 다음 상자 검사
                } // 다른 방 끝
                if (next.chestId != entry.chest.chestId || next.grade != entry.chest.grade || next.wasLocked != entry.chest.wasLocked || !ChestState.TryRestore(next, out _) || !IsForward(entry.chest, next)) // 동일 상자와 완료 이력 및 고정 보상 검사
                { // 상자 불일치 시작
                    return false; // 다른 상자로 변경 차단
                } // 상자 불일치 끝
                var saved = Snapshot(); // 기존 층 상태의 독립 복사
                saved.chests[index].chest = next; // 확인한 상자만 변경
                state = new FloorRoomContent(saved); // 기존 상태를 보존한 변경 결과
                return true; // 상자 상태 변경 성공
            } // 대상 검사 끝
            return false; // 상자가 없는 방 차단
        } // 변경 끝
        public bool TryCompleteTrap(out FloorRoomContent state) // 실제 함정 방의 1회 완료 전이
        { // 완료 시작
            state = null; // 기본 변경 실패
            if (data.trapCompleted) // 이미 완료한 함정 검사
            { // 중복 오류 시작
                return false; // 중복 함정 완료 차단
            } // 중복 오류 끝
            var saved = Snapshot(); // 기존 층 상태의 독립 복사
            saved.trapCompleted = true; // 확인한 함정만 완료
            state = new FloorRoomContent(saved); // 기존 상태를 보존한 변경 결과
            return true; // 정상 함정 완료 성공
        } // 완료 끝
        public static bool HasUniqueFloors(SavedFloorRoomContent[] floors) // 회차 저장의 중복 층 검사
        { // 검사 시작
            if (floors == null) // 누락된 층 목록 검사
            { // 목록 오류 시작
                return false; // 누락된 저장 목록 차단
            } // 목록 오류 끝
            var seen = new HashSet<int>(); // 이미 저장한 층 번호
            foreach (var floor in floors) // 저장된 층 순회
            { // 층 번호 검사 시작
                if (floor == null || floor.floor < 1 || !seen.Add(floor.floor)) // 누락과 중복 층 검사
                { // 층 오류 시작
                    return false; // 잘못된 중복 층 차단
                } // 층 오류 끝
            } // 층 번호 검사 끝
            return true; // 유일한 층 목록 확인
        } // 검사 끝
        private static string ChestId(int floor, int x, int y) // 층과 방 좌표의 안정적인 상자 식별자
        { // 식별 시작
            return "chest-" + floor + "-" + x + "-" + y; // 같은 회차 안의 고유 좌표 조합
        } // 식별 끝
        private static ChestGrade TrialGrade(int index) // 분포 미확정의 순서별 상자 등급 시험값
        { // 등급 조회 시작
            return index == 0 ? ChestGrade.Common : index == 1 ? ChestGrade.Advanced : ChestGrade.Rare; // 일반과 고급 및 희귀의 시험 순서
        } // 등급 조회 끝
        private static bool TrialLocked(int index) // 잠금 확률 미확정의 상자 시험값
        { // 잠금 조회 시작
            return index == 1; // 두 번째 고급 상자만 잠금
        } // 잠금 조회 끝
        private static bool IsForward(ChestSaveData before, ChestSaveData after) // 이미 확정된 상자 이력과 후보 보존 검사
        { // 검사 시작
            if (before.unlocked && !after.unlocked || before.forceAttempted && !after.forceAttempted || before.completed && !after.completed || before.mimicDefeated && !after.mimicDefeated || before.mimicState != 0 && before.mimicState != after.mimicState) // 잠금과 판정 및 완료 상태 역행 검사
            { // 이력 오류 시작
                return false; // 이미 처리한 상자의 재설정 차단
            } // 이력 오류 끝
            if (before.mimicState == 0) // 아직 판정하지 않은 상자 검사
            { // 미판정 시작
                return true; // 최초 판정 또는 잠금 상태 변경 허용
            } // 미판정 끝
            if (before.transactionId != after.transactionId || before.baseRewardCount != after.baseRewardCount || before.rewards.Length != after.rewards.Length || before.experience != after.experience && before.mimicState != 2) // 확정한 거래와 일반 보상 불변성 검사
            { // 보상 오류 시작
                return false; // 확정된 거래와 보상 변경 차단
            } // 보상 오류 끝
            for (var index = 0; index < before.rewards.Length; index++) // 확정된 보상 후보 순회
            { // 후보 검사 시작
                var oldItem = before.rewards[index]; // 기존 보상 후보
                var newItem = after.rewards[index]; // 변경된 보상 후보
                if (oldItem.candidateId != newItem.candidateId || oldItem.itemId != newItem.itemId || oldItem.quantity != newItem.quantity || (oldItem.equipment?.instanceId) != (newItem.equipment?.instanceId)) // 후보 식별자와 수량 및 개별 장비 검사
                { // 후보 오류 시작
                    return false; // 확정된 후보 교체 차단
                } // 후보 오류 끝
            } // 후보 검사 끝
            return true; // 확정된 이력과 후보 보존
        } // 검사 끝
        private static SavedFloorRoomContent Copy(SavedFloorRoomContent saved) // 상자와 후보를 포함한 심층 복사
        { // 복사 시작
            var chests = new SavedRoomChest[saved.chests.Length]; // 독립 상자 목록
            for (var index = 0; index < chests.Length; index++) // 검증된 상자 순회
            { // 상자 복사 시작
                var source = saved.chests[index]; // 원본 상자와 좌표
                ChestState.TryRestore(source.chest, out var chest); // 검증된 상자의 독립 상태
                chests[index] = new SavedRoomChest { x = source.x, y = source.y, chest = chest.Snapshot() }; // 좌표와 후보 심층 복사
            } // 상자 복사 끝
            return new SavedFloorRoomContent { floor = saved.floor, placementVersion = saved.placementVersion, chests = chests, trapX = saved.trapX, trapY = saved.trapY, trapCompleted = saved.trapCompleted }; // 독립 층별 저장 데이터
        } // 복사 끝
    } // 상태 클래스 끝
} // 이름 공간 끝
