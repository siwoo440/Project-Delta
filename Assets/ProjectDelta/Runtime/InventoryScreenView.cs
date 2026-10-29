using ProjectDelta.Core; // 인벤토리 슬롯 형식
using UnityEngine; // 실제 화면 객체
using UnityEngine.EventSystems; // 화면 취소 입력
using UnityEngine.UI; // 화면 배경과 글자
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class InventoryScreenView : MonoBehaviour, ICancelHandler // 탐험 중 인벤토리 화면
    { // 클래스 시작
        private AppRoot owner; // 현재 게임의 인벤토리 관리자
        private readonly Text[] slots = new Text[23]; // 최대 슬롯 표시 목록
        private readonly Button[] slotButtons = new Button[23]; // 최대 슬롯 선택 버튼 목록
        private readonly Button[] equipmentButtons = new Button[10]; // 장비 지급과 장착 및 해제 버튼
        private Button trialVariantButton; // 시험 희귀 저주 장비 지급 버튼
        private Button purifyButton; // 선택 저주 장비 정화 버튼
        private Button economyButton; // 시험 상점과 유물 화면 버튼
        private EconomyScreenView economyScreen; // 현재 열린 독립 경제 화면
        private Text equipmentText; // 현재 장착 장비 표시
        private Text selectedDetails; // 선택 장비의 공개 효과 표시
        private enum OfferStage // 가득 찬 인벤토리 획득 단계
        { // 단계 시작
            None, // 일반 인벤토리
            Choice, // 새 아이템 획득 선택
            Select, // 기존 슬롯 선택
            Confirm, // 교체 최종 확인
            Discard, // 보유 아이템 버리기 확인
            Preview, // 시험 장비의 획득 전 효과 확인
            Purify // 선택 장비의 정화 확인
        } // 단계 끝
        private OfferStage offerStage; // 현재 획득 선택 단계
        private Transform canvasRoot; // 획득 안내를 표시할 화면
        private GameObject offerPanel; // 획득 선택 또는 확인 화면
        private Button pickupButton; // 시험 아이템 획득 버튼
        private Button keyButton; // 시험 열쇠 지급 버튼
        private Button closeButton; // 인벤토리 닫기 버튼
        private Button potionButton; // 시험 회복약 지급 버튼
        private Button usePotionButton; // 시험 회복약 사용 버튼
        private Button largePotionButton; // 시험 대형 회복약 획득 버튼
        private Button manaPotionButton; // 시험 마나 회복약 획득 버튼
        private Button useSelectedButton; // 선택한 소비품 사용 버튼
        private Button discardButton; // 선택한 아이템 버리기 버튼
        private Text healthText; // 현재 플레이어 체력 표시
        private string offeredId; // 공간 부족으로 대기 중인 아이템 식별자
        private string offeredName; // 대기 중인 아이템 이름
        private int offeredLimit; // 대기 중인 아이템 최대 중첩
        private EquipmentVariantData offeredEquipment; // 확인한 시험 개별 장비
        private string expectedEquipment; // 확인 당시 개별 장비의 전체 내용
        private int lastPotionFrame = -1; // 같은 프레임 회복약 중복 지급 차단
        private int lastUseFrame = -1; // 같은 프레임 회복약 중복 사용 차단
        private Button cancelTargetButton; // 교체 대상 선택 취소 버튼
        private int replaceIndex = -1; // 교체 확인 대상 슬롯
        private InventorySlot expectedSlot; // 교체 선택 당시의 기존 슬롯
        private string expectedId; // 교체 선택 당시의 아이템 식별자
        private string expectedName; // 교체 선택 당시의 아이템 이름
        private int expectedQuantity; // 교체 선택 당시의 아이템 수량
        private int expectedLimit; // 교체 선택 당시의 중첩 한도
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
                view.canvasRoot = canvas.transform; // 획득 선택 화면 연결
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
                view.healthText = UiFactory.CreateText(canvas.transform, "InventoryHealth", "", 22, new Vector2(0f, 300f), new Vector2(800f, 50f), Color.white); // 현재 체력 표시
                for (var index = 0; index < view.slots.Length; index++) // 최대 슬롯 수만큼 반복
                { // 슬롯 화면 생성 시작
                    var x = -744f + (index / 5) * 372f; // 세로 순서로 배치한 다섯 열의 가로 위치
                    var y = 240f - (index % 5) * 65f; // 인접 번호를 아래로 배치한 세로 위치
                    var slotIndex = index; // 버튼별 슬롯 번호 보관
                    var slotButton = UiFactory.CreateButton(canvas.transform, "InventorySlotButton_" + index, "", new Vector2(x, y)); // 선택 가능한 아이템 슬롯
                    slotButton.GetComponent<RectTransform>().sizeDelta = new Vector2(350f, 56f); // 실제 슬롯 버튼 크기
                    var label = slotButton.transform.Find("Label"); // 슬롯 글자 객체
                    label.name = "InventorySlot_" + index; // 기존 슬롯 글자 이름 유지
                    label.GetComponent<RectTransform>().sizeDelta = new Vector2(350f, 56f); // 슬롯 글자 표시 영역
                    view.slots[index] = label.GetComponent<Text>(); // 실제 슬롯 글자 연결
                    view.slots[index].fontSize = 24; // 슬롯 글자 크기
                    view.slotButtons[index] = slotButton; // 슬롯 버튼 참조 보관
                    slotButton.onClick.AddListener(() => view.SelectSlot(slotIndex)); // 키보드와 게임패드 슬롯 선택
                    slotButton.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 슬롯 취소 입력 연결
                    slotButton.gameObject.AddComponent<InventorySlotDragHandler>().Initialize(view, slotIndex); // 마우스 드래그 연결
                } // 슬롯 화면 생성 끝
                view.selectedDetails = UiFactory.CreateText(canvas.transform, "SelectedEquipmentDetails", "슬롯 선택 후 이동 · 마우스 드래그 지원", 20, new Vector2(0f, -205f), new Vector2(1800f, 36f), accent); // 선택 장비의 공개 효과 안내
                var pickup = UiFactory.CreateButton(canvas.transform, "TrialPickupButton", "시험 아이템 획득", new Vector2(-700f, -365f)); // 시험 획득 버튼
                view.pickupButton = pickup; // 시험 획득 버튼 보관
                pickup.onClick.AddListener(view.AddTrialItem); // 시험 아이템 획득 연결
                pickup.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 선택 버튼의 취소 입력 연결
                var key = UiFactory.CreateButton(canvas.transform, "TrialKeyButton", "시험 열쇠 지급", new Vector2(-350f, -365f)); // 시험 열쇠 지급 버튼
                view.keyButton = key; // 시험 열쇠 버튼 보관
                key.onClick.AddListener(view.AddTrialKey); // 시험 열쇠 지급 연결
                key.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 선택 버튼의 취소 입력 연결
                var close = UiFactory.CreateButton(canvas.transform, "CloseInventoryButton", "닫기", new Vector2(700f, -365f)); // 탐험 복귀 버튼
                view.closeButton = close; // 닫기 버튼 보관
                close.onClick.AddListener(owner.CloseInventory); // 인벤토리 닫기 연결
                close.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 선택 버튼의 취소 입력 연결
                view.potionButton = UiFactory.CreateButton(canvas.transform, "TrialPotionButton", "시험 회복약 지급", new Vector2(0f, -365f)); // 시험 회복약 지급 버튼
                view.potionButton.onClick.AddListener(view.AddTrialPotion); // 시험 회복약 지급 연결
                view.potionButton.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 회복약 버튼 취소 입력 연결
                view.usePotionButton = UiFactory.CreateButton(canvas.transform, "UsePotionButton", "소형 회복약 사용", new Vector2(350f, -365f)); // 시험 회복약 사용 버튼
                view.usePotionButton.onClick.AddListener(view.UseTrialPotion); // 시험 회복약 사용 연결
                view.usePotionButton.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 사용 버튼 취소 입력 연결
                view.largePotionButton = UiFactory.CreateButton(canvas.transform, "TrialLargePotionButton", "시험 대형 회복약", new Vector2(-525f, -475f)); // 대형 회복약 시험 획득
                view.largePotionButton.onClick.AddListener(() => view.AddTrialConsumable("trial-large-potion")); // 대형 회복약 지급 연결
                view.manaPotionButton = UiFactory.CreateButton(canvas.transform, "TrialManaPotionButton", "시험 마나 회복약", new Vector2(-175f, -475f)); // 마나 회복약 시험 획득
                view.manaPotionButton.onClick.AddListener(() => view.AddTrialConsumable("trial-mana-potion")); // 마나 회복약 지급 연결
                view.useSelectedButton = UiFactory.CreateButton(canvas.transform, "UseSelectedItemButton", "선택 아이템 사용", new Vector2(175f, -475f)); // 선택한 소비품 사용
                view.useSelectedButton.onClick.AddListener(view.UseSelectedItem); // 실제 선택 사용 연결
                view.discardButton = UiFactory.CreateButton(canvas.transform, "DiscardSelectedItemButton", "선택 묶음 버리기", new Vector2(525f, -475f)); // 선택한 묶음 버리기
                view.discardButton.onClick.AddListener(view.ShowDiscardConfirmation); // 실제 버리기 확인 연결
                foreach (var button in new[] { view.largePotionButton, view.manaPotionButton, view.useSelectedButton, view.discardButton }) // 추가 아이템 버튼 순회
                { // 버튼 구성 시작
                    ResizeFooter(button); // 기존 행동 버튼 크기 적용
                    button.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 취소 입력 연결
                } // 버튼 구성 끝
                ResizeFooter(pickup); // 시험 획득 버튼 크기 조정
                ResizeFooter(key); // 시험 열쇠 버튼 크기 조정
                ResizeFooter(view.potionButton); // 시험 회복약 버튼 크기 조정
                ResizeFooter(view.usePotionButton); // 회복약 사용 버튼 크기 조정
                ResizeFooter(close); // 닫기 버튼 크기 조정
                view.cancelTargetButton = UiFactory.CreateButton(canvas.transform, "CancelTargetButton", "교체 취소", new Vector2(0f, -365f)); // 교체 대상 선택 취소 버튼
                view.cancelTargetButton.onClick.AddListener(view.ShowOfferChoice); // 획득 선택으로 복귀
                view.cancelTargetButton.gameObject.AddComponent<InventoryCancelRelay>().Initialize(view); // 취소 입력 연결
                view.cancelTargetButton.gameObject.SetActive(false); // 일반 인벤토리에서 숨김
                view.equipmentText = UiFactory.CreateText(canvas.transform, "EquipmentStatus", "", 20, new Vector2(0f, -155f), new Vector2(1800f, 48f), Color.white); // 장착 부위와 현재 장비 안내
                view.AddEquipmentButton(0, EquipmentCatalog.BasicSwordId, "시험 기본 검"); // 시험 기본 검 지급 버튼
                view.AddEquipmentButton(1, EquipmentCatalog.BasicAxeId, "시험 기본 도끼"); // 시험 기본 도끼 지급 버튼
                view.AddEquipmentButton(2, EquipmentCatalog.LightArmorId, "시험 경갑"); // 시험 경갑 지급 버튼
                view.AddEquipmentButton(3, EquipmentCatalog.OldBagId, "시험 낡은 주머니"); // 시험 가방 지급 버튼
                view.AddEquipmentButton(9, EquipmentCatalog.CombatAccessoryId, "시험 전투 장신구"); // 시험 장신구 지급 버튼
                view.equipmentButtons[4] = view.CreateEquipmentAction("EquipSelectedButton", "선택 장비 장착", view.EquipSelected); // 선택 장비 장착 버튼
                view.equipmentButtons[5] = view.CreateEquipmentAction("UnequipWeaponButton", "무기 해제", () => view.Unequip(EquipmentSlot.Weapon)); // 무기 해제 버튼
                view.equipmentButtons[6] = view.CreateEquipmentAction("UnequipArmorButton", "방어구 해제", () => view.Unequip(EquipmentSlot.Armor)); // 방어구 해제 버튼
                view.equipmentButtons[7] = view.CreateEquipmentAction("UnequipAccessoryButton", "장신구 해제", () => view.Unequip(EquipmentSlot.Accessory)); // 장신구 해제 버튼
                view.equipmentButtons[8] = view.CreateEquipmentAction("UnequipBagButton", "가방 해제", () => view.Unequip(EquipmentSlot.Bag)); // 가방 해제 버튼
                view.trialVariantButton = view.CreateEquipmentAction("TrialVariantButton", "시험 희귀 저주 검", view.ShowTrialVariant); // 시험 장비의 획득 전 확인
                view.purifyButton = view.CreateEquipmentAction("PurifySelectedButton", "선택 장비 정화", view.ShowPurifyConfirmation); // 선택 장비의 정화 확인
                view.economyButton = view.CreateEquipmentAction("OpenEconomyButton", "시험 상점 · 유물", view.OpenTrialEconomy); // 독립 시험 경제 화면 진입
                var controls = new[] { pickup, key, view.potionButton, view.usePotionButton, close, view.largePotionButton, view.manaPotionButton, view.useSelectedButton, view.discardButton, view.equipmentButtons[4], view.equipmentButtons[0], view.equipmentButtons[1], view.equipmentButtons[2], view.equipmentButtons[3], view.equipmentButtons[9], view.equipmentButtons[5], view.equipmentButtons[6], view.equipmentButtons[7], view.equipmentButtons[8], view.trialVariantButton, view.purifyButton, view.economyButton }; // 다섯 줄의 모든 행동 버튼
                for (var index = 0; index < controls.Length; index++) // 행동 버튼 순회
                { // 행동 버튼 배치 시작
                    controls[index].GetComponent<RectTransform>().anchoredPosition = new Vector2(-760f + (index % 5) * 380f, -245f - (index / 5) * 53f); // 다섯 열 다섯 줄 행동 버튼 배치
                    ResizeFooter(controls[index]); // 공통 버튼 크기 적용
                } // 행동 버튼 배치 끝
                view.cancelTargetButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -470f); // 교체 대상 선택 취소 위치
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
            if (offerStage == OfferStage.Preview || offerStage == OfferStage.Purify) // 장비 획득과 정화 확인 검사
            { // 장비 확인 취소 시작
                CancelEquipmentConfirmation(); // 기존 상태 보존과 확인 취소
                return; // 다른 취소 처리 차단
            } // 장비 확인 취소 끝
            if (offerStage == OfferStage.Discard) // 보유 아이템 버리기 확인 검사
            { // 버리기 취소 시작
                CancelDiscard(); // 아이템 보존과 확인 취소
                return; // 다른 취소 처리 차단
            } // 버리기 취소 끝
            if (offerStage == OfferStage.Select || offerStage == OfferStage.Confirm) // 교체 대상 또는 확인 단계 검사
            { // 교체 취소 조건 시작
                ShowOfferChoice(); // 원래 획득 선택으로 복귀
                return; // 인벤토리와 아이템 유지
            } // 교체 취소 조건 끝
            if (offerStage == OfferStage.Choice) // 새 아이템 획득 선택 검사
            { // 획득 취소 조건 시작
                LeaveOffer(); // 새 아이템을 두고 인벤토리 복귀
                return; // 기존 아이템 유지
            } // 획득 취소 조건 끝
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
            if (offerStage == OfferStage.Select) // 교체 대상 슬롯 선택 검사
            { // 교체 대상 선택 시작
                SelectReplacement(index); // 기존 묶음의 교체 확인 요청
                return; // 일반 슬롯 이동 차단
            } // 교체 대상 선택 끝
            if (offerStage != OfferStage.None) // 다른 획득 화면 검사
            { // 획득 화면 조건 시작
                return; // 확인 중 일반 슬롯 이동 차단
            } // 획득 화면 조건 끝
            if (draggedSlot >= 0 || lastSlotFrame == Time.frameCount || index < 0 || index >= owner.Inventory.Slots.Count) // 드래그와 중복 선택 및 슬롯 범위 검사
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
            if (offerStage != OfferStage.None || index < 0 || index >= owner.Inventory.Slots.Count || owner.Inventory.Slots[index].IsEmpty) // 획득 단계와 출발 슬롯 검사
            { // 잘못된 출발 조건 시작
                return; // 빈 슬롯의 드래그 차단
            } // 잘못된 출발 조건 끝
            selectedSlot = -1; // 이전 키보드 선택 취소
            draggedSlot = index; // 드래그 출발 슬롯 보관
            Refresh(); // 선택 표시 갱신
        } // 드래그 시작 끝
        public void DropSlot(int source, int destination) // 마우스 드래그 슬롯 교환
        { // 놓기 시작
            if (draggedSlot != source || source < 0 || source >= owner.Inventory.Slots.Count || destination < 0 || destination >= owner.Inventory.Slots.Count) // 현재 드래그와 도착 범위 검사
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
        private void SetBaseButtons(bool slotsEnabled, bool footerEnabled) // 획득 단계의 기존 버튼 잠금
        { // 설정 시작
            foreach (var button in slotButtons) // 모든 슬롯 버튼 순회
            { // 슬롯 설정 시작
                button.interactable = slotsEnabled; // 슬롯 선택 가능 여부
            } // 슬롯 설정 끝
            pickupButton.interactable = footerEnabled; // 시험 획득 가능 여부
            keyButton.interactable = footerEnabled; // 시험 열쇠 가능 여부
            closeButton.interactable = footerEnabled; // 인벤토리 닫기 가능 여부
            potionButton.interactable = footerEnabled; // 시험 회복약 지급 가능 여부
            usePotionButton.interactable = footerEnabled; // 시험 회복약 사용 가능 여부
            largePotionButton.interactable = footerEnabled; // 대형 회복약 지급 가능 여부
            manaPotionButton.interactable = footerEnabled; // 마나 회복약 지급 가능 여부
            useSelectedButton.interactable = footerEnabled; // 선택 소비품 사용 가능 여부
            discardButton.interactable = footerEnabled; // 선택 아이템 버리기 가능 여부
            trialVariantButton.interactable = footerEnabled; // 시험 장비 지급 가능 여부
            purifyButton.interactable = footerEnabled; // 장비 정화 가능 여부
            economyButton.interactable = footerEnabled; // 시험 상점 진입 가능 여부
            foreach (var button in equipmentButtons) // 장비 버튼 순회
            { // 장비 버튼 설정 시작
                button.interactable = footerEnabled; // 모달 중 장비 행동 잠금
            } // 장비 버튼 설정 끝
        } // 설정 끝
        private void RemoveOfferPanel() // 기존 획득 선택 화면 제거
        { // 제거 시작
            if (offerPanel != null) // 기존 화면 존재 검사
            { // 화면 제거 시작
                offerPanel.SetActive(false); // 기존 화면 즉시 숨김
                Destroy(offerPanel); // 기존 화면 정리
                offerPanel = null; // 화면 참조 해제
            } // 화면 제거 끝
        } // 제거 끝
        private GameObject CreateOfferPanel() // 획득 선택의 가림 화면 생성
        { // 생성 시작
            var panel = new GameObject("InventoryOfferPanel", typeof(RectTransform), typeof(Image)); // 선택 화면과 배경
            panel.transform.SetParent(canvasRoot, false); // 인벤토리 화면 위에 배치
            var rect = panel.GetComponent<RectTransform>(); // 화면 영역 조회
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 0.97f); // 선택 화면 배경색
            return panel; // 준비된 화면 전달
        } // 생성 끝
        private void ShowOfferChoice() // 새 아이템 획득 선택 표시
        { // 선택 화면 시작
            RemoveOfferPanel(); // 이전 선택 화면 정리
            selectedSlot = -1; // 기존 슬롯 이동 선택 해제
            draggedSlot = -1; // 기존 드래그 해제
            Refresh(); // 이전 이동 선택 표시 제거
            offerStage = OfferStage.Choice; // 획득 선택 단계 설정
            cancelTargetButton.gameObject.SetActive(false); // 대상 취소 버튼 숨김
            SetBaseButtons(false, false); // 선택 화면 아래 입력 잠금
            offerPanel = CreateOfferPanel(); // 획득 선택 화면 생성
            UiFactory.CreateText(offerPanel.transform, "FullOfferMessage", "가방이 가득 찼습니다\n" + offeredName + "을 어떻게 할까요?", 36, new Vector2(0f, 100f), new Vector2(1400f, 160f), Color.white); // 획득 선택 안내
            var leave = UiFactory.CreateButton(offerPanel.transform, "LeaveItemButton", "두고 가기", new Vector2(-250f, -180f)); // 새 아이템 포기 버튼
            leave.onClick.AddListener(LeaveOffer); // 기존 아이템 유지 연결
            leave.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 취소 입력 연결
            var replace = UiFactory.CreateButton(offerPanel.transform, "ReplaceItemButton", "기존 아이템 교체", new Vector2(250f, -180f)); // 기존 아이템 교체 버튼
            replace.onClick.AddListener(ChooseReplacement); // 교체 대상 선택 연결
            replace.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 취소 입력 연결
            EventSystem.current.SetSelectedGameObject(leave.gameObject); // 안전한 기본 선택
        } // 선택 화면 끝
        private void ChooseReplacement() // 교체할 기존 슬롯 선택
        { // 대상 선택 시작
            if (offerStage != OfferStage.Choice) // 현재 획득 단계 검사
            { // 단계 오류 조건 시작
                return; // 잘못된 대상 선택 차단
            } // 단계 오류 조건 끝
            RemoveOfferPanel(); // 획득 선택 화면 숨김
            offerStage = OfferStage.Select; // 교체 대상 선택 단계
            SetBaseButtons(true, false); // 슬롯만 선택 가능
            cancelTargetButton.gameObject.SetActive(true); // 대상 선택 취소 표시
            status.text = "버릴 기존 아이템을 선택하세요"; // 교체 대상 선택 안내
            EventSystem.current.SetSelectedGameObject(slotButtons[0].gameObject); // 첫 슬롯 기본 선택
        } // 대상 선택 끝
        private void SelectReplacement(int index) // 기존 아이템의 교체 확인 준비
        { // 확인 준비 시작
            if (offerStage != OfferStage.Select || index < 0 || index >= owner.Inventory.Slots.Count || owner.Inventory.Slots[index].IsEmpty) // 선택 단계와 기존 아이템 검사
            { // 잘못된 대상 조건 시작
                return; // 빈 슬롯과 잘못된 교체 차단
            } // 잘못된 대상 조건 끝
            replaceIndex = index; // 선택한 기존 슬롯 보관
            expectedSlot = owner.Inventory.Slots[index]; // 확인 전 원본 슬롯 참조
            expectedId = expectedSlot.ItemId; // 기존 아이템 식별자 보관
            expectedName = expectedSlot.Name; // 기존 아이템 이름 보관
            expectedQuantity = expectedSlot.Quantity; // 기존 아이템 수량 보관
            expectedLimit = expectedSlot.StackLimit; // 기존 중첩 한도 보관
            expectedEquipment = EquipmentSnapshot(expectedSlot.Equipment); // 확인 당시 개별 장비 전체 내용
            offerStage = OfferStage.Confirm; // 최종 교체 확인 단계
            cancelTargetButton.gameObject.SetActive(false); // 대상 취소 버튼 숨김
            SetBaseButtons(false, false); // 확인 화면 아래 입력 잠금
            offerPanel = CreateOfferPanel(); // 교체 확인 화면 생성
            UiFactory.CreateText(offerPanel.transform, "ReplaceConfirmMessage", expectedName + " × " + expectedQuantity + " 버리기\n" + offeredName + "으로 교체할까요?", 36, new Vector2(0f, 100f), new Vector2(1400f, 180f), Color.white); // 버릴 묶음과 교체 확인 안내
            var cancel = UiFactory.CreateButton(offerPanel.transform, "CancelReplaceButton", "취소", new Vector2(-250f, -180f)); // 교체 취소 버튼
            cancel.onClick.AddListener(ShowOfferChoice); // 원래 획득 선택으로 복귀
            cancel.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 취소 입력 연결
            var confirm = UiFactory.CreateButton(offerPanel.transform, "ConfirmReplaceButton", "버리고 교체", new Vector2(250f, -180f)); // 명시적인 교체 확인 버튼
            confirm.onClick.AddListener(ConfirmReplacement); // 안전한 최종 교체 연결
            confirm.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 취소 입력 연결
            EventSystem.current.SetSelectedGameObject(cancel.gameObject); // 취소를 기본 선택
        } // 확인 준비 끝
        private void ConfirmReplacement() // 확인한 기존 묶음의 최종 교체
        { // 교체 시작
            if (offerStage != OfferStage.Confirm || replaceIndex < 0 || replaceIndex >= owner.Inventory.Slots.Count) // 현재 확인과 대상 범위 검사
            { // 잘못된 단계 조건 시작
                return; // 잘못된 교체 차단
            } // 잘못된 단계 조건 끝
            var current = owner.Inventory.Slots[replaceIndex]; // 현재 기존 슬롯 조회
            if (!object.ReferenceEquals(current, expectedSlot) || current.ItemId != expectedId || current.Name != expectedName || current.Quantity != expectedQuantity || current.StackLimit != expectedLimit || EquipmentSnapshot(current.Equipment) != expectedEquipment) // 확인 이후 대상과 개별 장비 변경 검사
            { // 대상 변경 조건 시작
                ShowOfferChoice(); // 기존 아이템을 보존하고 선택 재시작
                offerPanel.transform.Find("FullOfferMessage").GetComponent<Text>().text = "기존 아이템이 변경되어 교체를 취소했습니다"; // 표시 중인 화면에 변경 안내
                return; // 예상하지 못한 기존 묶음 삭제 차단
            } // 대상 변경 조건 끝
            var replaced = offeredEquipment == null ? owner.Inventory.TryReplace(replaceIndex, offeredId, offeredName, 1, offeredLimit) : owner.Inventory.TryReplaceWithEquipment(replaceIndex, offeredEquipment); // 확인된 개별 장비 또는 일반 아이템 교체
            if (!replaced) // 실제 아이템 교체 검사
            { // 교체 실패 조건 시작
                ShowOfferChoice(); // 원래 획득 선택 복구
                offerPanel.transform.Find("FullOfferMessage").GetComponent<Text>().text = "아이템을 교체하지 못했습니다"; // 표시 중인 화면에 실패 안내
                return; // 기존 아이템 보존
            } // 교체 실패 조건 끝
            if (offeredId == AppRoot.TrialPotionId) // 확인된 회복약 교체 검사
            { // 회복약 기록 시작
                owner.RecordPotionAcquisition(); // 실제 회복약 획득 기록
            } // 회복약 기록 끝
            else if (TrialConsumableRules.Find(offeredId) != null) // 다른 시험 소비품 교체 검사
            { // 소비품 획득 기록 시작
                owner.RecordExplorationEvent(offeredName + " 1개 획득"); // 확인된 소비품 획득 기록
            } // 소비품 획득 기록 끝
            var acquiredName = offeredName; // 획득한 아이템 이름 보관
            LeaveOffer(); // 교체 완료 후 인벤토리 복귀
            status.text = acquiredName + "을 획득했습니다"; // 실제 획득 결과 안내
        } // 교체 끝
        private void LeaveOffer() // 새 아이템 포기 또는 획득 선택 종료
        { // 종료 시작
            if (offerStage == OfferStage.None) // 현재 선택 존재 검사
            { // 선택 없음 조건 시작
                return; // 중복 종료 차단
            } // 선택 없음 조건 끝
            RemoveOfferPanel(); // 획득 선택 화면 제거
            offerStage = OfferStage.None; // 일반 인벤토리 단계 복구
            offeredId = null; // 완료된 획득 아이템 식별자 해제
            offeredName = null; // 완료된 획득 아이템 이름 해제
            offeredLimit = 0; // 완료된 획득 중첩 한도 해제
            offeredEquipment = null; // 완료된 시험 장비 해제
            cancelTargetButton.gameObject.SetActive(false); // 대상 취소 버튼 숨김
            SetBaseButtons(true, true); // 기존 인벤토리 버튼 복구
            Refresh(); // 실제 슬롯과 보유 상태 갱신
            if (EventSystem.current != null) // 입력 관리자 존재 검사
            { // 선택 복구 시작
                EventSystem.current.SetSelectedGameObject(pickupButton.gameObject); // 시험 획득 버튼 선택 복구
            } // 선택 복구 끝
        } // 종료 끝
        private void AddTrialItem() // 기존 시험 아이템 획득
        { // 획득 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || lastPickupFrame == Time.frameCount) // 획득 화면과 드래그 및 중복 요청 검사
            { // 중복 조건 시작
                return; // 중복 획득 차단
            } // 중복 조건 끝
            lastPickupFrame = Time.frameCount; // 현재 프레임 획득 기록
            var result = owner.Inventory.Add("ITEM_DAY80_TEST_DROP", "전투 드롭 시험 아이템", 1, 1); // 원본 시험 아이템 한 개 획득
            Refresh(); // 실제 슬롯 상태 표시
            if (result.Remaining > 0) // 빈 슬롯 부족 검사
            { // 공간 부족 조건 시작
                offeredId = "ITEM_DAY80_TEST_DROP"; // 대기할 시험 아이템 식별자
                offeredName = "전투 드롭 시험 아이템"; // 대기할 시험 아이템 이름
                offeredLimit = 1; // 시험 아이템 최대 중첩
                ShowOfferChoice(); // 가득 찬 인벤토리의 획득 선택
            } // 공간 부족 조건 끝
        } // 획득 끝
        private static void ResizeFooter(Button button) // 다섯 행동 버튼의 크기 조정
        { // 크기 조정 시작
            var size = new Vector2(340f, 46f); // 행동 버튼의 공통 크기
            button.GetComponent<RectTransform>().sizeDelta = size; // 실제 버튼 크기 적용
            var label = button.transform.Find("Label"); // 버튼 글자 객체
            label.GetComponent<RectTransform>().sizeDelta = size; // 글자 표시 영역 적용
            label.GetComponent<Text>().fontSize = 24; // 긴 버튼 이름의 글자 크기
        } // 크기 조정 끝
        private Button CreateEquipmentAction(string id, string label, UnityEngine.Events.UnityAction action) // 장비 행동 버튼 생성
        { // 생성 시작
            var button = UiFactory.CreateButton(canvasRoot, id, label, Vector2.zero); // 실제 장비 행동 버튼
            button.onClick.AddListener(action); // 장비 행동 연결
            button.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 취소 입력 연결
            return button; // 준비된 버튼 전달
        } // 생성 끝
        private void AddEquipmentButton(int index, string id, string label) // 시험 장비 획득 버튼 생성
        { // 생성 시작
            equipmentButtons[index] = CreateEquipmentAction("TrialEquipmentButton_" + index, label, () => AddTrialEquipment(id)); // 실제 장비 정의 획득 연결
        } // 생성 끝
        private void OpenTrialEconomy() // 인벤토리에서 독립 시험 상점 열기
        { // 열기 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || economyScreen != null) // 다른 선택과 중복 화면 검사
            { // 진입 거절 시작
                return; // 중복 상점 화면 차단
            } // 진입 거절 끝
            if (!owner.EnsureTrialShop(out var message)) // 현재 층의 고정 상품 사전 저장 검사
            { // 준비 오류 시작
                status.text = message; // 상점 준비 실패 안내
                return; // 저장되지 않은 상점 표시 차단
            } // 준비 오류 끝
            gameObject.SetActive(false); // 아래 인벤토리의 모든 입력 차단
            try // 독립 경제 화면 생성
            { // 화면 생성 시작
                economyScreen = EconomyScreenView.Create(owner, CloseTrialEconomy); // 실제 시험 상점과 유물 화면 생성
            } // 화면 생성 끝
            catch (System.Exception) // 화면 생성 실패 검사
            { // 복구 시작
                gameObject.SetActive(true); // 기존 인벤토리 입력 복원
                Refresh(); // 실제 저장된 상태 표시
                status.text = "시험 상점을 열 수 없습니다"; // 화면 준비 오류 안내
            } // 복구 끝
        } // 열기 끝
        private void CloseTrialEconomy() // 경제 화면에서 인벤토리 복귀
        { // 복귀 시작
            economyScreen = null; // 닫힌 화면 참조 해제
            gameObject.SetActive(true); // 기존 인벤토리 복구
            selectedSlot = -1; // 거래 이전 슬롯 선택 해제
            Refresh(); // 거래 후 보유 상태 표시
            if (EventSystem.current != null) // 입력 관리자 검사
            { // 선택 복구 시작
                EventSystem.current.SetSelectedGameObject(economyButton.gameObject); // 이전 상점 버튼 선택
            } // 선택 복구 끝
        } // 복귀 끝
        private void OnDestroy() // 상위 인벤토리 제거 처리
        { // 정리 시작
            if (economyScreen != null) // 열린 경제 화면 검사
            { // 경제 화면 정리 시작
                Destroy(economyScreen.gameObject); // 별도 캔버스와 입력 정리
            } // 경제 화면 정리 끝
        } // 정리 끝
        private static string EquipmentSnapshot(EquipmentVariantData item) // 확인 전후 개별 장비 비교값
        { // 비교값 시작
            return item == null ? string.Empty : JsonUtility.ToJson(item); // 전체 인스턴스와 옵션의 직렬화
        } // 비교값 끝
        private void ShowTrialVariant() // 시험 희귀 저주 검의 획득 전 확인
        { // 확인 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || lastPickupFrame == Time.frameCount) // 다른 입력과 중복 생성 검사
            { // 입력 거절 시작
                return; // 중복 시험 장비 생성 차단
            } // 입력 거절 끝
            lastPickupFrame = Time.frameCount; // 현재 프레임 중복 생성 방지
            offeredEquipment = TrialEquipmentTraits.Create(EquipmentCatalog.BasicSwordId, EquipmentVariantRules.Rare, true); // 공개할 시험 개별 장비 생성
            if (offeredEquipment == null || !TrialEquipmentTraits.IsValid(offeredEquipment)) // 실제 시험 장비 정의 검사
            { // 생성 오류 시작
                offeredEquipment = null; // 불완전한 후보 정리
                status.text = "시험 장비를 준비할 수 없습니다"; // 실제 오류 안내
                return; // 잘못된 장비 표시 차단
            } // 생성 오류 끝
            var definition = EquipmentCatalog.Find(offeredEquipment.definitionId); // 실제 기본 장비 정의
            offeredId = definition.Id; // 공개한 기본 장비 식별자
            offeredName = definition.Name; // 공개한 기본 장비 이름
            offeredLimit = 1; // 단일 장비 중첩 제한
            offerStage = OfferStage.Preview; // 획득 전 공개 확인 단계
            SetBaseButtons(false, false); // 확인 화면 아래 입력 잠금
            offerPanel = CreateOfferPanel(); // 공개 확인 화면 생성
            var details = offeredName + " · 기본 공격 " + definition.AttackBonus + " · 치명타 " + definition.CriticalChance + "% / " + definition.CriticalMultiplierPercent + "%\n" + TrialEquipmentTraits.Describe(offeredEquipment); // 기본 능력치와 모든 시험 효과
            UiFactory.CreateText(offerPanel.transform, "TrialVariantDetails", "획득 전 확인 · 시험값\n" + details, 32, new Vector2(0f, 100f), new Vector2(1600f, 200f), Color.white); // 등급과 공개 저주 효과 표시
            var cancel = UiFactory.CreateButton(offerPanel.transform, "CancelTrialVariantButton", "취소", new Vector2(-250f, -180f)); // 후보 포기 버튼
            var confirm = UiFactory.CreateButton(offerPanel.transform, "AcceptTrialVariantButton", "획득", new Vector2(250f, -180f)); // 공개된 시험 장비 획득 버튼
            cancel.onClick.AddListener(CancelEquipmentConfirmation); // 후보 취소 연결
            confirm.onClick.AddListener(AcceptTrialVariant); // 실제 인벤토리 획득 연결
            cancel.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 취소 입력 연결
            confirm.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 획득 버튼 취소 입력 연결
            EventSystem.current.SetSelectedGameObject(cancel.gameObject); // 취소 기본 선택
        } // 확인 끝
        private void AcceptTrialVariant() // 공개된 동일 시험 장비의 실제 획득
        { // 획득 시작
            if (offerStage != OfferStage.Preview || !TrialEquipmentTraits.IsValid(offeredEquipment)) // 확인 단계와 후보 상태 검사
            { // 후보 오류 시작
                return; // 잘못된 후보 획득 차단
            } // 후보 오류 끝
            if (!owner.Inventory.TryAddEquipment(offeredEquipment)) // 실제 수용 검사
            { // 공간 부족 시작
                ShowOfferChoice(); // 동일 장비를 보존한 교체 또는 포기 선택
                return; // 새로운 장비 재생성 차단
            } // 공간 부족 끝
            var name = offeredName; // 실제 획득 이름 보관
            owner.RecordExplorationEvent(name + " 시험 장비 획득"); // 확인된 장비 획득 기록
            LeaveOffer(); // 후보 해제와 기존 화면 복구
            status.text = name + " 시험 장비를 획득했습니다"; // 실제 획득 안내
        } // 획득 끝
        private void CancelEquipmentConfirmation() // 장비 획득 또는 정화 확인 취소
        { // 취소 시작
            if (offerStage != OfferStage.Preview && offerStage != OfferStage.Purify) // 실제 장비 확인 단계 검사
            { // 단계 오류 시작
                return; // 다른 선택 화면 차단
            } // 단계 오류 끝
            var button = offerStage == OfferStage.Preview ? trialVariantButton : purifyButton; // 확인 전 행동 버튼
            RemoveOfferPanel(); // 확인 화면 제거
            offerStage = OfferStage.None; // 일반 인벤토리 복구
            offeredEquipment = null; // 미획득 후보 해제
            offeredId = null; // 미획득 식별자 해제
            offeredName = null; // 미획득 이름 해제
            offeredLimit = 0; // 미획득 중첩 정보 해제
            SetBaseButtons(true, true); // 기존 입력 복구
            Refresh(); // 보존된 상태 갱신
            EventSystem.current.SetSelectedGameObject(button.gameObject); // 확인 전 버튼 선택 복구
        } // 취소 끝
        private void ShowPurifyConfirmation() // 선택한 저주 장비의 정화 확인
        { // 확인 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || selectedSlot < 0 || selectedSlot >= owner.Inventory.Slots.Count) // 실제 선택과 입력 상태 검사
            { // 선택 오류 시작
                status.text = "정화할 저주 장비를 선택하세요"; // 명시적인 대상 안내
                return; // 잘못된 선택 차단
            } // 선택 오류 끝
            var slot = owner.Inventory.Slots[selectedSlot]; // 선택한 실제 보유 슬롯
            var item = slot.Equipment; // 독립 개별 장비 조회
            if (item == null || !item.cursed || !TrialEquipmentTraits.IsValid(item)) // 저주와 시험 장비 유효성 검사
            { // 대상 오류 시작
                status.text = "정화할 시험 저주 장비가 아닙니다"; // 정화 불가 안내
                return; // 일반 장비와 손상 장비 차단
            } // 대상 오류 끝
            replaceIndex = selectedSlot; // 정화할 실제 슬롯 보관
            expectedSlot = slot; // 확인 당시 슬롯 참조
            expectedId = slot.ItemId; // 확인 당시 기본 식별자
            expectedName = slot.Name; // 확인 당시 장비 이름
            expectedQuantity = slot.Quantity; // 확인 당시 수량
            expectedLimit = slot.StackLimit; // 확인 당시 중첩 한도
            expectedEquipment = EquipmentSnapshot(item); // 확인 당시 개별 장비 전체 내용
            offerStage = OfferStage.Purify; // 정화 확인 단계
            SetBaseButtons(false, false); // 확인 화면 아래 입력 잠금
            offerPanel = CreateOfferPanel(); // 정화 확인 화면 생성
            UiFactory.CreateText(offerPanel.transform, "PurifyQuestion", "시험 정화 · " + slot.Name + "\n" + TrialEquipmentTraits.Describe(item) + "\n정화할까요?", 30, new Vector2(0f, 100f), new Vector2(1600f, 220f), Color.white); // 실제 장비와 시험 효과 표시
            var cancel = UiFactory.CreateButton(offerPanel.transform, "CancelPurifyButton", "아니요", new Vector2(-250f, -180f)); // 정화 취소 버튼
            var confirm = UiFactory.CreateButton(offerPanel.transform, "ConfirmPurifyButton", "정화", new Vector2(250f, -180f)); // 명시적인 정화 버튼
            cancel.onClick.AddListener(CancelEquipmentConfirmation); // 원래 인벤토리 복귀
            confirm.onClick.AddListener(ConfirmPurify); // 확인된 개별 장비 정화
            cancel.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 취소 입력 연결
            confirm.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 확인 버튼 취소 입력 연결
            EventSystem.current.SetSelectedGameObject(cancel.gameObject); // 아니요 기본 선택
        } // 확인 끝
        private void ConfirmPurify() // 확인한 장비의 실제 정화
        { // 정화 시작
            if (offerStage != OfferStage.Purify || replaceIndex < 0 || replaceIndex >= owner.Inventory.Slots.Count) // 확인 단계와 현재 슬롯 검사
            { // 단계 오류 시작
                return; // 잘못된 정화 차단
            } // 단계 오류 끝
            var current = owner.Inventory.Slots[replaceIndex]; // 확인 시점의 현재 보유 슬롯
            if (!object.ReferenceEquals(current, expectedSlot) || current.ItemId != expectedId || current.Name != expectedName || current.Quantity != expectedQuantity || current.StackLimit != expectedLimit || EquipmentSnapshot(current.Equipment) != expectedEquipment) // 원본 슬롯과 전체 장비 변경 검사
            { // 변경 감지 시작
                CancelEquipmentConfirmation(); // 기존 아이템 보존과 확인 종료
                status.text = "장비가 변경되어 정화를 취소했습니다"; // 변경 충돌 안내
                return; // 예상하지 못한 장비 변경 차단
            } // 변경 감지 끝
            var success = owner.TryPurifyInventoryItem(replaceIndex, out var message); // 확인된 개체의 저장 포함 정화
            CancelEquipmentConfirmation(); // 확인 화면 정리
            if (success) // 실제 정화 성공 검사
            { // 선택 해제 시작
                selectedSlot = -1; // 이전 개별 장비 선택 해제
                Refresh(); // 실제 정화 상태 표시
            } // 선택 해제 끝
            status.text = message; // 저장 실패 또는 정화 결과 안내
        } // 정화 끝
        private void AddTrialEquipment(string id) // 등록된 시험 장비 획득
        { // 획득 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || lastPickupFrame == Time.frameCount) // 모달과 중복 획득 검사
            { // 획득 거절 시작
                return; // 중복 요청 차단
            } // 획득 거절 끝
            var item = EquipmentCatalog.Find(id); // 시험 장비 정의 조회
            if (item == null) // 미등록 장비 검사
            { // 정의 오류 시작
                status.text = "시험 장비를 찾을 수 없습니다"; // 정의 오류 안내
                return; // 잘못된 장비 지급 차단
            } // 정의 오류 끝
            lastPickupFrame = Time.frameCount; // 현재 프레임 중복 지급 차단
            var result = owner.Inventory.Add(item.Id, item.Name, 1, 1); // 중첩 없는 시험 장비 지급
            Refresh(); // 보유 상태 갱신
            if (result.Remaining == 0) // 실제 획득 성공 검사
            { // 획득 성공 시작
                owner.RecordExplorationEvent(item.Name + " 1개 획득"); // 실제 장비 획득 기록
                status.text = item.Name + "을 획득했습니다"; // 획득 성공 안내
                return; // 공간 부족 처리 제외
            } // 획득 성공 끝
            offeredId = item.Id; // 공간 부족 장비 식별자
            offeredName = item.Name; // 공간 부족 장비 이름
            offeredLimit = 1; // 장비 묶음 한도
            ShowOfferChoice(); // 기존 교체 또는 포기 선택
        } // 획득 끝
        private void EquipSelected() // 선택 슬롯의 장비 장착
        { // 장착 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0) // 모달과 드래그 검사
            { // 입력 거절 시작
                return; // 다른 입력 중 장착 차단
            } // 입력 거절 끝
            var success = owner.TryEquipInventoryItem(selectedSlot, out var message); // 실제 장착 및 용량 변경
            if (success) // 실제 장착 성공 검사
            { // 선택 정리 시작
                selectedSlot = -1; // 이전 인벤토리 선택 해제
            } // 선택 정리 끝
            Refresh(); // 장비와 슬롯 상태 갱신
            status.text = message; // 실제 장착 결과 안내
        } // 장착 끝
        private void Unequip(EquipmentSlot slot) // 선택 부위의 장비 해제
        { // 해제 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0) // 모달과 드래그 검사
            { // 입력 거절 시작
                return; // 다른 입력 중 해제 차단
            } // 입력 거절 끝
            var success = owner.TryUnequipItem(slot, out var message); // 실제 해제와 인벤토리 반환
            if (success) // 실제 장비 해제 성공 검사
            { // 선택 정리 시작
                selectedSlot = -1; // 변경 전 슬롯 선택 해제
            } // 선택 정리 끝
            Refresh(); // 장비와 가방 상태 갱신
            status.text = message; // 실제 해제 결과 안내
        } // 해제 끝
        private void AddTrialPotion() // 개발 확인용 소형 회복약 지급
        { // 지급 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || lastPotionFrame == Time.frameCount) // 획득 선택과 드래그 및 중복 지급 검사
            { // 지급 거절 조건 시작
                return; // 잘못된 회복약 지급 차단
            } // 지급 거절 조건 끝
            lastPotionFrame = Time.frameCount; // 회복약 지급 프레임 보관
            var result = owner.Inventory.Add(AppRoot.TrialPotionId, "시험 소형 회복약", 1, AppRoot.TrialPotionStackLimit); // 기획 수치의 시험 회복약 지급
            Refresh(); // 현재 슬롯과 체력 표시
            if (result.Remaining == 0) // 실제 회복약 획득 성공 검사
            { // 성공 기록 시작
                owner.RecordPotionAcquisition(); // 성공한 회복약 획득 기록
            } // 성공 기록 끝
            if (result.Remaining > 0) // 가방 공간 부족 검사
            { // 공간 부족 조건 시작
                offeredId = AppRoot.TrialPotionId; // 대기할 회복약 식별자
                offeredName = "시험 소형 회복약"; // 대기할 회복약 이름
                offeredLimit = AppRoot.TrialPotionStackLimit; // 회복약 최대 중첩
                ShowOfferChoice(); // 기존 획득 선택과 교체 확인
            } // 공간 부족 조건 끝
        } // 지급 끝
        private void AddTrialConsumable(string id) // 개발 확인용 추가 소비품 지급
        { // 지급 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || lastPotionFrame == Time.frameCount) // 선택 화면과 중복 지급 검사
            { // 지급 거절 시작
                return; // 잘못된 지급 차단
            } // 지급 거절 끝
            var item = TrialConsumableRules.Find(id); // 실제 시험 소비품 정의
            if (item == null) // 미등록 아이템 검사
            { // 정의 오류 시작
                return; // 근거 없는 아이템 지급 차단
            } // 정의 오류 끝
            lastPotionFrame = Time.frameCount; // 현재 프레임 중복 지급 차단
            var result = owner.Inventory.Add(item.Id, item.Name, 1, item.StackLimit); // 실제 시험 아이템 한 개 지급
            Refresh(); // 보유 상태 갱신
            if (result.Remaining == 0) // 실제 지급 성공 검사
            { // 획득 기록 시작
                owner.RecordExplorationEvent(item.Name + " 1개 획득"); // 확인된 획득 기록
                status.text = item.Name + "을 획득했습니다"; // 실제 획득 안내
                return; // 공간 부족 처리 제외
            } // 획득 기록 끝
            offeredId = item.Id; // 공간 부족으로 대기할 아이템
            offeredName = item.Name; // 대기할 아이템 이름
            offeredLimit = item.StackLimit; // 미확정 시험 중첩 한도
            ShowOfferChoice(); // 기존 아이템 교체 또는 포기 선택
        } // 지급 끝
        private void UseSelectedItem() // 선택한 시험 소비품 사용
        { // 사용 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || lastUseFrame == Time.frameCount) // 선택 화면과 중복 사용 검사
            { // 사용 거절 시작
                return; // 잘못된 사용 차단
            } // 사용 거절 끝
            lastUseFrame = Time.frameCount; // 같은 프레임 중복 사용 차단
            var used = owner.TryUseInventoryConsumable(selectedSlot, out var message); // 선택한 슬롯의 실제 소비와 회복
            if (used) // 실제 사용 성공 검사
            { // 선택 해제 시작
                selectedSlot = -1; // 사용 후 이전 슬롯 선택 해제
            } // 선택 해제 끝
            Refresh(); // 실제 슬롯과 자원 표시 갱신
            status.text = message; // 성공 또는 실패 안내
        } // 사용 끝
        private void ShowDiscardConfirmation() // 선택한 보유 묶음의 버리기 확인
        { // 확인 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || selectedSlot < 0 || selectedSlot >= owner.Inventory.Slots.Count || owner.Inventory.Slots[selectedSlot].IsEmpty) // 실제 선택과 슬롯 검사
            { // 대상 오류 시작
                status.text = "버릴 아이템을 선택하세요"; // 명시적인 선택 안내
                return; // 빈 슬롯과 잘못된 단계 차단
            } // 대상 오류 끝
            replaceIndex = selectedSlot; // 확인할 보유 슬롯 보관
            expectedSlot = owner.Inventory.Slots[replaceIndex]; // 확인 당시 슬롯 참조
            expectedId = expectedSlot.ItemId; // 확인 당시 아이템 식별자
            expectedName = expectedSlot.Name; // 확인 당시 아이템 이름
            expectedQuantity = expectedSlot.Quantity; // 확인 당시 묶음 수량
            expectedLimit = expectedSlot.StackLimit; // 확인 당시 중첩 한도
            expectedEquipment = EquipmentSnapshot(expectedSlot.Equipment); // 확인 당시 개별 장비 전체 내용
            offerStage = OfferStage.Discard; // 버리기 확인 단계
            SetBaseButtons(false, false); // 확인 화면 아래 입력 잠금
            offerPanel = CreateOfferPanel(); // 전체 확인 화면 생성
            UiFactory.CreateText(offerPanel.transform, "DiscardQuestion", expectedName + " × " + expectedQuantity + " 전체를 버릴까요?", 36, new Vector2(0f, 100f), new Vector2(1400f, 160f), Color.white); // 실제 버릴 묶음 확인
            var cancel = UiFactory.CreateButton(offerPanel.transform, "CancelDiscardButton", "아니요", new Vector2(-250f, -180f)); // 버리기 취소
            var confirm = UiFactory.CreateButton(offerPanel.transform, "ConfirmDiscardButton", "버리기", new Vector2(250f, -180f)); // 실제 버리기 확정
            cancel.onClick.AddListener(CancelDiscard); // 원래 인벤토리 복귀
            confirm.onClick.AddListener(ConfirmDiscard); // 확인한 묶음 삭제 연결
            cancel.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 취소 키 연결
            confirm.gameObject.AddComponent<InventoryCancelRelay>().Initialize(this); // 확인 버튼의 취소 키 연결
            EventSystem.current.SetSelectedGameObject(cancel.gameObject); // 기본 선택을 아니요로 지정
        } // 확인 끝
        private void CancelDiscard() // 보유 아이템 버리기 취소
        { // 취소 시작
            if (offerStage != OfferStage.Discard) // 실제 버리기 확인 검사
            { // 단계 오류 시작
                return; // 다른 확인 화면 차단
            } // 단계 오류 끝
            RemoveOfferPanel(); // 확인 화면 정리
            offerStage = OfferStage.None; // 일반 인벤토리 복구
            SetBaseButtons(true, true); // 기존 슬롯과 행동 입력 복구
            Refresh(); // 보존된 인벤토리 표시
            EventSystem.current.SetSelectedGameObject(discardButton.gameObject); // 기존 버리기 버튼 선택 복구
        } // 취소 끝
        private void ConfirmDiscard() // 확인한 묶음의 실제 버리기
        { // 확정 시작
            if (offerStage != OfferStage.Discard || replaceIndex < 0 || replaceIndex >= owner.Inventory.Slots.Count) // 확인 단계와 대상 범위 검사
            { // 단계 오류 시작
                return; // 잘못된 삭제 차단
            } // 단계 오류 끝
            var current = owner.Inventory.Slots[replaceIndex]; // 현재 선택된 실제 묶음
            if (!object.ReferenceEquals(current, expectedSlot) || current.ItemId != expectedId || current.Name != expectedName || current.Quantity != expectedQuantity || current.StackLimit != expectedLimit || EquipmentSnapshot(current.Equipment) != expectedEquipment) // 확인 이후의 슬롯과 장비 변경 검사
            { // 대상 변경 시작
                CancelDiscard(); // 기존 인벤토리 보존과 확인 종료
                status.text = "아이템이 변경되어 버리기를 취소했습니다"; // 변경 충돌 안내
                return; // 예상하지 못한 아이템 삭제 차단
            } // 대상 변경 끝
            if (!owner.Inventory.Remove(replaceIndex, expectedQuantity, out var removed) || removed != expectedQuantity) // 확인한 전체 묶음 삭제 검사
            { // 삭제 실패 시작
                CancelDiscard(); // 기존 상태와 화면 복구
                status.text = "아이템을 버릴 수 없습니다"; // 삭제 실패 안내
                return; // 성공 기록 차단
            } // 삭제 실패 끝
            var discarded = expectedName + " × " + removed; // 실제 버린 묶음 안내
            selectedSlot = -1; // 삭제된 슬롯 선택 해제
            CancelDiscard(); // 확인 화면 정리와 인벤토리 복귀
            owner.RecordExplorationEvent(discarded + " 버리기"); // 확인된 버리기 기록
            status.text = discarded + " 버림"; // 실제 삭제 결과 안내
        } // 확정 끝
        private void UseTrialPotion() // 탐험 인벤토리의 소형 회복약 사용
        { // 사용 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || lastUseFrame == Time.frameCount) // 획득 선택과 드래그 및 중복 사용 검사
            { // 사용 거절 조건 시작
                return; // 잘못된 회복약 사용 차단
            } // 사용 거절 조건 끝
            lastUseFrame = Time.frameCount; // 회복약 사용 프레임 보관
            selectedSlot = -1; // 회복약 사용 전 대기 중인 슬롯 이동 해제
            owner.TryUseTrialPotion(out var message); // 실제 체력 회복과 조건부 소비
            Refresh(); // 변경된 슬롯과 체력 갱신
            status.text = message; // 사용 결과 또는 불가 이유 안내
        } // 사용 끝
        private void AddTrialKey() // 개발 확인용 열쇠 지급
        { // 지급 시작
            if (offerStage != OfferStage.None || draggedSlot >= 0 || lastKeyFrame == Time.frameCount) // 획득 화면과 드래그 및 중복 검사
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
            var capacity = owner.Inventory.Slots.Count; // 실제 가방 용량
            if (selectedSlot >= capacity) // 축소된 슬롯 선택 검사
            { // 선택 복구 시작
                selectedSlot = -1; // 존재하지 않는 선택 해제
            } // 선택 복구 끝
            for (var index = 0; index < slots.Length; index++) // 실제 슬롯 순회
            { // 슬롯 표시 시작
                var available = index < capacity; // 현재 가방 슬롯 사용 여부
                slotButtons[index].gameObject.SetActive(available); // 현재 용량에 맞춘 슬롯 표시
                if (!available) // 미사용 슬롯 검사
                { // 숨김 슬롯 처리 시작
                    continue; // 없는 슬롯 접근 차단
                } // 숨김 슬롯 처리 끝
                var slot = owner.Inventory.Slots[index]; // 현재 슬롯 상태
                if (!slot.IsEmpty) // 아이템 존재 검사
                { // 사용 중 조건 시작
                    occupied++; // 사용 슬롯 수 증가
                } // 사용 중 조건 끝
                slots[index].text = (selectedSlot == index ? "▶ " : "") + (index + 1) + ". " + (slot.IsEmpty ? "비어 있음" : slot.Name + " × " + slot.Quantity); // 선택 상태와 아이템 표시
            } // 슬롯 표시 끝
            healthText.text = "체력 " + owner.PlayerHealth + " / 100 · 마나 " + owner.PlayerMana + " / 50"; // 현재 체력과 마나 표시 갱신
            equipmentText.text = "무기 " + (owner.Equipment.Get(EquipmentSlot.Weapon)?.Name ?? "없음") + " · 방어구 " + (owner.Equipment.Get(EquipmentSlot.Armor)?.Name ?? "없음") + " · 장신구 " + (owner.Equipment.Get(EquipmentSlot.Accessory)?.Name ?? "없음") + " · 가방 " + (owner.Equipment.Get(EquipmentSlot.Bag)?.Name ?? "없음"); // 실제 장착 장비 표시
            var selectedEquipment = selectedSlot < 0 ? null : owner.Inventory.Slots[selectedSlot].Equipment; // 선택한 개별 장비 조회
            selectedDetails.text = selectedEquipment == null ? "슬롯 선택 후 이동 · 마우스 드래그 지원" : owner.Inventory.Slots[selectedSlot].Name + " · " + TrialEquipmentTraits.Describe(selectedEquipment); // 실제 등급과 모든 시험 효과 표시
            status.text = selectedSlot >= 0 ? "아이템 선택됨 · 다른 칸으로 이동 또는 행동 선택" : "가방 " + occupied + " / " + capacity + " · 열쇠 " + owner.Keys.Count; // 이동 안내 또는 현재 슬롯 표시
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
