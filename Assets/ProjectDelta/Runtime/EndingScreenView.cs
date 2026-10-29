using System; // 화면 선택 콜백
using ProjectDelta.Core; // 결말 이름과 후보 규칙
using UnityEngine; // 화면 객체와 색상
using UnityEngine.EventSystems; // 기본 버튼 선택
using UnityEngine.UI; // 화면 글자와 버튼
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class EndingScreenView : MonoBehaviour // 저장된 결말 선택과 결과 화면
    { // 클래스 시작
        private Transform canvas; // 실제 화면 영역
        private GameObject choices; // 방향과 후보 선택 영역
        private GameObject results; // 영구 저장된 결말 결과 영역
        private Text notice; // 저장 실패와 시험 범위 안내
        private Text resultNotice; // 회차 종료 실패 안내
        private Action<string> onChoose; // 실제 후보 선택 콜백
        private Action onFinish; // 결과 확인 콜백
        private Button firstReturn; // 귀환 선택 버튼
        private Button firstStay; // 잔류 선택 버튼
        private GameObject returnCandidates; // 귀환 결말 후보 목록
        private GameObject stayCandidates; // 잔류 결말 후보 목록
        public static EndingScreenView Create(string[] returnIds, string[] stayIds, Action<string> choose, Action finish) // 확인된 후보의 화면 구성
        { // 생성 시작
            if (returnIds == null || stayIds == null || choose == null || finish == null || EventSystem.current == null) // 후보와 화면 입력 검사
            { // 오류 시작
                throw new ArgumentException("Missing ending candidates or callbacks."); // 잘못된 화면 차단
            } // 오류 끝
            var root = new GameObject("EndingScreen", typeof(EndingScreenView)); // 독립 결말 화면
            var view = root.GetComponent<EndingScreenView>(); // 실제 화면 제어
            view.onChoose = choose; // 결말 확정 콜백
            view.onFinish = finish; // 결과 종료 콜백
            view.Build(returnIds, stayIds); // 후보와 결과 화면 생성
            return view; // 완성된 화면 전달
        } // 생성 끝
        private void Build(string[] returnIds, string[] stayIds) // 실제 결말 화면 구성
        { // 구성 시작
            var layer = UiFactory.CreateCanvas("EndingCanvas"); // 결말 전용 화면
            layer.transform.SetParent(transform, false); // 독립 상위 객체 연결
            layer.sortingOrder = 130; // 다른 게임 화면 앞 표시
            canvas = layer.transform; // 결말 화면 영역 보관
            var background = new GameObject("EndingBackground", typeof(RectTransform), typeof(Image)); // 전체 배경
            background.transform.SetParent(canvas, false); // 화면 아래 배치
            var rect = background.GetComponent<RectTransform>(); // 배경 좌표
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 위 여백 제거
            background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 어두운 결과 배경
            choices = new GameObject("EndingChoices", typeof(RectTransform)); // 선택 영역 객체
            choices.transform.SetParent(canvas, false); // 화면 안에 배치
            UiFactory.CreateText(choices.transform, "EndingTitle", "마왕 전투 이후의 선택", 43, new Vector2(0f, 390f), new Vector2(1500f, 85f), Color.white); // 결말 선택 제목
            UiFactory.CreateText(choices.transform, "EndingLimit", "관계 · 정력 · 도감 등 미구현 조건은 판정하지 않습니다.", 22, new Vector2(0f, 315f), new Vector2(1550f, 55f), new Color(0.85f, 0.72f, 0.47f)); // 시험 범위 안내
            firstReturn = UiFactory.CreateButton(choices.transform, "EndingBranchReturn", "현실로 귀환", new Vector2(-230f, 220f)); // 현실 귀환 방향
            firstStay = UiFactory.CreateButton(choices.transform, "EndingBranchStay", "던전에 잔류", new Vector2(230f, 220f)); // 던전 잔류 방향
            returnCandidates = MakeCandidates("ReturnCandidates", returnIds); // 귀환 후보 목록
            stayCandidates = MakeCandidates("StayCandidates", stayIds); // 잔류 후보 목록
            firstReturn.onClick.AddListener(() => ShowBranch(true)); // 귀환 후보 표시
            firstStay.onClick.AddListener(() => ShowBranch(false)); // 잔류 후보 표시
            notice = UiFactory.CreateText(choices.transform, "EndingNotice", "방향을 선택한 뒤 결말을 직접 선택하세요.", 24, new Vector2(0f, -350f), new Vector2(1500f, 90f), Color.white); // 저장 오류와 선택 안내
            returnCandidates.SetActive(false); // 초기 후보 목록 숨김
            stayCandidates.SetActive(false); // 초기 후보 목록 숨김
            results = new GameObject("EndingResult", typeof(RectTransform)); // 확정 결과 영역
            results.transform.SetParent(canvas, false); // 화면 안에 배치
            results.SetActive(false); // 영구 저장 전 결과 숨김
            EventSystem.current.SetSelectedGameObject(firstReturn.gameObject); // 기본 방향 선택
        } // 구성 끝
        private GameObject MakeCandidates(string name, string[] ids) // 방향별 결말 선택 버튼 생성
        { // 생성 시작
            var parent = new GameObject(name, typeof(RectTransform)); // 후보 목록 객체
            parent.transform.SetParent(choices.transform, false); // 선택 영역 아래 배치
            for (var index = 0; index < ids.Length; index++) // 고정 후보 순회
            { // 후보 구성 시작
                var id = ids[index]; // 현재 후보 식별자 복사
                var label = EndingRules.DisplayName(id); // 기획의 실제 표시 이름
                if (label == null) // 유효한 결말 이름 검사
                { // 오류 시작
                    throw new ArgumentException("Unknown ending candidate."); // 잘못된 후보 표시 차단
                } // 오류 끝
                var button = UiFactory.CreateButton(parent.transform, "EndingCandidate_" + id, label, new Vector2(0f, 120f - index * 90f)); // 후보별 실제 선택 버튼
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, 72f); // 긴 결말 이름의 표시 너비
                button.onClick.AddListener(() => onChoose(id)); // 해당 후보의 실제 확정 요청
            } // 후보 구성 끝
            return parent; // 완성된 후보 목록 전달
        } // 생성 끝
        private void ShowBranch(bool returning) // 사용자가 고른 방향의 후보 표시
        { // 표시 시작
            returnCandidates.SetActive(returning); // 귀환 후보 표시 전환
            stayCandidates.SetActive(!returning); // 잔류 후보 표시 전환
            notice.text = "조건을 확인한 결말 중 하나를 선택하세요."; // 직접 선택 안내
            var target = returning ? returnCandidates : stayCandidates; // 실제 선택한 후보 목록
            if (target.transform.childCount > 0) // 후보 존재 검사
            { // 선택 시작
                EventSystem.current.SetSelectedGameObject(target.transform.GetChild(0).gameObject); // 첫 후보 기본 선택
            } // 선택 끝
        } // 표시 끝
        public void ShowError(string message) // 저장 실패와 재시도 안내
        { // 표시 시작
            notice.text = message ?? string.Empty; // 실제 오류 표시
        } // 표시 끝
        public void ShowRetry(string message, Action retry) // 확정된 결말 저장의 재시도 표시
        { // 표시 시작
            choices.SetActive(false); // 확정된 결말의 재선택 차단
            results.SetActive(true); // 결과 대기 영역 표시
            UiFactory.CreateText(results.transform, "EndingRetryNotice", message, 26, new Vector2(0f, 80f), new Vector2(1500f, 150f), Color.white); // 실제 저장 오류 안내
            var button = UiFactory.CreateButton(results.transform, "EndingRetryButton", "저장 다시 시도", new Vector2(0f, -100f)); // 같은 결말의 재시도 선택
            button.onClick.AddListener(() => retry()); // 기존 거래의 재시도 요청
            var title = UiFactory.CreateButton(results.transform, "EndingRetryTitleButton", "타이틀로", new Vector2(0f, -200f)); // 재실행과 저장 확인을 위한 복귀 선택
            title.onClick.AddListener(() => // 저장된 회차를 유지한 복귀 요청
            { // 복귀 시작
                Close(); // 재시도 화면 닫기
                AppRoot.Instance.ReturnToTitle(); // 기존 저장을 보존한 타이틀 복귀
            }); // 복귀 끝
            EventSystem.current.SetSelectedGameObject(button.gameObject); // 재시도 기본 선택
        } // 표시 끝
        public void ClearResult() // 기존 재시도 또는 결과 표시 정리
        { // 정리 시작
            foreach (Transform child in results.transform) // 결과 영역의 모든 표시 순회
            { // 자식 정리 시작
                child.gameObject.SetActive(false); // 이전 버튼 즉시 비활성화
                Destroy(child.gameObject); // 이전 표시 제거
            } // 자식 정리 끝
        } // 정리 끝
        public void ShowResult(string id, int granted, int total) // 영구 기록이 완료된 결말 결과 표시
        { // 표시 시작
            choices.SetActive(false); // 이미 확정된 선택 숨김
            results.SetActive(true); // 확인된 결과 표시
            var title = EndingRules.DisplayName(id); // 저장된 결말 이름
            UiFactory.CreateText(results.transform, "EndingResultName", title, 48, new Vector2(0f, 190f), new Vector2(1500f, 100f), Color.white); // 실제 결말 이름
            UiFactory.CreateText(results.transform, "EndingResultShards", "이번 최초 해금 파편 " + granted + " · 누적 획득 파편 " + total, 29, new Vector2(0f, 50f), new Vector2(1500f, 75f), new Color(0.85f, 0.72f, 0.47f)); // 실제 지급과 누적 획득 기록
            UiFactory.CreateText(results.transform, "EndingResultLimit", "난이도별 회차 완료 보상은 아직 구현되지 않았습니다.", 22, new Vector2(0f, -45f), new Vector2(1500f, 60f), Color.white); // 미구현 회차 보상 안내
            resultNotice = UiFactory.CreateText(results.transform, "EndingResultNotice", "", 22, new Vector2(0f, -110f), new Vector2(1500f, 60f), Color.white); // 회차 종료 실패 안내
            var finish = UiFactory.CreateButton(results.transform, "EndingFinishButton", "결말 확인", new Vector2(0f, -200f)); // 회차 종료 확인
            finish.onClick.AddListener(() => onFinish()); // 실제 회차 종료 요청
            EventSystem.current.SetSelectedGameObject(finish.gameObject); // 결과 확인 기본 선택
        } // 표시 끝
        public void ShowFinalError(string message) // 회차 종료 실패와 재시도 안내
        { // 표시 시작
            if (resultNotice != null) // 결과 안내 존재 검사
            { // 안내 시작
                resultNotice.text = message ?? string.Empty; // 실제 저장 오류 표시
            } // 안내 끝
        } // 표시 끝
        public void Close() // 화면을 즉시 닫고 제거
        { // 닫기 시작
            gameObject.SetActive(false); // 기존 입력 즉시 차단
            Destroy(gameObject); // 독립 화면 제거
        } // 닫기 끝
    } // 클래스 끝
} // 이름 공간 끝
