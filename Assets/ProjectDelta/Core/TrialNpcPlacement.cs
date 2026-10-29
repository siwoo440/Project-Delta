using System; // 기존 방 배열의 독립 복사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class TrialNpcPlacement // 발생 조건 미확정의 시험 상인 방 선택
    { // 배치 클래스 시작
        public static bool TrySelect(DungeonLayout layout, int seed, SavedFloorRoomContent contents, DungeonRoom[] encounters, DungeonRoom eventRoom, out DungeonRoom selected) // 기존 콘텐츠를 피한 결정적 상인 방 선택
        { // 선택 시작
            selected = null; // 기본 선택 실패
            if (layout == null || contents == null || contents.chests == null) // 층 구조와 방 콘텐츠 검사
            { // 콘텐츠 오류 시작
                return false; // 누락된 콘텐츠와 겹침 차단
            } // 콘텐츠 오류 끝
            if (eventRoom != null && layout.GetRoom(eventRoom.X, eventRoom.Y) == null) // 지정 이벤트 방의 실제 층 존재 검사
            { // 이벤트 오류 시작
                return false; // 다른 층의 이벤트 방 차단
            } // 이벤트 오류 끝
            var count = encounters == null ? 0 : encounters.Length; // 기존 조우 제외 방 수
            var excluded = new DungeonRoom[count + (eventRoom == null ? 0 : 1)]; // 조우와 이벤트의 독립 제외 목록
            if (encounters != null) // 실제 조우 제외 목록 검사
            { // 조우 복사 시작
                Array.Copy(encounters, excluded, count); // 기존 조우 방 좌표 복사
            } // 조우 복사 끝
            if (eventRoom != null) // 배치된 이벤트 방 검사
            { // 이벤트 제외 시작
                excluded[count] = eventRoom; // 시험 이벤트 방 좌표 제외
            } // 이벤트 제외 끝
            return TrialEventPlacement.TrySelect(layout, seed, contents, excluded, out selected); // 기존 상자와 함정 및 조우를 피한 결정적 시험 배치
        } // 선택 끝
    } // 배치 클래스 끝
} // 이름 공간 끝
