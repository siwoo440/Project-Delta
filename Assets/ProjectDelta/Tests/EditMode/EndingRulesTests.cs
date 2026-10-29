using System.Linq; // 후보의 실제 포함 여부
using NUnit.Framework; // 결말 규칙 검증 도구
using ProjectDelta.Core; // 독립 결말 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class EndingRulesTests // 시험 후보와 기획 보상 검증
    { // 클래스 시작
        [Test] // 동시 결말 후보 검증
        public void Victory_ExposesEveryEligibleChoiceWithoutPriority() // 실제 두 방향의 모든 후보
        { // 검증 시작
            Assert.That(EndingRules.TryCandidates(1, 20, 100, 0, 0, out var result), Is.True); // 빈손과 저체력의 일반 승리
            Assert.That(result.ReturnCandidates.Contains(EndingRules.ReturnBasic), Is.True); // 기본 현실 귀환 후보
            Assert.That(result.ReturnCandidates.Contains(EndingRules.ReturnHeroic), Is.True); // 영웅 귀환 후보
            Assert.That(result.ReturnCandidates.Contains(EndingRules.ReturnEmptyHanded), Is.True); // 빈손 귀환 후보
            Assert.That(result.ReturnCandidates.Contains(EndingRules.ReturnWounded), Is.True); // 체력 이십 퍼센트 후보
            Assert.That(result.ReturnCandidates.Count, Is.EqualTo(4)); // 우선순위 없이 전체 귀환 후보
            Assert.That(result.StayCandidates.Contains(EndingRules.StayBasic), Is.True); // 기본 던전 잔류 후보
            Assert.That(result.StayCandidates.Contains(EndingRules.StayConquest), Is.True); // 정복의 왕 후보
            Assert.That(result.StayCandidates.Count, Is.EqualTo(2)); // 우선순위 없는 잔류 후보
            Assert.That(result.AutomaticCandidate, Is.Null); // 승리의 자동 확정 없음
        } // 검증 끝
        [Test] // 장비와 체력 조건 경계 검증
        public void Victory_UsesEquipmentRelicsAndExactHealthThreshold() // 빈손과 체력 조건 확인
        { // 검증 시작
            Assert.That(EndingRules.TryCandidates(1, 21, 100, 1, 0, out var equipped), Is.True); // 장비를 착용한 일반 승리
            Assert.That(equipped.ReturnCandidates.Contains(EndingRules.ReturnEmptyHanded), Is.False); // 장비 보유의 빈손 차단
            Assert.That(equipped.ReturnCandidates.Contains(EndingRules.ReturnWounded), Is.False); // 체력 이십 퍼센트 초과
            Assert.That(EndingRules.TryCandidates(1, 20, 100, 0, 1, out var relic), Is.True); // 유물을 보유한 저체력
            Assert.That(relic.ReturnCandidates.Contains(EndingRules.ReturnEmptyHanded), Is.False); // 유물 보유의 빈손 차단
            Assert.That(relic.ReturnCandidates.Contains(EndingRules.ReturnWounded), Is.True); // 정확히 이십 퍼센트 허용
            Assert.That(EndingRules.TryCandidates(1, int.MaxValue, int.MaxValue, 0, 0, out var full), Is.True); // 최대 정수의 안전한 비율
            Assert.That(full.ReturnCandidates.Contains(EndingRules.ReturnWounded), Is.False); // 정수 넘침 없는 조건
        } // 검증 끝
        [Test] // 패배와 항복의 공통 결과 검증
        public void DefeatAndSurrender_ExposeOnlyDemonServant() // 선택 없는 공통 결말
        { // 검증 시작
            Assert.That(EndingRules.TryCandidates(2, 0, 100, 0, 0, out var defeated), Is.True); // 체력 소진 패배
            Assert.That(defeated.AutomaticCandidate, Is.EqualTo(EndingRules.DemonServant)); // 패배의 마왕의 종
            Assert.That(defeated.ReturnCandidates.Count, Is.Zero); // 패배 시 귀환 선택 없음
            Assert.That(defeated.StayCandidates.Count, Is.Zero); // 패배 시 잔류 선택 없음
            Assert.That(EndingRules.TryCandidates(3, 25, 100, 0, 0, out var surrendered), Is.True); // 실제 항복 결과
            Assert.That(surrendered.AutomaticCandidate, Is.EqualTo(EndingRules.DemonServant)); // 항복의 같은 결말
        } // 검증 끝
        [Test] // 기획 보상과 알 수 없는 보상 검증
        public void Shards_UseConfirmedFirstUnlockValuesAndRejectUnknown() // 실제 확인된 최초 보상
        { // 검증 시작
            Assert.That(EndingRules.TryFirstUnlockShards(EndingRules.ReturnBasic, out var basic), Is.True); // 기본 귀환 보상
            Assert.That(basic, Is.EqualTo(5)); // 기본 귀환 파편
            Assert.That(EndingRules.TryFirstUnlockShards(EndingRules.StayBasic, out var stay), Is.True); // 기본 잔류 보상
            Assert.That(stay, Is.EqualTo(5)); // 기본 잔류 파편
            Assert.That(EndingRules.TryFirstUnlockShards(EndingRules.DemonServant, out var servant), Is.True); // 마왕의 종 보상
            Assert.That(servant, Is.EqualTo(5)); // 마왕의 종 파편
            foreach (var id in new[] { EndingRules.ReturnHeroic, EndingRules.StayConquest, EndingRules.ReturnEmptyHanded, EndingRules.ReturnWounded }) // 십 파편 후보 순회
            { // 보상 검사 시작
                Assert.That(EndingRules.TryFirstUnlockShards(id, out var shards), Is.True); // 실제 기획 보상 존재
                Assert.That(shards, Is.EqualTo(10)); // 확인된 최초 십 파편
            } // 보상 검사 끝
            Assert.That(EndingRules.TryFirstUnlockShards("unknown", out _), Is.False); // 미확인 보상의 임의 확정 차단
        } // 검증 끝
        [Test] // 확인된 결말의 실제 표시명 검증
        public void DisplayName_MapsKnownCandidatesAndRejectsUnknown() // 후보별 한글 표시 이름
        { // 검증 시작
            Assert.That(EndingRules.DisplayName(EndingRules.ReturnBasic), Is.EqualTo("현실로의 귀환")); // 기본 귀환 표시
            Assert.That(EndingRules.DisplayName(EndingRules.ReturnHeroic), Is.EqualTo("영웅의 귀환")); // 영웅 귀환 표시
            Assert.That(EndingRules.DisplayName(EndingRules.ReturnEmptyHanded), Is.EqualTo("빈손의 귀환")); // 빈손 귀환 표시
            Assert.That(EndingRules.DisplayName(EndingRules.ReturnWounded), Is.EqualTo("상처뿐인 귀환")); // 저체력 귀환 표시
            Assert.That(EndingRules.DisplayName(EndingRules.StayBasic), Is.EqualTo("던전의 왕")); // 기본 잔류 표시
            Assert.That(EndingRules.DisplayName(EndingRules.StayConquest), Is.EqualTo("정복의 왕")); // 정복 잔류 표시
            Assert.That(EndingRules.DisplayName(EndingRules.DemonServant), Is.EqualTo("마왕의 종")); // 패배 결과 표시
            Assert.That(EndingRules.DisplayName("unknown"), Is.Null); // 미등록 결말 표시 차단
        } // 검증 끝
        [Test] // 손상된 결과와 자원 검증
        public void Candidates_RejectInvalidOutcomeAndResources() // 모순된 승패와 장비 차단
        { // 검증 시작
            Assert.That(EndingRules.TryCandidates(0, 100, 100, 0, 0, out _), Is.False); // 미완료 마왕전 차단
            Assert.That(EndingRules.TryCandidates(1, 0, 100, 0, 0, out _), Is.False); // 체력 소진 승리 차단
            Assert.That(EndingRules.TryCandidates(2, 1, 100, 0, 0, out _), Is.False); // 생존 상태의 체력 패배 차단
            Assert.That(EndingRules.TryCandidates(3, 0, 100, 0, 0, out _), Is.False); // 체력 소진 항복 차단
            Assert.That(EndingRules.TryCandidates(1, 101, 100, 0, 0, out _), Is.False); // 최대 체력 초과 차단
            Assert.That(EndingRules.TryCandidates(1, 10, 100, 5, 0, out _), Is.False); // 장비 부위 초과 차단
            Assert.That(EndingRules.TryCandidates(1, 10, 100, 0, -1, out _), Is.False); // 음수 유물 수 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
