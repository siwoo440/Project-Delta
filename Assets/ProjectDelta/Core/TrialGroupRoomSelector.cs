namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class TrialGroupRoomSelector // 발생 확률 미확정의 시험 다수 조우 방 선택
    { // 클래스 시작
        public static DungeonRoom Select(DungeonLayout layout, int tutorialX, int tutorialY) // 튜토리얼을 제외한 시험 일반 방 조회
        { // 선택 시작
            if (layout == null) // 던전 생성 실패 검사
            { // 생성 실패 시작
                return null; // 생성되지 않은 구조의 배치 차단
            } // 생성 실패 끝
            DungeonRoom chosen = null; // 확정된 시험 일반 방
            foreach (var room in layout.Rooms) // 실제 생성된 방 순회
            { // 후보 검사 시작
                if (room == null || room.IsSecret || room.Kind != RoomKind.Normal || (room.X == 0 && room.Y == 0) || (room.X == tutorialX && room.Y == tutorialY)) // 비밀과 특수 및 최초 조우 방 제외
                { // 후보 제외 시작
                    continue; // 일반 시험 조우 불가 방 건너뛰기
                } // 후보 제외 끝
                if (chosen == null || room.X < chosen.X || (room.X == chosen.X && room.Y < chosen.Y)) // 좌표 기준의 안정적인 순서 검사
                { // 후보 선택 시작
                    chosen = room; // 현재까지 가장 앞선 방 보관
                } // 후보 선택 끝
            } // 후보 검사 끝
            return chosen; // 동일 구조의 고정 후보 또는 후보 없음 전달
        } // 선택 끝
    } // 클래스 끝
} // 이름 공간 끝
