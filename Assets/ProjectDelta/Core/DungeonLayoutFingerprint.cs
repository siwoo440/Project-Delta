using System; // 지문 문자열 변환
using System.Collections.Generic; // 구조 정렬 목록
using System.IO; // 고정 이진 형식
using System.Security.Cryptography; // 구조 해시 계산
namespace ProjectDelta.Core // 게임 규칙 이름 공간
{ // 이름 공간 시작
    public static class DungeonLayoutFingerprint // 던전 구조의 결정적 지문
    { // 클래스 시작
        public static string Compute(DungeonLayout layout) // 방과 문의 정렬된 구조 해시
        { // 계산 시작
            if (layout == null) // 던전 구조 누락 검사
            { // 누락 조건 시작
                return null; // 누락된 구조의 지문 없음
            } // 누락 조건 끝
            var rooms = new List<DungeonRoom>(layout.Rooms); // 독립 방 목록
            rooms.Sort((left, right) => left.X != right.X ? left.X.CompareTo(right.X) : left.Y.CompareTo(right.Y)); // 좌표 기준 방 정렬
            var doors = new List<RoomDoor>(layout.Doors); // 독립 문 목록
            doors.Sort((left, right) => left.RoomX != right.RoomX ? left.RoomX.CompareTo(right.RoomX) : left.RoomY != right.RoomY ? left.RoomY.CompareTo(right.RoomY) : left.Direction.CompareTo(right.Direction)); // 방과 방향 기준 문 정렬
            using (var stream = new MemoryStream()) // 고정 형식 기록 버퍼
            { // 버퍼 시작
                using (var writer = new BinaryWriter(stream)) // 이진 기록 도구
                { // 기록 시작
                    writer.Write("PROJECT-DELTA-LAYOUT-1"); // 지문 형식 식별자
                    writer.Write(rooms.Count); // 방 수 기록
                    foreach (var room in rooms) // 정렬된 방 순회
                    { // 방 기록 시작
                        writer.Write(room.X); // 방 가로 좌표
                        writer.Write(room.Y); // 방 세로 좌표
                        writer.Write((int)room.Kind); // 방 역할
                        writer.Write(room.IsSecret); // 비밀 방 여부
                    } // 방 기록 끝
                    writer.Write(doors.Count); // 문 수 기록
                    foreach (var door in doors) // 정렬된 문 순회
                    { // 문 기록 시작
                        writer.Write(door.RoomX); // 출발 방 가로 좌표
                        writer.Write(door.RoomY); // 출발 방 세로 좌표
                        writer.Write(door.ApproachX); // 문 앞 가로 칸
                        writer.Write(door.ApproachY); // 문 앞 세로 칸
                        writer.Write((int)door.Direction); // 문 방향
                        writer.Write(door.TargetRoomX); // 도착 방 가로 좌표
                        writer.Write(door.TargetRoomY); // 도착 방 세로 좌표
                        writer.Write(door.TargetX); // 도착 가로 칸
                        writer.Write(door.TargetY); // 도착 세로 칸
                        writer.Write(door.IsSecret); // 비밀 문 여부
                    } // 문 기록 끝
                    writer.Flush(); // 기록 버퍼 반영
                    using (var hash = SHA256.Create()) // SHA256 계산 도구
                    { // 계산 시작
                        return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", string.Empty); // 결정적인 구조 지문
                    } // 계산 끝
                } // 기록 끝
            } // 버퍼 끝
        } // 계산 끝
    } // 클래스 끝
} // 이름 공간 끝
