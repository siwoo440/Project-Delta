using NUnit.Framework; // 순수 전투 검증 도구
using ProjectDelta.Core; // 이벤트 전투 상태
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class TrialEventBattleRulesTests // 주도권과 개별 진행 검증
    { // 클래스 시작
        private static TrialEventBattleEnemy[] Enemies() // 세 생존 참가자 준비
        { // 준비 시작
            return new[] { new TrialEventBattleEnemy { id = "one", health = 70, charm = 10 }, new TrialEventBattleEnemy { id = "two", slot = 1, health = 50, charm = 10 }, new TrialEventBattleEnemy { id = "three", slot = 2, health = 20, charm = 10 } }; // 서로 다른 생존 적
        } // 준비 끝
        [Test] // 참가자와 첫 주도권 검증
        public void Create_RequiresDistinctLivingEnemiesAndPlayerFirst() // 생존 적과 최초 주도권
        { // 검증 시작
            var enemies = Enemies(); // 일반 전투 생존 상태
            Assert.That(TrialEventBattleState.TryCreate(enemies, 100, 10, 101, out var state), Is.True); // 세 생존 적 진입
            Assert.That(state.Snapshot().turn, Is.EqualTo(-1)); // 플레이어 선공
            Assert.That(state.Snapshot().initialEnemies[1].slot, Is.EqualTo(1)); // 최초 적 슬롯 복사
            Assert.That(state.Snapshot().enemies[2].slot, Is.EqualTo(2)); // 현재 적 슬롯 복사
            Assert.That(TrialEventBattleState.TryRestore(state.Snapshot(), out var restored), Is.True); // 여러 적의 슬롯 복원
            Assert.That(restored.Snapshot().enemies[1].slot, Is.EqualTo(1)); // 복원된 둘째 적 슬롯
            var badSlot = state.Snapshot(); // 독립 전투 저장본
            badSlot.enemies[1].slot = 0; // 다른 슬롯으로 결과 위조
            Assert.That(TrialEventBattleState.TryRestore(badSlot, out _), Is.False); // 현재 슬롯 위조 차단
            enemies[0].health = 1; // 외부 일반 전투 값 변경
            Assert.That(state.Snapshot().enemies[0].health, Is.EqualTo(70)); // 참가자 스냅샷 보존
            enemies[1].slot = 0; // 중복 참가자 슬롯
            Assert.That(TrialEventBattleState.TryCreate(enemies, 100, 10, 101, out _), Is.False); // 중복 슬롯 차단
            enemies[1].slot = 1; // 실제 둘째 슬롯 복구
            enemies[1].id = enemies[0].id; // 중복 참가자 식별자
            Assert.That(TrialEventBattleState.TryCreate(enemies, 100, 10, 101, out _), Is.False); // 중복 참가자 차단
        } // 검증 끝
        [Test] // 양측 매력 판정과 동점 검증
        public void Turns_UseRollsAndKeepTiesForCurrentOwner() // 행동마다 매력 주도권 전환
        { // 검증 시작
            Assert.That(TrialEventBattleState.TryCreate(Enemies(), 100, 10, 100, out var initial), Is.True); // 실제 생존 적 진입
            Assert.That(initial.TryAction(0, 1, 5, 5, out var tied), Is.True); // 플레이어 동점 행동
            Assert.That(tied.Snapshot().turn, Is.EqualTo(-1)); // 동점의 플레이어 유지
            Assert.That(tied.Snapshot().enemies[0].progress, Is.EqualTo(10)); // 대상별 시험 진행
            Assert.That(tied.TryAction(0, 2, 1, 20, out var enemyTurn), Is.True); // 상대가 높은 주도권
            Assert.That(enemyTurn.Snapshot().turn, Is.EqualTo(0)); // 실제 상대 주도권
            Assert.That(enemyTurn.TryAction(1, 1, 5, 5, out _), Is.False); // 다른 상대의 불법 행동 차단
            Assert.That(enemyTurn.TryAction(0, 1, 5, 5, out var enemyTied), Is.True); // 상대 행동의 동점
            Assert.That(enemyTied.Snapshot().turn, Is.EqualTo(0)); // 동점의 상대 유지
            Assert.That(enemyTied.Snapshot().enemies[0].progress, Is.EqualTo(30)); // 상대 행동의 진행 불변
            Assert.That(TrialEventBattleState.TryRestore(enemyTied.Snapshot(), out _), Is.True); // 양측 행동 저장 복원
        } // 검증 끝
        [Test] // 전원 제거와 경험치 검증
        public void Removal_AllTargetsRequiredAndExperienceIsEightyPercent() // 체력과 이탈 및 진행 완료
        { // 검증 시작
            Assert.That(TrialEventBattleState.TryCreate(Enemies(), 100, 10, 101, out var state), Is.True); // 세 생존 적 진입
            Assert.That(state.TryDamageEnemy(0, 0, out var dead), Is.True); // 첫 상대 체력 소진
            Assert.That(dead.Snapshot().outcome, Is.Zero); // 남은 참가자의 전투 유지
            Assert.That(dead.TryRemoveEnemy(1, out var left), Is.True); // 둘째 상대 이탈
            Assert.That(left.TryRemoveEnemy(1, out _), Is.False); // 이미 이탈한 상대 차단
            Assert.That(left.TryAction(2, 3, 20, 1, out var a), Is.True); // 셋째 상대 첫 행동
            Assert.That(a.TryAction(2, 3, 20, 1, out var b), Is.True); // 셋째 상대 둘째 행동
            Assert.That(b.TryAction(2, 3, 20, 1, out var c), Is.True); // 셋째 상대 셋째 행동
            Assert.That(c.TryAction(2, 1, 20, 1, out var victory), Is.True); // 진행 백과 마지막 적 제거
            Assert.That(victory.Snapshot().outcome, Is.EqualTo(1)); // 전원 제거 승리
            Assert.That(victory.Snapshot().experience, Is.EqualTo(80)); // 백일 경험치의 팔십 퍼센트 버림
            Assert.That(victory.TryAction(2, 0, 1, 1, out _), Is.False); // 종료 후 행동 차단
            Assert.That(TrialEventBattleState.TryRestore(victory.Snapshot(), out _), Is.True); // 승리 기록 복원
        } // 검증 끝
        [Test] // 기존 처치와 생존 참가자의 경험치 합산 검증
        public void Experience_PreservesPriorKillsAndChecksOverflow() // 이전 처치 백 퍼센트와 생존 대상 팔십 퍼센트
        { // 검증 시작
            var one = new[] { new TrialEventBattleEnemy { id = "one", slot = 1, health = 20, charm = 10 } }; // 기존 처치 뒤 한 명의 생존 적
            Assert.That(TrialEventBattleState.TryCreate(one, 100, 10, 101, 40, out var single), Is.True); // 생존 적과 기존 처치 경험치
            Assert.That(single.TryRemoveEnemy(0, out var won), Is.True); // 생존 대상의 종료
            Assert.That(won.Snapshot().experience, Is.EqualTo(120)); // 팔십 퍼센트 버림과 기존 경험치 합산
            Assert.That(won.Snapshot().priorExperience, Is.EqualTo(40)); // 기존 처치 경험치 보관
            Assert.That(TrialEventBattleState.TryRestore(won.Snapshot(), out _), Is.True); // 혼합 경험치의 저장 복원
            Assert.That(TrialEventBattleState.TryCreate(Enemies(), 100, 10, 60, 50, out var group), Is.True); // 다수 참가자와 기존 처치 경험치
            Assert.That(group.TryRemoveEnemy(0, out var a), Is.True); // 첫 생존 적 종료
            Assert.That(a.TryRemoveEnemy(1, out var b), Is.True); // 둘째 생존 적 종료
            Assert.That(b.TryRemoveEnemy(2, out var c), Is.True); // 마지막 생존 적 종료
            Assert.That(c.Snapshot().experience, Is.EqualTo(98)); // 다수 생존 적 경험치와 기존 처치 합산
            Assert.That(TrialEventBattleState.TryCreate(one, 100, 10, 100, int.MaxValue, out _), Is.False); // 경험치 합산 오버플로 차단
            Assert.That(TrialEventBattleState.TryCreate(one, 100, 10, 100, -1, out _), Is.False); // 음수 기존 경험치 차단
        } // 검증 끝
        [Test] // 행동 이력의 최대 크기 검증
        public void Steps_LimitRestoreAndFurtherActions() // 과도한 재생과 무제한 기록 차단
        { // 검증 시작
            Assert.That(TrialEventBattleState.TryCreate(Enemies(), 100, 10, 100, out var state), Is.True); // 실제 전투 시작
            for (var index = 0; index < TrialEventBattleRules.MaxSteps; index++) // 허용된 기록 수만큼 관찰
            { // 행동 시작
                Assert.That(state.TryAction(0, 0, 10, 10, out var next), Is.True); // 정력 없는 동점 관찰 기록
                state = next; // 확정한 독립 상태 적용
            } // 행동 끝
            Assert.That(state.Snapshot().steps.Length, Is.EqualTo(TrialEventBattleRules.MaxSteps)); // 최대 기록 수 확인
            Assert.That(TrialEventBattleState.TryRestore(state.Snapshot(), out _), Is.True); // 최대 길이 저장 복원
            Assert.That(state.TryAction(0, 0, 10, 10, out _), Is.False); // 상한 뒤 행동 차단
            Assert.That(state.TryDamageEnemy(0, 0, out _), Is.False); // 상한 뒤 체력 변경 차단
            Assert.That(state.TryRemoveEnemy(0, out _), Is.False); // 상한 뒤 이탈 차단
            var excessive = state.Snapshot(); // 독립 저장본
            System.Array.Resize(ref excessive.steps, TrialEventBattleRules.MaxSteps + 1); // 최대 길이 초과 저장 구성
            Assert.That(TrialEventBattleState.TryRestore(excessive, out _), Is.False); // 초과 저장의 재생 차단
        } // 검증 끝
        [Test] // 정력 소진과 손상 저장 검증
        public void StaminaZero_LosesAndTamperedHistoryFails() // 즉시 패배와 재생 검증
        { // 검증 시작
            Assert.That(TrialEventBattleState.TryCreate(Enemies(), 3, 10, 100, out var initial), Is.True); // 낮은 정력 전투 생성
            Assert.That(initial.TryAction(0, 1, 20, 1, out var defeat), Is.True); // 남은 정력 소모
            var saved = defeat.Snapshot(); // 독립 패배 저장본
            Assert.That(saved.outcome, Is.EqualTo(2)); // 정력 소진 즉시 패배
            Assert.That(saved.experience, Is.Zero); // 패배 경험치 없음
            Assert.That(TrialEventBattleState.TryRestore(saved, out _), Is.True); // 정상 패배 복원
            saved.stamina = 1; // 실제 결과와 다른 정력
            Assert.That(TrialEventBattleState.TryRestore(saved, out _), Is.False); // 위조된 상태 차단
            saved = defeat.Snapshot(); // 정상 저장본 재조회
            saved.steps[0].playerRoll = 0; // 범위 밖 난수 조작
            Assert.That(TrialEventBattleState.TryRestore(saved, out _), Is.False); // 위조된 주사위 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
