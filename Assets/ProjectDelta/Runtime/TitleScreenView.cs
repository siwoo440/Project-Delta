using ProjectDelta.Core; // 진행 상태 형식
using UnityEngine; // 화면 배치 도구
using UnityEngine.EventSystems; // 기본 선택 대상
using UnityEngine.UI; // 실제 버튼 형식
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class TitleScreenView : MonoBehaviour // 타이틀 표시 담당
    { // 클래스 시작
        private Button newGameButton; // 새 게임 버튼
        private void Start() // 타이틀 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("TitleCanvas"); // 타이틀 화면 생성
            canvas.transform.SetParent(transform, false); // 씬 화면 아래 배치
            var mint = new Color(0.62f, 0.87f, 0.79f); // 타이틀 강조 색상
            UiFactory.CreateText(canvas.transform, "Eyebrow", "PROJECT  /  DELTA", 24, new Vector2(0f, 235f), new Vector2(900f, 70f), mint); // 프로젝트 이름 표시
            UiFactory.CreateText(canvas.transform, "Title", "프로젝트 델타", 72, new Vector2(0f, 130f), new Vector2(1200f, 120f), Color.white); // 게임 제목 표시
            UiFactory.CreateText(canvas.transform, "Subtitle", "미지의 던전에서 시작되는 선택과 관계", 25, new Vector2(0f, 28f), new Vector2(1200f, 70f), new Color(0.63f, 0.69f, 0.72f)); // 게임 방향 표시
            if (AppRoot.Instance != null && !string.IsNullOrEmpty(AppRoot.Instance.ObservationSaveMessage)) // 관찰 저장 오류 또는 복구 안내 검사
            { // 저장 안내 조건 시작
                UiFactory.CreateText(canvas.transform, "ObservationSaveWarning", AppRoot.Instance.ObservationSaveMessage, 21, new Vector2(0f, -265f), new Vector2(1500f, 100f), new Color(1f, 0.72f, 0.55f)); // 기존 저장 보호 상태 안내
            } // 저장 안내 조건 끝
            newGameButton = UiFactory.CreateButton(canvas.transform, "NewGameButton", "새 게임", new Vector2(0f, -115f)); // 새 게임 버튼 생성
            newGameButton.onClick.AddListener(OnNewGame); // 화면 요청 연결
            UiFactory.CreateText(canvas.transform, "BuildLabel", "1일차  ·  2D 기본 실행 구조", 21, new Vector2(0f, -390f), new Vector2(1000f, 70f), mint); // 개발 범위 표시
            UiFactory.CreateText(canvas.transform, "Controls", "마우스 클릭  /  Enter  /  게임패드 선택", 20, new Vector2(0f, -450f), new Vector2(1100f, 60f), new Color(0.54f, 0.61f, 0.65f)); // 조작 안내 표시
            EventSystem.current.SetSelectedGameObject(newGameButton.gameObject); // 키보드와 게임패드 기본 포커스
        } // 구성 끝
        private void OnNewGame() // 새 게임 요청 전달
        { // 요청 시작
            AppRoot.Instance.StartNewGame(); // 실행 흐름에 새 게임 요청
        } // 요청 끝
        private void Update() // 전환 중 버튼 상태 동기화
        { // 동기화 시작
            if (newGameButton != null && AppRoot.Instance != null) // 화면과 실행 객체 존재 검사
            { // 동기화 조건 시작
                newGameButton.interactable = AppRoot.Instance.Flow.Phase == GamePhase.Title; // 전환 중 잠금과 실패 후 복구
            } // 동기화 조건 끝
        } // 동기화 끝
        private void OnDestroy() // 화면 입력 연결 정리
        { // 정리 시작
            if (newGameButton != null) // 버튼 존재 검사
            { // 정리 조건 시작
                newGameButton.onClick.RemoveListener(OnNewGame); // 화면 요청 연결 해제
            } // 정리 조건 끝
        } // 정리 끝
    } // 클래스 끝
} // 이름 공간 끝
