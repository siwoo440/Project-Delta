using System.Collections.Generic; // 목록 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 던전 생성기가 방을 고를 때 필요한 최소 정보.
    // 30일차부터 경계 출구의 방향뿐 아니라 실제 방 내부 좌표도 보존한다.
    public sealed class RoomTemplate // 던전 생성기용 방 종류 요약
    {
        private readonly List<RoomExit> exits; // 실제 출구 목록
        private readonly List<CardinalDirection> exitDirections; // 기존 코드 호환용 방향 목록

        public string DefinitionId { get; } // 원본 RoomDefinition의 Id
        public IReadOnlyList<RoomExit> Exits => exits; // 좌표와 방향을 포함한 경계 출구 목록

        // 29일차 코드와 다른 호출부가 바로 깨지지 않도록 방향 목록도 계속 제공한다.
        // 신규 던전 생성 코드는 Exits를 사용한다.
        public IReadOnlyList<CardinalDirection> ExitDirections => exitDirections; // 기존 방향 목록 호환 속성

        public RoomTemplate(string definitionId, IReadOnlyList<RoomExit> roomExits) // 좌표를 포함한 방 종류 요약 생성자
        {
            DefinitionId = definitionId; // 정의 식별자 저장
            exits = new List<RoomExit>(); // 내부 출구 목록 생성
            exitDirections = new List<CardinalDirection>(); // 호환용 방향 목록 생성

            if (roomExits == null) // 전달된 출구 목록 존재 확인
            {
                return; // 출구가 없으면 빈 목록 유지
            }

            for (int i = 0; i < roomExits.Count; i++) // 출구 목록 전체 복사
            {
                RoomExit exit = roomExits[i]; // 현재 출구 조회
                exits.Add(exit); // 좌표 포함 출구 저장
                exitDirections.Add(exit.Direction); // 호환용 방향 저장
            }
        }

        // 기존 테스트나 이전 코드에서 방향만 넘기는 생성자를 계속 사용할 수 있게 남겨둔다.
        // 좌표가 없는 기존 호출은 원점 좌표로 변환되며, 실제 RoomDefinition 변환은 위 생성자를 사용한다.
        public RoomTemplate(string definitionId, IReadOnlyList<CardinalDirection> directions) // 기존 방향 전용 생성자
        {
            DefinitionId = definitionId; // 정의 식별자 저장
            exits = new List<RoomExit>(); // 내부 출구 목록 생성
            exitDirections = new List<CardinalDirection>(); // 호환용 방향 목록 생성

            if (directions == null) // 전달된 방향 목록 존재 확인
            {
                return; // 출구가 없으면 빈 목록 유지
            }

            for (int i = 0; i < directions.Count; i++) // 방향 목록 전체 변환
            {
                CardinalDirection direction = directions[i]; // 현재 방향 조회
                exits.Add(new RoomExit(GridPosition.Zero, direction)); // 좌표가 없는 기존 호출은 원점 출구로 변환
                exitDirections.Add(direction); // 기존 방향 목록 유지
            }
        }
    }
}
