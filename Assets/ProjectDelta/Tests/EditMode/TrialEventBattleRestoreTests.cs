using System; // 고유 회차 식별자
using System.Collections.Generic; // 실제 방 탐색
using NUnit.Framework; // 순수 복원 검증
using ProjectDelta.Core; // 던전과 전투 복원 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialEventBattleRestoreTests // 실제 조우와 이벤트 전투 복원 검증
    { // 클래스 시작
        private static RunSaveData MakeRun(bool grouped, int[] previousHealth, TrialEventBattleEnemy[] enemies, int normalExperience, int priorExperience) // 실제 생성 던전과 조우 저장 생성
        { // 생성 시작
            var layout = DungeonGenerator.GenerateVersioned(0, 3); // 재현 가능한 실제 층 구조
            var excluded = TrialRoomContentPlacement.Excluded(layout, 1, 3, grouped); // 실제 첫 조우와 다수 조우 배치
            Assert.That(excluded != null, Is.True); // 실제 조우 방 생성 확인
            var room = excluded[grouped ? 1 : 0]; // 실제 다수 또는 첫 조우 방
            var queue = new Queue<DungeonRoom>(); // 연결된 방 탐색 대기열
            var seen = new HashSet<string>(); // 방문한 방 좌표
            var visited = new List<SavedRoom>(); // 저장할 실제 방문 방
            queue.Enqueue(layout.GetRoom(0, 0)); // 시작 방 예약
            seen.Add("0,0"); // 시작 방 방문 표시
            while (queue.Count > 0) // 실제 연결 방 탐색
            { // 탐색 시작
                var current = queue.Dequeue(); // 다음 연결 방
                visited.Add(new SavedRoom { x = current.X, y = current.Y }); // 방문한 실제 방 기록
                foreach (var door in layout.Doors) // 현재 층 출구 순회
                { // 출구 검사 시작
                    if (door.RoomX != current.X || door.RoomY != current.Y || door.IsSecret) // 다른 방 또는 비밀 출구 검사
                    { // 제외 시작
                        continue; // 일반 연결만 탐색
                    } // 제외 끝
                    var target = layout.GetRoom(door.TargetRoomX, door.TargetRoomY); // 연결된 실제 방
                    var key = target.X + "," + target.Y; // 고유 방 좌표
                    if (seen.Add(key)) // 새 방 검사
                    { // 예약 시작
                        queue.Enqueue(target); // 다음 연결 방 예약
                    } // 예약 끝
                } // 출구 검사 끝
            } // 탐색 끝
            Assert.That(TrialEventBattleState.TryCreate(enemies, 100, 50, normalExperience, priorExperience, out var battle), Is.True); // 생존 적의 독립 전투 생성
            return new RunSaveData // 실제 현재 버전의 회차 저장본
            { // 저장 구성 시작
                version = RunSaveData.CurrentVersion, // 실제 현재 저장 버전
                runId = Guid.NewGuid().ToString("N"), // 유효한 회차 식별자
                seed = 0, // 던전 생성 시드
                floor = 1, // 첫 번째 층
                generationVersion = 3, // 실제 조우 생성 규칙
                layoutFingerprint = DungeonLayoutFingerprint.Compute(layout), // 실제 던전 구조 지문
                roomX = room.X, // 조우 방 가로 좌표
                roomY = room.Y, // 조우 방 세로 좌표
                visitedRooms = visited.ToArray(), // 연결된 실제 방문 기록
                doorLock = new DoorLockState(layout).Save(), // 현재 층의 잠금 상태
                secretPassages = new SavedSecretPassage[0], // 비밀 연결 미개방
                health = 100, // 실제 플레이어 체력
                maxHealth = 100, // 실제 최대 체력
                mana = 50, // 실제 플레이어 마나
                maxMana = 50, // 실제 최대 마나
                level = 1, // 기본 레벨
                statPoints = 5, // 기본 스탯 포인트
                slots = new InventoryState().SaveSlots(), // 실제 기본 가방
                history = new string[0], // 빈 탐험 기록
                relicIds = new string[0], // 빈 유물 기록
                shopStocks = new ShopStockData[0], // 빈 상점 기록
                roomContents = new SavedFloorRoomContent[0], // 비활성 방 콘텐츠 기록
                trialEvents = new SavedFloorTrialEvent[0], // 비활성 방 이벤트 기록
                groupEncounterEnabled = grouped, // 실제 시험 다수 조우 여부
                tutorialCompleted = grouped, // 다수 조우의 선행 첫 조우 완료
                groupEnemyHealth = previousHealth, // 이전 후퇴의 개별 체력
                hasTrialEventBattle = true, // 진행 중인 이벤트 전투
                trialEventBattle = battle.Snapshot() // 실제 재생 가능한 전투 저장
            }; // 저장 구성 끝
        } // 생성 끝
        private static TrialEventBattleEnemy Enemy(string species, int slot, int health) // 실제 조우의 단일 생존 적 생성
        { // 생성 시작
            return new TrialEventBattleEnemy { id = species + ":" + slot, slot = slot, health = health, charm = 50 }; // 원본 슬롯과 시험 매력
        } // 생성 끝
        [Test] // 첫 단일 조우의 저장 복원 검증
        public void SingleEncounter_AcceptsActualLivingParticipant() // 실제 첫 조우의 체력 확인
        { // 검증 시작
            var data = MakeRun(false, null, new[] { Enemy("MON_SLIME", 0, 47) }, 60, 0); // 실제 첫 슬라임 조우
            data.tutorialHealth = 50; // 이전 후퇴에서 남은 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 이전 체력보다 감소한 단일 적 허용
            data.tutorialHealth = 46; // 초기 체력보다 낮은 이전 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 이전 체력보다 증가한 참가자 차단
            data.tutorialHealth = 50; // 이전 체력 복구
            data.tutorialCompleted = true; // 이미 완료된 조우
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 완료된 조우의 전투 차단
        } // 검증 끝
        [Test] // 두 생존 적과 이전 처치 경험치 검증
        public void GroupEncounter_AcceptsAliveAndKillsInCurrentBattle() // 실제 다수 조우의 이전 처치
        { // 검증 시작
            var both = MakeRun(true, new[] { 60, 45 }, new[] { Enemy("MON_SLIME", 0, 40), Enemy("MON_SLIME", 1, 30) }, 120, 0); // 둘 다 생존한 기존 조우
            Assert.That(RunRestoreRules.IsValid(both), Is.True); // 두 실제 적의 전투 허용
            var killed = MakeRun(true, new[] { 60, 45 }, new[] { Enemy("MON_SLIME", 1, 30) }, 60, 60); // 이번 일반 전투에서 한 적 처치
            Assert.That(RunRestoreRules.IsValid(killed), Is.True); // 이전 처치 경험치 백 퍼센트 허용
            var afterRetreat = MakeRun(true, new[] { 0, 35 }, new[] { Enemy("MON_SLIME", 1, 30) }, 60, 0); // 이전 후퇴에서 이미 제거한 첫 적
            Assert.That(RunRestoreRules.IsValid(afterRetreat), Is.True); // 이전 회차 전투의 제거 적 보상 제외
        } // 검증 끝
        [Test] // 위치와 참가자 위조 거절 검증
        public void GroupEncounter_RejectsDifferentRoomSlotAndHealth() // 현재 방과 원본 슬롯 일치 검사
        { // 검증 시작
            var data = MakeRun(true, new[] { 0, 35 }, new[] { Enemy("MON_SLIME", 1, 30) }, 60, 0); // 이전 후퇴 후 생존 적
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 원본 저장의 유효성 확인
            data.roomX = 0; // 다른 시작 방 가로 좌표
            data.roomY = 0; // 다른 시작 방 세로 좌표
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 실제 조우가 없는 방 차단
            data = MakeRun(true, new[] { 0, 35 }, new[] { Enemy("MON_SLIME", 0, 30) }, 60, 0); // 이전에 제거된 원본 슬롯
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 이미 죽은 적의 재참가 차단
            data = MakeRun(true, new[] { 0, 35 }, new[] { Enemy("MON_SLIME", 1, 36) }, 60, 0); // 이전보다 증가한 생존 체력
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 실제 체력 증가 차단
            data = MakeRun(true, new[] { 0, 35 }, new[] { Enemy("MON_GOBLIN", 1, 30) }, 60, 0); // 다른 종족으로 위조
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 조우와 다른 종족 차단
        } // 검증 끝
        [Test] // 보상과 경험치 위조 거절 검증
        public void GroupEncounter_RejectsExperienceAndPendingRewardMismatch() // 현재 참가자 수와 보상 일치 검사
        { // 검증 시작
            var data = MakeRun(true, new[] { 60, 45 }, new[] { Enemy("MON_SLIME", 1, 30) }, 60, 60); // 이번 전투에서 한 적 처치
            Assert.That(RunRestoreRules.IsValid(data), Is.True); // 실제 경험치 구성 허용
            data = MakeRun(true, new[] { 60, 45 }, new[] { Enemy("MON_SLIME", 1, 30) }, 60, 0); // 이전 처치 경험치 누락
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 실제 처치와 경험치 불일치 차단
            data = MakeRun(true, new[] { 60, 45 }, new[] { Enemy("MON_SLIME", 1, 30) }, 120, 60); // 생존 적 경험치 위조
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 실제 생존 수와 경험치 불일치 차단
            data = MakeRun(true, new[] { 60, 45 }, new[] { Enemy("MON_SLIME", 1, 30) }, 60, 60); // 정상 전투 기록
            data.hasPendingReward = true; // 다른 미완료 승리 보상
            data.pendingReward = new SavedBattleReward { transactionId = Guid.NewGuid().ToString("N"), normalEnemies = 1, experience = 60 }; // 유효한 일반 보상 후보
            Assert.That(RunRestoreRules.IsValid(data), Is.False); // 이벤트 전투와 일반 보상 충돌 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
