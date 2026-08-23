using System.Collections.Generic; // 통로 목록 기능 사용
using ProjectDelta.Domain; // 통로 항목 데이터 형식 사용
using UnityEngine; // ScriptableObject 기능 사용

namespace ProjectDelta.Data // 데이터 네임스페이스
{
    // 방 하나의 정적 설계도 (기획서 10.2절 정적 정의 데이터 "RoomDefinition | 방 프리팹과 연결 규칙").
    // 어떤 문/벽이 어디에 있는지만 담고, 문이 열렸는지 같은 회차 중 상태는 담지 않는다.
    // 그 런타임 상태는 ProjectDelta.Domain.RoomInstance가 담당한다.
    [CreateAssetMenu(fileName = "RoomDefinition", menuName = "ProjectDelta/Data/Room Definition")]
    public sealed class RoomDefinition : DefinitionBase // 방 정의 데이터
    {
        [SerializeField] private int width = 5; // 방 가로 칸 수
        [SerializeField] private int height = 5; // 방 세로 칸 수
        [SerializeField] private List<PassageEntry> passages = new List<PassageEntry>(); // 방향별 통로 배치 목록

        public int Width => width; // 방 가로 칸 수 공개
        public int Height => height; // 방 세로 칸 수 공개
        public IReadOnlyList<PassageEntry> Passages => passages; // 통로 배치 목록 공개

        // 29일차: 방 범위(가로/세로 칸 수 기준, 홀수 크기 전제)를 벗어나는 칸을 계산하기 위한 경계값.
        // 기존 여러 곳에서 하드코딩되어 있던 -2~2 범위를 실제 데이터 기반으로 구하기 위해 추가했다.
        public int MinX => -(width / 2); // 방 최소 X 좌표
        public int MaxX => width / 2; // 방 최대 X 좌표
        public int MinZ => -(height / 2); // 방 최소 Z 좌표
        public int MaxZ => height / 2; // 방 최대 Z 좌표

        // 29일차: 문 통로 중 이웃 칸이 방 범위 밖으로 나가는 것만 "던전 생성이 쓸 수 있는 경계 출구"로 취급한다.
        // 방 내부에서 다른 칸으로만 이어지는 문(예: TestRoom_A의 (0,0) 북쪽 문)은 여기서 제외된다.
        public IEnumerable<PassageEntry> GetExits() // 방 경계 출구 목록 계산
        {
            foreach (PassageEntry entry in passages) // 통로 항목 전체 반복
            {
                if (entry.Type != PassageType.Door) // 문 종류인지 확인
                {
                    continue; // 문이 아니면 출구 후보에서 제외
                }

                GridPosition delta = GridMovement.GetDirectionDelta(entry.Direction); // 방향 변화량 계산
                int neighborX = entry.X + delta.X; // 이웃 칸 X 좌표 계산
                int neighborZ = entry.Z + delta.Z; // 이웃 칸 Z 좌표 계산
                bool isOutsideRoom = neighborX < MinX || neighborX > MaxX || neighborZ < MinZ || neighborZ > MaxZ; // 방 범위 밖 여부 확인

                if (isOutsideRoom) // 방 범위 밖으로 나가는 문인지 확인
                {
                    yield return entry; // 경계 출구로 반환
                }
            }
        }

        // 29일차: 던전 생성기(Domain.DungeonGenerator)가 쓸 수 있는 최소 정보로 변환한다.
        // Domain은 Data(RoomDefinition)를 직접 참조하지 않는다는 기존 원칙에 따라, 변환은 항상 이렇게
        // Data 쪽에서 Domain 쪽으로 이루어진다 (RoomInstance.Create가 PassageEntry만 받는 것과 같은 방향).
        public RoomTemplate ToRoomTemplate() // 던전 생성기용 방 종류 요약 생성
        {
            List<CardinalDirection> exitDirections = new List<CardinalDirection>(); // 출구 방향 목록 준비

            foreach (PassageEntry exit in GetExits()) // 경계 출구 전체 반복
            {
                exitDirections.Add(exit.Direction); // 출구 방향 추가
            }

            return new RoomTemplate(Id, exitDirections); // 완성된 방 종류 요약 반환
        }
    }
}
