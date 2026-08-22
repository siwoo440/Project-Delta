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

        private void Update() // 매 프레임 시점 회전
        {
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
