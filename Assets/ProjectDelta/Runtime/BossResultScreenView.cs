using System; // 콜백과 화면 생성 오류
using UnityEngine; // 화면 객체와 위치
using UnityEngine.EventSystems; // 기본 선택과 취소 입력
using UnityEngine.UI; // 화면 배경과 버튼
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class BossResultScreenView : MonoBehaviour, ICancelHandler // 저장된 마왕 전투 결과 대기 화면
    { // 클래스 시작
        private Action onTitle; // 타이틀 복귀 콜백
        private Text notice; // 전투 결과와 선택 안내
        private Button titleButton; // 타이틀 복귀 버튼
        private bool busy; // 중복 콜백 차단
        private bool closed; // 종료된 화면의 입력 차단
        public static BossResultScreenView Create(int outcome, Action onTitle) // 저장된 결과와 복귀 콜백으로 화면 생성
        { // 생성 시작
            if (outcome < 1 || outcome > 3 || onTitle == null || EventSystem.current == null) // 결과와 콜백 및 입력 관리자 검사
            { // 생성 거절 시작
                throw new ArgumentException("Invalid boss result or input state."); // 조작 불가능한 결과 화면 차단
            } // 생성 거절 끝
            var root = new GameObject("BossResultScreen", typeof(BossResultScreenView)); // 결과 화면 상위 객체
            try // 실제 화면 생성 보호
            { // 생성 보호 시작
                var view = root.GetComponent<BossResultScreenView>(); // 결과 화면 제어
                view.onTitle = onTitle; // 타이틀 복귀 콜백 보관
                view.Build(outcome); // 저장된 결과 화면 구성
                view.SelectDefault(); // 실제 타이틀 버튼 기본 선택
                return view; // 생성된 화면 전달
            } // 생성 보호 끝
            catch // 화면 생성 실패 검사
            { // 실패 정리 시작
                root.SetActive(false); // 불완전한 화면 숨김
                Destroy(root); // 불완전한 화면 제거
                throw; // 실제 생성 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void Build(int outcome) // 저장된 승패 결과 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("BossResultCanvas"); // 결과 전용 화면
            canvas.transform.SetParent(transform, false); // 결과 화면 상위 객체 연결
            canvas.sortingOrder = 125; // 전투 화면보다 앞 표시
            var background = new GameObject("BossResultBackground", typeof(RectTransform), typeof(Image)); // 전체 화면 배경
            background.transform.SetParent(canvas.transform, false); // 결과 화면 아래 배치
            var rect = background.GetComponent<RectTransform>(); // 배경 위치와 크기
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 결과 화면 바탕색
            var heading = outcome == 1 ? "마왕 전투 승리" : outcome == 2 ? "마왕 전투 패배" : "마왕 전투 항복"; // 실제 저장된 전투 결과
            var color = outcome == 1 ? new Color(0.85f, 0.75f, 0.48f, 1f) : new Color(0.86f, 0.55f, 0.53f, 1f); // 승리 또는 종료 강조색
            UiFactory.CreateText(canvas.transform, "BossResultHeading", heading, 48, new Vector2(0f, 195f), new Vector2(1400f, 110f), color); // 저장된 전투 결과 제목
            UiFactory.CreateText(canvas.transform, "BossResultSaved", "결과 저장됨 · 정식 결말 준비 중", 30, new Vector2(0f, 55f), new Vector2(1400f, 80f), Color.white); // 실제 결과 저장과 결말 준비 안내
            notice = UiFactory.CreateText(canvas.transform, "BossResultNotice", "", 24, new Vector2(0f, -65f), new Vector2(1400f, 70f), Color.white); // 타이틀 복귀 안내
            titleButton = UiFactory.CreateButton(canvas.transform, "BossResultTitleButton", "타이틀로", new Vector2(0f, -220f)); // 유일한 결과 화면 종료 선택
            titleButton.onClick.AddListener(ReturnToTitle); // 저장된 결과의 타이틀 복귀 연결
            titleButton.gameObject.AddComponent<BossResultCancelRelay>().Initialize(this); // 버튼의 취소 입력 차단
        } // 구성 끝
        public void SelectDefault() // 타이틀 복귀 버튼 기본 선택
        { // 선택 시작
            if (!closed && EventSystem.current != null && titleButton != null && gameObject.activeInHierarchy) // 활성 화면과 입력 관리자 검사
            { // 선택 적용 시작
                EventSystem.current.SetSelectedGameObject(titleButton.gameObject); // 실제 유일한 버튼 선택
            } // 선택 적용 끝
        } // 선택 끝
        public void ShowMessage(string message) // 콜백 실패 시 실제 안내 표시
        { // 표시 시작
            if (!closed && notice != null) // 활성 결과 화면 검사
            { // 안내 시작
                notice.text = message ?? string.Empty; // 확인된 오류 또는 빈 안내
            } // 안내 끝
        } // 표시 끝
        private void ReturnToTitle() // 타이틀 복귀 콜백 요청
        { // 복귀 시작
            if (closed || busy || onTitle == null) // 화면과 중복 요청 검사
            { // 복귀 거절 시작
                return; // 중복 화면 전환 차단
            } // 복귀 거절 끝
            busy = true; // 콜백 실행 중 입력 잠금
            try // 실제 타이틀 복귀 요청
            { // 요청 보호 시작
                onTitle(); // 호출자가 제공한 안전한 전환 처리
            } // 요청 보호 끝
            finally // 입력 상태 복구
            { // 복구 시작
                busy = false; // 실패 후 다음 선택 허용
            } // 복구 끝
        } // 복귀 끝
        public void Close() // 콜백 없이 결과 화면 정리
        { // 정리 시작
            if (closed) // 중복 정리 검사
            { // 정리 거절 시작
                return; // 이미 종료된 화면 보호
            } // 정리 거절 끝
            closed = true; // 이전 버튼의 재입력 차단
            gameObject.SetActive(false); // 즉시 화면과 입력 숨김
            Destroy(gameObject); // 결과 화면 객체 제거
        } // 정리 끝
        public void OnCancel(BaseEventData eventData) // ESC와 게임패드 취소 입력 차단
        { // 취소 시작
            eventData.Use(); // 다른 화면의 취소 입력 차단
            ShowMessage("타이틀로 버튼을 눌러 주세요."); // 명시적 복귀 버튼 안내
            SelectDefault(); // 실제 타이틀 버튼 선택 복구
        } // 취소 끝
    } // 화면 클래스 끝
    public sealed class BossResultCancelRelay : MonoBehaviour, ICancelHandler // 결과 버튼의 취소 입력 중계
    { // 클래스 시작
        private BossResultScreenView screen; // 연결된 결과 화면
        public void Initialize(BossResultScreenView target) // 결과 화면 연결
        { // 연결 시작
            screen = target; // 대상 화면 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // ESC 취소 입력 전달
        { // 전달 시작
            if (screen != null) // 연결된 화면 검사
            { // 전달 조건 시작
                screen.OnCancel(eventData); // 결과 화면에 취소 입력 전달
            } // 전달 조건 끝
        } // 전달 끝
    } // 클래스 끝
} // 이름 공간 끝
