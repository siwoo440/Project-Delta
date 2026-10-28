using System; // 구성 오류 형식
using NUnit.Framework; // 검증 도구
using ProjectDelta.Editor; // 방 구성 도구
using ProjectDelta.Runtime; // 플레이어 이동 구성
using UnityEditor; // 기존 타일 조회
using UnityEditor.SceneManagement; // 검증 씬 관리
using UnityEngine; // 검증용 객체
using UnityEngine.SceneManagement; // 활성 씬 관리
using UnityEngine.Tilemaps; // 실제 바닥과 장애물
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class DayThreeSceneSafetyTests // 방 구성의 사용자 변경 보존 검증
    { // 클래스 시작
        private Scene previousScene; // 복구할 기존 씬
        private Scene testScene; // 저장하지 않는 임시 씬
        [SetUp] // 검증 준비 표시
        public void CreateTemporaryScene() // 분리된 시험 씬 준비
        { // 준비 시작
            if (!Application.isBatchMode) // 사용자 편집 씬 보호 검사
            { // 자동 실행 한정 조건 시작
                Assert.Ignore("Batch mode is required for isolated scene safety checks."); // 사용자 씬 변경 방지
            } // 자동 실행 한정 조건 끝
            EditorSceneManager.OpenScene("Assets/ProjectDelta/Scenes/BootstrapScene.unity", OpenSceneMode.Single); // 저장된 자동 검증 씬
            previousScene = SceneManager.GetActiveScene(); // 기존 씬 보관
            testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); // 검증용 별도 씬
            SceneManager.SetActiveScene(testScene); // 검증 객체의 씬 지정
        } // 준비 끝
        [TearDown] // 검증 정리 표시
        public void RestorePreviousScene() // 임시 씬 정리
        { // 정리 시작
            if (testScene.IsValid() && testScene.isLoaded) // 임시 씬 검사
            { // 정리 조건 시작
                EditorSceneManager.CloseScene(testScene, true); // 저장 없이 시험 씬 닫기
            } // 정리 조건 끝
            if (previousScene.IsValid() && previousScene.isLoaded) // 기존 씬 검사
            { // 복구 조건 시작
                SceneManager.SetActiveScene(previousScene); // 이전 씬 활성화
            } // 복구 조건 끝
        } // 정리 끝
        [Test] // 개별 검증 표시
        public void Configure_WithDirtyScene_RejectsWithoutClosing() // 미저장 씬 보호 검증
        { // 검증 시작
            var marker = new GameObject("UnsavedRoomMarker"); // 미저장 사용자 객체 모사
            EditorSceneManager.MarkSceneDirty(testScene); // 미저장 상태 표시
            Assert.Throws<InvalidOperationException>(() => DayThreeProjectSetup.Configure()); // 자동 전환 거부 확인
            Assert.That(testScene.isLoaded, Is.True); // 사용자 씬 유지 확인
            Assert.That(marker, Is.Not.Null); // 미저장 객체 유지 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_PreservesConflictingExistingRoom() // 사용자 방 객체 보호 검증
        { // 검증 시작
            var existing = new GameObject("SecondDungeonGrid"); // 기존 사용자 방 객체
            Assert.Throws<InvalidOperationException>(() => DayThreeProjectSetup.ConfigureCurrentScene()); // 불완전한 구성 거절
            Assert.That(existing, Is.Not.Null); // 기존 객체 유지 확인
            Assert.That(GameObject.Find("RoomTravel"), Is.Null); // 새 구성 생성 방지
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_RejectsOccupiedDoorCell() // 기존 장애물 덮어쓰기 방지 검증
        { // 검증 시작
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/GridTile.asset"); // 기존 바닥 타일
            var grid = new GameObject("DungeonGrid", typeof(Grid)); // 검증용 격자 생성
            var floorObject = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer)); // 검증용 바닥
            floorObject.transform.SetParent(grid.transform, false); // 바닥을 격자에 연결
            var floor = floorObject.GetComponent<Tilemap>(); // 바닥 타일맵 조회
            var doorColumns = new[] // 문과 도착 칸 목록
            { // 목록 시작
                -6, -5, 5, 6 // 문과 도착 칸 가로 좌표
            }; // 목록 끝
            foreach (var x in doorColumns) // 문과 도착 칸 순회
            { // 칸 순회 시작
                floor.SetTile(new Vector3Int(x, 0, 0), tile); // 필요한 바닥 배치
            } // 칸 순회 끝
            var obstacleObject = new GameObject("Obstacles", typeof(Tilemap), typeof(TilemapRenderer)); // 기존 장애물 생성
            obstacleObject.transform.SetParent(grid.transform, false); // 장애물을 격자에 연결
            var obstacles = obstacleObject.GetComponent<Tilemap>(); // 장애물 타일맵 조회
            obstacles.SetTile(new Vector3Int(6, 0, 0), tile); // 사용자 장애물 배치
            new GameObject("PlayerPreview", typeof(GridMoveController)); // 검증용 이동 구성
            Assert.Throws<InvalidOperationException>(() => DayThreeProjectSetup.ConfigureCurrentScene()); // 점유된 문 칸 거부
            Assert.That(obstacles.GetTile(new Vector3Int(6, 0, 0)), Is.SameAs(tile)); // 기존 타일 보존
            Assert.That(GameObject.Find("RoomTravel"), Is.Null); // 불완전한 방 연결 생성 방지
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
