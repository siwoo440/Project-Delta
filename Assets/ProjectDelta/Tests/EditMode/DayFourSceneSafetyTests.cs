using System; // 구성 오류 형식
using NUnit.Framework; // 검증 도구
using ProjectDelta.Core; // 방 출구 방향
using ProjectDelta.Editor; // 지도 구성 도구
using ProjectDelta.Runtime; // 방 이동 제어기
using UnityEditor; // 기존 타일 에셋 조회
using UnityEditor.SceneManagement; // 검증 씬 관리
using UnityEngine; // 검증용 씬 객체
using UnityEngine.SceneManagement; // 활성 씬 관리
using UnityEngine.Tilemaps; // 바닥과 장애물 타일맵
namespace ProjectDelta.Tests // 검증 이름 공간
{ // 이름 공간 시작
    public sealed class DayFourSceneSafetyTests // 지도 구성의 기존 씬 보호 검증
    { // 클래스 시작
        private Scene previousScene; // 복구할 기존 씬
        private Scene testScene; // 저장하지 않는 시험 씬
        [SetUp] // 검증 준비 표시
        public void CreateTemporaryScene() // 분리된 검증 씬 준비
        { // 준비 시작
            if (!Application.isBatchMode) // 사용자 편집 씬 보호 검사
            { // 자동 실행 한정 조건 시작
                Assert.Ignore("Batch mode is required for isolated scene safety checks."); // 사용자 씬 변경 방지
            } // 자동 실행 한정 조건 끝
            EditorSceneManager.OpenScene("Assets/ProjectDelta/Scenes/BootstrapScene.unity", OpenSceneMode.Single); // 저장된 검증 씬 열기
            previousScene = SceneManager.GetActiveScene(); // 기존 씬 보관
            testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive); // 별도 시험 씬 생성
            SceneManager.SetActiveScene(testScene); // 새 객체의 시험 씬 지정
        } // 준비 끝
        [TearDown] // 검증 정리 표시
        public void RestorePreviousScene() // 시험 씬과 활성 상태 복구
        { // 정리 시작
            if (testScene.IsValid() && testScene.isLoaded) // 시험 씬 존재 검사
            { // 정리 조건 시작
                EditorSceneManager.CloseScene(testScene, true); // 저장 없이 시험 씬 닫기
            } // 정리 조건 끝
            if (previousScene.IsValid() && previousScene.isLoaded) // 이전 씬 존재 검사
            { // 복구 조건 시작
                SceneManager.SetActiveScene(previousScene); // 이전 씬 활성화
            } // 복구 조건 끝
        } // 정리 끝
        [Test] // 개별 검증 표시
        public void Configure_DirtyScene_RemainsOpenAndUnchanged() // 미저장 씬 보호
        { // 검증 시작
            var marker = new GameObject("UnsavedMapMarker"); // 사용자 미저장 변경 모사
            EditorSceneManager.MarkSceneDirty(testScene); // 미저장 상태 표시
            Assert.Throws<InvalidOperationException>(() => DayFourProjectSetup.Configure()); // 자동 씬 전환 거부
            Assert.That(testScene.isLoaded, Is.True); // 사용자 씬 유지
            Assert.That(marker, Is.Not.Null); // 사용자 객체 유지
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_ConflictingThirdRoom_IsPreserved() // 기존 세 번째 방 보호
        { // 검증 시작
            var existing = new GameObject("ThirdDungeonGrid"); // 사용자 소유 방 객체
            Assert.Throws<InvalidOperationException>(() => DayFourProjectSetup.ConfigureCurrentScene()); // 불일치한 기존 방 거부
            Assert.That(existing, Is.Not.Null); // 기존 방 객체 유지
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_OccupiedExit_PreservesTileAndCreatesNoRoom() // 기존 문 위치 타일 보호
        { // 검증 시작
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/ObstacleTile.asset"); // 기존 장애물 타일
            var floorTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/GridTile.asset"); // 기존 바닥 타일
            var first = new GameObject("DungeonGrid", typeof(Grid)); // 첫 번째 방 객체 생성
            var second = new GameObject("SecondDungeonGrid", typeof(Grid)); // 두 번째 방 객체 생성
            var floorObject = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer)); // 기존 바닥 생성
            floorObject.transform.SetParent(second.transform, false); // 바닥의 방 연결
            var floor = floorObject.GetComponent<Tilemap>(); // 바닥 타일맵 조회
            var cells = new[] // 새 출구와 도착 칸 목록
            { // 칸 목록 시작
                new Vector3Int(2, 4, 0), // 위쪽 출구
                new Vector3Int(2, 3, 0), // 위쪽 문 앞 칸
                new Vector3Int(2, -4, 0), // 아래쪽 출구
                new Vector3Int(2, -3, 0) // 아래쪽 문 앞 칸
            }; // 칸 목록 끝
            foreach (var cell in cells) // 필요한 바닥 칸 순회
            { // 바닥 순회 시작
                floor.SetTile(cell, floorTile); // 기존 바닥 설정
            } // 바닥 순회 끝
            var obstacleObject = new GameObject("Obstacles", typeof(Tilemap), typeof(TilemapRenderer)); // 기존 장애물 생성
            obstacleObject.transform.SetParent(second.transform, false); // 장애물의 방 연결
            var obstacles = obstacleObject.GetComponent<Tilemap>(); // 장애물 타일맵 조회
            obstacles.SetTile(new Vector3Int(-6, 0, 0), tile); // 기존 왼쪽 문 칸
            obstacles.SetTile(new Vector3Int(2, 4, 0), floorTile); // 새 출구 칸의 사용자 타일
            new GameObject("DoorMarker").transform.SetParent(second.transform, false); // 기존 문 표시 모사
            second.SetActive(false); // 기존 두 번째 방 비활성
            var player = new GameObject("PlayerPreview", typeof(GridMoveController)).GetComponent<GridMoveController>(); // 기존 플레이어 모사
            var travel = new GameObject("RoomTravel", typeof(RoomTravelController)).GetComponent<RoomTravelController>(); // 기존 방 이동 모사
            travel.Configure(first, second, player); // 기존 시험 방 연결 모사
            Assert.Throws<InvalidOperationException>(() => DayFourProjectSetup.ConfigureCurrentScene()); // 점유된 출구 칸 거부
            Assert.That(obstacles.GetTile(new Vector3Int(2, 4, 0)), Is.SameAs(floorTile)); // 사용자 타일 보존
            Assert.That(GameObject.Find("ThirdDungeonGrid"), Is.Null); // 새 방 생성 방지
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void LegacyTwoRoomReferences_AreRecognizedWithoutChangingThem() // 3일차 씬 참조 보존 검증
        { // 검증 시작
            var first = new GameObject("DungeonGrid"); // 기존 첫 번째 방
            var second = new GameObject("SecondDungeonGrid"); // 기존 두 번째 방
            var player = new GameObject("PlayerPreview", typeof(GridMoveController)).GetComponent<GridMoveController>(); // 기존 플레이어
            var travel = new GameObject("RoomTravel", typeof(RoomTravelController)).GetComponent<RoomTravelController>(); // 기존 방 이동 제어기
            var serialized = new SerializedObject(travel); // 기존 직렬화 상태 조회
            serialized.FindProperty("firstRoom").objectReferenceValue = first; // 3일차 첫 방 참조 설정
            serialized.FindProperty("secondRoom").objectReferenceValue = second; // 3일차 두 번째 방 참조 설정
            serialized.FindProperty("player").objectReferenceValue = player; // 3일차 플레이어 참조 설정
            serialized.ApplyModifiedPropertiesWithoutUndo(); // 기존 씬 형식 적용
            var rooms = new[] // 예상하는 기존 방 연결
            { // 방 목록 시작
                new RoomBinding(0, 0, false, first), // 첫 방
                new RoomBinding(1, 0, false, second) // 두 번째 방
            }; // 방 목록 끝
            var doors = new[] // 기존 기본 출구
            { // 출구 목록 시작
                new DoorBinding(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0), // 첫 방 오른쪽 출구
                new DoorBinding(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0) // 두 번째 방 왼쪽 출구
            }; // 출구 목록 끝
            Assert.That(travel.HasExactConfiguration(rooms, doors, player), Is.True); // 기존 두 방 형식 인식 확인
            Assert.That(serialized.FindProperty("firstRoom").objectReferenceValue, Is.SameAs(first)); // 기존 첫 방 참조 유지
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_ThirdRoomWithMissingDoors_IsRejected() // 불완전한 기존 세 번째 방 검사
        { // 검증 시작
            var first = new GameObject("DungeonGrid"); // 첫 번째 방
            var second = new GameObject("SecondDungeonGrid"); // 두 번째 방
            var third = new GameObject("ThirdDungeonGrid"); // 기존 세 번째 방
            var player = new GameObject("PlayerPreview", typeof(GridMoveController)).GetComponent<GridMoveController>(); // 플레이어 객체
            var travel = new GameObject("RoomTravel", typeof(RoomTravelController)).GetComponent<RoomTravelController>(); // 방 이동 객체
            var rooms = new[] // 세 방의 씬 연결
            { // 방 목록 시작
                new RoomBinding(0, 0, false, first), // 첫 방
                new RoomBinding(1, 0, false, second), // 두 번째 방
                new RoomBinding(1, 1, false, third) // 세 번째 방
            }; // 방 목록 끝
            travel.Configure(rooms, new DoorBinding[0], player); // 누락된 출구 정보 설정
            Assert.Throws<InvalidOperationException>(() => DayFourProjectSetup.ConfigureCurrentScene()); // 불완전한 연결 거부
            Assert.That(third, Is.Not.Null); // 사용자 방 보존 확인
        } // 검증 끝
        [Test] // 개별 검증 표시
        public void ConfigureCurrentScene_CustomDoorConfiguration_IsNotOverwritten() // 사용자 방 연결 보존 검증
        { // 검증 시작
            var obstacleTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/ObstacleTile.asset"); // 기존 시험 장애물
            var floorTile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/GridTile.asset"); // 기존 시험 바닥
            var first = new GameObject("DungeonGrid", typeof(Grid)); // 첫 번째 방
            var second = new GameObject("SecondDungeonGrid", typeof(Grid)); // 두 번째 방
            var floorObject = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer)); // 기존 바닥
            floorObject.transform.SetParent(second.transform, false); // 바닥의 방 연결
            var floor = floorObject.GetComponent<Tilemap>(); // 바닥 타일맵 조회
            var required = new[] // 새 문과 도착 위치
            { // 칸 목록 시작
                new Vector3Int(2, 4, 0), // 위쪽 문 칸
                new Vector3Int(2, 3, 0), // 위쪽 문 앞 칸
                new Vector3Int(2, -4, 0), // 아래쪽 문 칸
                new Vector3Int(2, -3, 0) // 아래쪽 문 앞 칸
            }; // 칸 목록 끝
            foreach (var cell in required) // 필요한 바닥 순회
            { // 바닥 순회 시작
                floor.SetTile(cell, floorTile); // 기존 바닥 설정
            } // 바닥 순회 끝
            var obstacleObject = new GameObject("Obstacles", typeof(Tilemap), typeof(TilemapRenderer)); // 기존 장애물
            obstacleObject.transform.SetParent(second.transform, false); // 장애물의 방 연결
            obstacleObject.GetComponent<Tilemap>().SetTile(new Vector3Int(-6, 0, 0), obstacleTile); // 기존 왼쪽 문
            new GameObject("DoorMarker").transform.SetParent(second.transform, false); // 기존 문 표시
            second.SetActive(false); // 기존 두 번째 방 비활성
            var player = new GameObject("PlayerPreview", typeof(GridMoveController)).GetComponent<GridMoveController>(); // 플레이어 객체
            var travel = new GameObject("RoomTravel", typeof(RoomTravelController)).GetComponent<RoomTravelController>(); // 방 이동 객체
            var rooms = new[] // 기존 두 방 연결
            { // 방 목록 시작
                new RoomBinding(0, 0, false, first), // 첫 방
                new RoomBinding(1, 0, false, second) // 두 번째 방
            }; // 방 목록 끝
            var doors = new[] // 사용자가 바꾼 기존 출구 칸
            { // 문 목록 시작
                new DoorBinding(0, 0, 4, 0, MoveDirection.Right, 1, 0, -5, 0), // 사용자 변경 문
                new DoorBinding(1, 0, -5, 0, MoveDirection.Left, 0, 0, 4, 0) // 사용자 변경 역방향 문
            }; // 문 목록 끝
            travel.Configure(rooms, doors, player); // 사용자 연결 저장
            Assert.Throws<InvalidOperationException>(() => DayFourProjectSetup.ConfigureCurrentScene()); // 사용자 연결 덮어쓰기 거부
            var stored = new SerializedObject(travel).FindProperty("doors"); // 저장된 출구 속성 조회
            Assert.That(stored.GetArrayElementAtIndex(0).FindPropertyRelative("ApproachX").intValue, Is.EqualTo(4)); // 사용자 출구 좌표 보존
        } // 검증 끝
    } // 클래스 끝
} // 이름 공간 끝
