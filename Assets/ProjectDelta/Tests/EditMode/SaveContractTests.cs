using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 저장 계약
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class SaveContractTests // 저장 계약 검증
    { // 클래스 시작
        [Test] // 정상 회차 검증
        public void Run_AcceptsValidSnapshot() // 정상 회차 허용
        { // 검증 시작
            var run = new RunSaveData { version = 1, floor = 2, health = 40, maxHealth = 100, slots = new[] { new SaveItemSlot { itemId = "potion", quantity = 2, stackLimit = 5 } } }; // 정상 회차 데이터
            Assert.That(run.IsValid(), Is.True); // 정상 데이터 허용 확인
        } // 검증 끝
        [Test] // 회차 버전과 체력 검증
        public void Run_RejectsUnknownVersionAndInvalidHealth() // 버전과 체력 오류 거절
        { // 검증 시작
            var run = new RunSaveData { version = 8, floor = 1, health = 1, maxHealth = 1, slots = new SaveItemSlot[0] }; // 알 수 없는 버전
            Assert.That(run.IsValid(), Is.False); // 새 버전 거절 확인
            run.version = 1; // 지원 버전 설정
            run.health = 2; // 최대치 초과 체력
            Assert.That(run.IsValid(), Is.False); // 잘못된 체력 거절 확인
            run.health = 1; // 정상 체력 설정
            run.floor = 0; // 잘못된 층 번호
            Assert.That(run.IsValid(), Is.False); // 잘못된 층 거절 확인
        } // 검증 끝
        [Test] // 아이템 수량과 빈 칸 검증
        public void Run_RejectsInvalidSlots() // 잘못된 아이템 거절
        { // 검증 시작
            var run = new RunSaveData { version = 1, floor = 1, health = 1, maxHealth = 1, slots = new[] { new SaveItemSlot { itemId = "potion", quantity = 6, stackLimit = 5 } } }; // 중첩 한도 초과
            Assert.That(run.IsValid(), Is.False); // 수량 오류 거절 확인
            run.slots[0] = new SaveItemSlot { itemId = "", quantity = 0, stackLimit = 1 }; // 정상 빈 슬롯
            Assert.That(run.IsValid(), Is.True); // 빈 슬롯 허용 확인
            run.slots = null; // 누락된 슬롯 목록
            Assert.That(run.IsValid(), Is.False); // 누락 거절 확인
        } // 검증 끝
        [Test] // 프로필 관찰 기록 검증
        public void Profile_RejectsDuplicateOrEmptyObservationIds() // 중복과 빈 식별자 거절
        { // 검증 시작
            var profile = new ProfileSaveData { version = 1, observationIds = new[] { "queen", "slime" } }; // 정상 영구 기록
            Assert.That(profile.IsValid(), Is.True); // 정상 기록 허용 확인
            profile.observationIds = new[] { "queen", "queen" }; // 중복 관찰 기록
            Assert.That(profile.IsValid(), Is.False); // 중복 기록 거절 확인
            profile.observationIds = new[] { " " }; // 공백 관찰 기록
            Assert.That(profile.IsValid(), Is.False); // 빈 기록 거절 확인
            profile.observationIds = new string[0]; // 빈 관찰 기록
            profile.version = 2; // 알 수 없는 버전
            Assert.That(profile.IsValid(), Is.False); // 새 버전 거절 확인
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
