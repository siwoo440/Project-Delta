using UnityEngine; // 실제 조우 화면 객체
using UnityEngine.EventSystems; // 기본 선택 버튼
using UnityEngine.UI; // 화면 배경
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class EncounterScreenView : MonoBehaviour // 시험 대상의 조우 선택 화면
    { // 클래스 시작
        private Button fight; // 전투 선택 버튼
        public static EncounterScreenView Create(AppRoot owner) // 조우 화면 생성
        { // 생성 시작
            if (EventSystem.current == null) // 화면 입력 관리자 존재 검사
            { // 입력 관리자 누락 조건 시작
                throw new System.InvalidOperationException("An active EventSystem is required."); // 조작 불가능한 조우 화면 차단
            } // 입력 관리자 누락 조건 끝
            var screen = new GameObject("EncounterScreen", typeof(EncounterScreenView)); // 조우 화면 상위 객체
            try // 조우 화면 구성
            { // 화면 구성 시작
                var view = screen.GetComponent<EncounterScreenView>(); // 실제 조우 화면 제어
                var canvas = UiFactory.CreateCanvas("EncounterCanvas"); // 조우 전용 화면 생성
                canvas.transform.SetParent(screen.transform, false); // 조우 화면 아래 배치
                canvas.sortingOrder = 100; // 탐험 화면보다 앞에 표시
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 조우 화면 배경
                background.transform.SetParent(canvas.transform, false); // 화면 전체에 배치
                var rect = background.GetComponent<RectTransform>(); // 배경 크기 조회
                rect.anchorMin = Vector2.zero; // 화면 왼쪽 아래 기준
                rect.anchorMax = Vector2.one; // 화면 오른쪽 위 기준
                rect.offsetMin = Vector2.zero; // 아래쪽 여백 제거
                rect.offsetMax = Vector2.zero; // 위쪽 여백 제거
                background.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 조우 화면 배경색
                var accent = new Color(0.62f, 0.87f, 0.79f); // 조우 화면 강조 색상
                CombatScreenView.AddPortrait(canvas.transform, "EncounterPortrait", "BattlePortraits/SlimeQueenPrototype", new Vector2(600f, 45f)); // 시험 대상의 임시 그림
                UiFactory.CreateText(canvas.transform, "Header", "조우", 58, new Vector2(0f, 410f), new Vector2(1000f, 90f), Color.white); // 조우 화면 제목
                UiFactory.CreateText(canvas.transform, "Status", "상위 개체를 발견했습니다", 30, new Vector2(0f, 295f), new Vector2(1100f, 70f), accent); // 시험 조우 상황
                UiFactory.CreateText(canvas.transform, "EncounterDescription", "행동을 선택하세요", 32, new Vector2(-160f, 45f), new Vector2(800f, 80f), Color.white); // 현재 선택 안내
                UiFactory.CreateText(canvas.transform, "EncounterScope", "도망 · 회유 · 유혹 · 관찰 · 아이템은 준비 중", 23, new Vector2(0f, -420f), new Vector2(1400f, 60f), accent); // 미구현 선택 안내
                view.fight = UiFactory.CreateButton(canvas.transform, "EncounterFightButton", "전투", new Vector2(0f, -300f)); // 실제 전투 선택
                view.fight.onClick.AddListener(() => owner.StartCombat()); // 전투 화면 진입 연결
                view.SelectDefault(); // 키보드와 게임패드 기본 선택
                return view; // 준비된 조우 화면 전달
            } // 화면 구성 끝
            catch // 화면 생성 실패 정리
            { // 실패 정리 시작
                screen.SetActive(false); // 실패한 화면 즉시 숨김
                Destroy(screen); // 불완전한 조우 화면 제거
                throw; // 기존 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        public void SelectDefault() // 전투 실패 후 조우 선택 복구
        { // 선택 시작
            if (fight != null && EventSystem.current != null) // 선택 버튼과 입력 관리자 검사
            { // 선택 가능 조건 시작
                EventSystem.current.SetSelectedGameObject(fight.gameObject); // 조우 전투 선택 복구
            } // 선택 가능 조건 끝
        } // 선택 끝
    } // 클래스 끝
} // 이름 공간 끝
