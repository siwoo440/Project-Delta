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
        private int lastPickupFrame = -1; // 같은 프레임의 중복 획득 차단
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
                    view.slots[index] = UiFactory.CreateText(canvas.transform, "InventorySlot_" + index, "", 24, new Vector2(x, y), new Vector2(720f, 65f), Color.white); // 개별 슬롯 표시
                } // 슬롯 화면 생성 끝
                UiFactory.CreateText(canvas.transform, "Notice", "시험 획득은 개발 확인용입니다", 22, new Vector2(0f, -255f), new Vector2(1200f, 55f), accent); // 시험 기능 안내
                var pickup = UiFactory.CreateButton(canvas.transform, "TrialPickupButton", "시험 아이템 획득", new Vector2(-235f, -365f)); // 시험 획득 버튼
                pickup.onClick.AddListener(view.AddTrialItem); // 시험 아이템 획득 연결
                pickup.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 선택 버튼의 취소 입력 연결
                var close = UiFactory.CreateButton(canvas.transform, "CloseInventoryButton", "닫기", new Vector2(235f, -365f)); // 탐험 복귀 버튼
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
            owner.CloseInventory(); // 기존 탐험 화면 복귀
        } // 취소 끝
        private void AddTrialItem() // 기존 시험 아이템 획득
        { // 획득 시작
            if (lastPickupFrame == Time.frameCount) // 같은 프레임의 중복 요청 검사
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
                slots[index].text = (index + 1) + ". " + (slot.IsEmpty ? "비어 있음" : slot.Name + " × " + slot.Quantity); // 슬롯 번호와 아이템 표시
            } // 슬롯 표시 끝
            status.text = "가방 " + occupied + " / " + slots.Length; // 사용 슬롯과 전체 슬롯 표시
        } // 갱신 끝
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
