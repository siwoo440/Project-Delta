using ProjectDelta.Application; // ApplicationFlow.Current 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 24일차: DungeonScene을 항상 직접 열어서 테스트하던 것에서 벗어나, 타이틀 → 새 게임 →
    // 던전 → (이 버튼) → 타이틀 순환 경로를 실제로 오갈 수 있게 하는 임시 디버그 버튼.
    // 27일차: 던전 안에서 바로 저장/불러오기를 확인할 수 있도록 두 버튼을 추가했다.
    // TODO: 정식 일시정지 메뉴(계속하기/설정/타이틀로)가 생기면 이 버튼들은 그 메뉴로 흡수한다.
    public sealed class DungeonDebugMenuController : MonoBehaviour // 던전 임시 저장/불러오기/나가기 버튼 제어
    {
        [SerializeField] private float feedbackDuration = 1.5f; // 저장 완료 문구가 표시되는 시간(초)

        private GUIStyle buttonStyle; // 버튼 글자 스타일
        private GUIStyle feedbackStyle; // 완료 안내 글자 스타일
        private string feedbackText; // 현재 표시 중인 완료 안내 문구
        private float feedbackTimer; // 완료 안내 남은 표시 시간

        private void Update() // 완료 안내 문구 시간 경과 처리
        {
            if (feedbackTimer <= 0f) // 표시 중인 안내가 있는지 확인
            {
                return; // 처리할 안내 없음
            }

            feedbackTimer -= Time.deltaTime; // 남은 표시 시간 감소

            if (feedbackTimer <= 0f) // 표시 시간 종료 확인
            {
                feedbackText = string.Empty; // 안내 문구 숨김
            }
        }

        private void OnGUI() // 좌측 상단 임시 저장/불러오기/나가기 버튼 표시
        {
            EnsureStyles(); // 스타일 준비

            if (GUI.Button(new Rect(16f, 16f, 140f, 32f), "저장하기 (임시)", buttonStyle)) // 저장 버튼
            {
                ApplicationFlow.Current?.SaveDungeonProgress(); // 현재 진행 상태 즉시 저장
                ShowFeedback("저장했습니다"); // 완료 안내 표시 (씬 전환이 없어 눈에 보임)
            }

            if (GUI.Button(new Rect(16f, 56f, 140f, 32f), "불러오기 (임시)", buttonStyle)) // 불러오기 버튼
            {
                ApplicationFlow.Current?.ContinueGame(); // 저장 시점으로 되돌아가기 (지금 진행 상황은 버려짐)
                // 불러오기는 곧바로 씬이 전환되어 이 오브젝트가 사라지므로 완료 안내를 띄우지 않는다.
            }

            if (GUI.Button(new Rect(16f, 96f, 140f, 32f), "타이틀로 (임시)", buttonStyle)) // 타이틀로 나가기 버튼
            {
                ApplicationFlow.Current?.ReturnToTitle(); // 현재 런 포기 후 타이틀 화면으로 이동
            }

            if (!string.IsNullOrEmpty(feedbackText)) // 표시할 완료 안내가 있는지 확인
            {
                GUI.Label(new Rect(16f, 136f, 200f, 24f), feedbackText, feedbackStyle); // 완료 안내 표시
            }
        }

        private void ShowFeedback(string text) // 완료 안내 문구 표시 시작
        {
            feedbackText = text; // 안내 문구 지정
            feedbackTimer = feedbackDuration; // 표시 시간 초기화
        }

        private void EnsureStyles() // GUI 스타일 최초 1회 생성
        {
            if (buttonStyle == null) // 버튼 스타일 존재 확인
            {
                buttonStyle = new GUIStyle(GUI.skin.button); // 기본 버튼 스타일 복제
                buttonStyle.fontSize = 14; // 버튼 글자 크기 적용
            }

            if (feedbackStyle == null) // 안내 스타일 존재 확인
            {
                feedbackStyle = new GUIStyle(GUI.skin.label); // 기본 라벨 스타일 복제
                feedbackStyle.fontSize = 13; // 안내 글자 크기 적용
                feedbackStyle.normal.textColor = new Color(0.6f, 1f, 0.6f, 1f); // 연한 녹색으로 완료 느낌 표시
            }
        }
    }
}
