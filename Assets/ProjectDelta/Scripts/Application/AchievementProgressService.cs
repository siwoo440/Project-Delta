using System; // 정수 변환 기능 사용
using System.Collections.Generic; // 목록 자료구조 사용
using ProjectDelta.Data; // 프로필 영구 데이터 사용
using ProjectDelta.Domain; // 도전과제 정의 데이터 사용

namespace ProjectDelta.Application
{
    // 134일차: 기존 영구 기록을 읽어 100개 도전과제를 판정하고 최초 달성 ID를 프로필에 적는다.
    public static class AchievementProgressService
    {
        public static AchievementProgressSnapshot EvaluateAndRecord(
            ProfileData profile)
        {
            if (profile == null) // 프로필 누락 확인
            {
                return AchievementProgressSnapshot.Empty(); // 빈 진행도 반환
            }

            EnsureProfileCollections(profile); // 구버전 세이브의 null 컬렉션 보정

            List<string> newlyUnlockedIds = new List<string>(); // 이번 평가 신규 달성 ID 목록 초기화

            for (int index = 0; index < AchievementCatalog.All.Count; index++) // 전체 100개 도전과제 순회
            {
                AchievementDefinition definition = AchievementCatalog.All[index]; // 현재 도전과제 선택

                if (profile.PermanentRecord.UnlockedAchievementIds.Contains(definition.Id)) // 이미 영구 달성 여부 확인
                {
                    continue; // 이미 달성한 항목 재등록 방지
                }

                if (!IsConditionMet(profile, definition)) // 현재 조건 충족 여부 확인
                {
                    continue; // 미달성 항목 건너뛰기
                }

                profile.PermanentRecord.UnlockedAchievementIds.Add(definition.Id); // 최초 달성 ID 영구 목록 추가
                newlyUnlockedIds.Add(definition.Id); // Steam 동기화용 신규 달성 ID 기록
            }

            int unlockedCount = CountCatalogUnlocks(profile.PermanentRecord.UnlockedAchievementIds); // 현재 카탈로그 기준 달성 수 계산

            return new AchievementProgressSnapshot( // 로비 표시용 진행도 반환
                AchievementCatalog.All.Count, // 전체 100개 수 전달
                unlockedCount, // 현재 달성 수 전달
                newlyUnlockedIds); // 이번 신규 달성 ID 목록 전달
        }

        private static bool IsConditionMet(
            ProfileData profile,
            AchievementDefinition definition)
        {
            switch (definition.ConditionType) // 도전과제 판정 방식 분기
            {
                case AchievementConditionType.MainEnding: // 주요 엔딩 직접 판정
                    return Contains( // 주요 엔딩 영구 기록 검색
                        profile.PermanentRecord.UnlockedMainEndingIds,
                        definition.TargetId);

                case AchievementConditionType.MonsterEndingCount: // 몬스터 개별 엔딩 개수 판정
                    return Count(profile.PermanentRecord.UnlockedMonsterEndingIds) >= definition.TargetValue; // 고유 엔딩 수 비교

                case AchievementConditionType.NpcEndingCount: // NPC 개별 엔딩 개수 판정
                    return Count(profile.PermanentRecord.UnlockedNpcEndingIds) >= definition.TargetValue; // 고유 엔딩 수 비교

                case AchievementConditionType.DefeatRecordCount: // 패배 기록 개수 판정
                    return Count(profile.PermanentRecord.DefeatRecordIds) >= definition.TargetValue; // 고유 패배 수 비교

                case AchievementConditionType.LifetimeStat: // 누적 통계 판정
                    return GetLifetimeStatValue(profile.LifetimeStats, definition.TargetId) >= definition.TargetValue; // 누적값 목표 비교

                case AchievementConditionType.ActionProficiency: // 행동 숙련도 판정
                    return IsActionProficiencyMet(profile, definition); // 행동별 숙련도 확인

                default: // 알 수 없는 판정 방식
                    return false; // 안전하게 미달성 처리
            }
        }

        private static bool IsActionProficiencyMet(
            ProfileData profile,
            AchievementDefinition definition)
        {
            if (!int.TryParse(definition.TargetId, out int actionIndex)) // 행동 카탈로그 인덱스 변환
            {
                return false; // 잘못된 인덱스 미달성 처리
            }

            if (actionIndex < 0 || actionIndex >= EventBattleActionCatalog.All.Count) // 행동 범위 확인
            {
                return false; // 범위 밖 인덱스 미달성 처리
            }

            IEventBattleCommand command = EventBattleActionCatalog.All[actionIndex]; // 실제 행동 정의 선택

            if (!profile.PermanentGrowth.EventBattleActionProficiency.TryGetValue( // 행동 숙련도 영구 기록 조회
                    command.Id,
                    out EventBattleActionProficiencyRecord record))
            {
                return false; // 아직 사용 기록이 없는 행동 미달성 처리
            }

            return record != null && record.Level >= definition.TargetValue; // 숙련도 목표 레벨 비교
        }

        private static int GetLifetimeStatValue(
            LifetimeStats stats,
            string statId)
        {
            if (stats == null) // 누적 통계 객체 누락 확인
            {
                return 0; // 누적값 0 반환
            }

            switch (statId) // 통계 필드 키 분기
            {
                case "TotalPlaytimeSeconds": return (int)stats.TotalPlaytimeSeconds; // 총 플레이 시간 초 반환
                case "RunsCompleted": return stats.RunsCompleted; // 완료 런 수 반환
                case "CharacterEndingsReached": return stats.CharacterEndingsReached; // 캐릭터 엔딩 수 반환
                case "GameOvers": return stats.GameOvers; // 게임오버 수 반환
                case "NormalBattleWins": return stats.NormalBattleWins; // 일반 전투 승리 수 반환
                case "AdultBattleWins": return stats.AdultBattleWins; // 이벤트 전투 승리 수 반환
                case "RoomsDiscovered": return stats.RoomsDiscovered; // 발견 방 수 반환
                case "SecretRoomsFound": return stats.SecretRoomsFound; // 비밀방 발견 수 반환
                case "ChestsOpened": return stats.ChestsOpened; // 상자 개봉 수 반환
                case "MonstersDefeated": return stats.MonstersDefeated; // 몬스터 처치 수 반환
                case "MonstersSatisfiedAway": return stats.MonstersSatisfiedAway; // 교감 해결 수 반환
                case "TotalMemoryShardsCollected": return stats.TotalMemoryShardsCollected; // 기억의 조각 누적 수 반환
                default: return 0; // 알 수 없는 통계 키 0 반환
            }
        }

        private static int CountCatalogUnlocks(
            List<string> unlockedIds)
        {
            if (unlockedIds == null) // 영구 달성 목록 누락 확인
            {
                return 0; // 달성 수 0 반환
            }

            int count = 0; // 카탈로그 일치 달성 수 초기화

            for (int index = 0; index < AchievementCatalog.All.Count; index++) // 현재 100개 카탈로그 순회
            {
                if (unlockedIds.Contains(AchievementCatalog.All[index].Id)) // 현재 도전과제 영구 달성 여부 확인
                {
                    count++; // 카탈로그 달성 수 증가
                }
            }

            return count; // 최종 달성 수 반환
        }

        private static bool Contains(
            List<string> values,
            string value)
        {
            return values != null && values.Contains(value); // null 안전 문자열 목록 검색
        }

        private static int Count(
            List<string> values)
        {
            return values != null ? values.Count : 0; // null 안전 고유 기록 개수 반환
        }

        private static void EnsureProfileCollections(
            ProfileData profile)
        {
            if (profile.PermanentRecord == null) // 영구 기록 객체 누락 확인
            {
                profile.PermanentRecord = new PermanentRecord(); // 영구 기록 객체 복구
            }

            if (profile.PermanentGrowth == null) // 영구 성장 객체 누락 확인
            {
                profile.PermanentGrowth = new PermanentGrowth(); // 영구 성장 객체 복구
            }

            if (profile.LifetimeStats == null) // 누적 통계 객체 누락 확인
            {
                profile.LifetimeStats = new LifetimeStats(); // 누적 통계 객체 복구
            }

            if (profile.PermanentRecord.UnlockedMainEndingIds == null) // 주요 엔딩 목록 누락 확인
            {
                profile.PermanentRecord.UnlockedMainEndingIds = new List<string>(); // 주요 엔딩 목록 복구
            }

            if (profile.PermanentRecord.UnlockedMonsterEndingIds == null) // 몬스터 엔딩 목록 누락 확인
            {
                profile.PermanentRecord.UnlockedMonsterEndingIds = new List<string>(); // 몬스터 엔딩 목록 복구
            }

            if (profile.PermanentRecord.UnlockedNpcEndingIds == null) // NPC 엔딩 목록 누락 확인
            {
                profile.PermanentRecord.UnlockedNpcEndingIds = new List<string>(); // NPC 엔딩 목록 복구
            }

            if (profile.PermanentRecord.DefeatRecordIds == null) // 패배 기록 목록 누락 확인
            {
                profile.PermanentRecord.DefeatRecordIds = new List<string>(); // 패배 기록 목록 복구
            }

            if (profile.PermanentRecord.UnlockedAchievementIds == null) // 도전과제 목록 누락 확인
            {
                profile.PermanentRecord.UnlockedAchievementIds = new List<string>(); // 도전과제 목록 복구
            }

            if (profile.PermanentGrowth.EventBattleActionProficiency == null) // 행동 숙련도 사전 누락 확인
            {
                profile.PermanentGrowth.EventBattleActionProficiency = // 행동 숙련도 사전 복구
                    new Dictionary<string, EventBattleActionProficiencyRecord>();
            }
        }
    }
}
