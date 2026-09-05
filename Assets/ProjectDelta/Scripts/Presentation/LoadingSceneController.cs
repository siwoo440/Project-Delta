using ProjectDelta.Application; // ApplicationFlow.Current 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // UGUI 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 143일차: 임시 로딩 화면 OnGUI를 런타임 UGUI로 전환한다.
    public sealed class LoadingSceneController : MonoBehaviour // 로딩 화면 임시 진행 제어
    {
        private GameObject canvasObject; // 로딩 화면 Canvas

        private void Awake() // 로딩 화면 초기화
        {
            BuildRuntimeUi(); // 로딩 UGUI 생성
        }

        private void BuildRuntimeUi() // 로딩 화면 UGUI 생성
        {
            if (canvasObject != null) // 기존 Canvas 확인
            {
                return; // 중복 생성 방지
            }

            RuntimeUiFactory.EnsureEventSystem(); // 공용 EventSystem 준비

            Transform canvasTransform = // 공용 전체 화면 Canvas 생성
                RuntimeUiFactory.BuildScreenCanvas( // 프로젝트 공용 Canvas 빌더 사용
                    transform, // 현재 컨트롤러 하위 생성
                    "LoadingRuntimeCanvas", // Canvas 오브젝트 이름
                    string.Empty); // 별도 타이틀 미사용

            canvasObject = canvasTransform.gameObject; // Canvas 오브젝트 참조 저장

            Canvas canvas = canvasObject.GetComponent<Canvas>(); // Canvas 참조 조회
            canvas.sortingOrder = 5000; // 로딩 화면 최상위 표시

            RectTransform labelRect = // 로딩 안내 영역 생성
                RuntimeUiFactory.CreateUiObject( // 공용 UI 오브젝트 생성
                    "LoadingLabel", // 안내 오브젝트 이름
                    canvasTransform); // Canvas 하위 배치

            labelRect.anchorMin = new Vector2(0.5f, 0.5f); // 화면 중앙 앵커 설정
            labelRect.anchorMax = new Vector2(0.5f, 0.5f); // 화면 중앙 앵커 설정
            labelRect.pivot = new Vector2(0.5f, 0.5f); // 중앙 피벗 설정
            labelRect.anchoredPosition = new Vector2(0f, 108f); // 기존 40% 높이 대응
            labelRect.sizeDelta = new Vector2(400f, 60f); // 기존 라벨 크기 유지

            Text loadingLabel = labelRect.gameObject.AddComponent<Text>(); // 로딩 안내 Text 추가
            RuntimeUiFactory.ConfigureText(loadingLabel, "로딩 중...", 28, FontStyle.Normal, TextAnchor.MiddleCenter); // 기존 안내 스타일 적용
            loadingLabel.raycastTarget = false; // 버튼 입력 방해 방지

            Text buttonLabel; // 버튼 라벨 참조 선언

            Button continueButton = // 임시 계속 버튼 생성
                RuntimeUiFactory.CreateCenteredButton( // 공용 중앙 버튼 생성
                    canvasTransform, // Canvas 하위 배치
                    "ContinueButton", // 버튼 오브젝트 이름
                    new Vector2(0f, -54f), // 기존 55% 높이 대응
                    new Vector2(220f, 50f), // 기존 버튼 크기 유지
                    "계속 (임시)", // 기존 버튼 문구 유지
                    20, // 기존 글자 크기 유지
                    ProceedFromLoadingScreen, // 기존 씬 전환 흐름 연결
                    out buttonLabel); // 라벨 참조 반환

            continueButton.interactable = true; // 임시 계속 버튼 활성화
            buttonLabel.raycastTarget = false; // 라벨 클릭 방해 방지
        }

        private void ProceedFromLoadingScreen() // 임시 계속 버튼 처리
        {
            ApplicationFlow.Current?.ProceedFromLoadingScreen(); // 기존 예정 목적지 씬 이동 유지
        }
    }
}
