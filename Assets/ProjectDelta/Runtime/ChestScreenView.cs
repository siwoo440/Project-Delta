using System; // 배열과 오류 처리
using System.Collections.Generic; // 선택된 보상 목록
using ProjectDelta.Core; // 상자와 아이템 규칙
using UnityEngine; // 화면 객체와 위치
using UnityEngine.EventSystems; // 취소 입력과 버튼 선택
using UnityEngine.UI; // 화면 구성 요소
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class ChestScreenView : MonoBehaviour, ICancelHandler // 상자 개방과 보상 화면
    { // 클래스 시작
        private AppRoot owner; // 현재 회차 관리자
        private Text title; // 상자 상태 안내
        private Text notice; // 행동 결과 안내
        private Button keyButton; // 열쇠 개방 버튼
        private Button forceButton; // 강제 개방 버튼
        private Button openButton; // 상자 열기 버튼
        private Button claimButton; // 선택 보상 획득 버튼
        private Button skipButton; // 보상 포기 버튼
        private Button replaceButton; // 보유 아이템 교체 버튼
        private Button closeButton; // 상자 화면 닫기 버튼
        private Button[] rewardButtons; // 확정 후보 선택 버튼
        private bool[] selected; // 후보별 선택 상태
        private GameObject selectionPanel; // 교체 대상 선택 화면
        private GameObject confirmationPanel; // 교체 최종 확인 화면
        private Button[] slotButtons; // 보유 슬롯 선택 버튼
        private Button cancelSelection; // 슬롯 선택 취소 버튼
        private Button cancelConfirmation; // 교체 확인 취소 버튼
        private Text confirmationText; // 버릴 묶음과 새 보상 안내
        private Text confirmationNotice; // 교체 실패 안내
        private int replacementIndex = -1; // 선택한 기존 슬롯 번호
        private string expectedSlot; // 확인 시점 슬롯 정보
        private string expectedCandidate; // 확인 시점 보상 정보
        public static ChestScreenView Create(AppRoot owner) // 상자 화면 생성
        { // 생성 시작
            if (owner == null || owner.CurrentChest == null) // 회차와 상자 검사
            { // 오류 시작
                throw new InvalidOperationException("Missing chest state."); // 불완전한 화면 생성 차단
            } // 오류 끝
            var root = new GameObject("ChestScreen", typeof(ChestScreenView)); // 화면 상위 객체
            try // 화면 생성 보호
            { // 보호 시작
                var view = root.GetComponent<ChestScreenView>(); // 화면 제어 객체
                view.owner = owner; // 회차 관리자 연결
                view.Build(); // 화면 구성
                view.Refresh(); // 저장된 상자 상태 표시
                return view; // 생성된 화면 전달
            } // 보호 끝
            catch // 생성 실패 검사
            { // 정리 시작
                root.SetActive(false); // 불완전한 화면 숨김
                Destroy(root); // 불완전한 화면 제거
                throw; // 실제 오류 전달
            } // 정리 끝
        } // 생성 끝
        private void Build() // 상자 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("ChestCanvas"); // 상자 전용 화면
            canvas.transform.SetParent(transform, false); // 화면 객체 연결
            canvas.sortingOrder = 110; // 탐험 화면 앞 표시
            CreatePanel(canvas.transform, "ChestBackground"); // 전체 화면 입력 차단 배경
            title = UiFactory.CreateText(canvas.transform, "ChestTitle", "", 44, new Vector2(0f, 410f), new Vector2(1500f, 80f), Color.white); // 상자 상태 제목
            notice = UiFactory.CreateText(canvas.transform, "ChestNotice", "", 25, new Vector2(0f, -265f), new Vector2(1450f, 70f), Color.white); // 행동 결과 문구
            keyButton = AddButton(canvas.transform, "ChestKey", "열쇠 사용", new Vector2(-220f, 0f), Unlock); // 열쇠 개방 연결
            forceButton = AddButton(canvas.transform, "ChestForce", "강제 개방", new Vector2(220f, 0f), Force); // 강제 개방 연결
            openButton = AddButton(canvas.transform, "ChestOpen", "상자 열기", new Vector2(0f, 0f), Reveal); // 미믹 판정 연결
            claimButton = AddButton(canvas.transform, "ChestClaim", "선택 보상 획득", new Vector2(-420f, -350f), Claim); // 선택 보상 획득
            skipButton = AddButton(canvas.transform, "ChestSkip", "아이템 포기", new Vector2(0f, -350f), Skip); // 모든 아이템 포기
            replaceButton = AddButton(canvas.transform, "ChestReplace", "보유 아이템 교체", new Vector2(420f, -350f), OpenSelection); // 교체 대상 선택
            closeButton = AddButton(canvas.transform, "ChestClose", "닫기", new Vector2(730f, 420f), Close); // 상자 화면 닫기
            rewardButtons = new Button[6]; // 최대 후보 여섯 개 공간
            selected = new bool[rewardButtons.Length]; // 후보 선택 기록
            for (var index = 0; index < rewardButtons.Length; index++) // 후보 버튼 구성
            { // 후보 생성 시작
                var candidateIndex = index; // 버튼별 후보 번호
                var button = AddButton(canvas.transform, "ChestReward_" + index, "", new Vector2(index % 2 == 0 ? -420f : 420f, 260f - index / 2 * 130f), () => Toggle(candidateIndex)); // 후보 선택 버튼
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(780f, 110f); // 후보 버튼 크기
                var label = button.transform.Find("Label").GetComponent<Text>(); // 후보 설명 글자
                label.rectTransform.sizeDelta = new Vector2(760f, 105f); // 후보 설명 영역
                label.fontSize = 23; // 후보 글자 크기
                label.resizeTextForBestFit = true; // 긴 옵션의 영역 맞춤
                label.resizeTextMinSize = 14; // 옵션 최소 글자 크기
                label.resizeTextMaxSize = 23; // 옵션 최대 글자 크기
                rewardButtons[index] = button; // 후보 버튼 저장
            } // 후보 생성 끝
            BuildSelection(canvas.transform); // 보유 슬롯 선택 구성
            BuildConfirmation(canvas.transform); // 교체 확인 구성
        } // 구성 끝
        private Button AddButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action) // 취소 입력이 연결된 버튼 생성
        { // 생성 시작
            var button = UiFactory.CreateButton(parent, name, label, position); // 공통 버튼 생성
            button.onClick.AddListener(action); // 버튼 동작 연결
            button.gameObject.AddComponent<ChestCancelRelay>().Initialize(this); // 취소 입력 연결
            return button; // 생성 버튼 전달
        } // 생성 끝
        private static GameObject CreatePanel(Transform parent, string name) // 전체 화면 배경 생성
        { // 생성 시작
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 전체 화면 패널
            panel.transform.SetParent(parent, false); // 상위 화면 연결
            var rect = panel.GetComponent<RectTransform>(); // 패널 위치
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 화면 입력 차단 배경
            return panel; // 패널 전달
        } // 생성 끝
        private void BuildSelection(Transform parent) // 교체 슬롯 선택 화면 구성
        { // 구성 시작
            selectionPanel = CreatePanel(parent, "ChestReplacementSelection"); // 슬롯 선택 배경
            UiFactory.CreateText(selectionPanel.transform, "Title", "교체할 보유 아이템을 선택하세요", 36, new Vector2(0f, 365f), new Vector2(1400f, 75f), Color.white); // 선택 안내
            slotButtons = new Button[owner.Inventory.Slots.Count]; // 실제 인벤토리 용량
            for (var index = 0; index < slotButtons.Length; index++) // 슬롯 버튼 구성
            { // 생성 시작
                var captured = index; // 버튼별 슬롯 번호
                var rows = (slotButtons.Length + 1) / 2; // 두 열의 행 수
                var spacing = Mathf.Min(90f, 480f / Mathf.Max(1, rows - 1)); // 슬롯 간격
                var button = AddButton(selectionPanel.transform, "ChestSlot_" + index, "", new Vector2(index < rows ? -400f : 400f, 255f - index % rows * spacing), () => SelectSlot(captured)); // 슬롯 선택 버튼
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, Mathf.Min(70f, spacing - 3f)); // 슬롯 버튼 크기
                var label = button.transform.Find("Label").GetComponent<Text>(); // 슬롯 설명 글자
                label.rectTransform.sizeDelta = button.GetComponent<RectTransform>().sizeDelta; // 버튼 안의 글자 영역
                label.fontSize = 24; // 슬롯 글자 크기
                slotButtons[index] = button; // 슬롯 버튼 저장
            } // 생성 끝
            cancelSelection = AddButton(selectionPanel.transform, "CancelChestReplacement", "취소", new Vector2(0f, -350f), CancelSelection); // 슬롯 선택 취소
            selectionPanel.SetActive(false); // 초기 선택 화면 숨김
        } // 구성 끝
        private void BuildConfirmation(Transform parent) // 최종 교체 확인 화면 구성
        { // 구성 시작
            confirmationPanel = CreatePanel(parent, "ChestReplacementConfirmation"); // 교체 확인 배경
            confirmationText = UiFactory.CreateText(confirmationPanel.transform, "Question", "", 32, new Vector2(0f, 140f), new Vector2(1450f, 220f), Color.white); // 버릴 아이템과 새 보상
            confirmationText.resizeTextForBestFit = true; // 긴 옵션의 영역 맞춤
            confirmationText.resizeTextMinSize = 16; // 확인 문구 최소 크기
            confirmationNotice = UiFactory.CreateText(confirmationPanel.transform, "Notice", "", 24, new Vector2(0f, -10f), new Vector2(1300f, 70f), Color.white); // 교체 오류 표시
            AddButton(confirmationPanel.transform, "ConfirmChestReplacement", "교체", new Vector2(-220f, -150f), ConfirmReplacement); // 교체 확정
            cancelConfirmation = AddButton(confirmationPanel.transform, "CancelChestConfirmation", "취소", new Vector2(220f, -150f), CancelConfirmation); // 최종 확인 취소
            confirmationPanel.SetActive(false); // 초기 확인 화면 숨김
        } // 구성 끝
        public void Refresh() // 현재 저장 상자 상태 반영
        { // 갱신 시작
            var chest = owner.CurrentChest; // 현재 상자 조회
            if (chest == null) // 상자 존재 검사
            { // 상자 없음 시작
                return; // 닫힌 화면 갱신 차단
            } // 상자 없음 끝
            var locked = !chest.unlocked; // 잠금 상태
            var waiting = chest.unlocked && chest.mimicState == 0; // 아직 열지 않은 상자
            var available = !chest.completed && (chest.mimicState == 1 || (chest.mimicState == 2 && chest.mimicDefeated)); // 획득 가능한 보상
            title.text = chest.completed ? "상자 보상 수령 완료" : locked ? "잠긴 상자" : waiting ? "상자를 열 수 있습니다" : available ? "상자 보상 · 경험치 " + chest.experience : "미믹과 전투 중"; // 단계별 상자 안내
            keyButton.gameObject.SetActive(locked); // 잠긴 상자 열쇠 표시
            forceButton.gameObject.SetActive(locked); // 잠긴 상자 강제 표시
            forceButton.interactable = !chest.forceAttempted; // 강제 시도는 한 번만 허용
            openButton.gameObject.SetActive(waiting); // 미개봉 상자 열기 표시
            claimButton.gameObject.SetActive(available); // 획득 버튼 표시
            skipButton.gameObject.SetActive(available); // 포기 버튼 표시
            replaceButton.gameObject.SetActive(available); // 교체 버튼 표시
            for (var index = 0; index < rewardButtons.Length; index++) // 확정 후보 표시
            { // 표시 시작
                var visible = available && chest.rewards != null && index < chest.rewards.Length; // 후보 표시 여부
                rewardButtons[index].gameObject.SetActive(visible); // 후보 버튼 표시 전환
                if (visible) // 실제 후보 검사
                { // 후보 표시 시작
                    rewardButtons[index].transform.Find("Label").GetComponent<Text>().text = (selected[index] ? "[선택] " : "[선택 안 함] ") + Describe(chest.rewards[index]); // 선택 상태와 실제 보상
                } // 후보 표시 끝
            } // 표시 끝
            if (EventSystem.current != null) // 입력 장치 선택 검사
            { // 선택 시작
                EventSystem.current.SetSelectedGameObject(locked ? keyButton.gameObject : waiting ? openButton.gameObject : available ? skipButton.gameObject : closeButton.gameObject); // 안전한 기본 버튼 선택
            } // 선택 끝
        } // 갱신 끝
        private static string Describe(ChestRewardCandidateData candidate) // 실제 후보 이름과 특성 표시
        { // 표시 시작
            if (candidate == null) // 손상된 후보 검사
            { // 오류 시작
                return "알 수 없는 보상"; // 잘못된 후보 안내
            } // 오류 끝
            var equipment = candidate.equipment == null ? null : EquipmentCatalog.Find(candidate.equipment.definitionId); // 장비 기본 정의
            var consumable = TrialConsumableRules.Find(candidate.itemId); // 소비품 정의
            var relic = TrialRelicCatalog.Find(candidate.itemId); // 시험 유물 정의
            var name = equipment != null ? equipment.Name : consumable != null ? consumable.Name : relic != null ? relic.Name : candidate.itemId; // 확인 가능한 실제 이름
            var traits = equipment == null ? string.Empty : "\n" + TrialEquipmentTraits.Describe(candidate.equipment); // 장비 등급과 옵션 공개
            return name + " × " + candidate.quantity + traits; // 후보 상세 전달
        } // 표시 끝
        private void Toggle(int index) // 후보 선택 상태 전환
        { // 전환 시작
            if (selectionPanel.activeSelf || confirmationPanel.activeSelf) // 교체 단계 검사
            { // 차단 시작
                return; // 교체 중 후보 변경 차단
            } // 차단 끝
            selected[index] = !selected[index]; // 후보 선택 전환
            Refresh(); // 선택 상태 반영
            if (EventSystem.current != null) // 이벤트 시스템 검사
            { // 선택 시작
                EventSystem.current.SetSelectedGameObject(rewardButtons[index].gameObject); // 이전 후보 선택 유지
            } // 선택 끝
        } // 전환 끝
        private int[] SelectedIndices() // 선택 후보 번호 수집
        { // 수집 시작
            var values = new List<int>(); // 후보 번호 목록
            for (var index = 0; index < selected.Length; index++) // 전체 선택 검사
            { // 검사 시작
                if (selected[index]) // 선택 여부 검사
                { // 추가 시작
                    values.Add(index); // 선택 후보 번호 추가
                } // 추가 끝
            } // 검사 끝
            return values.ToArray(); // 선택 목록 전달
        } // 수집 끝
        private void Unlock() // 열쇠로 상자 개방
        { // 개방 시작
            if (confirmationPanel.activeSelf || selectionPanel.activeSelf) // 교체 확인 중 기본 동작 검사
            { // 중복 입력 차단 시작
                return; // 확인 전 획득과 화면 종료 차단
            } // 중복 입력 차단 끝
            owner.TryUnlockChestWithKey(out var message); // 열쇠 사용과 저장 요청
            notice.text = message; // 행동 결과 안내
            Refresh(); // 저장된 상태 표시
        } // 개방 끝
        private void Force() // 상자 강제 개방
        { // 개방 시작
            if (confirmationPanel.activeSelf || selectionPanel.activeSelf) // 교체 확인 중 기본 동작 검사
            { // 중복 입력 차단 시작
                return; // 확인 전 획득과 화면 종료 차단
            } // 중복 입력 차단 끝
            owner.TryForceChest(out var message); // 강제 판정과 저장 요청
            notice.text = message; // 판정 결과 안내
            Refresh(); // 저장된 상태 표시
        } // 개방 끝
        private void Reveal() // 상자 후보와 미믹 확정
        { // 열기 시작
            if (confirmationPanel.activeSelf || selectionPanel.activeSelf) // 교체 확인 중 기본 동작 검사
            { // 중복 입력 차단 시작
                return; // 확인 전 획득과 화면 종료 차단
            } // 중복 입력 차단 끝
            owner.TryRevealChest(out var message); // 확정 저장과 미믹 조우 요청
            notice.text = message; // 확정 결과 안내
            Refresh(); // 현재 상자 상태 표시
        } // 열기 끝
        private void Claim() // 선택된 보상의 일반 획득
        { // 획득 시작
            if (confirmationPanel.activeSelf || selectionPanel.activeSelf) // 교체 확인 중 기본 동작 검사
            { // 중복 입력 차단 시작
                return; // 확인 전 획득과 화면 종료 차단
            } // 중복 입력 차단 끝
            if (SelectedIndices().Length == 0) // 빈 선택 검사
            { // 거절 시작
                notice.text = "획득할 보상을 선택하세요."; // 명시적 포기 안내
                return; // 실수로 전체 포기 차단
            } // 거절 끝
            if (!owner.TryClaimChest(SelectedIndices(), -1, out var message)) // 보상 지급과 저장 요청
            { // 오류 시작
                notice.text = message; // 지급 실패 안내
            } // 오류 끝
        } // 획득 끝
        private void Skip() // 모든 아이템 명시적 포기
        { // 포기 시작
            if (confirmationPanel.activeSelf || selectionPanel.activeSelf) // 교체 확인 중 기본 동작 검사
            { // 중복 입력 차단 시작
                return; // 확인 전 획득과 화면 종료 차단
            } // 중복 입력 차단 끝
            if (!owner.TryClaimChest(Array.Empty<int>(), -1, out var message)) // 빈 선택으로 보상 확정
            { // 오류 시작
                notice.text = message; // 포기 실패 안내
            } // 오류 끝
        } // 포기 끝
        private void OpenSelection() // 교체할 보유 슬롯 선택
        { // 선택 시작
            if (confirmationPanel.activeSelf || selectionPanel.activeSelf) // 교체 확인 중 기본 동작 검사
            { // 중복 입력 차단 시작
                return; // 확인 전 획득과 화면 종료 차단
            } // 중복 입력 차단 끝
            if (SelectedIndices().Length != 1) // 단일 보상 검사
            { // 오류 시작
                notice.text = "교체할 보상 한 개를 선택하세요."; // 단일 보상 안내
                return; // 여러 보상의 교체 차단
            } // 오류 끝
            if (slotButtons.Length != owner.Inventory.Slots.Count) // 현재 가방 용량 검사
            { // 오류 시작
                notice.text = "가방 크기가 변경되었습니다. 상자를 다시 여세요."; // 불일치 안내
                return; // 오래된 슬롯 화면 차단
            } // 오류 끝
            for (var index = 0; index < slotButtons.Length; index++) // 현재 슬롯 표시
            { // 표시 시작
                var slot = owner.Inventory.Slots[index]; // 실제 보유 묶음
                slotButtons[index].interactable = !slot.IsEmpty; // 보유 슬롯만 선택 허용
                slotButtons[index].transform.Find("Label").GetComponent<Text>().text = slot.IsEmpty ? (index + 1) + ". 빈 슬롯" : (index + 1) + ". " + slot.Name + " × " + slot.Quantity; // 실제 이름과 수량
            } // 표시 끝
            selectionPanel.SetActive(true); // 슬롯 선택 표시
            if (EventSystem.current != null) // 안전한 기본 취소 선택
            { // 조건 분기 시작
                EventSystem.current.SetSelectedGameObject(cancelSelection.gameObject); // 조건 동작 처리
            } // 조건 분기 끝
        } // 선택 끝
        private void SelectSlot(int index) // 교체할 슬롯과 확인값 보관
        { // 선택 시작
            if (!selectionPanel.activeSelf || index < 0 || index >= owner.Inventory.Slots.Count) // 선택 화면과 범위 검사
            { // 조건 분기 시작
                return; // 조건 동작 처리
            } // 조건 분기 끝
            var slot = owner.Inventory.Slots[index]; // 현재 보유 슬롯
            var indices = SelectedIndices(); // 현재 선택 보상
            var chest = owner.CurrentChest; // 현재 상자
            if (slot.IsEmpty || indices.Length != 1 || chest == null || chest.rewards == null || indices[0] >= chest.rewards.Length) // 슬롯과 보상 검사
            { // 조건 분기 시작
                return; // 조건 동작 처리
            } // 조건 분기 끝
            replacementIndex = index; // 교체할 슬롯 번호
            expectedSlot = SlotSnapshot(index); // 확인 시점 슬롯 값
            expectedCandidate = JsonUtility.ToJson(chest.rewards[indices[0]]); // 확인 시점 후보 값
            confirmationText.text = slot.Name + " × " + slot.Quantity + "개를 버리고\n" + Describe(chest.rewards[indices[0]]) + "\n으로 교체할까요?"; // 기존 묶음과 새 보상 비교
            confirmationNotice.text = string.Empty; // 이전 오류 초기화
            selectionPanel.SetActive(false); // 슬롯 선택 숨김
            confirmationPanel.SetActive(true); // 최종 확인 표시
            if (EventSystem.current != null) // 기본 선택 취소
            { // 조건 분기 시작
                EventSystem.current.SetSelectedGameObject(cancelConfirmation.gameObject); // 조건 동작 처리
            } // 조건 분기 끝
        } // 선택 끝
        private string SlotSnapshot(int index) // 확인할 슬롯의 주요 값 조회
        { // 조회 시작
            var slot = owner.Inventory.Slots[index]; // 선택한 보유 슬롯
            return slot.ItemId + "\n" + slot.Name + "\n" + slot.Quantity + "\n" + slot.StackLimit + "\n" + (slot.Equipment == null ? "" : JsonUtility.ToJson(slot.Equipment)); // 묶음과 장비 전체 값
        } // 조회 끝
        private void ConfirmReplacement() // 확인된 슬롯의 교체 요청
        { // 교체 시작
            var indices = SelectedIndices(); // 선택 보상 번호
            var chest = owner.CurrentChest; // 현재 상자
            if (!confirmationPanel.activeSelf || indices.Length != 1 || replacementIndex < 0 || replacementIndex >= owner.Inventory.Slots.Count || chest == null || chest.rewards == null || indices[0] >= chest.rewards.Length || expectedSlot != SlotSnapshot(replacementIndex) || expectedCandidate != JsonUtility.ToJson(chest.rewards[indices[0]])) // 확인 이후 슬롯과 후보 변경 검사
            { // 오류 시작
                confirmationNotice.text = "보유 아이템 또는 보상이 변경되었습니다. 다시 선택하세요."; // 변경 경고
                return; // 확인되지 않은 아이템 교체 차단
            } // 오류 끝
            if (!owner.TryClaimChest(indices, replacementIndex, out var message)) // 보상 교체와 저장 요청
            { // 오류 시작
                confirmationNotice.text = message; // 저장 오류 안내
            } // 오류 끝
        } // 교체 끝
        private void CancelSelection() // 교체 슬롯 선택 취소
        { // 취소 시작
            selectionPanel.SetActive(false); // 슬롯 선택 숨김
            replacementIndex = -1; // 교체 슬롯 해제
            if (EventSystem.current != null) // 이전 선택 복귀
            { // 조건 분기 시작
                EventSystem.current.SetSelectedGameObject(replaceButton.gameObject); // 조건 동작 처리
            } // 조건 분기 끝
        } // 취소 끝
        private void CancelConfirmation() // 교체 확인 취소
        { // 취소 시작
            confirmationPanel.SetActive(false); // 확인 화면 숨김
            OpenSelection(); // 슬롯 선택 복귀
        } // 취소 끝
        private void Close() // 상자 화면 닫기
        { // 닫기 시작
            if (confirmationPanel.activeSelf || selectionPanel.activeSelf) // 교체 확인 중 기본 동작 검사
            { // 중복 입력 차단 시작
                return; // 확인 전 획득과 화면 종료 차단
            } // 중복 입력 차단 끝
            owner.CloseChest(); // 회차 관리자에 닫기 요청
        } // 닫기 끝
        public void OnCancel(BaseEventData eventData) // ESC와 게임패드 취소
        { // 취소 시작
            eventData.Use(); // 다른 화면 취소 입력 차단
            if (confirmationPanel.activeSelf) // 확인 단계 검사
            { // 확인 취소 시작
                CancelConfirmation(); // 슬롯 선택 복귀
            } // 확인 취소 끝
            else if (selectionPanel.activeSelf) // 슬롯 선택 단계 검사
            { // 선택 취소 시작
                CancelSelection(); // 상자 화면 복귀
            } // 선택 취소 끝
            else // 일반 상자 화면
            { // 닫기 시작
                Close(); // 상자 화면 닫기
            } // 닫기 끝
        } // 취소 끝
    } // 클래스 끝
    public sealed class ChestCancelRelay : MonoBehaviour, ICancelHandler // 선택 버튼의 취소 입력 중계
    { // 클래스 시작
        private ChestScreenView screen; // 연결된 상자 화면
        public void Initialize(ChestScreenView target) // 화면 연결
        { // 연결 시작
            screen = target; // 상자 화면 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // ESC 취소 입력 전달
        { // 전달 시작
            if (screen != null) // 활성 상자에 취소 전달
            { // 조건 분기 시작
                screen.OnCancel(eventData); // 조건 동작 처리
            } // 조건 분기 끝
        } // 전달 끝
    } // 클래스 끝
} // 이름 공간 끝
