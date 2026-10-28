using UnityEngine; // 실제 화면 객체
using UnityEngine.EventSystems; // 버튼 기본 선택
using UnityEngine.UI; // 화면 배경과 버튼
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class CombatScreenView : MonoBehaviour // 시험 전투 화면
    { // 클래스 시작
        public static CombatScreenView Create(AppRoot owner) // 실제 시험 전투 화면 생성
        { // 생성 시작
            var screen = new GameObject("BattleScreen", typeof(CombatScreenView)); // 전투 화면 상위 객체
            try // 실제 화면 구성
            { // 화면 구성 시작
                var canvas = UiFactory.CreateCanvas("BattleCanvas"); // 전투 전용 화면 생성
                canvas.transform.SetParent(screen.transform, false); // 화면 상위 객체에 연결
                canvas.sortingOrder = 100; // 탐험 화면보다 앞에 표시
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 전투 배경 객체
                background.transform.SetParent(canvas.transform, false); // 전투 화면 아래 배치
                var rect = background.GetComponent<RectTransform>(); // 배경 위치 조회
                rect.anchorMin = Vector2.zero; // 화면 왼쪽 아래 기준
                rect.anchorMax = Vector2.one; // 화면 오른쪽 위 기준
                rect.offsetMin = Vector2.zero; // 아래쪽 여백 제거
                rect.offsetMax = Vector2.zero; // 위쪽 여백 제거
                background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 전투 화면 배경색
                var accent = new Color(0.62f, 0.87f, 0.79f); // 전투 안내 강조 색상
                UiFactory.CreateText(canvas.transform, "Header", "전투", 58, new Vector2(0f, 410f), new Vector2(1000f, 90f), Color.white); // 전투 화면 제목
                UiFactory.CreateText(canvas.transform, "Status", "조우 · 전투 준비", 26, new Vector2(0f, 330f), new Vector2(1100f, 60f), accent); // 현재 조우 상태
                UiFactory.CreateText(canvas.transform, "Player", "플레이어", 38, new Vector2(-390f, 70f), new Vector2(560f, 100f), accent); // 플레이어 영역
                UiFactory.CreateText(canvas.transform, "Opponent", "상위 개체", 38, new Vector2(390f, 70f), new Vector2(560f, 100f), new Color(1f, 0.62f, 0.58f)); // 상대 영역
                UiFactory.CreateText(canvas.transform, "Center", "VS", 44, Vector2.zero, new Vector2(200f, 100f), Color.white); // 대결 구분 표시
                UiFactory.CreateText(canvas.transform, "Notice", "전투 행동은 준비 중입니다", 25, new Vector2(0f, -200f), new Vector2(1200f, 70f), Color.white); // 현재 플레이 범위 안내
                var button = UiFactory.CreateButton(canvas.transform, "ExitBattleButton", "시험 종료", new Vector2(0f, -350f)); // 탐험 복귀 버튼
                button.onClick.AddListener(owner.EndCombat); // 실제 전투 종료 연결
                EventSystem.current.SetSelectedGameObject(button.gameObject); // 키보드와 게임패드 선택 지정
                return screen.GetComponent<CombatScreenView>(); // 준비된 화면 전달
            } // 화면 구성 끝
            catch // 화면 준비 실패 정리
            { // 실패 정리 시작
                Destroy(screen); // 불완전한 화면 제거
                throw; // 원래 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
