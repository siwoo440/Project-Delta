using System; // 예외와 문자열 기능
using System.Reflection; // 기존 컨트롤러 메서드 호출
using System.Text; // UI 상태 지문 생성
using UnityEngine; // Unity 런타임 기능

public sealed class EventBattleRuntimeAdapter : MonoBehaviour // 기존 EventBattleController와 UGUI 자동 연결기
{
    private static EventBattleRuntimeAdapter _instance; // 전역 자동 연결기 인스턴스
    private MonoBehaviour _controller; // 실제 EventBattleController 참조
    private MethodInfo _runtimeUiBuilder; // 자동 변환된 UI 빌더 메서드
    private EventBattleRuntimeView _view; // 런타임 UGUI 화면
    private string _lastFingerprint = string.Empty; // 마지막 표시 상태 지문
    private float _nextControllerSearchTime; // 다음 컨트롤러 검색 시간
    private bool _isBuilding; // 중복 UI 빌드 방지 상태

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // 장면 로드 후 자동 시작
    private static void Bootstrap() // 런타임 자동 연결기 생성
    {
        if (_instance != null) // 기존 연결기 확인
        {
            return; // 중복 생성 방지
        }

        GameObject host = new GameObject("EventBattleRuntimeAdapter"); // 자동 연결기 호스트 생성
        DontDestroyOnLoad(host); // 장면 전환 시 연결기 유지
        _instance = host.AddComponent<EventBattleRuntimeAdapter>(); // 자동 연결기 컴포넌트 추가
    }

    private void Awake() // 연결기 초기 준비
    {
        if (_instance != null && _instance != this) // 중복 연결기 확인
        {
            Destroy(gameObject); // 중복 호스트 제거
            return; // 중복 인스턴스 종료
        }

        _instance = this; // 현재 연결기 전역 등록
        _view = gameObject.AddComponent<EventBattleRuntimeView>(); // 런타임 UGUI 화면 추가
        _view.Initialize(); // UGUI 화면 초기화
        _view.ButtonPressed = HandleButton; // 버튼 입력 연결
        _view.ToggleChanged = HandleToggle; // 토글 입력 연결
        _view.TextChanged = HandleText; // 텍스트 입력 연결
        _view.SliderChanged = HandleSlider; // 슬라이더 입력 연결
        _view.SelectionChanged = HandleSelection; // 선택 입력 연결
    }

    private void Update() // 기존 OnGUI 상태를 변경 시점 UGUI로 반영
    {
        EnsureController(); // 활성 이벤트 전투 컨트롤러 확인

        if (_controller == null || _runtimeUiBuilder == null) // 연결 가능한 컨트롤러 확인
        {
            _view.Hide(); // 대상 없음 화면 숨김
            return; // 갱신 종료
        }

        EventBattleRuntimeFrame frame = BuildFrame(null); // 현재 컨트롤러 상태를 UI 프레임으로 읽기

        if (frame == null || frame.Root == null || frame.Root.Children.Count == 0) // 이벤트 전투 UI 표시 여부 확인
        {
            _view.Hide(); // 이벤트 전투 화면 숨김
            _lastFingerprint = string.Empty; // 표시 상태 지문 초기화
            return; // 갱신 종료
        }

        string fingerprint = BuildFingerprint(frame); // 현재 UI 상태 지문 생성

        if (!string.Equals(_lastFingerprint, fingerprint, StringComparison.Ordinal)) // 실제 표시 상태 변경 확인
        {
            _lastFingerprint = fingerprint; // 새 상태 지문 저장
            _view.Show(frame); // 상태 변경 시점에만 UGUI 재생성
        }
        else if (!_view.IsVisible) // 동일 상태지만 화면 숨김 여부 확인
        {
            _view.Show(frame); // 기존 상태 다시 표시
        }
    }

    private void EnsureController() // EventBattleController 자동 검색과 빌더 연결
    {
        if (_controller != null && _runtimeUiBuilder != null && _controller.gameObject.activeInHierarchy && _controller.enabled) // 기존 활성 연결 상태 확인
        {
            return; // 기존 활성 연결 유지
        }

        if (Time.unscaledTime < _nextControllerSearchTime) // 검색 주기 확인
        {
            return; // 다음 검색 시점까지 대기
        }

        _nextControllerSearchTime = Time.unscaledTime + 0.5f; // 다음 검색 시간 예약
        MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>(); // 현재 로드된 MonoBehaviour 검색
        MonoBehaviour fallback = null; // 비활성 컨트롤러 후보 저장

        for (int index = 0; index < behaviours.Length; index++) // 모든 MonoBehaviour 순회
        {
            MonoBehaviour behaviour = behaviours[index]; // 현재 컴포넌트 참조

            if (behaviour == null) // 파괴된 컴포넌트 확인
            {
                continue; // 빈 컴포넌트 건너뛰기
            }

            if (!behaviour.gameObject.scene.IsValid()) // 실제 장면 오브젝트 확인
            {
                continue; // 프리팹과 에셋 오브젝트 제외
            }

            if (!string.Equals(behaviour.GetType().Name, "EventBattleController", StringComparison.Ordinal)) // 대상 타입 이름 확인
            {
                continue; // 다른 컨트롤러 건너뛰기
            }

            if (behaviour.gameObject.activeInHierarchy && behaviour.enabled) // 활성 컨트롤러 확인
            {
                SetController(behaviour); // 활성 컨트롤러 즉시 연결
                return; // 검색 종료
            }

            fallback = behaviour; // 비활성 컨트롤러 후보 보관
        }

        if (fallback != null) // 비활성 후보 존재 확인
        {
            SetController(fallback); // 비활성 후보 연결
        }
    }

    private void SetController(MonoBehaviour controller) // 컨트롤러와 자동 변환 메서드 연결
    {
        _controller = controller; // 컨트롤러 참조 저장
        _runtimeUiBuilder = controller.GetType().GetMethod(Day141RuntimeMethodName(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); // 자동 변환 UI 빌더 검색
        _lastFingerprint = string.Empty; // 컨트롤러 교체 시 상태 지문 초기화
    }

    private EventBattleRuntimeFrame BuildFrame(EventBattleRuntimeInput input) // 기존 OnGUI 코드를 프록시 프레임으로 실행
    {
        if (_isBuilding) // 중복 실행 확인
        {
            return null; // 재진입 차단
        }

        _isBuilding = true; // UI 빌드 진행 상태 설정
        EventBattleRuntimeGuiProxy.BeginFrame(input); // 프록시 UI 기록 시작

        try // 원본 OnGUI 분기 안전 실행
        {
            _runtimeUiBuilder.Invoke(_controller, null); // 자동 변환된 기존 OnGUI 본문 실행
        }
        catch (TargetInvocationException exception) // Reflection 내부 예외 처리
        {
            if (!(exception.InnerException is EventBattleRuntimeExitGuiException)) // 정상 ExitGUI 신호 여부 확인
            {
                AppendRuntimeError(exception.InnerException ?? exception); // 실제 런타임 오류를 화면에 표시
            }
        }
        catch (EventBattleRuntimeExitGuiException) // 직접 ExitGUI 신호 처리
        {
        }
        catch (Exception exception) // 기타 UI 변환 오류 처리
        {
            AppendRuntimeError(exception); // 오류 내용을 화면에 표시
        }
        finally // UI 프레임 종료 보장
        {
            _isBuilding = false; // UI 빌드 진행 상태 해제
        }

        return EventBattleRuntimeGuiProxy.EndFrame(); // 완성된 UI 프레임 반환
    }

    private void ReplayInput(EventBattleRuntimeInput input) // UGUI 입력을 기존 OnGUI 반환값으로 재생
    {
        if (_controller == null || _runtimeUiBuilder == null) // 연결 상태 확인
        {
            return; // 입력 처리 대상 없음 종료
        }

        BuildFrame(input); // 클릭 또는 값 변경 분기 한 번 실행
        EventBattleRuntimeFrame refreshed = BuildFrame(null); // 변경된 상태로 깨끗한 UI 프레임 재생성

        if (refreshed == null || refreshed.Root == null || refreshed.Root.Children.Count == 0) // 변경 후 화면 종료 여부 확인
        {
            _view.Hide(); // 종료된 이벤트 전투 화면 숨김
            _lastFingerprint = string.Empty; // 상태 지문 초기화
            return; // 입력 처리 종료
        }

        _lastFingerprint = BuildFingerprint(refreshed); // 새 상태 지문 저장
        _view.Show(refreshed); // 입력 직후 즉시 UGUI 갱신
    }

    private void HandleButton(int controlId) // UGUI 버튼 입력 처리
    {
        EventBattleRuntimeInput input = new EventBattleRuntimeInput(); // 버튼 재생 입력 생성
        input.Kind = EventBattleRuntimeInputKind.Button; // 버튼 입력 종류 설정
        input.ControlId = controlId; // 대상 버튼 번호 설정
        ReplayInput(input); // 기존 버튼 분기 실행
    }

    private void HandleToggle(int controlId, bool value) // UGUI 토글 입력 처리
    {
        EventBattleRuntimeInput input = new EventBattleRuntimeInput(); // 토글 재생 입력 생성
        input.Kind = EventBattleRuntimeInputKind.Toggle; // 토글 입력 종류 설정
        input.ControlId = controlId; // 대상 토글 번호 설정
        input.BoolValue = value; // 새 토글 값 설정
        ReplayInput(input); // 기존 토글 반환값 분기 실행
    }

    private void HandleText(int controlId, string value) // UGUI 텍스트 입력 처리
    {
        EventBattleRuntimeInput input = new EventBattleRuntimeInput(); // 텍스트 재생 입력 생성
        input.Kind = EventBattleRuntimeInputKind.Text; // 텍스트 입력 종류 설정
        input.ControlId = controlId; // 대상 입력 번호 설정
        input.StringValue = value ?? string.Empty; // 새 텍스트 값 설정
        ReplayInput(input); // 기존 텍스트 반환값 분기 실행
    }

    private void HandleSlider(int controlId, float value) // UGUI 슬라이더 입력 처리
    {
        EventBattleRuntimeInput input = new EventBattleRuntimeInput(); // 슬라이더 재생 입력 생성
        input.Kind = EventBattleRuntimeInputKind.Slider; // 슬라이더 입력 종류 설정
        input.ControlId = controlId; // 대상 슬라이더 번호 설정
        input.FloatValue = value; // 새 슬라이더 값 설정
        ReplayInput(input); // 기존 슬라이더 반환값 분기 실행
    }

    private void HandleSelection(int controlId, int value) // UGUI 선택 입력 처리
    {
        EventBattleRuntimeInput input = new EventBattleRuntimeInput(); // 선택 재생 입력 생성
        input.Kind = EventBattleRuntimeInputKind.Selection; // 선택 입력 종류 설정
        input.ControlId = controlId; // 대상 선택 번호 설정
        input.IntValue = value; // 새 선택 인덱스 설정
        ReplayInput(input); // 기존 선택 반환값 분기 실행
    }

    private void AppendRuntimeError(Exception exception) // 변환 중 오류를 UGUI 프레임에 표시
    {
        string message = exception != null ? exception.GetType().Name + ": " + exception.Message : "알 수 없는 이벤트 전투 UI 오류"; // 오류 문자열 생성
        EventBattleRuntimeGuiProxy.BeginVertical(); // 오류 표시 그룹 시작
        EventBattleRuntimeGuiProxy.Box("[Day141 UGUI 변환 오류]"); // 오류 제목 표시
        EventBattleRuntimeGuiProxy.Label(message); // 오류 상세 표시
        EventBattleRuntimeGuiProxy.EndVertical(); // 오류 표시 그룹 종료
        if (exception != null) // 로그 가능한 예외 확인
        {
            Debug.LogException(exception); // Unity Console에도 원본 오류 기록
        }
    }

    private string BuildFingerprint(EventBattleRuntimeFrame frame) // 화면 상태 변경 감지용 문자열 지문 생성
    {
        StringBuilder builder = new StringBuilder(4096); // 상태 지문 버퍼 생성
        AppendNodeFingerprint(builder, frame.Root); // 전체 UI 트리 상태 기록
        return builder.ToString(); // 최종 상태 지문 반환
    }

    private void AppendNodeFingerprint(StringBuilder builder, EventBattleRuntimeNode node) // UI 노드 상태 재귀 기록
    {
        if (node == null) // 빈 노드 확인
        {
            builder.Append("null|"); // 빈 노드 표식 추가
            return; // 재귀 종료
        }

        builder.Append((int)node.Kind).Append('|'); // 노드 종류 기록
        builder.Append(node.Text).Append('|'); // 표시 문자열 기록
        builder.Append(node.Interactable ? '1' : '0').Append('|'); // 활성 상태 기록
        builder.Append(node.BoolValue ? '1' : '0').Append('|'); // 토글 값 기록
        builder.Append(node.StringValue).Append('|'); // 텍스트 입력값 기록
        builder.Append(node.FloatValue).Append('|'); // 슬라이더 값 기록
        builder.Append(node.IntValue).Append('|'); // 선택 값 기록
        builder.Append(node.ControlId).Append('|'); // 컨트롤 번호 기록

        if (node.Options != null) // 선택 항목 존재 확인
        {
            for (int index = 0; index < node.Options.Length; index++) // 선택 항목 순회
            {
                builder.Append(node.Options[index]).Append('^'); // 선택 문자열 기록
            }
        }

        builder.Append('{'); // 자식 목록 시작 표식

        for (int index = 0; index < node.Children.Count; index++) // 자식 노드 순회
        {
            AppendNodeFingerprint(builder, node.Children[index]); // 자식 상태 재귀 기록
        }

        builder.Append('}'); // 자식 목록 종료 표식
    }

    private string Day141RuntimeMethodName() // 자동 패처와 공유하는 런타임 메서드 이름 반환
    {
        return "BuildEventBattleRuntimeUi141"; // 자동 변환 메서드 이름 반환
    }
}
