using System; // 구성 오류 알림
using ProjectDelta.Runtime; // 방 이동과 플레이어 구성
using UnityEditor; // 제작 도구
using UnityEditor.SceneManagement; // 씬 열기와 저장
using UnityEngine; // 방 객체와 표시
using UnityEngine.SceneManagement; // 열린 씬 검사
using UnityEngine.Tilemaps; // 방의 바닥과 장애물
namespace ProjectDelta.Editor // 제작 도구 이름 공간
{ // 이름 공간 시작
    public static class DayThreeProjectSetup // 두 시험 방의 안전한 연결
    { // 클래스 시작
        private const string DungeonPath = "Assets/ProjectDelta/Scenes/DungeonScene.unity"; // 기존 던전 씬 위치
        [MenuItem("Project Delta/3일차/Windows 빌드")] // 편집기 빌드 메뉴
        public static void BuildWindows() // 3일차 Windows 시험 빌드
        { // 빌드 시작
            DayOneProjectSetup.BuildWindowsAt("Builds/Day03"); // 기존 실행 파일과 분리된 출력 경로
        } // 빌드 끝
        [MenuItem("Project Delta/3일차/시험 방 연결")] // 편집기 구성 메뉴
        public static void Configure() // 저장된 던전에 방 연결
        { // 구성 시작
            if (!PrepareOpenScenes()) // 미저장 씬 보존 검사
            { // 사용자 취소 조건 시작
                return; // 씬 변경 없이 종료
            } // 사용자 취소 조건 끝
            var scene = EditorSceneManager.OpenScene(DungeonPath, OpenSceneMode.Single); // 기존 던전 씬 열기
            ConfigureCurrentScene(); // 두 방과 문 연결
            if (!EditorSceneManager.SaveScene(scene)) // 실제 씬 저장 검사
            { // 저장 실패 조건 시작
                throw new InvalidOperationException("Dungeon scene could not be saved."); // 저장 오류 알림
            } // 저장 실패 조건 끝
            var saved = EditorSceneManager.OpenScene(DungeonPath, OpenSceneMode.Single); // 저장한 씬 재개방
            if (FindRoot(saved, "SecondDungeonGrid") == null || FindRoot(saved, "RoomTravel") == null) // 재개방된 방 연결 검사
            { // 재개방 실패 조건 시작
                throw new InvalidOperationException("Room travel did not persist after reopening."); // 씬 저장 검증 오류
            } // 재개방 실패 조건 끝
            EditorSceneManager.OpenScene("Assets/ProjectDelta/Scenes/BootstrapScene.unity", OpenSceneMode.Single); // 시작 씬으로 복귀
            AssetDatabase.SaveAssets(); // 변경한 에셋 저장
            Debug.Log("DAY03_DUNGEON_SAVED_AND_REOPENED"); // 실제 씬 저장 검증 기록
        } // 구성 끝
        public static void ConfigureCurrentScene() // 현재 던전의 시험 방 연결
        { // 연결 시작
            var scene = SceneManager.GetActiveScene(); // 현재 던전 씬 조회
            var first = FindRoot(scene, "DungeonGrid"); // 기존 첫 방 조회
            var second = FindRoot(scene, "SecondDungeonGrid"); // 기존 두 번째 방 조회
            var travel = FindRoot(scene, "RoomTravel"); // 기존 방 이동 객체 조회
            if (second != null || travel != null) // 이미 설치된 구성 검사
            { // 기존 구성 조건 시작
                if (second == null || travel == null || travel.GetComponent<RoomTravelController>() == null) // 불완전한 기존 구성 검사
                { // 충돌 조건 시작
                    throw new InvalidOperationException("Existing room travel setup is incomplete; preserve and inspect it."); // 기존 객체 덮어쓰기 방지
                } // 충돌 조건 끝
                return; // 기존 연결 보존
            } // 기존 구성 조건 끝
            var playerObject = GameObject.Find("PlayerPreview"); // 기존 플레이어 조회
            var movement = playerObject == null ? null : playerObject.GetComponent<GridMoveController>(); // 플레이어 이동 제어 조회
            var floorObject = first == null ? null : first.transform.Find("Floor"); // 첫 방 바닥 조회
            var obstacleObject = first == null ? null : first.transform.Find("Obstacles"); // 첫 방 장애물 조회
            var floor = floorObject == null ? null : floorObject.GetComponent<Tilemap>(); // 첫 방 바닥 타일맵
            var obstacles = obstacleObject == null ? null : obstacleObject.GetComponent<Tilemap>(); // 첫 방 장애물 타일맵
            var tile = AssetDatabase.LoadAssetAtPath<Tile>("Assets/ProjectDelta/Art/Prototype/ObstacleTile.asset"); // 문 막기용 시험 타일
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/ProjectDelta/Art/Prototype/DirectionArrow.png"); // 임시 문 방향 표시 이미지
            if (movement == null || floor == null || obstacles == null || tile == null || sprite == null) // 필수 기존 구성 검사
            { // 구성 누락 조건 시작
                throw new InvalidOperationException("Dungeon movement, maps, obstacle tile, and marker sprite are required."); // 누락된 구성 알림
            } // 구성 누락 조건 끝
            var east = new Vector3Int(6, 0, 0); // 첫 방 오른쪽 문 칸
            var west = new Vector3Int(-6, 0, 0); // 두 번째 방 왼쪽 문 칸
            if (!floor.HasTile(east) || !floor.HasTile(west) || !floor.HasTile(new Vector3Int(5, 0, 0)) || !floor.HasTile(new Vector3Int(-5, 0, 0)) || obstacles.HasTile(east) || obstacles.HasTile(west) || obstacles.HasTile(new Vector3Int(5, 0, 0)) || obstacles.HasTile(new Vector3Int(-5, 0, 0))) // 기존 문과 도착 칸 검사
            { // 충돌 조건 시작
                throw new InvalidOperationException("Trial door and arrival cells must be empty floor cells."); // 기존 사용자 타일 덮어쓰기 방지
            } // 충돌 조건 끝
            second = UnityEngine.Object.Instantiate(first); // 기존 방을 보존하며 시험 방 복제
            second.name = "SecondDungeonGrid"; // 두 번째 방 이름 설정
            var secondFloor = second.transform.Find("Floor").GetComponent<Tilemap>(); // 복제된 바닥 조회
            var secondObstacles = second.transform.Find("Obstacles").GetComponent<Tilemap>(); // 복제된 장애물 조회
            obstacles.SetTile(east, tile); // 첫 방의 문 칸 통행 차단
            secondObstacles.SetTile(west, tile); // 두 번째 방의 문 칸 통행 차단
            CreateMarker(first.transform, floor, east, sprite, -90f); // 첫 방의 문 표시
            CreateMarker(second.transform, secondFloor, west, sprite, 90f); // 두 번째 방의 문 표시
            second.SetActive(false); // 시작 시 첫 번째 방만 표시
            travel = new GameObject("RoomTravel"); // 방 이동 관리 객체 생성
            travel.AddComponent<RoomTravelController>().Configure(first, second, movement); // 두 방과 플레이어 연결
            EditorSceneManager.MarkSceneDirty(scene); // 씬 저장 대상 표시
        } // 연결 끝
        private static GameObject FindRoot(Scene scene, string name) // 비활성 객체를 포함한 씬 루트 조회
        { // 조회 시작
            foreach (var root in scene.GetRootGameObjects()) // 씬 루트 순회
            { // 순회 시작
                if (root.name == name) // 이름 일치 검사
                { // 일치 조건 시작
                    return root; // 기존 객체 전달
                } // 일치 조건 끝
            } // 순회 끝
            return null; // 일치하는 객체 없음
        } // 조회 끝
        private static void CreateMarker(Transform parent, Tilemap floor, Vector3Int cell, Sprite sprite, float angle) // 눈에 보이는 임시 문 표시
        { // 표시 생성 시작
            var marker = new GameObject("DoorMarker", typeof(SpriteRenderer)); // 문 표시 객체 생성
            marker.transform.SetParent(parent, false); // 해당 방 아래 연결
            marker.transform.position = floor.GetCellCenterWorld(cell); // 문 칸 중심에 배치
            marker.transform.localRotation = Quaternion.Euler(0f, 0f, angle); // 연결 방향 표시
            var renderer = marker.GetComponent<SpriteRenderer>(); // 문 표시 렌더러 조회
            renderer.sprite = sprite; // 기존 흰색 이미지 연결
            renderer.color = new Color(1f, 0.88f, 0.2f); // 눈에 띄는 노란 표시
            renderer.sortingOrder = 3; // 문 칸 위에 표시
        } // 표시 생성 끝
        private static bool PrepareOpenScenes() // 미저장 씬 보존
        { // 검사 시작
            if (!Application.isBatchMode) // 대화형 편집기 검사
            { // 대화형 조건 시작
                return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(); // 저장 또는 취소 선택
            } // 대화형 조건 끝
            for (var index = 0; index < SceneManager.sceneCount; index++) // 열린 씬 순회
            { // 순회 시작
                if (SceneManager.GetSceneAt(index).isDirty) // 미저장 변경 검사
                { // 미저장 조건 시작
                    throw new InvalidOperationException("Save or close modified scenes before configuring room travel."); // 미저장 씬 보호
                } // 미저장 조건 끝
            } // 순회 끝
            return true; // 저장된 씬 전환 허용
        } // 검사 끝
    } // 클래스 끝
} // 이름 공간 끝
