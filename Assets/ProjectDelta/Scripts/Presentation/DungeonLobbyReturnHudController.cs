using ProjectDelta.Application; // 애플리케이션 흐름 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // UGUI 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 143일차: 기존 OnGUI 로비 복귀 버튼을 런타임 UGUI 버튼으로 전환한다.
    public sealed class DungeonLobbyReturnHudController : MonoBehaviour // 던전 로비 복귀 HUD 제어
    {
        private const float ButtonWidth = 110f; // 버튼 가로 크기
        private const float ButtonHeight = 32f; // 버튼 세로 크기

        private GameObject canvasObject; // 전용 HUD Canvas

        private void Awake() // 런타임 HUD 초기화
        {
            BuildRuntimeUi(); // 로비 복귀 UGUI 생성
        }

        private void BuildRuntimeUi() // 로비 복귀 버튼 생성
        {
            if (canvasObject != null) // 기존 Canvas 확인
            {
                return; // 중복 생성 방지
            }

            RuntimeUiFactory.EnsureEventSystem(); // 공용 EventSystem 준비

            canvasObject = new GameObject( // HUD Canvas 오브젝트 생성
                "DungeonLobbyReturnHudCanvas", // Canvas 이름 지정
                typeof(RectTransform), // RectTransform 추가
                typeof(Canvas), // Canvas 추가
                typeof(CanvasScaler), // CanvasScaler 추가
                typeof(GraphicRaycaster)); // GraphicRaycaster 추가

            canvasObject.transform.SetParent( // 컨트롤러 하위 배치
                transform, // 현재 컨트롤러 Transform 사용
                false); // 로컬 Transform 유지

            Canvas canvas = // Canvas 참조 조회
                canvasObject.GetComponent<Canvas>(); // Canvas 컴포넌트 가져오기

            canvas.renderMode = // 렌더 모드 지정
                RenderMode.ScreenSpaceOverlay; // 화면 오버레이 사용

            canvas.sortingOrder = // HUD 표시 순서 지정
                2500; // 탐험 화면 위쪽에 표시

            CanvasScaler scaler = // CanvasScaler 참조 조회
                canvasObject.GetComponent<CanvasScaler>(); // 스케일러 가져오기

            scaler.uiScaleMode = // 스케일 방식 지정
                CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 사용

            UiScaleSettings.Refresh(); // 현재 UI 배율 갱신

            UiScaleSettings.ApplyToCanvasScaler( // 프로젝트 공용 배율 적용
                scaler, // 대상 스케일러 전달
                new Vector2(1920f, 1080f)); // 기준 해상도 전달

            RectTransform buttonRect = // 버튼 RectTransform 생성
                RuntimeUiFactory.CreateUiObject( // 공용 UI 오브젝트 생성
                    "ReturnToLobbyButton", // 버튼 오브젝트 이름
                    canvasObject.transform); // Canvas 하위 배치

            buttonRect.anchorMin = // 우측 상단 기준 설정
                new Vector2(1f, 1f); // 우측 상단 앵커 최소값

            buttonRect.anchorMax = // 우측 상단 기준 설정
                new Vector2(1f, 1f); // 우측 상단 앵커 최대값

            buttonRect.pivot = // 우측 상단 피벗 설정
                new Vector2(1f, 1f); // 우측 상단 피벗 사용

            buttonRect.anchoredPosition = // 기존 OnGUI 위치 대응
                new Vector2(-12f, -12f); // 우측 12, 상단 12 여백

            buttonRect.sizeDelta = // 버튼 크기 지정
                new Vector2(ButtonWidth, ButtonHeight); // 기존 버튼 크기 유지

            Image buttonImage = // 버튼 배경 Image 추가
                buttonRect.gameObject.AddComponent<Image>(); // Image 컴포넌트 추가

            buttonImage.color = // 버튼 배경색 지정
                new Color(0.2f, 0.2f, 0.26f, 0.96f); // 기존 정식 UI 계열 색상 사용

            Button button = // Button 컴포넌트 추가
                buttonRect.gameObject.AddComponent<Button>(); // 클릭 버튼 생성

            button.targetGraphic = // 버튼 대상 그래픽 지정
                buttonImage; // 배경 Image 사용

            button.onClick.AddListener( // 클릭 이벤트 연결
                ReturnToLobby); // 기존 로비 복귀 흐름 호출

            RectTransform labelRect = // 버튼 라벨 영역 생성
                RuntimeUiFactory.CreateStretchedRect( // 버튼 전체 영역 사용
                    "Label", // 라벨 이름 지정
                    buttonRect); // 버튼 하위 배치

            Text label = // 라벨 Text 추가
                labelRect.gameObject.AddComponent<Text>(); // Text 컴포넌트 생성

            RuntimeUiFactory.ConfigureText( // 공용 텍스트 설정
                label, // 대상 Text 전달
                "로비로", // 기존 버튼 문구 유지
                14, // 기존 글자 크기 유지
                FontStyle.Normal, // 일반 글꼴 사용
                TextAnchor.MiddleCenter); // 중앙 정렬 사용

            label.raycastTarget = // 라벨 입력 차단 여부 지정
                false; // 버튼 클릭 방해 방지
        }

        private void ReturnToLobby() // 로비 복귀 버튼 처리
        {
            ApplicationFlow.Current?.ReturnToLobby(); // 기존 애플리케이션 복귀 흐름 유지
        }
    }
}
