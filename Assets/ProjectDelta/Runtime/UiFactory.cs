using UnityEngine; // 화면 구성 도구
using UnityEngine.UI; // 기본 UI 도구
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public static class UiFactory // 공통 화면 요소 생성
    { // 클래스 시작
        private static Font screenFont; // 공통 한글 글꼴
        public static Canvas CreateCanvas(string name) // 기준 화면 생성
        { // 생성 시작
            var canvasObject = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); // 기본 화면 구성
            var canvas = canvasObject.GetComponent<Canvas>(); // 화면 구성 조회
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 고정 표시
            var scaler = canvasObject.GetComponent<CanvasScaler>(); // 해상도 조절 조회
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 해상도 비례 조절
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 기준 화면 크기
            scaler.matchWidthOrHeight = 0.5f; // 가로 세로 균형 조절
            return canvas; // 구성된 화면 전달
        } // 생성 끝
        public static Text CreateText(Transform parent, string name, string content, int size, Vector2 position, Vector2 bounds, Color color) // 공통 글자 생성
        { // 생성 시작
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text)); // 글자 객체 생성
            textObject.transform.SetParent(parent, false); // 지정 화면에 배치
            var text = textObject.GetComponent<Text>(); // 글자 구성 조회
            if (screenFont == null) // 공통 글꼴 준비 검사
            { // 글꼴 준비 시작
                var fontNames = new[] // 실행 환경 글꼴 후보
                { // 글꼴 목록 시작
                    "Malgun Gothic", // Windows 기본 한글 글꼴
                    "Arial" // 기본 영문 대체 글꼴
                }; // 글꼴 목록 끝
                screenFont = Font.CreateDynamicFontFromOSFont(fontNames, 32); // Windows 기본 글꼴 선택
            } // 글꼴 준비 끝
            text.font = screenFont; // 실행 환경 글꼴 적용
            text.text = content; // 표시 내용 적용
            text.fontSize = size; // 글자 크기 적용
            text.alignment = TextAnchor.MiddleCenter; // 글자 중앙 정렬
            text.color = color; // 글자 색상 적용
            text.raycastTarget = false; // 글자의 버튼 입력 가림 방지
            text.rectTransform.anchoredPosition = position; // 화면 내 위치 지정
            text.rectTransform.sizeDelta = bounds; // 글자 표시 영역 지정
            return text; // 생성한 글자 전달
        } // 생성 끝
        public static Button CreateButton(Transform parent, string name, string label, Vector2 position) // 공통 선택 버튼 생성
        { // 생성 시작
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)); // 버튼 객체 생성
            buttonObject.transform.SetParent(parent, false); // 지정 화면에 배치
            var rect = buttonObject.GetComponent<RectTransform>(); // 버튼 표시 영역 조회
            rect.anchoredPosition = position; // 버튼 중앙 위치
            rect.sizeDelta = new Vector2(380f, 84f); // 버튼 표시 크기
            var image = buttonObject.GetComponent<Image>(); // 버튼 배경 조회
            image.color = new Color(0.12f, 0.32f, 0.34f); // 기본 배경 색상
            var button = buttonObject.GetComponent<Button>(); // 버튼 동작 조회
            button.targetGraphic = image; // 입력 반응 배경 연결
            var colors = button.colors; // 버튼 상태 색상 조회
            colors.highlightedColor = new Color(0.65f, 0.95f, 0.88f); // 마우스 강조 색상
            colors.selectedColor = new Color(0.65f, 0.95f, 0.88f); // 키보드 선택 색상
            colors.pressedColor = new Color(0.5f, 0.75f, 0.7f); // 누른 상태 색상
            button.colors = colors; // 버튼 반응 색상 적용
            CreateText(buttonObject.transform, "Label", label, 30, Vector2.zero, rect.sizeDelta, Color.white); // 버튼 한글 표시
            return button; // 생성한 버튼 전달
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
