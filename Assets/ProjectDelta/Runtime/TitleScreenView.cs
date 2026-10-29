using ProjectDelta.Core; // 진행 상태 형식
using UnityEngine; // 화면 배치 도구
using UnityEngine.EventSystems; // 기본 선택 대상
using UnityEngine.UI; // 실제 버튼 형식
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class TitleScreenView : MonoBehaviour // 타이틀 표시 담당
    { // 클래스 시작
        private Button newGameButton; // 새 게임 버튼
        private Button continueButton; // 회차 이어하기 버튼
        private Button journalButton; // 관찰 기록 버튼
        private GameObject journalPanel; // 현재 관찰 기록 화면
        private Canvas titleCanvas; // 관찰 화면이 표시될 타이틀
        private void Start() // 타이틀 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("TitleCanvas"); // 타이틀 화면 생성
            canvas.transform.SetParent(transform, false); // 씬 화면 아래 배치
            titleCanvas = canvas; // 타이틀 화면 보관
            var mint = new Color(0.62f, 0.87f, 0.79f); // 타이틀 강조 색상
            UiFactory.CreateText(canvas.transform, "Eyebrow", "PROJECT  /  DELTA", 24, new Vector2(0f, 235f), new Vector2(900f, 70f), mint); // 프로젝트 이름 표시
            UiFactory.CreateText(canvas.transform, "Title", "프로젝트 델타", 72, new Vector2(0f, 130f), new Vector2(1200f, 120f), Color.white); // 게임 제목 표시
            UiFactory.CreateText(canvas.transform, "Subtitle", "미지의 던전에서 시작되는 선택과 관계", 25, new Vector2(0f, 28f), new Vector2(1200f, 70f), new Color(0.63f, 0.69f, 0.72f)); // 게임 방향 표시
            if (AppRoot.Instance != null && !string.IsNullOrEmpty(AppRoot.Instance.ObservationSaveMessage)) // 관찰 저장 오류 또는 복구 안내 검사
            { // 저장 안내 조건 시작
                UiFactory.CreateText(canvas.transform, "ObservationSaveWarning", AppRoot.Instance.ObservationSaveMessage, 21, new Vector2(0f, -330f), new Vector2(1500f, 100f), new Color(1f, 0.72f, 0.55f)); // 기존 저장 보호 상태 안내
            } // 저장 안내 조건 끝
            if (AppRoot.Instance != null && !string.IsNullOrEmpty(AppRoot.Instance.RunSaveMessage)) // 회차 저장 안내 검사
            { // 안내 조건 시작
                UiFactory.CreateText(canvas.transform, "RunSaveWarning", AppRoot.Instance.RunSaveMessage, 20, new Vector2(0f, -340f), new Vector2(1500f, 70f), new Color(1f, 0.72f, 0.55f)); // 회차 저장 오류 또는 성공 안내
            } // 안내 조건 끝
            newGameButton = UiFactory.CreateButton(canvas.transform, "NewGameButton", "새 게임", new Vector2(0f, -95f)); // 새 게임 버튼 생성
            newGameButton.onClick.AddListener(OnNewGame); // 화면 요청 연결
            continueButton = UiFactory.CreateButton(canvas.transform, "ContinueButton", "이어하기", new Vector2(0f, -185f)); // 저장 회차 재개 버튼
            continueButton.onClick.AddListener(OnContinue); // 회차 재개 연결
            journalButton = UiFactory.CreateButton(canvas.transform, "JournalButton", "관찰 기록", new Vector2(0f, -275f)); // 관찰 기록 버튼 생성
            journalButton.onClick.AddListener(OpenJournal); // 기록 화면 열기 연결
            UiFactory.CreateText(canvas.transform, "BuildLabel", "2D 탐험  ·  조우  ·  관찰 기록", 21, new Vector2(0f, -390f), new Vector2(1000f, 70f), mint); // 개발 범위 표시
            UiFactory.CreateText(canvas.transform, "Controls", "마우스 클릭  /  Enter  /  게임패드 선택", 20, new Vector2(0f, -450f), new Vector2(1100f, 60f), new Color(0.54f, 0.61f, 0.65f)); // 조작 안내 표시
            EventSystem.current.SetSelectedGameObject(newGameButton.gameObject); // 키보드와 게임패드 기본 포커스
        } // 구성 끝
        private void OnContinue() // 저장 회차 재개 요청
        { // 요청 시작
            if (journalPanel == null && AppRoot.Instance != null) // 기록 화면과 실행 객체 검사
            { // 재개 조건 시작
                AppRoot.Instance.ContinueRun(); // 저장된 회차 재개
            } // 재개 조건 끝
        } // 요청 끝
        private void OnNewGame() // 새 게임 요청 전달
        { // 요청 시작
            if (journalPanel == null && AppRoot.Instance != null) // 기록 화면과 실행 객체 검사
            { // 새 게임 조건 시작
                AppRoot.Instance.StartNewGame(); // 실행 흐름에 새 게임 요청
            } // 새 게임 조건 끝
        } // 요청 끝
        private void OpenJournal() // 읽기 전용 관찰 기록 열기
        { // 열기 시작
            if (journalPanel != null || AppRoot.Instance == null || AppRoot.Instance.Flow.Phase != GamePhase.Title) // 현재 타이틀과 중복 열기 검사
            { // 열기 거절 조건 시작
                return; // 잘못된 화면 열기 차단
            } // 열기 거절 조건 끝
            try // 도감 화면 준비
            { // 화면 준비 시작
                journalPanel = JournalScreenView.Create(titleCanvas.transform, AppRoot.Instance, CloseJournal); // 저장 기록의 읽기 전용 화면
                newGameButton.interactable = false; // 도감 표시 중 새 게임 잠금
                continueButton.interactable = false; // 도감 표시 중 이어하기 잠금
                journalButton.interactable = false; // 중복 도감 열기 잠금
            } // 화면 준비 끝
            catch (System.Exception exception) // 화면 생성 실패 복구
            { // 실패 복구 시작
                Debug.LogError("Journal screen failed: " + exception.Message); // 실제 오류 기록
                if (EventSystem.current != null) // 입력 관리자 존재 검사
                { // 선택 복구 시작
                    EventSystem.current.SetSelectedGameObject(journalButton.gameObject); // 도감 버튼 선택 복구
                } // 선택 복구 끝
            } // 실패 복구 끝
        } // 열기 끝
        private void CloseJournal() // 관찰 기록 닫기와 타이틀 복귀
        { // 닫기 시작
            if (journalPanel == null) // 도감 화면 존재 검사
            { // 화면 없음 조건 시작
                return; // 중복 닫기 차단
            } // 화면 없음 조건 끝
            journalPanel.SetActive(false); // 기록 화면 즉시 숨김
            Destroy(journalPanel); // 기록 화면 제거
            journalPanel = null; // 화면 참조 해제
            newGameButton.interactable = true; // 새 게임 선택 복구
            journalButton.interactable = true; // 관찰 기록 선택 복구
            if (EventSystem.current != null) // 입력 관리자 존재 검사
            { // 선택 복구 시작
                EventSystem.current.SetSelectedGameObject(journalButton.gameObject); // 도감 버튼 선택 복구
            } // 선택 복구 끝
        } // 닫기 끝
        private void Update() // 전환 중 버튼 상태 동기화
        { // 동기화 시작
            if (newGameButton != null && AppRoot.Instance != null) // 화면과 실행 객체 존재 검사
            { // 동기화 조건 시작
                var canSelect = AppRoot.Instance.Flow.Phase == GamePhase.Title && journalPanel == null; // 타이틀과 도감 닫힘 상태
                newGameButton.interactable = canSelect; // 전환 또는 도감 표시 중 잠금
                journalButton.interactable = canSelect; // 전환 또는 도감 표시 중 중복 열기 차단
                continueButton.interactable = canSelect && AppRoot.Instance.CanContinueRun; // 유효한 회차만 재개 허용
            } // 동기화 조건 끝
        } // 동기화 끝
        private void OnDestroy() // 화면 입력 연결 정리
        { // 정리 시작
            if (continueButton != null) // 재개 버튼 존재 검사
            { // 연결 정리 시작
                continueButton.onClick.RemoveListener(OnContinue); // 재개 연결 해제
            } // 연결 정리 끝
            if (journalButton != null) // 관찰 기록 버튼 존재 검사
            { // 기록 연결 정리 시작
                journalButton.onClick.RemoveListener(OpenJournal); // 화면 열기 연결 해제
            } // 기록 연결 정리 끝
            if (newGameButton != null) // 버튼 존재 검사
            { // 정리 조건 시작
                newGameButton.onClick.RemoveListener(OnNewGame); // 화면 요청 연결 해제
            } // 정리 조건 끝
        } // 정리 끝
    } // 클래스 끝
} // 이름 공간 끝
