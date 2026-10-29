using UnityEngine; // 실제 화면 객체
using UnityEngine.EventSystems; // 화면 취소 입력
using UnityEngine.UI; // 화면 배경과 글자
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class InventoryScreenView : MonoBehaviour, ICancelHandler // 탐험 중 인벤토리 화면
    { // 클래스 시작
        private AppRoot owner; // 현재 게임의 인벤토리 관리자
        private readonly Text[] slots = new Text[10]; // 실제 슬롯 표시 목록
        private Text status; // 슬롯 수와 획득 결과 표시
        private int selectedSlot = -1; // 이동할 출발 슬롯
        private int draggedSlot = -1; // 현재 마우스 드래그 슬롯
        private int lastSlotFrame = -1; // 같은 프레임 중복 슬롯 선택 차단
        private int lastPickupFrame = -1; // 같은 프레임의 중복 획득 차단
        private int lastKeyFrame = -1; // 같은 프레임의 중복 열쇠 지급 차단
        public static InventoryScreenView Create(AppRoot owner) // 실제 인벤토리 화면 생성
        { // 생성 시작
            var screen = new GameObject("InventoryScreen", typeof(InventoryScreenView)); // 인벤토리 상위 객체
            try // 실제 화면 구성
            { // 화면 구성 시작
                var view = screen.GetComponent<InventoryScreenView>(); // 새 인벤토리 화면 제어
                view.owner = owner; // 현재 게임 관리자 연결
                var canvas = UiFactory.CreateCanvas("InventoryCanvas"); // 인벤토리 전용 화면 생성
                canvas.transform.SetParent(screen.transform, false); // 화면 상위 객체에 연결
                canvas.sortingOrder = 100; // 탐험 화면보다 앞에 표시
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 인벤토리 배경 객체
                background.transform.SetParent(canvas.transform, false); // 인벤토리 화면 아래 배치
                var rect = background.GetComponent<RectTransform>(); // 배경 위치 조회
                rect.anchorMin = Vector2.zero; // 화면 왼쪽 아래 기준
                rect.anchorMax = Vector2.one; // 화면 오른쪽 위 기준
                rect.offsetMin = Vector2.zero; // 아래쪽 여백 제거
                rect.offsetMax = Vector2.zero; // 위쪽 여백 제거
                background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 인벤토리 화면 배경색
                var accent = new Color(0.62f, 0.87f, 0.79f); // 인벤토리 강조 색상
                UiFactory.CreateText(canvas.transform, "Header", "인벤토리", 58, new Vector2(0f, 420f), new Vector2(1000f, 90f), Color.white); // 인벤토리 제목
                view.status = UiFactory.CreateText(canvas.transform, "InventoryStatus", "", 25, new Vector2(0f, 345f), new Vector2(1200f, 60f), accent); // 슬롯과 획득 상태
                for (var index = 0; index < view.slots.Length; index++) // 기본 슬롯 수만큼 반복
                { // 슬롯 화면 생성 시작
                    var x = index < 5 ? -450f : 450f; // 두 열의 가로 위치
                    var y = 240f - (index % 5) * 85f; // 각 슬롯의 세로 위치
                    var slotIndex = index; // 버튼별 슬롯 번호 보관
                    var slotButton = UiFactory.CreateButton(canvas.transform, "InventorySlotButton_" + index, "", new Vector2(x, y)); // 선택 가능한 아이템 슬롯
                    slotButton.GetComponent<RectTransform>().sizeDelta = new Vector2(720f, 65f); // 실제 슬롯 버튼 크기
                    var label = slotButton.transform.Find("Label"); // 슬롯 글자 객체
                    label.name = "InventorySlot_" + index; // 기존 슬롯 글자 이름 유지
                    label.GetComponent<RectTransform>().sizeDelta = new Vector2(720f, 65f); // 슬롯 글자 표시 영역
                    view.slots[index] = label.GetComponent<Text>(); // 실제 슬롯 글자 연결
                    view.slots[index].fontSize = 24; // 슬롯 글자 크기
                    slotButton.onClick.AddListener(() => view.SelectSlot(slotIndex)); // 키보드와 게임패드 슬롯 선택
                    slotButton.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 슬롯 취소 입력 연결
                    slotButton.gameObject.AddComponent<InventorySlotDragHandler>().Initialize(view, slotIndex); // 마우스 드래그 연결
                } // 슬롯 화면 생성 끝
                UiFactory.CreateText(canvas.transform, "Notice", "슬롯 선택 후 이동 · 마우스 드래그 지원", 22, new Vector2(0f, -255f), new Vector2(1200f, 55f), accent); // 시험 기능 안내
                var pickup = UiFactory.CreateButton(canvas.transform, "TrialPickupButton", "시험 아이템 획득", new Vector2(-500f, -365f)); // 시험 획득 버튼
                pickup.onClick.AddListener(view.AddTrialItem); // 시험 아이템 획득 연결
                pickup.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 선택 버튼의 취소 입력 연결
                var key = UiFactory.CreateButton(canvas.transform, "TrialKeyButton", "시험 열쇠 지급", new Vector2(0f, -365f)); // 시험 열쇠 지급 버튼
                key.onClick.AddListener(view.AddTrialKey); // 시험 열쇠 지급 연결
                key.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 선택 버튼의 취소 입력 연결
                var close = UiFactory.CreateButton(canvas.transform, "CloseInventoryButton", "닫기", new Vector2(500f, -365f)); // 탐험 복귀 버튼
                close.onClick.AddListener(owner.CloseInventory); // 인벤토리 닫기 연결
                close.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 선택 버튼의 취소 입력 연결
                view.Refresh(); // 현재 게임의 슬롯 표시
                EventSystem.current.SetSelectedGameObject(pickup.gameObject); // 키보드와 게임패드 선택 지정
                return view; // 준비된 화면 전달
            } // 화면 구성 끝
            catch // 화면 준비 실패 정리
            { // 실패 정리 시작
                Destroy(screen); // 불완전한 화면 제거
                throw; // 원래 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        public void OnCancel(BaseEventData eventData) // 취소 또는 같은 단축키 처리
        { // 취소 시작
            eventData.Use(); // 중복 취소 전파 차단
            if (draggedSlot >= 0) // 진행 중인 마우스 드래그 검사
            { // 드래그 취소 조건 시작
                draggedSlot = -1; // 대기 중인 드래그 취소
                Refresh(); // 기존 아이템 표시 복구
                return; // 인벤토리 화면 유지
            } // 드래그 취소 조건 끝
            if (selectedSlot >= 0) // 이동할 출발 슬롯 선택 검사
            { // 이동 취소 조건 시작
                selectedSlot = -1; // 대기 중인 슬롯 이동 취소
                Refresh(); // 기존 아이템과 안내 갱신
                return; // 인벤토리 화면 유지
            } // 이동 취소 조건 끝
            owner.CloseInventory(); // 기존 탐험 화면 복귀
        } // 취소 끝
        private void SelectSlot(int index) // 키보드와 게임패드 슬롯 선택
        { // 선택 시작
            if (draggedSlot >= 0 || lastSlotFrame == Time.frameCount || index < 0 || index >= slots.Length) // 드래그와 중복 선택 및 슬롯 범위 검사
            { // 선택 거절 조건 시작
                return; // 잘못된 슬롯 선택 차단
            } // 선택 거절 조건 끝
            lastSlotFrame = Time.frameCount; // 현재 프레임의 슬롯 선택 기록
            if (selectedSlot < 0) // 출발 슬롯 미선택 검사
            { // 출발 선택 시작
                if (owner.Inventory.Slots[index].IsEmpty) // 빈 출발 슬롯 검사
                { // 빈 슬롯 조건 시작
                    status.text = "이동할 아이템이 없습니다"; // 출발 슬롯 안내
                    return; // 빈 슬롯에서 이동 차단
                } // 빈 슬롯 조건 끝
                selectedSlot = index; // 이동할 출발 슬롯 보관
            } // 출발 선택 끝
            else // 도착 슬롯 선택 분기
            { // 도착 선택 시작
                owner.Inventory.TrySwap(selectedSlot, index); // 같은 슬롯은 그대로 유지하고 다른 슬롯은 교환
                selectedSlot = -1; // 완료한 슬롯 선택 해제
            } // 도착 선택 끝
            Refresh(); // 현재 슬롯과 이동 상태 갱신
        } // 선택 끝
        public void BeginSlotDrag(int index) // 마우스 슬롯 드래그 준비
        { // 드래그 시작
            draggedSlot = -1; // 이전 드래그 초기화
            if (index < 0 || index >= slots.Length || owner.Inventory.Slots[index].IsEmpty) // 출발 슬롯과 아이템 검사
            { // 잘못된 출발 조건 시작
                return; // 빈 슬롯의 드래그 차단
            } // 잘못된 출발 조건 끝
            selectedSlot = -1; // 이전 키보드 선택 취소
            draggedSlot = index; // 드래그 출발 슬롯 보관
            Refresh(); // 선택 표시 갱신
        } // 드래그 시작 끝
        public void DropSlot(int source, int destination) // 마우스 드래그 슬롯 교환
        { // 놓기 시작
            if (draggedSlot != source || source < 0 || destination < 0 || destination >= slots.Length) // 현재 드래그와 도착 범위 검사
            { // 잘못된 놓기 조건 시작
                return; // 다른 화면과 잘못된 드래그 차단
            } // 잘못된 놓기 조건 끝
            owner.Inventory.TrySwap(source, destination); // 전체 아이템 묶음 교환
            draggedSlot = -1; // 완료한 드래그 해제
            Refresh(); // 실제 슬롯 표시 갱신
        } // 놓기 끝
        public void EndSlotDrag(int source) // 마우스 드래그 종료 또는 취소
        { // 종료 시작
            if (draggedSlot == source) // 현재 드래그 출발 검사
            { // 취소 조건 시작
                draggedSlot = -1; // 놓지 않은 드래그 취소
                Refresh(); // 기존 슬롯 표시 복구
            } // 취소 조건 끝
        } // 종료 끝
        private void AddTrialItem() // 기존 시험 아이템 획득
        { // 획득 시작
            if (draggedSlot >= 0 || lastPickupFrame == Time.frameCount) // 드래그와 같은 프레임 중복 요청 검사
            { // 중복 조건 시작
                return; // 중복 획득 차단
            } // 중복 조건 끝
            lastPickupFrame = Time.frameCount; // 현재 프레임 획득 기록
            var result = owner.Inventory.Add("ITEM_DAY80_TEST_DROP", "전투 드롭 시험 아이템", 1, 1); // 원본 시험 아이템 한 개 획득
            Refresh(); // 실제 슬롯 상태 표시
            if (result.Remaining > 0) // 빈 슬롯 부족 검사
            { // 공간 부족 조건 시작
                status.text = "가방이 가득 찼습니다 · 남은 수량 " + result.Remaining; // 미획득 수량 안내
            } // 공간 부족 조건 끝
        } // 획득 끝
        private void AddTrialKey() // 개발 확인용 열쇠 지급
        { // 지급 시작
            if (draggedSlot >= 0 || lastKeyFrame == Time.frameCount) // 드래그와 같은 프레임 중복 검사
            { // 중복 조건 시작
                return; // 중복 지급 차단
            } // 중복 조건 끝
            lastKeyFrame = Time.frameCount; // 지급 프레임 기록
            var added = owner.Keys.Add(1); // 열쇠 한 개 지급
            Refresh(); // 보유 열쇠 표시 갱신
            if (!added) // 최대 보유량 검사
            { // 지급 실패 조건 시작
                status.text = "열쇠를 더 받을 수 없습니다"; // 지급 실패 안내
            } // 지급 실패 조건 끝
        } // 지급 끝
        private void Refresh() // 현재 게임의 인벤토리 표시
        { // 갱신 시작
            var occupied = 0; // 사용 중인 슬롯 수
            for (var index = 0; index < slots.Length; index++) // 실제 슬롯 순회
            { // 슬롯 표시 시작
                var slot = owner.Inventory.Slots[index]; // 현재 슬롯 상태
                if (!slot.IsEmpty) // 아이템 존재 검사
                { // 사용 중 조건 시작
                    occupied++; // 사용 슬롯 수 증가
                } // 사용 중 조건 끝
                slots[index].text = (selectedSlot == index ? "▶ " : "") + (index + 1) + ". " + (slot.IsEmpty ? "비어 있음" : slot.Name + " × " + slot.Quantity); // 선택 상태와 아이템 표시
            } // 슬롯 표시 끝
            status.text = selectedSlot >= 0 ? "이동할 칸을 선택하세요 · 취소로 선택 해제" : "가방 " + occupied + " / " + slots.Length + " · 열쇠 " + owner.Keys.Count; // 이동 안내 또는 현재 슬롯 표시
        } // 갱신 끝
    } // 클래스 끝
    public sealed class InventorySlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler // 마우스 슬롯 드래그 중계
    { // 클래스 시작
        private InventoryScreenView screen; // 실제 인벤토리 화면
        private int index; // 현재 슬롯 번호
        public void Initialize(InventoryScreenView target, int slotIndex) // 화면과 슬롯 연결
        { // 연결 시작
            screen = target; // 인벤토리 화면 보관
            index = slotIndex; // 슬롯 번호 보관
        } // 연결 끝
        public void OnBeginDrag(PointerEventData eventData) // 슬롯 드래그 시작
        { // 시작 처리
            if (screen != null && eventData.button == PointerEventData.InputButton.Left) // 화면과 마우스 왼쪽 버튼 검사
            { // 드래그 조건 시작
                screen.BeginSlotDrag(index); // 출발 슬롯 준비
            } // 드래그 조건 끝
        } // 시작 끝
        public void OnDrag(PointerEventData eventData) // 드래그 이동 중 입력 유지
        { // 이동 시작
        } // 이동 끝
        public void OnDrop(PointerEventData eventData) // 도착 슬롯에 드래그 놓기
        { // 놓기 시작
            var source = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<InventorySlotDragHandler>(); // 드래그 출발 슬롯 조회
            if (screen != null && source != null && source.screen == screen && eventData.button == PointerEventData.InputButton.Left) // 같은 화면의 마우스 드래그 검사
            { // 놓기 조건 시작
                screen.DropSlot(source.index, index); // 출발과 도착 슬롯 교환
            } // 놓기 조건 끝
        } // 놓기 끝
        public void OnEndDrag(PointerEventData eventData) // 드래그 종료 또는 바깥 취소
        { // 종료 시작
            if (screen != null) // 화면 존재 검사
            { // 종료 조건 시작
                screen.EndSlotDrag(index); // 남은 드래그 상태 해제
            } // 종료 조건 끝
        } // 종료 끝
    } // 클래스 끝
    public sealed class InventoryCancelRelay : MonoBehaviour, ICancelHandler // 선택 버튼의 취소 입력 중계
    { // 클래스 시작
        private InventoryScreenView screen; // 닫을 인벤토리 화면
        public void Initialize(InventoryScreenView target) // 취소 대상 연결
        { // 연결 시작
            screen = target; // 실제 인벤토리 화면 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // 선택 버튼의 취소 입력 처리
        { // 처리 시작
            if (screen != null) // 인벤토리 화면 존재 검사
            { // 화면 존재 조건 시작
                screen.OnCancel(eventData); // 인벤토리 닫기 전달
            } // 화면 존재 조건 끝
        } // 처리 끝
    } // 클래스 끝
} // 이름 공간 끝
