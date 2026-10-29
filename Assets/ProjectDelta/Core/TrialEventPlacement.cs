using System; // 좌표 해시와 정수 비교
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class TrialEventPlacement // 발생 확률 미확정의 시험 이벤트 방 선택
    { // 배치 클래스 시작
        public static bool TrySelect(DungeonLayout layout, int seed, SavedFloorRoomContent contents, DungeonRoom[] encounters, out DungeonRoom selected) // 상자와 함정 및 기존 조우를 제외한 시험 배치
        { // 선택 시작
            selected = null; // 기본 선택 실패
            if (layout == null || contents == null || contents.chests == null) // 실제 층과 저장 콘텐츠 검사
            { // 데이터 오류 시작
                return false; // 누락된 콘텐츠의 겹침 차단
            } // 데이터 오류 끝
            var count = encounters == null ? 0 : encounters.Length; // 기존 조우 제외 방 수
            var excluded = new DungeonRoom[count + contents.chests.Length + 1]; // 조우와 상자 및 함정 제외 목록
            if (encounters != null) // 실제 조우 제외 목록 검사
            { // 조우 복사 시작
                Array.Copy(encounters, excluded, encounters.Length); // 기존 조우의 좌표 보존
            } // 조우 복사 끝
            for (var index = 0; index < contents.chests.Length; index++) // 실제 상자 방 순회
            { // 상자 제외 시작
                var chest = contents.chests[index]; // 저장된 방별 상자
                if (chest == null || chest.chest == null || layout.GetRoom(chest.x, chest.y) == null) // 상자 데이터와 실제 방 검사
                { // 상자 오류 시작
                    return false; // 누락된 상자 정보 차단
                } // 상자 오류 끝
                excluded[count + index] = new DungeonRoom(chest.x, chest.y); // 상자 방 좌표 제외
            } // 상자 제외 끝
            if (layout.GetRoom(contents.trapX, contents.trapY) == null) // 실제 함정 방 검사
            { // 함정 오류 시작
                return false; // 누락된 함정 정보 차단
            } // 함정 오류 끝
            excluded[excluded.Length - 1] = new DungeonRoom(contents.trapX, contents.trapY); // 함정 방 좌표 제외
            return TrySelect(layout, seed, excluded, out selected); // 모든 기존 콘텐츠를 제외한 결정적 선택
        } // 선택 끝
        public static bool TrySelect(DungeonLayout layout, int seed, DungeonRoom[] excluded, out DungeonRoom selected) // 기존 콘텐츠를 제외한 결정적 이벤트 방 선택
        { // 선택 시작
            selected = null; // 기본 선택 실패
            if (layout == null) // 생성된 층 검사
            { // 층 오류 시작
                return false; // 누락된 층의 배치 차단
            } // 층 오류 끝
            foreach (var room in layout.Rooms) // 실제 층의 방 순회
            { // 후보 검사 시작
                if (room == null || room.IsSecret || room.Kind != RoomKind.Normal || (room.X == 0 && room.Y == 0) || IsExcluded(room, excluded)) // 특수 방과 기존 콘텐츠 검사
                { // 제외 대상 시작
                    continue; // 점유된 방의 이벤트 배치 차단
                } // 제외 대상 끝
                if (selected == null || Compare(room, selected, seed) < 0) // 현재까지 가장 앞선 결정적 방 검사
                { // 후보 선택 시작
                    selected = room; // 비어 있는 일반 방 보관
                } // 후보 선택 끝
            } // 후보 검사 끝
            return selected != null; // 실제 빈 방이 있을 때만 성공
        } // 선택 끝
        private static bool IsExcluded(DungeonRoom room, DungeonRoom[] excluded) // 기존 조우와 상자 및 함정 좌표 검사
        { // 제외 검사 시작
            if (excluded == null) // 별도 제외 목록 검사
            { // 목록 없음 시작
                return false; // 지정된 제외 방 없음
            } // 목록 없음 끝
            foreach (var existing in excluded) // 기존 콘텐츠 방 순회
            { // 좌표 검사 시작
                if (existing != null && existing.X == room.X && existing.Y == room.Y) // 현재 후보와 실제 좌표 비교
                { // 좌표 일치 시작
                    return true; // 기존 콘텐츠 방 제외
                } // 좌표 일치 끝
            } // 좌표 검사 끝
            return false; // 다른 콘텐츠의 좌표 없음
        } // 제외 검사 끝
        private static int Compare(DungeonRoom left, DungeonRoom right, int seed) // 입력 방 순서에 영향을 받지 않는 비교
        { // 비교 시작
            var order = Rank(left, seed).CompareTo(Rank(right, seed)); // 독립 시험 해시 순서
            if (order != 0) // 서로 다른 해시 검사
            { // 해시 차이 시작
                return order; // 결정적 해시 순서 전달
            } // 해시 차이 끝
            order = left.X.CompareTo(right.X); // 같은 해시의 가로 좌표 비교
            return order != 0 ? order : left.Y.CompareTo(right.Y); // 동일 가로 좌표의 세로 비교
        } // 비교 끝
        private static uint Rank(DungeonRoom room, int seed) // 플랫폼 독립적인 이벤트 방 해시
        { // 해시 시작
            unchecked // 해시 정수 넘침 허용
            { // 해시 계산 시작
                var value = 2166136261u; // 고정된 해시 시작 값
                value = (value ^ 0x45564e54u) * 16777619u; // 다른 콘텐츠와 분리된 이벤트 표식
                value = (value ^ (uint)seed) * 16777619u; // 실제 층 씨드 반영
                value = (value ^ (uint)room.X) * 16777619u; // 방 가로 좌표 반영
                return (value ^ (uint)room.Y) * 16777619u; // 방 세로 좌표 반영
            } // 해시 계산 끝
        } // 해시 끝
    } // 배치 클래스 끝
} // 이름 공간 끝
