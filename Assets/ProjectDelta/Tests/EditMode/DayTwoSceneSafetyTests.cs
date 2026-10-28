using System; // 구성 오류 형식
using NUnit.Framework; // 검증 도구
using ProjectDelta.Editor; // 던전 씬 구성 도구
using UnityEditor; // 기존 타일 에셋 조회
using UnityEditor.SceneManagement; // 임시 씬 관리
using UnityEngine; // 임시 씬 객체
using UnityEngine.SceneManagement; // 활성 씬 관리
using UnityEngine.Tilemaps; // 실제 바닥 타일
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class DayTwoSceneSafetyTests // 씬 편집 보존 검증
    { // 클래스 시작
        private Scene previousScene; // 복구할 기존 씬
        private Scene testScene; // 검증 전용 임시 씬
        [SetUp] // 검증 전 준비 표시
        public void CreateTemporaryScene() // 기존 씬과 분리된 시험 환경
        { // 준비 시작
            if (!Application.isBatchMode) // 사용자 편집 씬 보호 검사
            { // 자동 실행 한정 조건 시작
                Assert.Ignore("Batch mode is required for isolated scene safety checks."); // 편집기 사용자 씬 변경 방지
            } // 자동 실행 한정 조건 끝
            EditorSceneManager.OpenScene("Assets/ProjectDelta/Scenes/BootstrapScene.unity", OpenSceneMode.Single); // 자동 검증용 저장된 시작 씬
            previousScene = SceneManager.GetActiveScene(); // 기존 활성 씬 보관
            testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); // 저장하지 않는 별도 씬 생성
            SceneManager.SetActiveScene(testScene); // 생성 객체의 임시 씬 지정
        } // 준비 끝
        [TearDown] // 검증 후 정리 표시
        public void RestorePreviousScene() // 시험 씬 정리와 기존 씬 복구
        { // 정리 시작
            if (testScene.IsValid() && testScene.isLoaded) // 남아 있는 시험 씬 검사
            { // 임시 씬 정리 시작
                EditorSceneManager.CloseScene(testScene, true); // 검증용 임시 씬 닫기
            } // 임시 씬 정리 끝
            if (previousScene.IsValid() && previousScene.isLoaded) // 기존 씬 존재 검사
            { // 기존 씬 복구 시작
                SceneManager.SetActiveScene(previousScene); // 기존 활성 씬 복구
            } // 기존 씬 복구 끝
        } // 정리 끝
        [Test] // 개별 검증 표시
        public void Configure_WithDirtyOpenScene_RejectsWithoutClosingIt() // 저장되지 않은 열린 씬 보호 검증
        { // 검증 시작
            var marker = new GameObject("UnsavedSceneMarker"); // 저장되지 않은 사용자 변경 모사
            EditorSceneManager.MarkSceneDirty(testScene); // 미저장 상태 표시
            Assert.Throws<InvalidOperationException>(() => DayTwoProjectSetup.Configure()); // 씬 전환 거절 확인
            Assert.That(testScene.isLoaded, Is.True); // 변경된 씬 유지 확인
            Assert.That(marker, Is.Not.Null); // 미저장 객체 유지 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_PreservesExistingFloorHoles() // 기존 바닥 구멍 보존 검증
        { // 검증 시작
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/GridTile.asset"); // 기존 격자 타일 조회
            Assert.That(tile, Is.Not.Null); // 시험용 타일 존재 확인
            var grid = new GameObject("DungeonGrid", typeof(Grid)); // 시험 격자 생성
            var floorObject = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer)); // 시험 바닥 생성
            floorObject.transform.SetParent(grid.transform, false); // 바닥의 격자 연결
            var floor = floorObject.GetComponent<Tilemap>(); // 시험 바닥 조회
            floor.SetTile(Vector3Int.zero, tile); // 실제 시작 칸 배치
            floor.SetTile(Vector3Int.right, tile); // 기존 바닥의 다른 칸 배치
            new GameObject("PlayerPreview", typeof(SpriteRenderer)); // 시작 칸 플레이어 생성
            DayTwoProjectSetup.ConfigureCurrentScene(); // 실제 던전 연결 실행
            Assert.That(floor.HasTile(Vector3Int.up), Is.False); // 기존 바닥 구멍 보존 확인
            Assert.That(floor.HasTile(Vector3Int.zero), Is.True); // 기존 시작 칸 보존 확인
            var obstacles = GameObject.Find("DungeonGrid/Obstacles").GetComponent<Tilemap>(); // 연결된 장애물 조회
            Assert.That(obstacles.HasTile(new Vector3Int(0, 3, 0)), Is.False); // 기존 맵에 시험 장애물 추가 방지
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_SeedsNewPrototypeWithoutChangingExistingTiles() // 새 시험 방 구성 검증
        { // 검증 시작
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/GridTile.asset"); // 기존 격자 타일 조회
            var grid = new GameObject("DungeonGrid", typeof(Grid)); // 빈 시험 격자 생성
            var floorObject = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer)); // 빈 시험 바닥 생성
            floorObject.transform.SetParent(grid.transform, false); // 바닥의 격자 연결
            new GameObject("PlayerPreview", typeof(SpriteRenderer)); // 시작 칸 플레이어 생성
            DayTwoProjectSetup.ConfigureCurrentScene(); // 빈 던전의 실제 연결 실행
            var floor = floorObject.GetComponent<Tilemap>(); // 구성된 바닥 조회
            var obstacles = GameObject.Find("DungeonGrid/Obstacles").GetComponent<Tilemap>(); // 구성된 장애물 조회
            Assert.That(floor.GetTile(Vector3Int.zero), Is.SameAs(tile)); // 시작 칸 바닥 생성 확인
            Assert.That(floor.HasTile(new Vector3Int(-6, -4, 0)), Is.True); // 초기 방 경계 생성 확인
            Assert.That(obstacles.HasTile(new Vector3Int(0, 3, 0)), Is.True); // 초기 시험 장애물 생성 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_WithPrebuiltPrototype_AddsTrialObstacles() // 1일차 씬 재생성 연결 검증
        { // 검증 시작
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/GridTile.asset"); // 기존 격자 타일 조회
            var grid = new GameObject("DungeonGrid", typeof(Grid)); // 재생성 시험 격자
            var floorObject = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer)); // 미리 생성한 바닥
            floorObject.transform.SetParent(grid.transform, false); // 바닥의 격자 연결
            var floor = floorObject.GetComponent<Tilemap>(); // 시험 바닥 조회
            floor.SetTile(Vector3Int.zero, tile); // 기존 씬 생성기의 바닥 모사
            new GameObject("PlayerPreview", typeof(SpriteRenderer)); // 시작 칸 플레이어 생성
            DayTwoProjectSetup.ConfigureCurrentScene(true); // 새 시험 방의 명시적 구성
            var obstacles = GameObject.Find("DungeonGrid/Obstacles").GetComponent<Tilemap>(); // 구성된 장애물 조회
            Assert.That(obstacles.HasTile(new Vector3Int(0, 3, 0)), Is.True); // 새 시험 방 장애물 연결 확인
            Assert.That(floor.HasTile(Vector3Int.zero), Is.True); // 기존 시험 바닥 유지 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_UpgradesOnlyMatchingLegacyPrototype() // 기존 시험 장애물의 안전한 교체 검증
        { // 검증 시작
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/GridTile.asset"); // 기존 어두운 바닥 타일
            var grid = new GameObject("DungeonGrid", typeof(Grid)); // 시험 격자 생성
            var floorObject = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer)); // 기존 바닥 생성
            floorObject.transform.SetParent(grid.transform, false); // 바닥의 격자 연결
            floorObject.GetComponent<Tilemap>().SetTile(Vector3Int.zero, tile); // 시작 칸 보존
            var obstacleObject = new GameObject("Obstacles", typeof(Tilemap), typeof(TilemapRenderer)); // 기존 시험 장애물 생성
            obstacleObject.transform.SetParent(grid.transform, false); // 장애물의 격자 연결
            var obstacles = obstacleObject.GetComponent<Tilemap>(); // 기존 장애물 조회
            obstacles.color = new Color(2.8f, 0.65f, 0.55f); // 이전 시험 장애물의 색상
            obstacles.SetTile(new Vector3Int(3, 0, 0), tile); // 기존 오른쪽 장애물
            obstacles.SetTile(new Vector3Int(-3, 1, 0), tile); // 기존 왼쪽 위 장애물
            obstacles.SetTile(new Vector3Int(0, 3, 0), tile); // 기존 위쪽 장애물
            obstacles.CompressBounds(); // 실제 장애물 범위 갱신
            new GameObject("PlayerPreview", typeof(SpriteRenderer)); // 시작 칸 플레이어 생성
            DayTwoProjectSetup.ConfigureCurrentScene(); // 실제 던전 구성 실행
            Assert.That(obstacles.GetTile(new Vector3Int(3, 0, 0)), Is.Not.SameAs(tile)); // 시험 장애물의 전용 타일 교체 확인
            Assert.That(obstacles.GetTile(new Vector3Int(-3, 1, 0)), Is.Not.SameAs(tile)); // 두 번째 시험 장애물 교체 확인
            Assert.That(obstacles.GetTile(new Vector3Int(0, 3, 0)), Is.Not.SameAs(tile)); // 세 번째 시험 장애물 교체 확인
            Assert.That(obstacles.color, Is.EqualTo(Color.white)); // 새 타일의 본래 색상 유지 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_PreservesCustomObstacleTilesAndColor() // 사용자 장애물 보존 검증
        { // 검증 시작
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/GridTile.asset"); // 기존 바닥 타일 조회
            var grid = new GameObject("DungeonGrid", typeof(Grid)); // 시험 격자 생성
            var floorObject = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer)); // 사용자 바닥 생성
            floorObject.transform.SetParent(grid.transform, false); // 바닥의 격자 연결
            floorObject.GetComponent<Tilemap>().SetTile(Vector3Int.zero, tile); // 시작 칸 배치
            var obstacleObject = new GameObject("Obstacles", typeof(Tilemap), typeof(TilemapRenderer)); // 사용자 장애물 생성
            obstacleObject.transform.SetParent(grid.transform, false); // 장애물의 격자 연결
            var obstacles = obstacleObject.GetComponent<Tilemap>(); // 사용자 장애물 조회
            var customColor = new Color(2.8f, 0.65f, 0.55f); // 기존 색상과 같은 사용자 색상
            obstacles.color = customColor; // 사용자 장애물 색상 지정
            obstacles.SetTile(new Vector3Int(2, 2, 0), tile); // 사용자 위치의 장애물 배치
            obstacles.CompressBounds(); // 사용자 장애물 범위 갱신
            new GameObject("PlayerPreview", typeof(SpriteRenderer)); // 시작 칸 플레이어 생성
            DayTwoProjectSetup.ConfigureCurrentScene(); // 실제 던전 구성 실행
            Assert.That(obstacles.GetTile(new Vector3Int(2, 2, 0)), Is.SameAs(tile)); // 사용자 장애물 종류 보존
            Assert.That(obstacles.color, Is.EqualTo(customColor)); // 사용자 장애물 색상 보존
            Assert.That(obstacles.HasTile(new Vector3Int(0, 3, 0)), Is.False); // 시험 장애물 추가 방지
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
