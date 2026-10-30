using System; // 콜백과 화면 생성 오류
using System.Collections.Generic; // 보유 아이템 이름 목록
using ProjectDelta.Core; // 장비 부위와 시험 유물 정의
using UnityEngine; // 화면 객체와 위치
using UnityEngine.EventSystems; // 화면 선택과 취소 입력
using UnityEngine.UI; // 화면 버튼과 글자
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class BossPreparationScreenView : MonoBehaviour, ICancelHandler // 마왕 전투 전 준비 확인 화면
    { // 클래스 시작
        private AppRoot owner; // 현재 회차 관리자
        private Action enter; // 마왕 전투 진입 콜백
        private Action continuePreparation; // 탐험 준비 복귀 콜백
        private Text status; // 현재 체력과 회차 상태
        private Text equipment; // 실제 장착 장비
        private Text relics; // 실제 보유 유물
        private Text inventory; // 실제 보유 아이템
        private Text notice; // 실제 진입 실패 안내
        private Button continueButton; // 안전한 기본 복귀 선택
        private bool busy; // 중복 콜백 차단
        private bool closed; // 종료된 화면의 입력 차단
        public static BossPreparationScreenView Create(AppRoot owner, Action onEnter, Action onContinue) // 회차 상태와 선택 콜백으로 화면 생성
        { // 생성 시작
            if (owner == null || owner.Inventory == null || owner.Equipment == null || owner.Relics == null || onEnter == null || onContinue == null || EventSystem.current == null) // 회차와 입력 및 콜백 검사
            { // 생성 거절 시작
                throw new ArgumentException("Missing preparation state, callbacks or input system."); // 조작 불가능한 화면 차단
            } // 생성 거절 끝
            var root = new GameObject("BossPreparationScreen", typeof(BossPreparationScreenView)); // 준비 화면 상위 객체
            try // 화면 생성 보호
            { // 생성 보호 시작
                var view = root.GetComponent<BossPreparationScreenView>(); // 준비 화면 제어
                view.owner = owner; // 현재 회차 연결
                view.enter = onEnter; // 전투 진입 콜백 보관
                view.continuePreparation = onContinue; // 탐험 준비 복귀 콜백 보관
                view.Build(); // 실제 준비 화면 구성
                view.Refresh(); // 현재 체력과 보유 상태 표시
                view.SelectDefault(); // 안전한 준비 계속 기본 선택
                return view; // 생성된 화면 전달
            } // 생성 보호 끝
            catch // 화면 생성 실패 검사
            { // 실패 정리 시작
                root.SetActive(false); // 불완전한 화면 숨김
                Destroy(root); // 불완전한 화면 제거
                throw; // 실제 생성 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void Build() // 전투 전 준비 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("BossPreparationCanvas"); // 마왕 준비 전용 화면
            canvas.transform.SetParent(transform, false); // 화면 상위 객체 연결
            canvas.sortingOrder = 115; // 탐험 화면 앞 표시
            var background = new GameObject("BossPreparationBackground", typeof(RectTransform), typeof(Image)); // 전체 화면 배경
            background.transform.SetParent(canvas.transform, false); // 준비 화면 아래 배치
            var rect = background.GetComponent<RectTransform>(); // 배경 위치와 크기
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 준비 화면 바탕색
            UiFactory.Decorate(background.GetComponent<Image>(), "BattlePanel"); // 화면 용도별 그림 장식
            var accent = new Color(0.85f, 0.72f, 0.47f, 1f); // 준비 정보 강조색
            UiFactory.CreateText(canvas.transform, "BossPreparationTitle", "마왕 전투 준비", 46, new Vector2(0f, 455f), new Vector2(1500f, 75f), Color.white); // 전투 준비 제목
            status = UiFactory.CreateText(canvas.transform, "BossPreparationStatus", "", 28, new Vector2(0f, 365f), new Vector2(1600f, 70f), accent); // 체력과 마나 및 회차 수치
            equipment = UiFactory.CreateText(canvas.transform, "BossPreparationEquipment", "", 25, new Vector2(-470f, 135f), new Vector2(800f, 285f), Color.white); // 실제 부위별 장착 장비
            equipment.alignment = TextAnchor.UpperLeft; // 장비 정보 왼쪽 정렬
            relics = UiFactory.CreateText(canvas.transform, "BossPreparationRelics", "", 24, new Vector2(-470f, -100f), new Vector2(800f, 155f), Color.white); // 실제 유물과 효과
            relics.alignment = TextAnchor.UpperLeft; // 유물 정보 왼쪽 정렬
            inventory = UiFactory.CreateText(canvas.transform, "BossPreparationInventory", "", 24, new Vector2(450f, 110f), new Vector2(850f, 360f), Color.white); // 실제 인벤토리 목록
            inventory.alignment = TextAnchor.UpperLeft; // 소지품 정보 왼쪽 정렬
            inventory.resizeTextForBestFit = true; // 긴 아이템 목록의 표시 영역 맞춤
            inventory.resizeTextMinSize = 14; // 소지품 최소 글자 크기
            UiFactory.CreateText(canvas.transform, "BossPreparationUnavailable", "정력 · 상태이상 · 스킬 목록은 아직 구현되지 않았습니다.", 23, new Vector2(0f, -225f), new Vector2(1600f, 70f), accent); // 미구현 정보 안내
            UiFactory.CreateText(canvas.transform, "BossPreparationHint", "준비를 계속하면 탐험으로 돌아가 장비와 소지품을 정리할 수 있습니다.", 22, new Vector2(0f, -290f), new Vector2(1600f, 60f), Color.white); // 준비 복귀 안내
            notice = UiFactory.CreateText(canvas.transform, "BossPreparationNotice", "", 22, new Vector2(0f, -350f), new Vector2(1500f, 55f), Color.white); // 전투 진입 실패 안내
            MakeButton(canvas.transform, "BossPreparationEnter", "마왕 전투 진입", new Vector2(-220f, -435f), Enter); // 실제 전투 진입 선택
            continueButton = MakeButton(canvas.transform, "BossPreparationContinue", "준비 계속", new Vector2(220f, -435f), Continue); // 안전한 탐험 복귀 선택
        } // 구성 끝
        private Button MakeButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action) // 취소 입력이 연결된 준비 버튼 생성
        { // 생성 시작
            var button = UiFactory.CreateButton(parent, name, label, position); // 공통 준비 화면 버튼
            button.onClick.AddListener(action); // 실제 선택 콜백 연결
            button.gameObject.AddComponent<BossPreparationCancelRelay>().Initialize(this); // 버튼의 취소 입력 연결
            return button; // 생성된 버튼 전달
        } // 생성 끝
        public void Refresh() // 실제 현재 회차의 준비 정보 갱신
        { // 갱신 시작
            if (closed || owner == null || owner.Inventory == null || owner.Equipment == null || owner.Relics == null) // 화면과 회차 구성 검사
            { // 갱신 거절 시작
                return; // 종료되거나 누락된 상태의 표시 차단
            } // 갱신 거절 끝
            status.text = "레벨 " + owner.Level + " · 경험치 " + owner.Experience + " · 체력 " + owner.PlayerHealth + "/100 · 마나 " + owner.PlayerMana + "/50 · 골드 " + owner.Gold; // 실제 현재 회차 수치
            equipment.text = "장착 장비\n무기: " + EquipmentName(EquipmentSlot.Weapon) + "\n방어구: " + EquipmentName(EquipmentSlot.Armor) + "\n장신구: " + EquipmentName(EquipmentSlot.Accessory) + "\n가방: " + EquipmentName(EquipmentSlot.Bag); // 실제 부위별 장비 이름
            var relicNames = new List<string>(); // 현재 보유 유물 이름
            foreach (var id in owner.Relics.SaveIds()) // 실제 회차 유물 순회
            { // 유물 이름 검사 시작
                var definition = TrialRelicCatalog.Find(id); // 현재 등록된 유물 정의
                relicNames.Add(definition == null ? id : definition.Name); // 확인된 이름 또는 원본 식별자
            } // 유물 이름 검사 끝
            relics.text = "유물 " + owner.Relics.Count + "/" + RelicState.Capacity + "\n" + (relicNames.Count == 0 ? "없음" : string.Join(", ", relicNames)) + "\n공격 +" + owner.Relics.AttackBonus + " · 방어 +" + owner.Relics.DefenseBonus + " · 속도 +" + owner.Relics.SpeedBonus; // 실제 보유 유물과 현재 효과
            var items = new List<string>(); // 실제 소지품 요약 목록
            foreach (var slot in owner.Inventory.Slots) // 현재 회차 슬롯 순회
            { // 슬롯 검사 시작
                if (!slot.IsEmpty) // 보유 중인 아이템 검사
                { // 소지품 표시 시작
                    items.Add(slot.Name + " × " + slot.Quantity); // 실제 이름과 묶음 수량
                } // 소지품 표시 끝
            } // 슬롯 검사 끝
            inventory.text = "소지품 " + items.Count + "/" + owner.Inventory.Slots.Count + "칸\n" + (items.Count == 0 ? "없음" : string.Join("\n", items)); // 실제 보유 묶음과 용량
        } // 갱신 끝
        private string EquipmentName(EquipmentSlot slot) // 현재 부위의 실제 장비 이름
        { // 이름 조회 시작
            var item = owner.Equipment.Get(slot); // 현재 부위 장비 정의
            return item == null ? "없음" : item.Name; // 장착 장비 또는 빈 부위
        } // 이름 조회 끝
        public void ShowMessage(string message) // 콜백 실패의 실제 안내 표시
        { // 안내 시작
            if (!closed && notice != null) // 활성 화면 검사
            { // 표시 시작
                notice.text = message ?? string.Empty; // 확인된 오류 또는 빈 안내
            } // 표시 끝
        } // 안내 끝
        public void SelectDefault() // 안전한 준비 계속 버튼 선택
        { // 선택 시작
            if (!closed && EventSystem.current != null && continueButton != null && gameObject.activeInHierarchy) // 화면과 입력 관리자 검사
            { // 기본 선택 시작
                EventSystem.current.SetSelectedGameObject(continueButton.gameObject); // 우발적 전투 진입 방지
            } // 기본 선택 끝
        } // 선택 끝
        private void Enter() // 실제 마왕 전투 진입 콜백
        { // 진입 시작
            InvokeCallback(enter); // 저장과 진입을 담당하는 콜백 전달
        } // 진입 끝
        private void Continue() // 탐험 준비 계속 콜백
        { // 복귀 시작
            InvokeCallback(continuePreparation); // 기존 탐험 복귀를 담당하는 콜백 전달
        } // 복귀 끝
        private void InvokeCallback(Action action) // 확인된 단일 화면 행동 실행
        { // 호출 시작
            if (closed || busy || action == null) // 종료와 중복 및 콜백 검사
            { // 호출 거절 시작
                return; // 중복 화면 전환 차단
            } // 호출 거절 끝
            busy = true; // 콜백 실행 중 입력 잠금
            try // 실제 호출 처리
            { // 호출 보호 시작
                action(); // 호출자가 제공한 화면 전환 처리
            } // 호출 보호 끝
            finally // 입력 상태 복구
            { // 복구 시작
                busy = false; // 다음 선택 입력 허용
            } // 복구 끝
        } // 호출 끝
        public void Close() // 콜백 없이 준비 화면 제거
        { // 종료 시작
            if (closed) // 중복 종료 검사
            { // 종료 거절 시작
                return; // 이미 종료된 화면 보호
            } // 종료 거절 끝
            closed = true; // 이전 버튼의 재입력 차단
            gameObject.SetActive(false); // 즉시 화면과 입력 숨김
            Destroy(gameObject); // 현재 준비 화면 제거
        } // 종료 끝
        public void OnCancel(BaseEventData eventData) // ESC와 게임패드 취소 입력
        { // 취소 시작
            eventData.Use(); // 다른 화면의 취소 입력 차단
            Continue(); // 안전한 준비 계속 콜백
        } // 취소 끝
    } // 화면 클래스 끝
    public sealed class BossPreparationCancelRelay : MonoBehaviour, ICancelHandler // 준비 버튼의 취소 입력 중계
    { // 클래스 시작
        private BossPreparationScreenView screen; // 연결된 준비 화면
        public void Initialize(BossPreparationScreenView target) // 준비 화면 연결
        { // 연결 시작
            screen = target; // 대상 화면 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // ESC 취소 입력 전달
        { // 전달 시작
            if (screen != null) // 연결된 화면 검사
            { // 전달 조건 시작
                screen.OnCancel(eventData); // 준비 화면에 취소 입력 전달
            } // 전달 조건 끝
        } // 전달 끝
    } // 클래스 끝
} // 이름 공간 끝
