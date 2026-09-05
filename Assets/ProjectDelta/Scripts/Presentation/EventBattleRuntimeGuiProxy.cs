using System; // 예외와 기본 형식
using System.Collections.Generic; // UI 노드 목록
using UnityEngine; // Unity GUI 호환 형식

public enum EventBattleRuntimeNodeKind // 런타임 UI 노드 종류
{
    Root, // 최상위 세로 그룹
    Vertical, // 세로 그룹
    Horizontal, // 가로 그룹
    Area, // 영역 그룹
    Scroll, // 스크롤 그룹
    Label, // 텍스트 표시
    Box, // 강조 박스
    Button, // 버튼
    Toggle, // 토글
    TextField, // 한 줄 입력
    TextArea, // 여러 줄 입력
    Slider, // 슬라이더
    SelectionGrid, // 선택 격자
    Toolbar, // 툴바 선택
    Space, // 고정 여백
    FlexibleSpace // 가변 여백
}

public enum EventBattleRuntimeInputKind // UGUI 입력 재생 종류
{
    None, // 입력 없음
    Button, // 버튼 입력
    Toggle, // 토글 입력
    Text, // 텍스트 입력
    Slider, // 슬라이더 입력
    Selection // 선택 입력
}

public sealed class EventBattleRuntimeInput // 원본 OnGUI 분기 재생용 입력
{
    public EventBattleRuntimeInputKind Kind; // 입력 종류
    public int ControlId = -1; // 대상 컨트롤 번호
    public bool BoolValue; // 토글 값
    public string StringValue = string.Empty; // 텍스트 값
    public float FloatValue; // 슬라이더 값
    public int IntValue; // 선택 인덱스
}

public sealed class EventBattleRuntimeNode // 프록시가 기록하는 UI 요소
{
    public EventBattleRuntimeNodeKind Kind; // 요소 종류
    public string Text = string.Empty; // 표시 문자열
    public bool Interactable = true; // 입력 가능 여부
    public bool BoolValue; // 토글 현재값
    public string StringValue = string.Empty; // 입력 현재값
    public float FloatValue; // 슬라이더 현재값
    public float MinValue; // 슬라이더 최소값
    public float MaxValue; // 슬라이더 최대값
    public int IntValue; // 선택 현재값
    public int Columns = 1; // 선택 격자 열 수
    public int ControlId = -1; // 입력 컨트롤 번호
    public float SpaceSize; // 고정 여백 크기
    public Color TextColor = Color.white; // 텍스트 색상
    public Color BackgroundColor = Color.white; // 배경 색상
    public string[] Options = Array.Empty<string>(); // 선택 항목 문자열
    public readonly List<EventBattleRuntimeNode> Children = new List<EventBattleRuntimeNode>(); // 자식 UI 요소
}

public sealed class EventBattleRuntimeFrame // 한 번의 OnGUI 변환 결과
{
    public EventBattleRuntimeNode Root; // 최상위 UI 트리
    public int ControlCount; // 입력 가능한 컨트롤 수
    public bool ExitRequested; // ExitGUI 호출 여부
}

public sealed class EventBattleRuntimeExitGuiException : Exception // 기존 ExitGUI 흐름 종료 신호
{
}

public static class EventBattleRuntimeGuiProxy // IMGUI 호출을 UGUI 데이터로 기록하는 프록시
{
    private static EventBattleRuntimeFrame _frame; // 현재 생성 중인 프레임
    private static readonly Stack<EventBattleRuntimeNode> _groupStack = new Stack<EventBattleRuntimeNode>(); // 현재 레이아웃 그룹 스택
    private static EventBattleRuntimeInput _pendingInput; // 재생할 UGUI 입력
    private static int _nextControlId; // 다음 컨트롤 번호
    private static bool _inputConsumed; // 현재 입력 소비 여부
    private static Event _fallbackEvent; // OnGUI 외부용 대체 이벤트
    private static GUISkin _skin; // OnGUI 외부용 대체 GUI 스킨

    public static bool enabled = true; // 기존 GUI.enabled 호환 값
    public static bool changed; // 기존 GUI.changed 호환 값
    public static Color color = Color.white; // 기존 GUI.color 호환 값
    public static Color backgroundColor = Color.white; // 기존 GUI.backgroundColor 호환 값
    public static Color contentColor = Color.white; // 기존 GUI.contentColor 호환 값
    public static GUISkin skin // 기존 GUI.skin 호환 값
    {
        get
        {
            if (_skin == null) // 대체 스킨 존재 확인
            {
                _skin = CreateFallbackSkin(); // OnGUI 외부용 스킨 생성
            }

            return _skin; // 현재 대체 스킨 반환
        }
        set
        {
            _skin = value; // 기존 코드의 스킨 변경값 저장
        }
    }
    public static int depth; // 기존 GUI.depth 호환 값
    public static Matrix4x4 matrix = Matrix4x4.identity; // 기존 GUI.matrix 호환 값
    public static string tooltip = string.Empty; // 기존 GUI.tooltip 호환 값

    public static Event CurrentEvent // 기존 Event.current 대체 값
    {
        get
        {
            if (_fallbackEvent == null) // 대체 이벤트 존재 확인
            {
                _fallbackEvent = new Event(); // 빈 이벤트 생성
            }

            return _fallbackEvent; // 대체 이벤트 반환
        }
    }

    public static void BeginFrame(EventBattleRuntimeInput pendingInput) // 새 프록시 프레임 시작
    {
        EventBattleRuntimeNode root = new EventBattleRuntimeNode(); // 최상위 노드 생성
        root.Kind = EventBattleRuntimeNodeKind.Root; // 최상위 종류 지정
        _frame = new EventBattleRuntimeFrame(); // 새 프레임 생성
        _frame.Root = root; // 최상위 노드 연결
        _groupStack.Clear(); // 이전 그룹 스택 초기화
        _groupStack.Push(root); // 최상위 그룹 시작
        _pendingInput = pendingInput; // 재생 입력 저장
        _nextControlId = 0; // 컨트롤 번호 초기화
        _inputConsumed = false; // 입력 소비 상태 초기화
        enabled = true; // 기본 입력 활성화
        changed = false; // 변경 상태 초기화
        color = Color.white; // 기본 전체 색상 초기화
        backgroundColor = Color.white; // 기본 배경 색상 초기화
        contentColor = Color.white; // 기본 내용 색상 초기화
    }

    public static EventBattleRuntimeFrame EndFrame() // 현재 프록시 프레임 종료
    {
        if (_frame == null) // 프레임 존재 확인
        {
            BeginFrame(null); // 빈 프레임 생성
        }

        _frame.ControlCount = _nextControlId; // 최종 컨트롤 수 저장
        EventBattleRuntimeFrame result = _frame; // 현재 프레임 복사
        _frame = null; // 현재 프레임 해제
        _groupStack.Clear(); // 그룹 스택 정리
        _pendingInput = null; // 재생 입력 정리
        return result; // 완성 프레임 반환
    }

    public static void ExitGUI() // 기존 GUIUtility.ExitGUI 대체
    {
        if (_frame != null) // 현재 프레임 확인
        {
            _frame.ExitRequested = true; // 조기 종료 상태 기록
        }

        throw new EventBattleRuntimeExitGuiException(); // 현재 빌드 흐름 즉시 종료
    }


    public static void BeginGroup(Rect position) // 기존 GUI.BeginGroup 호환
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, string.Empty); // 그룹 영역 생성
    }

    public static void BeginGroup(Rect position, string text) // 문자열 그룹 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, text); // 제목 포함 그룹 영역 생성
    }

    public static void BeginGroup(Rect position, GUIContent content) // GUIContent 그룹 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, ContentText(content)); // GUIContent 제목 그룹 생성
    }

    public static void BeginGroup(Rect position, GUIContent content, GUIStyle style) // 스타일 그룹 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, ContentText(content)); // 스타일 그룹 영역 생성
    }

    public static void EndGroup() // 기존 GUI.EndGroup 호환
    {
        PopGroup(); // 현재 그룹 종료
    }

    public static void SetNextControlName(string name) // 기존 GUI.SetNextControlName 호환
    {
        tooltip = name ?? string.Empty; // 다음 컨트롤 이름 임시 저장
    }

    public static string GetNameOfFocusedControl() // 기존 GUI.GetNameOfFocusedControl 호환
    {
        return string.Empty; // 런타임 포커스 이름 미사용 반환
    }

    public static void FocusControl(string name) // 기존 GUI.FocusControl 호환
    {
        tooltip = name ?? string.Empty; // 요청 포커스 이름 임시 저장
    }

    public static void BeginHorizontal(params GUILayoutOption[] options) // 가로 레이아웃 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Horizontal, string.Empty); // 가로 그룹 생성
    }

    public static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options) // 스타일 가로 레이아웃 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Horizontal, string.Empty); // 가로 그룹 생성
    }

    public static void BeginHorizontal(string text, GUIStyle style, params GUILayoutOption[] options) // 제목 포함 가로 레이아웃 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Horizontal, text); // 제목 포함 가로 그룹 생성
    }

    public static void BeginHorizontal(GUIContent content, GUIStyle style, params GUILayoutOption[] options) // GUIContent 가로 레이아웃 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Horizontal, ContentText(content)); // GUIContent 가로 그룹 생성
    }

    public static void EndHorizontal() // 가로 레이아웃 종료
    {
        PopGroup(); // 현재 그룹 종료
    }

    public static void BeginVertical(params GUILayoutOption[] options) // 세로 레이아웃 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Vertical, string.Empty); // 세로 그룹 생성
    }

    public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options) // 스타일 세로 레이아웃 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Vertical, string.Empty); // 세로 그룹 생성
    }

    public static void BeginVertical(string text, GUIStyle style, params GUILayoutOption[] options) // 제목 포함 세로 레이아웃 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Vertical, text); // 제목 포함 세로 그룹 생성
    }

    public static void BeginVertical(GUIContent content, GUIStyle style, params GUILayoutOption[] options) // GUIContent 세로 레이아웃 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Vertical, ContentText(content)); // GUIContent 세로 그룹 생성
    }

    public static void EndVertical() // 세로 레이아웃 종료
    {
        PopGroup(); // 현재 그룹 종료
    }

    public static void BeginArea(Rect screenRect) // 고정 영역 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, string.Empty); // 영역 그룹 생성
    }

    public static void BeginArea(Rect screenRect, string text) // 제목 포함 고정 영역 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, text); // 제목 포함 영역 그룹 생성
    }

    public static void BeginArea(Rect screenRect, GUIContent content) // GUIContent 영역 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, ContentText(content)); // GUIContent 제목 영역 생성
    }

    public static void BeginArea(Rect screenRect, string text, GUIStyle style) // 스타일 영역 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, text); // 스타일 영역 그룹 생성
    }

    public static void BeginArea(Rect screenRect, GUIContent content, GUIStyle style) // GUIContent 스타일 영역 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, ContentText(content)); // GUIContent 스타일 영역 생성
    }

    public static void EndArea() // 고정 영역 종료
    {
        PopGroup(); // 현재 영역 종료
    }

    public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options) // 기본 스크롤 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Scroll, string.Empty); // 스크롤 그룹 생성
        return scrollPosition; // 기존 스크롤 위치 유지
    }

    public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, params GUILayoutOption[] options) // 표시 옵션 스크롤 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Scroll, string.Empty); // 스크롤 그룹 생성
        return scrollPosition; // 기존 스크롤 위치 유지
    }

    public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle style, params GUILayoutOption[] options) // 배경 스타일 스크롤 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Scroll, string.Empty); // 스크롤 그룹 생성
        return scrollPosition; // 기존 스크롤 위치 유지
    }

    public static Vector2 BeginScrollView(Vector2 scrollPosition, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, params GUILayoutOption[] options) // 스타일 스크롤 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Scroll, string.Empty); // 스크롤 그룹 생성
        return scrollPosition; // 기존 스크롤 위치 유지
    }

    public static Vector2 BeginScrollView(Vector2 scrollPosition, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background, params GUILayoutOption[] options) // 전체 스타일 스크롤 시작
    {
        PushGroup(EventBattleRuntimeNodeKind.Scroll, string.Empty); // 스크롤 그룹 생성
        return scrollPosition; // 기존 스크롤 위치 유지
    }

    public static void EndScrollView() // 스크롤 종료
    {
        PopGroup(); // 현재 스크롤 그룹 종료
    }

    public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect) // 기존 GUI.BeginScrollView 기본 호환
    {
        PushGroup(EventBattleRuntimeNodeKind.Scroll, string.Empty); // 고정 좌표 스크롤 그룹 생성
        return scrollPosition; // 기존 스크롤 위치 유지
    }

    public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical) // 기존 GUI.BeginScrollView 표시 옵션 호환
    {
        PushGroup(EventBattleRuntimeNodeKind.Scroll, string.Empty); // 고정 좌표 스크롤 그룹 생성
        return scrollPosition; // 기존 스크롤 위치 유지
    }

    public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar) // 기존 GUI.BeginScrollView 스타일 호환
    {
        PushGroup(EventBattleRuntimeNodeKind.Scroll, string.Empty); // 고정 좌표 스크롤 그룹 생성
        return scrollPosition; // 기존 스크롤 위치 유지
    }

    public static void EndScrollView(bool handleScrollWheel) // 기존 GUI.EndScrollView 옵션 호환
    {
        PopGroup(); // 현재 스크롤 그룹 종료
    }

    public static void Label(string text, params GUILayoutOption[] options) // 기본 텍스트 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Label, text); // 텍스트 노드 기록
    }

    public static void Label(string text, GUIStyle style, params GUILayoutOption[] options) // 스타일 텍스트 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Label, text); // 텍스트 노드 기록
    }

    public static void Label(GUIContent content, params GUILayoutOption[] options) // GUIContent 텍스트 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Label, ContentText(content)); // 텍스트 노드 기록
    }

    public static void Label(GUIContent content, GUIStyle style, params GUILayoutOption[] options) // 스타일 GUIContent 텍스트 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Label, ContentText(content)); // 텍스트 노드 기록
    }

    public static void Label(Texture image, params GUILayoutOption[] options) // 이미지 라벨 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, image != null ? image.name : "이미지"); // 이미지 자리 노드 기록
    }

    public static void Label(Texture image, GUIStyle style, params GUILayoutOption[] options) // 스타일 이미지 라벨 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, image != null ? image.name : "이미지"); // 이미지 자리 노드 기록
    }

    public static void Label(Rect position, string text) // 고정 좌표 텍스트 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Label, text); // 텍스트 노드 기록
    }

    public static void Label(Rect position, string text, GUIStyle style) // 고정 좌표 스타일 텍스트 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Label, text); // 텍스트 노드 기록
    }

    public static void Label(Rect position, GUIContent content) // 고정 좌표 GUIContent 텍스트 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Label, ContentText(content)); // 텍스트 노드 기록
    }

    public static void Label(Rect position, GUIContent content, GUIStyle style) // 고정 좌표 스타일 GUIContent 텍스트 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Label, ContentText(content)); // 텍스트 노드 기록
    }

    public static void Box(string text, params GUILayoutOption[] options) // 기본 박스 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, text); // 박스 노드 기록
    }

    public static void Box(string text, GUIStyle style, params GUILayoutOption[] options) // 스타일 박스 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, text); // 박스 노드 기록
    }

    public static void Box(GUIContent content, params GUILayoutOption[] options) // GUIContent 박스 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, ContentText(content)); // 박스 노드 기록
    }

    public static void Box(GUIContent content, GUIStyle style, params GUILayoutOption[] options) // 스타일 GUIContent 박스 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, ContentText(content)); // 스타일 GUIContent 박스 기록
    }

    public static void Box(Texture image, params GUILayoutOption[] options) // 이미지 박스 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, image != null ? image.name : "이미지"); // 이미지 자리 노드 기록
    }

    public static void Box(Texture image, GUIStyle style, params GUILayoutOption[] options) // 스타일 이미지 박스 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, image != null ? image.name : "이미지"); // 이미지 자리 노드 기록
    }

    public static void Box(Rect position, string text) // 고정 좌표 박스 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, text); // 박스 노드 기록
    }

    public static void Box(Rect position, string text, GUIStyle style) // 고정 좌표 스타일 박스 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, text); // 고정 좌표 스타일 박스 기록
    }

    public static void Box(Rect position, GUIContent content) // 고정 좌표 GUIContent 박스 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, ContentText(content)); // 박스 노드 기록
    }

    public static void Box(Rect position, GUIContent content, GUIStyle style) // 고정 좌표 스타일 GUIContent 박스 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, ContentText(content)); // 고정 좌표 스타일 GUIContent 박스 기록
    }

    public static void DrawTexture(Rect position, Texture image) // 고정 좌표 이미지 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, image != null ? image.name : "이미지"); // 이미지 자리 노드 기록
    }

    public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode) // 스케일 모드 이미지 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, image != null ? image.name : "이미지"); // 이미지 자리 노드 기록
    }

    public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend) // 알파 옵션 이미지 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, image != null ? image.name : "이미지"); // 이미지 자리 노드 기록
    }

    public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect) // 비율 옵션 이미지 표시
    {
        AddTextNode(EventBattleRuntimeNodeKind.Box, image != null ? image.name : "이미지"); // 이미지 자리 노드 기록
    }

    public static bool Button(string text, params GUILayoutOption[] options) // 기본 버튼 입력
    {
        return AddButton(text); // 버튼 노드와 입력 결과 반환
    }

    public static bool Button(string text, GUIStyle style, params GUILayoutOption[] options) // 스타일 버튼 입력
    {
        return AddButton(text); // 버튼 노드와 입력 결과 반환
    }

    public static bool Button(GUIContent content, params GUILayoutOption[] options) // GUIContent 버튼 입력
    {
        return AddButton(ContentText(content)); // 버튼 노드와 입력 결과 반환
    }

    public static bool Button(GUIContent content, GUIStyle style, params GUILayoutOption[] options) // 스타일 GUIContent 버튼 입력
    {
        return AddButton(ContentText(content)); // 버튼 노드와 입력 결과 반환
    }

    public static bool Button(Texture image, params GUILayoutOption[] options) // 이미지 버튼 입력
    {
        return AddButton(image != null ? image.name : "이미지"); // 이미지 이름 버튼으로 처리
    }

    public static bool Button(Texture image, GUIStyle style, params GUILayoutOption[] options) // 스타일 이미지 버튼 입력
    {
        return AddButton(image != null ? image.name : "이미지"); // 이미지 이름 버튼으로 처리
    }

    public static bool Button(Rect position, string text) // 고정 좌표 버튼 입력
    {
        return AddButton(text); // 버튼 노드와 입력 결과 반환
    }

    public static bool Button(Rect position, string text, GUIStyle style) // 고정 좌표 스타일 버튼 입력
    {
        return AddButton(text); // 버튼 노드와 입력 결과 반환
    }

    public static bool Button(Rect position, GUIContent content) // 고정 좌표 GUIContent 버튼 입력
    {
        return AddButton(ContentText(content)); // 버튼 노드와 입력 결과 반환
    }

    public static bool Button(Rect position, GUIContent content, GUIStyle style) // 고정 좌표 스타일 GUIContent 버튼 입력
    {
        return AddButton(ContentText(content)); // 버튼 노드와 입력 결과 반환
    }

    public static bool RepeatButton(string text, params GUILayoutOption[] options) // 반복 버튼 입력
    {
        return AddButton(text); // 일반 버튼 방식으로 처리
    }

    public static bool RepeatButton(string text, GUIStyle style, params GUILayoutOption[] options) // 스타일 반복 버튼 입력
    {
        return AddButton(text); // 스타일 반복 버튼 처리
    }

    public static bool RepeatButton(GUIContent content, params GUILayoutOption[] options) // GUIContent 반복 버튼 입력
    {
        return AddButton(ContentText(content)); // GUIContent 반복 버튼 처리
    }

    public static bool RepeatButton(GUIContent content, GUIStyle style, params GUILayoutOption[] options) // 스타일 GUIContent 반복 버튼 입력
    {
        return AddButton(ContentText(content)); // 스타일 GUIContent 반복 버튼 처리
    }

    public static bool RepeatButton(Texture image, params GUILayoutOption[] options) // 이미지 반복 버튼 입력
    {
        return AddButton(image != null ? image.name : "이미지"); // 이미지 반복 버튼 처리
    }

    public static bool RepeatButton(Texture image, GUIStyle style, params GUILayoutOption[] options) // 스타일 이미지 반복 버튼 입력
    {
        return AddButton(image != null ? image.name : "이미지"); // 스타일 이미지 반복 버튼 처리
    }

    public static bool RepeatButton(Rect position, string text) // 고정 좌표 반복 버튼 입력
    {
        return AddButton(text); // 일반 버튼 방식으로 처리
    }

    public static bool RepeatButton(Rect position, string text, GUIStyle style) // 고정 좌표 스타일 반복 버튼 입력
    {
        return AddButton(text); // 고정 좌표 스타일 반복 버튼 처리
    }

    public static bool RepeatButton(Rect position, GUIContent content) // 고정 좌표 GUIContent 반복 버튼 입력
    {
        return AddButton(ContentText(content)); // 고정 좌표 GUIContent 반복 버튼 처리
    }

    public static bool RepeatButton(Rect position, GUIContent content, GUIStyle style) // 고정 좌표 스타일 GUIContent 반복 버튼 입력
    {
        return AddButton(ContentText(content)); // 고정 좌표 스타일 GUIContent 반복 버튼 처리
    }

    public static bool Toggle(bool value, string text, params GUILayoutOption[] options) // 기본 토글 입력
    {
        return AddToggle(value, text); // 토글 노드와 입력 결과 반환
    }

    public static bool Toggle(bool value, string text, GUIStyle style, params GUILayoutOption[] options) // 스타일 토글 입력
    {
        return AddToggle(value, text); // 토글 노드와 입력 결과 반환
    }

    public static bool Toggle(bool value, GUIContent content, params GUILayoutOption[] options) // GUIContent 토글 입력
    {
        return AddToggle(value, ContentText(content)); // 토글 노드와 입력 결과 반환
    }

    public static bool Toggle(bool value, GUIContent content, GUIStyle style, params GUILayoutOption[] options) // 스타일 GUIContent 토글 입력
    {
        return AddToggle(value, ContentText(content)); // 스타일 GUIContent 토글 처리
    }

    public static bool Toggle(bool value, Texture image, params GUILayoutOption[] options) // 이미지 토글 입력
    {
        return AddToggle(value, image != null ? image.name : "이미지"); // 이미지 이름 토글로 처리
    }

    public static bool Toggle(bool value, Texture image, GUIStyle style, params GUILayoutOption[] options) // 스타일 이미지 토글 입력
    {
        return AddToggle(value, image != null ? image.name : "이미지"); // 스타일 이미지 토글 처리
    }

    public static bool Toggle(Rect position, bool value, string text) // 고정 좌표 토글 입력
    {
        return AddToggle(value, text); // 토글 노드와 입력 결과 반환
    }

    public static bool Toggle(Rect position, bool value, string text, GUIStyle style) // 고정 좌표 스타일 토글 입력
    {
        return AddToggle(value, text); // 토글 노드와 입력 결과 반환
    }

    public static bool Toggle(Rect position, bool value, GUIContent content) // 고정 좌표 GUIContent 토글 입력
    {
        return AddToggle(value, ContentText(content)); // 고정 좌표 GUIContent 토글 처리
    }

    public static bool Toggle(Rect position, bool value, GUIContent content, GUIStyle style) // 고정 좌표 스타일 GUIContent 토글 입력
    {
        return AddToggle(value, ContentText(content)); // 고정 좌표 스타일 GUIContent 토글 처리
    }

    public static string TextField(string text, params GUILayoutOption[] options) // 기본 한 줄 입력
    {
        return AddTextInput(EventBattleRuntimeNodeKind.TextField, text); // 텍스트 입력 결과 반환
    }

    public static string TextField(string text, int maxLength, params GUILayoutOption[] options) // 길이 제한 한 줄 입력
    {
        string value = AddTextInput(EventBattleRuntimeNodeKind.TextField, text); // 텍스트 입력 결과 생성
        return ClampText(value, maxLength); // 최대 길이 적용
    }

    public static string TextField(string text, GUIStyle style, params GUILayoutOption[] options) // 스타일 한 줄 입력
    {
        return AddTextInput(EventBattleRuntimeNodeKind.TextField, text); // 텍스트 입력 결과 반환
    }

    public static string TextField(string text, int maxLength, GUIStyle style, params GUILayoutOption[] options) // 길이 제한 스타일 한 줄 입력
    {
        string value = AddTextInput(EventBattleRuntimeNodeKind.TextField, text); // 스타일 텍스트 입력 결과 생성
        return ClampText(value, maxLength); // 최대 길이 적용
    }

    public static string TextField(Rect position, string text) // 고정 좌표 한 줄 입력
    {
        return AddTextInput(EventBattleRuntimeNodeKind.TextField, text); // 텍스트 입력 결과 반환
    }

    public static string TextField(Rect position, string text, int maxLength) // 고정 좌표 길이 제한 한 줄 입력
    {
        string value = AddTextInput(EventBattleRuntimeNodeKind.TextField, text); // 텍스트 입력 결과 생성
        return ClampText(value, maxLength); // 최대 길이 적용
    }

    public static string TextField(Rect position, string text, GUIStyle style) // 고정 좌표 스타일 한 줄 입력
    {
        return AddTextInput(EventBattleRuntimeNodeKind.TextField, text); // 고정 좌표 스타일 입력 처리
    }

    public static string TextField(Rect position, string text, int maxLength, GUIStyle style) // 고정 좌표 길이 제한 스타일 입력
    {
        string value = AddTextInput(EventBattleRuntimeNodeKind.TextField, text); // 고정 좌표 스타일 입력 결과 생성
        return ClampText(value, maxLength); // 최대 길이 적용
    }

    public static string TextArea(string text, params GUILayoutOption[] options) // 기본 여러 줄 입력
    {
        return AddTextInput(EventBattleRuntimeNodeKind.TextArea, text); // 여러 줄 입력 결과 반환
    }

    public static string TextArea(string text, GUIStyle style, params GUILayoutOption[] options) // 스타일 여러 줄 입력
    {
        return AddTextInput(EventBattleRuntimeNodeKind.TextArea, text); // 여러 줄 입력 결과 반환
    }

    public static string TextArea(string text, int maxLength, params GUILayoutOption[] options) // 길이 제한 여러 줄 입력
    {
        string value = AddTextInput(EventBattleRuntimeNodeKind.TextArea, text); // 여러 줄 입력 결과 생성
        return ClampText(value, maxLength); // 최대 길이 적용
    }

    public static string TextArea(string text, int maxLength, GUIStyle style, params GUILayoutOption[] options) // 길이 제한 스타일 여러 줄 입력
    {
        string value = AddTextInput(EventBattleRuntimeNodeKind.TextArea, text); // 스타일 여러 줄 입력 결과 생성
        return ClampText(value, maxLength); // 최대 길이 적용
    }

    public static string TextArea(Rect position, string text) // 고정 좌표 여러 줄 입력
    {
        return AddTextInput(EventBattleRuntimeNodeKind.TextArea, text); // 여러 줄 입력 결과 반환
    }

    public static string TextArea(Rect position, string text, GUIStyle style) // 고정 좌표 스타일 여러 줄 입력
    {
        return AddTextInput(EventBattleRuntimeNodeKind.TextArea, text); // 고정 좌표 스타일 여러 줄 입력 처리
    }

    public static string PasswordField(string password, char maskChar, params GUILayoutOption[] options) // 비밀번호 입력
    {
        return AddTextInput(EventBattleRuntimeNodeKind.TextField, password); // 일반 텍스트 입력 방식 처리
    }

    public static string PasswordField(string password, char maskChar, int maxLength, params GUILayoutOption[] options) // 길이 제한 비밀번호 입력
    {
        string value = AddTextInput(EventBattleRuntimeNodeKind.TextField, password); // 비밀번호 입력 결과 생성
        return ClampText(value, maxLength); // 최대 길이 적용
    }

    public static string PasswordField(string password, char maskChar, GUIStyle style, params GUILayoutOption[] options) // 스타일 비밀번호 입력
    {
        return AddTextInput(EventBattleRuntimeNodeKind.TextField, password); // 스타일 비밀번호 입력 처리
    }

    public static string PasswordField(string password, char maskChar, int maxLength, GUIStyle style, params GUILayoutOption[] options) // 길이 제한 스타일 비밀번호 입력
    {
        string value = AddTextInput(EventBattleRuntimeNodeKind.TextField, password); // 스타일 비밀번호 입력 결과 생성
        return ClampText(value, maxLength); // 최대 길이 적용
    }

    public static int SelectionGrid(int selected, string[] texts, int xCount, params GUILayoutOption[] options) // 문자열 선택 격자
    {
        return AddSelection(EventBattleRuntimeNodeKind.SelectionGrid, selected, texts, xCount); // 선택 결과 반환
    }

    public static int SelectionGrid(int selected, string[] texts, int xCount, GUIStyle style, params GUILayoutOption[] options) // 스타일 문자열 선택 격자
    {
        return AddSelection(EventBattleRuntimeNodeKind.SelectionGrid, selected, texts, xCount); // 스타일 문자열 선택 결과 반환
    }

    public static int SelectionGrid(int selected, GUIContent[] contents, int xCount, params GUILayoutOption[] options) // GUIContent 선택 격자
    {
        return AddSelection(EventBattleRuntimeNodeKind.SelectionGrid, selected, ContentTexts(contents), xCount); // 선택 결과 반환
    }

    public static int SelectionGrid(int selected, GUIContent[] contents, int xCount, GUIStyle style, params GUILayoutOption[] options) // 스타일 GUIContent 선택 격자
    {
        return AddSelection(EventBattleRuntimeNodeKind.SelectionGrid, selected, ContentTexts(contents), xCount); // 스타일 GUIContent 선택 결과 반환
    }

    public static int SelectionGrid(Rect position, int selected, string[] texts, int xCount) // 고정 좌표 선택 격자
    {
        return AddSelection(EventBattleRuntimeNodeKind.SelectionGrid, selected, texts, xCount); // 선택 결과 반환
    }

    public static int Toolbar(int selected, string[] texts, params GUILayoutOption[] options) // 문자열 툴바 선택
    {
        return AddSelection(EventBattleRuntimeNodeKind.Toolbar, selected, texts, Math.Max(1, texts != null ? texts.Length : 1)); // 툴바 선택 결과 반환
    }

    public static int Toolbar(int selected, string[] texts, GUIStyle style, params GUILayoutOption[] options) // 스타일 문자열 툴바 선택
    {
        return AddSelection(EventBattleRuntimeNodeKind.Toolbar, selected, texts, Math.Max(1, texts != null ? texts.Length : 1)); // 스타일 문자열 툴바 선택 반환
    }

    public static int Toolbar(int selected, GUIContent[] contents, params GUILayoutOption[] options) // GUIContent 툴바 선택
    {
        string[] texts = ContentTexts(contents); // GUIContent 문자열 변환
        return AddSelection(EventBattleRuntimeNodeKind.Toolbar, selected, texts, Math.Max(1, texts.Length)); // 툴바 선택 결과 반환
    }

    public static int Toolbar(int selected, GUIContent[] contents, GUIStyle style, params GUILayoutOption[] options) // 스타일 GUIContent 툴바 선택
    {
        string[] texts = ContentTexts(contents); // 스타일 GUIContent 문자열 변환
        return AddSelection(EventBattleRuntimeNodeKind.Toolbar, selected, texts, Math.Max(1, texts.Length)); // 스타일 GUIContent 툴바 선택 반환
    }

    public static int Toolbar(Rect position, int selected, string[] texts) // 고정 좌표 툴바 선택
    {
        return AddSelection(EventBattleRuntimeNodeKind.Toolbar, selected, texts, Math.Max(1, texts != null ? texts.Length : 1)); // 툴바 선택 결과 반환
    }

    public static float HorizontalSlider(float value, float leftValue, float rightValue, params GUILayoutOption[] options) // 가로 슬라이더 입력
    {
        return AddSlider(value, leftValue, rightValue); // 슬라이더 입력 결과 반환
    }

    public static float HorizontalSlider(float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, params GUILayoutOption[] options) // 스타일 가로 슬라이더 입력
    {
        return AddSlider(value, leftValue, rightValue); // 슬라이더 입력 결과 반환
    }

    public static float HorizontalSlider(Rect position, float value, float leftValue, float rightValue) // 고정 좌표 가로 슬라이더 입력
    {
        return AddSlider(value, leftValue, rightValue); // 슬라이더 입력 결과 반환
    }

    public static float VerticalSlider(float value, float topValue, float bottomValue, params GUILayoutOption[] options) // 세로 슬라이더 입력
    {
        return AddSlider(value, bottomValue, topValue); // 슬라이더 입력 결과 반환
    }

    public static float VerticalSlider(float value, float topValue, float bottomValue, GUIStyle slider, GUIStyle thumb, params GUILayoutOption[] options) // 스타일 세로 슬라이더 입력
    {
        return AddSlider(value, bottomValue, topValue); // 스타일 세로 슬라이더 결과 반환
    }

    public static float HorizontalScrollbar(float value, float size, float leftValue, float rightValue, params GUILayoutOption[] options) // GUILayout 가로 스크롤바 호환
    {
        return AddSlider(value, leftValue, rightValue); // 가로 스크롤 값을 슬라이더로 처리
    }

    public static float HorizontalScrollbar(Rect position, float value, float size, float leftValue, float rightValue) // GUI 가로 스크롤바 호환
    {
        return AddSlider(value, leftValue, rightValue); // 가로 스크롤 값을 슬라이더로 처리
    }

    public static float VerticalScrollbar(float value, float size, float topValue, float bottomValue, params GUILayoutOption[] options) // GUILayout 세로 스크롤바 호환
    {
        return AddSlider(value, bottomValue, topValue); // 세로 스크롤 값을 슬라이더로 처리
    }

    public static float VerticalScrollbar(Rect position, float value, float size, float topValue, float bottomValue) // GUI 세로 스크롤바 호환
    {
        return AddSlider(value, bottomValue, topValue); // 세로 스크롤 값을 슬라이더로 처리
    }

    public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string text, params GUILayoutOption[] options) // 기존 GUI/GUILayout Window 호환
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, text); // 창 영역 그룹 생성

        if (func != null) // 창 콜백 존재 확인
        {
            func(id); // 기존 창 그리기 메서드 실행
        }

        PopGroup(); // 창 영역 그룹 종료
        return screenRect; // 기존 창 위치 유지
    }

    public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, GUIContent content, params GUILayoutOption[] options) // GUIContent Window 호환
    {
        return Window(id, screenRect, func, ContentText(content), options); // 문자열 창 방식으로 처리
    }

    public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, GUIContent content, GUIStyle style, params GUILayoutOption[] options) // 스타일 GUIContent Window 호환
    {
        return Window(id, screenRect, func, ContentText(content), options); // 스타일 GUIContent 창 처리
    }

    public static Rect Window(int id, Rect screenRect, GUI.WindowFunction func, string text, GUIStyle style, params GUILayoutOption[] options) // 스타일 Window 호환
    {
        return Window(id, screenRect, func, text, options); // 기본 창 방식으로 처리
    }

    public static Rect ModalWindow(int id, Rect screenRect, GUI.WindowFunction func, string text) // 기존 GUI.ModalWindow 호환
    {
        return Window(id, screenRect, func, text); // 일반 창 방식으로 처리
    }

    public static void DragWindow() // 기존 GUI.DragWindow 호환
    {
    }

    public static void DragWindow(Rect position) // 범위 지정 GUI.DragWindow 호환
    {
    }

    public static void BeginClip(Rect position) // 기존 GUI.BeginClip 호환
    {
        PushGroup(EventBattleRuntimeNodeKind.Area, string.Empty); // 클립 영역 그룹 생성
    }

    public static void EndClip() // 기존 GUI.EndClip 호환
    {
        PopGroup(); // 클립 영역 그룹 종료
    }

    public static void Space(float pixels) // 고정 여백 추가
    {
        EventBattleRuntimeNode node = NewNode(EventBattleRuntimeNodeKind.Space); // 여백 노드 생성
        node.SpaceSize = Mathf.Max(0f, pixels); // 여백 크기 저장
        AddNode(node); // 여백 노드 추가
    }

    public static void FlexibleSpace() // 가변 여백 추가
    {
        EventBattleRuntimeNode node = NewNode(EventBattleRuntimeNodeKind.FlexibleSpace); // 가변 여백 노드 생성
        AddNode(node); // 가변 여백 노드 추가
    }

    public static GUILayoutOption Width(float width) // 기존 GUILayout.Width 호환
    {
        return null; // 프록시에서 크기 옵션을 직접 사용하지 않음
    }

    public static GUILayoutOption Height(float height) // 기존 GUILayout.Height 호환
    {
        return null; // 프록시에서 크기 옵션을 직접 사용하지 않음
    }

    public static GUILayoutOption MinWidth(float minWidth) // 기존 GUILayout.MinWidth 호환
    {
        return null; // 프록시에서 최소 너비 옵션을 직접 사용하지 않음
    }

    public static GUILayoutOption MaxWidth(float maxWidth) // 기존 GUILayout.MaxWidth 호환
    {
        return null; // 프록시에서 최대 너비 옵션을 직접 사용하지 않음
    }

    public static GUILayoutOption MinHeight(float minHeight) // 기존 GUILayout.MinHeight 호환
    {
        return null; // 프록시에서 최소 높이 옵션을 직접 사용하지 않음
    }

    public static GUILayoutOption MaxHeight(float maxHeight) // 기존 GUILayout.MaxHeight 호환
    {
        return null; // 프록시에서 최대 높이 옵션을 직접 사용하지 않음
    }

    public static GUILayoutOption ExpandWidth(bool expand) // 기존 GUILayout.ExpandWidth 호환
    {
        return null; // 프록시에서 가로 확장 옵션을 직접 사용하지 않음
    }

    public static GUILayoutOption ExpandHeight(bool expand) // 기존 GUILayout.ExpandHeight 호환
    {
        return null; // 프록시에서 세로 확장 옵션을 직접 사용하지 않음
    }

    public static Rect GetRect(float width, float height, params GUILayoutOption[] options) // 기존 GUILayoutUtility.GetRect 크기형 호환
    {
        return new Rect(0f, 0f, width, height); // 임시 좌표 영역 반환
    }

    public static Rect GetRect(float width, float height, GUIStyle style, params GUILayoutOption[] options) // 기존 GUILayoutUtility.GetRect 스타일 크기형 호환
    {
        return new Rect(0f, 0f, width, height); // 임시 좌표 영역 반환
    }

    public static Rect GetLastRect() // 기존 GUILayoutUtility.GetLastRect 호환
    {
        return new Rect(0f, 0f, 100f, 30f); // 마지막 요소 임시 영역 반환
    }

    public static Rect GetRect(GUIContent content, GUIStyle style, params GUILayoutOption[] options) // 기존 GUILayoutUtility.GetRect 내용형 호환
    {
        return new Rect(0f, 0f, 100f, 30f); // 임시 좌표 영역 반환
    }

    public static Rect GetRect(float minWidth, float maxWidth, float minHeight, float maxHeight, GUIStyle style, params GUILayoutOption[] options) // 기존 GUILayoutUtility.GetRect 범위형 호환
    {
        return new Rect(0f, 0f, Mathf.Max(minWidth, maxWidth), Mathf.Max(minHeight, maxHeight)); // 임시 좌표 영역 반환
    }

    private static void PushGroup(EventBattleRuntimeNodeKind kind, string text) // 레이아웃 그룹 추가
    {
        EnsureFrame(); // 프레임 존재 보장
        EventBattleRuntimeNode node = NewNode(kind); // 그룹 노드 생성
        node.Text = text ?? string.Empty; // 그룹 제목 저장
        AddNode(node); // 현재 그룹에 추가
        _groupStack.Push(node); // 새 그룹을 현재 그룹으로 설정
    }

    private static void PopGroup() // 현재 레이아웃 그룹 종료
    {
        if (_groupStack.Count > 1) // 최상위 그룹 보호
        {
            _groupStack.Pop(); // 현재 그룹 종료
        }
    }

    private static void AddTextNode(EventBattleRuntimeNodeKind kind, string text) // 텍스트 계열 노드 추가
    {
        EventBattleRuntimeNode node = NewNode(kind); // 텍스트 노드 생성
        node.Text = text ?? string.Empty; // 표시 문자열 저장
        AddNode(node); // 현재 그룹에 추가
    }

    private static bool AddButton(string text) // 버튼 노드와 재생 입력 처리
    {
        int controlId = NextControlId(); // 버튼 컨트롤 번호 생성
        EventBattleRuntimeNode node = NewNode(EventBattleRuntimeNodeKind.Button); // 버튼 노드 생성
        node.Text = text ?? string.Empty; // 버튼 문자열 저장
        node.ControlId = controlId; // 버튼 번호 저장
        node.Interactable = enabled; // 기존 GUI.enabled 반영
        AddNode(node); // 버튼 노드 추가
        bool clicked = ConsumeInput(controlId, EventBattleRuntimeInputKind.Button); // UGUI 클릭 재생 확인

        if (clicked) // 클릭 입력 확인
        {
            changed = true; // 기존 GUI.changed 갱신
        }

        return clicked; // 기존 GUILayout.Button 반환값 제공
    }

    private static bool AddToggle(bool value, string text) // 토글 노드와 재생 입력 처리
    {
        int controlId = NextControlId(); // 토글 컨트롤 번호 생성
        EventBattleRuntimeNode node = NewNode(EventBattleRuntimeNodeKind.Toggle); // 토글 노드 생성
        node.Text = text ?? string.Empty; // 토글 문자열 저장
        node.ControlId = controlId; // 토글 번호 저장
        node.Interactable = enabled; // 기존 GUI.enabled 반영
        node.BoolValue = value; // 기존 토글 값 저장
        AddNode(node); // 토글 노드 추가

        if (ConsumeInput(controlId, EventBattleRuntimeInputKind.Toggle)) // 토글 입력 재생 확인
        {
            changed = value != _pendingInput.BoolValue; // 값 변경 여부 저장
            return _pendingInput.BoolValue; // 새 토글 값 반환
        }

        return value; // 기존 토글 값 유지
    }

    private static string AddTextInput(EventBattleRuntimeNodeKind kind, string value) // 텍스트 입력 노드와 재생 입력 처리
    {
        int controlId = NextControlId(); // 텍스트 컨트롤 번호 생성
        string safeValue = value ?? string.Empty; // null 문자열 보정
        EventBattleRuntimeNode node = NewNode(kind); // 텍스트 입력 노드 생성
        node.ControlId = controlId; // 텍스트 입력 번호 저장
        node.Interactable = enabled; // 기존 GUI.enabled 반영
        node.StringValue = safeValue; // 기존 입력값 저장
        AddNode(node); // 텍스트 입력 노드 추가

        if (ConsumeInput(controlId, EventBattleRuntimeInputKind.Text)) // 텍스트 입력 재생 확인
        {
            string nextValue = _pendingInput.StringValue ?? string.Empty; // 새 입력값 보정
            changed = !string.Equals(safeValue, nextValue, StringComparison.Ordinal); // 값 변경 여부 저장
            return nextValue; // 새 텍스트 값 반환
        }

        return safeValue; // 기존 텍스트 값 유지
    }

    private static float AddSlider(float value, float minValue, float maxValue) // 슬라이더 노드와 재생 입력 처리
    {
        int controlId = NextControlId(); // 슬라이더 컨트롤 번호 생성
        EventBattleRuntimeNode node = NewNode(EventBattleRuntimeNodeKind.Slider); // 슬라이더 노드 생성
        node.ControlId = controlId; // 슬라이더 번호 저장
        node.Interactable = enabled; // 기존 GUI.enabled 반영
        node.FloatValue = value; // 기존 슬라이더 값 저장
        node.MinValue = minValue; // 슬라이더 최소값 저장
        node.MaxValue = maxValue; // 슬라이더 최대값 저장
        AddNode(node); // 슬라이더 노드 추가

        if (ConsumeInput(controlId, EventBattleRuntimeInputKind.Slider)) // 슬라이더 입력 재생 확인
        {
            float nextValue = Mathf.Clamp(_pendingInput.FloatValue, Mathf.Min(minValue, maxValue), Mathf.Max(minValue, maxValue)); // 새 슬라이더 값 제한
            changed = !Mathf.Approximately(value, nextValue); // 값 변경 여부 저장
            return nextValue; // 새 슬라이더 값 반환
        }

        return value; // 기존 슬라이더 값 유지
    }

    private static int AddSelection(EventBattleRuntimeNodeKind kind, int selected, string[] options, int columns) // 선택 노드와 재생 입력 처리
    {
        int controlId = NextControlId(); // 선택 컨트롤 번호 생성
        string[] safeOptions = options ?? Array.Empty<string>(); // null 선택 목록 보정
        EventBattleRuntimeNode node = NewNode(kind); // 선택 노드 생성
        node.ControlId = controlId; // 선택 번호 저장
        node.Interactable = enabled; // 기존 GUI.enabled 반영
        node.IntValue = selected; // 기존 선택 인덱스 저장
        node.Columns = Mathf.Max(1, columns); // 선택 열 수 저장
        node.Options = safeOptions; // 선택 문자열 저장
        AddNode(node); // 선택 노드 추가

        if (ConsumeInput(controlId, EventBattleRuntimeInputKind.Selection)) // 선택 입력 재생 확인
        {
            int maxIndex = Mathf.Max(-1, safeOptions.Length - 1); // 최대 선택 인덱스 계산
            int nextValue = Mathf.Clamp(_pendingInput.IntValue, -1, maxIndex); // 새 선택 인덱스 제한
            changed = selected != nextValue; // 값 변경 여부 저장
            return nextValue; // 새 선택 인덱스 반환
        }

        return selected; // 기존 선택 인덱스 유지
    }

    private static EventBattleRuntimeNode NewNode(EventBattleRuntimeNodeKind kind) // 공통 UI 노드 생성
    {
        EventBattleRuntimeNode node = new EventBattleRuntimeNode(); // 새 노드 생성
        node.Kind = kind; // 노드 종류 저장
        node.Interactable = enabled; // 현재 입력 가능 상태 저장
        node.TextColor = contentColor * color; // 현재 텍스트 색상 저장
        node.BackgroundColor = backgroundColor * color; // 현재 배경 색상 저장
        return node; // 새 노드 반환
    }

    private static void AddNode(EventBattleRuntimeNode node) // 현재 그룹에 UI 노드 추가
    {
        EnsureFrame(); // 프레임 존재 보장
        _groupStack.Peek().Children.Add(node); // 현재 그룹 자식으로 추가
    }

    private static int NextControlId() // 다음 입력 컨트롤 번호 생성
    {
        int controlId = _nextControlId; // 현재 번호 저장
        _nextControlId++; // 다음 번호 증가
        return controlId; // 생성 번호 반환
    }

    private static bool ConsumeInput(int controlId, EventBattleRuntimeInputKind kind) // 현재 UGUI 입력 재생 여부 확인
    {
        if (_inputConsumed) // 이미 입력을 사용했는지 확인
        {
            return false; // 중복 입력 방지
        }

        if (_pendingInput == null) // 재생 입력 존재 확인
        {
            return false; // 입력 없음 반환
        }

        if (_pendingInput.ControlId != controlId) // 대상 컨트롤 확인
        {
            return false; // 다른 컨트롤 입력 반환
        }

        if (_pendingInput.Kind != kind) // 입력 종류 확인
        {
            return false; // 다른 입력 종류 반환
        }

        if (!enabled) // 기존 GUI.enabled 확인
        {
            return false; // 비활성 컨트롤 입력 차단
        }

        _inputConsumed = true; // 현재 입력 소비 처리
        return true; // 입력 재생 승인
    }

    private static void EnsureFrame() // 프록시 프레임 존재 보장
    {
        if (_frame == null) // 현재 프레임 확인
        {
            BeginFrame(null); // 기본 프레임 생성
        }
    }

    private static string ContentText(GUIContent content) // GUIContent 문자열 변환
    {
        return content != null ? content.text ?? string.Empty : string.Empty; // null 안전 문자열 반환
    }

    private static string[] ContentTexts(GUIContent[] contents) // GUIContent 배열 문자열 변환
    {
        if (contents == null) // 배열 존재 확인
        {
            return Array.Empty<string>(); // 빈 배열 반환
        }

        string[] result = new string[contents.Length]; // 결과 문자열 배열 생성

        for (int index = 0; index < contents.Length; index++) // GUIContent 배열 순회
        {
            result[index] = ContentText(contents[index]); // 각 항목 문자열 변환
        }

        return result; // 변환 문자열 배열 반환
    }

    private static GUISkin CreateFallbackSkin() // OnGUI 외부용 기본 GUI 스킨 생성
    {
        GUISkin fallback = ScriptableObject.CreateInstance<GUISkin>(); // 빈 GUI 스킨 생성
        fallback.box = new GUIStyle(); // 박스 스타일 준비
        fallback.button = new GUIStyle(); // 버튼 스타일 준비
        fallback.toggle = new GUIStyle(); // 토글 스타일 준비
        fallback.label = new GUIStyle(); // 라벨 스타일 준비
        fallback.textField = new GUIStyle(); // 한 줄 입력 스타일 준비
        fallback.textArea = new GUIStyle(); // 여러 줄 입력 스타일 준비
        fallback.window = new GUIStyle(); // 창 스타일 준비
        fallback.horizontalSlider = new GUIStyle(); // 가로 슬라이더 스타일 준비
        fallback.horizontalSliderThumb = new GUIStyle(); // 가로 슬라이더 핸들 스타일 준비
        fallback.verticalSlider = new GUIStyle(); // 세로 슬라이더 스타일 준비
        fallback.verticalSliderThumb = new GUIStyle(); // 세로 슬라이더 핸들 스타일 준비
        fallback.horizontalScrollbar = new GUIStyle(); // 가로 스크롤바 스타일 준비
        fallback.horizontalScrollbarThumb = new GUIStyle(); // 가로 스크롤바 핸들 스타일 준비
        fallback.verticalScrollbar = new GUIStyle(); // 세로 스크롤바 스타일 준비
        fallback.verticalScrollbarThumb = new GUIStyle(); // 세로 스크롤바 핸들 스타일 준비
        fallback.scrollView = new GUIStyle(); // 스크롤 뷰 스타일 준비
        return fallback; // 대체 GUI 스킨 반환
    }

    private static string ClampText(string value, int maxLength) // 텍스트 최대 길이 제한
    {
        string safeValue = value ?? string.Empty; // null 문자열 보정

        if (maxLength < 0 || safeValue.Length <= maxLength) // 길이 제한 필요 여부 확인
        {
            return safeValue; // 기존 문자열 반환
        }

        return safeValue.Substring(0, maxLength); // 최대 길이로 잘라 반환
    }
}
