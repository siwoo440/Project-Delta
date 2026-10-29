using System; // 기록 문자열 검사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class RunRestoreRules // 현재 구현 범위의 복원 검사
    { // 클래스 시작
        public const int TrialEnemyMaxHealth = 70; // 현재 시험 상대의 최대 체력
        public static bool IsValid(RunSaveData data) // 재개 전 회차 상태 검사
        { // 검사 시작
            if (data == null || !data.IsValid() || data.floor > 2 || data.health == 0 || data.maxHealth != 100 || data.keys < 0 || data.facing < 0 || data.facing > 4 || data.encounterHealth < 0 || data.encounterHealth > TrialEnemyMaxHealth || data.encounterCompleted && data.encounterHealth != 0 || data.history == null || data.history.Length > 10000 || InventoryState.FromSavedSlots(data.slots) == null) // 범위와 소지품 검사
            { // 오류 조건 시작
                return false; // 잘못된 회차 거절
            } // 오류 조건 끝
            foreach (var entry in data.history) // 저장된 탐험 기록 순회
            { // 기록 검사 시작
                if (string.IsNullOrWhiteSpace(entry) || entry.Length > 1024) // 빈 기록과 길이 검사
                { // 기록 오류 시작
                    return false; // 잘못된 기록 거절
                } // 기록 오류 끝
            } // 기록 검사 끝
            var floorSeed = data.floor == 1 ? data.seed : unchecked(data.seed + 7919); // 현재 층의 재현 시드
            var layout = DungeonGenerator.Generate(floorSeed); // 저장된 층 구조 재생성
            if (DungeonDiscovery.FromSaved(layout, data.visitedRooms, data.roomX, data.roomY) == null || DoorLockState.FromSaved(layout, data.doorLock) == null) // 방문과 문 상태 검사
            { // 던전 오류 시작
                return false; // 잘못된 던전 상태 거절
            } // 던전 오류 끝
            if (data.hasPreviousRoom && (layout.GetConnection(data.previousRoomX, data.previousRoomY, data.roomX, data.roomY) == null || DungeonDiscovery.FromSaved(layout, data.visitedRooms, data.previousRoomX, data.previousRoomY) == null)) // 이전 방의 연결과 방문 기록 검사
            { // 연결 오류 시작
                return false; // 잘못된 이전 경로 거절
            } // 연결 오류 끝
            return true; // 논리 저장 상태 검증 통과
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
