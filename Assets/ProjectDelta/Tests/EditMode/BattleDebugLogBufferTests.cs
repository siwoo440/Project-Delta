using NUnit.Framework; // NUnit 테스트 기능
using ProjectDelta.Presentation; // 전투 로그 버퍼 접근

namespace ProjectDelta.Tests.EditMode // EditMode 테스트 영역
{
    public sealed class BattleDebugLogBufferTests // 전투 로그 버퍼 테스트
    {
        [Test] // 단일 행동 기록 테스트
        public void TryAppendAction_FirstAction_AddsRoundAndAction() // 첫 행동 기록 검증
        {
            BattleDebugLogBuffer buffer = new BattleDebugLogBuffer(); // 테스트 버퍼 생성
            buffer.BeginBattle(); // 새 전투 시작

            bool appended = buffer.TryAppendAction(1, 1, "PLAYER", "Attack", new[] { "공격 적중 / PLAYER → ENEMY / 10 데미지" }); // 첫 행동 추가

            Assert.That(appended, Is.True); // 추가 성공 확인
            Assert.That(buffer.Lines, Does.Contain("--- Round 1 ---")); // 라운드 기록 확인
            Assert.That(buffer.Lines, Does.Contain("[R1] [PLAYER] [Attack] 공격 적중 / PLAYER → ENEMY / 10 데미지")); // 행동 기록 확인
        }

        [Test] // 중복 행동 방지 테스트
        public void TryAppendAction_SameSequence_DoesNotDuplicate() // 같은 시퀀스 중복 방지 검증
        {
            BattleDebugLogBuffer buffer = new BattleDebugLogBuffer(); // 테스트 버퍼 생성
            buffer.BeginBattle(); // 새 전투 시작
            buffer.TryAppendAction(3, 2, "PLAYER", "Defend", new[] { "방어" }); // 첫 기록 추가

            bool appended = buffer.TryAppendAction(3, 2, "PLAYER", "Defend", new[] { "방어" }); // 같은 기록 재추가

            Assert.That(appended, Is.False); // 중복 거부 확인
            Assert.That(buffer.Count, Is.EqualTo(3)); // 시작선·라운드·행동만 유지 확인
        }

        [Test] // 새 라운드 기록 테스트
        public void TryAppendAction_NewRound_AddsNewRoundHeader() // 라운드 변경 기록 검증
        {
            BattleDebugLogBuffer buffer = new BattleDebugLogBuffer(); // 테스트 버퍼 생성
            buffer.BeginBattle(); // 새 전투 시작
            buffer.TryAppendAction(1, 1, "PLAYER", "Attack", new[] { "첫 행동" }); // 1라운드 행동 추가

            buffer.TryAppendAction(2, 2, "ENEMY", "Attack", new[] { "둘째 행동" }); // 2라운드 행동 추가

            Assert.That(buffer.Lines, Does.Contain("--- Round 2 ---")); // 새 라운드 표기 확인
        }

        [Test] // 최대 로그 수 테스트
        public void TryAppendAction_OverCapacity_RemovesOldestLines() // 오래된 로그 제거 검증
        {
            BattleDebugLogBuffer buffer = new BattleDebugLogBuffer(4); // 작은 최대치 버퍼 생성
            buffer.BeginBattle(); // 새 전투 시작
            buffer.TryAppendAction(1, 1, "PLAYER", "Attack", new[] { "A" }); // 첫 행동 추가
            buffer.TryAppendAction(2, 1, "ENEMY", "Attack", new[] { "B" }); // 둘째 행동 추가
            buffer.TryAppendAction(3, 1, "PLAYER", "Attack", new[] { "C" }); // 셋째 행동 추가

            Assert.That(buffer.Count, Is.EqualTo(4)); // 최대 네 줄 유지 확인
            Assert.That(buffer.Lines[0], Is.EqualTo("--- Round 1 ---")); // 가장 오래된 시작선 제거 확인
        }


        [Test] // 시작 직후 행동 보존 테스트
        public void BeginBattle_CurrentSequenceBelongsToBattle_CapturesFirstAction() // 같은 프레임 첫 행동 보존 검증
        {
            BattleDebugLogBuffer buffer = new BattleDebugLogBuffer(); // 테스트 버퍼 생성
            buffer.BeginBattle(7, true); // 새 전투 첫 행동 존재 상태 시작

            bool appended = buffer.TryAppendAction(7, 1, "ENEMY", "Attack", new[] { "첫 적 행동" }); // 시작 프레임 행동 추가

            Assert.That(appended, Is.True); // 첫 행동 기록 확인
        }

        [Test] // 이전 전투 잔여 행동 무시 테스트
        public void BeginBattle_CurrentSequenceIsStale_IgnoresPreviousAction() // 이전 행동 잔여 결과 차단 검증
        {
            BattleDebugLogBuffer buffer = new BattleDebugLogBuffer(); // 테스트 버퍼 생성
            buffer.BeginBattle(7, false); // 이전 행동 잔여 상태 시작

            bool appended = buffer.TryAppendAction(7, 1, "PLAYER", "Attack", new[] { "이전 행동" }); // 이전 시퀀스 재입력

            Assert.That(appended, Is.False); // 이전 행동 기록 차단 확인
        }

        [Test] // 새 전투 초기화 테스트
        public void BeginBattle_AfterExistingLogs_ClearsPreviousBattle() // 이전 전투 로그 제거 검증
        {
            BattleDebugLogBuffer buffer = new BattleDebugLogBuffer(); // 테스트 버퍼 생성
            buffer.BeginBattle(); // 첫 전투 시작
            buffer.TryAppendAction(1, 1, "PLAYER", "Attack", new[] { "이전 로그" }); // 이전 전투 기록 추가

            buffer.BeginBattle(); // 다음 전투 시작

            Assert.That(buffer.Count, Is.EqualTo(1)); // 시작선 하나만 유지 확인
            Assert.That(buffer.Lines[0], Is.EqualTo("=== Battle Start ===")); // 새 전투 시작선 확인
        }
    }
}
