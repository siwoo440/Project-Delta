using System; // 닫기 요청 연결
using UnityEngine; // 실제 도감 화면 객체
using UnityEngine.EventSystems; // 기본 선택 버튼
using UnityEngine.UI; // 화면 배경과 버튼
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public static class JournalScreenView // 타이틀 관찰 기록 화면 생성
    { // 클래스 시작
        public static GameObject Create(Transform parent, AppRoot owner, Action close) // 읽기 전용 관찰 화면 생성
        { // 생성 시작
            if (parent == null || owner == null || close == null || EventSystem.current == null) // 화면과 입력 준비 검사
            { // 준비 실패 조건 시작
                throw new InvalidOperationException("Journal screen requires a title and active input."); // 사용할 수 없는 화면 생성 차단
            } // 준비 실패 조건 끝
            var panel = new GameObject("JournalPanel", typeof(RectTransform), typeof(Image)); // 도감 화면과 배경 생성
            try // 도감 화면 구성
            { // 구성 시작
                panel.transform.SetParent(parent, false); // 타이틀 화면 위에 배치
                var rect = panel.GetComponent<RectTransform>(); // 화면 배경 영역
                rect.anchorMin = Vector2.zero; // 화면 왼쪽 아래 기준
                rect.anchorMax = Vector2.one; // 화면 오른쪽 위 기준
                rect.offsetMin = Vector2.zero; // 아래쪽 여백 제거
                rect.offsetMax = Vector2.zero; // 위쪽 여백 제거
                panel.GetComponent<Image>().color = new Color(0.035f, 0.055f, 0.075f, 1f); // 도감 화면 배경색
                var accent = new Color(0.62f, 0.87f, 0.79f); // 화면 강조 색상
                UiFactory.CreateText(panel.transform, "JournalHeader", "관찰 기록", 58, new Vector2(0f, 410f), new Vector2(1000f, 90f), Color.white); // 도감 화면 제목
                var observed = owner.Observations.Contains("trial-elite"); // 저장된 시험 대상 기록 검사
                var content = observed ? "시험 상위 개체 · 1단계\n체력 " + CombatScreenView.TrialEnemyHealth + " · 기본 공격 위주 (시험값)" : "관찰 기록이 없습니다"; // 기록에 따른 공개 정보
                UiFactory.CreateText(panel.transform, "JournalContents", content, 32, new Vector2(-160f, 45f), new Vector2(800f, 160f), Color.white); // 읽기 전용 관찰 정보
                if (observed) // 관찰한 시험 대상 검사
                { // 관찰 정보 조건 시작
                    CombatScreenView.AddPortrait(panel.transform, "JournalPortrait", "BattlePortraits/SlimeQueenPrototype", new Vector2(600f, 45f)); // 시험 대상의 개발용 그림
                } // 관찰 정보 조건 끝
                if (!string.IsNullOrEmpty(owner.ObservationSaveMessage)) // 저장 오류 또는 복구 상태 검사
                { // 저장 안내 조건 시작
                    UiFactory.CreateText(panel.transform, "JournalSaveWarning", owner.ObservationSaveMessage, 22, new Vector2(0f, -165f), new Vector2(1400f, 110f), new Color(1f, 0.72f, 0.55f)); // 원본 보존과 저장 상태 안내
                } // 저장 안내 조건 끝
                UiFactory.CreateText(panel.transform, "JournalScope", "시험 대상 1단계 · 정식 도감은 준비 중", 23, new Vector2(0f, -420f), new Vector2(1400f, 60f), accent); // 현재 정보 범위 안내
                var button = UiFactory.CreateButton(panel.transform, "CloseJournalButton", "닫기", new Vector2(0f, -300f)); // 도감 닫기 버튼
                button.onClick.AddListener(() => close()); // 타이틀 복귀 연결
                EventSystem.current.SetSelectedGameObject(button.gameObject); // 키보드와 게임패드 기본 선택
                return panel; // 완성된 읽기 전용 화면 전달
            } // 구성 끝
            catch // 화면 구성 실패 정리
            { // 실패 정리 시작
                panel.SetActive(false); // 불완전한 화면 즉시 숨김
                UnityEngine.Object.Destroy(panel); // 임시 화면 제거
                throw; // 기존 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
