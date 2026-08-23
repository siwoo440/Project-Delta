using System.Collections.Generic; // 목록·집합 기능 사용
using NUnit.Framework; // NUnit 테스트 기능 사용
using ProjectDelta.Domain; // 던전 생성 도메인 사용

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public sealed class DungeonGenerationStabilityTests // 35일차 검증·재시도·저장/복원 테스트
    {
        [Test]
        public void Validator_ValidGeneratedDungeon_PassesAllConstraints() // 정상 생성 결과가 최종 검증을 통과하는지 확인
        {
            DungeonGenerationSettings settings = StableSettings(); // 안정성 테스트 설정
            GeneratedDungeon dungeon = new DungeonGenerator(35).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 던전 생성
            DungeonValidationResult validation = new DungeonGenerationValidator().Validate(dungeon, settings); // 전체 검증

            Assert.IsTrue(validation.IsValid, JoinIssues(validation.Issues)); // 문제 없음 확인
            Assert.AreEqual(settings.TargetRoomCount, dungeon.Layout.AllRooms.Count); // 정확한 방 수 확인
        }

        [Test]
        public void Validator_DetectsRoomCountMismatch() // 목표 방 수 미달을 생성 성공으로 취급하지 않는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(
                10,
                5,
                5,
                branchChance: 0d,
                loopChance: 0d); // 가지를 막아 목표 방 수에 도달하지 못하게 설정

            GeneratedDungeon dungeon = new DungeonGenerator(3).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 메인 경로만 생성
            DungeonValidationResult validation = new DungeonGenerationValidator().Validate(dungeon, settings); // 검증
            bool found = ContainsIssue(validation, DungeonValidationCode.RoomCountMismatch); // 방 수 문제 검색

            Assert.IsFalse(validation.IsValid); // 전체 검증 실패 확인
            Assert.IsTrue(found, JoinIssues(validation.Issues)); // 목표 방 수 실패 원인 존재 확인
        }

        [Test]
        public void GenerationService_FailedAttempts_RecordSequentialSeeds() // 실패 시 Seed를 증가시키며 정해진 횟수만 재시도하는지 확인
        {
            DungeonGenerationSettings settings = new DungeonGenerationSettings(
                10,
                5,
                5,
                branchChance: 0d,
                loopChance: 0d); // 모든 Seed가 방 수 검증에 실패하도록 설정

            DungeonGenerationRunResult result = new DungeonGenerationService().GenerateWithRetry(
                CrossTemplate("ROOM_ENTRY"),
                CrossPool(),
                settings,
                requestedSeed: 100,
                maxAttempts: 3); // 세 번 재시도

            Assert.IsFalse(result.Success); // 최종 실패 확인
            Assert.AreEqual(3, result.AttemptCount); // 정확한 시도 횟수 확인
            Assert.AreEqual(100, result.Attempts[0].Seed); // 첫 Seed 기록
            Assert.AreEqual(101, result.Attempts[1].Seed); // 두 번째 Seed 기록
            Assert.AreEqual(102, result.Attempts[2].Seed); // 세 번째 Seed 기록
            Assert.AreEqual(102, result.SuccessfulSeed); // 실패 시 마지막 Seed 기록 확인
        }

        [Test]
        public void GenerationService_Success_RecordsSuccessfulSeedAndValidation() // 성공한 Seed와 최종 검증 정보를 보존하는지 확인
        {
            DungeonGenerationSettings settings = StableSettings(); // 안정 설정
            DungeonGenerationRunResult result = new DungeonGenerationService().GenerateWithRetry(
                CrossTemplate("ROOM_ENTRY"),
                CrossPool(),
                settings,
                requestedSeed: 500,
                maxAttempts: 10); // 유효 던전 생성

            Assert.IsTrue(result.Success, BuildRunFailure(result)); // 최종 성공 확인
            Assert.IsNotNull(result.Dungeon); // 성공 던전 존재 확인
            Assert.IsNotNull(result.Validation); // 검증 결과 존재 확인
            Assert.IsTrue(result.Validation.IsValid, JoinIssues(result.Validation.Issues)); // 성공 결과 유효성 확인
            Assert.AreEqual(result.RequestedSeed + result.AttemptCount - 1, result.SuccessfulSeed); // Seed 증가 규칙 확인
        }

        [Test]
        public void Snapshot_Restore_PreservesRoomsConnectionsRolesAndEndpoints() // Snapshot 왕복 후 논리 레이아웃이 유지되는지 확인
        {
            DungeonGenerationSettings settings = StableSettings(); // 안정 설정
            DungeonGenerationRunResult run = new DungeonGenerationService().GenerateWithRetry(
                CrossTemplate("ROOM_ENTRY"),
                CrossPool(),
                settings,
                requestedSeed: 700,
                maxAttempts: 10); // 유효 던전 생성

            Assert.IsTrue(run.Success, BuildRunFailure(run)); // 사전 생성 성공 확인

            DungeonLayoutSnapshot snapshot = DungeonLayoutSnapshot.Capture(run.Dungeon, run.SuccessfulSeed); // 저장 데이터 생성
            GeneratedDungeon restored = snapshot.Restore(); // 새 그래프로 복원
            DungeonValidationResult validation = new DungeonGenerationValidator().Validate(restored, settings); // 복원 결과 검증

            Assert.IsTrue(validation.IsValid, JoinIssues(validation.Issues)); // 복원 그래프 유효성 확인
            Assert.AreEqual(run.Dungeon.Layout.AllRooms.Count, restored.Layout.AllRooms.Count); // 방 수 보존
            Assert.AreEqual(run.Dungeon.EntryRoom.RoomId, restored.EntryRoom.RoomId); // Entry 보존
            Assert.AreEqual(run.Dungeon.StairsRoom.RoomId, restored.StairsRoom.RoomId); // Stairs 보존
            Assert.AreEqual(run.Dungeon.MainPath.Count, restored.MainPath.Count); // 메인 경로 보존
            CollectionAssert.AreEquivalent(BuildConnectionSet(run.Dungeon.Layout), BuildConnectionSet(restored.Layout)); // 전체 연결 보존
            CollectionAssert.AreEquivalent(BuildRoleSet(run.Dungeon), BuildRoleSet(restored)); // 생성 역할 보존
        }

        [Test]
        public void SameSeed_RecreatesIdenticalSnapshotSignature() // 같은 Seed가 전체 논리 레이아웃을 동일하게 재현하는지 확인
        {
            DungeonGenerationSettings settings = StableSettings(); // 동일 설정
            GeneratedDungeon first = new DungeonGenerator(12345).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 첫 생성
            GeneratedDungeon second = new DungeonGenerator(12345).Generate(CrossTemplate("ROOM_ENTRY"), CrossPool(), settings); // 두 번째 생성
            DungeonLayoutSnapshot firstSnapshot = DungeonLayoutSnapshot.Capture(first, 12345); // 첫 Snapshot
            DungeonLayoutSnapshot secondSnapshot = DungeonLayoutSnapshot.Capture(second, 12345); // 두 번째 Snapshot

            Assert.AreEqual(BuildSnapshotSignature(firstSnapshot), BuildSnapshotSignature(secondSnapshot)); // 전체 저장 데이터 재현 확인
        }

        [Test]
        [Category("Stress")]
        public void Stress_10000RequestedSeeds_ProduceValidDungeonWithRetry() // 10,000개 요청 Seed 대량 자동 생성 검증
        {
            DungeonGenerationSettings settings = StableSettings(); // 안정 설정
            DungeonGenerationService service = new DungeonGenerationService(); // 재시도 포함 서비스
            RoomTemplate entry = CrossTemplate("ROOM_ENTRY"); // 공통 시작 방
            List<RoomTemplate> pool = CrossPool(); // 공통 방 풀

            for (int requestedSeed = 0; requestedSeed < 10000; requestedSeed++) // Seed 0~9999 검증
            {
                DungeonGenerationRunResult result = service.GenerateWithRetry(
                    entry,
                    pool,
                    settings,
                    requestedSeed,
                    maxAttempts: 10); // Seed별 최대 10회 시도

                Assert.IsTrue(
                    result.Success,
                    $"RequestedSeed={requestedSeed}\n{BuildRunFailure(result)}"); // 실패 Seed와 원인을 즉시 출력

                Assert.IsTrue(
                    result.Validation.IsValid,
                    $"RequestedSeed={requestedSeed}, SuccessfulSeed={result.SuccessfulSeed}\n{JoinIssues(result.Validation.Issues)}"); // 최종 검증 이중 확인
            }
        }

        private static DungeonGenerationSettings StableSettings() // 스트레스 검증에서 사용할 충분히 생성 가능한 규칙
        {
            return new DungeonGenerationSettings(
                targetRoomCount: 8,
                minMainPathLength: 6,
                maxMainPathLength: 6,
                branchChance: 1d,
                minBranchLength: 1,
                maxBranchLength: 1,
                specialCandidateChance: 0.30d,
                loopChance: 0d); // 10,000회 검증은 단순하고 안정적인 구조로 생성기 무결성에 집중
        }

        private static bool ContainsIssue(DungeonValidationResult validation, DungeonValidationCode code) // 특정 실패 종류 존재 여부
        {
            for (int i = 0; i < validation.Issues.Count; i++) // 문제 전체 순회
            {
                if (validation.Issues[i].Code == code) // 원하는 종류인지 확인
                {
                    return true; // 발견
                }
            }

            return false; // 미발견
        }

        private static HashSet<string> BuildConnectionSet(DungeonLayoutGraph graph) // 무방향 연결과 RoomExit 쌍 비교 집합
        {
            HashSet<string> result = new HashSet<string>(); // 연결 집합

            foreach (RoomNode room in graph.AllRooms) // 전체 방 순회
            {
                foreach (KeyValuePair<CardinalDirection, RoomConnectionEdge> pair in room.Connections) // 모든 Edge 순회
                {
                    RoomConnectionEdge edge = pair.Value; // 현재 Edge

                    if (edge == null || edge.Neighbor == null || !edge.HasExactExitPair) // 비교 가능한 Edge인지 확인
                    {
                        continue; // 잘못된 Edge는 Validator가 별도 검출
                    }

                    string first = $"{room.RoomId}|{edge.LocalExit.Value}";
                    string second = $"{edge.Neighbor.RoomId}|{edge.NeighborExit.Value}";
                    string key = string.CompareOrdinal(room.RoomId, edge.Neighbor.RoomId) < 0
                        ? $"{first}<->{second}"
                        : $"{second}<->{first}"; // 방향과 무관한 연결 서명
                    result.Add(key); // 중복 제거
                }
            }

            return result; // 전체 연결 반환
        }

        private static HashSet<string> BuildRoleSet(GeneratedDungeon dungeon) // 방 ID별 생성 역할 비교 집합
        {
            HashSet<string> result = new HashSet<string>(); // 역할 집합

            foreach (RoomNode room in dungeon.Layout.AllRooms) // 전체 방 순회
            {
                if (dungeon.TryGetRoomRole(room, out DungeonRoomRole role)) // 역할 조회
                {
                    result.Add($"{room.RoomId}:{role}"); // ID와 역할 저장
                }
            }

            return result; // 역할 집합 반환
        }

        private static string BuildSnapshotSignature(DungeonLayoutSnapshot snapshot) // Snapshot 전체 비교용 문자열
        {
            List<string> parts = new List<string> // 기본 메타데이터
            {
                $"Seed={snapshot.Seed}",
                $"Entry={snapshot.EntryRoomId}",
                $"Stairs={snapshot.StairsRoomId}",
                $"MainTarget={snapshot.TargetMainPathLength}",
                $"RoomTarget={snapshot.TargetRoomCount}",
                $"Main={string.Join(",", snapshot.MainPathRoomIds)}",
                $"Branch={string.Join(",", snapshot.BranchRoomIds)}",
                $"Dead={string.Join(",", snapshot.DeadEndCandidateRoomIds)}",
                $"Special={string.Join(",", snapshot.SpecialCandidateRoomIds)}"
            };

            for (int i = 0; i < snapshot.Rooms.Count; i++) // 방 데이터 추가
            {
                DungeonRoomSnapshot room = snapshot.Rooms[i]; // 현재 방
                parts.Add($"R:{room.RoomId}:{room.DefinitionId}:{room.MacroX}:{room.MacroZ}:{room.Role}"); // 방 서명
            }

            for (int i = 0; i < snapshot.Connections.Count; i++) // 연결 데이터 추가
            {
                DungeonConnectionSnapshot edge = snapshot.Connections[i]; // 현재 연결
                parts.Add(
                    $"E:{edge.FromRoomId}:{edge.FromExitX}:{edge.FromExitZ}:{edge.FromExitDirection}"
                    + $"->{edge.ToRoomId}:{edge.ToExitX}:{edge.ToExitZ}:{edge.ToExitDirection}:{edge.IsLocked}"); // 연결 서명
            }

            return string.Join("\n", parts); // 전체 문자열 반환
        }

        private static string BuildRunFailure(DungeonGenerationRunResult result) // 재시도 실패 로그 문자열
        {
            if (result == null) // 결과 존재 확인
            {
                return "DungeonGenerationRunResult가 null입니다."; // null 설명
            }

            List<string> lines = new List<string>
            {
                $"RequestedSeed={result.RequestedSeed}",
                $"LastSeed={result.SuccessfulSeed}",
                $"Attempts={result.AttemptCount}"
            }; // 기본 실패 정보

            for (int attemptIndex = 0; attemptIndex < result.Attempts.Count; attemptIndex++) // 모든 시도 로그 출력
            {
                DungeonGenerationAttemptLog attempt = result.Attempts[attemptIndex]; // 현재 시도
                lines.Add($"Attempt {attempt.AttemptNumber} / Seed {attempt.Seed} / Rooms {attempt.GeneratedRoomCount}"); // 시도 요약

                for (int issueIndex = 0; issueIndex < attempt.Issues.Count; issueIndex++) // 해당 시도 문제 출력
                {
                    lines.Add($"  - {attempt.Issues[issueIndex]}"); // 실패 원인 추가
                }
            }

            return string.Join("\n", lines); // 하나의 로그 문자열 반환
        }

        private static string JoinIssues(IReadOnlyList<DungeonValidationIssue> issues) // 검증 문제 문자열 결합
        {
            List<string> lines = new List<string>(); // 출력 줄

            if (issues == null) // 목록 존재 확인
            {
                return "검증 결과가 없습니다."; // null 설명
            }

            for (int i = 0; i < issues.Count; i++) // 문제 전체 순회
            {
                lines.Add(issues[i].ToString()); // 문자열 추가
            }

            return string.Join("\n", lines); // 줄바꿈 결합
        }

        private static RoomTemplate CrossTemplate(string id) // 5×5 중앙 4출구 방 템플릿
        {
            return new RoomTemplate(id, new List<RoomExit>
            {
                new RoomExit(new GridPosition(0, 2), CardinalDirection.North),
                new RoomExit(new GridPosition(2, 0), CardinalDirection.East),
                new RoomExit(new GridPosition(0, -2), CardinalDirection.South),
                new RoomExit(new GridPosition(-2, 0), CardinalDirection.West)
            });
        }

        private static List<RoomTemplate> CrossPool() // 안정성 검증용 다중 출구 방 풀
        {
            return new List<RoomTemplate>
            {
                CrossTemplate("ROOM_CROSS_A"),
                CrossTemplate("ROOM_CROSS_B"),
                CrossTemplate("ROOM_CROSS_C")
            };
        }
    }
}
