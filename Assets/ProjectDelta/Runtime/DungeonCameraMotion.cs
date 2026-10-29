using ProjectDelta.Core; // 방 출구 방향 형식
using UnityEngine; // 카메라와 부드러운 위치 보간
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonCameraMotion : MonoBehaviour // 플레이어 추적과 방 전환 화면 이동
    { // 클래스 시작
        private GridMoveController player; // 실제 플레이어 이동 제어
        private Vector3 startOffset; // 도착 화면의 전환 시작 오프셋
        private float elapsed; // 전환 경과 시간
        private float cameraDepth; // 기존 카메라 깊이
        private Camera view; // 화면 범위 계산용 카메라
        private const float TransitionDuration = 0.35f; // 부드러운 방 전환 시간
        public bool IsTransitioning // 현재 방 전환 진행 상태
        { // 속성 시작
            get; // 전환 진행 여부 조회
            private set; // 내부 전환 상태 변경
        } // 속성 끝
        public void Initialize(GridMoveController target) // 실제 플레이어와 카메라 연결
        { // 연결 시작
            player = target; // 플레이어 이동 제어 보관
            cameraDepth = transform.position.z; // 기존 카메라 깊이 보관
            view = GetComponent<Camera>(); // 실제 카메라 구성 조회
            Follow(Vector3.zero); // 최초 플레이어 위치 표시
        } // 연결 끝
        public void BeginTransition(MoveDirection direction) // 도착 방으로 부드러운 화면 이동 시작
        { // 전환 시작
            if (player == null) // 플레이어 존재 검사
            { // 누락 조건 시작
                return; // 대상 없는 전환 차단
            } // 누락 조건 끝
            var directionVector = Direction(direction); // 실제 출구 방향 벡터
            startOffset = -directionVector * 4f; // 두 배 타일 두 칸의 시작 오프셋
            elapsed = 0f; // 전환 경과 시간 초기화
            IsTransitioning = true; // 전환 진행 상태 설정
            player.SetRoomTransitionLocked(true); // 전환 중 플레이어 이동 잠금
            Follow(startOffset); // 도착 방을 이동 시작 위치에 표시
        } // 전환 시작 끝
        private void LateUpdate() // 플레이어 위치와 방 전환 반영
        { // 갱신 시작
            if (player == null) // 플레이어 존재 검사
            { // 누락 조건 시작
                return; // 대상 없는 카메라 이동 차단
            } // 누락 조건 끝
            if (!IsTransitioning) // 방 전환 진행 여부 검사
            { // 일반 추적 시작
                Follow(Vector3.zero); // 실제 플레이어 중심 추적
                return; // 전환 보간 생략
            } // 일반 추적 끝
            elapsed += Time.unscaledDeltaTime; // 화면 전환 경과 시간 누적
            var progress = Mathf.Clamp01(elapsed / TransitionDuration); // 전환 진행 비율
            var eased = Mathf.SmoothStep(0f, 1f, progress); // 양끝이 부드러운 이동 비율
            Follow(Vector3.Lerp(startOffset, Vector3.zero, eased)); // 도착 화면을 플레이어 중심으로 이동
            if (progress >= 1f) // 전환 완료 검사
            { // 완료 조건 시작
                IsTransitioning = false; // 완료한 전환 상태 해제
                player.SetRoomTransitionLocked(false); // 완료 후 플레이어 이동 복구
            } // 완료 조건 끝
        } // 갱신 끝
        private void OnDisable() // 카메라 비활성화 시 입력 잠금 해제
        { // 정리 시작
            if (player != null) // 플레이어 존재 검사
            { // 잠금 복구 시작
                player.SetRoomTransitionLocked(false); // 중단된 전환의 이동 복구
            } // 잠금 복구 끝
            IsTransitioning = false; // 중단된 전환 상태 해제
        } // 정리 끝
        private void Follow(Vector3 offset) // 기존 깊이를 유지하며 플레이어 위치 표시
        { // 표시 시작
            var position = player.transform.position; // 현재 플레이어 월드 위치
            var bounds = player.RoomWorldBounds; // 현재 방의 월드 범위
            if (view != null && view.orthographic && bounds.size.x > 0f && bounds.size.y > 0f) // 직교 카메라와 유효한 방 검사
            { // 화면 범위 제한 시작
                var halfHeight = view.orthographicSize; // 화면의 세로 절반
                var halfWidth = halfHeight * view.aspect; // 화면의 가로 절반
                position.x = bounds.size.x <= halfWidth * 2f ? bounds.center.x : Mathf.Clamp(position.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth); // 방의 가로 범위 내 표시
                position.y = bounds.size.y <= halfHeight * 2f ? bounds.center.y : Mathf.Clamp(position.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight); // 방의 세로 범위 내 표시
            } // 화면 범위 제한 끝
            position += offset; // 방 전환 중 부드러운 화면 이동
            transform.position = new Vector3(position.x, position.y, cameraDepth); // 실제 카메라 화면 위치
        } // 표시 끝
        private static Vector3 Direction(MoveDirection direction) // 출구 방향의 월드 벡터
        { // 방향 변환 시작
            switch (direction) // 실제 출구 방향 검사
            { // 선택 시작
                case MoveDirection.Up: // 위쪽 출구
                    return Vector3.up; // 위쪽 벡터
                case MoveDirection.Down: // 아래쪽 출구
                    return Vector3.down; // 아래쪽 벡터
                case MoveDirection.Left: // 왼쪽 출구
                    return Vector3.left; // 왼쪽 벡터
                case MoveDirection.Right: // 오른쪽 출구
                    return Vector3.right; // 오른쪽 벡터
                default: // 유효하지 않은 출구
                    return Vector3.zero; // 이동 없는 기본 벡터
            } // 선택 끝
        } // 방향 변환 끝
    } // 클래스 끝
} // 이름 공간 끝
