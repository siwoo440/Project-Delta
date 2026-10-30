using System; // 화면 생성 오류 처리
using UnityEngine; // 화면 객체와 위치
using UnityEngine.EventSystems; // 기본 선택과 취소 입력
using UnityEngine.UI; // 버튼과 안내 표시
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class TrialRestScreenView : MonoBehaviour, ICancelHandler // 휴식 방 선택 화면
    { // 클래스 시작
        private AppRoot owner; // 실제 회차 관리자
        private Text notice; // 회복과 저장 오류 안내
        private bool busy; // 중복 입력 차단
        public static TrialRestScreenView Create(AppRoot owner) // 휴식 선택 화면 생성
        { // 생성 시작
            if (owner == null || EventSystem.current == null) // 회차와 입력 관리자 검사
            { // 오류 시작
                throw new InvalidOperationException("Missing run or input system."); // 조작 불가능한 화면 차단
            } // 오류 끝
            var root = new GameObject("TrialRestScreen", typeof(TrialRestScreenView)); // 휴식 화면 객체
            try // 화면 생성 보호
            { // 생성 보호 시작
                var view = root.GetComponent<TrialRestScreenView>(); // 실제 화면 제어
                view.owner = owner; // 회차 관리자 연결
                view.Build(); // 휴식 화면 구성
                return view; // 생성된 화면 전달
            } // 생성 보호 끝
            catch // 생성 실패 검사
            { // 실패 정리 시작
                root.SetActive(false); // 불완전한 화면 숨김
                Destroy(root); // 불완전한 화면 제거
                throw; // 실제 생성 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void Build() // 휴식 선택과 안내 표시
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("TrialRestCanvas"); // 휴식 전용 화면
            canvas.transform.SetParent(transform, false); // 화면 상위 객체 연결
            canvas.sortingOrder = 115; // 탐험 화면보다 앞에 표시
            var background = new GameObject("TrialRestBackground", typeof(RectTransform), typeof(Image)); // 화면 배경 객체
            background.transform.SetParent(canvas.transform, false); // 배경의 화면 연결
            var rect = background.GetComponent<RectTransform>(); // 배경 크기 조회
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 0.97f); // 휴식 화면 배경색
            UiFactory.Decorate(background.GetComponent<Image>(), "DialoguePanel"); // 화면 용도별 그림 장식
            UiFactory.CreateText(canvas.transform, "TrialRestTitle", "휴식 공간", 48, new Vector2(0f, 300f), new Vector2(1000f, 90f), Color.white); // 휴식 화면 제목
            UiFactory.CreateText(canvas.transform, "TrialRestDescription", "휴식하면 체력과 마나를 각각 최대치의 30% 회복합니다.\n이 휴식 공간은 한 번만 사용할 수 있습니다. 정력 회복은 준비 중입니다.", 28, new Vector2(0f, 80f), new Vector2(1300f, 180f), Color.white); // 회복량과 사용 제한 안내
            notice = UiFactory.CreateText(canvas.transform, "TrialRestNotice", "", 24, new Vector2(0f, -80f), new Vector2(1300f, 90f), Color.white); // 저장 실패 안내
            var rest = UiFactory.CreateButton(canvas.transform, "TrialRestConfirm", "휴식", new Vector2(-210f, -260f)); // 실제 휴식 선택
            var leave = UiFactory.CreateButton(canvas.transform, "TrialRestLeave", "나중에", new Vector2(210f, -260f)); // 휴식 보류 선택
            rest.onClick.AddListener(Confirm); // 회복과 저장 연결
            leave.onClick.AddListener(Close); // 회복 없이 닫기 연결
            rest.gameObject.AddComponent<TrialRestCancelRelay>().Initialize(this); // 휴식 버튼 취소 입력
            leave.gameObject.AddComponent<TrialRestCancelRelay>().Initialize(this); // 보류 버튼 취소 입력
            EventSystem.current.SetSelectedGameObject(leave.gameObject); // 사용하지 않는 기본 선택
        } // 구성 끝
        private void Confirm() // 회복과 사용 완료 저장
        { // 확정 시작
            if (busy || owner == null) // 중복 요청과 회차 검사
            { // 차단 시작
                return; // 잘못된 요청 차단
            } // 차단 끝
            busy = true; // 중복 입력 잠금
            if (!owner.TryUseTrialRest(out var message)) // 회복과 사용 기록의 단일 저장 검사
            { // 오류 시작
                notice.text = message; // 실제 저장 오류 안내
                busy = false; // 저장 실패 후 재시도 허용
                return; // 기존 화면 보존
            } // 오류 끝
            owner.CloseTrialRest(); // 저장 성공 후 탐험 복귀
        } // 확정 끝
        public void Close() // 회복 없이 탐험 복귀
        { // 닫기 시작
            if (!busy && owner != null) // 진행 중 저장과 관리자 검사
            { // 닫기 조건 시작
                owner.CloseTrialRest(); // 실제 휴식 화면 종료
            } // 닫기 조건 끝
        } // 닫기 끝
        public void OnCancel(BaseEventData eventData) // 취소 입력 처리
        { // 취소 시작
            eventData.Use(); // 다른 화면으로 입력 전달 차단
            Close(); // 보류 선택과 동일한 종료
        } // 취소 끝
    } // 클래스 끝
    public sealed class TrialRestCancelRelay : MonoBehaviour, ICancelHandler // 버튼의 취소 입력 전달
    { // 클래스 시작
        private TrialRestScreenView screen; // 실제 휴식 화면
        public void Initialize(TrialRestScreenView value) // 취소할 화면 연결
        { // 연결 시작
            screen = value; // 화면 참조 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // 버튼의 취소 입력 처리
        { // 취소 시작
            if (screen != null) // 실제 화면 검사
            { // 전달 시작
                screen.OnCancel(eventData); // 휴식 화면에 취소 전달
            } // 전달 끝
        } // 취소 끝
    } // 클래스 끝
} // 이름 공간 끝
