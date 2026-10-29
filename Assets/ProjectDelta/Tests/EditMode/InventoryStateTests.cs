using NUnit.Framework; // 단위 검증 도구
using ProjectDelta.Core; // 인벤토리 규칙
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class InventoryStateTests // 슬롯과 획득 규칙 검증
    { // 클래스 시작
        [Test] // 슬롯 교환과 수량 보존 검증
        public void Swap_MovesAndExchangesCompleteStacks() // 빈 칸 이동과 서로 다른 묶음 교환
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            inventory.Add("potion", "물약", 3, 5); // 첫 아이템 묶음
            inventory.Add("herb", "약초", 2, 3); // 두 번째 아이템 묶음
            var method = typeof(InventoryState).GetMethod("TrySwap"); // 슬롯 교환 규칙 조회
            Assert.That(method, Is.Not.Null); // 슬롯 교환 기능 제공 확인
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, 8 // 슬롯과 아이템 인수
            }), Is.True); // 빈 칸으로 이동
            Assert.That(inventory.Slots[0].IsEmpty, Is.True); // 이전 칸 비움
            Assert.That(inventory.Slots[8].ItemId, Is.EqualTo("potion")); // 이동한 아이템 확인
            Assert.That(inventory.Slots[8].Quantity, Is.EqualTo(3)); // 이동한 수량 확인
            Assert.That(inventory.Slots[8].StackLimit, Is.EqualTo(5)); // 이동한 중첩 한도 확인
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                8, 1 // 슬롯과 아이템 인수
            }), Is.True); // 두 묶음 교환
            Assert.That(inventory.Slots[1].ItemId, Is.EqualTo("potion")); // 첫 묶음 위치 확인
            Assert.That(inventory.Slots[8].ItemId, Is.EqualTo("herb")); // 두 번째 묶음 위치 확인
            Assert.That(inventory.Slots[8].Quantity, Is.EqualTo(2)); // 두 번째 묶음 수량 확인
            Assert.That(inventory.Slots[8].StackLimit, Is.EqualTo(3)); // 두 번째 묶음 한도 확인
        } // 검증 끝
        [Test] // 잘못된 슬롯과 같은 슬롯 검증
        public void Swap_InvalidIndicesAndSameSlotPreserveContents() // 범위 밖 입력과 취소 처리
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            inventory.Add("potion", "물약", 2, 5); // 시험 아이템 보관
            var method = typeof(InventoryState).GetMethod("TrySwap"); // 슬롯 교환 규칙 조회
            Assert.That(method, Is.Not.Null); // 슬롯 교환 기능 제공 확인
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                -1, 0 // 슬롯과 아이템 인수
            }), Is.False); // 범위 밖 출발 거절
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, 10 // 슬롯과 아이템 인수
            }), Is.False); // 범위 밖 도착 거절
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, 0 // 슬롯과 아이템 인수
            }), Is.True); // 같은 칸 선택 처리
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo("potion")); // 기존 아이템 보존
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(2)); // 기존 수량 보존
        } // 검증 끝
        [Test] // 가득 찬 슬롯의 안전한 교체 검증
        public void Replace_ValidatesBeforeReplacingWholeStack() // 확인된 슬롯의 전체 묶음 교체
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            inventory.Add("herb", "약초", 3, 5); // 기존 아이템 묶음
            var method = typeof(InventoryState).GetMethod("TryReplace"); // 슬롯 교체 규칙 조회
            Assert.That(method, Is.Not.Null); // 안전한 교체 기능 제공 확인
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, "", "새 아이템", 1, 1 // 슬롯과 아이템 인수
            }), Is.False); // 빈 아이템 식별자 거절
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, "new", "새 아이템", 2, 1 // 슬롯과 아이템 인수
            }), Is.False); // 중첩 한도 초과 거절
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                1, "new", "새 아이템", 1, 1 // 슬롯과 아이템 인수
            }), Is.False); // 빈 슬롯의 교체 거절
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo("herb")); // 잘못된 요청 후 기존 아이템 보존
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(3)); // 기존 수량 보존
            Assert.That(method.Invoke(inventory, new object[] // 판정 인수
            { // 인수 목록 시작
                0, "new", "새 아이템", 1, 1 // 슬롯과 아이템 인수
            }), Is.True); // 확인된 슬롯 교체
            Assert.That(inventory.Slots[0].ItemId, Is.EqualTo("new")); // 새 아이템 확인
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 새 수량 확인
            Assert.That(inventory.Slots[0].StackLimit, Is.EqualTo(1)); // 새 중첩 한도 확인
        } // 검증 끝
        [TestCase(50, 100, 75)] // 일반 체력 회복
        [TestCase(90, 100, 100)] // 최대 체력 제한
        public void PotionRecovery_RestoresHealthUpToMaximum(int current, int maximum, int expected) // 회복량과 상한 확인
        { // 검증 시작
            var type = typeof(InventoryState).Assembly.GetType("ProjectDelta.Core.PotionRecoveryRules"); // 순수 회복 규칙 조회
            Assert.That(type, Is.Not.Null); // 회복 규칙 제공 확인
            var method = type.GetMethod("TryRecover"); // 회복 판정 조회
            var args = new object[] // 회복 판정 인수
            { // 인수 목록 시작
                current, maximum, 25, 0 // 현재 체력과 최대 체력 및 시험 회복량
            }; // 인수 목록 끝
            Assert.That(method.Invoke(null, args), Is.True); // 유효한 회복 확인
            Assert.That(args[3], Is.EqualTo(expected)); // 회복 결과 확인
        } // 검증 끝
        [TestCase(100, 100, 25)] // 최대 체력에서 사용 차단
        [TestCase(0, 100, 25)] // 체력 소진 상태 차단
        [TestCase(50, 100, 0)] // 회복량 없는 사용 차단
        [TestCase(101, 100, 25)] // 잘못된 현재 체력 차단
        public void PotionRecovery_NoEffectOrInvalidInputIsRejected(int current, int maximum, int amount) // 효과 없는 소비 방지
        { // 검증 시작
            var type = typeof(InventoryState).Assembly.GetType("ProjectDelta.Core.PotionRecoveryRules"); // 순수 회복 규칙 조회
            Assert.That(type, Is.Not.Null); // 회복 규칙 제공 확인
            var args = new object[] // 회복 판정 인수
            { // 인수 목록 시작
                current, maximum, amount, 0 // 체력과 회복량
            }; // 인수 목록 끝
            Assert.That(type.GetMethod("TryRecover").Invoke(null, args), Is.False); // 효과 없는 회복 거절
            Assert.That(args[3], Is.EqualTo(current)); // 원래 체력 보존
        } // 검증 끝
        [Test] // 큰 체력과 회복량의 정수 범위 검증
        public void PotionRecovery_LargeValuesDoNotOverflow() // 최대 정수 체력 제한 확인
        { // 검증 시작
            var type = typeof(InventoryState).Assembly.GetType("ProjectDelta.Core.PotionRecoveryRules"); // 순수 회복 규칙 조회
            Assert.That(type, Is.Not.Null); // 회복 규칙 제공 확인
            var args = new object[] // 큰 회복 판정 인수
            { // 인수 목록 시작
                int.MaxValue - 1, int.MaxValue, int.MaxValue, 0 // 최대 정수 체력과 회복량
            }; // 인수 목록 끝
            Assert.That(type.GetMethod("TryRecover").Invoke(null, args), Is.True); // 유효한 회복 확인
            Assert.That(args[3], Is.EqualTo(int.MaxValue)); // 넘침 없는 최대 체력 확인
        } // 검증 끝
        [Test] // 전투 회복약 성공과 아이템 소비 검증
        public void BattlePotion_HealsAndConsumesOnePotion() // 전투 체력과 회복약 수량 변경
        { // 검증 시작
            var inventory = new InventoryState(); // 시험 인벤토리
            inventory.Add("potion", "물약", 2, 5); // 회복약 두 개 보관
            var actor = new BattleActor("플레이어", 10, true, 0); // 시험 플레이어
            var fighter = new CombatFighter(actor, 100, 50, 40, 90, 40, 50); // 피해를 받은 시험 전투자
            var type = typeof(InventoryState).Assembly.GetType("ProjectDelta.Core.BattlePotionRules"); // 전투 회복약 규칙 조회
            Assert.That(type, Is.Not.Null); // 전투 회복약 규칙 제공 확인
            var args = new object[] // 사용 판정 인수
            { // 인수 목록 시작
                inventory, fighter, "potion", 25, 0 // 인벤토리와 전투자 및 회복량
            }; // 인수 목록 끝
            Assert.That(type.GetMethod("TryUse").Invoke(null, args), Is.True); // 유효한 전투 회복약 사용
            Assert.That(args[4], Is.EqualTo(25)); // 실제 회복량 확인
            Assert.That(fighter.Health, Is.EqualTo(75)); // 전투 체력 회복 확인
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 회복약 한 개 소비 확인
        } // 검증 끝
        [Test] // 전투 회복약 사용 불가의 데이터 보존 검증
        public void BattlePotion_RejectsNoEffectAndMissingPotion() // 최대 체력과 없는 아이템 처리
        { // 검증 시작
            var inventory = new InventoryState(); // 시험 인벤토리
            inventory.Add("potion", "물약", 1, 5); // 시험 회복약 보관
            var actor = new BattleActor("플레이어", 10, true, 0); // 시험 플레이어
            var fighter = new CombatFighter(actor, 100, 50, 40, 90, 40); // 최대 체력의 시험 전투자
            var type = typeof(InventoryState).Assembly.GetType("ProjectDelta.Core.BattlePotionRules"); // 전투 회복약 규칙 조회
            Assert.That(type, Is.Not.Null); // 전투 회복약 규칙 제공 확인
            var args = new object[] // 최대 체력 판정 인수
            { // 인수 목록 시작
                inventory, fighter, "potion", 25, 0 // 인벤토리와 전투자 및 회복량
            }; // 인수 목록 끝
            Assert.That(type.GetMethod("TryUse").Invoke(null, args), Is.False); // 효과 없는 사용 차단
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 회복약 수량 보존
            fighter.ApplyDamage(20); // 시험 체력 감소
            args[2] = "missing"; // 없는 아이템 식별자
            Assert.That(type.GetMethod("TryUse").Invoke(null, args), Is.False); // 없는 회복약 사용 차단
            Assert.That(fighter.Health, Is.EqualTo(80)); // 실패 후 전투 체력 보존
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 다른 회복약 수량 보존
        } // 검증 끝
        [Test] // 기본 슬롯 검증
        public void NewInventory_HasTenEmptySlots() // 새 게임의 빈 슬롯 확인
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            Assert.That(inventory.Slots.Count, Is.EqualTo(10)); // 기존 기본 10칸 확인
            foreach (var slot in inventory.Slots) // 모든 슬롯 순회
            { // 슬롯 검사 시작
                Assert.That(slot.IsEmpty, Is.True); // 빈 슬롯 확인
            } // 슬롯 검사 끝
        } // 검증 끝
        [Test] // 기존 중첩 우선 검증
        public void Add_FillsExistingStacksBeforeEmptySlots() // 기존 묶음과 새 슬롯 분배
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            var first = inventory.Add("potion", "물약", 3, 5); // 첫 묶음 획득
            var second = inventory.Add("potion", "물약", 4, 5); // 같은 아이템 추가 획득
            Assert.That(first.Added, Is.EqualTo(3)); // 첫 획득 수량
            Assert.That(second.Added, Is.EqualTo(4)); // 추가 획득 수량
            Assert.That(second.Remaining, Is.Zero); // 남은 수량 없음
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(5)); // 기존 묶음 먼저 채움
            Assert.That(inventory.Slots[1].Quantity, Is.EqualTo(2)); // 나머지 수량 새 슬롯 배치
        } // 검증 끝
        [Test] // 가득 찬 인벤토리 검증
        public void Add_ReportsRemainingWhenCapacityIsFull() // 공간 부족 수량 보존
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            var result = inventory.Add("stone", "돌", 22, 2); // 열 묶음을 넘는 아이템 획득
            Assert.That(result.Added, Is.EqualTo(20)); // 실제 저장된 수량 확인
            Assert.That(result.Remaining, Is.EqualTo(2)); // 넣지 못한 수량 확인
            Assert.That(inventory.Slots[9].Quantity, Is.EqualTo(2)); // 마지막 슬롯 중첩 확인
            var blocked = inventory.Add("other", "다른 아이템", 1, 1); // 빈 슬롯이 없는 추가 획득
            Assert.That(blocked.Added, Is.Zero); // 기존 슬롯 덮어쓰기 방지
            Assert.That(blocked.Remaining, Is.EqualTo(1)); // 넣지 못한 아이템 보존
        } // 검증 끝
        [Test] // 수량 제거 검증
        public void Remove_ClampsAmountAndClearsEmptySlot() // 일부 제거와 빈 슬롯 초기화
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            inventory.Add("key", "열쇠", 3, 5); // 세 개의 열쇠 획득
            Assert.That(inventory.Remove(0, 2, out var first), Is.True); // 일부 수량 제거
            Assert.That(first, Is.EqualTo(2)); // 실제 제거 수량 확인
            Assert.That(inventory.Slots[0].Quantity, Is.EqualTo(1)); // 남은 수량 확인
            Assert.That(inventory.Remove(0, 9, out var last), Is.True); // 남은 수량 이상 제거
            Assert.That(last, Is.EqualTo(1)); // 실제 보유 수량만 제거
            Assert.That(inventory.Slots[0].IsEmpty, Is.True); // 빈 슬롯 초기화
            Assert.That(inventory.Remove(0, 1, out _), Is.False); // 빈 슬롯 제거 차단
        } // 검증 끝
        [Test] // 잘못된 입력 검증
        public void Add_InvalidInputDoesNotChangeSlots() // 잘못된 획득 입력 차단
        { // 검증 시작
            var inventory = new InventoryState(); // 새 인벤토리
            var invalid = inventory.Add("", "", 2, 5); // 없는 아이템 식별자
            Assert.That(invalid.Added, Is.Zero); // 잘못된 획득 차단
            Assert.That(invalid.Remaining, Is.EqualTo(2)); // 미획득 수량 보존
            Assert.That(inventory.Add("item", "아이템", -2, 5).Added, Is.Zero); // 음수 수량 차단
            Assert.That(inventory.Remove(-1, 1, out _), Is.False); // 잘못된 슬롯 차단
            Assert.That(inventory.Slots[0].IsEmpty, Is.True); // 원래 슬롯 보존
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
