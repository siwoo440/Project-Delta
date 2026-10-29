using System; // 버튼 동작 전달
using UnityEngine; // 화면 위치와 객체
using UnityEngine.InputSystem; // 마우스 입력
using UnityEngine.UI; // 버튼 배경 표시
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class MapPointerButton : MonoBehaviour // 지도 화면용 마우스 버튼
    { // 클래스 시작
        private RectTransform rect; // 버튼 화면 영역
        private Action action; // 클릭 시 실행할 동작
        private int createdFrame; // 버튼 생성 프레임
        private InputAction click; // 마우스 클릭 감지 동작
        public static MapPointerButton Create(Transform parent, string name, string label, Vector2 position, Action onClick) // 지도 버튼 생성
        { // 생성 시작
            var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(MapPointerButton)); // 버튼 화면 객체
            button.transform.SetParent(parent, false); // 지정 화면에 배치
            var view = button.GetComponent<MapPointerButton>(); // 버튼 입력 제어
            view.rect = button.GetComponent<RectTransform>(); // 버튼 영역 연결
            view.rect.anchoredPosition = position; // 버튼 화면 위치
            view.rect.sizeDelta = new Vector2(240f, 70f); // 버튼 표시 크기
            view.action = onClick; // 실제 클릭 동작 연결
            view.createdFrame = Time.frameCount; // 생성 프레임의 중복 클릭 차단
            view.click = new InputAction(name + "Click", InputActionType.Button, "<Mouse>/leftButton"); // 개별 마우스 클릭 감지
            view.click.performed += view.OnClick; // 입력 이벤트 연결
            view.click.Enable(); // 마우스 클릭 감지 시작
            var image = button.GetComponent<Image>(); // 버튼 배경 이미지
            image.color = new Color(0.12f, 0.32f, 0.34f, 0.96f); // 선명한 버튼 배경
            image.raycastTarget = false; // 다른 UI 입력 가림 방지
            UiFactory.CreateText(button.transform, "Label", label, 25, Vector2.zero, view.rect.sizeDelta, Color.white); // 버튼 이름 표시
            return view; // 완성한 버튼 전달
        } // 생성 끝
        private void OnClick(InputAction.CallbackContext context) // 마우스 클릭 위치 검사
        { // 클릭 검사 시작
            var mouse = context.control.device as Mouse; // 실제 클릭한 마우스 장치
            if (Time.frameCount <= createdFrame || mouse == null) // 생성 프레임과 장치 검사
            { // 입력 제외 조건 시작
                return; // 열린 화면의 중복 클릭 차단
            } // 입력 제외 조건 끝
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, mouse.position.ReadValue(), null)) // 버튼 안쪽 클릭 검사
            { // 클릭 조건 시작
                action?.Invoke(); // 지도 열기 또는 닫기 실행
            } // 클릭 조건 끝
        } // 클릭 검사 끝
        private void OnDestroy() // 마우스 입력 정리
        { // 정리 시작
            if (click != null) // 입력 동작 존재 검사
            { // 정리 조건 시작
                click.performed -= OnClick; // 클릭 이벤트 연결 해제
                click.Dispose(); // 입력 동작과 리소스 정리
            } // 정리 조건 끝
        } // 정리 끝
    } // 클래스 끝
} // 이름 공간 끝
