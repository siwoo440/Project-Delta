using System; // 상태 형식 조회
using System.Reflection; // 구현 전 메서드 조회
using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 게임 진행 상태
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class EncounterFlowTests // 조우 화면 상태와 실패 복구 검증
    { // 클래스 시작
        [Test] // 조우와 전투 상태 검증
        public void Encounter_RequiresExplorationAndCombatChoice() // 조우 진입과 전투 선택 확인
        { // 검증 시작
            var encounter = typeof(GamePhase).GetField("Encounter"); // 조우 화면 상태 조회
            var begin = typeof(GameFlowState).GetMethod("TryStartEncounter"); // 조우 시작 조회
            var cancel = typeof(GameFlowState).GetMethod("TryCancelEncounter"); // 실패한 조우 복구 조회
            var revert = typeof(GameFlowState).GetMethod("TryRevertCombatToEncounter"); // 실패한 전투 복구 조회
            Assert.That(encounter, Is.Not.Null); // 조우 상태 제공 확인
            Assert.That(begin, Is.Not.Null); // 조우 시작 제공 확인
            Assert.That(cancel, Is.Not.Null); // 조우 실패 복구 제공 확인
            Assert.That(revert, Is.Not.Null); // 전투 실패 복구 제공 확인
            var flow = new GameFlowState(); // 새 게임 흐름
            Assert.That((bool)begin.Invoke(flow, null), Is.False); // 초기 상태의 조우 차단
            Assert.That(flow.TryBeginTransition(GamePhase.Title), Is.True); // 타이틀 전환 시작
            flow.CompleteTransition(); // 타이틀 전환 완료
            Assert.That(flow.TryBeginTransition(GamePhase.Exploration), Is.True); // 탐험 전환 시작
            flow.CompleteTransition(); // 탐험 전환 완료
            Assert.That((bool)begin.Invoke(flow, null), Is.True); // 탐험 중 조우 시작
            Assert.That(flow.Phase, Is.EqualTo(encounter.GetValue(null))); // 조우 화면 상태 확인
            Assert.That((bool)begin.Invoke(flow, null), Is.False); // 중복 조우 차단
            Assert.That(flow.TryOpenInventory(), Is.False); // 조우 중 인벤토리 차단
            Assert.That(flow.TryStartCombat(), Is.True); // 조우에서 전투 선택
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Combat)); // 전투 상태 확인
            Assert.That((bool)revert.Invoke(flow, null), Is.True); // 전투 화면 생성 실패 복구
            Assert.That(flow.Phase, Is.EqualTo(encounter.GetValue(null))); // 기존 조우 화면 복구
            Assert.That(flow.TryStartCombat(), Is.True); // 복구 후 전투 선택 재시도
            Assert.That(flow.TryEndCombat(), Is.True); // 실제 전투 종료
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 탐험 상태 복구
            Assert.That((bool)begin.Invoke(flow, null), Is.True); // 새 조우 진입
            Assert.That((bool)cancel.Invoke(flow, null), Is.True); // 화면 생성 실패 복구
            Assert.That(flow.Phase, Is.EqualTo(GamePhase.Exploration)); // 조우 실패 시 탐험 복구
            Assert.That((bool)cancel.Invoke(flow, null), Is.False); // 중복 복구 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
