using ProjectDelta.Domain; // 도메인 층 상태·이동 규칙 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 계단으로 다음 층에 내려갈 때 새 방을 만들어 플레이어를 옮긴다.
    // TODO: 지금은 정해진 방 프리팹 중 하나를 그대로 갖다 놓는 자리표시자다.
    // "계단을 중심으로 한 절차적 생성"과 "계단이 반드시 도달 가능한 위치에 있도록 보장"하는
    // 실제 생성 알고리즘은 26~35일차 던전 생성 구간에서 만든다.
    public sealed class DungeonFloorController : MonoBehaviour // 층 전환 제어
    {
        [SerializeField] private RoomView[] nextFloorRoomPrefabs; // 다음 층 자리표시자로 쓸 방 프리팹 목록
        [SerializeField] private Vector3 floorOrigin = new Vector3(0f, 0f, 200f); // 첫 자리표시자 층의 월드 원점
        [SerializeField] private Vector3 floorSpacing = new Vector3(200f, 0f, 0f); // 층마다 서로 겹치지 않도록 띄우는 간격

        private DungeonRunState dungeonState; // 층 번호를 보유한 런타임 상태 (실제 런이 없으면 테스트용 로컬 상태)
        private RoomView spawnedRoomView; // 이 컨트롤러가 만든 직전 자리표시자 방 (원래 씬의 테스트 방은 여기 포함하지 않음)

        private void Awake() // 층 상태 연결
        {
            GetDungeonState(); // 이 시점에 미리 준비 (다른 스크립트가 Awake 순서와 무관하게 먼저 호출할 수도 있어 아래 GetDungeonState()가 실제로는 항상 안전함)
        }

        // 27일차: 같은 Player 오브젝트의 다른 컴포넌트가 이 컨트롤러보다 먼저 Awake()를 실행해서
        // EnsureCurrentFloorRoomExists()를 호출하더라도 안전하도록, 여기서 지연 초기화한다.
        private DungeonRunState GetDungeonState()
        {
            if (dungeonState == null) // 아직 초기화되지 않았는지 확인
            {
                // 20~21일차와 같은 패턴: 실제 런이 있으면 그 던전 상태를, 없으면(테스트 씬) 로컬 상태를 사용한다.
                dungeonState = RunContext.Current != null ? RunContext.Current.Dungeon : new DungeonRunState();
            }

            return dungeonState; // 준비된 던전 상태 반환
        }

        // 계단 상호작용이 성공했을 때 호출한다. 다음 층 자리표시자 방을 만들고 플레이어를 그 안에 옮긴다.
        public bool TryDescend(PlayerGridMovementController movementController)
        {
            if (movementController == null || nextFloorRoomPrefabs == null || nextFloorRoomPrefabs.Length == 0) // 필요한 참조 확인
            {
                Debug.LogWarning("[Project Delta] 다음 층 방 프리팹이 지정되지 않아 계단 이동을 처리할 수 없습니다.", this); // 설정 누락 경고 출력
                return false; // 층 이동 불가 반환
            }

            GetDungeonState().AdvanceFloor(); // 층 번호 증가 (되돌아가는 방향은 없음, 기획서 3.1절)
            RoomView newRoomView = SpawnRoomForCurrentFloor(); // 새 층 번호에 맞는 자리표시자 방 생성

            movementController.EnterRoom(newRoomView, GridPosition.Zero, CardinalDirection.North); // 새 방 원점으로 정식 진입 절차 실행

            Debug.Log($"[Project Delta] 계단 이동: {GetDungeonState().CurrentFloor}층 / {newRoomView.name}", this); // 층 이동 결과 출력
            return true; // 층 이동 성공 반환
        }

        // 27일차: 이어하기로 CurrentFloor가 이미 1보다 큰 상태로 씬이 로드될 때 호출한다.
        // 계단을 실제로 밟지 않아도, 저장된 층 번호에 맞는 자리표시자 방을 미리 만들어둔다.
        // PlayerGridMovementController가 RoomId로 방을 찾기 전에 반드시 이 메서드부터 호출해야 한다.
        public void EnsureCurrentFloorRoomExists()
        {
            if (GetDungeonState().CurrentFloor <= 1) // 1층이면(씬 기본 시작 방) 처리 불필요 확인
            {
                return; // 준비할 자리표시자 방 없음
            }

            if (spawnedRoomView != null) // 이미 만들어둔 자리표시자 방이 있는지 확인
            {
                return; // 중복 생성 방지
            }

            if (nextFloorRoomPrefabs == null || nextFloorRoomPrefabs.Length == 0) // 자리표시자 방 프리팹 존재 확인
            {
                Debug.LogWarning("[Project Delta] 다음 층 방 프리팹이 지정되지 않아 이어하기 대상 층을 준비할 수 없습니다.", this); // 설정 누락 경고 출력
                return; // 생성 불가
            }

            SpawnRoomForCurrentFloor(); // 현재 층 번호에 맞는 자리표시자 방 생성
        }

        // 현재 층 번호에 맞는 자리표시자 방을 생성하고, 이전 방이 있었다면 정리한다.
        private RoomView SpawnRoomForCurrentFloor() // 층 번호 기반 자리표시자 방 생성
        {
            int floor = GetDungeonState().CurrentFloor; // 현재 층 번호 조회
            RoomView prefab = nextFloorRoomPrefabs[(floor - 1) % nextFloorRoomPrefabs.Length]; // 자리표시자 방 순환 선택
            Vector3 spawnPosition = floorOrigin + (floorSpacing * (floor - 1)); // 층별 스폰 위치 계산 (기존 층과 겹치지 않게)
            RoomView newRoomView = Instantiate(prefab, spawnPosition, Quaternion.identity); // 자리표시자 방 생성

            if (spawnedRoomView != null) // 이전에 만든 자리표시자 방 존재 확인
            {
                Destroy(spawnedRoomView.gameObject); // 이전 자리표시자 방 정리 (씬 원본 방은 건드리지 않음)
            }

            spawnedRoomView = newRoomView; // 새로 만든 자리표시자 방 기록
            return newRoomView; // 생성된 방 반환
        }
    }
}
