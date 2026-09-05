using System.Collections.Generic; // 도전과제 목록 자료구조 사용

namespace ProjectDelta.Domain
{
    // 134일차: 기획서 7.5절 기준 Steam 도전과제 100개의 로컬 원본 카탈로그다.
    public static class AchievementCatalog
    {
        public const int ExpectedCount = 100; // 기획서 고정 총 도전과제 수

        public static readonly IReadOnlyList<AchievementDefinition> All = Build(); // 전체 100개 카탈로그 생성

        private static IReadOnlyList<AchievementDefinition> Build()
        {
            List<AchievementDefinition> definitions = new List<AchievementDefinition>(ExpectedCount); // 100개 목록 공간 준비

            AddMainEndingAchievements(definitions); // 주요 엔딩 15개 추가
            AddMonsterEndingAchievements(definitions); // 몬스터 개별 엔딩 20개 추가
            AddNpcEndingAchievements(definitions); // NPC 개별 엔딩 10개 추가
            AddDefeatAchievements(definitions); // 패배 기록 31개 추가
            AddLifetimeAchievements(definitions); // 탐험·전투·성장 12개 추가
            AddActionProficiencyAchievements(definitions); // 행동 숙련도 12개 추가

            return definitions; // 완성된 100개 목록 반환
        }

        private static void AddMainEndingAchievements(
            List<AchievementDefinition> definitions)
        {
            MainEndingId[] endings = new MainEndingId[] // 주요 엔딩 15종 고정 순서
            {
                MainEndingId.ReturnToReality, // 현실로의 귀환
                MainEndingId.HeroicReturn, // 영웅의 귀환
                MainEndingId.ReturnOfWill, // 의지의 귀환
                MainEndingId.ReturnLeavingEverythingBehind, // 모든 것을 남겨 둔 귀환
                MainEndingId.EmptyHandedReturn, // 빈손의 귀환
                MainEndingId.WoundedReturn, // 상처뿐인 귀환
                MainEndingId.CursedReturn, // 저주를 품은 귀환
                MainEndingId.CompleteExplorerReturn, // 완전한 탐험자의 귀환
                MainEndingId.KingOfTheDungeon, // 던전의 왕
                MainEndingId.KingOfConquest, // 정복의 왕
                MainEndingId.KingOfCharm, // 매혹의 왕
                MainEndingId.CursedKing, // 저주받은 왕
                MainEndingId.KingOfRecords, // 기록의 왕
                MainEndingId.MonsterHarem, // 몬스터 하렘
                MainEndingId.ServantOfTheDemonLord // 마왕의 종
            };

            for (int index = 0; index < endings.Length; index++) // 주요 엔딩 15종 순회
            {
                MainEndingId ending = endings[index]; // 현재 주요 엔딩 선택

                definitions.Add( // 주요 엔딩 도전과제 등록
                    new AchievementDefinition(
                        $"ACH_END_MAIN_{index + 1:00}", // 안정적인 도전과제 ID 생성
                        MainEndingRules.GetDisplayName(ending), // 기존 엔딩 표시 이름 재사용
                        AchievementCategory.Ending, // 엔딩 카테고리 지정
                        AchievementConditionType.MainEnding, // 엔딩 ID 직접 판정 지정
                        ending.ToString(), // PermanentRecord 저장 키와 동일한 문자열 사용
                        1, // 1회 획득 조건
                        true)); // 숨김 도전과제로 지정
            }
        }

        private static void AddMonsterEndingAchievements(
            List<AchievementDefinition> definitions)
        {
            // 실제 20종 콘텐츠 ID가 모두 확정되기 전까지는 고유 몬스터 엔딩 개수를 슬롯으로 센다.
            for (int index = 1; index <= 20; index++) // 몬스터 개별 엔딩 20칸 생성
            {
                definitions.Add( // 몬스터 엔딩 도전과제 등록
                    new AchievementDefinition(
                        $"ACH_END_MONSTER_{index:00}", // 몬스터 엔딩 슬롯 ID 생성
                        $"몬스터 개별 엔딩 {index:00}", // 임시 표시 이름 생성
                        AchievementCategory.Ending, // 엔딩 카테고리 지정
                        AchievementConditionType.MonsterEndingCount, // 고유 엔딩 개수 판정 지정
                        string.Empty, // 실제 몬스터 ID 확정 전 대상 ID 미사용
                        index, // 누적 고유 엔딩 필요 개수
                        true)); // 숨김 도전과제로 지정
            }
        }

        private static void AddNpcEndingAchievements(
            List<AchievementDefinition> definitions)
        {
            // 현재 NPC는 테스트 역할 4개뿐이므로 정식 10명 ID가 확정되기 전까지 고유 엔딩 개수를 슬롯으로 센다.
            for (int index = 1; index <= 10; index++) // NPC 개별 엔딩 10칸 생성
            {
                definitions.Add( // NPC 엔딩 도전과제 등록
                    new AchievementDefinition(
                        $"ACH_END_NPC_{index:00}", // NPC 엔딩 슬롯 ID 생성
                        $"NPC 개별 엔딩 {index:00}", // 임시 표시 이름 생성
                        AchievementCategory.Ending, // 엔딩 카테고리 지정
                        AchievementConditionType.NpcEndingCount, // 고유 엔딩 개수 판정 지정
                        string.Empty, // 실제 NPC ID 확정 전 대상 ID 미사용
                        index, // 누적 고유 엔딩 필요 개수
                        true)); // 숨김 도전과제로 지정
            }
        }

        private static void AddDefeatAchievements(
            List<AchievementDefinition> definitions)
        {
            // 패배 기록도 31종의 정식 대상 매핑이 완성되기 전까지 PermanentRecord의 고유 기록 개수와 1:1 대응한다.
            for (int index = 1; index <= 31; index++) // 패배 기록 31칸 생성
            {
                definitions.Add( // 패배 기록 도전과제 등록
                    new AchievementDefinition(
                        $"ACH_DEFEAT_{index:00}", // 패배 기록 슬롯 ID 생성
                        $"패배 기록 {index:00}", // 임시 표시 이름 생성
                        AchievementCategory.Defeat, // 패배 카테고리 지정
                        AchievementConditionType.DefeatRecordCount, // 고유 패배 개수 판정 지정
                        string.Empty, // 정식 상대 ID 매핑 전 대상 ID 미사용
                        index, // 누적 고유 패배 기록 필요 개수
                        true)); // 숨김 도전과제로 지정
            }
        }

        private static void AddLifetimeAchievements(
            List<AchievementDefinition> definitions)
        {
            // 기획서에 12개 개별 임계값이 없어서 134일차 임시값으로 둔다. 콘텐츠 밸런싱 때 숫자만 교체한다.
            AddLifetime(definitions, "ACH_LIFE_PLAYTIME_1H", "한 시간의 탐험", "TotalPlaytimeSeconds", 3600); // 총 플레이 1시간
            AddLifetime(definitions, "ACH_LIFE_RUNS_10", "열 번의 여정", "RunsCompleted", 10); // 완료 런 10회
            AddLifetime(definitions, "ACH_LIFE_CHARACTER_ENDINGS_5", "다섯 개의 인연", "CharacterEndingsReached", 5); // 캐릭터 엔딩 5회
            AddLifetime(definitions, "ACH_LIFE_GAMEOVERS_10", "다시 일어서는 자", "GameOvers", 10); // 게임오버 10회
            AddLifetime(definitions, "ACH_LIFE_NORMAL_WINS_50", "전투의 숙련자", "NormalBattleWins", 50); // 일반 전투 승리 50회
            AddLifetime(definitions, "ACH_LIFE_ADULT_WINS_20", "교감의 숙련자", "AdultBattleWins", 20); // 이벤트 전투 승리 20회
            AddLifetime(definitions, "ACH_LIFE_ROOMS_100", "지도 제작자", "RoomsDiscovered", 100); // 방 발견 100개
            AddLifetime(definitions, "ACH_LIFE_SECRET_ROOMS_10", "숨겨진 길", "SecretRoomsFound", 10); // 비밀방 발견 10개
            AddLifetime(definitions, "ACH_LIFE_CHESTS_30", "보물 사냥", "ChestsOpened", 30); // 상자 개봉 30개
            AddLifetime(definitions, "ACH_LIFE_MONSTERS_100", "백전의 기록", "MonstersDefeated", 100); // 몬스터 처치 100마리
            AddLifetime(definitions, "ACH_LIFE_SATISFIED_20", "싸우지 않는 승리", "MonstersSatisfiedAway", 20); // 교감 해결 20회
            AddLifetime(definitions, "ACH_LIFE_SHARDS_100", "기억의 수집가", "TotalMemoryShardsCollected", 100); // 기억의 조각 누적 100개
        }

        private static void AddLifetime(
            List<AchievementDefinition> definitions,
            string id,
            string displayName,
            string statId,
            int targetValue)
        {
            definitions.Add( // 누적 통계 도전과제 등록
                new AchievementDefinition(
                    id, // 고정 도전과제 ID 사용
                    displayName, // 공개 표시 이름 사용
                    AchievementCategory.Lifetime, // 누적 통계 카테고리 지정
                    AchievementConditionType.LifetimeStat, // LifetimeStats 판정 지정
                    statId, // 조회할 통계 필드 키 지정
                    targetValue, // 임시 목표값 지정
                    false)); // 공개 도전과제로 지정
        }

        private static void AddActionProficiencyAchievements(
            List<AchievementDefinition> definitions)
        {
            // EventBattleActionCatalog.All의 0~11 순서를 그대로 써서 실제 행동 ID와 런타임에 연결한다.
            for (int index = 0; index < 12; index++) // 공통 행동 12종 순회
            {
                definitions.Add( // 행동 숙련도 도전과제 등록
                    new AchievementDefinition(
                        $"ACH_ACTION_{index + 1:00}_LV5", // 행동 슬롯별 고정 도전과제 ID 생성
                        $"행동 숙련도 {index + 1:00}", // 정식 이름 확정 전 공개 임시 이름 생성
                        AchievementCategory.ActionProficiency, // 행동 숙련도 카테고리 지정
                        AchievementConditionType.ActionProficiency, // 숙련도 판정 지정
                        index.ToString(), // EventBattleActionCatalog 인덱스 저장
                        5, // 임시 숙련도 목표 Lv.5
                        false)); // 공개 도전과제로 지정
            }
        }
    }
}
