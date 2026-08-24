using ProjectDelta.Application; // Encounter 애플리케이션 로직 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // uGUI 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    public sealed class EncounterPanelController : MonoBehaviour
    {
        [Header("Encounter")]
        [SerializeField] private ExplorationMonsterEncounterController encounterController; // Encounter 컨트롤러
        [SerializeField] private GameObject panelRoot; // 인카운터 패널 루트

        [Header("Target Info")]
        [SerializeField] private Text stateText; // 상태 텍스트
        [SerializeField] private Text monsterIdText; // 몬스터 ID 텍스트
        [SerializeField] private Text roomIdText; // 방 ID 텍스트
        [SerializeField] private Text gridPositionText; // GridPosition 텍스트
        [SerializeField] private Text resultText; // 행동 결과·선택 불가 사유 텍스트

        [Header("Actions")]
        [SerializeField] private Button battleButton; // 전투 버튼
        [SerializeField] private Button escapeButton; // 회피 버튼
        [SerializeField] private Button testEndButton; // 테스트 종료 버튼

        private bool wasVisible; // 이전 프레임 패널 표시 여부

        private void Awake()
        {
            ResolveEncounterController(); // Encounter 컨트롤러 자동 연결
            BindButtons(); // 버튼 이벤트 연결
            SetPanelVisible(false); // 시작 시 패널 숨김
        }

        private void OnDestroy()
        {
            UnbindButtons(); // 버튼 이벤트 해제
        }

        private void Update()
        {
            ResolveEncounterController(); // 누락 참조 재검색

            bool shouldShow =
                encounterController != null
                && encounterController.CurrentState == EncounterState.Active; // Active 상태 표시 조건

            SetPanelVisible(
                shouldShow); // 패널 표시 상태 반영

            if (!shouldShow) // 패널 숨김 상태 확인
            {
                wasVisible =
                    false; // 다음 표시 시 초기화 준비

                return; // UI 갱신 중단
            }

            if (!wasVisible) // 새 Encounter 패널 표시 확인
            {
                wasVisible =
                    true; // 표시 상태 저장

                SetResultText(
                    string.Empty); // 이전 결과 문구 초기화
            }

            RefreshTargetInfo(); // 대상 정보 갱신
            RefreshActionState(); // 행동 버튼과 사유 갱신
        }

        private void ResolveEncounterController()
        {
            if (encounterController != null) // 기존 참조 확인
            {
                return; // 재검색 생략
            }

            encounterController =
                FindFirstObjectByType<ExplorationMonsterEncounterController>(); // 씬에서 자동 검색
        }

        private void BindButtons()
        {
            if (battleButton != null) // 전투 버튼 확인
            {
                battleButton.onClick.AddListener(
                    OnBattleClicked); // 전투 클릭 연결
            }

            if (escapeButton != null) // 회피 버튼 확인
            {
                escapeButton.onClick.AddListener(
                    OnEscapeClicked); // 회피 클릭 연결
            }

            if (testEndButton != null) // 테스트 종료 버튼 확인
            {
                testEndButton.onClick.AddListener(
                    OnTestEndClicked); // 테스트 종료 클릭 연결
            }
        }

        private void UnbindButtons()
        {
            if (battleButton != null) // 전투 버튼 확인
            {
                battleButton.onClick.RemoveListener(
                    OnBattleClicked); // 전투 클릭 해제
            }

            if (escapeButton != null) // 회피 버튼 확인
            {
                escapeButton.onClick.RemoveListener(
                    OnEscapeClicked); // 회피 클릭 해제
            }

            if (testEndButton != null) // 테스트 종료 버튼 확인
            {
                testEndButton.onClick.RemoveListener(
                    OnTestEndClicked); // 테스트 종료 클릭 해제
            }
        }

        private void OnBattleClicked()
        {
            if (encounterController == null) // Encounter 컨트롤러 확인
            {
                return; // 클릭 처리 중단
            }

            ShowCommandResult(
                encounterController.SelectBattleCommand()); // 전투 행동 실행 결과 표시
        }

        private void OnEscapeClicked()
        {
            if (encounterController == null) // Encounter 컨트롤러 확인
            {
                return; // 클릭 처리 중단
            }

            ShowCommandResult(
                encounterController.SelectEscapeCommand()); // 회피 행동 실행 결과 표시
        }

        private void OnTestEndClicked()
        {
            if (encounterController == null) // Encounter 컨트롤러 확인
            {
                return; // 클릭 처리 중단
            }

            encounterController.CompleteTestEncounter(); // 테스트 Encounter 종료
        }

        private void RefreshTargetInfo()
        {
            if (encounterController == null) // Encounter 컨트롤러 확인
            {
                return; // 정보 갱신 중단
            }

            EncounterContext context =
                encounterController.CurrentContext; // 현재 Context 읽기

            if (stateText != null) // 상태 텍스트 확인
            {
                stateText.text =
                    $"State : {encounterController.CurrentState}"; // 현재 상태 표시
            }

            if (context == null) // Context 누락 확인
            {
                if (monsterIdText != null) // 몬스터 텍스트 확인
                {
                    monsterIdText.text =
                        "Monster : -"; // 몬스터 정보 초기화
                }

                if (roomIdText != null) // 방 텍스트 확인
                {
                    roomIdText.text =
                        "Room : -"; // 방 정보 초기화
                }

                if (gridPositionText != null) // Grid 텍스트 확인
                {
                    gridPositionText.text =
                        "Grid : -"; // Grid 정보 초기화
                }

                return; // 대상 정보 갱신 종료
            }

            if (monsterIdText != null) // 몬스터 텍스트 확인
            {
                monsterIdText.text =
                    $"Monster : {context.MonsterDefinitionId}"; // 몬스터 ID 표시
            }

            if (roomIdText != null) // 방 텍스트 확인
            {
                roomIdText.text =
                    $"Room : {context.RoomId}"; // 방 ID 표시
            }

            if (gridPositionText != null) // Grid 텍스트 확인
            {
                gridPositionText.text =
                    $"Grid : {context.MonsterGridPosition}"; // 몬스터 GridPosition 표시
            }
        }

        private void RefreshActionState()
        {
            if (encounterController == null) // Encounter 컨트롤러 확인
            {
                SetActionButtonsInteractable(
                    false); // 행동 버튼 비활성

                return; // 상태 갱신 중단
            }

            EncounterActionAvailability availability =
                encounterController.GetActionAvailability(); // 행동 선택 가능 여부 계산

            SetActionButtonsInteractable(
                availability.CanSelect); // 전투·회피 버튼 활성 상태 반영

            EncounterCommandResult lastResult =
                encounterController.LastCommandResult; // 마지막 Command 결과 읽기

            if (lastResult != null) // 행동 실행 결과 확인
            {
                string prefix =
                    lastResult.Accepted
                        ? "선택"
                        : "실패"; // 결과 접두어 결정

                if (!availability.CanSelect
                    && !string.IsNullOrEmpty(availability.Reason)
                    && lastResult.Message != availability.Reason) // 행동 결과와 다른 선택 불가 사유 확인
                {
                    SetResultText(
                        $"{prefix} : {lastResult.Message}\n{availability.Reason}"); // 결과와 선택 불가 사유 함께 표시
                }
                else
                {
                    SetResultText(
                        $"{prefix} : {lastResult.Message}"); // 일반 결과 표시
                }

                return; // 결과 표시 완료
            }

            if (!availability.CanSelect) // 선택 불가 상태 확인
            {
                SetResultText(
                    availability.Reason); // 선택 불가 사유 표시
            }
        }

        private void ShowCommandResult(
            EncounterCommandResult result)
        {
            if (result == null) // 결과 누락 확인
            {
                SetResultText(
                    string.Empty); // 결과 문구 초기화

                return; // 표시 처리 종료
            }

            string prefix =
                result.Accepted
                    ? "선택"
                    : "실패"; // 결과 접두어 결정

            SetResultText(
                $"{prefix} : {result.Message}"); // 행동 결과 표시
        }

        private void SetActionButtonsInteractable(
            bool interactable)
        {
            if (battleButton != null) // 전투 버튼 확인
            {
                battleButton.interactable =
                    interactable; // 전투 버튼 활성 상태 적용
            }

            if (escapeButton != null) // 회피 버튼 확인
            {
                escapeButton.interactable =
                    interactable; // 회피 버튼 활성 상태 적용
            }
        }

        private void SetResultText(
            string message)
        {
            if (resultText != null) // 결과 텍스트 확인
            {
                resultText.text =
                    message; // 결과·사유 문구 반영
            }
        }

        private void SetPanelVisible(
            bool visible)
        {
            if (panelRoot == null) // 패널 루트 확인
            {
                return; // 표시 처리 중단
            }

            if (panelRoot.activeSelf != visible) // 표시 상태 변경 여부 확인
            {
                panelRoot.SetActive(
                    visible); // 패널 표시 상태 변경
            }
        }
    }
}
