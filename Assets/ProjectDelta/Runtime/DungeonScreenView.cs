using UnityEngine; // 화면 배치 도구
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonScreenView : MonoBehaviour // 던전 안내 표시
    { // 클래스 시작
        private void Start() // 기본 안내 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("DungeonCanvas"); // 던전 안내 화면 생성
            canvas.transform.SetParent(transform, false); // 씬 화면 아래 배치
            var mint = new Color(0.62f, 0.87f, 0.79f); // 안내 강조 색상
            UiFactory.CreateText(canvas.transform, "Header", "던전 탐험", 38, new Vector2(0f, 450f), new Vector2(1000f, 90f), Color.white); // 던전 진입 결과 표시
            UiFactory.CreateText(canvas.transform, "Status", "2D 탐험 씬 진입 완료", 23, new Vector2(0f, 375f), new Vector2(1000f, 60f), mint); // 현재 실행 상태 표시
            UiFactory.CreateText(canvas.transform, "Scope", "1일차: 격자와 플레이어 표시  ·  이동은 다음 작업", 23, new Vector2(0f, -380f), new Vector2(1400f, 70f), new Color(0.66f, 0.72f, 0.75f)); // 현재 구현 범위 표시
            UiFactory.CreateText(canvas.transform, "Controls", "Esc  /  게임패드 취소: 타이틀로 복귀", 23, new Vector2(0f, -455f), new Vector2(1300f, 60f), mint); // 복귀 조작 안내 표시
        } // 구성 끝
    } // 클래스 끝
} // 이름 공간 끝
