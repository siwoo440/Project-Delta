using ProjectDelta.Domain; // 도메인 방향 규칙 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // Input System 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public sealed class PlayerLookController : MonoBehaviour // 플레이어 자유 시점 제어
    {
        [SerializeField] private InputActionAsset inputActions; // 프로젝트 입력 액션 에셋
        [SerializeField] private Transform cameraTransform; // 상하 시점 카메라 Transform
        [SerializeField] private float mouseSensitivity = 0.08f; // 마우스 회전 감도
        [SerializeField] private float minPitch = -80f; // 최대 위쪽 시점
        [SerializeField] private float maxPitch = 80f; // 최대 아래쪽 시점

        private InputActionMap explorationMap; // 탐험 입력 맵
        private InputAction lookAction; // 마우스 시점 액션
        private float yaw; // 현재 수평 회전값
        private float pitch; // 현재 수직 회전값
        private bool isUiRequestingFreeCursor; // 25일차: 상자 패널 등 마우스가 필요한 UI가 열려있는 동안 커서 해제 요청
        private bool isAltHeld; // 26일차: Alt 키를 누르고 있는 동안 커서 해제 요청

        // 둘 중 하나라도 커서를 요구하면 커서를 풀어둔다 (26일차: 서로 독립적으로 추적해서,
        // 예를 들어 상자 패널이 열린 채로 Alt를 뗐다고 커서가 다시 잠기는 일이 없게 한다).
        private bool ShouldFreeCursor => isUiRequestingFreeCursor || isAltHeld;

        public float YawDegrees => transform.eulerAngles.y; // 현재 수평 시점 각도 공개
        public float PitchDegrees => pitch; // 현재 수직 시점 각도 공개
        public CardinalDirection FacingDirection => GridMovement.GetFacingFromYaw(YawDegrees); // 현재 4방향 시선 공개

        private void Awake() // 초기 시점 상태 구성
        {
            if (cameraTransform == null) // 카메라 미지정 확인
            {
                Camera mainCamera = Camera.main; // 메인 카메라 검색
                cameraTransform = mainCamera != null ? mainCamera.transform : null; // 카메라 Transform 자동 연결
            }

            yaw = transform.eulerAngles.y; // Player 수평 각도 저장

            if (cameraTransform != null) // 카메라 존재 확인
            {
                Vector3 cameraAngles = cameraTransform.localEulerAngles; // 카메라 로컬 각도 읽기
                pitch = NormalizeSignedAngle(cameraAngles.x); // 수직 각도 부호 범위 변환
                yaw += NormalizeSignedAngle(cameraAngles.y); // 기존 카메라 수평 각도 Player에 합산
                transform.rotation = Quaternion.Euler(0f, yaw, 0f); // 수평 회전을 Player에 적용
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f); // 수직 회전만 카메라에 유지
            }
        }

        private void OnEnable() // 자유 시점 입력 활성화
        {
            if (inputActions == null) // 입력 에셋 미지정 확인
            {
                Debug.LogError("[Project Delta] PlayerLookController에 Input Actions가 지정되지 않았습니다.", this); // 입력 에셋 오류 출력
                return; // 입력 연결 중단
            }

            explorationMap = inputActions.FindActionMap("Exploration", true); // 탐험 입력 맵 검색
            lookAction = explorationMap.FindAction("Look", true); // 자유 시점 액션 검색
            explorationMap.Enable(); // 탐험 입력 맵 활성화
            LockCursor(); // 마우스 커서 잠금
        }

        // 25일차: 상자 패널처럼 마우스로 클릭해야 하는 UI가 열릴 때 호출한다.
        // isFree가 true면 커서를 풀어 클릭 가능하게 하고 시점 회전을 멈춘다.
        public void SetCursorFreeForUi(bool isFree)
        {
            isUiRequestingFreeCursor = isFree; // UI 쪽 커서 해제 요청 상태 갱신
            RefreshCursorState(); // 최종 커서 상태 반영
        }

        private void Update() // 매 프레임 Alt 입력 확인 및 시점 회전
        {
            UpdateAltHeld(); // 26일차: Alt를 누르고 있는 동안 커서 해제

            if (ShouldFreeCursor) // 커서가 필요한 상태인지 확인
            {
                return; // 커서가 필요한 동안은 시점 회전 생략
            }

            if (lookAction == null || cameraTransform == null) // 입력 또는 카메라 누락 확인
            {
                return; // 시점 처리 중단
            }

            Vector2 lookDelta = lookAction.ReadValue<Vector2>(); // 현재 마우스 이동량 읽기

            if (lookDelta.sqrMagnitude <= 0f) // 마우스 이동 여부 확인
            {
                return; // 회전 처리 생략
            }

            yaw += lookDelta.x * mouseSensitivity; // 좌우 마우스 이동을 Yaw에 반영
            pitch -= lookDelta.y * mouseSensitivity; // 상하 마우스 이동을 Pitch에 반영
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch); // 상하 시점 범위 제한
            transform.rotation = Quaternion.Euler(0f, yaw, 0f); // Player 수평 회전 적용
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f); // Camera 수직 회전 적용
        }

        private void OnDisable() // 자유 시점 입력 해제
        {
            UnlockCursor(); // 마우스 커서 잠금 해제
        }

        // 26일차: 좌우 Alt 키 중 하나라도 눌려있으면 커서를 해제한다.
        private void UpdateAltHeld() // Alt 키 상태 확인 및 커서 상태 갱신
        {
            if (Keyboard.current == null) // 키보드 장치 확인
            {
                return; // 조회 불가, 처리 중단
            }

            bool altPressed = Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed; // 좌우 Alt 키 상태 확인

            if (altPressed == isAltHeld) // 이전 프레임과 상태 변화 여부 확인
            {
                return; // 변화 없음, 처리 생략
            }

            isAltHeld = altPressed; // Alt 보유 상태 갱신
            RefreshCursorState(); // 최종 커서 상태 반영
        }

        private void RefreshCursorState() // UI 요청·Alt 보유 상태를 합쳐 최종 커서 상태 적용
        {
            if (ShouldFreeCursor) // 커서가 필요한 상태인지 확인
            {
                UnlockCursor(); // 커서 잠금 해제 및 표시
            }
            else // 둘 다 커서를 요구하지 않는 경우
            {
                LockCursor(); // 커서 화면 중앙 고정 및 숨김
            }
        }

        private static float NormalizeSignedAngle(float angle) // 0~360 각도를 부호 각도로 변환
        {
            if (angle > 180f) // 180도 초과 확인
            {
                angle -= 360f; // 음수 각도로 변환
            }

            return angle; // 변환 각도 반환
        }

        private static void LockCursor() // 게임용 커서 잠금
        {
            Cursor.lockState = CursorLockMode.Locked; // 커서 화면 중앙 고정
            Cursor.visible = false; // 커서 숨김
        }

        private static void UnlockCursor() // 에디터용 커서 해제
        {
            Cursor.lockState = CursorLockMode.None; // 커서 잠금 해제
            Cursor.visible = true; // 커서 표시
        }
    }
}
