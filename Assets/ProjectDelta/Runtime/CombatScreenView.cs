using ProjectDelta.Core; // 전투 라운드와 참가자
using UnityEngine; // 실제 화면 객체
using UnityEngine.EventSystems; // 버튼 기본 선택
using UnityEngine.UI; // 화면 배경과 버튼
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class CombatScreenView : MonoBehaviour // 시험 전투 화면
    { // 클래스 시작
        private BattleRound round; // 시험 전투 라운드 상태
        private BattleActor current; // 현재 시험 행동자
        private Text roundText; // 라운드와 행동자 표시
        public static CombatScreenView Create(AppRoot owner) // 실제 시험 전투 화면 생성
        { // 생성 시작
            var screen = new GameObject("BattleScreen", typeof(CombatScreenView)); // 전투 화면 상위 객체
            try // 실제 화면 구성
            { // 화면 구성 시작
                var view = screen.GetComponent<CombatScreenView>(); // 새 전투 화면 제어
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
                view.roundText = UiFactory.CreateText(canvas.transform, "RoundStatus", "", 27, new Vector2(0f, 245f), new Vector2(1100f, 60f), Color.white); // 라운드와 행동자 표시
                view.StartTrialRound(); // 시험 참가자의 첫 라운드 시작
                UiFactory.CreateText(canvas.transform, "Player", "플레이어", 38, new Vector2(-390f, 70f), new Vector2(560f, 100f), accent); // 플레이어 영역
                UiFactory.CreateText(canvas.transform, "Opponent", "상위 개체", 38, new Vector2(390f, 70f), new Vector2(560f, 100f), new Color(1f, 0.62f, 0.58f)); // 상대 영역
                UiFactory.CreateText(canvas.transform, "Center", "VS", 44, Vector2.zero, new Vector2(200f, 100f), Color.white); // 대결 구분 표시
                UiFactory.CreateText(canvas.transform, "Notice", "시험 참가자의 행동 순서", 25, new Vector2(0f, -175f), new Vector2(1200f, 70f), Color.white); // 현재 플레이 범위 안내
                var advance = UiFactory.CreateButton(canvas.transform, "AdvanceBattleButton", "시험 행동 진행", new Vector2(0f, -275f)); // 다음 행동 진행 버튼
                advance.onClick.AddListener(view.Advance); // 실제 라운드 진행 연결
                var button = UiFactory.CreateButton(canvas.transform, "ExitBattleButton", "시험 종료", new Vector2(0f, -390f)); // 탐험 복귀 버튼
                button.onClick.AddListener(owner.EndCombat); // 실제 전투 종료 연결
                EventSystem.current.SetSelectedGameObject(button.gameObject); // 키보드와 게임패드 선택 지정
                return view; // 준비된 화면 전달
            } // 화면 구성 끝
            catch // 화면 준비 실패 정리
            { // 실패 정리 시작
                Destroy(screen); // 불완전한 화면 제거
                throw; // 원래 오류 전달
            } // 실패 정리 끝
        } // 생성 끝
        private void StartTrialRound() // 시험 참가자의 첫 행동 준비
        { // 준비 시작
            var participants = new[] // 시험 참가자 목록
            { // 목록 시작
                new BattleActor("플레이어", 10, true, 0), // 시험 플레이어 속도
                new BattleActor("상위 개체", 5, false, 1) // 시험 상대 속도
            }; // 목록 끝
            round = new BattleRound(participants); // 참가자별 라운드 생성
            if (!round.TryStartRound() || !round.TryNext(out current)) // 첫 라운드와 행동자 검사
            { // 준비 실패 조건 시작
                throw new System.InvalidOperationException("Trial round could not start."); // 잘못된 전투 화면 차단
            } // 준비 실패 조건 끝
            UpdateRoundText(); // 첫 행동자 표시
        } // 준비 끝
        private void Advance() // 시험 행동 완료와 다음 행동 선택
        { // 진행 시작
            if (!round.TryCompleteAction()) // 현재 행동 완료 검사
            { // 진행 거절 조건 시작
                return; // 중복 행동 요청 무시
            } // 진행 거절 조건 끝
            if (!round.TryNext(out current)) // 남은 행동자 검사
            { // 라운드 종료 조건 시작
                if (!round.TryEndRound() || !round.TryStartRound() || !round.TryNext(out current)) // 다음 라운드 준비 검사
                { // 다음 라운드 실패 조건 시작
                    roundText.text = "행동 가능한 참가자가 없습니다"; // 더 진행할 참가자 없음
                    return; // 잘못된 행동자 표시 방지
                } // 다음 라운드 실패 조건 끝
            } // 라운드 종료 조건 끝
            UpdateRoundText(); // 현재 라운드와 행동자 갱신
        } // 진행 끝
        private void UpdateRoundText() // 현재 라운드와 행동자 표시
        { // 표시 시작
            roundText.text = round.Number + "라운드 · " + current.Name + " 행동"; // 현재 행동 순서 표시
        } // 표시 끝
    } // 클래스 끝
} // 이름 공간 끝
