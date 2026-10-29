using ProjectDelta.Core; // 저장된 승리 보상 후보
using UnityEngine; // 화면 객체와 위치
using UnityEngine.EventSystems; // 기본 버튼 선택
using UnityEngine.UI; // 보상 안내와 배경
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class BattleRewardScreenView : MonoBehaviour, ICancelHandler // 저장된 승리 보상 확인 화면
    { // 클래스 시작
        private AppRoot owner; // 현재 회차와 보상 관리자
        private Text notice; // 저장 오류와 선택 안내
        private GameObject selectionPanel; // 교체할 보유 슬롯 선택 화면
        private GameObject confirmationPanel; // 교체 최종 확인 화면
        private Button[] slotButtons; // 교체 대상 슬롯 버튼
        private Button replaceButton; // 보유 아이템 교체 선택 버튼
        private Button takeButton; // 기본 보상 획득 버튼
        private Button skipButton; // 아이템 포기 버튼
        private Button cancelSelection; // 슬롯 선택 취소 버튼
        private Button cancelConfirmation; // 최종 확인 취소 버튼
        private Text confirmationText; // 버릴 아이템과 수량 안내
        private Text confirmationNotice; // 교체 저장 오류 안내
        private int selectedIndex = -1; // 확인할 보유 슬롯 번호
        private string expectedId; // 확인 시점의 아이템 식별자
        private string expectedName; // 확인 시점의 아이템 이름
        private int expectedQuantity; // 확인 시점의 아이템 수량
        private int expectedLimit; // 확인 시점의 최대 중첩
        private string rewardName; // 저장된 보상 소비품 이름
        public static BattleRewardScreenView Create(AppRoot owner, SavedBattleReward reward) // 실제 보상 화면 생성
        { // 생성 시작
            var screen = new GameObject("BattleRewardScreen", typeof(BattleRewardScreenView)); // 보상 화면 상위 객체
            try // 화면 구성
            { // 구성 시작
                var view = screen.GetComponent<BattleRewardScreenView>(); // 실제 화면 제어
                view.owner = owner; // 현재 회차 연결
                view.slotButtons = new Button[owner.Inventory.Slots.Count]; // 실제 가방 용량의 교체 버튼
                var definition = reward.potionCount > 0 ? TrialConsumableRules.Find(reward.itemId) : null; // 저장된 보상 소비품 정의
                if (reward.potionCount > 0 && definition == null) // 미등록 보상 소비품 검사
                { // 정의 오류 시작
                    throw new System.InvalidOperationException("Unknown reward item."); // 잘못된 보상 화면 차단
                } // 정의 오류 끝
                view.rewardName = definition == null ? string.Empty : definition.Name; // 실제 보상 아이템 이름
                var canvas = UiFactory.CreateCanvas("BattleRewardCanvas"); // 보상 전용 화면
                canvas.transform.SetParent(screen.transform, false); // 보상 화면 연결
                canvas.sortingOrder = 110; // 전투보다 앞에 표시
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 화면 배경
                background.transform.SetParent(canvas.transform, false); // 보상 화면에 배치
                var rect = background.GetComponent<RectTransform>(); // 배경 위치
                rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
                rect.anchorMax = Vector2.one; // 오른쪽 위 기준
                rect.offsetMin = Vector2.zero; // 아래 여백 제거
                rect.offsetMax = Vector2.zero; // 위 여백 제거
                background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 보상 배경색
                UiFactory.CreateText(canvas.transform, "Header", "승리 보상", 58, new Vector2(0f, 300f), new Vector2(1000f, 100f), Color.white); // 결과 제목
                UiFactory.CreateText(canvas.transform, "RewardAmounts", "경험치 " + reward.experience + " · 시험 골드 " + reward.gold, 34, new Vector2(0f, 150f), new Vector2(1100f, 80f), Color.white); // 경험치와 시험 골드
                UiFactory.CreateText(canvas.transform, "RewardItem", reward.potionCount > 0 ? view.rewardName + " " + reward.potionCount + "개" : "획득한 소비 아이템 없음", 30, new Vector2(0f, 55f), new Vector2(900f, 70f), Color.white); // 고정된 소비 아이템 후보
                view.notice = UiFactory.CreateText(canvas.transform, "RewardNotice", "보상을 확인하세요", 24, new Vector2(0f, -70f), new Vector2(1100f, 100f), Color.white); // 획득과 저장 안내
                var take = UiFactory.CreateButton(canvas.transform, "TakeRewardButton", "보상 받기", new Vector2(-215f, -210f)); // 모든 보상 획득
                var skip = UiFactory.CreateButton(canvas.transform, "SkipRewardItemButton", "아이템 포기", new Vector2(215f, -210f)); // 아이템을 두고 보상 확정
                view.takeButton = take; // 기본 획득 버튼 보관
                view.skipButton = skip; // 아이템 포기 버튼 보관
                take.onClick.AddListener(() => view.Resolve(true)); // 실제 아이템 획득
                skip.onClick.AddListener(() => view.Resolve(false)); // 명시적 아이템 포기
                take.gameObject.AddComponent<BattleRewardCancelRelay>().Initialize(view); // 선택 버튼 취소 입력 연결
                skip.gameObject.AddComponent<BattleRewardCancelRelay>().Initialize(view); // 포기 버튼 취소 입력 연결
                skip.gameObject.SetActive(reward.potionCount > 0); // 아이템이 있을 때만 포기 선택
                view.replaceButton = UiFactory.CreateButton(canvas.transform, "ReplaceRewardItemButton", "보유 아이템 교체", new Vector2(0f, -320f)); // 보유 슬롯 교체 선택
                view.replaceButton.onClick.AddListener(view.OpenSelection); // 교체할 슬롯 선택 화면 연결
                view.replaceButton.gameObject.AddComponent<BattleRewardCancelRelay>().Initialize(view); // 교체 버튼 취소 입력 연결
                view.replaceButton.gameObject.SetActive(reward.potionCount > 0); // 후보 아이템이 있을 때만 교체 선택
                view.CreateSelectionPanel(canvas.transform); // 보유 슬롯과 교체 취소 화면 구성
                view.CreateConfirmationPanel(canvas.transform); // 버릴 묶음의 최종 확인 화면 구성
                EventSystem.current.SetSelectedGameObject(take.gameObject); // 기본 보상 획득 선택
                return view; // 준비된 화면 전달
            } // 구성 끝
            catch // 생성 실패 검사
            { // 실패 정리 시작
                screen.SetActive(false); // 불완전한 화면 숨김
                Destroy(screen); // 실패한 화면 제거
                throw; // 실제 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void CreateSelectionPanel(Transform parent) // 교체 대상 선택 화면 구성
        { // 구성 시작
            selectionPanel = CreatePanel(parent, "RewardReplacementSelection"); // 보상 선택을 가리는 슬롯 선택 배경
            UiFactory.CreateText(selectionPanel.transform, "ReplacementTitle", "교체할 아이템을 선택하세요", 36, new Vector2(0f, 365f), new Vector2(1300f, 75f), Color.white); // 기존 아이템 선택 안내
            for (var index = 0; index < slotButtons.Length; index++) // 기본 인벤토리 슬롯 순회
            { // 슬롯 생성 시작
                var slotIndex = index; // 버튼별 슬롯 번호 보관
                var rows = (slotButtons.Length + 1) / 2; // 두 열의 행 수
                var spacing = Mathf.Min(90f, 480f / Mathf.Max(1, rows - 1)); // 전체 슬롯에 맞는 세로 간격
                var x = index < rows ? -400f : 400f; // 두 열의 가로 위치
                var y = 255f - (index % rows) * spacing; // 전체 행의 세로 위치
                var button = UiFactory.CreateButton(selectionPanel.transform, "RewardReplacementSlot_" + index, "", new Vector2(x, y)); // 교체 대상 버튼
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, Mathf.Min(70f, spacing - 3f)); // 슬롯 버튼 크기
                var label = button.transform.Find("Label").GetComponent<RectTransform>(); // 슬롯 글자 영역
                label.sizeDelta = new Vector2(700f, Mathf.Min(70f, spacing - 3f)); // 버튼 안의 글자 영역
                label.GetComponent<Text>().fontSize = 24; // 슬롯 이름 글자 크기
                button.onClick.AddListener(() => SelectReplacement(slotIndex)); // 명시적인 보유 슬롯 선택
                button.gameObject.AddComponent<BattleRewardCancelRelay>().Initialize(this); // 슬롯 버튼 취소 입력 연결
                slotButtons[index] = button; // 실제 슬롯 버튼 보관
            } // 슬롯 생성 끝
            cancelSelection = UiFactory.CreateButton(selectionPanel.transform, "CancelRewardReplacementSelection", "취소", new Vector2(0f, -350f)); // 슬롯 선택 취소
            cancelSelection.onClick.AddListener(CancelSelection); // 보상 화면 복귀 연결
            cancelSelection.gameObject.AddComponent<BattleRewardCancelRelay>().Initialize(this); // 슬롯 선택 취소 입력 연결
            selectionPanel.SetActive(false); // 초기 슬롯 선택 화면 숨김
        } // 구성 끝
        private void CreateConfirmationPanel(Transform parent) // 교체 최종 확인 화면 구성
        { // 구성 시작
            confirmationPanel = CreatePanel(parent, "RewardReplacementConfirmation"); // 전체 확인 배경
            confirmationText = UiFactory.CreateText(confirmationPanel.transform, "RewardReplacementQuestion", "", 36, new Vector2(0f, 125f), new Vector2(1400f, 180f), Color.white); // 버릴 묶음 확인 안내
            confirmationNotice = UiFactory.CreateText(confirmationPanel.transform, "RewardReplacementNotice", "", 24, new Vector2(0f, -5f), new Vector2(1200f, 65f), Color.white); // 저장 오류 안내
            var confirm = UiFactory.CreateButton(confirmationPanel.transform, "ConfirmRewardReplacement", "교체", new Vector2(-215f, -140f)); // 실제 교체 확정
            cancelConfirmation = UiFactory.CreateButton(confirmationPanel.transform, "CancelRewardReplacementConfirmation", "취소", new Vector2(215f, -140f)); // 최종 확인 취소
            confirm.onClick.AddListener(ConfirmReplacement); // 실제 교체 저장 연결
            cancelConfirmation.onClick.AddListener(CancelConfirmation); // 슬롯 선택으로 복귀 연결
            confirm.gameObject.AddComponent<BattleRewardCancelRelay>().Initialize(this); // 확정 버튼 취소 입력 연결
            cancelConfirmation.gameObject.AddComponent<BattleRewardCancelRelay>().Initialize(this); // 확인 취소 입력 연결
            confirmationPanel.SetActive(false); // 초기 최종 확인 화면 숨김
        } // 구성 끝
        private static GameObject CreatePanel(Transform parent, string name) // 전체 화면 선택 배경 생성
        { // 생성 시작
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 전체 화면 패널
            panel.transform.SetParent(parent, false); // 보상 화면의 최상위 배치
            var rect = panel.GetComponent<RectTransform>(); // 패널 위치 조회
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 기존 보상 입력을 가리는 배경
            return panel; // 생성된 패널 전달
        } // 생성 끝
        private void OpenSelection() // 기존 아이템 교체 대상 선택
        { // 선택 시작
            if (!replaceButton.gameObject.activeInHierarchy || selectionPanel.activeSelf || confirmationPanel.activeSelf) // 후보와 중복 화면 검사
            { // 선택 거절 시작
                return; // 후보 없는 교체 차단
            } // 선택 거절 끝
            selectedIndex = -1; // 이전 슬롯 확인 해제
            takeButton.interactable = false; // 선택 중 기본 획득 차단
            skipButton.interactable = false; // 선택 중 아이템 포기 차단
            replaceButton.interactable = false; // 중복 교체 화면 차단
            for (var index = 0; index < slotButtons.Length; index++) // 실제 보유 슬롯 순회
            { // 슬롯 표시 시작
                var slot = owner.Inventory.Slots[index]; // 현재 인벤토리 묶음
                var button = slotButtons[index]; // 해당 슬롯 버튼
                button.interactable = !slot.IsEmpty; // 보유한 아이템만 교체 허용
                button.transform.Find("Label").GetComponent<Text>().text = slot.IsEmpty ? (index + 1) + ". 빈 슬롯" : (index + 1) + ". " + slot.Name + " × " + slot.Quantity; // 슬롯 이름과 수량 표시
            } // 슬롯 표시 끝
            selectionPanel.SetActive(true); // 교체 대상 선택 화면 표시
            EventSystem.current.SetSelectedGameObject(cancelSelection.gameObject); // 안전한 기본 취소 선택
        } // 선택 끝
        private void SelectReplacement(int index) // 교체할 슬롯과 확인 시점 값 보관
        { // 슬롯 선택 시작
            if (!selectionPanel.activeSelf || index < 0 || index >= owner.Inventory.Slots.Count) // 선택 화면과 슬롯 범위 검사
            { // 선택 거절 시작
                return; // 잘못된 슬롯 선택 차단
            } // 선택 거절 끝
            var slot = owner.Inventory.Slots[index]; // 현재 선택한 보유 묶음
            if (slot.IsEmpty) // 빈 슬롯 검사
            { // 빈 슬롯 거절 시작
                return; // 버릴 아이템 없는 교체 차단
            } // 빈 슬롯 거절 끝
            selectedIndex = index; // 선택한 슬롯 번호 보관
            expectedId = slot.ItemId; // 확인 시점의 아이템 식별자 보관
            expectedName = slot.Name; // 확인 시점의 아이템 이름 보관
            expectedQuantity = slot.Quantity; // 확인 시점의 아이템 수량 보관
            expectedLimit = slot.StackLimit; // 확인 시점의 중첩 한도 보관
            confirmationText.text = expectedName + " × " + expectedQuantity + "개를 버리고\n" + rewardName + "으로 교체할까요?"; // 실제 버릴 묶음과 새 아이템 확인
            confirmationNotice.text = string.Empty; // 이전 확인 오류 초기화
            selectionPanel.SetActive(false); // 슬롯 선택 화면 숨김
            confirmationPanel.SetActive(true); // 최종 확인 화면 표시
            EventSystem.current.SetSelectedGameObject(cancelConfirmation.gameObject); // 기본 선택을 취소로 지정
        } // 슬롯 선택 끝
        private void ConfirmReplacement() // 확인한 슬롯의 원자적 교체 요청
        { // 확정 시작
            if (!confirmationPanel.activeSelf || selectedIndex < 0) // 실제 확인 단계와 슬롯 검사
            { // 확정 거절 시작
                return; // 확인되지 않은 교체 차단
            } // 확정 거절 끝
            if (!owner.TryResolveVictoryRewardReplacing(selectedIndex, expectedId, expectedName, expectedQuantity, expectedLimit, out var error)) // 확인 시점 값과 보상 저장 검사
            { // 저장 실패 시작
                confirmationNotice.text = error; // 기존 데이터 보존과 오류 안내
            } // 저장 실패 끝
        } // 확정 끝
        private void CancelSelection() // 대상 선택을 취소하고 보상 화면 복귀
        { // 취소 시작
            selectionPanel.SetActive(false); // 보유 슬롯 선택 숨김
            selectedIndex = -1; // 확인 대기 슬롯 해제
            takeButton.interactable = true; // 기본 보상 획득 복구
            skipButton.interactable = true; // 아이템 포기 선택 복구
            replaceButton.interactable = true; // 교체 선택 복구
            EventSystem.current.SetSelectedGameObject(replaceButton.gameObject); // 이전 교체 선택 복귀
        } // 취소 끝
        private void CancelConfirmation() // 최종 확인을 취소하고 슬롯 선택 복귀
        { // 취소 시작
            confirmationPanel.SetActive(false); // 최종 확인 화면 숨김
            OpenSelection(); // 현재 보유 슬롯 다시 표시
        } // 취소 끝
        public void OnCancel(BaseEventData eventData) // ESC와 게임패드 취소 처리
        { // 취소 시작
            eventData.Use(); // 취소 입력의 다른 화면 전파 차단
            if (confirmationPanel.activeSelf) // 최종 교체 확인 검사
            { // 확인 취소 시작
                CancelConfirmation(); // 인벤토리 변경 없이 슬롯 선택 복귀
                return; // 다른 취소 처리 차단
            } // 확인 취소 끝
            if (selectionPanel.activeSelf) // 대상 선택 화면 검사
            { // 선택 취소 시작
                CancelSelection(); // 인벤토리 변경 없이 보상 화면 복귀
            } // 선택 취소 끝
        } // 취소 끝
        private void Resolve(bool takePotion) // 명시적인 보상 획득 또는 아이템 포기
        { // 선택 시작
            if (selectionPanel.activeSelf || confirmationPanel.activeSelf) // 교체 선택 또는 확인 단계 검사
            { // 중복 선택 차단 시작
                return; // 확인 전 다른 보상 확정 차단
            } // 중복 선택 차단 끝
            if (!owner.TryResolveVictoryReward(takePotion, out var error)) // 원자적 보상 확정 검사
            { // 확정 실패 시작
                notice.text = error; // 공간 또는 저장 오류 안내
            } // 확정 실패 끝
        } // 선택 끝
    } // 클래스 끝
    public sealed class BattleRewardCancelRelay : MonoBehaviour, ICancelHandler // 선택된 버튼의 취소 입력 중계
    { // 클래스 시작
        private BattleRewardScreenView screen; // 취소 입력을 전달할 보상 화면
        public void Initialize(BattleRewardScreenView target) // 보상 화면 연결
        { // 연결 시작
            screen = target; // 실제 화면 참조 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // ESC 또는 게임패드 취소 입력
        { // 취소 시작
            if (screen != null) // 보상 화면 존재 검사
            { // 전달 시작
                screen.OnCancel(eventData); // 현재 교체 단계 취소 전달
            } // 전달 끝
        } // 취소 끝
    } // 클래스 끝
} // 이름 공간 끝
