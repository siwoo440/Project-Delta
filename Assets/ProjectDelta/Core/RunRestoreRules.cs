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
            if (data.version >= 5 && data.maxMana != 50) // 현재 시험 회차의 최대 마나 검사
            { // 마나 범위 오류 시작
                return false; // 아직 지원하지 않는 최대 마나 거절
            } // 마나 범위 오류 끝
            var generationVersion = data.version < 3 ? 1 : data.generationVersion; // 기존 저장과 새 생성 버전 구분
            if (generationVersion < 1 || generationVersion > 3 || (data.version < 4 && generationVersion > 2) || (data.version >= 3 && data.secretPassages == null)) // 생성 버전과 비밀 상태 검사
            { // 생성 오류 시작
                return false; // 알 수 없는 생성 규칙 거절
            } // 생성 오류 끝
            if (data.version >= 4 && (data.tutorialHealth < 0 || data.tutorialHealth > TrialEnemyMaxHealth || (data.tutorialCompleted && data.tutorialHealth != 0) || (generationVersion != 3 && data.tutorialHealth != 0))) // 첫 조우 완료와 시험 체력 검사
            { // 조우 오류 시작
                return false; // 잘못된 첫 조우 상태 거절
            } // 조우 오류 끝
            var floorSeed = data.floor == 1 ? data.seed : unchecked(data.seed + 7919); // 현재 층의 재현 시드
            var layout = DungeonGenerator.GenerateVersioned(floorSeed, generationVersion); // 저장된 생성 규칙의 층 구조
            if (!DungeonGenerator.IsValidVersioned(layout, generationVersion)) // 재생성한 구조의 필수 경로 검사
            { // 구조 오류 시작
                return false; // 잘못된 던전 복원 차단
            } // 구조 오류 끝
            if (data.version >= 2 && data.layoutFingerprint != DungeonLayoutFingerprint.Compute(layout)) // 저장된 구조와 실제 지문 비교
            { // 지문 오류 시작
                return false; // 변경되거나 누락된 구조 거절
            } // 지문 오류 끝
            if (DungeonDiscovery.FromSaved(layout, data.visitedRooms, data.roomX, data.roomY, data.secretPassages) == null || DoorLockState.FromSaved(layout, data.doorLock) == null) // 방문과 문 상태 검사
            { // 던전 오류 시작
                return false; // 잘못된 던전 상태 거절
            } // 던전 오류 끝
            if (data.hasPreviousRoom && (layout.GetConnection(data.previousRoomX, data.previousRoomY, data.roomX, data.roomY) == null || DungeonDiscovery.FromSaved(layout, data.visitedRooms, data.previousRoomX, data.previousRoomY, data.secretPassages) == null)) // 이전 방의 연결과 방문 기록 검사
            { // 연결 오류 시작
                return false; // 잘못된 이전 경로 거절
            } // 연결 오류 끝
            return true; // 논리 저장 상태 검증 통과
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
