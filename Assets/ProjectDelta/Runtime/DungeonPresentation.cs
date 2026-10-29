using System; // 리소스 구성과 수치 검사
using UnityEngine; // 스프라이트와 방 객체
using UnityEngine.Tilemaps; // 기존 시험 타일 교체
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonPresentationTag : MonoBehaviour // 중복 확대 방지 표시
    { // 클래스 시작
    } // 클래스 끝
    public static class DungeonPresentation // 실행 중 던전 이미지와 크기 적용
    { // 클래스 시작
        private static Tile floorTile; // 생성한 바닥 이미지 타일
        private static Tile obstacleTile; // 생성한 장애물 이미지 타일
        private static Sprite doorSprite; // 생성한 문 이미지
        private static Tile borderTile; // 각 바닥 칸의 검은 테두리
        public static void Apply(RoomBinding[] rooms) // 실제 생성된 방의 표시 구성
        { // 적용 시작
            if (rooms == null) // 방 연결 존재 검사
            { // 누락 조건 시작
                return; // 변경할 방 없음
            } // 누락 조건 끝
            PrepareArt(); // 생성 이미지와 타일 준비
            foreach (var binding in rooms) // 생성된 방 순회
            { // 방 적용 시작
                var root = binding == null ? null : binding.Root; // 실제 방 객체 조회
                if (root == null || root.GetComponent<DungeonPresentationTag>() != null) // 방 존재와 중복 적용 검사
                { // 제외 조건 시작
                    continue; // 없는 방과 이미 적용한 방 보존
                } // 제외 조건 끝
                root.transform.localScale = Vector3.Scale(root.transform.localScale, new Vector3(2f, 2f, 1f)); // 격자와 자식 이미지의 가로 세로 두 배
                root.AddComponent<DungeonPresentationTag>(); // 현재 방의 중복 확대 차단
                var floorObject = root.transform.Find("Floor"); // 실제 바닥 객체
                var obstacleObject = root.transform.Find("Obstacles"); // 실제 장애물 객체
                var floor = floorObject == null ? null : floorObject.GetComponent<Tilemap>(); // 실제 바닥 타일맵
                var obstacles = obstacleObject == null ? null : obstacleObject.GetComponent<Tilemap>(); // 실제 장애물 타일맵
                ReplacePrototype(floor, "GridTile", floorTile); // 기존 시험 바닥만 이미지 교체
                AddTileBorders(root, floor); // 바닥 칸마다 검은 테두리 표시
                ReplacePrototype(obstacles, "ObstacleTile", obstacleTile); // 기존 시험 장애물만 이미지 교체
                foreach (Transform child in root.transform) // 실제 방의 표시 객체 순회
                { // 문 표시 검사 시작
                    if (child.name != "DoorMarker" || doorSprite == null) // 시험 문 표시와 이미지 검사
                    { // 제외 조건 시작
                        continue; // 다른 표시와 사용자 객체 보존
                    } // 제외 조건 끝
                    var renderer = child.GetComponent<SpriteRenderer>(); // 문 표시 렌더러 조회
                    if (renderer != null) // 문 렌더러 존재 검사
                    { // 문 이미지 적용 시작
                        renderer.sprite = doorSprite; // 생성한 문 이미지 적용
                        renderer.color = Color.white; // 이미지 본래 색상 표시
                    } // 문 이미지 적용 끝
                } // 문 표시 검사 끝
            } // 방 적용 끝
        } // 적용 끝
        private static void PrepareArt() // 프로젝트의 생성 이미지 준비
        { // 준비 시작
            if (borderTile == null) // 격자 테두리 준비 여부 검사
            { // 격자 준비 시작
                borderTile = CreateBorderTile(); // 투명 중심의 검은 테두리 타일
            } // 격자 준비 끝
            if (floorTile == null) // 바닥 이미지 준비 여부 검사
            { // 바닥 준비 시작
                floorTile = CreateTile("DungeonArt/DungeonFloor", "GeneratedDungeonFloor"); // 생성 바닥의 실행 타일
            } // 바닥 준비 끝
            if (obstacleTile == null) // 장애물 이미지 준비 여부 검사
            { // 장애물 준비 시작
                obstacleTile = CreateTile("DungeonArt/DungeonObstacle", "GeneratedDungeonObstacle"); // 생성 장애물의 실행 타일
            } // 장애물 준비 끝
            if (doorSprite == null) // 문 이미지 준비 여부 검사
            { // 문 준비 시작
                doorSprite = CreateSprite("DungeonArt/DungeonDoor"); // 생성 문 이미지 스프라이트
            } // 문 준비 끝
        } // 준비 끝
        private static Tile CreateBorderTile() // 바닥 격자의 검은 선 생성
        { // 생성 시작
            const int size = 32; // 테두리 이미지 해상도
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false); // 투명 배경의 선 이미지
            texture.filterMode = FilterMode.Point; // 선명한 격자 가장자리
            texture.wrapMode = TextureWrapMode.Clamp; // 인접 타일의 번짐 방지
            for (var y = 0; y < size; y++) // 이미지 세로 픽셀 순회
            { // 세로 순회 시작
                for (var x = 0; x < size; x++) // 이미지 가로 픽셀 순회
                { // 가로 순회 시작
                    var edge = x == 0 || y == 0 || x == size - 1 || y == size - 1; // 각 칸의 가장자리 검사
                    texture.SetPixel(x, y, edge ? Color.black : Color.clear); // 검은 선과 투명한 중심
                } // 가로 순회 끝
            } // 세로 순회 끝
            texture.Apply(); // 실제 표시 이미지 갱신
            var tile = ScriptableObject.CreateInstance<Tile>(); // 실행 중 격자 타일 생성
            tile.name = "DungeonTileBorder"; // 격자 타일 식별 이름
            tile.sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size); // 한 칸 크기의 선 이미지
            tile.colliderType = Tile.ColliderType.None; // 이동 판정에 영향 없는 표시
            return tile; // 완성된 격자 타일 전달
        } // 생성 끝
        private static void AddTileBorders(GameObject root, Tilemap floor) // 실제 바닥 칸마다 격자 표시
        { // 표시 시작
            if (floor == null || borderTile == null) // 바닥과 선 이미지 검사
            { // 누락 조건 시작
                return; // 원본 바닥 유지
            } // 누락 조건 끝
            var borderObject = new GameObject("TileBorders", typeof(Tilemap), typeof(TilemapRenderer)); // 격자 표시 전용 객체
            borderObject.transform.SetParent(root.transform, false); // 현재 방의 격자 좌표 사용
            var borderMap = borderObject.GetComponent<Tilemap>(); // 격자 표시 타일맵
            var borderRenderer = borderObject.GetComponent<TilemapRenderer>(); // 격자 표시 렌더러
            var floorRenderer = floor.GetComponent<TilemapRenderer>(); // 바닥 표시 렌더러
            borderRenderer.sortingLayerID = floorRenderer.sortingLayerID; // 바닥과 동일한 표시 계층
            borderRenderer.sortingOrder = floorRenderer.sortingOrder + 1; // 바닥 위의 검은 격자
            foreach (var cell in floor.cellBounds.allPositionsWithin) // 실제 바닥 범위의 칸 순회
            { // 칸 순회 시작
                if (floor.HasTile(cell)) // 바닥이 있는 칸 검사
                { // 바닥 조건 시작
                    borderMap.SetTile(cell, borderTile); // 해당 바닥 칸의 테두리 표시
                } // 바닥 조건 끝
            } // 칸 순회 끝
        } // 표시 끝
        private static Tile CreateTile(string path, string name) // 생성 이미지의 실행 타일 구성
        { // 생성 시작
            var sprite = CreateSprite(path); // 실제 생성 이미지 스프라이트
            if (sprite == null) // 생성 이미지 누락 검사
            { // 누락 조건 시작
                return null; // 기존 시험 타일 보존
            } // 누락 조건 끝
            var tile = ScriptableObject.CreateInstance<Tile>(); // 실행 중 사용할 타일
            tile.name = name; // 검증 가능한 타일 이름
            tile.sprite = sprite; // 생성 이미지 연결
            tile.colliderType = Tile.ColliderType.Sprite; // 기존 타일 충돌 형식 유지
            return tile; // 완성한 실행 타일 전달
        } // 생성 끝
        private static Sprite CreateSprite(string path) // 생성 이미지를 한 칸 크기의 스프라이트로 변환
        { // 생성 시작
            var texture = Resources.Load<Texture2D>(path); // 프로젝트 리소스 조회
            if (texture == null || texture.width <= 0 || texture.height <= 0) // 생성 이미지 존재 검사
            { // 누락 조건 시작
                Debug.LogWarning("Dungeon image unavailable: " + path); // 누락된 이미지 안내
                return null; // 기존 표시 보존
            } // 누락 조건 끝
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), Math.Max(texture.width, texture.height)); // 이미지 전체를 한 칸에 연결
        } // 생성 끝
        private static void ReplacePrototype(Tilemap map, string oldName, Tile replacement) // 이름이 일치하는 시험 타일만 교체
        { // 교체 시작
            if (map == null || replacement == null) // 타일맵과 생성 이미지 검사
            { // 누락 조건 시작
                return; // 기존 타일 보존
            } // 누락 조건 끝
            var count = map.GetUsedTilesCount(); // 실제 사용 중인 타일 종류 수
            var used = new TileBase[count]; // 타일 종류 보관 배열
            map.GetUsedTilesNonAlloc(used); // 현재 타일 종류 조회
            foreach (var tile in used) // 현재 타일 종류 순회
            { // 타일 검사 시작
                if (tile != null && tile.name == oldName) // 알려진 시험 타일 검사
                { // 시험 타일 조건 시작
                    map.SwapTile(tile, replacement); // 같은 시험 타일만 생성 이미지로 변경
                } // 시험 타일 조건 끝
            } // 타일 검사 끝
        } // 교체 끝
    } // 클래스 끝
} // 이름 공간 끝
