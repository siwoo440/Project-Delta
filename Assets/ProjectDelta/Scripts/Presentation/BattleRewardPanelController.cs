using ProjectDelta.Application; // 보상 상태 및 포맷터 참조
using UnityEngine; // Unity 기본 기능 참조
using UnityEngine.UI; // uGUI 참조

namespace ProjectDelta.Presentation // 프레젠테이션 계층
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    public sealed class BattleRewardPanelController : MonoBehaviour // 전투 보상 패널 제어
    {
        [Header("Battle")] // 전투 연결 구역
        [SerializeField] private ExplorationMonsterEncounterController encounterController; // 전투 인카운터 연결

        [Header("Reward UI")] // 보상 UI 연결 구역
        [SerializeField] private GameObject panelRoot; // 전체 보상 패널 루트

        [SerializeField] private Button[] rewardButtons = // 추가 보상 버튼 배열
            new Button[0];

        [SerializeField] private Text[] rewardTexts = // 추가 보상 버튼 글자 배열
            new Text[0];

        [Header("81일차 정식 보상 요약")] // 전투 결과 구역
        [SerializeField] private Text summaryText; // 성장 및 드롭 결과 글자

        private bool wasVisible; // 이전 프레임 표시 상태

        private void Awake() // 초기 UI 준비
        {
            ResolveEncounterController(); // 인카운터 자동 연결
            EnsureSummaryText(); // 결과 글자 준비
            ArrangeRewardButtons(); // 추가 보상 버튼 배치
            BindButtons(); // 버튼 클릭 연결

            SetPanelVisible( // 시작 시 패널 숨김
                false);
        }

        private void OnDestroy() // 오브젝트 제거 처리
        {
            UnbindButtons(); // 버튼 이벤트 해제
        }

        private void Update() // 보상 대기 상태 감시
        {
            ResolveEncounterController(); // 누락된 인카운터 재탐색

            bool shouldShow = // 보상 패널 표시 여부 계산
                encounterController != null
                && encounterController.IsBattleRewardPending
                && BattleRewardState.IsPending;

            SetPanelVisible( // 계산된 표시 상태 반영
                shouldShow);

            if (shouldShow // 패널 첫 표시 확인
                && !wasVisible)
            {
                RefreshSummary(); // 전투 결과 갱신
                RefreshOptions(); // 추가 보상 갱신
            }

            wasVisible = // 현재 표시 상태 저장
                shouldShow;
        }

        private void ResolveEncounterController() // 인카운터 연결 보정
        {
            if (encounterController != null) // 기존 연결 확인
            {
                return; // 재탐색 생략
            }

            encounterController = // 씬의 인카운터 탐색
                FindFirstObjectByType<ExplorationMonsterEncounterController>();
        }

        private void EnsureSummaryText() // 결과 글자 확보
        {
            if (summaryText != null) // 기존 연결 확인
            {
                ConfigureSummaryText( // 기존 결과 글자 재정렬
                    summaryText);

                return; // 신규 탐색 생략
            }

            if (panelRoot == null) // 패널 루트 확인
            {
                return; // 패널 없으면 중단
            }

            Text[] texts = // 비활성 자식까지 Text 탐색
                panelRoot.GetComponentsInChildren<Text>(
                    true);

            for (int index = 0; // 첫 Text부터 순회
                 index < texts.Length; // 배열 범위 확인
                 index++) // 다음 Text 이동
            {
                Text candidate = // 현재 Text 참조
                    texts[index];

                if (candidate == null // 유효 Text 확인
                    || candidate.gameObject.name != "Day81RewardSummary")
                {
                    continue; // 다른 Text 건너뜀
                }

                summaryText = // 결과 Text 연결
                    candidate;

                ConfigureSummaryText( // 결과 영역 정렬
                    summaryText);

                return; // 탐색 완료
            }

            Transform summaryParent = // 상단 결과 패널 우선 선택
                ResolveSummaryParent();

            GameObject summaryObject = // 런타임 보정용 결과 오브젝트 생성
                new GameObject(
                    "Day81RewardSummary",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Text));

            summaryObject.transform.SetParent( // 결과 패널 내부 연결
                summaryParent,
                false);

            summaryText = // 생성된 Text 연결
                summaryObject.GetComponent<Text>();

            summaryText.font = // 기존 버튼 폰트 우선 사용
                ResolveFont();

            summaryText.color = // 결과 글자 색상
                Color.white;

            summaryText.supportRichText = // 단순 텍스트 사용
                false;

            ConfigureSummaryText( // 결과 영역 배치
                summaryText);
        }

        private Transform ResolveSummaryParent() // 결과 글자 부모 선택
        {
            Transform resultPanel = // 상단 결과 패널 탐색
                panelRoot.transform.Find(
                    "BattleResultPanel");

            return resultPanel != null // 상단 패널 존재 여부
                ? resultPanel
                : panelRoot.transform;
        }

        private Font ResolveFont() // 사용 가능한 UI 폰트 선택
        {
            if (rewardTexts != null) // 기존 버튼 글자 배열 확인
            {
                for (int index = 0; // 배열 시작 인덱스
                     index < rewardTexts.Length; // 배열 범위 확인
                     index++) // 다음 글자 이동
                {
                    Text rewardText = // 현재 버튼 글자
                        rewardTexts[index];

                    if (rewardText != null // 글자 존재 확인
                        && rewardText.font != null)
                    {
                        return rewardText.font; // 기존 폰트 재사용
                    }
                }
            }

            return Resources.GetBuiltinResource<Font>( // Unity 기본 폰트 사용
                "LegacyRuntime.ttf");
        }

        private static void ConfigureSummaryText( // 결과 글자 공통 설정
            Text targetText) // 설정 대상 글자
        {
            if (targetText == null) // 대상 확인
            {
                return; // 잘못된 대상 중단
            }

            targetText.fontSize = // 결과 글자 크기
                24;

            targetText.fontStyle = // 결과 기본 글자 스타일
                FontStyle.Normal;

            targetText.alignment = // 결과 중앙 정렬
                TextAnchor.MiddleCenter;

            targetText.horizontalOverflow = // 가로 줄바꿈 사용
                HorizontalWrapMode.Wrap;

            targetText.verticalOverflow = // 세로 내용 유지
                VerticalWrapMode.Overflow;

            targetText.lineSpacing = // 읽기 쉬운 줄 간격
                1.05f;

            targetText.raycastTarget = // 버튼 클릭 방해 방지
                false;

            ConfigureSummaryRect( // 결과 영역 위치 적용
                targetText.rectTransform);
        }

        private static void ConfigureSummaryRect( // 결과 글자 영역 배치
            RectTransform rectTransform) // 결과 RectTransform
        {
            if (rectTransform == null) // RectTransform 확인
            {
                return; // 잘못된 대상 중단
            }

            rectTransform.anchorMin = // 상단 패널 내부 왼쪽 아래
                new Vector2(
                    0.05f,
                    0.05f);

            rectTransform.anchorMax = // 상단 패널 내부 오른쪽 위
                new Vector2(
                    0.95f,
                    0.95f);

            rectTransform.offsetMin = // 추가 여백 제거
                Vector2.zero;

            rectTransform.offsetMax = // 추가 여백 제거
                Vector2.zero;
        }

        private void ArrangeRewardButtons() // 추가 보상 버튼 가로 배치
        {
            if (rewardButtons == null // 버튼 배열 확인
                || rewardButtons.Length == 0)
            {
                return; // 버튼 없으면 중단
            }

            const float startX = // 버튼 묶음 시작 위치
                0.04f;

            const float endX = // 버튼 묶음 끝 위치
                0.96f;

            const float gap = // 버튼 사이 간격
                0.025f;

            const float bottom = // 버튼 하단 위치
                0.08f;

            const float top = // 버튼 상단 위치
                0.50f;

            int activeSlotCount = // 현재 버튼 개수
                rewardButtons.Length;

            float availableWidth = // 전체 버튼 사용 가능 폭
                endX
                - startX
                - gap
                * Mathf.Max(
                    0,
                    activeSlotCount - 1);

            float buttonWidth = // 버튼 한 칸 폭
                availableWidth
                / activeSlotCount;

            for (int index = 0; // 첫 버튼부터 순회
                 index < rewardButtons.Length; // 버튼 배열 범위 확인
                 index++) // 다음 버튼 이동
            {
                Button button = // 현재 버튼 참조
                    rewardButtons[index];

                if (button == null) // 누락 버튼 확인
                {
                    continue; // 누락 항목 건너뜀
                }

                RectTransform rect = // 버튼 RectTransform 변환
                    button.transform as RectTransform;

                if (rect == null) // RectTransform 확인
                {
                    continue; // 잘못된 버튼 건너뜀
                }

                float left = // 현재 버튼 왼쪽 위치
                    startX
                    + index
                    * (buttonWidth + gap);

                float right = // 현재 버튼 오른쪽 위치
                    left
                    + buttonWidth;

                rect.anchorMin = // 버튼 왼쪽 아래 앵커
                    new Vector2(
                        left,
                        bottom);

                rect.anchorMax = // 버튼 오른쪽 위 앵커
                    new Vector2(
                        right,
                        top);

                rect.offsetMin = // 추가 위치 보정 제거
                    Vector2.zero;

                rect.offsetMax = // 추가 위치 보정 제거
                    Vector2.zero;
            }
        }

        private void BindButtons() // 버튼 클릭 이벤트 연결
        {
            if (rewardButtons == null) // 버튼 배열 확인
            {
                return; // 버튼 없으면 중단
            }

            for (int index = 0; // 첫 버튼부터 순회
                 index < rewardButtons.Length; // 버튼 배열 범위 확인
                 index++) // 다음 버튼 이동
            {
                Button button = // 현재 버튼 참조
                    rewardButtons[index];

                if (button == null) // 누락 버튼 확인
                {
                    continue; // 누락 항목 건너뜀
                }

                int capturedIndex = // 람다용 버튼 인덱스 복사
                    index;

                button.onClick.AddListener( // 클릭 이벤트 등록
                    () => OnRewardClicked(
                        capturedIndex));
            }
        }

        private void UnbindButtons() // 버튼 클릭 이벤트 해제
        {
            if (rewardButtons == null) // 버튼 배열 확인
            {
                return; // 버튼 없으면 중단
            }

            for (int index = 0; // 첫 버튼부터 순회
                 index < rewardButtons.Length; // 버튼 배열 범위 확인
                 index++) // 다음 버튼 이동
            {
                Button button = // 현재 버튼 참조
                    rewardButtons[index];

                if (button == null) // 누락 버튼 확인
                {
                    continue; // 누락 항목 건너뜀
                }

                button.onClick.RemoveAllListeners(); // 기존 클릭 이벤트 제거
            }
        }

        private void RefreshSummary() // 전투 결과 텍스트 갱신
        {
            if (summaryText == null // 결과 글자 연결 확인
                || encounterController == null)
            {
                return; // 필요한 연결 없으면 중단
            }

            string summary = // 79·80일차 결과 문자열 생성
                BattleRewardSummaryFormatter.Build(
                    encounterController.LastBattleGrowthResult,
                    encounterController.LastBattleDropResult);

            summaryText.text = // 상단 패널에는 결과만 출력
                RemoveBonusPrompt(
                    summary);
        }

        private static string RemoveBonusPrompt( // 결과 문자열에서 선택 안내 제거
            string summary) // 원본 결과 문자열
        {
            if (string.IsNullOrEmpty( // 빈 문자열 확인
                    summary))
            {
                return string.Empty; // 빈 결과 반환
            }

            const string prompt = // 하단 패널 전용 안내 문구
                "추가 보상 하나를 선택하세요.";

            string cleaned = // 앞선 빈 줄과 안내 문구 제거
                summary.Replace(
                    "\n\n" + prompt,
                    string.Empty);

            cleaned = // 예외적인 단독 안내 문구도 제거
                cleaned.Replace(
                    prompt,
                    string.Empty);

            return cleaned.TrimEnd(); // 끝 공백 제거 후 반환
        }

        private void RefreshOptions() // 선택 보상 버튼 갱신
        {
            int buttonCount = // 연결된 버튼 수 계산
                rewardButtons != null
                    ? rewardButtons.Length
                    : 0;

            for (int index = 0; // 첫 버튼부터 순회
                 index < buttonCount; // 버튼 개수 범위 확인
                 index++) // 다음 버튼 이동
            {
                bool hasOption = // 현재 인덱스 보상 존재 여부
                    index
                    < BattleRewardState.CurrentOptions.Count;

                Button button = // 현재 버튼 참조
                    rewardButtons[index];

                Text rewardText = // 현재 버튼 글자 참조
                    rewardTexts != null
                    && index < rewardTexts.Length
                        ? rewardTexts[index]
                        : null;

                if (button != null) // 버튼 존재 확인
                {
                    button.gameObject.SetActive( // 보상 존재 시 버튼 표시
                        hasOption);

                    button.interactable = // 보상 존재 시 클릭 허용
                        hasOption;
                }

                if (rewardText != null) // 버튼 글자 존재 확인
                {
                    rewardText.text = // 보상 이름 출력
                        hasOption
                            ? BattleRewardState.CurrentOptions[index].DisplayName
                            : string.Empty;
                }
            }
        }

        private void OnRewardClicked( // 보상 버튼 클릭 처리
            int optionIndex) // 클릭한 보상 인덱스
        {
            if (encounterController == null // 인카운터 연결 확인
                || !BattleRewardState.IsPending
                || optionIndex < 0
                || optionIndex >= BattleRewardState.CurrentOptions.Count)
            {
                return; // 잘못된 클릭 중단
            }

            BattleRewardOption option = // 선택한 보상 데이터
                BattleRewardState.CurrentOptions[optionIndex];

            encounterController.ConfirmBattleReward( // 기존 보상 확정 흐름 호출
                option.Id);
        }

        private void SetPanelVisible( // 전체 보상 패널 표시 상태 변경
            bool visible) // 목표 표시 상태
        {
            if (panelRoot == null) // 패널 연결 확인
            {
                return; // 패널 없으면 중단
            }

            if (panelRoot.activeSelf != visible) // 상태 변경 필요 여부
            {
                panelRoot.SetActive( // 패널 활성 상태 반영
                    visible);
            }
        }
    }
}
