using System.Collections.Generic; // 목록 기능 사용

namespace ProjectDelta.Domain // 도메인 네임스페이스
{
    // 29일차: 던전 생성기가 방을 고를 때 필요한 최소 정보 - "이 방 종류는 어느 방향에
    // 경계 출구를 갖고 있는가"만 담는다. 방 내부 모양(칸 크기, 문 배치)의 실제 데이터는
    // Data.RoomDefinition이 갖고 있지만, Domain은 Data를 직접 참조하지 않는다는 기존 원칙
    // (RoomInstance.Create가 PassageEntry만 받는 것과 같은 이유)에 따라, Data 쪽에서
    // RoomDefinition.ToRoomTemplate()로 변환해서 넘겨준다.
    public sealed class RoomTemplate // 던전 생성기용 방 종류 요약
    {
        public string DefinitionId { get; } // 원본 RoomDefinition의 Id
        public IReadOnlyList<CardinalDirection> ExitDirections { get; } // 방 경계에 있는 출구 방향 목록

        public RoomTemplate(string definitionId, IReadOnlyList<CardinalDirection> exitDirections) // 방 종류 요약 생성자
        {
            DefinitionId = definitionId; // 정의 식별자 저장
            ExitDirections = exitDirections; // 출구 방향 목록 저장
        }
    }
}
