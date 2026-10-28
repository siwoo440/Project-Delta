using System; // 구성 오류 알림
using System.IO; // 방향 이미지 저장
using ProjectDelta.Runtime; // 플레이어 이동 구성
using UnityEditor; // 제작 도구
using UnityEditor.SceneManagement; // 씬 열기와 저장
using UnityEngine; // 씬 객체와 2D 이미지
using UnityEngine.SceneManagement; // 열린 씬 목록
using UnityEngine.Tilemaps; // 바닥과 장애물 타일맵
namespace ProjectDelta.Editor // 제작 도구 이름 공간
{ // 이름 공간 시작
    public static class DayTwoProjectSetup // 기존 던전의 이동 구성
    { // 클래스 시작
        private const string DungeonPath = "Assets/ProjectDelta/Scenes/DungeonScene.unity"; // 기존 던전 씬 위치
        private const string TilePath = "Assets/ProjectDelta/Art/Prototype/GridTile.asset"; // 기존 격자 타일 위치
        private const string ArrowPath = "Assets/ProjectDelta/Art/Prototype/DirectionArrow.png"; // 임시 방향 표시 위치
        private const string ObstacleImagePath = "Assets/ProjectDelta/Art/Prototype/ObstacleCell.png"; // 장애물 이미지 위치
        private const string ObstacleTilePath = "Assets/ProjectDelta/Art/Prototype/ObstacleTile.asset"; // 장애물 타일 위치
        [MenuItem("Project Delta/2일차/Windows 빌드")] // 편집기 빌드 메뉴
        public static void BuildWindows() // 2일차 Windows 시험 빌드
        { // 빌드 시작
            DayOneProjectSetup.BuildWindowsAt("Builds/Day02"); // 기존 실행 파일과 분리된 출력 경로
        } // 빌드 끝
        [MenuItem("Project Delta/2일차/던전 이동 구성")] // 편집기 구성 메뉴
        public static void Configure() // 기존 던전 씬의 이동 요소 추가
        { // 구성 시작
            if (!PrepareOpenScenes()) // 현재 열려 있는 씬 보존 검사
            { // 사용자 취소 조건 시작
                return; // 씬 변경 없이 작업 중단
            } // 사용자 취소 조건 끝
            var scene = EditorSceneManager.OpenScene(DungeonPath, OpenSceneMode.Single); // 기존 던전 씬 열기
            ConfigureCurrentScene(); // 바닥과 플레이어 연결
            EditorSceneManager.MarkSceneDirty(scene); // 씬 변경 사항 저장 대상으로 표시
            if (!EditorSceneManager.SaveScene(scene)) // 실제 씬 저장 결과 검사
            { // 저장 실패 조건 시작
                throw new InvalidOperationException("Dungeon scene could not be saved."); // 저장 오류 알림
            } // 저장 실패 조건 끝
            EditorSceneManager.OpenScene(DungeonPath, OpenSceneMode.Single); // 저장한 던전 씬 재개방
            var savedFloor = GameObject.Find("DungeonGrid/Floor").GetComponent<Tilemap>(); // 저장된 바닥 타일맵 조회
            var savedPlayer = GameObject.Find("PlayerPreview"); // 저장된 플레이어 조회
            var savedCell = savedFloor.WorldToCell(savedPlayer.transform.position); // 저장된 시작 위치의 격자 칸
            if (!savedFloor.HasTile(savedCell)) // 재개방 후 실제 시작 칸 검사
            { // 저장 검증 실패 시작
                throw new InvalidOperationException("Dungeon floor did not persist after reopening."); // 타일 저장 실패 알림
            } // 저장 검증 실패 끝
            EditorSceneManager.OpenScene("Assets/ProjectDelta/Scenes/BootstrapScene.unity", OpenSceneMode.Single); // 시작 씬으로 편집기 복귀
            AssetDatabase.SaveAssets(); // 새 표시 리소스 저장
            Debug.Log("DAY02_DUNGEON_SAVED_AND_REOPENED"); // 실제 씬 저장 검증 기록
        } // 구성 끝
        public static void ConfigureCurrentScene(bool seedPrototype = false) // 현재 던전의 이동 요소 연결
        { // 연결 시작
            var gridObject = GameObject.Find("DungeonGrid"); // 기존 격자 객체 조회
            var floorObject = GameObject.Find("DungeonGrid/Floor"); // 기존 바닥 객체 조회
            var player = GameObject.Find("PlayerPreview"); // 기존 플레이어 객체 조회
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath); // 저장된 격자 타일 조회
            if (gridObject == null || floorObject == null || player == null || tile == null) // 기존 구성 존재 검사
            { // 구성 누락 조건 시작
                throw new InvalidOperationException("Dungeon grid, floor, player, and grid tile are required."); // 누락된 구성 알림
            } // 구성 누락 조건 끝
            var floor = floorObject.GetComponent<Tilemap>(); // 바닥 타일맵 조회
            if (floor == null) // 바닥 구성 요소 검사
            { // 타일맵 누락 조건 시작
                throw new InvalidOperationException("Dungeon floor must have a Tilemap."); // 누락된 바닥 알림
            } // 타일맵 누락 조건 끝
            var seedEmptyFloor = floor.GetUsedTilesCount() == 0; // 기존 바닥이 완전히 비어 있는지 검사
            if (seedEmptyFloor) // 최초 시험용 바닥 생성 검사
            { // 빈 바닥 구성 시작
                for (var x = -6; x <= 6; x++) // 가로 바닥 칸 순회
                { // 가로 반복 시작
                    for (var y = -4; y <= 4; y++) // 세로 바닥 칸 순회
                    { // 세로 반복 시작
                        floor.SetTile(new Vector3Int(x, y, 0), tile); // 초기 방의 임시 바닥 배치
                    } // 세로 반복 끝
                } // 가로 반복 끝
                floor.CompressBounds(); // 초기 바닥 범위 갱신
            } // 빈 바닥 구성 끝
            var startingCell = floor.WorldToCell(player.transform.position); // 플레이어의 시작 격자 칸
            if (!floor.HasTile(startingCell)) // 시작 위치의 기존 바닥 검사
            { // 시작 칸 누락 조건 시작
                throw new InvalidOperationException("Player start must be on an existing floor tile."); // 기존 바닥 변경 없이 오류 알림
            } // 시작 칸 누락 조건 끝
            var obstacleTransform = gridObject.transform.Find("Obstacles"); // 기존 장애물 객체 조회
            if (obstacleTransform == null) // 장애물 객체 최초 생성 검사
            { // 최초 생성 조건 시작
                var obstacleObject = new GameObject("Obstacles", typeof(Tilemap), typeof(TilemapRenderer)); // 장애물 타일맵 생성
                obstacleObject.transform.SetParent(gridObject.transform, false); // 같은 격자에 장애물 배치
                obstacleObject.GetComponent<TilemapRenderer>().sortingOrder = 1; // 바닥 앞 장애물 표시
                obstacleTransform = obstacleObject.transform; // 생성한 장애물 객체 보관
            } // 최초 생성 조건 끝
            var obstacles = obstacleTransform.GetComponent<Tilemap>(); // 장애물 타일맵 조회
            if (obstacles == null) // 장애물 구성 요소 검사
            { // 장애물 누락 조건 시작
                throw new InvalidOperationException("Dungeon obstacles must have a Tilemap."); // 누락된 장애물 알림
            } // 장애물 누락 조건 끝
            if (IsLegacyPrototype(obstacles, tile)) // 이전 시험 장애물의 정확한 구성 검사
            { // 기존 시험 장애물 교체 시작
                var replacement = GetObstacleTile(); // 밝은 장애물 타일 조회
                obstacles.SetTile(new Vector3Int(3, 0, 0), replacement); // 오른쪽 시험 장애물 교체
                obstacles.SetTile(new Vector3Int(-3, 1, 0), replacement); // 왼쪽 위 시험 장애물 교체
                obstacles.SetTile(new Vector3Int(0, 3, 0), replacement); // 위쪽 시험 장애물 교체
                obstacles.color = Color.white; // 전용 타일의 원래 색상 적용
            } // 기존 시험 장애물 교체 끝
            if (seedEmptyFloor || seedPrototype) // 새 시험용 방의 장애물 배치 검사
            { // 시험 장애물 조건 시작
                var obstacleTile = GetObstacleTile(); // 전용 장애물 타일 조회
                AddObstacle(obstacles, obstacleTile, new Vector3Int(3, 0, 0)); // 오른쪽 시험 장애물
                AddObstacle(obstacles, obstacleTile, new Vector3Int(-3, 1, 0)); // 왼쪽 위 시험 장애물
                AddObstacle(obstacles, obstacleTile, new Vector3Int(0, 3, 0)); // 위쪽 시험 장애물
            } // 시험 장애물 조건 끝
            var indicator = player.transform.Find("DirectionIndicator"); // 기존 방향 표시 조회
            if (indicator == null) // 방향 표시 최초 생성 검사
            { // 방향 표시 생성 시작
                var arrowObject = new GameObject("DirectionIndicator", typeof(SpriteRenderer)); // 방향 화살표 객체 생성
                arrowObject.transform.SetParent(player.transform, false); // 플레이어 중심에 표시
                var arrowRenderer = arrowObject.GetComponent<SpriteRenderer>(); // 화살표 표시 구성 조회
                arrowRenderer.sprite = GetDirectionSprite(); // 직접 생성한 방향 표시 연결
                arrowRenderer.color = Color.white; // 선명한 흰색 표시
                arrowRenderer.sortingOrder = 3; // 바닥과 플레이어 앞 표시
                indicator = arrowObject.transform; // 생성한 방향 표시 보관
            } // 방향 표시 생성 끝
            var movement = player.GetComponent<GridMoveController>(); // 기존 이동 제어기 조회
            if (movement == null) // 이동 제어기 최초 생성 검사
            { // 이동 제어기 생성 시작
                movement = player.AddComponent<GridMoveController>(); // 실제 플레이어 이동 연결
            } // 이동 제어기 생성 끝
            movement.Configure(floor, obstacles, indicator); // 바닥과 장애물과 방향 표시 연결
            EditorUtility.SetDirty(floor); // 바닥 타일 변경 표시
            EditorUtility.SetDirty(obstacles); // 장애물 타일 변경 표시
            EditorUtility.SetDirty(movement); // 플레이어 연결 변경 표시
            EditorSceneManager.MarkSceneDirty(floor.gameObject.scene); // 현재 던전 씬 변경 표시
            if (!floor.HasTile(startingCell) || ((seedEmptyFloor || seedPrototype) && !obstacles.HasTile(new Vector3Int(0, 3, 0)))) // 새 방과 시작 칸 검사
            { // 타일 구성 실패 시작
                throw new InvalidOperationException("Dungeon floor or obstacle tiles were not created."); // 실제 구성 실패 알림
            } // 타일 구성 실패 끝
        } // 연결 끝
        private static bool PrepareOpenScenes() // 저장되지 않은 열린 씬 보호
        { // 검사 시작
            if (!Application.isBatchMode) // 사용자 편집기 실행 검사
            { // 대화형 실행 조건 시작
                return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(); // 기존 Unity 저장 또는 취소 선택
            } // 대화형 실행 조건 끝
            for (var index = 0; index < SceneManager.sceneCount; index++) // 자동 실행의 열린 씬 순회
            { // 열린 씬 검사 시작
                if (SceneManager.GetSceneAt(index).isDirty) // 저장되지 않은 변경 검사
                { // 변경 보호 조건 시작
                    throw new InvalidOperationException("Save or close modified scenes before configuring the dungeon."); // 자동 실행의 변경 유실 방지
                } // 변경 보호 조건 끝
            } // 열린 씬 검사 끝
            return true; // 변경 없이 씬 전환 허용
        } // 검사 끝
        private static void AddObstacle(Tilemap obstacles, Tile tile, Vector3Int cell) // 기존 칸을 보존하는 장애물 추가
        { // 추가 시작
            if (!obstacles.HasTile(cell)) // 기존 장애물 존재 검사
            { // 빈 칸 조건 시작
                obstacles.SetTile(cell, tile); // 빈 칸에 시험 장애물 배치
            } // 빈 칸 조건 끝
        } // 추가 끝
        private static bool IsLegacyPrototype(Tilemap obstacles, Tile tile) // 이전 시험 장애물 식별
        { // 식별 시작
            if (obstacles.color != new Color(2.8f, 0.65f, 0.55f) || obstacles.GetTile(new Vector3Int(3, 0, 0)) != tile || obstacles.GetTile(new Vector3Int(-3, 1, 0)) != tile || obstacles.GetTile(new Vector3Int(0, 3, 0)) != tile) // 기존 시험 배치와 색상 검사
            { // 불일치 조건 시작
                return false; // 사용자 장애물 보존
            } // 불일치 조건 끝
            var bounds = obstacles.cellBounds; // 기존 장애물 범위 조회
            if (bounds.size.x > 64 || bounds.size.y > 64 || bounds.size.z > 1) // 과도한 범위 순회 방지
            { // 넓은 범위 조건 시작
                return false; // 기존 구성 보존
            } // 넓은 범위 조건 끝
            var count = 0; // 실제 장애물 수
            foreach (var cell in bounds.allPositionsWithin) // 기존 범위의 칸 순회
            { // 칸 순회 시작
                if (obstacles.HasTile(cell)) // 장애물 존재 검사
                { // 장애물 칸 조건 시작
                    count++; // 실제 장애물 수 증가
                } // 장애물 칸 조건 끝
            } // 칸 순회 끝
            return count == 3; // 시험 장애물 세 개만 있는지 확인
        } // 식별 끝
        private static Tile GetObstacleTile() // 밝은 장애물 타일 준비
        { // 준비 시작
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(ObstacleTilePath); // 기존 장애물 타일 조회
            if (existing != null) // 저장된 타일 검사
            { // 기존 타일 조건 시작
                return existing; // 기존 타일 보존
            } // 기존 타일 조건 끝
            if (File.Exists(ObstacleTilePath)) // 다른 형식의 타일 파일 검사
            { // 파일 충돌 조건 시작
                throw new InvalidOperationException("Obstacle tile already exists but is not a Tile."); // 기존 파일 덮어쓰기 방지
            } // 파일 충돌 조건 끝
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ObstacleImagePath); // 기존 장애물 스프라이트 조회
            if (sprite == null) // 스프라이트 최초 생성 검사
            { // 이미지 생성 시작
                if (File.Exists(ObstacleImagePath)) // 다른 형식의 기존 이미지 검사
                { // 파일 충돌 조건 시작
                    throw new InvalidOperationException("Obstacle image already exists but is not a sprite."); // 기존 파일 덮어쓰기 방지
                } // 파일 충돌 조건 끝
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false); // 장애물 이미지 생성
                for (var y = 0; y < 16; y++) // 세로 픽셀 순회
                { // 세로 반복 시작
                    for (var x = 0; x < 16; x++) // 가로 픽셀 순회
                    { // 가로 반복 시작
                        var edge = x == 0 || x == 15 || y == 0 || y == 15; // 장애물 테두리 판정
                        texture.SetPixel(x, y, edge ? new Color(0.38f, 0.08f, 0.06f) : new Color(0.72f, 0.2f, 0.12f)); // 붉은 장애물 픽셀 설정
                    } // 가로 반복 끝
                } // 세로 반복 끝
                texture.Apply(); // 이미지 픽셀 반영
                File.WriteAllBytes(ObstacleImagePath, texture.EncodeToPNG()); // 장애물 이미지 저장
                UnityEngine.Object.DestroyImmediate(texture); // 제작용 이미지 메모리 정리
                AssetDatabase.ImportAsset(ObstacleImagePath); // 새 이미지 에셋 등록
                var importer = (TextureImporter)AssetImporter.GetAtPath(ObstacleImagePath); // 이미지 설정 조회
                importer.textureType = TextureImporterType.Sprite; // 2D 스프라이트 형식
                importer.spriteImportMode = SpriteImportMode.Single; // 단일 장애물 이미지
                importer.spritePixelsPerUnit = 16f; // 한 칸 기준 픽셀 크기
                importer.filterMode = FilterMode.Point; // 장애물 경계 선명도
                importer.mipmapEnabled = false; // 축소 이미지 생성 제외
                importer.SaveAndReimport(); // 스프라이트 설정 적용
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ObstacleImagePath); // 저장한 스프라이트 조회
            } // 이미지 생성 끝
            if (sprite == null) // 스프라이트 가져오기 검사
            { // 가져오기 실패 조건 시작
                throw new InvalidOperationException("Obstacle sprite could not be imported."); // 이미지 연결 오류 알림
            } // 가져오기 실패 조건 끝
            var result = ScriptableObject.CreateInstance<Tile>(); // 전용 타일 에셋 생성
            result.sprite = sprite; // 장애물 이미지 연결
            AssetDatabase.CreateAsset(result, ObstacleTilePath); // 전용 타일 에셋 저장
            return result; // 완성된 장애물 타일 전달
        } // 준비 끝
        private static Sprite GetDirectionSprite() // 자체 제작 방향 화살표
        { // 제작 시작
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath); // 기존 방향 스프라이트 조회
            if (existing != null) // 이미 저장된 표시 검사
            { // 기존 표시 조건 시작
                return existing; // 기존 스프라이트 보존
            } // 기존 표시 조건 끝
            if (File.Exists(ArrowPath)) // 다른 형식의 기존 파일 검사
            { // 파일 충돌 조건 시작
                throw new InvalidOperationException("Direction image already exists but is not a sprite."); // 기존 파일 덮어쓰기 방지
            } // 파일 충돌 조건 끝
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false); // 임시 화살표 이미지 생성
            for (var y = 0; y < 16; y++) // 세로 픽셀 순회
            { // 세로 반복 시작
                for (var x = 0; x < 16; x++) // 가로 픽셀 순회
                { // 가로 반복 시작
                    var shaft = y <= 8 && x >= 6 && x <= 9; // 화살표 몸통 판정
                    var tip = y >= 8 && Mathf.Abs(x - 7.5f) <= 15 - y; // 위쪽 화살표 끝 판정
                    texture.SetPixel(x, y, shaft || tip ? Color.white : Color.clear); // 표시와 투명 배경 설정
                } // 가로 반복 끝
            } // 세로 반복 끝
            texture.Apply(); // 이미지 픽셀 반영
            File.WriteAllBytes(ArrowPath, texture.EncodeToPNG()); // 방향 이미지 저장
            UnityEngine.Object.DestroyImmediate(texture); // 제작용 이미지 메모리 정리
            AssetDatabase.ImportAsset(ArrowPath); // 새 이미지 에셋 등록
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArrowPath); // 이미지 설정 조회
            importer.textureType = TextureImporterType.Sprite; // 2D 스프라이트 형식
            importer.spriteImportMode = SpriteImportMode.Single; // 단일 방향 이미지 사용
            importer.spritePixelsPerUnit = 16f; // 한 칸 기준 픽셀 크기
            importer.filterMode = FilterMode.Point; // 표시 경계 선명도
            importer.mipmapEnabled = false; // 축소 이미지 생성 제외
            importer.SaveAndReimport(); // 스프라이트 설정 적용
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowPath); // 저장한 스프라이트 조회
            if (sprite == null) // 가져온 이미지 검사
            { // 스프라이트 누락 조건 시작
                throw new InvalidOperationException("Direction sprite could not be imported."); // 이미지 연결 오류 알림
            } // 스프라이트 누락 조건 끝
            return sprite; // 완성된 방향 표시 전달
        } // 제작 끝
    } // 클래스 끝
} // 이름 공간 끝
