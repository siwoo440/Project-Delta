using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 조우 종족 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class GroupEncounterSpeciesRulesTests // 다수 조우 종족 검증
    { // 검증 클래스 시작
        [Test] // 신규 회차 종족 검증
        public void NewRun_UsesGoblin() // 신규 회차 고블린 확인
        { // 검증 시작
            Assert.That(GroupEncounterSpeciesRules.ForNewRun(), Is.True); // 신규 회차 고블린 선택
        } // 검증 끝
        [TestCase(1, false)] // 첫 저장의 슬라임 선택
        [TestCase(8, false)] // 이전 마지막 저장 버전
        [TestCase(8, true)] // 이전 저장의 값 무시
        public void OldSave_AlwaysUsesSlime(int version, bool storedGoblin) // 이전 저장 호환성 확인
        { // 검증 시작
            Assert.That(GroupEncounterSpeciesRules.FromSave(version, storedGoblin), Is.False); // 이전 저장 슬라임 유지
        } // 검증 끝
        [TestCase(9, false)] // 새 저장의 슬라임 복원
        [TestCase(9, true)] // 새 저장의 고블린 복원
        [TestCase(10, false)] // 이후 저장의 슬라임 복원
        [TestCase(10, true)] // 이후 저장의 고블린 복원
        public void NewSave_RestoresStoredSpecies(int version, bool storedGoblin) // 저장된 종족 복원 확인
        { // 검증 시작
            Assert.That(GroupEncounterSpeciesRules.FromSave(version, storedGoblin), Is.EqualTo(storedGoblin)); // 저장 종족 유지
        } // 검증 끝
    } // 검증 클래스 끝
} // 이름 공간 끝
