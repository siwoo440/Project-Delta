using System; // 생성 범위와 입력 오류
using System.Collections.Generic; // 방과 문 목록 복사
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class FloorLayoutRules // 층별 결정적 구조와 최심부 역할
    { // 클래스 시작
        public static DungeonLayout Generate(int seed, int floor, int version) // 회차 시드의 층별 구조 생성
        { // 생성 시작
            var layout = DungeonGenerator.GenerateVersioned(FloorSeedRules.For(seed, floor), version); // 기존 생성 규칙 재사용
            if (floor != 5) // 첫 네 층 검사
            { // 기존 구조 시작
                return layout; // 기존 방과 문 구조 보존
            } // 기존 구조 끝
            var rooms = new DungeonRoom[layout.Rooms.Count]; // 기존 좌표를 보존한 방 목록
            for (var index = 0; index < rooms.Length; index++) // 생성된 방 순회
            { // 변환 시작
                var room = layout.Rooms[index]; // 원래 생성된 방
                var kind = room.Kind == RoomKind.Stairs ? RoomKind.Boss : room.Kind == RoomKind.Elite ? RoomKind.Normal : room.Kind; // 계단의 마왕 방과 상위 방의 일반 방 전환
                rooms[index] = new DungeonRoom(room.X, room.Y, room.IsSecret, kind); // 좌표와 비밀 여부 보존
            } // 변환 끝
            return new DungeonLayout(rooms, new List<RoomDoor>(layout.Doors).ToArray()); // 기존 양방향 문 보존
        } // 생성 끝
        public static bool IsValid(DungeonLayout layout, int seed, int floor, int version) // 실제 층 구조의 완전 재현 검증
        { // 검사 시작
            if (layout == null || floor < 1 || floor > 5 || version < 1 || version > 3) // 층과 생성 버전 검사
            { // 오류 시작
                return false; // 잘못된 입력 차단
            } // 오류 끝
            try // 기존 생성 규칙과 지문 대조
            { // 재현 검사 시작
                var original = DungeonGenerator.GenerateVersioned(FloorSeedRules.For(seed, floor), version); // 층별 기존 원본 구조
                if (!DungeonGenerator.IsValidVersioned(original, version)) // 기존 구조의 연결과 필수 방 검사
                { // 오류 시작
                    return false; // 잘못된 원본 구조 차단
                } // 오류 끝
                var expected = Generate(seed, floor, version); // 실제 층의 결정적 목표 구조
                if (DungeonLayoutFingerprint.Compute(layout) != DungeonLayoutFingerprint.Compute(expected)) // 모든 방과 문의 정렬된 지문 대조
                { // 오류 시작
                    return false; // 누락되거나 변조된 연결과 방 차단
                } // 오류 끝
                if (floor == 5) // 최심부의 필수 역할 검사
                { // 마왕 방 검사 시작
                    var boss = 0; // 마왕 방 수
                    var stairs = 0; // 계단 방 수
                    var elite = 0; // 상위 개체 방 수
                    foreach (var room in layout.Rooms) // 실제 방 순회
                    { // 역할 검사 시작
                        if (room.Kind == RoomKind.Boss) // 마왕 방 검사
                        { // 마왕 방 집계 시작
                            boss++; // 마왕 방 수 누적
                        } // 마왕 방 집계 끝
                        if (room.Kind == RoomKind.Stairs) // 계단 방 검사
                        { // 계단 방 집계 시작
                            stairs++; // 계단 방 수 누적
                        } // 계단 방 집계 끝
                        if (room.Kind == RoomKind.Elite) // 상위 개체 방 검사
                        { // 상위 방 집계 시작
                            elite++; // 상위 개체 방 수 누적
                        } // 상위 방 집계 끝
                    } // 역할 검사 끝
                    return boss == 1 && stairs == 0 && elite == 0; // 기획된 최심부 역할 확인
                } // 마왕 방 검사 끝
                return true; // 완전히 재현된 층 구조
            } // 재현 검사 끝
            catch (Exception) // 생성 실패 검사
            { // 오류 시작
                return false; // 복원할 수 없는 구조 차단
            } // 오류 끝
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
