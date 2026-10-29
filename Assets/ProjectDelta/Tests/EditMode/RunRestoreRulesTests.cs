using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 회차 복원 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class RunRestoreRulesTests // 회차 복원 안전성 검증
    { // 클래스 시작
        private static RunSaveData ValidSnapshot() // 재개 가능한 기본 회차
        { // 생성 시작
            var layout = DungeonGenerator.Generate(123); // 재현 가능한 던전
            return new RunSaveData { version = 1, seed = 123, floor = 1, health = 90, maxHealth = 100, slots = new InventoryState().SaveSlots(), visitedRooms = new[] { new SavedRoom() }, doorLock = new DoorLockState(layout).Save(), history = new string[0] }; // 시작 방의 정상 저장본
        } // 생성 끝
        private static RunSaveData ExpandedSnapshot(int floor) // 새 층의 독립 저장 준비
        { // 준비 시작
            const int seed = 123; // 재현 가능한 최초 시드
            const int generation = 3; // 현재 시험 생성 규칙
            var layout = FloorLayoutRules.Generate(seed, floor, generation); // 실제 층의 결정적 구조
            return new RunSaveData { version = 18, runId = System.Guid.NewGuid().ToString("N"), seed = seed, floor = floor, generationVersion = generation, layoutFingerprint = DungeonLayoutFingerprint.Compute(layout), health = 90, maxHealth = 100, mana = 50, maxMana = 50, level = 1, slots = new InventoryState().SaveSlots(), visitedRooms = new[] { new SavedRoom() }, doorLock = new DoorLockState(layout).Save(), history = new string[0], secretPassages = new SavedSecretPassage[0], relicIds = new string[0], shopStocks = new ShopStockData[0], roomContents = new SavedFloorRoomContent[0], trialEvents = new SavedFloorTrialEvent[0] }; // 신규 저장의 필수 빈 목록
        } // 준비 끝
        [Test] // 각 층 재현과 이전 버전 확인
        public void Validate_ExpandedFloorsPreserveVersionAndFingerprint() // 신규 다섯 층과 이전 형식 검증
        { // 검증 시작
            for (var floor = 1; floor <= 5; floor++) // 현재 지원 층 순회
            { // 층별 검사 시작
                var data = ExpandedSnapshot(floor); // 실제 층의 정상 저장
                Assert.That(RunRestoreRules.IsValid(data), Is.True, "층 " + floor); // 정상 층 복원 허용
                data.layoutFingerprint = "incorrect"; // 손상된 실제 구조 지문
                Assert.That(RunRestoreRules.IsValid(data), Is.False, "층 " + floor); // 잘못된 지문 차단
            } // 층별 검사 끝
            var legacy = ExpandedSnapshot(2); // 기존 두 번째 층
            legacy.version = 17; // 이전 저장 형식
            Assert.That(RunRestoreRules.IsValid(legacy), Is.True); // 기존 두 번째 층 유지
            legacy = ExpandedSnapshot(3); // 신규 세 번째 층
            legacy.version = 17; // 기존 저장 형식 적용
            Assert.That(RunRestoreRules.IsValid(legacy), Is.False); // 이전 형식의 미지원 층 차단
            var invalid = ExpandedSnapshot(5); // 정상 다섯 번째 층
            invalid.floor = 6; // 범위 밖 다음 층
            Assert.That(RunRestoreRules.IsValid(invalid), Is.False); // 여섯 번째 층 차단
        } // 검증 끝
        [Test] // 다섯 층 콘텐츠와 상점 재현 검증
        public void Validate_ExpandedFloorCollectionsRejectChangedPlacements() // 저장된 층별 상태 검증
        { // 검증 시작
            var data = ExpandedSnapshot(5); // 다섯 번째 층 저장
            data.roomContentEnabled = true; // 층별 콘텐츠 활성화
            data.trialEventsEnabled = true; // 층별 이벤트 활성화
            data.roomContents = new SavedFloorRoomContent[5]; // 다섯 층 콘텐츠 목록
            data.trialEvents = new SavedFloorTrialEvent[5]; // 다섯 층 이벤트 목록
            data.shopStocks = new ShopStockData[5]; // 다섯 층 상점 목록
            for (var floor = 1; floor <= 5; floor++) // 각 층 저장 생성
            { // 층별 생성 시작
                var seed = FloorSeedRules.For(data.seed, floor); // 저장 규칙의 층별 시드
                var layout = FloorLayoutRules.Generate(data.seed, floor, data.generationVersion); // 동일한 층별 구조
                var excluded = TrialRoomContentPlacement.Excluded(layout, floor, data.generationVersion, false); // 기존 조우 제외
                Assert.That(FloorRoomContent.TryCreate(floor, layout, seed, excluded, out var content), Is.True, "층 " + floor); // 해당 층 콘텐츠 생성
                data.roomContents[floor - 1] = content.Snapshot(); // 실제 상자와 함정 저장
                Assert.That(TrialEventPlacement.TrySelect(layout, seed, data.roomContents[floor - 1], excluded, out var room), Is.True, "층 " + floor); // 기존 콘텐츠와 분리된 이벤트
                Assert.That(TrialEventState.TryCreate("trial-event-01", data.seed & 1, out var eventState), Is.True); // 회차의 고정 선택 순서
                data.trialEvents[floor - 1] = new SavedFloorTrialEvent { floor = floor, x = room.X, y = room.Y, eventData = eventState.Snapshot() }; // 해당 층 이벤트 저장
                data.shopStocks[floor - 1] = new ShopStockData { floor = floor, offers = new ShopOfferData[0] }; // 독립 층의 정상 상점
            } // 층별 생성 끝
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 다섯 층의 결정적 상태 복원 허용
            data.trialEvents[2].x += 100; // 세 번째 층 이벤트 좌표 변조
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 다른 층의 변조된 배치 차단
            data.trialEvents[2].x -= 100; // 원래 이벤트 좌표 복구
            data.shopStocks[4].floor = 4; // 중복된 상점 층
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 중복된 층별 상점 차단
        } // 검증 끝
        [Test] // 최종 보스의 미지원 조우 저장 검증
        public void Validate_FinalFloorRejectsUnsupportedEliteState() // 보스 층의 잘못된 상위 조우 차단
        { // 검증 시작
            var data = ExpandedSnapshot(5); // 정상 다섯 번째 층
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 최종 층의 일반 탐험 허용
            data.encounterHealth = 20; // 없는 상위 개체의 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 미지원 상위 체력 거절
            data.encounterHealth = 0; // 체력 기록 해제
            data.encounterCompleted = true; // 없는 상위 개체의 완료 표시
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 미지원 상위 완료 거절
            data.encounterCompleted = false; // 없는 완료 기록 해제
            var layout = FloorLayoutRules.Generate(data.seed, 5, data.generationVersion); // 실제 최종 층 구조
            var visited = new System.Collections.Generic.List<SavedRoom>(); // 실제 방문한 방 목록
            DungeonRoom boss = null; // 실제 마왕 방
            foreach (var room in layout.Rooms) // 층의 방 순회
            { // 방 확인 시작
                if (!room.IsSecret) // 일반 연결 방 검사
                { // 방문 기록 시작
                    visited.Add(new SavedRoom { x = room.X, y = room.Y }); // 실제 방 방문
                } // 방문 기록 끝
                if (room.Kind == RoomKind.Boss) // 실제 보스 방 검사
                { // 보스 확인 시작
                    boss = room; // 실제 보스 방 보관
                } // 보스 확인 끝
            } // 방 확인 끝
            Assert.That(boss, Is.Not.Null); // 실제 보스 방 존재 확인
            data.visitedRooms = visited.ToArray(); // 접근 가능한 방 방문 목록
            data.roomX = boss.X; // 실제 보스 방 가로 좌표
            data.roomY = boss.Y; // 실제 보스 방 세로 좌표
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 보스 방의 일반 탐험 저장 허용
            var enemy = new TrialEventBattleEnemy { id = "trial-elite:0", slot = 0, health = 70, charm = 50 }; // 기존 상위 개체로 위조된 대상
            Assert.That(TrialEventBattleState.TryCreate(new[] { enemy }, 100, 50, 420, 0, out var battle), Is.True); // 구조적으로 정상인 위조 전투 생성
            data.hasTrialEventBattle = true; // 미지원 보스 방의 전투 설정
            data.trialEventBattle = battle.Snapshot(); // 위조된 상위 전투 기록
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 보스 방의 미지원 전투 차단
        } // 검증 끝
        [Test] // 상위 조우의 층별 이벤트 경험치 검증
        public void Validate_ExpandedEventBattleRequiresFloorExperience() // 층별 조우와 경험치 일치 확인
        { // 검증 시작
            for (var floor = 3; floor <= 4; floor++) // 신규 시험 상위 층 순회
            { // 층별 검사 시작
                var data = ExpandedSnapshot(floor); // 현재 층 정상 저장
                var layout = FloorLayoutRules.Generate(data.seed, floor, data.generationVersion); // 실제 층 구조
                var elite = System.Array.Find(new System.Collections.Generic.List<DungeonRoom>(layout.Rooms).ToArray(), room => room.Kind == RoomKind.Elite); // 실제 상위 방
                Assert.That(elite, Is.Not.Null); // 상위 방 생성 확인
                var visited = new System.Collections.Generic.List<SavedRoom>(); // 실제 방 방문 목록
                foreach (var room in layout.Rooms) // 생성된 방 순회
                { // 방문 기록 시작
                    if (!room.IsSecret) // 일반 연결 방 검사
                    { // 일반 방문 시작
                        visited.Add(new SavedRoom { x = room.X, y = room.Y }); // 실제 방문 방 저장
                    } // 일반 방문 끝
                } // 방문 기록 끝
                data.visitedRooms = visited.ToArray(); // 현재 층의 실제 방문 기록
                data.roomX = elite.X; // 실제 상위 방 가로 좌표
                data.roomY = elite.Y; // 실제 상위 방 세로 좌표
                var enemy = new TrialEventBattleEnemy { id = "trial-elite:0", slot = 0, health = 70, charm = 50 }; // 실제 시험 상위 대상
                var experience = (40 + floor * 20) * 3; // 해당 층의 상위 기본 경험치
                Assert.That(TrialEventBattleState.TryCreate(new[] { enemy }, 100, 50, experience, 0, out var battle), Is.True); // 정상 이벤트 전투 생성
                data.hasTrialEventBattle = true; // 전투 복원 활성화
                data.trialEventBattle = battle.Snapshot(); // 검증된 전투 저장
                Assert.That(RunRestoreRules.IsValid(data), Is.True, "층 " + floor); // 해당 층의 실제 경험치 허용
                Assert.That(TrialEventBattleState.TryCreate(new[] { enemy }, 100, 50, 240, 0, out battle), Is.True); // 다른 층의 상위 경험치
                data.trialEventBattle = battle.Snapshot(); // 잘못된 층의 경험치 적용
                Assert.That(RunRestoreRules.IsValid(data), Is.False, "층 " + floor); // 이전 2층 경험치 차단
            } // 층별 검사 끝
        } // 검증 끝
        private static RunSaveData BossResultSnapshot() // 실제 보스 방의 결과 저장 준비
        { // 준비 시작
            var data = ExpandedSnapshot(5); // 현재 최종 층 저장
            data.version = 19; // 보스 결과를 지원하는 저장 형식
            var layout = FloorLayoutRules.Generate(data.seed, 5, data.generationVersion); // 실제 최종 층 구조
            var visited = new System.Collections.Generic.List<SavedRoom>(); // 연결된 방문 방 목록
            foreach (var room in layout.Rooms) // 실제 방 순회
            { // 방문 기록 시작
                if (!room.IsSecret) // 비밀 방이 아닌 실제 방 검사
                { // 일반 방 시작
                    visited.Add(new SavedRoom { x = room.X, y = room.Y }); // 방문한 방 기록
                } // 일반 방 끝
                if (room.Kind == RoomKind.Boss) // 최종 보스 방 검사
                { // 보스 위치 시작
                    data.roomX = room.X; // 실제 보스 방 가로 좌표
                    data.roomY = room.Y; // 실제 보스 방 세로 좌표
                } // 보스 위치 끝
            } // 방문 기록 끝
            data.visitedRooms = visited.ToArray(); // 검증 가능한 방문 기록
            data.bossTransactionId = System.Guid.NewGuid().ToString("N"); // 실제 결과의 고유 거래
            return data; // 검증 가능한 보스 방 저장
        } // 준비 끝
        [Test] // 마왕 결과와 체력의 저장 검증
        public void Validate_BossResultRequiresMatchingLocationAndHealth() // 승리와 패배 및 항복의 범위 검사
        { // 검증 시작
            var data = BossResultSnapshot(); // 실제 보스 방 저장
            data.bossOutcome = 1; // 일반 전투 승리
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 생존 승리 결과 허용
            data.bossOutcome = 3; // 명시적 항복
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 생존 항복 결과 허용
            data.health = 0; // 실제 체력 소진
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 체력 없는 항복 거절
            data.bossOutcome = 1; // 일반 전투 승리
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 체력 없는 승리 거절
            data.bossOutcome = 2; // 체력 소진 패배
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 체력 소진 패배 허용
            data.health = 1; // 생존 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 생존한 체력 소진 패배 거절
            data.health = 0; // 패배 체력 복구
            data.roomX = 0; // 보스가 아닌 시작 방
            data.roomY = 0; // 시작 방 세로 좌표
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 다른 방의 보스 결과 차단
        } // 검증 끝
        [Test] // 결과 식별자와 저장 상태 충돌 검증
        public void Validate_BossResultRejectsInvalidOrConcurrentState() // 고유 식별자와 전투 충돌 검사
        { // 검증 시작
            var data = BossResultSnapshot(); // 실제 보스 방 저장
            data.bossOutcome = 1; // 실제 보스 승리
            data.bossTransactionId = "invalid"; // 잘못된 거래 식별자
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 위조된 결과 식별자 차단
            data.bossTransactionId = System.Guid.NewGuid().ToString("N"); // 유효한 결과 식별자
            data.hasPendingReward = true; // 일반 승리 보상 중복
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 일반 보상과 결과 충돌 차단
            data.hasPendingReward = false; // 일반 보상 비활성화
            data.hasTrialEventBattle = true; // 일반 이벤트 전투 중복
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 다른 전투와 결과 충돌 차단
            data.hasTrialEventBattle = false; // 다른 전투 비활성화
            data.pendingReward = new SavedBattleReward(); // 비활성 표시 뒤 남은 보상 후보
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 숨겨진 보상 후보 차단
            data.pendingReward = null; // 보상 후보 제거
            data.rewardReceipts = new[] { new SavedRewardReceipt { transactionId = data.bossTransactionId } }; // 기존 보상과 겹치는 거래
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 이미 사용된 거래 식별자 차단
            data.rewardReceipts = null; // 보상 거래 충돌 해제
            data.bossOutcome = 0; // 아직 결정되지 않은 보스 결과
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 미결 결과의 거래 식별자 차단
            data.bossTransactionId = null; // 거래 없는 미결 상태
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 보스 방 탐험 허용
            data.bossOutcome = 4; // 지원하지 않는 결과 값
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 알 수 없는 결과 차단
        } // 검증 끝
        [Test] // 이전 저장과 미결 전투 호환 검증
        public void Validate_BossFieldsDoNotChangeLegacySaves() // 기존 저장의 보스 필드 무시
        { // 검증 시작
            var data = ExpandedSnapshot(5); // 기존 최종 층 저장
            data.bossOutcome = 2; // 기존 버전에서 사용하지 않는 값
            data.bossTransactionId = "invalid"; // 기존 버전에서 사용하지 않는 식별자
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 기존 버전의 복원 유지
            data.health = 0; // 기존 저장의 체력 소진
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 이전 저장의 사망 상태 차단 유지
        } // 검증 끝
        [Test] // 유효한 회차의 복원 가능 여부
        public void Validate_AcceptsCoherentSnapshot() // 정상 회차 허용
        { // 검증 시작
            Assert.That(RunRestoreRules.IsValid(ValidSnapshot()), Is.True); // 정상 저장본 검증
        } // 검증 끝
        [Test] // 불일치 상태의 복원 거절
        public void Validate_RejectsInvalidLocationAndInventory() // 잘못된 저장 거절
        { // 검증 시작
            var data = ValidSnapshot(); // 정상 회차 준비
            data.roomX = 999; // 없는 방 좌표
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 잘못된 방 거절
            data = ValidSnapshot(); // 정상 회차 재준비
            data.keys = -1; // 음수 열쇠 수량
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 잘못된 열쇠 거절
            data = ValidSnapshot(); // 정상 회차 재준비
            data.slots = new SaveItemSlot[0]; // 누락된 인벤토리 칸
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 슬롯 수 오류 거절
        } // 검증 끝
        [Test] // 새 저장의 던전 구조 일치 검증
        public void Validate_RequiresMatchingLayoutFingerprintForNewVersion() // 던전 구조 변경 거절
        { // 검증 시작
            var data = ValidSnapshot(); // 정상 이전 저장 준비
            data.version = 2; // 새 회차 저장 버전
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(DungeonGenerator.Generate(data.seed)); // 실제 생성 구조의 지문
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 같은 던전의 재개 허용
            data.layoutFingerprint = "incorrect"; // 변조된 던전 지문
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 다른 구조 거절
            data.layoutFingerprint = null; // 지문 누락
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 누락된 지문 거절
        } // 검증 끝
        [Test] // 이전 회차 저장 호환성 검증
        public void Validate_AcceptsLegacyVersionWithoutFingerprint() // 이전 저장 읽기 유지
        { // 검증 시작
            var data = ValidSnapshot(); // 기존 버전 회차
            Assert.That(data.version, Is.EqualTo(1)); // 기존 버전 확인
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 지문이 없는 기존 회차 허용
        } // 검증 끝
        [Test] // 새로운 던전 생성 버전과 비밀 상태 검증
        public void Validate_AcceptsVersionedSecretLayoutAndRejectsMissingState() // 비밀 생성과 저장 상태 일치
        { // 검증 시작
            var data = ValidSnapshot(); // 정상 이전 회차 준비
            var layout = DungeonGenerator.GenerateWithSecret(data.seed); // 비밀방 생성 규칙
            data.version = 3; // 비밀 상태를 포함한 저장 버전
            data.generationVersion = 2; // 비밀방 생성 버전
            data.secretPassages = new SavedSecretPassage[0]; // 아직 미발견된 비밀 연결
            data.doorLock = new DoorLockState(layout).Save(); // 새 구조의 시험 문 상태
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(layout); // 새 구조의 지문
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 새 구조의 재개 허용
            data.secretPassages = null; // 누락된 비밀 상태
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 누락된 상태 거절
        } // 검증 끝
        [Test] // 기존 생성 구조의 새 저장 형식 유지
        public void Validate_LegacyLayoutCanBeResavedWithOriginalGenerator() // 이전 회차 생성 규칙 보존
        { // 검증 시작
            var data = ValidSnapshot(); // 기존 생성 구조의 정상 회차
            data.version = 3; // 새 저장 형식
            data.generationVersion = 1; // 기존 던전 생성 규칙
            data.secretPassages = new SavedSecretPassage[0]; // 기존 구조의 빈 비밀 기록
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(DungeonGenerator.Generate(data.seed)); // 기존 구조 지문
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 기존 구조의 새 형식 저장 허용
            data.generationVersion = 2; // 잘못된 새 생성 규칙
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 다른 구조로 재생성 거절
        } // 검증 끝
        [Test] // 첫 조우 상태와 새 저장 버전 검증
        public void Validate_TutorialEncounterStateMatchesGeneratedLayout() // 첫 조우의 저장 상태 확인
        { // 검증 시작
            var data = ValidSnapshot(); // 이전 형식의 정상 데이터
            var layout = DungeonGenerator.GenerateVersioned(data.seed, 3); // 첫 일반 방이 보장된 구조
            data.version = 4; // 첫 조우 저장 버전
            data.generationVersion = 3; // 첫 조우 생성 버전
            data.secretPassages = new SavedSecretPassage[0]; // 비밀 연결의 초기 상태
            data.doorLock = new DoorLockState(layout).Save(); // 현재 구조의 시험 문
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(layout); // 현재 구조 지문
            data.tutorialHealth = 40; // 도망 후 남은 상대 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 미완료 조우의 유효 체력 허용
            data.tutorialCompleted = true; // 첫 조우 완료 상태
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 완료 조우의 남은 체력 거절
            data.tutorialHealth = 0; // 완료 상태의 체력 기록 제거
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 완료한 첫 조우 허용
        } // 검증 끝
        [Test] // 마나를 포함한 저장과 기존 저장 호환 검증
        public void Validate_ManaSnapshotRequiresValidBounds() // 새 회차의 마나 범위 확인
        { // 검증 시작
            var data = ValidSnapshot(); // 기존 회차 저장 준비
            data.version = 5; // 마나를 포함하는 저장 버전
            data.generationVersion = 1; // 기존 던전 생성 규칙
            data.secretPassages = new SavedSecretPassage[0]; // 기존 구조의 비밀 기록
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(DungeonGenerator.Generate(data.seed)); // 기존 구조 지문
            data.mana = 25; // 현재 마나 설정
            data.maxMana = 50; // 기본 최대 마나 설정
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 마나가 유효한 저장 허용
            data.mana = 51; // 최대치를 넘는 마나 설정
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 잘못된 마나 저장 차단
            data.mana = -1; // 음수 마나 설정
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 음수 마나 저장 차단
            Assert.That(RunRestoreRules.IsValid(ValidSnapshot()), Is.True); // 마나가 없는 기존 저장 허용
        } // 검증 끝
        [Test] // 다수 조우의 저장과 이전 저장 검증
        public void Validate_GroupEncounterRequiresCoherentEnemyHealth() // 적별 체력과 완료 상태 확인
        { // 검증 시작
            var data = ValidSnapshot(); // 기존 회차 저장 준비
            data.version = 6; // 다수 조우 저장 버전
            data.generationVersion = 3; // 시험 조우가 있는 던전 생성 규칙
            data.secretPassages = new SavedSecretPassage[0]; // 새 구조의 비밀 기록
            var layout = DungeonGenerator.GenerateVersioned(data.seed, 3); // 시험 조우 구조 생성
            data.doorLock = new DoorLockState(layout).Save(); // 실제 시험 문의 저장 상태
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(layout); // 실제 구조 지문
            data.mana = 50; // 현재 마나
            data.maxMana = 50; // 기본 최대 마나
            data.groupEncounterEnabled = true; // 시험 다수 조우 활성화
            data.groupEnemyHealth = new[] { 0, 35 }; // 쓰러진 적과 남은 적 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 일부 생존한 조우 허용
            data.groupEnemyHealth = new[] { 0, 0 }; // 모두 쓰러진 미완료 조우
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 미완료 조우의 전멸 거절
            data.groupEnemyHealth = new[] { 70, 71 }; // 잘못된 적 최대 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 범위 밖 체력 거절
            data.groupEnemyHealth = new[] { 70, 20 }; // 정상 상태 재설정
            data.groupEncounterCompleted = true; // 완료 상태와 남은 적 동시 설정
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 완료와 미완료 상태 충돌 거절
            data.groupEnemyHealth = null; // 완료 후 적별 체력 기록 제거
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 완료 조우 저장 허용
            Assert.That(RunRestoreRules.IsValid(ValidSnapshot()), Is.True); // 다수 조우 없는 기존 저장 허용
        } // 검증 끝
        [Test] // 회차별 영구 기록 식별자 검증
        public void Validate_RunIdentifierIsRequiredOnlyForNewVersion() // 새 회차 식별자와 기존 저장 확인
        { // 검증 시작
            var data = ValidSnapshot(); // 기존 회차 저장 준비
            data.version = 7; // 회차 식별자를 포함하는 저장 버전
            data.generationVersion = 1; // 기존 던전 생성 규칙
            data.secretPassages = new SavedSecretPassage[0]; // 기존 구조의 비밀 기록
            data.layoutFingerprint = DungeonLayoutFingerprint.Compute(DungeonGenerator.Generate(data.seed)); // 기존 구조 지문
            data.mana = 50; // 현재 마나
            data.maxMana = 50; // 기본 최대 마나
            data.runId = "invalid"; // 잘못된 회차 식별자
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 잘못된 식별자 차단
            data.runId = System.Guid.NewGuid().ToString("N"); // 유효한 회차 식별자
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 정상 회차 식별자 허용
            Assert.That(RunRestoreRules.IsValid(ValidSnapshot()), Is.True); // 식별자가 없는 기존 저장 허용
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
