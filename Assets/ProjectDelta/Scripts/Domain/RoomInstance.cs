using System; // 직렬화 기능 사용
using System.Collections.Generic; // 통로 목록 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    [Serializable] // RoomDefinition에서 목록으로 직렬화하기 위한 표시
    public struct PassageEntry // 방 정의가 갖는 통로 한 칸 데이터
    {
        public int X; // 통로 기준 칸 X 좌표
        public int Z; // 통로 기준 칸 Z 좌표
        public CardinalDirection Direction; // 통로 방향
        public PassageType Type; // 통로 종류 (Open/Wall/Door)
        public bool IsLocked; // 문일 때 잠김 여부
    }

    public sealed class RoomInstance // 층에 실제로 배치된 방 하나의 런타임 표현
    {
        public string RoomId { get; } // 이 방 인스턴스의 식별자
        public string DefinitionId { get; } // 원본 RoomDefinition의 Id
        public RoomGridLayout Layout { get; } // 이 방의 통로 상태

        private RoomInstance(string roomId, string definitionId, RoomGridLayout layout) // 방 인스턴스 생성자
        {
            RoomId = roomId; // 방 식별자 저장
            DefinitionId = definitionId; // 정의 식별자 저장
            Layout = layout; // 통로 데이터 저장
        }

        // RoomDefinition의 정적 통로 목록으로부터 실제 방 인스턴스를 만든다.
        // Domain은 Data(RoomDefinition)를 직접 참조하지 않고, 순수 데이터 목록만 받는다.
        public static RoomInstance Create(string roomId, string definitionId, IEnumerable<PassageEntry> passages) // 정의 데이터로 방 인스턴스 생성
        {
            RoomGridLayout layout = new RoomGridLayout(); // 새 통로 데이터 생성

            if (passages != null) // 통로 목록 존재 확인
            {
                foreach (PassageEntry entry in passages) // 정의된 통로 항목 반복
                {
                    GridPassage passage = CreatePassage(entry); // 통로 항목을 실제 통로 객체로 변환
                    layout.SetPassage(new GridPosition(entry.X, entry.Z), entry.Direction, passage); // 통로 데이터 등록
                }
            }

            return new RoomInstance(roomId, definitionId, layout); // 완성된 방 인스턴스 반환
        }

        private static GridPassage CreatePassage(PassageEntry entry) // 통로 항목을 GridPassage로 변환
        {
            switch (entry.Type) // 통로 종류 분기
            {
                case PassageType.Wall: // 벽 처리
                    return GridPassage.CreateWall(); // 벽 통로 반환
                case PassageType.Door: // 문 처리
                    return GridPassage.CreateDoor(entry.IsLocked); // 문 통로 반환 (잠김 여부 포함)
                default: // 일반 통로 처리
                    return GridPassage.CreateOpen(); // 일반 통로 반환
            }
        }
    }
}
