using System; // 잘못된 던전 구조 오류
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public sealed class DoorLockState // 시험 문의 양방향 잠금 상태
    { // 클래스 시작
        private bool locked; // 선택된 문의 현재 잠금 여부
        private bool forceAttempted; // 강제 개방 시도 여부
        private DoorKeyRing reservedKeys; // 해제 시 열쇠를 예약한 보관소
        public RoomDoor TrialDoor // 필수 경로를 막지 않는 시험 문
        { // 속성 시작
            get; // 시험 문 조회
        } // 속성 끝
        public DoorLockState(DungeonLayout layout) // 현재 층의 시험 문 선택
        { // 선택 시작
            if (layout == null) // 던전 구조 검사
            { // 구조 누락 조건 시작
                throw new ArgumentNullException(nameof(layout)); // 잘못된 구조 차단
            } // 구조 누락 조건 끝
            foreach (var room in layout.Rooms) // 생성된 방 순회
            { // 말단 방 검사 시작
                if (room.Kind != RoomKind.Normal || room.IsSecret) // 공개 일반 방 검사
                { // 다른 방 조건 시작
                    continue; // 필수 방과 비밀 방 제외
                } // 다른 방 조건 끝
                RoomDoor exit = null; // 현재 방의 단일 출구
                var count = 0; // 현재 방의 출구 수
                foreach (var door in layout.Doors) // 양방향 문 순회
                { // 출구 검사 시작
                    if (door.RoomX == room.X && door.RoomY == room.Y) // 현재 방에서 출발하는 문 검사
                    { // 현재 방 출구 조건 시작
                        exit = door; // 출구 보관
                        count++; // 출구 수 증가
                    } // 현재 방 출구 조건 끝
                } // 출구 검사 끝
                if (count != 1 || exit.IsSecret) // 공개 말단 방 검사
                { // 말단 아님 조건 시작
                    continue; // 필수 연결 보존
                } // 말단 아님 조건 끝
                TrialDoor = layout.GetConnection(exit.TargetRoomX, exit.TargetRoomY, room.X, room.Y); // 말단 방으로 들어가는 문 선택
                locked = TrialDoor != null; // 유효한 시험 문 잠금
                break; // 첫 시험 문 선택 완료
            } // 말단 방 검사 끝
        } // 선택 끝
        public bool IsLocked(RoomDoor door) // 양방향 잠금 상태 조회
        { // 조회 시작
            return locked && Matches(door); // 선택된 연결의 현재 잠금 여부
        } // 조회 끝
        public bool CanForce(RoomDoor door) // 현재 문의 강제 개방 가능 여부
        { // 조회 시작
            return IsLocked(door) && !forceAttempted; // 잠금과 첫 시도 확인
        } // 조회 끝
        public bool TryForce(RoomDoor door, int attack, int modifier, int difficulty, int playerRoll, int eventRoll, out bool success) // 한 번의 공격력 판정으로 잠금 해제
        { // 판정 시작
            success = false; // 기본 실패 상태
            if (!CanForce(door) || !EventCheck.TryResolve(attack, modifier, difficulty, playerRoll, eventRoll, out success)) // 잠금과 주사위 유효성 검사
            { // 판정 거절 조건 시작
                return false; // 잘못된 재시도와 난수 차단
            } // 판정 거절 조건 끝
            forceAttempted = true; // 양방향 연결의 시도 기록
            if (success) // 성공한 강제 개방 검사
            { // 개방 조건 시작
                locked = false; // 열쇠 소모 없이 양방향 잠금 해제
            } // 개방 조건 끝
            return true; // 유효한 강제 개방 판정 완료
        } // 판정 끝
        public bool TryUnlock(RoomDoor door, DoorKeyRing keys) // 열쇠 소모와 잠금 해제
        { // 해제 시작
            if (!IsLocked(door) || keys == null || !keys.TryReserve()) // 잠긴 시험 문과 열쇠 검사
            { // 해제 거절 조건 시작
                return false; // 잘못된 문과 열쇠 부족 차단
            } // 해제 거절 조건 끝
            reservedKeys = keys; // 예약한 열쇠 보관소 기록
            locked = false; // 양방향 연결 잠금 해제
            return true; // 열쇠 예약과 해제 성공
        } // 해제 끝
        public bool Commit(RoomDoor door, DoorKeyRing keys) // 성공한 이동의 잠금 해제 확정
        { // 확정 시작
            if (locked || !Matches(door) || keys == null || reservedKeys != keys || !keys.CommitReservation()) // 같은 문의 예약 확정 검사
            { // 확정 거절 조건 시작
                return false; // 잘못된 중복 확정 차단
            } // 확정 거절 조건 끝
            reservedKeys = null; // 확정된 열쇠 보관소 해제
            return true; // 잠금 해제 확정 성공
        } // 확정 끝
        public bool Rollback(RoomDoor door, DoorKeyRing keys) // 실패한 이동의 열쇠와 잠금 복구
        { // 복구 시작
            if (locked || !Matches(door) || keys == null || reservedKeys != keys || !keys.RefundReservation()) // 같은 문의 예약 열쇠 복구 검사
            { // 복구 거절 조건 시작
                return false; // 잘못된 중복 복구 차단
            } // 복구 거절 조건 끝
            reservedKeys = null; // 복구된 열쇠 보관소 해제
            locked = true; // 양방향 연결 잠금 복구
            return true; // 열쇠와 잠금 복구 성공
        } // 복구 끝
        private bool Matches(RoomDoor door) // 시험 연결의 양방향 일치 검사
        { // 검사 시작
            if (TrialDoor == null || door == null) // 문 존재 검사
            { // 문 누락 조건 시작
                return false; // 유효하지 않은 문 제외
            } // 문 누락 조건 끝
            var forward = door.RoomX == TrialDoor.RoomX && door.RoomY == TrialDoor.RoomY && door.TargetRoomX == TrialDoor.TargetRoomX && door.TargetRoomY == TrialDoor.TargetRoomY; // 선택된 정방향 연결
            var reverse = door.RoomX == TrialDoor.TargetRoomX && door.RoomY == TrialDoor.TargetRoomY && door.TargetRoomX == TrialDoor.RoomX && door.TargetRoomY == TrialDoor.RoomY; // 선택된 역방향 연결
            return forward || reverse; // 같은 연결의 두 방향 허용
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
