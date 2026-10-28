using System; // 씬 구성 오류
using ProjectDelta.Core; // 출구 방향 규칙
using ProjectDelta.Runtime; // 방 데이터와 화면 연결
using UnityEditor; // 제작 도구
using UnityEditor.SceneManagement; // 씬 열기와 저장
using UnityEngine; // 씬 객체와 문 표시
using UnityEngine.SceneManagement; // 열린 씬 검사
using UnityEngine.Tilemaps; // 바닥과 장애물 타일
namespace ProjectDelta.Editor // 제작 도구 이름 공간
{ // 이름 공간 시작
    public static class DayFourProjectSetup // 세 시험 방과 지도 연결
    { // 클래스 시작
        private const string DungeonPath = "Assets/ProjectDelta/Scenes/DungeonScene.unity"; // 기존 던전 씬 위치
        [MenuItem("Project Delta/4일차/Windows 빌드")] // 편집기 빌드 메뉴
        public static void BuildWindows() // 4일차 Windows 시험 빌드
        { // 빌드 시작
            DayOneProjectSetup.BuildWindowsAt("Builds/Day04"); // 기존 실행 파일과 분리된 출력 경로
        } // 빌드 끝
        [MenuItem("Project Delta/4일차/시험 방과 지도 구성")] // 편집기 구성 메뉴
        public static void Configure() // 기존 던전에 세 번째 방 연결
        { // 구성 시작
            if (!PrepareOpenScenes()) // 미저장 씬 보존 검사
            { // 사용자 취소 조건 시작
                return; // 씬 변경 없이 종료
            } // 사용자 취소 조건 끝
            var scene = EditorSceneManager.OpenScene(DungeonPath, OpenSceneMode.Single); // 기존 던전 씬 열기
            ConfigureCurrentScene(); // 세 시험 방과 출구 연결
            if (!EditorSceneManager.SaveScene(scene)) // 실제 씬 저장 검사
            { // 저장 실패 조건 시작
                throw new InvalidOperationException("Dungeon scene could not be saved."); // 저장 오류 알림
            } // 저장 실패 조건 끝
            var saved = EditorSceneManager.OpenScene(DungeonPath, OpenSceneMode.Single); // 저장한 씬 재개방
            var third = FindRoot(saved, "ThirdDungeonGrid"); // 저장된 세 번째 방 조회
            var travelObject = FindRoot(saved, "RoomTravel"); // 저장된 방 이동 객체 조회
            var travel = travelObject == null ? null : travelObject.GetComponent<RoomTravelController>(); // 저장된 방 이동 제어기
            var first = FindRoot(saved, "DungeonGrid"); // 저장된 첫 번째 방
            var second = FindRoot(saved, "SecondDungeonGrid"); // 저장된 두 번째 방
            var playerObject = FindRoot(saved, "PlayerPreview"); // 저장된 플레이어 객체
            var movement = playerObject == null ? null : playerObject.GetComponent<GridMoveController>(); // 저장된 이동 제어기
            if (first == null || second == null || third == null || travel == null || movement == null || !travel.HasExactConfiguration(CreateRooms(first, second, third), CreateDoors(true), movement)) // 실제 저장된 전체 방과 출구 연결 검사
            { // 재개방 실패 조건 시작
                throw new InvalidOperationException("Third room binding did not persist after reopening."); // 씬 저장 검증 오류
            } // 재개방 실패 조건 끝
            EditorSceneManager.OpenScene("Assets/ProjectDelta/Scenes/BootstrapScene.unity", OpenSceneMode.Single); // 시작 씬으로 복귀
            Debug.Log("DAY04_DUNGEON_SAVED_AND_REOPENED"); // 씬 저장 검증 기록
        } // 구성 끝
        public static void ConfigureCurrentScene() // 현재 던전의 세 번째 시험 방 연결
        { // 연결 시작
            var scene = SceneManager.GetActiveScene(); // 현재 던전 씬 조회
            var first = FindRoot(scene, "DungeonGrid"); // 기존 첫 번째 방
            var second = FindRoot(scene, "SecondDungeonGrid"); // 기존 두 번째 방
            var third = FindRoot(scene, "ThirdDungeonGrid"); // 기존 세 번째 방
            var travelObject = FindRoot(scene, "RoomTravel"); // 기존 방 이동 객체
            var travel = travelObject == null ? null : travelObject.GetComponent<RoomTravelController>(); // 방 이동 제어기 조회
            var playerObject = FindRoot(scene, "PlayerPreview"); // 기존 플레이어 조회
            var movement = playerObject == null ? null : playerObject.GetComponent<GridMoveController>(); // 플레이어 이동 제어기
            if (third != null) // 기존 세 번째 방 검사
            { // 기존 객체 조건 시작
                if (first == null || second == null || travel == null || movement == null || !travel.HasExactConfiguration(CreateRooms(first, second, third), CreateDoors(true), movement)) // 기존 모든 방과 출구 검사
                { // 충돌 조건 시작
                    throw new InvalidOperationException("Existing third room must be inspected before configuration."); // 사용자 객체 덮어쓰기 방지
                } // 충돌 조건 끝
                return; // 이미 구성된 방과 연결 보존
            } // 기존 객체 조건 끝
            var floorObject = second == null ? null : second.transform.Find("Floor"); // 두 번째 방 바닥 조회
            var obstacleObject = second == null ? null : second.transform.Find("Obstacles"); // 두 번째 방 장애물 조회
            var oldMarker = second == null ? null : second.transform.Find("DoorMarker"); // 기존 왼쪽 문 표시 조회
            var floor = floorObject == null ? null : floorObject.GetComponent<Tilemap>(); // 두 번째 방 바닥 타일맵
            var obstacles = obstacleObject == null ? null : obstacleObject.GetComponent<Tilemap>(); // 두 번째 방 장애물 타일맵
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/ObstacleTile.asset"); // 기존 시험 장애물 타일
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectDelta/Art/Prototype/DirectionArrow.png"); // 기존 문 표시 이미지
            if (first == null || second == null || travel == null || movement == null || floor == null || obstacles == null || oldMarker == null || tile == null || sprite == null || !first.activeSelf || second.activeSelf) // 기존 시험 던전 검사
            { // 구성 누락 조건 시작
                throw new InvalidOperationException("The original two-room dungeon must be complete and inactive in the second room."); // 기존 구성 보존
            } // 구성 누락 조건 끝
            if (!travel.HasExactConfiguration(CreateRooms(first, second, null), CreateDoors(false), movement)) // 기존 두 방과 출구의 사용자 설정 검사
            { // 기존 설정 불일치 조건 시작
                throw new InvalidOperationException("Existing room and door bindings differ from the trial setup."); // 사용자 방 연결 덮어쓰기 방지
            } // 기존 설정 불일치 조건 끝
            var north = new Vector3Int(2, 4, 0); // 두 번째 방 위쪽 문 칸
            var northApproach = new Vector3Int(2, 3, 0); // 두 번째 방 문 앞 칸
            var south = new Vector3Int(2, -4, 0); // 세 번째 방 아래쪽 문 칸
            var southApproach = new Vector3Int(2, -3, 0); // 세 번째 방 도착 칸
            var west = new Vector3Int(-6, 0, 0); // 복제된 기존 왼쪽 문 칸
            if (!floor.HasTile(north) || !floor.HasTile(northApproach) || !floor.HasTile(south) || !floor.HasTile(southApproach) || obstacles.HasTile(north) || obstacles.HasTile(northApproach) || obstacles.HasTile(south) || obstacles.HasTile(southApproach) || obstacles.GetTile(west) != tile) // 새 문과 도착 칸의 기존 타일 검사
            { // 칸 충돌 조건 시작
                throw new InvalidOperationException("Trial door and arrival cells must be empty, and the original trial door must exist."); // 사용자 타일 덮어쓰기 방지
            } // 칸 충돌 조건 끝
            third = UnityEngine.Object.Instantiate(second); // 기존 두 번째 방을 보존하며 새 방 복제
            third.name = "ThirdDungeonGrid"; // 세 번째 방 이름 설정
            var thirdFloor = third.transform.Find("Floor").GetComponent<Tilemap>(); // 복제된 바닥 조회
            var thirdObstacles = third.transform.Find("Obstacles").GetComponent<Tilemap>(); // 복제된 장애물 조회
            var copiedMarker = third.transform.Find("DoorMarker"); // 복제된 기존 문 표시
            UnityEngine.Object.DestroyImmediate(copiedMarker.gameObject); // 복제본에서 사용하지 않는 문 표시 제거
            thirdObstacles.SetTile(west, null); // 복제본의 기존 왼쪽 문 칸 해제
            obstacles.SetTile(north, tile); // 두 번째 방 위쪽 문 칸 차단
            thirdObstacles.SetTile(south, tile); // 세 번째 방 아래쪽 문 칸 차단
            CreateMarker(second.transform, floor, north, sprite, 0f); // 두 번째 방의 위쪽 문 표시
            CreateMarker(third.transform, thirdFloor, south, sprite, 180f); // 세 번째 방의 아래쪽 문 표시
            third.SetActive(false); // 시작 시 세 번째 방 비활성
            travel.Configure(CreateRooms(first, second, third), CreateDoors(true), movement); // 공통 방 구조와 실제 객체 연결
            EditorUtility.SetDirty(travel); // 방 연결 변경 표시
            EditorSceneManager.MarkSceneDirty(scene); // 씬 저장 대상 표시
        } // 연결 끝
        private static RoomBinding[] CreateRooms(GameObject first, GameObject second, GameObject third) // 예상하는 시험 방 연결
        { // 생성 시작
            if (third == null) // 기존 두 방 구성 검사
            { // 두 방 조건 시작
                return new[] // 기존 두 방 목록
                { // 방 목록 시작
                    new RoomBinding(0, 0, false, first), // 시작 방
                    new RoomBinding(1, 0, false, second) // 오른쪽 방
                }; // 방 목록 끝
            } // 두 방 조건 끝
            return new[] // 세 시험 방 목록
            { // 방 목록 시작
                new RoomBinding(0, 0, false, first), // 시작 방
                new RoomBinding(1, 0, false, second), // 오른쪽 방
                new RoomBinding(1, 1, false, third) // 위쪽 방
            }; // 방 목록 끝
        } // 생성 끝
        private static DoorBinding[] CreateDoors(bool includeThird) // 예상하는 양방향 출구 목록
        { // 생성 시작
            if (!includeThird) // 기존 두 방 구성 검사
            { // 두 방 조건 시작
                return new[] // 기존 두 출구 목록
                { // 출구 목록 시작
                    new DoorBinding(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0), // 시작 방 오른쪽 문
                    new DoorBinding(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0) // 두 번째 방 왼쪽 문
                }; // 출구 목록 끝
            } // 두 방 조건 끝
            return new[] // 세 방의 출구 목록
            { // 출구 목록 시작
                new DoorBinding(0, 0, 5, 0, MoveDirection.Right, 1, 0, -5, 0), // 시작 방 오른쪽 문
                new DoorBinding(1, 0, -5, 0, MoveDirection.Left, 0, 0, 5, 0), // 두 번째 방 왼쪽 문
                new DoorBinding(1, 0, 2, 3, MoveDirection.Up, 1, 1, 2, -3), // 두 번째 방 위쪽 문
                new DoorBinding(1, 1, 2, -3, MoveDirection.Down, 1, 0, 2, 3) // 세 번째 방 아래쪽 문
            }; // 출구 목록 끝
        } // 생성 끝
        private static GameObject FindRoot(Scene scene, string name) // 비활성 객체를 포함한 씬 루트 조회
        { // 조회 시작
            foreach (var root in scene.GetRootGameObjects()) // 씬 루트 순회
            { // 루트 순회 시작
                if (root.name == name) // 이름 일치 검사
                { // 일치 조건 시작
                    return root; // 기존 객체 전달
                } // 일치 조건 끝
            } // 루트 순회 끝
            return null; // 일치하는 객체 없음
        } // 조회 끝
        private static void CreateMarker(Transform parent, Tilemap floor, Vector3Int cell, Sprite sprite, float angle) // 임시 출구 방향 표시
        { // 표시 생성 시작
            var marker = new GameObject("DoorMarker", typeof(SpriteRenderer)); // 출구 표시 객체 생성
            marker.transform.SetParent(parent, false); // 해당 방 아래 연결
            marker.transform.position = floor.GetCellCenterWorld(cell); // 출구 칸 중심에 배치
            marker.transform.localRotation = Quaternion.Euler(0f, 0f, angle); // 출구 방향 회전
            var renderer = marker.GetComponent<SpriteRenderer>(); // 표시 렌더러 조회
            renderer.sprite = sprite; // 기존 흰색 이미지 연결
            renderer.color = new Color(1f, 0.88f, 0.2f); // 기존 문 표시 색상
            renderer.sortingOrder = 3; // 문 칸 위에 표시
        } // 표시 생성 끝
        private static bool PrepareOpenScenes() // 미저장 씬 보호
        { // 검사 시작
            if (!Application.isBatchMode) // 대화형 편집기 검사
            { // 대화형 조건 시작
                return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(); // 저장 또는 취소 선택
            } // 대화형 조건 끝
            for (var index = 0; index < SceneManager.sceneCount; index++) // 열린 씬 순회
            { // 열린 씬 검사 시작
                if (SceneManager.GetSceneAt(index).isDirty) // 미저장 변경 검사
                { // 미저장 조건 시작
                    throw new InvalidOperationException("Save or close modified scenes before configuring the minimap."); // 사용자 변경 유실 방지
                } // 미저장 조건 끝
            } // 열린 씬 검사 끝
            return true; // 저장된 씬 전환 허용
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
