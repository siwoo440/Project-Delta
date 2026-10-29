using System; // 방 구성 오류
using System.Collections.Generic; // 실제 방 목록
using ProjectDelta.Core; // 생성된 방과 출구
using UnityEngine; // 2D 방 객체와 표시
using UnityEngine.Tilemaps; // 바닥과 장애물 타일
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class SecretDoorMarker : MonoBehaviour // 실제 비밀벽 표시의 방향 식별
    { // 클래스 시작
        public int RoomX; // 출발 방 가로 좌표
        public int RoomY; // 출발 방 세로 좌표
        public int TargetX; // 도착 방 가로 좌표
        public int TargetY; // 도착 방 세로 좌표
    } // 클래스 끝
    public static class GeneratedRoomFactory // 생성된 방 데이터를 2D 시험 방에 연결
    { // 클래스 시작
        private sealed class MarkerSnapshot // 기존 문 표시의 활성 상태 보관
        { // 클래스 시작
            public readonly GameObject Marker; // 기존 문 표시 객체
            public readonly bool WasActive; // 기존 표시의 활성 상태
            public MarkerSnapshot(GameObject marker) // 표시 상태 저장
            { // 저장 시작
                Marker = marker; // 기존 표시 객체 보관
                WasActive = marker.activeSelf; // 기존 활성 상태 보관
            } // 저장 끝
        } // 클래스 끝
        private sealed class CellSnapshot // 출구 칸의 기존 타일 속성 보관
        { // 클래스 시작
            public readonly Vector3Int Cell; // 출구 격자 칸
            public readonly TileBase Tile; // 기존 타일
            public readonly Color Color; // 기존 타일 색상
            public readonly Matrix4x4 Transform; // 기존 타일 변환
            public readonly TileFlags Flags; // 기존 타일 잠금 설정
            public CellSnapshot(Tilemap map, Vector3Int cell) // 타일 속성 저장
            { // 저장 시작
                Cell = cell; // 출구 칸 보관
                Tile = map.GetTile(cell); // 기존 타일 보관
                Color = map.GetColor(cell); // 기존 색상 보관
                Transform = map.GetTransformMatrix(cell); // 기존 변환 보관
                Flags = map.GetTileFlags(cell); // 기존 잠금 설정 보관
            } // 저장 끝
            public void Restore(Tilemap map) // 저장한 타일 속성 복구
            { // 복구 시작
                map.SetTile(Cell, Tile); // 기존 타일 복구
                if (Tile != null) // 기존 타일 존재 검사
                { // 타일 복구 조건 시작
                    map.SetTileFlags(Cell, TileFlags.None); // 속성 복구를 위한 잠금 해제
                    map.SetColor(Cell, Color); // 기존 색상 복구
                    map.SetTransformMatrix(Cell, Transform); // 기존 변환 복구
                    map.SetTileFlags(Cell, Flags); // 기존 잠금 설정 복구
                } // 타일 복구 조건 끝
            } // 복구 끝
        } // 클래스 끝
        private sealed class RoomSnapshot // 변경 전 기존 방의 복구 정보
        { // 클래스 시작
            private readonly GameObject root; // 기존 방 객체
            private readonly bool wasActive; // 기존 방 활성 상태
            private readonly Tilemap obstacles; // 기존 장애물 타일맵
            private readonly List<CellSnapshot> cells = new List<CellSnapshot>(); // 변경 가능 출구 칸 보관
            private readonly List<MarkerSnapshot> markers = new List<MarkerSnapshot>(); // 기존 문 표시 상태 보관
            public RoomSnapshot(GameObject room) // 기존 방 상태 저장
            { // 저장 시작
                root = room; // 기존 방 객체 보관
                wasActive = room.activeSelf; // 기존 방 활성 상태 보관
                obstacles = room.transform.Find("Obstacles").GetComponent<Tilemap>(); // 기존 장애물 타일맵 조회
                var positions = new[] // 시험 방의 네 출구 칸
                { // 위치 목록 시작
                    new Vector3Int(6, 0, 0), // 오른쪽 출구 칸
                    new Vector3Int(-6, 0, 0), // 왼쪽 출구 칸
                    new Vector3Int(2, 4, 0), // 위쪽 출구 칸
                    new Vector3Int(2, -4, 0) // 아래쪽 출구 칸
                }; // 위치 목록 끝
                foreach (var position in positions) // 출구 칸 순회
                { // 칸 저장 시작
                    cells.Add(new CellSnapshot(obstacles, position)); // 기존 타일 속성 보관
                } // 칸 저장 끝
                foreach (Transform child in room.transform) // 기존 방 객체 순회
                { // 표시 검사 시작
                    if (child.name == "DoorMarker") // 기존 문 표시 검사
                    { // 기존 표시 조건 시작
                        markers.Add(new MarkerSnapshot(child.gameObject)); // 기존 활성 상태 보관
                    } // 기존 표시 조건 끝
                } // 표시 검사 끝
            } // 저장 끝
            public void Restore() // 생성 실패 시 기존 방 상태 복구
            { // 복구 시작
                if (root == null || obstacles == null) // 기존 방 존재 검사
                { // 방 누락 조건 시작
                    return; // 복구 대상 없음
                } // 방 누락 조건 끝
                foreach (var cell in cells) // 저장한 출구 칸 순회
                { // 칸 복구 시작
                    cell.Restore(obstacles); // 원래 타일 속성 복구
                } // 칸 복구 끝
                foreach (var marker in markers) // 기존 문 표시 순회
                { // 표시 복구 시작
                    if (marker.Marker != null) // 기존 표시 존재 검사
                    { // 표시 존재 조건 시작
                        marker.Marker.SetActive(marker.WasActive); // 원래 표시 상태 복구
                    } // 표시 존재 조건 끝
                } // 표시 복구 끝
                root.SetActive(wasActive); // 원래 방 활성 상태 복구
            } // 복구 끝
        } // 클래스 끝
        public static RoomBinding[] Create(DungeonLayout layout, RoomBinding[] originalRooms, DoorBinding[] originalDoors, GridMoveController player) // 실행 전용 방과 문 생성
        { // 생성 시작
            if (layout == null || originalRooms == null || originalDoors == null || player == null) // 필수 구성 검사
            { // 누락 조건 시작
                throw new ArgumentException("A layout, original rooms, and player are required."); // 잘못된 구성 거절
            } // 누락 조건 끝
            var origin = FindOriginal(originalRooms, 0, 0); // 저장된 시작 방 조회
            if (origin == null || !origin.activeSelf) // 원본 시작 방 검사
            { // 시작 방 누락 조건 시작
                throw new ArgumentException("An active original start room is required."); // 잘못된 시작 방 거절
            } // 시작 방 누락 조건 끝
            var originalObstacles = origin.transform.Find("Obstacles"); // 원본 장애물 타일맵 객체
            var indicator = player.transform.Find("DirectionIndicator"); // 기존 방향 이미지 객체
            var renderer = indicator == null ? null : indicator.GetComponent<SpriteRenderer>(); // 기존 방향 이미지 조회
            var obstacleMap = originalObstacles == null ? null : originalObstacles.GetComponent<Tilemap>(); // 원본 장애물 타일맵 조회
            var obstacleTile = obstacleMap == null ? null : obstacleMap.GetTile(new Vector3Int(3, 0, 0)); // 시험 방의 장애물 타일
            var sprite = renderer == null ? null : renderer.sprite; // 기존 문 표시 이미지
            if (obstacleTile == null || sprite == null) // 시험 방 리소스 검사
            { // 리소스 누락 조건 시작
                throw new ArgumentException("The prototype obstacle tile and direction sprite are required."); // 필수 리소스 오류
            } // 리소스 누락 조건 끝
            foreach (var room in layout.Rooms) // 실제 변경 전 모든 방의 출구 검사
            { // 사전 검사 시작
                var original = FindOriginal(originalRooms, room.X, room.Y); // 같은 좌표의 기존 방 조회
                ValidateDoors(original == null ? origin : original, layout, room, originalDoors, original == null ? 0 : room.X, original == null ? 0 : room.Y, obstacleTile); // 원본 또는 복제 원형의 충돌 검사
            } // 사전 검사 끝
            GameObject template = null; // 실패 시 정리할 실행용 원형
            var createdRooms = new List<GameObject>(); // 실패 시 정리할 방 복제본
            var createdMarkers = new List<GameObject>(); // 실패 시 정리할 새 문 표시
            var oldMarkers = new List<GameObject>(); // 성공 후 제거할 기존 문 표시
            var snapshots = new List<RoomSnapshot>(); // 실패 시 복구할 기존 방 상태
            try // 실제 방 생성과 출구 연결
            { // 방 구성 시작
                template = UnityEngine.Object.Instantiate(origin, origin.transform.parent); // 기존 상위 객체의 활성 상태 유지
                template.name = "RoomTemplateRuntime"; // 실행용 원형 이름
                template.SetActive(false); // 원형 화면 표시 방지
                var bindings = new List<RoomBinding>(); // 생성된 방과 씬 객체 연결
                foreach (var room in layout.Rooms) // 생성된 모든 방 순회
                { // 방 순회 시작
                    var root = FindOriginal(originalRooms, room.X, room.Y); // 같은 좌표의 기존 시험 방 조회
                    var sourceX = root == null ? 0 : room.X; // 복제 원본 방의 가로 좌표
                    var sourceY = root == null ? 0 : room.Y; // 복제 원본 방의 세로 좌표
                    if (root == null) // 새 방 인스턴스 필요 검사
                    { // 새 방 조건 시작
                        root = UnityEngine.Object.Instantiate(template); // 원형에서 독립 방 복제
                        createdRooms.Add(root); // 실패 시 삭제할 복제본 보관
                        root.name = "GeneratedRoom_" + room.X + "_" + room.Y; // 좌표별 방 객체 이름
                    } // 새 방 조건 끝
                    else // 기존 방 사용 분기
                    { // 기존 방 조건 시작
                        snapshots.Add(new RoomSnapshot(root)); // 변경 전 기존 방의 복구 정보
                    } // 기존 방 조건 끝
                    if (room.X != 0 || room.Y != 0) // 시작 방 이외의 초기 상태 검사
                    { // 비활성 방 조건 시작
                        root.SetActive(false); // 실제 진입 전 표시 방지
                    } // 비활성 방 조건 끝
                    ConfigureDoors(root, layout, room, originalDoors, sourceX, sourceY, obstacleTile, sprite, oldMarkers, createdMarkers); // 생성된 출구를 실제 방에 반영
                    bindings.Add(new RoomBinding(room.X, room.Y, room.IsSecret, root)); // 방 좌표와 실제 객체 연결
                } // 방 순회 끝
                foreach (var marker in oldMarkers) // 교체한 기존 표시 순회
                { // 기존 표시 정리 시작
                    UnityEngine.Object.Destroy(marker); // 성공 후 이전 표시 제거
                } // 기존 표시 정리 끝
                return bindings.ToArray(); // 실제 방 연결 목록 전달
            } // 방 구성 끝
            catch // 실패 시 기존 씬과 임시 객체 복구
            { // 복구 시작
                foreach (var marker in createdMarkers) // 새로 생성한 문 표시 순회
                { // 새 표시 정리 시작
                    if (marker != null) // 새 표시 존재 검사
                    { // 새 표시 조건 시작
                        marker.SetActive(false); // 실패한 표시 즉시 숨김
                        UnityEngine.Object.Destroy(marker); // 실패한 표시 제거
                    } // 새 표시 조건 끝
                } // 새 표시 정리 끝
                foreach (var snapshot in snapshots) // 변경된 기존 방 상태 순회
                { // 기존 방 복구 시작
                    snapshot.Restore(); // 원래 타일과 표시와 활성 상태 복구
                } // 기존 방 복구 끝
                foreach (var room in createdRooms) // 새로 복제한 방 순회
                { // 복제 방 정리 시작
                    if (room != null) // 복제 방 존재 검사
                    { // 복제 방 조건 시작
                        room.SetActive(false); // 실패한 방 즉시 숨김
                        UnityEngine.Object.Destroy(room); // 실패한 방 제거
                    } // 복제 방 조건 끝
                } // 복제 방 정리 끝
                throw; // 원래 생성 오류 전달
            } // 복구 끝
            finally // 성공과 실패의 공통 정리
            { // 원형 정리 시작
                if (template != null) // 임시 원형 존재 검사
                { // 원형 조건 시작
                    UnityEngine.Object.Destroy(template); // 실행용 원형 제거
                } // 원형 조건 끝
            } // 원형 정리 끝
        } // 생성 끝
        private static GameObject FindOriginal(RoomBinding[] rooms, int x, int y) // 기존 시험 방의 좌표별 조회
        { // 조회 시작
            foreach (var room in rooms) // 기존 방 연결 순회
            { // 방 순회 시작
                if (room != null && room.X == x && room.Y == y) // 좌표 일치 검사
                { // 일치 조건 시작
                    return room.Root; // 기존 방 객체 전달
                } // 일치 조건 끝
            } // 방 순회 끝
            return null; // 해당 좌표의 기존 방 없음
        } // 조회 끝
        private static void ValidateDoors(GameObject root, DungeonLayout layout, DungeonRoom room, DoorBinding[] originalDoors, int sourceX, int sourceY, TileBase obstacleTile) // 원본을 변경하기 전 모든 출구 검사
        { // 검사 시작
            var floorObject = root.transform.Find("Floor"); // 원본 바닥 객체 조회
            var obstacleObject = root.transform.Find("Obstacles"); // 원본 장애물 객체 조회
            var floor = floorObject == null ? null : floorObject.GetComponent<Tilemap>(); // 원본 바닥 타일맵
            var obstacles = obstacleObject == null ? null : obstacleObject.GetComponent<Tilemap>(); // 원본 장애물 타일맵
            if (floor == null || obstacles == null || root.GetComponent<Grid>() == null) // 필수 구성 검사
            { // 구성 누락 조건 시작
                throw new ArgumentException("Every generated room needs floor and obstacle tilemaps."); // 잘못된 방 템플릿 거절
            } // 구성 누락 조건 끝
            foreach (var oldDoor in originalDoors) // 원본 방의 기존 출구 순회
            { // 기존 출구 검사 시작
                if (oldDoor == null || oldDoor.RoomX != sourceX || oldDoor.RoomY != sourceY) // 원본 방 출구 검사
                { // 다른 방 조건 시작
                    continue; // 다른 방 출구 제외
                } // 다른 방 조건 끝
                var oldCell = new Vector3Int(oldDoor.ApproachX, oldDoor.ApproachY, 0) + Offset(oldDoor.Direction); // 기존 출구 칸
                if (obstacles.HasTile(oldCell) && obstacles.GetTile(oldCell) != obstacleTile) // 사용자 지정 타일 검사
                { // 사용자 타일 조건 시작
                    throw new ArgumentException("Original door tile differs from the prototype."); // 기존 타일 보존
                } // 사용자 타일 조건 끝
            } // 기존 출구 검사 끝
            foreach (var door in layout.Doors) // 새로 생성된 출구 순회
            { // 출구 검사 시작
                if (door.RoomX != room.X || door.RoomY != room.Y) // 현재 방의 출구 검사
                { // 다른 방 조건 시작
                    continue; // 다른 방 출구 제외
                } // 다른 방 조건 끝
                var approach = new Vector3Int(door.ApproachX, door.ApproachY, 0); // 새 출구 앞 칸
                var cell = approach + Offset(door.Direction); // 새 출구 칸
                var originalDoor = false; // 기존 출구 칸 여부
                foreach (var oldDoor in originalDoors) // 기존 출구 위치 순회
                { // 기존 위치 검사 시작
                    if (oldDoor != null && oldDoor.RoomX == sourceX && oldDoor.RoomY == sourceY && new Vector3Int(oldDoor.ApproachX, oldDoor.ApproachY, 0) + Offset(oldDoor.Direction) == cell) // 기존 출구와 새 출구의 위치 검사
                    { // 기존 위치 조건 시작
                        originalDoor = true; // 제거 가능한 기존 출구 칸
                    } // 기존 위치 조건 끝
                } // 기존 위치 검사 끝
                if (!floor.HasTile(approach) || !floor.HasTile(cell) || obstacles.HasTile(approach) || (obstacles.HasTile(cell) && !originalDoor)) // 바닥과 기존 장애물 검사
                { // 충돌 조건 시작
                    throw new ArgumentException("Generated door or approach conflicts with the room template."); // 변경 전 충돌 거절
                } // 충돌 조건 끝
            } // 출구 검사 끝
        } // 검사 끝
        private static void ConfigureDoors(GameObject root, DungeonLayout layout, DungeonRoom room, DoorBinding[] originalDoors, int sourceX, int sourceY, TileBase obstacleTile, Sprite sprite, List<GameObject> oldMarkers, List<GameObject> createdMarkers) // 한 방의 생성된 출구 배치
        { // 구성 시작
            var floorObject = root.transform.Find("Floor"); // 방의 바닥 객체 조회
            var obstacleObject = root.transform.Find("Obstacles"); // 방의 장애물 객체 조회
            var floor = floorObject == null ? null : floorObject.GetComponent<Tilemap>(); // 바닥 타일맵 조회
            var obstacles = obstacleObject == null ? null : obstacleObject.GetComponent<Tilemap>(); // 장애물 타일맵 조회
            var grid = root.GetComponent<Grid>(); // 방의 격자 구성 조회
            if (floor == null || obstacles == null || grid == null) // 실제 타일맵과 격자 검사
            { // 타일맵 누락 조건 시작
                throw new ArgumentException("Every generated room needs floor and obstacle tilemaps."); // 방 구성 오류 알림
            } // 타일맵 누락 조건 끝
            foreach (var oldDoor in originalDoors) // 복제 원본의 기존 출구 순회
            { // 기존 출구 검사 시작
                if (oldDoor == null || oldDoor.RoomX != sourceX || oldDoor.RoomY != sourceY) // 복제 원본 방의 출구 검사
                { // 다른 방 조건 시작
                    continue; // 다른 방의 출구 제외
                } // 다른 방 조건 끝
                var oldCell = new Vector3Int(oldDoor.ApproachX, oldDoor.ApproachY, 0) + Offset(oldDoor.Direction); // 원본 출구 정보의 문 칸
                if (obstacles.HasTile(oldCell) && obstacles.GetTile(oldCell) != obstacleTile) // 사용자 지정 출구 타일 검사
                { // 다른 타일 조건 시작
                    throw new ArgumentException("Original door tile differs from the prototype."); // 사용자 지정 타일 덮어쓰기 방지
                } // 다른 타일 조건 끝
                obstacles.SetTile(oldCell, null); // 실행 중 원본 출구 칸 해제
            } // 기존 출구 검사 끝
            for (var index = root.transform.childCount - 1; index >= 0; index--) // 이전 시험 문 표시 순회
            { // 이전 표시 검사 시작
                var marker = root.transform.GetChild(index); // 기존 방의 자식 객체 조회
                if (marker.name != "DoorMarker") // 시험 문 표시 이름 검사
                { // 다른 객체 조건 시작
                    continue; // 다른 방 객체 보존
                } // 다른 객체 조건 끝
                marker.gameObject.SetActive(false); // 이전 표시 즉시 숨김
                oldMarkers.Add(marker.gameObject); // 전체 성공 뒤 제거할 표시 보관
            } // 이전 표시 검사 끝
            foreach (var door in layout.Doors) // 생성된 모든 출구 순회
            { // 출구 순회 시작
                if (door.RoomX != room.X || door.RoomY != room.Y) // 현재 방의 출구 검사
                { // 다른 방 조건 시작
                    continue; // 다른 방의 출구 제외
                } // 다른 방 조건 끝
                var approach = new Vector3Int(door.ApproachX, door.ApproachY, 0); // 문 앞 칸
                var cell = approach + Offset(door.Direction); // 문 표시와 차단 칸
                if (!floor.HasTile(approach) || !floor.HasTile(cell) || obstacles.HasTile(approach) || obstacles.HasTile(cell)) // 출구와 접근 칸 검사
                { // 출구 충돌 조건 시작
                    throw new ArgumentException("Generated door conflicts: room " + room.X + "," + room.Y + " direction " + door.Direction + " approach " + approach + " door " + cell + " floor " + floor.HasTile(approach) + "/" + floor.HasTile(cell) + " blocked " + obstacles.HasTile(approach) + "/" + obstacles.HasTile(cell)); // 충돌한 방과 칸 알림
                } // 출구 충돌 조건 끝
                obstacles.SetTile(cell, obstacleTile); // 생성된 문 칸의 직접 통행 차단
                var marker = new GameObject("DoorMarker", typeof(SpriteRenderer)); // 문 방향 표시 객체 생성
                createdMarkers.Add(marker); // 실패 시 제거할 새 표시 보관
                marker.transform.SetParent(root.transform, false); // 해당 방 아래 배치
                if (door.IsSecret) // 비밀 출구 검사
                { // 비밀벽 표시 시작
                    var secret = marker.AddComponent<SecretDoorMarker>(); // 비밀벽 연결 식별 정보
                    secret.RoomX = door.RoomX; // 출발 방 가로 좌표
                    secret.RoomY = door.RoomY; // 출발 방 세로 좌표
                    secret.TargetX = door.TargetRoomX; // 도착 방 가로 좌표
                    secret.TargetY = door.TargetRoomY; // 도착 방 세로 좌표
                    marker.SetActive(false); // 발견 전 비밀벽 표시 숨김
                } // 비밀벽 표시 끝
                var cellSize = grid.cellSize; // 격자 칸 크기 조회
                var cellGap = grid.cellGap; // 격자 칸 사이 간격 조회
                var anchor = floor.tileAnchor; // 바닥 타일 중심 기준
                var local = new Vector3((cell.x + anchor.x) * cellSize.x + cell.x * cellGap.x, (cell.y + anchor.y) * cellSize.y + cell.y * cellGap.y, (cell.z + anchor.z) * cellSize.z + cell.z * cellGap.z); // 비활성 방에서도 유효한 칸 중심
                marker.transform.position = floor.transform.TransformPoint(local); // 생성된 문 칸 중심
                marker.transform.localRotation = Quaternion.Euler(0f, 0f, Angle(door.Direction)); // 출구 방향 표시
                var image = marker.GetComponent<SpriteRenderer>(); // 문 표시 렌더러 조회
                image.sprite = sprite; // 기존 임시 방향 이미지 연결
                image.color = new Color(1f, 0.88f, 0.2f); // 기존 노란 문 표시 색상
                image.sortingOrder = 3; // 바닥과 문 타일 위 표시
            } // 출구 순회 끝
        } // 구성 끝
        private static Vector3Int Offset(MoveDirection direction) // 문 앞에서 문 칸으로 향하는 방향
        { // 변환 시작
            switch (direction) // 출구 방향 선택
            { // 선택 시작
                case MoveDirection.Right: // 오른쪽 방향
                    return Vector3Int.right; // 오른쪽 한 칸
                case MoveDirection.Up: // 위쪽 방향
                    return Vector3Int.up; // 위쪽 한 칸
                case MoveDirection.Left: // 왼쪽 방향
                    return Vector3Int.left; // 왼쪽 한 칸
                default: // 아래쪽 방향
                    return Vector3Int.down; // 아래쪽 한 칸
            } // 선택 끝
        } // 변환 끝
        private static float Angle(MoveDirection direction) // 출구 방향의 이미지 각도
        { // 변환 시작
            switch (direction) // 출구 방향 선택
            { // 선택 시작
                case MoveDirection.Right: // 오른쪽 방향
                    return -90f; // 오른쪽 회전 각도
                case MoveDirection.Left: // 왼쪽 방향
                    return 90f; // 왼쪽 회전 각도
                case MoveDirection.Down: // 아래쪽 방향
                    return 180f; // 아래쪽 회전 각도
                default: // 위쪽 방향
                    return 0f; // 위쪽 회전 각도
            } // 선택 끝
        } // 변환 끝
    } // 클래스 끝
} // 이름 공간 끝
