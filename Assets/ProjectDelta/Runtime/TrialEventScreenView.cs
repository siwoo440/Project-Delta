using System; // 화면 생성 오류 처리
using UnityEngine; // 화면 객체와 위치
using UnityEngine.EventSystems; // 화면 선택과 취소 입력
using UnityEngine.UI; // 화면 구성 요소
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class TrialEventScreenView : MonoBehaviour, ICancelHandler // 시험 이벤트 선택과 결과 화면
    { // 클래스 시작
        private AppRoot owner; // 현재 회차 관리자
        private Text title; // 이벤트 제목
        private Text description; // 선택 또는 결과 설명
        private Text requirement; // 행동 조건과 성공률
        private Text notice; // 저장 실패와 취소 안내
        private readonly Button[] choices = new Button[2]; // 저장된 순서의 선택 버튼
        private Button confirm; // 저장된 결과 확인 버튼
        private bool busy; // 같은 화면의 중복 처리 차단
        public static TrialEventScreenView Create(AppRoot owner) // 현재 이벤트 화면 생성
        { // 생성 시작
            if (owner == null || owner.CurrentEvent == null || EventSystem.current == null) // 실제 이벤트와 입력 관리자 검사
            { // 오류 시작
                throw new InvalidOperationException("Missing event state or input system."); // 조작 불가능한 화면 차단
            } // 오류 끝
            var root = new GameObject("TrialEventScreen", typeof(TrialEventScreenView)); // 이벤트 화면 상위 객체
            try // 화면 생성 보호
            { // 생성 보호 시작
                var view = root.GetComponent<TrialEventScreenView>(); // 화면 제어 객체
                view.owner = owner; // 회차 관리자 연결
                view.Build(); // 실제 화면 구성
                view.Refresh(); // 저장된 선택 또는 결과 표시
                return view; // 생성된 화면 전달
            } // 생성 보호 끝
            catch // 생성 실패 검사
            { // 실패 정리 시작
                root.SetActive(false); // 불완전한 화면 숨김
                Destroy(root); // 불완전한 화면 제거
                throw; // 실제 생성 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void Build() // 이벤트 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("TrialEventCanvas"); // 이벤트 전용 화면
            canvas.transform.SetParent(transform, false); // 화면 상위 객체 연결
            canvas.sortingOrder = 115; // 탐험과 인벤토리 앞 표시
            var background = new GameObject("TrialEventBackground", typeof(RectTransform), typeof(Image)); // 전체 화면 배경
            background.transform.SetParent(canvas.transform, false); // 배경의 화면 연결
            var backgroundRect = background.GetComponent<RectTransform>(); // 배경 크기 조회
            backgroundRect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            backgroundRect.anchorMax = Vector2.one; // 오른쪽 위 기준
            backgroundRect.offsetMin = Vector2.zero; // 아래 여백 제거
            backgroundRect.offsetMax = Vector2.zero; // 위 여백 제거
            background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 이벤트 화면 바탕색
            UiFactory.Decorate(background.GetComponent<Image>(), "DialoguePanel"); // 화면 용도별 그림 장식
            var accent = new Color(0.89f, 0.77f, 0.48f, 1f); // 시험 이벤트 강조색
            title = UiFactory.CreateText(canvas.transform, "TrialEventTitle", "", 48, new Vector2(0f, 390f), new Vector2(1500f, 90f), Color.white); // 이벤트 제목
            description = UiFactory.CreateText(canvas.transform, "TrialEventDescription", "", 32, new Vector2(0f, 190f), new Vector2(1400f, 230f), Color.white); // 선택과 결과 설명
            description.resizeTextForBestFit = true; // 긴 결과의 영역 맞춤
            description.resizeTextMinSize = 20; // 결과 최소 글자 크기
            requirement = UiFactory.CreateText(canvas.transform, "TrialEventRequirement", "", 26, new Vector2(0f, 25f), new Vector2(1400f, 100f), accent); // 조건과 판정 확률
            requirement.resizeTextForBestFit = true; // 긴 조건의 영역 맞춤
            requirement.resizeTextMinSize = 18; // 조건 최소 글자 크기
            notice = UiFactory.CreateText(canvas.transform, "TrialEventNotice", "", 25, new Vector2(0f, -340f), new Vector2(1450f, 100f), Color.white); // 저장 오류와 이탈 안내
            for (var index = 0; index < choices.Length; index++) // 저장된 순서의 버튼 생성
            { // 버튼 생성 시작
                var buttonIndex = index; // 버튼별 선택 위치
                var button = UiFactory.CreateButton(canvas.transform, "TrialEventChoice_" + index, "", new Vector2(index == 0 ? -430f : 430f, -190f)); // 선택 위치별 버튼
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(780f, 110f); // 선택 버튼 크기
                var label = button.transform.Find("Label").GetComponent<Text>(); // 선택 버튼 설명
                label.rectTransform.sizeDelta = new Vector2(740f, 105f); // 선택 글자 영역
                label.resizeTextForBestFit = true; // 긴 선택지의 영역 맞춤
                label.resizeTextMinSize = 18; // 선택지 최소 글자 크기
                button.onClick.AddListener(() => Choose(buttonIndex)); // 저장된 선택 위치 전달
                button.gameObject.AddComponent<TrialEventCancelRelay>().Initialize(this); // 선택 중 취소 입력 연결
                choices[index] = button; // 생성된 버튼 저장
            } // 버튼 생성 끝
            confirm = UiFactory.CreateButton(canvas.transform, "TrialEventConfirm", "결과 확인", new Vector2(0f, -190f)); // 결과 확인 버튼
            confirm.onClick.AddListener(Confirm); // 실제 결과 확인 연결
            confirm.gameObject.AddComponent<TrialEventCancelRelay>().Initialize(this); // 결과 중 취소 입력 연결
        } // 구성 끝
        public void Refresh() // 저장된 이벤트 단계 표시
        { // 갱신 시작
            var current = owner.CurrentEvent; // 현재 이벤트 저장 상태
            if (current == null || current.phase < 0 || current.phase > 1 || current.choices == null || current.choices.Length != choices.Length || current.choices[0] == current.choices[1]) // 표시 가능한 단계와 고유 선택지 검사
            { // 상태 오류 시작
                throw new InvalidOperationException("Invalid event display state."); // 불완전한 이벤트 화면 차단
            } // 상태 오류 끝
            var selecting = current.phase == 0; // 선택 단계 여부
            title.text = selecting ? "수상한 장치 · 시험 이벤트" : "이벤트 결과"; // 단계별 화면 제목
            description.text = selecting ? "오래된 장치가 놓여 있습니다.\n어떻게 행동할지 선택하세요." : current.outcomeText; // 시험 상황 또는 저장된 결과
            var disabled = owner.EventDisabledReason; // 행동 불가 이유와 현재 조건
            requirement.text = selecting ? "조사 비용 10 골드 · 현재 " + owner.Gold + " · 성공률 " + owner.EventProbability + "%" + (string.IsNullOrEmpty(disabled) ? string.Empty : "\n" + disabled) : string.Empty; // 실제 조건과 판정 성공률
            Button defaultButton = null; // 안전한 기본 선택
            for (var index = 0; index < choices.Length; index++) // 저장된 선택 순서 표시
            { // 선택 표시 시작
                var choice = current.choices[index]; // 현재 위치의 고정 선택
                if (choice != 0 && choice != 1) // 지원하는 선택 번호 검사
                { // 선택 오류 시작
                    throw new InvalidOperationException("Unknown event choice."); // 알 수 없는 선택지 차단
                } // 선택 오류 끝
                var button = choices[index]; // 현재 위치 버튼
                button.gameObject.SetActive(selecting); // 선택 단계에만 버튼 표시
                button.interactable = selecting && (choice != 0 || string.IsNullOrEmpty(disabled)); // 조건 미충족 행동 비활성화
                button.transform.Find("Label").GetComponent<Text>().text = choice == 0 ? "장치를 조사한다" : "떠난다"; // 실제 행동 또는 이탈 선택
                if (choice == 1) // 안전한 이탈 선택 검사
                { // 기본 선택 시작
                    defaultButton = button; // 처음에는 이탈 선택 지정
                } // 기본 선택 끝
            } // 선택 표시 끝
            confirm.gameObject.SetActive(!selecting); // 결과 확인 단계에만 버튼 표시
            if (EventSystem.current != null) // 화면 입력 관리자 검사
            { // 선택 적용 시작
                EventSystem.current.SetSelectedGameObject(selecting ? defaultButton.gameObject : confirm.gameObject); // 이탈 또는 결과 확인 기본 선택
            } // 선택 적용 끝
        } // 갱신 끝
        private void Choose(int position) // 저장된 순서의 선택 처리
        { // 선택 시작
            var current = owner.CurrentEvent; // 현재 이벤트 상태
            if (busy || current == null || current.phase != 0 || current.choices == null || position < 0 || position >= current.choices.Length) // 중복과 단계 및 위치 검사
            { // 선택 거절 시작
                return; // 오래된 선택과 중복 입력 차단
            } // 선택 거절 끝
            var choice = current.choices[position]; // 저장된 위치의 실제 선택
            if (choice == 0 && !string.IsNullOrEmpty(owner.EventDisabledReason)) // 행동 조건 검사
            { // 조건 오류 시작
                notice.text = owner.EventDisabledReason; // 실제 조건 미충족 안내
                return; // 조건 없는 행동 차단
            } // 조건 오류 끝
            busy = true; // 선택 처리 중 중복 입력 잠금
            try // 원자적 선택과 저장 처리
            { // 처리 시작
                if (!owner.TryChooseTrialEvent(choice, out var message)) // 실제 선택과 저장 검사
                { // 저장 실패 시작
                    notice.text = message; // 저장 실패 안내
                    Refresh(); // 기존 선택 상태 복구
                    return; // 저장되지 않은 결과 표시 차단
                } // 저장 실패 끝
                notice.text = string.Empty; // 이전 오류 안내 초기화
                Refresh(); // 저장된 결과 화면 표시
            } // 처리 끝
            finally // 처리 상태 복구
            { // 복구 시작
                busy = false; // 다음 확인 입력 허용
            } // 복구 끝
        } // 선택 끝
        private void Confirm() // 저장된 결과 확인과 이벤트 완료
        { // 확인 시작
            var current = owner.CurrentEvent; // 현재 이벤트 상태
            if (busy || current == null || current.phase != 1) // 중복과 결과 단계 검사
            { // 확인 거절 시작
                return; // 잘못된 결과 확인 차단
            } // 확인 거절 끝
            busy = true; // 결과 확인 중 중복 입력 잠금
            try // 결과 확인과 완료 저장
            { // 확인 처리 시작
                if (!owner.ConfirmTrialEvent(out var message)) // 실제 완료 저장 검사
                { // 저장 실패 시작
                    notice.text = message; // 저장 실패와 재시도 안내
                } // 저장 실패 끝
            } // 확인 처리 끝
            finally // 처리 상태 복구
            { // 복구 시작
                busy = false; // 다음 확인 입력 허용
            } // 복구 끝
        } // 확인 끝
        public void OnCancel(BaseEventData eventData) // ESC와 게임패드 취소 입력 차단
        { // 취소 처리 시작
            eventData.Use(); // 다른 화면의 취소 입력 차단
            var current = owner.CurrentEvent; // 현재 이벤트 단계
            notice.text = current != null && current.phase == 1 ? "결과를 확인하세요." : "떠나려면 선택지에서 떠난다를 선택하세요."; // 단계별 명시적 이탈 안내
        } // 취소 처리 끝
    } // 화면 클래스 끝
    public sealed class TrialEventCancelRelay : MonoBehaviour, ICancelHandler // 선택 버튼의 취소 입력 중계
    { // 클래스 시작
        private TrialEventScreenView screen; // 연결된 이벤트 화면
        public void Initialize(TrialEventScreenView target) // 이벤트 화면 연결
        { // 연결 시작
            screen = target; // 대상 화면 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // ESC 취소 입력 전달
        { // 전달 시작
            if (screen != null) // 연결된 화면 검사
            { // 전달 조건 시작
                screen.OnCancel(eventData); // 이벤트 화면에 취소 입력 전달
            } // 전달 조건 끝
        } // 전달 끝
    } // 클래스 끝
} // 이름 공간 끝
