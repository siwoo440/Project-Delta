using System; // UGUI 입력 콜백
using UnityEngine; // Unity 기본 기능
using UnityEngine.EventSystems; // EventSystem 구성
using UnityEngine.UI; // UGUI 구성 요소

public sealed class EventBattleRuntimeView : MonoBehaviour // 이벤트 전투 런타임 UGUI 화면
{
    private GameObject _canvasObject; // 전용 캔버스 오브젝트
    private GameObject _screenRoot; // 전체 화면 루트
    private RectTransform _contentRoot; // 동적 내용 루트
    private Font _font; // 기본 런타임 글꼴

    public Action<int> ButtonPressed; // 버튼 입력 전달
    public Action<int, bool> ToggleChanged; // 토글 입력 전달
    public Action<int, string> TextChanged; // 텍스트 입력 전달
    public Action<int, float> SliderChanged; // 슬라이더 입력 전달
    public Action<int, int> SelectionChanged; // 선택 입력 전달

    public bool IsInitialized => _screenRoot != null; // 화면 초기화 여부
    public bool IsVisible => _screenRoot != null && _screenRoot.activeSelf; // 화면 표시 여부

    public void Initialize() // 전용 UGUI 화면 생성
    {
        if (_screenRoot != null) // 기존 화면 존재 확인
        {
            return; // 중복 생성 방지
        }

        EnsureEventSystem(); // UI 입력 시스템 준비
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 기본 글꼴 로드
        _canvasObject = new GameObject("EventBattleRuntimeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 전용 캔버스 생성
        DontDestroyOnLoad(_canvasObject); // 장면 전환 시 캔버스 유지
        Canvas canvas = _canvasObject.GetComponent<Canvas>(); // 캔버스 참조 획득
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 오버레이 모드 설정
        canvas.sortingOrder = 5000; // 다른 런타임 HUD보다 앞에 표시
        CanvasScaler scaler = _canvasObject.GetComponent<CanvasScaler>(); // 캔버스 스케일러 참조
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 비례 스케일 설정
        scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 해상도 설정
        scaler.matchWidthOrHeight = 0.5f; // 가로세로 중간 보정 설정
        _screenRoot = CreatePanel(_canvasObject.transform, "EventBattleScreen", new Color(0.025f, 0.035f, 0.055f, 0.985f)); // 전체 화면 배경 생성
        Stretch(_screenRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 전체 화면 크기 적용
        BuildHeader(_screenRoot.transform); // 상단 제목 영역 생성
        BuildScrollBody(_screenRoot.transform); // 본문 스크롤 영역 생성
        _screenRoot.SetActive(false); // 초기 화면 숨김
    }

    public void Show(EventBattleRuntimeFrame frame) // 프레임 표시
    {
        if (_screenRoot == null) // 초기화 상태 확인
        {
            Initialize(); // 화면 자동 초기화
        }

        _screenRoot.SetActive(true); // 이벤트 전투 화면 표시
        Render(frame); // 현재 프레임 렌더링
    }

    public void Hide() // 화면 숨김
    {
        if (_screenRoot == null) // 화면 존재 확인
        {
            return; // 미생성 상태 종료
        }

        _screenRoot.SetActive(false); // 이벤트 전투 화면 숨김
    }

    public void Render(EventBattleRuntimeFrame frame) // UI 트리 전체 재생성
    {
        if (_contentRoot == null) // 본문 루트 존재 확인
        {
            Initialize(); // 화면 자동 초기화
        }

        ClearChildren(_contentRoot); // 이전 동적 UI 제거

        if (frame == null || frame.Root == null) // 유효 프레임 확인
        {
            CreateMessage(_contentRoot, "표시할 이벤트 전투 UI가 없습니다."); // 빈 프레임 안내 표시
            return; // 렌더링 종료
        }

        for (int index = 0; index < frame.Root.Children.Count; index++) // 최상위 UI 요소 순회
        {
            RenderNode(_contentRoot, frame.Root.Children[index]); // 각 UI 노드 생성
        }

        Canvas.ForceUpdateCanvases(); // 새 레이아웃 즉시 계산
    }

    private void BuildHeader(Transform parent) // 화면 상단 제목 영역 생성
    {
        GameObject header = CreatePanel(parent, "Header", new Color(0.075f, 0.095f, 0.14f, 1f)); // 제목 패널 생성
        RectTransform rect = header.GetComponent<RectTransform>(); // 제목 패널 좌표 획득
        Stretch(rect, new Vector2(0f, 0.9f), new Vector2(1f, 1f), new Vector2(24f, 12f), new Vector2(-24f, -12f)); // 제목 패널 배치
        Text title = CreateText(header.transform, "Title", "이벤트 전투", 34, TextAnchor.MiddleLeft); // 화면 제목 생성
        Stretch(title.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-24f, 0f)); // 제목 텍스트 배치
    }

    private void BuildScrollBody(Transform parent) // 전체 본문 스크롤 영역 생성
    {
        GameObject viewport = CreatePanel(parent, "Viewport", new Color(0f, 0f, 0f, 0f)); // 본문 표시 영역 생성
        RectTransform viewportRect = viewport.GetComponent<RectTransform>(); // 본문 표시 좌표 획득
        Stretch(viewportRect, new Vector2(0f, 0f), new Vector2(1f, 0.9f), new Vector2(28f, 28f), new Vector2(-28f, -8f)); // 본문 표시 영역 배치
        viewport.AddComponent<RectMask2D>(); // 화면 밖 내용 마스크 적용
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); // 스크롤 내용 루트 생성
        content.transform.SetParent(viewport.transform, false); // 표시 영역 자식 연결
        _contentRoot = content.GetComponent<RectTransform>(); // 내용 루트 좌표 저장
        _contentRoot.anchorMin = new Vector2(0f, 1f); // 상단 왼쪽 기준 설정
        _contentRoot.anchorMax = new Vector2(1f, 1f); // 상단 오른쪽 기준 설정
        _contentRoot.pivot = new Vector2(0.5f, 1f); // 상단 중심 기준점 설정
        _contentRoot.anchoredPosition = Vector2.zero; // 시작 위치 초기화
        _contentRoot.sizeDelta = Vector2.zero; // 자동 높이 계산 준비
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>(); // 세로 레이아웃 참조 획득
        layout.padding = new RectOffset(8, 8, 8, 24); // 전체 내용 여백 설정
        layout.spacing = 10f; // 최상위 요소 간격 설정
        layout.childControlWidth = true; // 자식 너비 자동 관리
        layout.childControlHeight = true; // 자식 높이 자동 관리
        layout.childForceExpandWidth = true; // 가로 폭 전체 사용
        layout.childForceExpandHeight = false; // 세로 강제 확장 해제
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>(); // 내용 크기 자동 조절 참조
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 내용 높이 자동 계산
        ScrollRect scroll = viewport.AddComponent<ScrollRect>(); // 본문 스크롤 기능 추가
        scroll.viewport = viewportRect; // 스크롤 표시 영역 지정
        scroll.content = _contentRoot; // 스크롤 내용 영역 지정
        scroll.horizontal = false; // 가로 스크롤 비활성화
        scroll.vertical = true; // 세로 스크롤 활성화
        scroll.movementType = ScrollRect.MovementType.Clamped; // 범위 밖 이동 제한
        scroll.scrollSensitivity = 32f; // 마우스 휠 감도 설정
    }

    private void RenderNode(Transform parent, EventBattleRuntimeNode node) // 노드 종류별 UGUI 생성
    {
        if (node == null) // 빈 노드 확인
        {
            return; // 빈 노드 건너뛰기
        }

        switch (node.Kind) // 노드 종류 분기
        {
            case EventBattleRuntimeNodeKind.Vertical: // 세로 그룹 처리
            case EventBattleRuntimeNodeKind.Area: // 영역 그룹 처리
            case EventBattleRuntimeNodeKind.Scroll: // 내부 스크롤 그룹 처리
                RenderVerticalGroup(parent, node); // 세로 그룹 생성
                break; // 그룹 처리 종료
            case EventBattleRuntimeNodeKind.Horizontal: // 가로 그룹 처리
                RenderHorizontalGroup(parent, node); // 가로 그룹 생성
                break; // 그룹 처리 종료
            case EventBattleRuntimeNodeKind.Label: // 텍스트 처리
                RenderLabel(parent, node); // 텍스트 생성
                break; // 텍스트 처리 종료
            case EventBattleRuntimeNodeKind.Box: // 박스 처리
                RenderBox(parent, node); // 박스 생성
                break; // 박스 처리 종료
            case EventBattleRuntimeNodeKind.Button: // 버튼 처리
                RenderButton(parent, node); // 버튼 생성
                break; // 버튼 처리 종료
            case EventBattleRuntimeNodeKind.Toggle: // 토글 처리
                RenderToggle(parent, node); // 토글 생성
                break; // 토글 처리 종료
            case EventBattleRuntimeNodeKind.TextField: // 한 줄 입력 처리
                RenderInputField(parent, node, false); // 한 줄 입력 생성
                break; // 한 줄 입력 처리 종료
            case EventBattleRuntimeNodeKind.TextArea: // 여러 줄 입력 처리
                RenderInputField(parent, node, true); // 여러 줄 입력 생성
                break; // 여러 줄 입력 처리 종료
            case EventBattleRuntimeNodeKind.Slider: // 슬라이더 처리
                RenderSlider(parent, node); // 슬라이더 생성
                break; // 슬라이더 처리 종료
            case EventBattleRuntimeNodeKind.SelectionGrid: // 선택 격자 처리
            case EventBattleRuntimeNodeKind.Toolbar: // 툴바 처리
                RenderSelection(parent, node); // 선택 버튼 묶음 생성
                break; // 선택 처리 종료
            case EventBattleRuntimeNodeKind.Space: // 고정 여백 처리
                RenderSpace(parent, node.SpaceSize, false); // 고정 여백 생성
                break; // 고정 여백 처리 종료
            case EventBattleRuntimeNodeKind.FlexibleSpace: // 가변 여백 처리
                RenderSpace(parent, 18f, true); // 가변 여백 생성
                break; // 가변 여백 처리 종료
        }
    }

    private void RenderVerticalGroup(Transform parent, EventBattleRuntimeNode node) // 세로 그룹 UGUI 생성
    {
        GameObject group = CreatePanel(parent, "VerticalGroup", new Color(0.055f, 0.07f, 0.1f, 0.92f)); // 세로 그룹 패널 생성
        VerticalLayoutGroup layout = group.AddComponent<VerticalLayoutGroup>(); // 세로 레이아웃 추가
        layout.padding = new RectOffset(12, 12, 12, 12); // 그룹 안쪽 여백 설정
        layout.spacing = 8f; // 자식 간격 설정
        layout.childControlWidth = true; // 자식 너비 자동 관리
        layout.childControlHeight = true; // 자식 높이 자동 관리
        layout.childForceExpandWidth = true; // 자식 가로 폭 확장
        layout.childForceExpandHeight = false; // 자식 세로 강제 확장 해제
        ContentSizeFitter fitter = group.AddComponent<ContentSizeFitter>(); // 그룹 높이 자동 계산 추가
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 선호 높이 자동 적용
        AddOptionalGroupTitle(group.transform, node.Text); // 그룹 제목 표시

        for (int index = 0; index < node.Children.Count; index++) // 그룹 자식 순회
        {
            RenderNode(group.transform, node.Children[index]); // 자식 UI 생성
        }
    }

    private void RenderHorizontalGroup(Transform parent, EventBattleRuntimeNode node) // 가로 그룹 UGUI 생성
    {
        GameObject group = CreatePanel(parent, "HorizontalGroup", new Color(0.045f, 0.06f, 0.085f, 0.9f)); // 가로 그룹 패널 생성
        HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>(); // 가로 레이아웃 추가
        layout.padding = new RectOffset(12, 12, 10, 10); // 그룹 안쪽 여백 설정
        layout.spacing = 8f; // 자식 간격 설정
        layout.childControlWidth = true; // 자식 너비 자동 관리
        layout.childControlHeight = true; // 자식 높이 자동 관리
        layout.childForceExpandWidth = true; // 남은 폭 균등 분배
        layout.childForceExpandHeight = false; // 세로 강제 확장 해제
        ContentSizeFitter fitter = group.AddComponent<ContentSizeFitter>(); // 그룹 높이 자동 계산 추가
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 선호 높이 자동 적용

        if (!string.IsNullOrEmpty(node.Text)) // 그룹 제목 존재 확인
        {
            Text title = CreateText(group.transform, "GroupTitle", node.Text, 20, TextAnchor.MiddleLeft); // 그룹 제목 생성
            AddPreferredHeight(title.gameObject, 44f); // 제목 높이 설정
        }

        for (int index = 0; index < node.Children.Count; index++) // 그룹 자식 순회
        {
            RenderNode(group.transform, node.Children[index]); // 자식 UI 생성
        }
    }

    private void RenderLabel(Transform parent, EventBattleRuntimeNode node) // 일반 텍스트 UGUI 생성
    {
        Text text = CreateText(parent, "Label", node.Text, 22, TextAnchor.MiddleLeft); // 텍스트 오브젝트 생성
        text.color = ResolveTextColor(node.TextColor); // 기존 GUI 텍스트 색상 반영
        text.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 문자열 줄바꿈 허용
        text.verticalOverflow = VerticalWrapMode.Overflow; // 세로 내용 전체 표시
        AddPreferredHeight(text.gameObject, EstimateTextHeight(node.Text, 34f)); // 텍스트 예상 높이 적용
    }

    private void RenderBox(Transform parent, EventBattleRuntimeNode node) // 박스 UGUI 생성
    {
        GameObject box = CreatePanel(parent, "Box", ResolveBackgroundColor(node.BackgroundColor, new Color(0.11f, 0.13f, 0.18f, 1f))); // 박스 배경 생성
        VerticalLayoutGroup layout = box.AddComponent<VerticalLayoutGroup>(); // 박스 세로 레이아웃 추가
        layout.padding = new RectOffset(14, 14, 10, 10); // 박스 안쪽 여백 설정
        Text text = CreateText(box.transform, "BoxText", node.Text, 21, TextAnchor.MiddleCenter); // 박스 텍스트 생성
        text.color = ResolveTextColor(node.TextColor); // 기존 GUI 텍스트 색상 반영
        AddPreferredHeight(text.gameObject, EstimateTextHeight(node.Text, 42f)); // 박스 텍스트 높이 설정
        ContentSizeFitter fitter = box.AddComponent<ContentSizeFitter>(); // 박스 높이 자동 계산 추가
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; // 박스 선호 높이 적용
    }

    private void RenderButton(Transform parent, EventBattleRuntimeNode node) // 버튼 UGUI 생성
    {
        Button button = CreateButton(parent, "Button", node.Text); // 기본 버튼 생성
        button.interactable = node.Interactable; // 기존 GUI.enabled 반영
        Image image = button.GetComponent<Image>(); // 버튼 배경 이미지 참조
        image.color = ResolveBackgroundColor(node.BackgroundColor, new Color(0.16f, 0.22f, 0.34f, 1f)); // 기존 GUI 배경색 반영
        Text label = button.GetComponentInChildren<Text>(); // 버튼 텍스트 참조
        label.color = ResolveTextColor(node.TextColor); // 기존 GUI 텍스트 색상 반영
        int controlId = node.ControlId; // 람다용 컨트롤 번호 복사
        button.onClick.AddListener(() => ButtonPressed?.Invoke(controlId)); // 원본 OnGUI 버튼 분기 재생 연결
        AddPreferredHeight(button.gameObject, 54f); // 버튼 높이 설정
    }

    private void RenderToggle(Transform parent, EventBattleRuntimeNode node) // 토글 UGUI 생성
    {
        GameObject root = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle), typeof(HorizontalLayoutGroup)); // 토글 루트 생성
        root.transform.SetParent(parent, false); // 토글 부모 연결
        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>(); // 토글 가로 레이아웃 참조
        layout.spacing = 10f; // 체크박스와 텍스트 간격 설정
        layout.childAlignment = TextAnchor.MiddleLeft; // 토글 내용 왼쪽 정렬
        layout.childControlWidth = true; // 자식 너비 자동 관리
        layout.childControlHeight = true; // 자식 높이 자동 관리
        layout.childForceExpandWidth = false; // 자식 가로 강제 확장 해제
        GameObject background = CreatePanel(root.transform, "Background", new Color(0.14f, 0.17f, 0.23f, 1f)); // 체크박스 배경 생성
        AddPreferredSize(background, 34f, 34f); // 체크박스 크기 설정
        GameObject checkmark = CreatePanel(background.transform, "Checkmark", new Color(0.45f, 0.75f, 1f, 1f)); // 체크 표시 생성
        Stretch(checkmark.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(7f, 7f), new Vector2(-7f, -7f)); // 체크 표시 안쪽 배치
        Text label = CreateText(root.transform, "Label", node.Text, 21, TextAnchor.MiddleLeft); // 토글 텍스트 생성
        label.color = ResolveTextColor(node.TextColor); // 기존 GUI 텍스트 색상 반영
        LayoutElement labelLayout = label.gameObject.AddComponent<LayoutElement>(); // 토글 텍스트 레이아웃 추가
        labelLayout.flexibleWidth = 1f; // 남은 가로 폭 사용
        Toggle toggle = root.GetComponent<Toggle>(); // 토글 컴포넌트 참조
        toggle.targetGraphic = background.GetComponent<Image>(); // 체크박스 배경 연결
        toggle.graphic = checkmark.GetComponent<Image>(); // 체크 표시 연결
        toggle.isOn = node.BoolValue; // 기존 토글 값 반영
        toggle.interactable = node.Interactable; // 기존 GUI.enabled 반영
        int controlId = node.ControlId; // 람다용 컨트롤 번호 복사
        toggle.onValueChanged.AddListener(value => ToggleChanged?.Invoke(controlId, value)); // 원본 토글 반환값 재생 연결
        AddPreferredHeight(root, 48f); // 토글 전체 높이 설정
    }

    private void RenderInputField(Transform parent, EventBattleRuntimeNode node, bool multiline) // 텍스트 입력 UGUI 생성
    {
        GameObject root = CreatePanel(parent, multiline ? "TextArea" : "TextField", new Color(0.075f, 0.09f, 0.13f, 1f)); // 입력 배경 생성
        InputField input = root.AddComponent<InputField>(); // 입력 필드 컴포넌트 추가
        Text text = CreateText(root.transform, "Text", node.StringValue, 20, TextAnchor.UpperLeft); // 입력 텍스트 생성
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 8f), new Vector2(-12f, -8f)); // 입력 텍스트 배치
        input.textComponent = text; // 입력 텍스트 연결
        input.text = node.StringValue ?? string.Empty; // 기존 입력값 반영
        input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine; // 한 줄 또는 여러 줄 모드 설정
        input.interactable = node.Interactable; // 기존 GUI.enabled 반영
        int controlId = node.ControlId; // 람다용 컨트롤 번호 복사
        input.onEndEdit.AddListener(value => TextChanged?.Invoke(controlId, value)); // 편집 완료 시 원본 반환값 재생 연결
        AddPreferredHeight(root, multiline ? 120f : 52f); // 입력 필드 높이 설정
    }

    private void RenderSlider(Transform parent, EventBattleRuntimeNode node) // 슬라이더 UGUI 생성
    {
        GameObject root = new GameObject("Slider", typeof(RectTransform), typeof(Slider)); // 슬라이더 루트 생성
        root.transform.SetParent(parent, false); // 슬라이더 부모 연결
        Slider slider = root.GetComponent<Slider>(); // 슬라이더 컴포넌트 참조
        GameObject background = CreatePanel(root.transform, "Background", new Color(0.12f, 0.14f, 0.19f, 1f)); // 슬라이더 배경 생성
        Stretch(background.GetComponent<RectTransform>(), new Vector2(0f, 0.35f), new Vector2(1f, 0.65f), new Vector2(10f, 0f), new Vector2(-10f, 0f)); // 슬라이더 배경 배치
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform)); // 슬라이더 채움 영역 생성
        fillArea.transform.SetParent(root.transform, false); // 채움 영역 부모 연결
        Stretch(fillArea.GetComponent<RectTransform>(), new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(10f, 0f), new Vector2(-18f, 0f)); // 채움 영역 배치
        GameObject fill = CreatePanel(fillArea.transform, "Fill", new Color(0.3f, 0.65f, 0.95f, 1f)); // 슬라이더 채움 이미지 생성
        Stretch(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); // 채움 이미지 배치
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform)); // 슬라이더 핸들 영역 생성
        handleArea.transform.SetParent(root.transform, false); // 핸들 영역 부모 연결
        Stretch(handleArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f)); // 핸들 영역 배치
        GameObject handle = CreatePanel(handleArea.transform, "Handle", new Color(0.85f, 0.9f, 1f, 1f)); // 슬라이더 핸들 생성
        RectTransform handleRect = handle.GetComponent<RectTransform>(); // 핸들 좌표 참조
        handleRect.sizeDelta = new Vector2(24f, 36f); // 핸들 크기 설정
        slider.fillRect = fill.GetComponent<RectTransform>(); // 슬라이더 채움 연결
        slider.handleRect = handleRect; // 슬라이더 핸들 연결
        slider.targetGraphic = handle.GetComponent<Image>(); // 슬라이더 선택 그래픽 연결
        slider.minValue = Mathf.Min(node.MinValue, node.MaxValue); // 최소값 설정
        slider.maxValue = Mathf.Max(node.MinValue, node.MaxValue); // 최대값 설정
        slider.value = node.FloatValue; // 기존 슬라이더 값 반영
        slider.interactable = node.Interactable; // 기존 GUI.enabled 반영
        int controlId = node.ControlId; // 람다용 컨트롤 번호 복사
        slider.onValueChanged.AddListener(value => SliderChanged?.Invoke(controlId, value)); // 슬라이더 값 변경 재생 연결
        AddPreferredHeight(root, 52f); // 슬라이더 높이 설정
    }

    private void RenderSelection(Transform parent, EventBattleRuntimeNode node) // 선택 격자 또는 툴바 UGUI 생성
    {
        GameObject grid = new GameObject("Selection", typeof(RectTransform), typeof(GridLayoutGroup)); // 선택 격자 루트 생성
        grid.transform.SetParent(parent, false); // 선택 격자 부모 연결
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>(); // 격자 레이아웃 참조
        int columns = Mathf.Max(1, node.Columns); // 실제 열 수 계산
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; // 고정 열 수 모드 설정
        layout.constraintCount = columns; // 열 수 적용
        layout.cellSize = new Vector2(220f, 50f); // 선택 버튼 기본 크기 설정
        layout.spacing = new Vector2(8f, 8f); // 선택 버튼 간격 설정
        layout.childAlignment = TextAnchor.UpperLeft; // 선택 버튼 왼쪽 위 정렬
        int optionCount = node.Options != null ? node.Options.Length : 0; // 선택 항목 수 계산

        for (int index = 0; index < optionCount; index++) // 선택 항목 순회
        {
            string label = node.Options[index] ?? string.Empty; // 현재 선택 문자열 읽기
            Button button = CreateButton(grid.transform, "Option_" + index, label); // 선택 버튼 생성
            button.interactable = node.Interactable; // 기존 GUI.enabled 반영
            Image image = button.GetComponent<Image>(); // 선택 버튼 배경 참조
            image.color = index == node.IntValue ? new Color(0.2f, 0.48f, 0.75f, 1f) : new Color(0.13f, 0.18f, 0.27f, 1f); // 현재 선택 상태 색상 표시
            int controlId = node.ControlId; // 람다용 컨트롤 번호 복사
            int selectedIndex = index; // 람다용 선택 인덱스 복사
            button.onClick.AddListener(() => SelectionChanged?.Invoke(controlId, selectedIndex)); // 원본 선택 반환값 재생 연결
        }

        int rows = Mathf.Max(1, Mathf.CeilToInt(optionCount / (float)columns)); // 필요한 행 수 계산
        AddPreferredHeight(grid, rows * 58f); // 격자 전체 높이 설정
    }

    private void RenderSpace(Transform parent, float size, bool flexible) // 여백 UGUI 생성
    {
        GameObject space = new GameObject(flexible ? "FlexibleSpace" : "Space", typeof(RectTransform), typeof(LayoutElement)); // 여백 오브젝트 생성
        space.transform.SetParent(parent, false); // 여백 부모 연결
        LayoutElement layout = space.GetComponent<LayoutElement>(); // 여백 레이아웃 참조
        layout.preferredHeight = Mathf.Max(1f, size); // 기본 여백 높이 설정
        layout.flexibleHeight = flexible ? 1f : 0f; // 가변 여백 확장 설정
    }

    private void AddOptionalGroupTitle(Transform parent, string title) // 그룹 제목 선택 생성
    {
        if (string.IsNullOrEmpty(title)) // 제목 존재 확인
        {
            return; // 제목 없음 종료
        }

        Text text = CreateText(parent, "GroupTitle", title, 23, TextAnchor.MiddleLeft); // 그룹 제목 텍스트 생성
        text.fontStyle = FontStyle.Bold; // 그룹 제목 굵게 표시
        AddPreferredHeight(text.gameObject, 40f); // 그룹 제목 높이 설정
    }

    private void CreateMessage(Transform parent, string message) // 상태 안내 텍스트 생성
    {
        Text text = CreateText(parent, "Message", message, 24, TextAnchor.MiddleCenter); // 안내 텍스트 생성
        AddPreferredHeight(text.gameObject, 80f); // 안내 텍스트 높이 설정
    }

    private GameObject CreatePanel(Transform parent, string objectName, Color panelColor) // 공통 패널 생성
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(Image)); // 패널 오브젝트 생성
        panel.transform.SetParent(parent, false); // 패널 부모 연결
        Image image = panel.GetComponent<Image>(); // 패널 이미지 참조
        image.color = panelColor; // 패널 색상 설정
        return panel; // 완성 패널 반환
    }

    private Text CreateText(Transform parent, string objectName, string value, int fontSize, TextAnchor alignment) // 공통 텍스트 생성
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text)); // 텍스트 오브젝트 생성
        textObject.transform.SetParent(parent, false); // 텍스트 부모 연결
        Text text = textObject.GetComponent<Text>(); // 텍스트 컴포넌트 참조
        text.font = _font; // 런타임 기본 글꼴 설정
        text.text = value ?? string.Empty; // 표시 문자열 설정
        text.fontSize = fontSize; // 글자 크기 설정
        text.alignment = alignment; // 텍스트 정렬 설정
        text.color = Color.white; // 기본 글자색 설정
        text.raycastTarget = false; // 불필요한 입력 차단
        return text; // 완성 텍스트 반환
    }

    private Button CreateButton(Transform parent, string objectName, string label) // 공통 버튼 생성
    {
        GameObject buttonObject = CreatePanel(parent, objectName, new Color(0.16f, 0.22f, 0.34f, 1f)); // 버튼 배경 생성
        Button button = buttonObject.AddComponent<Button>(); // 버튼 컴포넌트 추가
        button.targetGraphic = buttonObject.GetComponent<Image>(); // 버튼 그래픽 연결
        Text text = CreateText(buttonObject.transform, "Label", label, 21, TextAnchor.MiddleCenter); // 버튼 문자열 생성
        Stretch(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, -4f)); // 버튼 문자열 배치
        return button; // 완성 버튼 반환
    }

    private void EnsureEventSystem() // EventSystem 존재 보장
    {
        EventSystem existing = UnityEngine.Object.FindFirstObjectByType<EventSystem>(); // 기존 EventSystem 검색

        if (existing != null) // 기존 EventSystem 확인
        {
            return; // 중복 생성 방지
        }

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule)); // 기본 EventSystem 생성
        DontDestroyOnLoad(eventSystem); // 장면 전환 시 입력 시스템 유지
    }

    private void ClearChildren(Transform parent) // 동적 UI 자식 제거
    {
        if (parent == null) // 부모 존재 확인
        {
            return; // 빈 부모 종료
        }

        for (int index = parent.childCount - 1; index >= 0; index--) // 자식 역순 순회
        {
            Destroy(parent.GetChild(index).gameObject); // 기존 UI 오브젝트 제거
        }
    }

    private void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax) // RectTransform 전체 배치 보조
    {
        rect.anchorMin = anchorMin; // 최소 앵커 설정
        rect.anchorMax = anchorMax; // 최대 앵커 설정
        rect.offsetMin = offsetMin; // 최소 오프셋 설정
        rect.offsetMax = offsetMax; // 최대 오프셋 설정
    }

    private void AddPreferredHeight(GameObject target, float height) // 레이아웃 선호 높이 추가
    {
        LayoutElement layout = target.GetComponent<LayoutElement>(); // 기존 레이아웃 요소 확인

        if (layout == null) // 기존 요소 없음 확인
        {
            layout = target.AddComponent<LayoutElement>(); // 새 레이아웃 요소 추가
        }

        layout.preferredHeight = height; // 선호 높이 설정
    }

    private void AddPreferredSize(GameObject target, float width, float height) // 레이아웃 선호 크기 추가
    {
        LayoutElement layout = target.GetComponent<LayoutElement>(); // 기존 레이아웃 요소 확인

        if (layout == null) // 기존 요소 없음 확인
        {
            layout = target.AddComponent<LayoutElement>(); // 새 레이아웃 요소 추가
        }

        layout.preferredWidth = width; // 선호 너비 설정
        layout.preferredHeight = height; // 선호 높이 설정
        layout.minWidth = width; // 최소 너비 설정
        layout.minHeight = height; // 최소 높이 설정
    }

    private float EstimateTextHeight(string text, float minimum) // 문자열 줄 수 기반 예상 높이 계산
    {
        if (string.IsNullOrEmpty(text)) // 빈 문자열 확인
        {
            return minimum; // 최소 높이 반환
        }

        int lineCount = 1; // 기본 한 줄 설정

        for (int index = 0; index < text.Length; index++) // 문자열 문자 순회
        {
            if (text[index] == '\n') // 줄바꿈 문자 확인
            {
                lineCount++; // 표시 줄 수 증가
            }
        }

        return Mathf.Max(minimum, lineCount * 30f + 10f); // 예상 높이 반환
    }

    private Color ResolveTextColor(Color source) // 기존 GUI 글자색 보정
    {
        if (source.a <= 0.01f) // 완전 투명색 확인
        {
            return Color.white; // 기본 글자색 반환
        }

        return source; // 기존 글자색 반환
    }

    private Color ResolveBackgroundColor(Color source, Color fallback) // 기존 GUI 배경색 보정
    {
        bool defaultWhite = Mathf.Approximately(source.r, 1f) && Mathf.Approximately(source.g, 1f) && Mathf.Approximately(source.b, 1f) && Mathf.Approximately(source.a, 1f); // 기본 흰색 여부 확인

        if (defaultWhite || source.a <= 0.01f) // 별도 배경색 지정 여부 확인
        {
            return fallback; // 런타임 기본 배경색 반환
        }

        return source; // 기존 배경색 반환
    }
}
