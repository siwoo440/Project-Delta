using ProjectDelta.Application; // ApplicationFlow.Current 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Canvas UI 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 24일차: OnGUI 임시 화면으로 시작했다가, 139일차에 기획서 8.2절 "화면별 정식 UI"
    // 전환의 첫 대상으로 CG·도전과제 갤러리(133~134일차)와 같은 런타임 Canvas 방식으로
    // 옮겼다. 버튼 구성/이동 로직 자체는 그대로 두고 그리는 방식만 바꿨다.
    public sealed class TitleSceneController : MonoBehaviour // 타이틀 화면 버튼 제어
    {
        private const float ButtonWidth = 220f; // 버튼 가로 크기
        private const float ButtonHeight = 50f; // 버튼 세로 크기
        private const float Spacing = ButtonHeight + 16f; // 버튼 사이 간격 포함 세로 이동량

        private void Awake()
        {
            RuntimeUiFactory.EnsureEventSystem(); // UI 입력 처리에 필요한 EventSystem 준비

            Transform canvasTransform = // 배경+제목까지 포함한 Canvas 생성
                RuntimeUiFactory.BuildScreenCanvas(
                    transform,
                    "TitleCanvas",
                    "Project Delta");

            BuildButtons(
                canvasTransform);
        }

        private void BuildButtons(
            Transform parent)
        {
            bool hasSavedRun = // 26일차: 저장된 런 존재 여부 확인
                ApplicationFlow.Current != null
                && ApplicationFlow.Current.HasSavedRun();

            float y = 0f; // 첫 버튼부터의 누적 세로 오프셋(0에서 시작해 매번 내려감)

            if (hasSavedRun) // 저장된 런이 있을 때만 이어하기 버튼 표시
            {
                RuntimeUiFactory.CreateCenteredButton(
                    parent,
                    "ContinueButton",
                    new Vector2(0f, y),
                    new Vector2(ButtonWidth, ButtonHeight),
                    "이어하기",
                    20,
                    () => ApplicationFlow.Current?.ContinueGame(), // 저장된 런 복원 후 로딩 화면을 거쳐 던전으로 이동
                    out _);

                y -= Spacing; // 다음 버튼 위치로 이동
            }

            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "NewGameButton",
                new Vector2(0f, y),
                new Vector2(ButtonWidth, ButtonHeight),
                "새 게임",
                20,
                () => ApplicationFlow.Current?.EnterLobby(), // 123일차: 곧바로 던전 대신 로비로 이동
                out _);

            y -= Spacing;

            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "SettingsButton",
                new Vector2(0f, y),
                new Vector2(ButtonWidth, ButtonHeight),
                "설정",
                20,
                () => ApplicationFlow.Current?.OpenSettings(),
                out _);

            y -= Spacing;

            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "CgGalleryButton",
                new Vector2(0f, y),
                new Vector2(ButtonWidth, ButtonHeight),
                "CG 목록",
                20,
                () => ApplicationFlow.Current?.OpenCgGallery(), // 133일차: CG 갤러리
                out _);

            y -= Spacing;

            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "AchievementGalleryButton",
                new Vector2(0f, y),
                new Vector2(ButtonWidth, ButtonHeight),
                "도전과제",
                20,
                () => ApplicationFlow.Current?.OpenAchievementGallery(), // 134일차: 도전과제 갤러리
                out _);

            y -= Spacing;

            RuntimeUiFactory.CreateCenteredButton(
                parent,
                "QuitButton",
                new Vector2(0f, y),
                new Vector2(ButtonWidth, ButtonHeight),
                "종료",
                20,
                QuitGame,
                out _);
        }

        private static void QuitGame() // 에디터/빌드 환경에 맞는 종료 처리
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 에디터 플레이 모드 종료
#else
            Application.Quit(); // 빌드 실행 파일 종료
#endif
        }
    }
}
