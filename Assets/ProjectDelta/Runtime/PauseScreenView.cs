using System; // 기록 페이지 범위 계산
using System.Collections.Generic; // 현재 페이지 기록 목록
using UnityEngine; // 메뉴 화면 구성
using UnityEngine.EventSystems; // 취소 입력과 선택 관리
using UnityEngine.UI; // 버튼과 글자 표시
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class PauseScreenView : MonoBehaviour, ICancelHandler // 탐험 일시정지 메뉴
    { // 클래스 시작
        private const int PageSize = 8; // 한 페이지에 표시할 기록 수
        private AppRoot owner; // 현재 회차 관리자
        private Text entries; // 현재 회차 기록 표시
        private Text runStatus; // 현재 회차의 위치와 상태
        private Text pageLabel; // 현재 기록 페이지 표시
        private Text notice; // 저장과 불러오기 결과 표시
        private Button load; // 저장 회차 불러오기 버튼
        private int page; // 현재 기록 페이지 번호
        public static PauseScreenView Create(AppRoot owner, Transform parent) // 현재 회차 메뉴 생성
        { // 생성 시작
            var screen = new GameObject("PauseScreen", typeof(PauseScreenView)); // 메뉴 상위 객체
            screen.transform.SetParent(parent, false); // 던전 씬 수명 연결
            try // 메뉴 화면 구성
            { // 구성 시작
                var view = screen.GetComponent<PauseScreenView>(); // 메뉴 제어 조회
                view.owner = owner; // 회차 관리자 연결
                var canvas = UiFactory.CreateCanvas("PauseCanvas"); // 메뉴 화면 생성
                canvas.transform.SetParent(screen.transform, false); // 메뉴 상위 객체 연결
                canvas.sortingOrder = 200; // 탐험 화면 앞에 표시
                Panel(canvas.transform, "PauseShade", Vector2.zero, new Vector2(1920f, 1080f), new Color(0.015f, 0.025f, 0.035f, 0.8f)); // 탐험 위 반투명 배경
                Panel(canvas.transform, "PauseMenuPanel", new Vector2(-690f, 0f), new Vector2(440f, 850f), new Color(0.04f, 0.09f, 0.10f, 0.98f)); // 왼쪽 메뉴 상자
                Panel(canvas.transform, "PauseRecordPanel", new Vector2(250f, 0f), new Vector2(1300f, 850f), new Color(0.04f, 0.09f, 0.10f, 0.98f)); // 오른쪽 회차 기록 상자
                UiFactory.CreateText(canvas.transform, "PauseTitle", "메뉴", 42, new Vector2(-690f, 345f), new Vector2(380f, 70f), Color.white); // 왼쪽 메뉴 제목
                UiFactory.CreateText(canvas.transform, "PauseRecordTitle", "현재 회차 기록", 42, new Vector2(250f, 345f), new Vector2(1150f, 70f), Color.white); // 오른쪽 기록 제목
                view.runStatus = UiFactory.CreateText(canvas.transform, "PauseRunStatus", "", 24, new Vector2(250f, 265f), new Vector2(1120f, 72f), new Color(0.62f, 0.87f, 0.79f)); // 층과 성장 등 현재 회차 상태
                var resume = UiFactory.CreateButton(canvas.transform, "PauseResumeButton", "계속하기", new Vector2(-690f, 200f)); // 탐험 복귀 버튼
                resume.onClick.AddListener(owner.ClosePause); // 탐험 복귀 연결
                var save = UiFactory.CreateButton(canvas.transform, "PauseSaveButton", "저장", new Vector2(-690f, 90f)); // 현재 회차 저장 버튼
                save.onClick.AddListener(view.Save); // 실제 저장 연결
                view.load = UiFactory.CreateButton(canvas.transform, "PauseLoadButton", "불러오기", new Vector2(-690f, -20f)); // 저장 회차 불러오기 버튼
                view.load.onClick.AddListener(view.Load); // 실제 불러오기 연결
                var title = UiFactory.CreateButton(canvas.transform, "PauseTitleButton", "타이틀로", new Vector2(-690f, -130f)); // 타이틀 복귀 버튼
                title.onClick.AddListener(view.SaveAndReturnToTitle); // 자동 저장 후 타이틀 복귀 연결
                view.entries = UiFactory.CreateText(canvas.transform, "PauseHistoryEntries", "", 27, new Vector2(250f, -10f), new Vector2(1120f, 470f), Color.white); // 회차 기록 본문
                view.entries.alignment = TextAnchor.UpperLeft; // 기록 왼쪽 위 정렬
                view.pageLabel = UiFactory.CreateText(canvas.transform, "PauseHistoryPage", "", 24, new Vector2(250f, -285f), new Vector2(250f, 50f), Color.white); // 기록 페이지 번호
                var previous = UiFactory.CreateButton(canvas.transform, "PauseHistoryPrevious", "이전", new Vector2(0f, -345f)); // 이전 기록 버튼
                previous.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 60f); // 이전 버튼 크기
                previous.onClick.AddListener(() => view.MovePage(-1)); // 이전 페이지 연결
                var next = UiFactory.CreateButton(canvas.transform, "PauseHistoryNext", "다음", new Vector2(500f, -345f)); // 다음 기록 버튼
                next.GetComponent<RectTransform>().sizeDelta = new Vector2(190f, 60f); // 다음 버튼 크기
                next.onClick.AddListener(() => view.MovePage(1)); // 다음 페이지 연결
                foreach (var button in new[] { resume, save, view.load, title, previous, next }) // 모든 메뉴 버튼 순회
                { // 취소 연결 시작
                    button.gameObject.AddComponent<PauseCancelRelay>().Initialize(view); // 선택된 버튼의 취소 입력 연결
                } // 취소 연결 끝
                view.notice = UiFactory.CreateText(canvas.transform, "PauseNotice", "", 22, new Vector2(-690f, -310f), new Vector2(380f, 120f), Color.white); // 저장 결과 안내
                view.Refresh(); // 현재 회차 기록 표시
                EventSystem.current.SetSelectedGameObject(resume.gameObject); // 첫 메뉴 버튼 선택
                return view; // 생성된 메뉴 전달
            } // 구성 끝
            catch // 화면 생성 실패 검사
            { // 실패 정리 시작
                Destroy(screen); // 불완전한 메뉴 제거
                throw; // 원래 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        public void OnCancel(BaseEventData eventData) // Esc 또는 게임패드 취소 처리
        { // 취소 시작
            owner.ClosePause(); // 현재 메뉴 닫기
        } // 취소 끝
        private static void Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color) // 메뉴 배경 상자 생성
        { // 생성 시작
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image)); // 배경 상자 객체
            panel.transform.SetParent(parent, false); // 메뉴 화면 아래 배치
            var rect = panel.GetComponent<RectTransform>(); // 상자 위치와 크기 조회
            rect.anchoredPosition = position; // 상자 중심 위치
            rect.sizeDelta = size; // 상자 크기 지정
            var image = panel.GetComponent<Image>(); // 배경 이미지 조회
            image.color = color; // 상자 색상 적용
            image.raycastTarget = false; // 버튼 입력 가림 방지
        } // 생성 끝
        private void Save() // 현재 회차 저장
        { // 저장 시작
            var saved = owner.TrySaveRun(out var error); // 기존 안전 저장 실행
            notice.text = saved ? "회차를 저장했습니다." : error; // 실제 저장 결과 표시
            load.interactable = owner.CanContinueRun; // 저장 회차의 불러오기 상태 갱신
        } // 저장 끝
        private void Load() // 저장 회차 불러오기
        { // 불러오기 시작
            if (!owner.ReloadRunFromPause(out var error)) // 검증된 회차 불러오기 검사
            { // 오류 안내 시작
                notice.text = error; // 실제 불러오기 실패 표시
            } // 오류 안내 끝
        } // 불러오기 끝
        private void SaveAndReturnToTitle() // 자동 저장 성공 후 타이틀 복귀
        { // 복귀 시작
            if (!owner.TrySaveRun(out var error)) // 현재 회차 자동 저장 검사
            { // 저장 실패 시작
                notice.text = error; // 실제 저장 오류 표시
                return; // 회차와 메뉴 상태 보존
            } // 저장 실패 끝
            owner.ReturnToTitle(); // 저장 성공 후 타이틀 복귀
        } // 복귀 끝
        private void MovePage(int direction) // 회차 기록 페이지 이동
        { // 이동 시작
            var total = Math.Max(1, (owner.ExplorationHistory.Entries.Count + PageSize - 1) / PageSize); // 기록 페이지 수
            page = Mathf.Clamp(page + direction, 0, total - 1); // 페이지 범위 보호
            Refresh(); // 현재 기록 표시 갱신
        } // 이동 끝
        private void Refresh() // 현재 회차 기록과 저장 상태 표시
        { // 표시 시작
            var history = owner.ExplorationHistory.Entries; // 현재 회차 기록 조회
            var room = GameObject.Find("RoomTravel"); // 현재 던전 객체 조회
            var travel = room == null ? null : room.GetComponent<RoomTravelController>(); // 현재 방과 층 조회
            runStatus.text = travel == null ? "체력 " + owner.PlayerHealth + " / 100 · 마나 " + owner.PlayerMana + " / 50" : travel.Floor + "층 · 방 (" + travel.RoomX + ", " + travel.RoomY + ") · 체력 " + owner.PlayerHealth + " / 100 · 마나 " + owner.PlayerMana + " / 50 · 열쇠 " + owner.Keys.Count; // 실제 회차 상태 표시
            runStatus.text += "\n레벨 " + owner.Level + " · 경험치 " + owner.Experience + " · 골드 " + owner.Gold + " · 스탯 포인트 " + owner.StatPoints; // 실제 성장과 보유 재화 표시
            var total = Math.Max(1, (history.Count + PageSize - 1) / PageSize); // 기록 페이지 수
            page = Mathf.Clamp(page, 0, total - 1); // 페이지 범위 보호
            var lines = new List<string>(); // 현재 페이지 기록 목록
            for (var index = page * PageSize; index < Math.Min((page + 1) * PageSize, history.Count); index++) // 현재 페이지 순회
            { // 기록 추가 시작
                lines.Add((index + 1) + ". " + history[index]); // 기록 순서와 내용 추가
            } // 기록 추가 끝
            entries.text = lines.Count == 0 ? "아직 탐험 기록이 없습니다." : string.Join("\n", lines); // 현재 기록 표시
            pageLabel.text = (page + 1) + " / " + total; // 현재 페이지 표시
            load.interactable = owner.CanContinueRun; // 저장된 회차의 불러오기 허용
        } // 표시 끝
    } // 클래스 끝
    public sealed class PauseCancelRelay : MonoBehaviour, ICancelHandler // 메뉴 버튼의 취소 입력 중계
    { // 클래스 시작
        private PauseScreenView screen; // 현재 일시정지 화면
        public void Initialize(PauseScreenView view) // 메뉴 화면 연결
        { // 연결 시작
            screen = view; // 현재 메뉴 보관
        } // 연결 끝
        public void OnCancel(BaseEventData eventData) // 선택된 버튼의 취소 처리
        { // 처리 시작
            if (screen != null) // 현재 메뉴 존재 검사
            { // 취소 전달 시작
                screen.OnCancel(eventData); // 메뉴 닫기 전달
            } // 취소 전달 끝
        } // 처리 끝
    } // 클래스 끝
} // 이름 공간 끝
