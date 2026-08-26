using ProjectDelta.Application; // 전투 결과 자료형 접근
using UnityEngine; // Unity 기본 기능
using UnityEngine.InputSystem; // 새 입력 시스템 접근
using UnityEngine.UI; // Canvas UI 기능

namespace ProjectDelta.Presentation // 프레젠테이션 영역
{
    [DisallowMultipleComponent] // 중복 컴포넌트 방지
    public sealed class BattleDebugLogOverlay : MonoBehaviour // F1 전투 로그 오버레이
    {
        private const int MaxLogLines = 200; // 최대 보관 로그 수
        private const int VisibleLogLines = 18; // 화면 표시 로그 수
        private const float PanelWidth = 620f; // 패널 너비
        private const float PanelHeight = 420f; // 패널 높이
        private const float PanelMargin = 20f; // 화면 가장자리 여백

        private readonly BattleDebugLogBuffer logBuffer = new BattleDebugLogBuffer(MaxLogLines); // 전투 로그 버퍼
        private ExplorationMonsterEncounterController encounterController; // 현재 전투 컨트롤러
        private BattleContext trackedBattleContext; // 현재 추적 전투 컨텍스트
        private GameObject panelObject; // 표시 패널 오브젝트
        private Text logText; // 로그 출력 텍스트
        private bool isVisible; // 현재 표시 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 첫 씬 로드 후 자동 생성
        private static void CreateOverlay() // 전역 디버그 로그 생성
        {
            BattleDebugLogOverlay existingOverlay = FindFirstObjectByType<BattleDebugLogOverlay>(); // 기존 오버레이 검색

            if (existingOverlay != null) // 기존 오버레이 존재 확인
            {
                return; // 중복 생성 방지
            }

            GameObject hostObject = new GameObject(nameof(BattleDebugLogOverlay)); // 오버레이 호스트 생성
            DontDestroyOnLoad(hostObject); // 씬 전환 후 유지
            hostObject.AddComponent<BattleDebugLogOverlay>(); // 오버레이 컴포넌트 추가
        }

        private void Awake() // 오버레이 초기화
        {
            isVisible = false; // 시작 상태 숨김 지정
            CreateCanvasUi(); // Canvas UI 자동 생성
            SetVisible(false); // 초기 패널 숨김 적용
        }

        private void Update() // 매 프레임 디버그 상태 갱신
        {
            HandleToggleInput(); // F1 토글 입력 처리
            ResolveEncounterController(); // 전투 컨트롤러 탐색
            TrackBattleContext(); // 새 전투 시작 감지
            CaptureLatestAction(); // 최신 행동 로그 기록
        }

        private void HandleToggleInput() // F1 표시 토글 처리
        {
            if (Keyboard.current == null || !Keyboard.current.f1Key.wasPressedThisFrame) // F1 입력 여부 확인
            {
                return; // 입력 없음 종료
            }

            SetVisible(!isVisible); // 표시 상태 반전
        }

        private void ResolveEncounterController() // 현재 씬 전투 컨트롤러 탐색
        {
            if (encounterController != null) // 기존 참조 유효 확인
            {
                return; // 재검색 생략
            }

            encounterController = FindFirstObjectByType<ExplorationMonsterEncounterController>(); // 씬의 전투 컨트롤러 검색
        }

        private void TrackBattleContext() // 전투 시작과 종료 추적
        {
            BattleContext currentContext = encounterController != null ? encounterController.CurrentBattleContext : null; // 현재 전투 컨텍스트 조회

            if (currentContext != null && currentContext != trackedBattleContext) // 새 전투 컨텍스트 확인
            {
                trackedBattleContext = currentContext; // 새 전투 컨텍스트 저장
                bool captureCurrentSequence = DoesLastActionBelongToContext(currentContext); // 시작 프레임 행동 소속 확인
                logBuffer.BeginBattle(encounterController.LastActionSequence, captureCurrentSequence); // 이전 로그 제거 후 새 전투 시작
                RefreshText(); // 새 전투 표시 갱신
                return; // 시작 처리 후 종료
            }

            if (currentContext == null && trackedBattleContext != null) // 전투 컨텍스트 종료 확인
            {
                trackedBattleContext = null; // 종료된 전투 참조 해제
            }
        }

        private void CaptureLatestAction() // 최신 전투 행동 수집
        {
            if (encounterController == null) // 전투 컨트롤러 없음 확인
            {
                return; // 수집 불가 종료
            }

            int currentSequence = encounterController.LastActionSequence; // 현재 행동 시퀀스 조회
            BattleActionResult actionResult = encounterController.LastBattleActionResult; // 최신 행동 결과 조회

            if (actionResult == null) // 행동 결과 없음 확인
            {
                return; // 기록 불가 종료
            }

            int round = actionResult.BattleEndResult != null ? actionResult.BattleEndResult.RoundCount : encounterController.BattleRoundNumber; // 행동 라운드 결정
            BattleParticipant actor = encounterController.LastActingParticipant; // 마지막 행동자 조회
            string actorId = actor != null ? actor.InstanceId : "UNKNOWN"; // 행동자 이름 결정
            bool appended = logBuffer.TryAppendAction(currentSequence, round, actorId, actionResult.CommandId, actionResult.Logs); // 실제 행동 로그 추가

            if (appended) // 새 로그 추가 여부 확인
            {
                RefreshText(); // 화면 텍스트 갱신
            }
        }

        private bool DoesLastActionBelongToContext(BattleContext context) // 마지막 행동의 현재 전투 소속 확인
        {
            if (context == null || encounterController == null) // 필수 참조 확인
            {
                return false; // 소속 확인 불가 반환
            }

            BattleParticipant actor = encounterController.LastActingParticipant; // 마지막 행동자 조회

            if (actor == null) // 마지막 행동자 없음 확인
            {
                return false; // 시작 행동 없음 반환
            }

            bool found = context.TryGetParticipant(actor.InstanceId, out BattleParticipant currentParticipant); // 현재 전투 참가자 조회
            return found && currentParticipant == actor; // 동일 인스턴스 여부 반환
        }

        private void CreateCanvasUi() // 런타임 Canvas UI 구성
        {
            GameObject canvasObject = new GameObject("BattleDebugLogCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)); // Canvas 오브젝트 생성
            canvasObject.transform.SetParent(transform, false); // 호스트 아래 배치
            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 컴포넌트 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 모드 지정
            canvas.sortingOrder = 10000; // 다른 UI보다 위에 표시
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); // Canvas 스케일러 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도 대응 스케일 지정
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 지정
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; // 가로세로 혼합 스케일 지정
            scaler.matchWidthOrHeight = 0.5f; // 가로세로 균형 지정

            panelObject = new GameObject("BattleDebugLogPanel", typeof(RectTransform), typeof(Image)); // 로그 패널 생성
            panelObject.transform.SetParent(canvasObject.transform, false); // Canvas 아래 배치
            RectTransform panelRect = panelObject.GetComponent<RectTransform>(); // 패널 RectTransform 조회
            panelRect.anchorMin = new Vector2(1f, 1f); // 오른쪽 위 최소 앵커 지정
            panelRect.anchorMax = new Vector2(1f, 1f); // 오른쪽 위 최대 앵커 지정
            panelRect.pivot = new Vector2(1f, 1f); // 오른쪽 위 피벗 지정
            panelRect.anchoredPosition = new Vector2(-PanelMargin, -PanelMargin); // 우측 상단 여백 적용
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight); // 패널 크기 지정
            Image panelImage = panelObject.GetComponent<Image>(); // 패널 이미지 조회
            panelImage.color = new Color(0f, 0f, 0f, 0.82f); // 반투명 검정 배경 지정
            panelImage.raycastTarget = false; // 마우스 입력 차단 방지

            GameObject textObject = new GameObject("BattleDebugLogText", typeof(RectTransform), typeof(Text)); // 로그 텍스트 생성
            textObject.transform.SetParent(panelObject.transform, false); // 패널 아래 배치
            RectTransform textRect = textObject.GetComponent<RectTransform>(); // 텍스트 RectTransform 조회
            textRect.anchorMin = Vector2.zero; // 패널 전체 최소 앵커 지정
            textRect.anchorMax = Vector2.one; // 패널 전체 최대 앵커 지정
            textRect.offsetMin = new Vector2(16f, 16f); // 왼쪽 아래 내부 여백 지정
            textRect.offsetMax = new Vector2(-16f, -16f); // 오른쪽 위 내부 여백 지정
            logText = textObject.GetComponent<Text>(); // Text 컴포넌트 조회
            logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 폰트 지정
            logText.fontSize = 16; // 로그 글자 크기 지정
            logText.lineSpacing = 1f; // 기본 줄 간격 지정
            logText.alignment = TextAnchor.UpperLeft; // 왼쪽 위 정렬 지정
            logText.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 문장 자동 줄바꿈 지정
            logText.verticalOverflow = VerticalWrapMode.Truncate; // 패널 밖 텍스트 숨김 지정
            logText.color = Color.white; // 흰색 글자 지정
            logText.supportRichText = false; // 리치 텍스트 해석 비활성화
            logText.raycastTarget = false; // 텍스트 마우스 입력 차단 방지
            RefreshText(); // 초기 문구 적용
        }

        private void SetVisible(bool visible) // 패널 표시 상태 변경
        {
            isVisible = visible; // 표시 상태 저장

            if (panelObject != null) // 패널 생성 여부 확인
            {
                panelObject.SetActive(isVisible); // 패널 활성 상태 적용
            }

            if (isVisible) // 표시 전환 확인
            {
                RefreshText(); // 최신 로그 즉시 표시
            }
        }

        private void RefreshText() // 로그 화면 문자열 갱신
        {
            if (logText == null) // 텍스트 생성 여부 확인
            {
                return; // 갱신 불가 종료
            }

            logText.text = logBuffer.BuildDisplayText(VisibleLogLines); // 최근 로그 문자열 적용
        }
    }
}
