using System; // 기록 문자열 검사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class RunRestoreRules // 현재 구현 범위의 복원 검사
    { // 클래스 시작
        public const int TrialEnemyMaxHealth = 70; // 현재 시험 상대의 최대 체력
        public static bool IsValid(RunSaveData data) // 재개 전 회차 상태 검사
        { // 검사 시작
            if (data == null || !data.IsValid() || data.floor > 5 || data.health == 0 || data.maxHealth != 100 || data.keys < 0 || data.facing < 0 || data.facing > 4 || data.encounterHealth < 0 || data.encounterHealth > TrialEnemyMaxHealth || data.encounterCompleted && data.encounterHealth != 0 || data.history == null || data.history.Length > 10000 || InventoryState.FromSavedSlots(data.slots) == null) // 범위와 소지품 검사
            { // 오류 조건 시작
                return false; // 잘못된 회차 거절
            } // 오류 조건 끝
            if (data.version >= 7 && !Guid.TryParseExact(data.runId, "N", out _)) // 새 저장의 회차 식별자 검사
            { // 식별자 오류 시작
                return false; // 잘못된 회차 식별자 차단
            } // 식별자 오류 끝
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
            if (data.version >= 6) // 다수 조우 저장 형식 검사
            { // 다수 조우 검사 시작
                if (data.version >= 9 && data.groupGoblin && !data.groupEncounterEnabled) // 비활성 다수 조우의 종족 검사
                { // 종족 오류 시작
                    return false; // 존재하지 않는 고블린 조우 차단
                } // 종족 오류 끝
                var hasGroupHealth = data.groupEnemyHealth != null && data.groupEnemyHealth.Length > 0; // 실제 적 체력 기록 여부
                if ((!data.groupEncounterEnabled && (data.groupEncounterCompleted || hasGroupHealth)) || (data.groupEncounterCompleted && hasGroupHealth)) // 활성과 완료 상태 충돌 검사
                { // 다수 조우 충돌 시작
                    return false; // 잘못된 조우 상태 차단
                } // 다수 조우 충돌 끝
                if (hasGroupHealth) // 후퇴한 다수 적 체력 검사
                { // 체력 목록 검사 시작
                    if (data.floor != 1 || data.groupEnemyHealth.Length != 2) // 시험 층과 적 수 검사
                    { // 체력 목록 오류 시작
                        return false; // 잘못된 적 수와 층 차단
                    } // 체력 목록 오류 끝
                    var alive = false; // 생존한 적 확인 상태
                    foreach (var health in data.groupEnemyHealth) // 개별 적 체력 순회
                    { // 체력 검사 시작
                        if (health < 0 || health > TrialEnemyMaxHealth) // 체력 범위 검사
                        { // 범위 오류 시작
                            return false; // 잘못된 체력 차단
                        } // 범위 오류 끝
                        alive |= health > 0; // 생존 적 상태 누적
                    } // 체력 검사 끝
                    if (!alive) // 미완료 조우의 전멸 검사
                    { // 전멸 오류 시작
                        return false; // 완료되지 않은 전멸 상태 차단
                    } // 전멸 오류 끝
                } // 체력 목록 검사 끝
            } // 다수 조우 검사 끝
            var generationVersion = data.version < 3 ? 1 : data.generationVersion; // 기존 저장과 새 생성 버전 구분
            if (generationVersion < 1 || generationVersion > 3 || (data.version < 4 && generationVersion > 2) || (data.version >= 3 && data.secretPassages == null)) // 생성 버전과 비밀 상태 검사
            { // 생성 오류 시작
                return false; // 알 수 없는 생성 규칙 거절
            } // 생성 오류 끝
            if (data.version >= 6 && data.groupEncounterEnabled && generationVersion != 3) // 시험 다수 조우의 생성 규칙 검사
            { // 생성 규칙 충돌 시작
                return false; // 시험 방이 없는 구조 차단
            } // 생성 규칙 충돌 끝
            if (data.version >= 4 && (data.tutorialHealth < 0 || data.tutorialHealth > TrialEnemyMaxHealth || (data.tutorialCompleted && data.tutorialHealth != 0) || (generationVersion != 3 && data.tutorialHealth != 0))) // 첫 조우 완료와 시험 체력 검사
            { // 조우 오류 시작
                return false; // 잘못된 첫 조우 상태 거절
            } // 조우 오류 끝
            var layout = FloorLayoutRules.Generate(data.seed, data.floor, generationVersion); // 저장된 생성 규칙의 층 구조
            if (!FloorLayoutRules.IsValid(layout, data.seed, data.floor, generationVersion)) // 재생성한 구조의 필수 경로 검사
            { // 구조 오류 시작
                return false; // 잘못된 던전 복원 차단
            } // 구조 오류 끝
            if (data.floor == 5 && (data.encounterCompleted || data.encounterHealth != 0 || data.hasPendingReward && layout.GetRoom(data.roomX, data.roomY)?.Kind == RoomKind.Boss)) // 미지원 최종 보스와 이전 조우 상태 검사
            { // 보스 조우 오류 시작
                return false; // 존재하지 않는 상위 조우와 보스 보상 차단
            } // 보스 조우 오류 끝
            if (data.version >= 2 && data.layoutFingerprint != DungeonLayoutFingerprint.Compute(layout)) // 저장된 구조와 실제 지문 비교
            { // 지문 오류 시작
                return false; // 변경되거나 누락된 구조 거절
            } // 지문 오류 끝
            if (data.version >= 14 && data.roomContentEnabled) // 저장된 방 콘텐츠 배치 검사
            { // 콘텐츠 복원 검사 시작
                var currentFloorFound = false; // 현재 층 저장 존재 여부
                foreach (var savedFloor in data.roomContents) // 모든 방문 층 콘텐츠 순회
                { // 층별 배치 검사 시작
                    if (savedFloor.floor > data.floor || savedFloor.floor > 5) // 진행하지 않은 층 검사
                    { // 층 오류 시작
                        return false; // 미래 층의 잘못된 콘텐츠 차단
                    } // 층 오류 끝
                    var contentSeed = FloorSeedRules.For(data.seed, savedFloor.floor); // 층별 재현 씨드
                    var contentLayout = FloorLayoutRules.Generate(data.seed, savedFloor.floor, generationVersion); // 저장된 층의 던전 구조
                    var excluded = TrialRoomContentPlacement.Excluded(contentLayout, savedFloor.floor, generationVersion, data.groupEncounterEnabled); // 기존 조우 방의 안정적 제외
                    if (excluded == null || !FloorRoomContent.TryRestore(savedFloor, contentLayout, contentSeed, excluded, out _)) // 기존 조우와 결정적 배치 검사
                    { // 배치 오류 시작
                        return false; // 손상되거나 변경된 콘텐츠 차단
                    } // 배치 오류 끝
                    currentFloorFound |= savedFloor.floor == data.floor; // 현재 층의 콘텐츠 확인
                } // 층별 배치 검사 끝
                if (!currentFloorFound) // 현재 층 상태 누락 검사
                { // 누락 오류 시작
                    return false; // 누락된 현재 층의 복원 차단
                } // 누락 오류 끝
            } // 콘텐츠 복원 검사 끝
            if (data.version >= 15 && data.trialEventsEnabled) // 이벤트 위치와 회차 검증
            { // 이벤트 검사 시작
                var currentEventFound = false; // 현재 층의 이벤트 존재 여부
                foreach (var record in data.trialEvents) // 층별 이벤트 순회
                { // 배치 검증 시작
                    SavedFloorRoomContent content = null; // 이벤트 층의 방 콘텐츠
                    foreach (var candidate in data.roomContents) // 저장된 콘텐츠 순회
                    { // 층 비교 시작
                        if (candidate.floor == record.floor) // 같은 층 검사
                        { // 일치 시작
                            content = candidate; // 현재 이벤트 층의 콘텐츠
                            break; // 추가 검색 종료
                        } // 일치 끝
                    } // 층 비교 끝
                    var eventSeed = FloorSeedRules.For(data.seed, record.floor); // 층별 재현 씨드
                    var eventLayout = FloorLayoutRules.Generate(data.seed, record.floor, generationVersion); // 실제 층의 결정적 구조
                    var excluded = TrialRoomContentPlacement.Excluded(eventLayout, record.floor, generationVersion, data.groupEncounterEnabled); // 기존 조우 방 제외
                    if (content == null || excluded == null || !TrialEventPlacement.TrySelect(eventLayout, eventSeed, content, excluded, out var eventRoom) || eventRoom.X != record.x || eventRoom.Y != record.y || record.eventData.choices[0] != (data.seed & 1)) // 좌표와 회차 선택 순서 검사
                    { // 오류 시작
                        return false; // 변경된 방 또는 선택 순서 차단
                    } // 오류 끝
                    if (record.eventData.phase == 1 && (record.floor != data.floor || record.x != data.roomX || record.y != data.roomY)) // 확인 전 결과의 실제 위치 검사
                    { // 위치 오류 시작
                        return false; // 확인 전 다른 방으로 이동한 저장 차단
                    } // 위치 오류 끝
                    currentEventFound |= record.floor == data.floor; // 현재 층 이벤트 확인
                } // 배치 검증 끝
                if (!currentEventFound) // 현재 층 이벤트 누락 검사
                { // 오류 시작
                    return false; // 복원 불가능한 기록 차단
                } // 오류 끝
            } // 이벤트 검사 끝
            if (data.version >= 17 && data.hasTrialEventBattle && !ValidTrialEventBattle(data, layout, generationVersion)) // 이벤트 전투와 실제 방 및 참가자 검사
            { // 전투 오류 시작
                return false; // 다른 방과 잘못된 참가자의 복원 차단
            } // 전투 오류 끝
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
        private static bool ValidTrialEventBattle(RunSaveData data, DungeonLayout layout, int generationVersion) // 이벤트 전투와 실제 조우 대조
        { // 검사 시작
            if (data.hasPendingReward || !TrialEventBattleState.TryRestore(data.trialEventBattle, out var battle)) // 기존 보상과 전투 재생 검사
            { // 오류 시작
                return false; // 다른 보상과 손상된 전투 차단
            } // 오류 끝
            var saved = battle.Snapshot(); // 검증된 독립 전투 상태
            if (saved.initialStamina != 100 || saved.playerCharm != 50) // 현재 구현의 시작 자원과 매력 검사
            { // 시작 오류 시작
                return false; // 미지원 능력치의 전투 차단
            } // 시작 오류 끝
            if (data.roomContentEnabled) // 현재 층의 상자 상태 검사
            { // 상자 검사 시작
                foreach (var floor in data.roomContents) // 저장된 층별 콘텐츠 순회
                { // 층 검사 시작
                    if (floor.floor != data.floor) // 다른 층 검사
                    { // 다른 층 시작
                        continue; // 현재 층만 확인
                    } // 다른 층 끝
                    foreach (var chest in floor.chests) // 실제 상자 방 순회
                    { // 상자 검사 시작
                        if (chest.x == data.roomX && chest.y == data.roomY && chest.chest.mimicState == 2 && !chest.chest.mimicDefeated) // 현재 미믹 조우 검사
                        { // 미믹 오류 시작
                            return false; // 미지원 미믹 이벤트 전투 차단
                        } // 미믹 오류 끝
                    } // 상자 검사 끝
                } // 층 검사 끝
            } // 상자 검사 끝
            var room = layout.GetRoom(data.roomX, data.roomY); // 저장된 실제 방
            if (room == null) // 실제 방 존재 검사
            { // 방 오류 시작
                return false; // 존재하지 않는 방 차단
            } // 방 오류 끝
            var excluded = TrialRoomContentPlacement.Excluded(layout, data.floor, generationVersion, data.groupEncounterEnabled); // 실제 첫 조우와 다수 방 재현
            if (excluded == null) // 조우 배치 실패 검사
            { // 배치 오류 시작
                return false; // 잘못된 조우 구조 차단
            } // 배치 오류 끝
            var first = excluded.Length > 0 ? excluded[0] : null; // 실제 첫 슬라임 방
            var group = excluded.Length > 1 ? excluded[1] : null; // 실제 다수 조우 방
            var isFirst = first != null && first.X == data.roomX && first.Y == data.roomY && !data.tutorialCompleted; // 미완료 첫 조우
            var isGroup = group != null && group.X == data.roomX && group.Y == data.roomY && data.groupEncounterEnabled && !data.groupEncounterCompleted; // 미완료 다수 조우
            var isElite = room.Kind == RoomKind.Elite && !data.encounterCompleted; // 미완료 상위 조우
            if (!isFirst && !isGroup && !isElite) // 실제 조우 유형 검사
            { // 조우 오류 시작
                return false; // 완료 조우와 일반 방 차단
            } // 조우 오류 끝
            var species = isGroup ? data.groupGoblin ? "MON_GOBLIN" : "MON_SLIME" : isFirst ? "MON_SLIME" : "trial-elite"; // 실제 조우 대상 종족
            var maximumSlots = isGroup ? 2 : 1; // 실제 일반 전투의 적 수
            var available = 0; // 일반 전투 시작 시 생존 적 수
            for (var slot = 0; slot < maximumSlots; slot++) // 실제 일반 전투의 적 순회
            { // 생존 검사 시작
                var oldHealth = isGroup && data.groupEnemyHealth != null && data.groupEnemyHealth.Length == 2 ? data.groupEnemyHealth[slot] : isFirst && data.tutorialHealth > 0 ? data.tutorialHealth : isElite && data.encounterHealth > 0 ? data.encounterHealth : TrialEnemyMaxHealth; // 이전 후퇴 또는 최초 체력
                if (oldHealth > 0) // 이전부터 생존한 적 검사
                { // 생존 시작
                    available++; // 기존 전투의 보상 대상
                } // 생존 끝
            } // 생존 검사 끝
            foreach (var enemy in saved.initialEnemies) // 이벤트에 참가한 생존 적 순회
            { // 참가자 검사 시작
                if (enemy.slot < 0 || enemy.slot >= maximumSlots || enemy.id != species + ":" + enemy.slot || enemy.charm != 50 || enemy.health < 1 || enemy.health > TrialEnemyMaxHealth) // 원본 슬롯과 종족 및 시험 매력 검사
                { // 참가자 오류 시작
                    return false; // 변경된 참가자 차단
                } // 참가자 오류 끝
                var oldHealth = isGroup && data.groupEnemyHealth != null && data.groupEnemyHealth.Length == 2 ? data.groupEnemyHealth[enemy.slot] : isFirst && data.tutorialHealth > 0 ? data.tutorialHealth : isElite && data.encounterHealth > 0 ? data.encounterHealth : TrialEnemyMaxHealth; // 같은 슬롯의 기존 체력
                if (oldHealth == 0 || enemy.health > oldHealth) // 이전에 처치된 적과 체력 증가 검사
                { // 체력 오류 시작
                    return false; // 존재하지 않는 생존 적 차단
                } // 체력 오류 끝
            } // 참가자 검사 끝
            if (available < saved.initialEnemies.Length) // 실제 전투의 생존 참가자 수 검사
            { // 수량 오류 시작
                return false; // 원래 전투보다 많은 참가자 차단
            } // 수량 오류 끝
            var baseExperience = 40 + data.floor * 20; // 층별 기획 일반 경험치
            var unit = isElite ? baseExperience * 3 : baseExperience; // 상위 또는 일반 적별 경험치
            return saved.normalExperience == saved.initialEnemies.Length * unit && saved.priorExperience == (available - saved.initialEnemies.Length) * unit; // 생존 참가자와 이전 처치 경험치 일치
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
