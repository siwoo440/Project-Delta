using System; // 고유 거래와 배열 구성
using NUnit.Framework; // 상자 규칙 검증 도구
using ProjectDelta.Core; // 독립 상자와 미믹 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class ChestRulesTests // 상자 판정과 저장 상태 검증
    { // 클래스 시작
        private static ChestRewardCandidateData[] Rewards(int count) // 확정된 시험 보상 후보 생성
        { // 후보 생성 시작
            var result = new ChestRewardCandidateData[count]; // 고유 후보 목록
            for (var index = 0; index < count; index++) // 후보 수 순회
            { // 후보 구성 시작
                result[index] = new ChestRewardCandidateData { candidateId = "reward-" + index, itemId = "item-" + index, quantity = 1 }; // 고유 시험 보상
            } // 후보 구성 끝
            return result; // 완성된 독립 후보 목록
        } // 후보 생성 끝
        [Test] // 상자 등급별 기획 값 검증
        public void Grade_ProvidesMimicRateExperienceAndRewardRanges() // 실제 상자 등급별 수치 확인
        { // 검증 시작
            Assert.That(ChestRules.MimicChance(ChestGrade.Common), Is.EqualTo(8)); // 일반 상자의 미믹 확률
            Assert.That(ChestRules.MimicChance(ChestGrade.Advanced), Is.EqualTo(12)); // 고급 상자의 미믹 확률
            Assert.That(ChestRules.MimicChance(ChestGrade.Rare), Is.EqualTo(18)); // 희귀 상자의 미믹 확률
            Assert.That(ChestRules.NormalExperience(ChestGrade.Common), Is.EqualTo(10)); // 일반 상자의 경험치
            Assert.That(ChestRules.NormalExperience(ChestGrade.Advanced), Is.EqualTo(20)); // 고급 상자의 경험치
            Assert.That(ChestRules.NormalExperience(ChestGrade.Rare), Is.EqualTo(40)); // 희귀 상자의 경험치
            Assert.That(ChestRules.TryRewardCount(ChestGrade.Common, 1, false, false, out var normal), Is.True); // 일반 상자의 최소 후보
            Assert.That(normal, Is.EqualTo(1)); // 일반 상자 기본 수량
            Assert.That(ChestRules.TryRewardCount(ChestGrade.Advanced, 3, true, false, out var locked), Is.True); // 잠긴 고급 상자의 보상
            Assert.That(locked, Is.EqualTo(4)); // 잠긴 상자의 추가 한 개
            Assert.That(ChestRules.TryRewardCount(ChestGrade.Rare, 4, true, true, out var mimic), Is.True); // 잠긴 희귀 미믹의 보상
            Assert.That(mimic, Is.EqualTo(6)); // 잠금과 미믹의 각 추가 한 개
            Assert.That(ChestRules.TryRewardCount(ChestGrade.Rare, 2, false, false, out _), Is.False); // 등급 밖 기본 수량 차단
        } // 검증 끝
        [Test] // 잠금과 실패 후 열쇠 사용 검증
        public void LockedChest_FailedForceCannotRepeatAndKeyStillUnlocks() // 단일 강제 시도와 열쇠 복구
        { // 검증 시작
            Assert.That(ChestState.TryCreate("chest-1", ChestGrade.Common, true, out var chest), Is.True); // 잠긴 일반 상자 생성
            Assert.That(chest.TryForce(10, 20, 1, 20, out var failed, out var success), Is.True); // 실패하는 강제 판정
            Assert.That(success, Is.False); // 실제 강제 개방 실패
            Assert.That(failed.Snapshot().forceAttempted, Is.True); // 사용한 강제 시도 저장
            Assert.That(failed.TryForce(100, 1, 20, 1, out _, out _), Is.False); // 실패 후 재시도 차단
            Assert.That(ChestState.TryRestore(failed.Snapshot(), out var restored), Is.True); // 실패한 시도의 저장 복원
            Assert.That(restored.TryForce(100, 1, 20, 1, out _, out _), Is.False); // 재시작 후 강제 재시도 차단
            Assert.That(failed.TryUnlockWithKey(0, out _, out _), Is.False); // 열쇠 없는 개방 차단
            Assert.That(failed.TryUnlockWithKey(1, out var unlocked, out var keys), Is.True); // 실패 후 열쇠 개방
            Assert.That(keys, Is.Zero); // 열쇠 한 개 차감
            Assert.That(unlocked.Snapshot().unlocked, Is.True); // 열쇠 개방 상태 확인
            Assert.That(chest.Snapshot().forceAttempted, Is.False); // 최초 상자의 불변성 확인
        } // 검증 끝
        [Test] // 강제 개방의 동점과 입력 범위 검증
        public void Force_TiesSucceedAndInvalidDiceDoNotConsumeAttempt() // 동점 성공과 잘못된 판정 차단
        { // 검증 시작
            Assert.That(ChestState.TryCreate("chest-2", ChestGrade.Advanced, true, out var chest), Is.True); // 잠긴 고급 상자 생성
            Assert.That(chest.TryForce(10, 10, 0, 1, out _, out _), Is.False); // 범위 밖 주사위 차단
            Assert.That(chest.Snapshot().forceAttempted, Is.False); // 잘못된 판정의 시도 보존
            Assert.That(chest.TryForce(10, 10, 5, 5, out var opened, out var success), Is.True); // 같은 수치의 강제 시도
            Assert.That(success, Is.True); // 동점 성공 확인
            Assert.That(opened.Snapshot().unlocked, Is.True); // 동점 후 잠금 해제
        } // 검증 끝
        [Test] // 확정한 일반 상자와 중복 보상 검증
        public void OrdinaryChest_RestoresFixedCandidatesAndCompletesOnlyOnce() // 후보와 거래 식별자의 재개 보존
        { // 검증 시작
            Assert.That(ChestState.TryCreate("chest-3", ChestGrade.Common, false, out var chest), Is.True); // 잠금 없는 일반 상자
            var transaction = Guid.NewGuid().ToString("N"); // 고유 보상 거래 식별자
            var rewards = Rewards(1); // 확정할 실제 보상 후보
            Assert.That(chest.TryReveal(8, 1, transaction, rewards, out var revealed), Is.True); // 경계값의 일반 상자 확정
            rewards[0].itemId = "changed"; // 외부 후보 원본 변경
            var snapshot = revealed.Snapshot(); // 저장 가능한 확정 상태
            Assert.That(snapshot.mimicState, Is.EqualTo(1)); // 미믹이 아닌 결과 확인
            Assert.That(snapshot.experience, Is.EqualTo(10)); // 일반 상자 경험치 확인
            Assert.That(snapshot.rewards[0].itemId, Is.EqualTo("item-0")); // 저장 후보의 독립성 확인
            Assert.That(ChestState.TryRestore(snapshot, out var restored), Is.True); // 중단 이후 상태 복원
            Assert.That(restored.TryReveal(0, 2, transaction, Rewards(3), out _), Is.False); // 이미 확정된 결과 재추첨 차단
            Assert.That(restored.TryComplete(out var complete), Is.True); // 보상 지급 후 완료 표시 준비
            Assert.That(complete.TryComplete(out _), Is.False); // 완료 거래의 중복 지급 차단
        } // 검증 끝
        [Test] // 미믹 승리 전 보상과 전투 경험치 검증
        public void Mimic_RequiresVictoryAndUsesBattleExperience() // 미믹 승리 뒤 고정 보상 처리
        { // 검증 시작
            Assert.That(ChestState.TryCreate("chest-4", ChestGrade.Common, false, out var chest), Is.True); // 일반 상자 생성
            var transaction = Guid.NewGuid().ToString("N"); // 고유 거래 식별자
            Assert.That(chest.TryReveal(7, 1, transaction, Rewards(2), out var mimic), Is.True); // 미믹 확률 경계 안쪽
            Assert.That(mimic.Snapshot().mimicState, Is.EqualTo(2)); // 미믹 결과 확인
            Assert.That(mimic.TryComplete(out _), Is.False); // 미믹 승리 전 지급 차단
            Assert.That(mimic.TryWinMimic(60, out var victory), Is.True); // 실제 전투 경험치 전달
            Assert.That(victory.Snapshot().experience, Is.EqualTo(60)); // 상자 경험치 대신 전투 경험치
            Assert.That(victory.TryWinMimic(60, out _), Is.False); // 미믹 승리 중복 확정 차단
            Assert.That(victory.TryComplete(out _), Is.True); // 승리 후 보상 확정 허용
        } // 검증 끝
        [Test] // 보상 후보의 개별 장비 심층 복사 검증
        public void RewardEquipment_PreservesIdenticalInstanceAndTraits() // 상자 후보의 실제 장비 보존
        { // 검증 시작
            Assert.That(ChestState.TryCreate("chest-gear", ChestGrade.Common, false, out var chest), Is.True); // 일반 상자 생성
            var item = TrialEquipmentTraits.Create(EquipmentCatalog.BasicSwordId, EquipmentVariantRules.Rare, true); // 공개된 개별 저주 장비
            var rewards = new[] { new ChestRewardCandidateData { candidateId = "gear", itemId = item.definitionId, quantity = 1, equipment = item } }; // 한 개의 확정 장비 후보
            Assert.That(chest.TryReveal(99, 1, Guid.NewGuid().ToString("N"), rewards, out var revealed), Is.True); // 장비 후보 확정
            item.options[0].value = 9; // 외부 장비 객체의 변경
            Assert.That(revealed.Snapshot().rewards[0].equipment.options[0].value, Is.EqualTo(2)); // 확정한 장비 옵션 보존
            var snapshot = revealed.Snapshot(); // 독립 저장본
            Assert.That(ChestState.TryRestore(snapshot, out var restored), Is.True); // 실제 후보 재개
            Assert.That(restored.Snapshot().rewards[0].equipment.instanceId, Is.EqualTo(item.instanceId)); // 재추첨 없는 인스턴스 유지
            snapshot.rewards[0].equipment.curseEffects[0].value = -8; // 반환한 저장본의 수정
            Assert.That(restored.Snapshot().rewards[0].equipment.curseEffects[0].value, Is.EqualTo(-2)); // 원본 상자의 저주 보존
        } // 검증 끝
        [Test] // 미믹 재조우 체력과 복원 검증
        public void MimicHealth_RestoresAndClearsAfterVictory() // 남은 체력 유지와 승리 후 초기화
        { // 검증 시작
            Assert.That(ChestState.TryCreate("chest-health", ChestGrade.Common, false, out var chest), Is.True); // 초기 상자 생성
            Assert.That(chest.TryStoreMimicHealth(10, out _), Is.False); // 미확정 상자의 체력 저장 차단
            Assert.That(chest.TryReveal(0, 1, Guid.NewGuid().ToString("N"), Rewards(2), out var mimic), Is.True); // 미믹 후보 확정
            Assert.That(mimic.Snapshot().mimicHealth, Is.Zero); // 저장 전 미믹 체력 미설정
            Assert.That(mimic.TryStoreMimicHealth(0, out _), Is.False); // 체력 소진 상태 저장 차단
            Assert.That(mimic.TryStoreMimicHealth(-1, out _), Is.False); // 음수 체력 차단
            Assert.That(mimic.TryStoreMimicHealth(17, out var injured), Is.True); // 살아 있는 미믹 체력 저장
            Assert.That(mimic.Snapshot().mimicHealth, Is.Zero); // 원본 상태의 불변성 확인
            Assert.That(ChestState.TryRestore(injured.Snapshot(), out var restored), Is.True); // 중단 후 미믹 복원
            Assert.That(restored.Snapshot().mimicHealth, Is.EqualTo(17)); // 저장한 남은 체력 확인
            Assert.That(restored.TryWinMimic(60, out var victory), Is.True); // 복원된 미믹의 승리 확정
            Assert.That(victory.Snapshot().mimicHealth, Is.Zero); // 처치 후 체력 초기화
            Assert.That(victory.TryStoreMimicHealth(1, out _), Is.False); // 처치 후 체력 저장 차단
            var bad = injured.Snapshot(); // 생존 상태의 독립 저장본
            bad.mimicHealth = -1; // 손상된 음수 체력
            Assert.That(ChestState.TryRestore(bad, out _), Is.False); // 손상 체력 복원 차단
            bad = victory.Snapshot(); // 승리한 상태의 독립 저장본
            bad.mimicHealth = 1; // 처치 상태의 모순된 체력
            Assert.That(ChestState.TryRestore(bad, out _), Is.False); // 처치 상태의 체력 복원 차단
            bad = chest.Snapshot(); // 미확정 상자의 독립 저장본
            bad.mimicHealth = 1; // 미확정 상자의 잘못된 체력
            Assert.That(ChestState.TryRestore(bad, out _), Is.False); // 미확정 상자의 체력 차단
        } // 검증 끝
        [Test] // 손상된 저장과 잘못된 후보 검증
        public void Restore_RejectsCorruptStateAndDuplicateCandidateIds() // 잘못된 후보와 저장 차단
        { // 검증 시작
            Assert.That(ChestState.TryCreate("chest-5", ChestGrade.Common, false, out var chest), Is.True); // 일반 상자 생성
            var rewards = Rewards(2); // 두 후보 구성
            rewards[1].candidateId = rewards[0].candidateId; // 중복 후보 식별자
            Assert.That(chest.TryReveal(99, 2, Guid.NewGuid().ToString("N"), rewards, out _), Is.False); // 중복 지급 후보 차단
            var bad = chest.Snapshot(); // 실제 초기 상자 저장본
            bad.completed = true; // 판정 없는 잘못된 완료 상태
            Assert.That(ChestState.TryRestore(bad, out _), Is.False); // 중단 복원 오류 차단
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
