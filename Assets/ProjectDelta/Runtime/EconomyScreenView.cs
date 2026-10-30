using System; // 닫기 콜백과 상태 비교
using ProjectDelta.Core; // 시험 상점과 유물 규칙
using UnityEngine; // 화면 구성과 위치
using UnityEngine.EventSystems; // 취소 입력 전달
using UnityEngine.UI; // 버튼과 화면 글자
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class EconomyScreenView : MonoBehaviour, ICancelHandler // 인벤토리 위의 시험 경제 화면
    { // 화면 클래스 시작
        private AppRoot owner; // 실제 회차 관리자
        private Action onClose; // 인벤토리 복귀 콜백
        private CanvasGroup mainGroup; // 확인 화면 아래 입력 잠금
        private Transform canvasRoot; // 경제 화면의 최상위 캔버스
        private GameObject modal; // 유물 교체 선택 또는 확인 화면
        private Text notice; // 거래 결과와 실패 안내
        private Text wallet; // 현재 골드와 선택 표시
        private Button closeButton; // 안전한 화면 복귀 선택
        private readonly Button[] offers = new Button[6]; // 한 페이지의 상품 버튼
        private readonly Button[] inventory = new Button[23]; // 최대 가방 용량의 소지품 버튼
        private readonly Button[] relics = new Button[6]; // 시험 유물 획득 버튼
        private int offerPage; // 현재 상품 목록 페이지
        private int selectedOffer = -1; // 실제 상점 상품 번호
        private int selectedSlot = -1; // 실제 보유 슬롯 번호
        private string incomingRelic; // 명시적으로 선택한 새 유물
        private string existingRelic; // 교체를 확인할 기존 유물
        private string[] expectedRelics; // 확인 시점의 보유 유물 목록
        private bool trialRelicsEnabled; // 개발용 무료 유물 허용 여부
        private bool closed; // 중복 닫기 방지
        public static EconomyScreenView Create(AppRoot owner, Action onClose, bool allowTrialRelics = true) // 실제 시험 경제 화면 생성
        { // 생성 시작
            if (owner == null || onClose == null || owner.Shop == null || owner.Relics == null) // 회차와 복귀 기능 검사
            { // 생성 오류 시작
                throw new ArgumentException("Invalid economy screen state."); // 불완전한 화면 생성 차단
            } // 생성 오류 끝
            var root = new GameObject("EconomyScreen", typeof(EconomyScreenView)); // 경제 화면 상위 객체
            try // 실제 화면 구성
            { // 구성 시작
                var view = root.GetComponent<EconomyScreenView>(); // 새 경제 화면 제어
                view.owner = owner; // 현재 회차 연결
                view.trialRelicsEnabled = allowTrialRelics; // 진입 경로별 개발용 유물 제한
                view.onClose = onClose; // 기존 인벤토리 복귀 연결
                var canvas = UiFactory.CreateCanvas("EconomyCanvas"); // 독립 경제 화면 캔버스
                canvas.transform.SetParent(root.transform, false); // 화면 상위 객체 연결
                canvas.sortingOrder = 120; // 기존 인벤토리보다 앞에 표시
                view.canvasRoot = canvas.transform; // 확인 화면의 부모 보관
                var main = new GameObject("EconomyMain", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)); // 전체 화면 배경과 입력 그룹
                main.transform.SetParent(canvas.transform, false); // 경제 캔버스에 배치
                Stretch(main.GetComponent<RectTransform>()); // 화면 전체 배경 확장
                main.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 입력을 가리는 불투명 배경
                UiFactory.Decorate(main.GetComponent<Image>(), "DialoguePanel"); // 화면 용도별 그림 장식
                view.mainGroup = main.GetComponent<CanvasGroup>(); // 확인 중 기존 입력 잠금
                UiFactory.CreateText(main.transform, "EconomyTitle", allowTrialRelics ? "시험 상점 · 유물" : "떠돌이 상인 · 시험 상점", 46, new Vector2(0f, 470f), new Vector2(1400f, 70f), Color.white); // 시험 경제 화면 제목
                view.wallet = UiFactory.CreateText(main.transform, "EconomyWallet", "", 24, new Vector2(0f, 413f), new Vector2(1700f, 55f), Color.white); // 실제 잔액과 선택 안내
                UiFactory.CreateText(main.transform, "ShopTitle", "상품 · 재고", 27, new Vector2(-570f, 350f), new Vector2(700f, 45f), Color.white); // 상점 상품 제목
                UiFactory.CreateText(main.transform, "InventoryTitle", "보유 아이템 · 판매할 칸 선택", 27, new Vector2(385f, 350f), new Vector2(1000f, 45f), Color.white); // 실제 소지품 제목
                for (var index = 0; index < view.offers.Length; index++) // 한 페이지의 상품 버튼 생성
                { // 상품 생성 시작
                    var entry = index; // 버튼의 페이지 내 위치
                    view.offers[index] = view.MakeButton(main.transform, "ShopOffer_" + index, "", new Vector2(-570f, 290f - index * 58f), new Vector2(680f, 50f), () => view.SelectOffer(entry)); // 상품 선택 버튼
                } // 상품 생성 끝
                for (var index = 0; index < view.inventory.Length; index++) // 최대 인벤토리 슬롯 생성
                { // 슬롯 생성 시작
                    var slot = index; // 버튼의 실제 슬롯 번호
                    var x = 50f + (index / 6) * 245f; // 네 열의 슬롯 위치
                    var y = 290f - (index % 6) * 58f; // 여섯 행의 슬롯 위치
                    view.inventory[index] = view.MakeButton(main.transform, "ShopInventory_" + index, "", new Vector2(x, y), new Vector2(228f, 50f), () => view.SelectInventory(slot)); // 실제 보유품 선택
                } // 슬롯 생성 끝
                view.MakeButton(main.transform, "ShopPreviousPage", "이전 상품", new Vector2(-760f, -80f), new Vector2(180f, 46f), () => view.ChangePage(-1)); // 이전 상품 페이지
                view.MakeButton(main.transform, "ShopNextPage", "다음 상품", new Vector2(-565f, -80f), new Vector2(180f, 46f), () => view.ChangePage(1)); // 다음 상품 페이지
                view.MakeButton(main.transform, "ShopBuyButton", "선택 상품 구매", new Vector2(-370f, -80f), new Vector2(190f, 46f), view.Buy); // 실제 구매 버튼
                view.MakeButton(main.transform, "ShopSellButton", "선택 아이템 판매", new Vector2(120f, -80f), new Vector2(260f, 46f), view.Sell); // 실제 판매 버튼
                UiFactory.CreateText(main.transform, "RelicTitle", "시험 유물 · 클릭하여 획득 또는 교체", 25, new Vector2(0f, -145f), new Vector2(1500f, 40f), Color.white).gameObject.SetActive(allowTrialRelics); // 시험 유물 안내
                var ids = TrialRelicCatalog.Ids(); // 등록된 시험 유물 목록
                for (var index = 0; index < view.relics.Length && index < ids.Length; index++) // 시험 유물 버튼 순회
                { // 유물 버튼 생성 시작
                    var id = ids[index]; // 선택할 시험 유물 식별자
                    var x = -550f + (index % 3) * 550f; // 세 열의 유물 위치
                    var y = -205f - (index / 3) * 58f; // 두 행의 유물 위치
                    view.relics[index] = view.MakeButton(main.transform, "TrialRelic_" + index, "", new Vector2(x, y), new Vector2(520f, 50f), () => view.AcquireRelic(id)); // 실제 유물 획득 버튼
                    view.relics[index].gameObject.SetActive(allowTrialRelics); // 상인 화면의 무료 지급 숨김
                } // 유물 버튼 생성 끝
                view.notice = UiFactory.CreateText(main.transform, "EconomyNotice", "같은 상품을 선택해 거래 · 개별 장비 판매 가격은 미정", 22, new Vector2(0f, -335f), new Vector2(1700f, 55f), Color.white); // 실제 결과 안내
                var close = view.MakeButton(main.transform, "CloseEconomyButton", allowTrialRelics ? "인벤토리로 돌아가기" : "상인에게 돌아가기", new Vector2(0f, -430f), new Vector2(430f, 60f), view.Close); // 화면 복귀 버튼
                view.closeButton = close; // 확인 취소 후 기본 선택 대상
                view.Refresh(); // 현재 회차의 상품과 자산 표시
                if (EventSystem.current != null) // 입력 관리자 존재 검사
                { // 초기 선택 시작
                    EventSystem.current.SetSelectedGameObject(close.gameObject); // 안전한 복귀 버튼 기본 선택
                } // 초기 선택 끝
                return view; // 준비된 경제 화면 전달
            } // 구성 끝
            catch // 화면 생성 실패 검사
            { // 실패 정리 시작
                root.SetActive(false); // 불완전한 화면 즉시 숨김
                Destroy(root); // 실패한 화면 정리
                throw; // 실제 생성 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private static void Stretch(RectTransform rect) // 전체 화면 배경 확장
        { // 확장 시작
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
        } // 확장 끝
        private Button MakeButton(Transform parent, string id, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action) // 크기와 취소 입력을 포함한 버튼 생성
        { // 생성 시작
            var button = UiFactory.CreateButton(parent, id, label, position); // 실제 화면 버튼
            button.GetComponent<RectTransform>().sizeDelta = size; // 버튼 크기 적용
            var text = button.transform.Find("Label").GetComponent<Text>(); // 버튼 글자 조회
            text.rectTransform.sizeDelta = size; // 글자 영역 적용
            text.fontSize = 20; // 긴 상품과 유물 이름 크기
            text.resizeTextForBestFit = true; // 버튼 크기에 맞춘 글자 표시
            text.resizeTextMinSize = 12; // 글자 최소 크기
            text.resizeTextMaxSize = 20; // 글자 최대 크기
            button.onClick.AddListener(action); // 실제 행동 연결
            button.gameObject.AddComponent<EconomyCancelRelay>().Initialize(this); // 취소 입력 연결
            return button; // 준비된 버튼 전달
        } // 생성 끝
        public void OnCancel(BaseEventData eventData) // ESC와 게임패드 취소 입력
        { // 취소 시작
            eventData.Use(); // 아래 화면으로의 입력 전파 차단
            if (modal != null) // 유물 선택 또는 확인 검사
            { // 모달 취소 시작
                RemoveModal(); // 선택을 취소하고 경제 화면 복구
                return; // 인벤토리 닫기 차단
            } // 모달 취소 끝
            Close(); // 경제 화면 닫고 인벤토리 복귀
        } // 취소 끝
        private void Close() // 경제 화면 종료
        { // 종료 시작
            if (closed || modal != null) // 중복 종료와 확인 화면 검사
            { // 종료 거절 시작
                return; // 확인 중 아래 화면 복귀 차단
            } // 종료 거절 끝
            closed = true; // 중복 종료 방지
            gameObject.SetActive(false); // 아래 화면 입력 전 화면 숨김
            Destroy(gameObject); // 경제 화면 정리
            onClose(); // 기존 인벤토리 복귀 알림
        } // 종료 끝
        private void SelectOffer(int index) // 현재 페이지의 실제 상품 선택
        { // 선택 시작
            var actual = offerPage * offers.Length + index; // 실제 상품 목록 위치
            var stock = owner.Shop.Snapshot(); // 현재 고정된 상점 재고
            if (actual < 0 || actual >= stock.offers.Length) // 상품 범위 검사
            { // 범위 오류 시작
                return; // 없는 상품 선택 차단
            } // 범위 오류 끝
            selectedOffer = actual; // 실제 상품 번호 보관
            Refresh(); // 선택과 최신 재고 표시
        } // 선택 끝
        private void SelectInventory(int index) // 판매할 실제 인벤토리 슬롯 선택
        { // 선택 시작
            if (index < 0 || index >= owner.Inventory.Slots.Count || owner.Inventory.Slots[index].IsEmpty) // 실제 보유 슬롯 검사
            { // 선택 오류 시작
                notice.text = "판매할 보유 아이템을 선택하세요"; // 빈 슬롯 안내
                return; // 없는 아이템 선택 차단
            } // 선택 오류 끝
            selectedSlot = index; // 실제 슬롯 번호 보관
            Refresh(); // 선택 표시 갱신
        } // 선택 끝
        private void ChangePage(int delta) // 상점 상품 목록 페이지 변경
        { // 변경 시작
            var stock = owner.Shop.Snapshot(); // 현재 상품 목록
            var last = Math.Max(0, (stock.offers.Length - 1) / offers.Length); // 마지막 상품 페이지
            offerPage = Math.Max(0, Math.Min(last, offerPage + delta)); // 유효한 페이지로 제한
            Refresh(); // 현재 페이지 상품 표시
        } // 변경 끝
        private void Buy() // 선택 상품 구매
        { // 구매 시작
            owner.TryBuyShopItem(selectedOffer, out var message); // 실제 구매와 저장 요청
            Refresh(); // 변경된 골드와 재고 표시
            notice.text = message; // 성공 또는 실패 안내
        } // 구매 끝
        private void Sell() // 선택 상품 기준의 보유 아이템 판매
        { // 판매 시작
            owner.TrySellShopItem(selectedOffer, selectedSlot, out var message); // 실제 판매와 저장 요청
            Refresh(); // 변경된 골드와 보유품 표시
            notice.text = message; // 성공 또는 실패 안내
        } // 판매 끝
        private void AcquireRelic(string id) // 선택한 시험 유물 획득
        { // 획득 시작
            if (!trialRelicsEnabled) // 상인 서비스의 개발용 지급 검사
            { // 지급 차단 시작
                return; // 상인 화면에서 무료 유물 지급 차단
            } // 지급 차단 끝
            if (owner.Relics.Count < RelicState.Capacity || owner.Relics.Contains(id)) // 여유 공간 또는 중복 검사
            { // 직접 획득 시작
                owner.TryAcquireRelic(id, null, out var message); // 실제 획득과 중복 거절 요청
                Refresh(); // 현재 유물과 자산 표시
                notice.text = message; // 성공 또는 실패 안내
                return; // 교체 선택 제외
            } // 직접 획득 끝
            incomingRelic = id; // 선택한 새 유물 보관
            ShowRelicSelection(); // 명시적인 기존 유물 선택
        } // 획득 끝
        private GameObject CreateModal(string name) // 아래 화면을 차단하는 전체 확인 화면
        { // 생성 시작
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 모달과 입력 차단 배경
            panel.transform.SetParent(canvasRoot, false); // 경제 화면 위에 표시
            Stretch(panel.GetComponent<RectTransform>()); // 전체 화면 배경
            panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 0.98f); // 불투명 확인 배경
            UiFactory.Decorate(panel.GetComponent<Image>(), "DialoguePanel"); // 화면 용도별 그림 장식
            mainGroup.interactable = false; // 아래 화면의 키보드와 버튼 입력 잠금
            return panel; // 준비된 확인 화면
        } // 생성 끝
        private void ShowRelicSelection() // 교체할 기존 유물 선택
        { // 선택 시작
            RemoveModal(); // 이전 확인 화면 정리
            modal = CreateModal("RelicReplacementSelection"); // 기존 유물 선택 화면
            var incoming = TrialRelicCatalog.Find(incomingRelic); // 선택한 새 시험 유물
            UiFactory.CreateText(modal.transform, "RelicReplacementTitle", "교체할 유물을 선택하세요\n새 유물: " + (incoming == null ? "알 수 없음" : incoming.Name), 30, new Vector2(0f, 320f), new Vector2(1400f, 100f), Color.white); // 새 유물과 선택 안내
            var current = owner.Relics.SaveIds(); // 현재 보유 유물의 독립 목록
            for (var index = 0; index < current.Length; index++) // 실제 보유 유물 순회
            { // 선택 버튼 생성 시작
                var id = current[index]; // 선택할 기존 유물
                var definition = TrialRelicCatalog.Find(id); // 현재 시험 유물 정의
                MakeButton(modal.transform, "RelicReplacement_" + index, RelicLabel(definition), new Vector2(0f, 220f - index * 80f), new Vector2(700f, 60f), () => ShowRelicConfirmation(id)); // 명시적인 보유 유물 선택
            } // 선택 버튼 생성 끝
            var cancel = MakeButton(modal.transform, "CancelRelicReplacement", "취소", new Vector2(0f, -310f), new Vector2(380f, 60f), RemoveModal); // 기존 상태 유지 버튼
            if (EventSystem.current != null) // 입력 관리자 존재 검사
            { // 기본 선택 시작
                EventSystem.current.SetSelectedGameObject(cancel.gameObject); // 취소 기본 선택
            } // 기본 선택 끝
        } // 선택 끝
        private void ShowRelicConfirmation(string id) // 선택한 유물의 교체 전 최종 확인
        { // 확인 시작
            if (!owner.Relics.Contains(id) || incomingRelic == null || owner.Relics.Contains(incomingRelic)) // 실제 기존 유물과 후보 검사
            { // 후보 오류 시작
                RemoveModal(); // 기존 화면 복구
                notice.text = "유물 상태가 변경되어 교체를 취소했습니다"; // 변경 충돌 안내
                return; // 잘못된 유물 교체 차단
            } // 후보 오류 끝
            existingRelic = id; // 확인할 기존 유물 식별자
            expectedRelics = owner.Relics.SaveIds(); // 확인 당시 보유 목록 복사
            if (modal != null) // 이전 선택 화면 검사
            { // 화면 교체 시작
                modal.SetActive(false); // 이전 선택 화면 즉시 숨김
                Destroy(modal); // 이전 선택 화면 정리
            } // 화면 교체 끝
            modal = CreateModal("RelicReplacementConfirmation"); // 교체 최종 확인 화면
            UiFactory.CreateText(modal.transform, "RelicReplacementQuestion", RelicLabel(TrialRelicCatalog.Find(existingRelic)) + "\n버리고\n" + RelicLabel(TrialRelicCatalog.Find(incomingRelic)) + "\n획득할까요?", 29, new Vector2(0f, 100f), new Vector2(1600f, 230f), Color.white); // 실제 교체 전 양쪽 효과 비교
            var cancel = MakeButton(modal.transform, "CancelRelicConfirmation", "아니요", new Vector2(-240f, -180f), new Vector2(380f, 60f), RemoveModal); // 교체 취소 버튼
            MakeButton(modal.transform, "ConfirmRelicReplacement", "교체", new Vector2(240f, -180f), new Vector2(380f, 60f), ConfirmRelicReplacement); // 명시적인 최종 교체 버튼
            if (EventSystem.current != null) // 입력 관리자 존재 검사
            { // 기본 선택 시작
                EventSystem.current.SetSelectedGameObject(cancel.gameObject); // 아니요 기본 선택
            } // 기본 선택 끝
        } // 확인 끝
        private void ConfirmRelicReplacement() // 확인한 유물의 실제 저장 교체
        { // 교체 시작
            var current = owner.Relics.SaveIds(); // 실제 현재 보유 유물
            if (expectedRelics == null || current.Length != expectedRelics.Length) // 확인 전후 보유 수 검사
            { // 변경 오류 시작
                RemoveModal(); // 기존 화면 복구
                notice.text = "유물 상태가 변경되어 교체를 취소했습니다"; // 변경 충돌 안내
                return; // 예기치 않은 교체 차단
            } // 변경 오류 끝
            for (var index = 0; index < current.Length; index++) // 확인한 유물 목록 순회
            { // 항목 검사 시작
                if (current[index] != expectedRelics[index]) // 확인 전후 유물 비교
                { // 변경 감지 시작
                    RemoveModal(); // 기존 화면 복구
                    notice.text = "유물 상태가 변경되어 교체를 취소했습니다"; // 변경 충돌 안내
                    return; // 다른 유물의 삭제 차단
                } // 변경 감지 끝
            } // 항목 검사 끝
            owner.TryAcquireRelic(incomingRelic, existingRelic, out var message); // 확인한 유물의 실제 교체와 저장
            RemoveModal(); // 확인 화면 정리
            Refresh(); // 보유 유물과 골드 갱신
            notice.text = message; // 성공 또는 실패 안내
        } // 교체 끝
        private void RemoveModal() // 유물 선택과 확인 화면 닫기
        { // 제거 시작
            if (modal != null) // 실제 모달 존재 검사
            { // 모달 제거 시작
                modal.SetActive(false); // 아래 화면 입력 전 모달 숨김
                Destroy(modal); // 기존 확인 화면 정리
                modal = null; // 화면 참조 해제
            } // 모달 제거 끝
            mainGroup.interactable = true; // 경제 화면 입력 복구
            existingRelic = null; // 확인한 기존 유물 해제
            expectedRelics = null; // 확인 목록 해제
            if (EventSystem.current != null && closeButton != null) // 입력 관리자와 복귀 버튼 검사
            { // 선택 복구 시작
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject); // 안전한 기본 선택 복구
            } // 선택 복구 끝
        } // 제거 끝
        private static string ItemName(string id) // 등록된 소비품 또는 장비 이름 조회
        { // 조회 시작
            var consumable = TrialConsumableRules.Find(id); // 시험 소비품 정의
            if (consumable != null) // 소비품 정의 검사
            { // 소비품 반환 시작
                return consumable.Name; // 실제 소비품 이름
            } // 소비품 반환 끝
            var equipment = EquipmentCatalog.Find(id); // 시험 장비 정의
            return equipment == null ? id : equipment.Name; // 실제 장비 이름 또는 식별자
        } // 조회 끝
        private static string ItemEffects(string id) // 시험 상품의 기본 능력치 표시
        { // 표시 시작
            var consumable = TrialConsumableRules.Find(id); // 시험 소비품 조회
            if (consumable != null) // 소비품 존재 검사
            { // 소비품 표시 시작
                return " · " + (consumable.Resource == TrialConsumableResource.Health ? "체력" : "마나") + "+" + consumable.Recovery; // 실제 시험 회복 효과
            } // 소비품 표시 끝
            var equipment = EquipmentCatalog.Find(id); // 시험 장비 조회
            if (equipment == null) // 장비 정의 검사
            { // 미등록 상품 시작
                return string.Empty; // 별도 수치 없음
            } // 미등록 상품 끝
            return " · 공격" + equipment.AttackBonus + " 방어" + equipment.DefenseBonus + " 속도" + equipment.SpeedBonus + " 가방+" + equipment.CapacityBonus; // 실제 기본 시험 능력치
        } // 표시 끝
        private static string RelicLabel(RelicDefinition relic) // 시험 유물의 실제 능력치 표시
        { // 표시 시작
            return relic == null ? "유물 정보 없음" : relic.Name + " · 공격+" + relic.AttackBonus + " 방어+" + relic.DefenseBonus + " 속도+" + relic.SpeedBonus; // 임의 시험 효과 공개
        } // 표시 끝
        private void Refresh() // 실제 상점과 인벤토리 상태 표시
        { // 갱신 시작
            var stock = owner.Shop.Snapshot(); // 고정 가격과 현재 재고
            wallet.text = "골드 " + owner.Gold + " · 유물 " + owner.Relics.Count + " / " + RelicState.Capacity + " · 상품 " + (selectedOffer < 0 ? "미선택" : (selectedOffer + 1).ToString()) + " · 소지품 " + (selectedSlot < 0 ? "미선택" : (selectedSlot + 1).ToString()); // 실제 자산과 선택 상태
            for (var index = 0; index < offers.Length; index++) // 한 페이지의 상품 순회
            { // 상품 표시 시작
                var actual = offerPage * offers.Length + index; // 실제 상품 번호
                var available = actual < stock.offers.Length; // 표시할 상품 검사
                offers[index].gameObject.SetActive(available); // 없는 상품 숨김
                if (!available) // 상품 부재 검사
                { // 상품 부재 시작
                    continue; // 없는 상품 접근 차단
                } // 상품 부재 끝
                var offer = stock.offers[actual]; // 실제 상품과 재고
                var traits = offer.equipment == null ? string.Empty : " · " + TrialEquipmentTraits.Describe(offer.equipment); // 장비의 공개 시험 효과
                offers[index].transform.Find("Label").GetComponent<Text>().text = (actual == selectedOffer ? "▶ " : "") + ItemName(offer.itemId) + " · " + offer.price + "G · 재고 " + offer.remaining + ItemEffects(offer.itemId) + traits; // 실제 상품 가격과 재고 및 효과
            } // 상품 표시 끝
            for (var index = 0; index < inventory.Length; index++) // 최대 소지품 슬롯 순회
            { // 슬롯 표시 시작
                var available = index < owner.Inventory.Slots.Count; // 실제 가방 용량 검사
                inventory[index].gameObject.SetActive(available); // 없는 슬롯 숨김
                if (!available) // 슬롯 부재 검사
                { // 슬롯 부재 시작
                    continue; // 없는 슬롯 접근 차단
                } // 슬롯 부재 끝
                var slot = owner.Inventory.Slots[index]; // 현재 보유 아이템
                inventory[index].transform.Find("Label").GetComponent<Text>().text = (index == selectedSlot ? "▶ " : "") + (index + 1) + ". " + (slot.IsEmpty ? "비어 있음" : slot.Name + " × " + slot.Quantity); // 실제 수량과 선택 표시
            } // 슬롯 표시 끝
            for (var index = 0; index < relics.Length; index++) // 시험 유물 버튼 순회
            { // 유물 표시 시작
                if (relics[index] == null) // 등록되지 않은 시험 버튼 검사
                { // 버튼 부재 시작
                    continue; // 없는 버튼 접근 차단
                } // 버튼 부재 끝
                var ids = TrialRelicCatalog.Ids(); // 실제 시험 유물 목록
                var relic = TrialRelicCatalog.Find(ids[index]); // 시험 유물 정의
                relics[index].transform.Find("Label").GetComponent<Text>().text = RelicLabel(relic) + (owner.Relics.Contains(ids[index]) ? " · 보유" : ""); // 시험 효과와 보유 표시
            } // 유물 표시 끝
        } // 갱신 끝
    } // 화면 클래스 끝
    public sealed class EconomyCancelRelay : MonoBehaviour, ICancelHandler // 경제 버튼의 취소 입력 중계
    { // 중계 클래스 시작
        private EconomyScreenView screen; // 실제 경제 화면
        public void Initialize(EconomyScreenView target) // 취소 대상 연결
        { // 연결 시작
            screen = target; // 경제 화면 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // 버튼에서 취소 입력 처리
        { // 처리 시작
            if (screen != null) // 실제 화면 존재 검사
            { // 화면 검사 시작
                screen.OnCancel(eventData); // 경제 화면 취소 전달
            } // 화면 검사 끝
        } // 처리 끝
    } // 중계 클래스 끝
} // 이름 공간 끝
