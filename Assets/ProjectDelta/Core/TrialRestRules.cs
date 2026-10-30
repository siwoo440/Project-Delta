using System; // 직렬화와 독립 방 목록
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    [Serializable] // 층별 휴식 기록 직렬화
    public sealed class SavedFloorRest // 단일 층의 휴식 방 저장
    { // 저장 클래스 시작
        public int floor; // 실제 던전 층
        public int x; // 휴식 방 가로 좌표
        public int y; // 휴식 방 세로 좌표
        public bool used; // 휴식 사용 완료 여부
    } // 저장 클래스 끝
    public static class TrialRestRules // 시험 휴식 방의 배치와 회복
    { // 규칙 클래스 시작
        public static DungeonRoom[] ExcludeLockedRoom(DungeonLayout layout, DungeonRoom[] excluded) // 잠긴 말단 방의 필수 콘텐츠 배치 방지
        { // 제외 시작
            if (layout == null || excluded == null) // 구조와 기존 제외 목록 검사
            { // 오류 시작
                return null; // 잘못된 입력 차단
            } // 오류 끝
            var door = new DoorLockState(layout).TrialDoor; // 초기 잠금 대상의 실제 연결
            if (door == null) // 잠긴 말단 방 검사
            { // 잠금 없음 시작
                return (DungeonRoom[])excluded.Clone(); // 독립 제외 목록 반환
            } // 잠금 없음 끝
            var result = new DungeonRoom[excluded.Length + 1]; // 잠긴 방을 포함한 목록
            Array.Copy(excluded, result, excluded.Length); // 기존 조우 제외 목록 복사
            result[result.Length - 1] = layout.GetRoom(door.TargetRoomX, door.TargetRoomY); // 잠긴 말단 방 제외
            return result; // 독립 제외 목록 반환
        } // 제외 끝
        public static bool TryCreate(int floor, DungeonLayout layout, int seed, SavedFloorRoomContent content, DungeonRoom[] encounters, DungeonRoom eventRoom, DungeonRoom npcRoom, out SavedFloorRest saved) // 기존 콘텐츠를 피한 휴식 방 생성
        { // 생성 시작
            saved = null; // 기본 생성 실패
            if (floor < 1 || floor > 5 || layout == null || content == null || content.floor != floor || eventRoom != null && layout.GetRoom(eventRoom.X, eventRoom.Y) == null || npcRoom != null && layout.GetRoom(npcRoom.X, npcRoom.Y) == null) // 층과 지정된 방 검사
            { // 잘못된 입력 시작
                return false; // 다른 층과 잘못된 상태 차단
            } // 잘못된 입력 끝
            var count = encounters == null ? 0 : encounters.Length; // 기존 조우 방 수
            var excluded = new DungeonRoom[count + (eventRoom == null ? 0 : 1) + (npcRoom == null ? 0 : 1)]; // 기존 조우와 추가 제외 방 목록
            if (encounters != null) // 기존 조우 존재 검사
            { // 조우 복사 시작
                Array.Copy(encounters, excluded, count); // 기존 조우 목록 보존
            } // 조우 복사 끝
            if (eventRoom != null) // 기존 이벤트 방 검사
            { // 이벤트 제외 시작
                excluded[count++] = eventRoom; // 이벤트 방 제외
            } // 이벤트 제외 끝
            if (npcRoom != null) // 기존 상인 방 검사
            { // 상인 제외 시작
                excluded[count] = npcRoom; // 상인 방 제외
            } // 상인 제외 끝
            if (!TrialEventPlacement.TrySelect(layout, seed, content, excluded, out var room)) // 모든 기존 콘텐츠 제외와 빈 방 선택
            { // 배치 실패 시작
                return false; // 빈 방 부족 시 기존 콘텐츠 보존
            } // 배치 실패 끝
            saved = new SavedFloorRest { floor = floor, x = room.X, y = room.Y, used = false }; // 결정적 휴식 방 기록
            return true; // 정상 휴식 방 생성
        } // 생성 끝
        public static bool TryRestore(SavedFloorRest saved, DungeonLayout layout, int seed, SavedFloorRoomContent content, DungeonRoom[] encounters, DungeonRoom eventRoom, DungeonRoom npcRoom) // 휴식 저장의 결정적 좌표 검증
        { // 복원 검사 시작
            return saved != null && TryCreate(saved.floor, layout, seed, content, encounters, eventRoom, npcRoom, out var expected) && saved.x == expected.x && saved.y == expected.y; // 실제 배치와 저장 좌표 비교
        } // 복원 검사 끝
        public static bool TryUse(SavedFloorRest saved, int health, int maxHealth, int mana, int maxMana, out SavedFloorRest next, out int newHealth, out int newMana) // 현재 자원과 일회 휴식 적용
        { // 사용 시작
            next = null; // 기본 저장 변경 없음
            newHealth = health; // 기본 체력 보존
            newMana = mana; // 기본 마나 보존
            if (saved == null || saved.floor < 1 || saved.floor > 5 || saved.used || maxHealth < 1 || health < 1 || health > maxHealth || maxMana < 1 || mana < 0 || mana > maxMana) // 일회 사용과 생존 자원 범위 검사
            { // 잘못된 사용 시작
                return false; // 잘못된 휴식과 중복 사용 차단
            } // 잘못된 사용 끝
            newHealth = (int)Math.Min((long)maxHealth, (long)health + (long)maxHealth * 3 / 10); // 최대 체력 삼십 퍼센트 회복
            newMana = (int)Math.Min((long)maxMana, (long)mana + (long)maxMana * 3 / 10); // 최대 마나 삼십 퍼센트 회복
            next = new SavedFloorRest { floor = saved.floor, x = saved.x, y = saved.y, used = true }; // 원본을 보존한 사용 완료 기록
            return true; // 회복과 사용 기록 준비 성공
        } // 사용 끝
    } // 규칙 클래스 끝
} // 이름 공간 끝
