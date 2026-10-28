using System.IO; // 씬과 빌드 폴더 준비
using ProjectDelta.Runtime; // 새 실행 구성
using UnityEditor; // 제작 도구
using UnityEditor.Build; // 빌드 실패 알림
using UnityEditor.Build.Reporting; // 빌드 결과 확인
using UnityEditor.SceneManagement; // 제작용 씬 저장
using UnityEngine; // 기본 씬 객체
using UnityEngine.SceneManagement; // 씬 형식
using UnityEngine.Tilemaps; // 2D 격자 표시
namespace ProjectDelta.Editor // 제작 도구 이름 공간
{ // 이름 공간 시작
    public static class DayOneProjectSetup // 1일차 씬과 빌드 제작
    { // 클래스 시작
        private const string SceneFolder = "Assets/ProjectDelta/Scenes"; // 기본 씬 저장 위치
        private const string ArtFolder = "Assets/ProjectDelta/Art/Prototype"; // 임시 격자 리소스 위치
        [MenuItem("Project Delta/1일차/기본 씬 재생성")] // 편집기 제작 메뉴
        public static void Generate() // 기본 씬 세 개 생성
        { // 생성 시작
            Directory.CreateDirectory(SceneFolder); // 씬 폴더 준비
            Directory.CreateDirectory(ArtFolder); // 임시 리소스 폴더 준비
            AssetDatabase.Refresh(); // 새 폴더 등록
            var tile = CreateGridTile(); // 단순 격자 타일 준비
            var bootstrap = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // 초기화 씬 생성
            new GameObject("AppRoot", typeof(AppRoot)); // 유일한 실행 객체 배치
            EditorSceneManager.SaveScene(bootstrap, SceneFolder + "/BootstrapScene.unity"); // 초기화 씬 저장
            var title = CreateScreenScene(); // 타이틀 배경 씬 생성
            new GameObject("TitleScreen", typeof(TitleScreenView)); // 타이틀 화면 배치
            EditorSceneManager.SaveScene(title, SceneFolder + "/TitleScene.unity"); // 타이틀 씬 저장
            var dungeon = CreateScreenScene(); // 던전 배경 씬 생성
            CreateDungeonGrid(tile); // 탐험용 정지 격자 배치
            new GameObject("DungeonScreen", typeof(DungeonScreenView)); // 던전 안내 화면 배치
            DayTwoProjectSetup.ConfigureCurrentScene(true); // 새 격자의 이동과 시험 장애물 구성
            DayThreeProjectSetup.ConfigureCurrentScene(); // 새 던전의 두 시험 방 연결
            EditorSceneManager.SaveScene(dungeon, SceneFolder + "/DungeonScene.unity"); // 던전 씬 저장
            EditorBuildSettings.scenes = new[] // 실제 실행 씬 목록
            { // 씬 목록 시작
                new EditorBuildSettingsScene(SceneFolder + "/BootstrapScene.unity", true), // 첫 실행 씬 등록
                new EditorBuildSettingsScene(SceneFolder + "/TitleScene.unity", true), // 타이틀 씬 등록
                new EditorBuildSettingsScene(SceneFolder + "/DungeonScene.unity", true) // 던전 씬 등록
            }; // 씬 목록 끝
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions"); // 기본 템플릿 전역 입력 연결 해제
            PlayerSettings.companyName = "Project Delta"; // 새 개발 데이터 식별자
            PlayerSettings.productName = "Project Delta 2D"; // 새 프로젝트 실행 이름
            PlayerSettings.defaultScreenWidth = 1280; // 기본 창 가로 크기
            PlayerSettings.defaultScreenHeight = 720; // 기본 창 세로 크기
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; // 검증용 창 모드
            PlayerSettings.resizableWindow = true; // 창 크기 변경 허용
            PlayerSettings.runInBackground = true; // 포커스 이동 중 검증 유지
            AssetDatabase.SaveAssets(); // 생성 리소스 저장
            EditorSceneManager.OpenScene(SceneFolder + "/BootstrapScene.unity"); // 기본 실행 씬 선택
            Debug.Log("DAY01_SCENES_GENERATED: Bootstrap, Title, Dungeon"); // 생성 완료 근거 기록
        } // 생성 끝
        [MenuItem("Project Delta/1일차/Windows 빌드")] // 편집기 빌드 메뉴
        public static void BuildWindows() // Windows 시험 빌드
        { // 빌드 시작
            BuildWindowsAt("Builds/Windows"); // 1일차 기본 출력 경로
        } // 빌드 끝
        public static void BuildWindowsAt(string outputFolder) // 지정 폴더의 Windows 빌드
        { // 빌드 시작
            Directory.CreateDirectory(outputFolder); // 빌드 출력 폴더 준비
            var options = new BuildPlayerOptions // 실제 빌드 옵션
            { // 옵션 목록 시작
                scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path), // 검증 대상 씬 목록
                locationPathName = Path.Combine(outputFolder, "ProjectDelta.exe"), // 실행 파일 경로
                target = BuildTarget.StandaloneWindows64, // Windows 64비트 대상
                options = BuildOptions.None // 일반 실행 파일 빌드
            }; // 옵션 목록 끝
            var report = BuildPipeline.BuildPlayer(options); // 실제 빌드 실행
            if (report.summary.result != BuildResult.Succeeded) // 실제 빌드 결과 검사
            { // 실패 조건 시작
                throw new BuildFailedException("Windows build failed: " + report.summary.result); // 실패 종료 전달
            } // 실패 조건 끝
            Debug.Log("DELTA_WINDOWS_BUILD_SUCCEEDED: " + report.summary.totalSize + " bytes"); // 빌드 성공 근거 기록
        } // 빌드 끝
        private static Scene CreateScreenScene() // 고정 직교 화면 생성
        { // 생성 시작
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // 빈 화면 씬 생성
            var cameraObject = new GameObject("Main Camera", typeof(Camera)); // 화면 카메라 생성
            cameraObject.tag = "MainCamera"; // 기본 카메라 표시
            cameraObject.transform.position = new Vector3(0f, 0f, -10f); // 2D 정면 위치
            var camera = cameraObject.GetComponent<Camera>(); // 카메라 설정 조회
            camera.orthographic = true; // 원근 없는 탑뷰 표시
            camera.orthographicSize = 7.7f; // 격자와 안내 영역 확보
            camera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경 표시
            camera.backgroundColor = new Color(0.035f, 0.055f, 0.075f); // 어두운 던전 배경
            return scene; // 생성한 화면 전달
        } // 생성 끝
        private static Tile CreateGridTile() // 외부 이미지 없는 임시 타일 생성
        { // 생성 시작
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false); // 단순 격자 이미지 준비
            for (var y = 0; y < 16; y++) // 세로 픽셀 순회
            { // 세로 반복 시작
                for (var x = 0; x < 16; x++) // 가로 픽셀 순회
                { // 가로 반복 시작
                    var border = x == 0 || y == 0 || x == 15 || y == 15; // 셀 테두리 검사
                    texture.SetPixel(x, y, border ? new Color(0.16f, 0.23f, 0.27f) : new Color(0.08f, 0.13f, 0.17f)); // 바닥과 테두리 표시
                } // 가로 반복 끝
            } // 세로 반복 끝
            texture.Apply(); // 픽셀 변경 반영
            var texturePath = ArtFolder + "/GridCell.png"; // 임시 이미지 저장 경로
            File.WriteAllBytes(texturePath, texture.EncodeToPNG()); // 직접 생성한 이미지 저장
            Object.DestroyImmediate(texture); // 제작 중 이미지 메모리 정리
            AssetDatabase.ImportAsset(texturePath); // 이미지 에셋 등록
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath); // 이미지 설정 조회
            importer.textureType = TextureImporterType.Sprite; // 2D 스프라이트 형식
            importer.spritePixelsPerUnit = 16f; // 한 셀의 월드 크기
            importer.filterMode = FilterMode.Point; // 격자 경계 선명도
            importer.mipmapEnabled = false; // 작은 이미지 축소 단계 제외
            importer.SaveAndReimport(); // 스프라이트 설정 적용
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath); // 생성된 스프라이트 조회
            var tilePath = ArtFolder + "/GridTile.asset"; // 타일 저장 경로
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath); // 기존 임시 타일 조회
            if (tile == null) // 타일 최초 생성 검사
            { // 최초 생성 시작
                tile = ScriptableObject.CreateInstance<Tile>(); // 타일 데이터 생성
                AssetDatabase.CreateAsset(tile, tilePath); // 타일 에셋 저장
            } // 최초 생성 끝
            tile.sprite = sprite; // 격자 이미지 연결
            EditorUtility.SetDirty(tile); // 타일 변경 표시
            return tile; // 준비한 타일 전달
        } // 생성 끝
        private static void CreateDungeonGrid(Tile tile) // 정지 던전 표시 구성
        { // 구성 시작
            var gridObject = new GameObject("DungeonGrid", typeof(Grid)); // 월드 격자 생성
            var floorObject = new GameObject("Floor", typeof(Tilemap), typeof(TilemapRenderer)); // 바닥 타일맵 생성
            floorObject.transform.SetParent(gridObject.transform, false); // 월드 격자에 배치
            var tilemap = floorObject.GetComponent<Tilemap>(); // 바닥 타일맵 조회
            for (var x = -6; x <= 6; x++) // 가로 셀 순회
            { // 가로 반복 시작
                for (var y = -4; y <= 4; y++) // 세로 셀 순회
                { // 세로 반복 시작
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile); // 시험 바닥 셀 배치
                } // 세로 반복 끝
            } // 가로 반복 끝
            gridObject.transform.position = new Vector3(-0.5f, -0.5f, 0f); // 셀 중심 기준 정렬
            var playerObject = new GameObject("PlayerPreview", typeof(SpriteRenderer)); // 정지 플레이어 표시
            var renderer = playerObject.GetComponent<SpriteRenderer>(); // 플레이어 표시 조회
            renderer.sprite = tile.sprite; // 외부 리소스 없는 임시 표시
            renderer.color = new Color(5f, 4.5f, 2.5f); // 어두운 타일 위 강조 색상
            renderer.sortingOrder = 1; // 바닥 앞 표시
            playerObject.transform.localScale = new Vector3(0.65f, 0.65f, 1f); // 셀보다 작은 플레이어 크기
        } // 구성 끝
    } // 클래스 끝
} // 이름 공간 끝
