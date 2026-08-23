using System; // 예외 기능 사용
using System.Collections.Generic; // 목록 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    public sealed class DungeonGenerationAttemptLog // 생성 한 번의 디버그 기록
    {
        private readonly List<DungeonValidationIssue> issues; // 해당 시도의 검증 문제

        public int AttemptNumber { get; } // 1부터 시작하는 시도 번호
        public int Seed { get; } // 이 시도에 사용된 Seed
        public int GeneratedRoomCount { get; } // 생성된 방 수
        public IReadOnlyList<DungeonValidationIssue> Issues => issues; // 검증 실패 목록
        public bool IsValid => issues.Count == 0; // 시도 성공 여부

        public DungeonGenerationAttemptLog(
            int attemptNumber,
            int seed,
            int generatedRoomCount,
            IReadOnlyList<DungeonValidationIssue> validationIssues) // 시도 기록 생성자
        {
            AttemptNumber = attemptNumber; // 시도 번호 저장
            Seed = seed; // Seed 저장
            GeneratedRoomCount = generatedRoomCount; // 방 수 저장
            issues = validationIssues != null
                ? new List<DungeonValidationIssue>(validationIssues)
                : new List<DungeonValidationIssue>(); // 검증 문제 복사
        }
    }

    public sealed class DungeonGenerationRunResult // 재시도까지 포함한 최종 생성 결과
    {
        private readonly List<DungeonGenerationAttemptLog> attempts; // 모든 시도 기록

        public bool Success { get; } // 최종 성공 여부
        public GeneratedDungeon Dungeon { get; } // 성공한 던전, 실패 시 null
        public int RequestedSeed { get; } // 최초 요청 Seed
        public int SuccessfulSeed { get; } // 성공 Seed, 실패 시 마지막 시도 Seed
        public int AttemptCount => attempts.Count; // 실제 시도 횟수
        public IReadOnlyList<DungeonGenerationAttemptLog> Attempts => attempts; // 전체 시도 로그
        public DungeonValidationResult Validation { get; } // 마지막 또는 성공 시도의 검증 결과

        public DungeonGenerationRunResult(
            bool success,
            GeneratedDungeon dungeon,
            int requestedSeed,
            int successfulSeed,
            IReadOnlyList<DungeonGenerationAttemptLog> attemptLogs,
            DungeonValidationResult validation) // 최종 결과 생성자
        {
            Success = success; // 성공 여부 저장
            Dungeon = dungeon; // 성공 던전 저장
            RequestedSeed = requestedSeed; // 요청 Seed 저장
            SuccessfulSeed = successfulSeed; // 최종 Seed 저장
            attempts = attemptLogs != null
                ? new List<DungeonGenerationAttemptLog>(attemptLogs)
                : new List<DungeonGenerationAttemptLog>(); // 시도 로그 복사
            Validation = validation; // 최종 검증 결과 저장
        }
    }

    public sealed class DungeonGenerationService // 생성·검증·재시도를 묶는 상위 서비스
    {
        private readonly DungeonGenerationValidator validator; // 던전 검증기

        public DungeonGenerationService() // 기본 생성자
            : this(new DungeonGenerationValidator())
        {
        }

        public DungeonGenerationService(DungeonGenerationValidator validator) // 검증기 주입 생성자
        {
            this.validator = validator ?? throw new ArgumentNullException(nameof(validator)); // null 검증기 차단
        }

        public DungeonGenerationRunResult GenerateWithRetry(
            RoomTemplate entryTemplate,
            IReadOnlyList<RoomTemplate> roomPool,
            DungeonGenerationSettings settings,
            int requestedSeed,
            int maxAttempts = 10) // Seed를 증가시키며 유효한 던전이 나올 때까지 재시도
        {
            if (entryTemplate == null) // 시작 방 템플릿 확인
            {
                throw new ArgumentNullException(nameof(entryTemplate)); // 누락 차단
            }

            if (settings == null) // 생성 설정 확인
            {
                throw new ArgumentNullException(nameof(settings)); // 누락 차단
            }

            if (maxAttempts < 1) // 재시도 횟수 확인
            {
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "최대 생성 시도 횟수는 1 이상이어야 합니다."); // 잘못된 횟수 차단
            }

            List<DungeonGenerationAttemptLog> logs = new List<DungeonGenerationAttemptLog>(); // 전체 시도 로그
            DungeonValidationResult lastValidation = null; // 마지막 검증 결과
            int lastSeed = requestedSeed; // 마지막 Seed 초기값

            for (int attemptIndex = 0; attemptIndex < maxAttempts; attemptIndex++) // 최대 횟수만큼 생성 시도
            {
                int seed = unchecked(requestedSeed + attemptIndex); // 요청 Seed부터 1씩 증가
                lastSeed = seed; // 마지막 사용 Seed 갱신
                GeneratedDungeon dungeon = null; // 이번 생성 결과
                List<DungeonValidationIssue> exceptionIssues = null; // 생성 중 예외 기록

                try // 개별 Seed 실패가 전체 스트레스 테스트를 중단하지 않도록 보호
                {
                    dungeon = new DungeonGenerator(seed).Generate(entryTemplate, roomPool, settings); // 현재 Seed로 던전 생성
                    lastValidation = validator.Validate(dungeon, settings); // 완성 결과 전체 검증
                }
                catch (Exception exception) // 생성 중 예상하지 못한 예외
                {
                    exceptionIssues = new List<DungeonValidationIssue>
                    {
                        new DungeonValidationIssue(
                            DungeonValidationCode.GeneratorReportedFailure,
                            $"생성 중 예외가 발생했습니다: {exception.GetType().Name} - {exception.Message}")
                    }; // 예외를 재현 가능한 시도 로그로 변환
                    lastValidation = new DungeonValidationResult(exceptionIssues, -1); // 실패 검증 결과 생성
                }

                int roomCount = dungeon?.Layout?.AllRooms.Count ?? 0; // 이번 시도의 생성 방 수
                logs.Add(new DungeonGenerationAttemptLog(
                    attemptIndex + 1,
                    seed,
                    roomCount,
                    lastValidation?.Issues)); // 시도 로그 저장

                if (lastValidation != null && lastValidation.IsValid) // 모든 제약을 통과했는지 확인
                {
                    return new DungeonGenerationRunResult(
                        true,
                        dungeon,
                        requestedSeed,
                        seed,
                        logs,
                        lastValidation); // 성공 Seed와 던전 반환
                }
            }

            return new DungeonGenerationRunResult(
                false,
                null,
                requestedSeed,
                lastSeed,
                logs,
                lastValidation); // 최대 횟수 내 성공하지 못한 최종 실패 반환
        }
    }
}
