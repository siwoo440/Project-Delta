using ProjectDelta.Application; // 전투 응용 기능 사용
using UnityEngine; // Unity 기능 사용
using UnityEngine.UI; // Unity UI 기능 사용

namespace ProjectDelta.Presentation // 화면 표시 네임스페이스
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    public sealed class BattleSurrenderController : MonoBehaviour // 항복 UI 제어
    {
        [Header("Battle")] // 전투 참조 구분
        [SerializeField] private ExplorationMonsterEncounterController encounterController; // 조우 컨트롤러 참조

        [Header("Surrender UI")] // 항복 UI 구분
        [SerializeField] private Button surrenderButton; // 항복 버튼
        [SerializeField] private GameObject confirmationRoot; // 확인창 루트
        [SerializeField] private Button confirmButton; // 확인 버튼
        [SerializeField] private Button cancelButton; // 취소 버튼

        private readonly SurrenderBattleCommand surrenderCommand =
            new SurrenderBattleCommand(); // 항복 명령 객체

        private void Awake() // 초기 연결 처리
        {
            ResolveEncounterController(); // 조우 컨트롤러 자동 탐색
            BindButtons(); // 버튼 이벤트 연결
            SetConfirmationVisible(
                false); // 확인창 초기 숨김
        }

        private void OnDestroy() // 제거 전 정리
        {
            UnbindButtons(); // 버튼 이벤트 해제
        }

        private void Update() // 항복 가능 상태 갱신
        {
            ResolveEncounterController(); // 누락된 컨트롤러 재탐색

            if (IsConfirmationVisible()
                && !CanSurrender()) // 확인 중 전투 상태 변경 확인
            {
                SetConfirmationVisible(
                    false); // 잘못된 확인창 자동 닫기
            }

            if (surrenderButton != null) // 항복 버튼 존재 확인
            {
                surrenderButton.interactable =
                    CanSurrender()
                    && !IsConfirmationVisible(); // 항복 버튼 활성 상태 적용
            }
        }

        private void ResolveEncounterController() // 조우 컨트롤러 탐색
        {
            if (encounterController != null) // 기존 참조 확인
            {
                return;
            }

            encounterController =
                FindFirstObjectByType<ExplorationMonsterEncounterController>(); // 씬 컨트롤러 탐색
        }

        private void BindButtons() // 버튼 이벤트 연결
        {
            if (surrenderButton != null) // 항복 버튼 확인
            {
                surrenderButton.onClick.AddListener(
                    RequestSurrender); // 항복 요청 연결
            }

            if (confirmButton != null) // 확인 버튼 확인
            {
                confirmButton.onClick.AddListener(
                    ConfirmSurrender); // 항복 확정 연결
            }

            if (cancelButton != null) // 취소 버튼 확인
            {
                cancelButton.onClick.AddListener(
                    CancelSurrender); // 항복 취소 연결
            }
        }

        private void UnbindButtons() // 버튼 이벤트 해제
        {
            if (surrenderButton != null) // 항복 버튼 확인
            {
                surrenderButton.onClick.RemoveListener(
                    RequestSurrender); // 항복 요청 해제
            }

            if (confirmButton != null) // 확인 버튼 확인
            {
                confirmButton.onClick.RemoveListener(
                    ConfirmSurrender); // 항복 확정 해제
            }

            if (cancelButton != null) // 취소 버튼 확인
            {
                cancelButton.onClick.RemoveListener(
                    CancelSurrender); // 항복 취소 해제
            }
        }

        public void RequestSurrender() // 항복 확인창 요청
        {
            if (!CanSurrender()) // 항복 가능 여부 확인
            {
                return;
            }

            SetConfirmationVisible(
                true); // 확인창 표시
        }

        public void ConfirmSurrender() // 항복 확정 처리
        {
            if (!CanSurrender()) // 항복 가능 상태 재확인
            {
                SetConfirmationVisible(
                    false); // 잘못 열린 확인창 닫기

                return;
            }

            BattleCommandResult declaration =
                surrenderCommand.Execute(
                    encounterController.CurrentBattleContext,
                    encounterController.CurrentBattleActor,
                    null); // 항복 선언 검증

            if (!declaration.Accepted) // 선언 승인 여부 확인
            {
                SetConfirmationVisible(
                    false); // 확인창 닫기

                return;
            }

            BattleDefeatService.RecordSurrender(
                encounterController.BattleRoundNumber); // 항복 패배 기록

            SetConfirmationVisible(
                false); // 확인창 닫기

            encounterController.TestLoseBattle(); // 기존 패배 종료 흐름 진입
        }

        public void CancelSurrender() // 항복 취소 처리
        {
            SetConfirmationVisible(
                false); // 확인창 닫기
        }

        private bool CanSurrender() // 항복 가능 여부 계산
        {
            return encounterController != null
                && encounterController.IsBattleActive
                && encounterController.CurrentBattleState == BattleState.AwaitingAction
                && encounterController.CurrentBattleActor != null
                && encounterController.CurrentBattleActor.Team == BattleTeam.Player;
        }

        private bool IsConfirmationVisible() // 확인창 표시 여부 조회
        {
            return confirmationRoot != null
                && confirmationRoot.activeSelf;
        }

        private void SetConfirmationVisible( // 확인창 표시 상태 변경
            bool visible) // 목표 표시 상태 입력
        {
            if (confirmationRoot == null) // 확인창 루트 존재 확인
            {
                return;
            }

            if (confirmationRoot.activeSelf != visible) // 현재 상태 비교
            {
                confirmationRoot.SetActive(
                    visible); // 확인창 활성 상태 적용
            }
        }
    }
}
