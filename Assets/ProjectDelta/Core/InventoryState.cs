using System; // 수량과 이름 검증
using System.Collections.Generic; // 인벤토리 슬롯 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class InventoryState // 현재 게임의 인벤토리
    { // 클래스 시작
        private const int BaseSlots = 10; // 기존 기본 슬롯 수
        private const int MaxSlots = 23; // 기본 슬롯과 영구 강화 및 최대 가방 합계
        private readonly List<InventorySlot> slots = new List<InventorySlot>(); // 실제 슬롯 목록
        private readonly IReadOnlyList<InventorySlot> view; // 외부 수정이 불가능한 슬롯 목록
        public IReadOnlyList<InventorySlot> Slots => view; // 슬롯 조회
        public SaveItemSlot[] SaveSlots() // 위치를 보존하는 슬롯 저장본
        { // 저장 시작
            var result = new SaveItemSlot[slots.Count]; // 독립 저장 슬롯 배열
            for (var index = 0; index < slots.Count; index++) // 슬롯 순서 순회
            { // 슬롯 복사 시작
                var slot = slots[index]; // 현재 아이템 묶음
                result[index] = new SaveItemSlot { itemId = slot.ItemId, name = slot.Name, quantity = slot.Quantity, stackLimit = slot.StackLimit }; // 위치와 내용을 독립 복사
            } // 슬롯 복사 끝
            return result; // 저장 슬롯 전달
        } // 저장 끝
        public static InventoryState FromSavedSlots(SaveItemSlot[] saved) // 검증된 저장 슬롯 복원
        { // 복원 시작
            if (saved == null || saved.Length < BaseSlots || saved.Length > MaxSlots) // 지원 저장 슬롯 수 검사
            { // 수량 오류 시작
                return null; // 잘못된 목록 거절
            } // 수량 오류 끝
            var result = new InventoryState(); // 독립 인벤토리 생성
            if (!result.TryResize(saved.Length)) // 저장 용량 확장 검사
            { // 확장 실패 시작
                return null; // 잘못된 목록 거절
            } // 확장 실패 끝
            for (var index = 0; index < saved.Length; index++) // 저장된 순서 순회
            { // 복원 순회 시작
                var slot = saved[index]; // 저장된 슬롯 조회
                if (slot == null || !slot.IsValid()) // 누락과 잘못된 아이템 검사
                { // 슬롯 오류 시작
                    return null; // 불완전한 복원 거절
                } // 슬롯 오류 끝
                result.slots[index].ItemId = slot.itemId ?? string.Empty; // 아이템 식별자 복원
                result.slots[index].Name = slot.name ?? string.Empty; // 아이템 표시 이름 복원
                result.slots[index].Quantity = slot.quantity; // 수량 복원
                result.slots[index].StackLimit = slot.stackLimit; // 중첩 제한 복원
            } // 복원 순회 끝
            return result; // 검증된 인벤토리 전달
        } // 복원 끝
        public InventoryState() // 기본 10칸 초기화
        { // 생성 시작
            for (var index = 0; index < BaseSlots; index++) // 기본 슬롯 수 반복
            { // 슬롯 생성 시작
                slots.Add(new InventorySlot()); // 빈 슬롯 추가
            } // 슬롯 생성 끝
            view = slots.AsReadOnly(); // 외부에서 목록 변경 차단
        } // 생성 끝
        public bool TryResize(int capacity) // 보유 아이템을 보존하는 용량 변경
        { // 변경 시작
            if (capacity < BaseSlots || capacity > MaxSlots) // 지원 용량 범위 검사
            { // 범위 오류 시작
                return false; // 기존 인벤토리 보존
            } // 범위 오류 끝
            var occupied = 0; // 보유 아이템 묶음 수
            foreach (var slot in slots) // 현재 슬롯 순회
            { // 보유 수량 검사 시작
                if (!slot.IsEmpty) // 사용 중인 슬롯 검사
                { // 사용 슬롯 분기 시작
                    occupied++; // 실제 묶음 수 증가
                } // 사용 슬롯 분기 끝
            } // 보유 수량 검사 끝
            if (occupied > capacity) // 축소 후 공간 부족 검사
            { // 공간 부족 시작
                return false; // 슬롯 변경 없이 축소 거절
            } // 공간 부족 끝
            while (slots.Count < capacity) // 신규 슬롯 필요 검사
            { // 확장 시작
                slots.Add(new InventorySlot()); // 빈 슬롯 추가
            } // 확장 끝
            for (var index = capacity; index < slots.Count; index++) // 축소 범위 슬롯 순회
            { // 후반 아이템 보존 시작
                if (slots[index].IsEmpty) // 빈 후반 슬롯 검사
                { // 빈 슬롯 분기 시작
                    continue; // 이동 없는 다음 슬롯 검사
                } // 빈 슬롯 분기 끝
                for (var target = 0; target < capacity; target++) // 보존 범위 빈 슬롯 순회
                { // 빈 슬롯 탐색 시작
                    if (slots[target].IsEmpty) // 앞쪽 빈 슬롯 검사
                    { // 이동 대상 발견 시작
                        slots[target] = slots[index]; // 기존 묶음과 수량 보존 이동
                        slots[index] = new InventorySlot(); // 이동한 후반 슬롯 비우기
                        break; // 다음 후반 슬롯 검사
                    } // 이동 대상 발견 끝
                } // 빈 슬롯 탐색 끝
            } // 후반 아이템 보존 끝
            if (slots.Count > capacity) // 제거할 빈 후반 슬롯 검사
            { // 축소 시작
                slots.RemoveRange(capacity, slots.Count - capacity); // 보존 후 초과 슬롯 제거
            } // 축소 끝
            return true; // 용량 변경 성공
        } // 변경 끝
        public InventoryAddResult Add(string itemId, string name, int quantity, int stackLimit) // 아이템 획득
        { // 획득 시작
            var requested = Math.Max(0, quantity); // 음수를 제외한 요청 수량
            if (string.IsNullOrEmpty(itemId) || requested == 0) // 아이템 식별자와 수량 검사
            { // 잘못된 입력 조건 시작
                return new InventoryAddResult(0, requested); // 미획득 수량 보존
            } // 잘못된 입력 조건 끝
            var remaining = requested; // 아직 넣지 못한 수량
            var safeLimit = Math.Max(1, stackLimit); // 최소 한 개의 중첩 제한
            foreach (var slot in slots) // 기존 슬롯 순회
            { // 기존 묶음 채우기 시작
                if (remaining == 0) // 요청 수량 처리 완료 검사
                { // 완료 조건 시작
                    break; // 추가 슬롯 검사 종료
                } // 완료 조건 끝
                if (slot.IsEmpty || slot.ItemId != itemId || slot.Quantity >= slot.StackLimit) // 같은 아이템의 남은 공간 검사
                { // 다른 슬롯 조건 시작
                    continue; // 기존 묶음 제외
                } // 다른 슬롯 조건 끝
                var placed = Math.Min(remaining, slot.StackLimit - slot.Quantity); // 기존 묶음에 들어갈 수량
                slot.Quantity += placed; // 기존 묶음 수량 증가
                remaining -= placed; // 남은 요청 수량 감소
            } // 기존 묶음 채우기 끝
            foreach (var slot in slots) // 빈 슬롯 순회
            { // 새 묶음 채우기 시작
                if (remaining == 0) // 요청 수량 처리 완료 검사
                { // 완료 조건 시작
                    break; // 추가 슬롯 검사 종료
                } // 완료 조건 끝
                if (!slot.IsEmpty) // 빈 슬롯 여부 검사
                { // 사용 중 조건 시작
                    continue; // 기존 아이템 보존
                } // 사용 중 조건 끝
                var placed = Math.Min(remaining, safeLimit); // 새 슬롯에 들어갈 수량
                slot.ItemId = itemId; // 아이템 식별자 저장
                slot.Name = string.IsNullOrEmpty(name) ? itemId : name; // 이름 또는 식별자 표시
                slot.Quantity = placed; // 새 묶음 수량 저장
                slot.StackLimit = safeLimit; // 새 묶음 최대 수량 저장
                remaining -= placed; // 남은 요청 수량 감소
            } // 새 묶음 채우기 끝
            return new InventoryAddResult(requested - remaining, remaining); // 실제 추가와 남은 수량 전달
        } // 획득 끝
        public bool TryReplace(int slotIndex, string itemId, string name, int quantity, int stackLimit) // 확인된 기존 묶음을 새 묶음으로 교체
        { // 교체 시작
            if (slotIndex < 0 || slotIndex >= slots.Count || slots[slotIndex].IsEmpty || string.IsNullOrWhiteSpace(itemId) || quantity <= 0 || stackLimit <= 0 || quantity > stackLimit) // 대상과 새 묶음의 유효성 검사
            { // 잘못된 교체 조건 시작
                return false; // 기존 아이템 변경 차단
            } // 잘못된 교체 조건 끝
            var replacement = new InventorySlot // 검증된 새 아이템 묶음 준비
            { // 묶음 생성 시작
                ItemId = itemId, // 새 아이템 식별자
                Name = string.IsNullOrEmpty(name) ? itemId : name, // 새 아이템 표시 이름
                Quantity = quantity, // 새 아이템 수량
                StackLimit = stackLimit // 새 아이템 중첩 한도
            }; // 묶음 생성 끝
            slots[slotIndex] = replacement; // 검증 후 선택한 슬롯만 교체
            return true; // 안전한 묶음 교체 완료
        } // 교체 끝
        public bool TrySwap(int first, int second) // 두 슬롯의 전체 아이템 교환
        { // 교환 시작
            if (first < 0 || first >= slots.Count || second < 0 || second >= slots.Count) // 두 슬롯의 범위 검사
            { // 범위 오류 조건 시작
                return false; // 잘못된 슬롯 변경 차단
            } // 범위 오류 조건 끝
            var previous = slots[first]; // 첫 슬롯의 전체 묶음 보관
            slots[first] = slots[second]; // 두 번째 묶음을 첫 슬롯에 이동
            slots[second] = previous; // 첫 묶음을 두 번째 슬롯에 이동
            return true; // 수량과 중첩 한도를 보존한 교환 완료
        } // 교환 끝
        public bool Remove(int slotIndex, int quantity, out int removed) // 슬롯의 아이템 수량 제거
        { // 제거 시작
            removed = 0; // 제거 수량 초기화
            if (slotIndex < 0 || slotIndex >= slots.Count || quantity <= 0 || slots[slotIndex].IsEmpty) // 슬롯과 제거 수량 검사
            { // 제거 불가 조건 시작
                return false; // 잘못된 제거 차단
            } // 제거 불가 조건 끝
            var slot = slots[slotIndex]; // 대상 슬롯 조회
            removed = Math.Min(quantity, slot.Quantity); // 실제 보유량까지만 제거
            slot.Quantity -= removed; // 남은 수량 갱신
            if (slot.Quantity == 0) // 슬롯의 수량 소진 검사
            { // 빈 슬롯 조건 시작
                slot.ItemId = string.Empty; // 아이템 식별자 비우기
                slot.Name = string.Empty; // 표시 이름 비우기
                slot.StackLimit = 1; // 기본 중첩 제한 복구
            } // 빈 슬롯 조건 끝
            return true; // 유효한 제거 성공
        } // 제거 끝
    } // 클래스 끝
} // 이름 공간 끝
