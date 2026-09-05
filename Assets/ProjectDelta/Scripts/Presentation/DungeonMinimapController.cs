using System.Collections.Generic;
using ProjectDelta.Domain;
using DungeonRunState = ProjectDelta.Domain.DungeonRunState;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectDelta.Presentation
{
    // 37일차 미니맵을 유지하고 38일차 전체 지도 기능을 확장한다.
    // 미발견 방은 전체 지도에서도 계속 숨기며 발견된 영역만 중앙 정렬한다.
    public sealed class DungeonMinimapController : MonoBehaviour
    {
        [SerializeField] private PlayerGridMovementController movementController;
        [SerializeField] private Transform viewTransform;
        [SerializeField] private float mapSize = 240f;
        [SerializeField] private float margin = 20f;
        [SerializeField] private float roomSpacing = 34f;
        [SerializeField] private float roomMarkerSize = 24f;
        [SerializeField] private float playerIconSize = 15f;

        // 기존 Scene 직렬화 값과 호환하기 위해 37일차 필드명을 유지한다.
        [SerializeField] private float fullMapScale = 5f;
        [SerializeField] private float fullMapMaxScreenRatio = 0.92f;
        [SerializeField] private float fullMapContentScale = 1.5f;

        [Header("38일차 전체 지도")]
        [SerializeField] private float fullMapMinZoom = 0.6f;
        [SerializeField] private float fullMapMaxZoom = 2f;
        [SerializeField] private float fullMapZoomStep = 0.2f;
        [SerializeField] private float fullMapConnectionThickness = 4f;
        [SerializeField] private float fullMapPadding = 28f;
        [SerializeField] private float fullMapInfoHeight = 76f;
        [SerializeField] private float fullMapBottomHeight = 34f;
        [SerializeField] private int totalFloorCount = 5;

        [Header("114일차 플레이어 중심 미니맵")]
        [SerializeField] private float localMapCellPixelSize = 30f;
        [SerializeField] private int localRevealRadius = 1;

        private readonly DungeonMinimapRevealTracker revealTracker =
            new DungeonMinimapRevealTracker();

        private readonly Dictionary<string, HashSet<GridPosition>> revealedLocalTiles =
            new Dictionary<string, HashSet<GridPosition>>();

        private GeneratedDungeon localRevealDungeon;

        private DungeonFloorController floorController;
        private Texture2D playerIconTexture;
        private bool isFullMapOpen;
        private float fullMapZoom = 1f;

        private static readonly Color PanelColor =
            new Color(0f, 0f, 0f, 0.58f);

        private static readonly Color UnvisitedRoomColor =
            new Color(0.18f, 0.18f, 0.18f, 0.82f);

        private static readonly Color VisitedRoomColor =
            new Color(0.72f, 0.72f, 0.72f, 0.94f);

        private static readonly Color CurrentRoomColor =
            new Color(1f, 0.78f, 0.25f, 1f);

        private static readonly Color ConnectionColor =
            new Color(0.58f, 0.58f, 0.58f, 0.9f);

        // 113일차: 현재 방 타일 지도 색상.
        private static readonly Color LocalCellColor =
            new Color(0.12f, 0.12f, 0.12f, 0.82f);

        private static readonly Color LocalGridColor =
            new Color(0.28f, 0.28f, 0.28f, 0.92f);

        private static readonly Color LocalWallColor =
            new Color(0.92f, 0.92f, 0.92f, 1f);

        // 116일차: 발견된 구성요소를 미니맵과 전체 맵에서 같은 문자 색상으로 표시한다.
        private static readonly Color LocalStairsColor =
            new Color(0.35f, 0.90f, 1f, 1f);

        private static readonly Color LocalChestColor =
            new Color(1f, 0.78f, 0.20f, 1f);

        private static readonly Color LocalSecretWallColor =
            new Color(0.82f, 0.48f, 1f, 1f);

        private static readonly Color LocalNpcColor =
            new Color(0.35f, 1f, 0.48f, 1f);

        private static readonly Color LocalAmbientColor =
            new Color(0.78f, 0.78f, 0.78f, 1f);

        private static readonly Color LocalMonsterColor =
            new Color(1f, 0.32f, 0.32f, 1f);

        private void Awake()
        {
            if (movementController == null)
            {
                movementController =
                    GetComponent<PlayerGridMovementController>();
            }

            floorController =
                GetComponent<DungeonFloorController>();

            if (floorController == null)
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>();
            }

            if (viewTransform == null)
            {
                Camera mainCamera = Camera.main;

                viewTransform = mainCamera != null
                    ? mainCamera.transform
                    : transform;
            }

            playerIconTexture =
                CreatePlayerIconTexture(32);

            fullMapZoom =
                Mathf.Clamp(
                    1f,
                    fullMapMinZoom,
                    fullMapMaxZoom);
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                isFullMapOpen = !isFullMapOpen;
                return;
            }

            if (!isFullMapOpen)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isFullMapOpen = false;
                return;
            }

            if (Mouse.current == null)
            {
                return;
            }

            float scrollY =
                Mouse.current.scroll.ReadValue().y;

            if (Mathf.Abs(scrollY) <= 0.01f)
            {
                return;
            }

            float direction =
                Mathf.Sign(scrollY);

            fullMapZoom =
                Mathf.Clamp(
                    fullMapZoom
                    + (direction * fullMapZoomStep),
                    fullMapMinZoom,
                    fullMapMaxZoom);
        }

        private void OnDestroy()
        {
            if (playerIconTexture != null)
            {
                Destroy(playerIconTexture);
                playerIconTexture = null;
            }
        }

        // DAY142_DUNGEON_MINIMAP_UGUI_PATCH
        private void BuildDungeonMinimapRuntimeUi142()
        {
            if (movementController == null
                || movementController.PlayerState == null)
            {
                return;
            }

            if (floorController == null)
            {
                floorController =
                    FindFirstObjectByType<DungeonFloorController>();
            }

            GeneratedDungeon dungeon =
                floorController != null
                    ? floorController.CurrentDungeon
                    : null;

            if (dungeon == null)
            {
                return;
            }

            string currentRoomId =
                movementController.PlayerState.CurrentRoomId;

            DungeonRunState runState =
                RunContext.Current != null
                    ? RunContext.Current.Dungeon
                    : null;

            if (runState != null
                && !revealTracker.IsTracking(dungeon))
            {
                revealTracker.Restore(
                    dungeon,
                    runState.RevealedRoomIds);
            }

            DungeonMinimapSnapshot snapshot =
                DungeonMinimapSnapshotBuilder.Build(
                    dungeon,
                    runState,
                    currentRoomId);

            if (!snapshot.HasCurrentRoom)
            {
                return;
            }

            revealTracker.Update(
                dungeon,
                currentRoomId);

            if (runState != null)
            {
                runState.MergeRevealedRooms(
                    revealTracker.RevealedRoomIds);
            }

            if (isFullMapOpen)
            {
                DrawFullMap(
                    snapshot,
                    dungeon,
                    runState);

                return;
            }

            DrawMiniMap(snapshot);
        }

        private void DrawMiniMap(
            DungeonMinimapSnapshot snapshot)
        {
            Rect panelRect = new Rect(
                Screen.width - margin - mapSize,
                margin,
                mapSize,
                mapSize);

            DrawPanelBackground(panelRect);

            // 114일차: 플레이어를 중앙에 고정하고 발견한 타일 지도를 반대 방향으로 이동시킨다.
            if (TryDrawPlayerCenteredGrid(
                    panelRect,
                    snapshot))
            {
                return;
            }

            DrawCurrentCenteredMap(
                panelRect,
                snapshot,
                roomSpacing,
                roomMarkerSize,
                playerIconSize);
        }

        private void DrawFullMap(
            DungeonMinimapSnapshot snapshot,
            GeneratedDungeon dungeon,
            DungeonRunState runState)
        {
            DrawFullScreenDim();

            float maxAllowedSize =
                Mathf.Min(
                    Screen.width,
                    Screen.height)
                * fullMapMaxScreenRatio;

            float fullSize =
                Mathf.Min(
                    mapSize * fullMapScale,
                    maxAllowedSize);

            Rect panelRect = new Rect(
                (Screen.width - fullSize) * 0.5f,
                (Screen.height - fullSize) * 0.5f,
                fullSize,
                fullSize);

            DrawPanelBackground(panelRect);

            DrawFullMapInfo(
                panelRect,
                snapshot,
                dungeon,
                runState);

            Rect graphRect =
                new Rect(
                    panelRect.x + fullMapPadding,
                    panelRect.y + fullMapInfoHeight,
                    panelRect.width
                    - (fullMapPadding * 2f),
                    panelRect.height
                    - fullMapInfoHeight
                    - fullMapBottomHeight
                    - fullMapPadding);

            // 115일차: 전체 맵 패널도 우측 미니맵과 같은 발견 타일 지도를 사용한다.
            if (!TryDrawFullPlayerCenteredGrid(
                    graphRect,
                    snapshot))
            {
                // 상세 타일 지도를 구성할 수 없는 예외 상황에서는 기존 방 단위 지도로 복구한다.
                DrawBoundsCenteredMap(
                    graphRect,
                    snapshot,
                    dungeon);
            }

            DrawFullMapHint(panelRect);
        }

        // 115일차: M 전체 맵 패널에서도 플레이어 중심의 발견 타일 지도를 크게 표시한다.
        private bool TryDrawFullPlayerCenteredGrid(
            Rect graphRect,
            DungeonMinimapSnapshot snapshot)
        {
            RoomView currentRoomView = // 현재 방 뷰 조회
                movementController != null // 이동 컨트롤러 존재 여부 확인
                    ? movementController.CurrentRoomView // 현재 이동 방 사용
                    : null; // 방 뷰 없음 처리

            if ((currentRoomView == null // 현재 방 뷰 누락 확인
                    || currentRoomView.PassageController == null // 통로 컨트롤러 누락 확인
                    || currentRoomView.PassageController.RoomId != snapshot.CurrentRoomId) // 현재 방 ID 불일치 확인
                && floorController != null // 층 컨트롤러 존재 확인
                && floorController.SpawnedRooms.TryGetValue( // 생성 방 목록 조회
                    snapshot.CurrentRoomId, // 현재 방 ID 전달
                    out RoomView spawnedRoom)) // 현재 방 반환
            {
                currentRoomView = // 현재 방 뷰 교체
                    spawnedRoom; // 생성된 방 사용
            }

            if (currentRoomView == null // 현재 방 뷰 존재 여부 확인
                || currentRoomView.PassageController == null) // 통로 컨트롤러 존재 여부 확인
            {
                return false; // 상세 전체 지도 사용 불가 반환
            }

            ProjectDelta.Data.RoomDefinition currentDefinition = // 현재 방 정의 조회
                currentRoomView.PassageController.RoomDefinition; // 방 정의 사용

            if (currentDefinition == null) // 방 정의 존재 여부 확인
            {
                return false; // 상세 전체 지도 사용 불가 반환
            }

            EnsureLocalRevealTracking(); // 현재 층 공개 기록 준비

            RevealCurrentNeighborhood( // 현재 위치 주변 타일 공개
                snapshot.CurrentRoomId, // 현재 방 ID 전달
                currentDefinition); // 현재 방 범위 전달

            float fullMapCellPixelSize = // 전체 지도용 타일 크기 계산
                Mathf.Max( // 지나치게 작은 타일 방지
                    4f, // 최소 픽셀 크기 지정
                    localMapCellPixelSize // 기본 미니맵 타일 크기 사용
                    * fullMapContentScale // 전체 지도 기본 확대 비율 적용
                    * fullMapZoom); // 마우스 휠 줌 비율 적용

            Vector2 mapCenter = // 전체 지도 플레이어 고정 중심 계산
                new Vector2( // 중심 좌표 생성
                    graphRect.width * 0.5f, // 중앙 X 계산
                    graphRect.height * 0.5f); // 중앙 Y 계산

            DungeonMinimapRuntimeGuiProxy.BeginGroup(graphRect); // 전체 지도 영역 클리핑 시작

            DrawRevealedLocalMap( // 미니맵과 동일한 발견 타일 지도 그리기
                mapCenter, // 플레이어 중심점 전달
                graphRect.width, // 전체 지도 너비 전달
                graphRect.height, // 전체 지도 높이 전달
                fullMapCellPixelSize); // 전체 지도 확대 타일 크기 전달

            DrawPlayerDirection( // 플레이어 방향 아이콘 그리기
                mapCenter, // 플레이어를 전체 지도 중앙에 고정
                Mathf.Max( // 최소 아이콘 크기 보장
                    14f, // 전체 지도 최소 아이콘 크기
                    fullMapCellPixelSize * 0.48f)); // 타일 크기 비례 아이콘 사용

            DrawFullMapRoomTypeBadge( // 현재 방 종류 배지 표시
                snapshot, // 현재 방 정보 전달
                graphRect.width); // 지도 너비 전달

            DungeonMinimapRuntimeGuiProxy.EndGroup(); // 전체 지도 영역 클리핑 종료

            return true; // 상세 전체 지도 표시 성공 반환
        }

        private static void DrawFullMapRoomTypeBadge( // 전체 지도 현재 방 종류 표시
            DungeonMinimapSnapshot snapshot, // 미니맵 스냅샷
            float width) // 전체 지도 너비
        {
            if (!snapshot.TryGetRoom( // 현재 방 데이터 조회
                    snapshot.CurrentRoomId, // 현재 방 ID 전달
                    out DungeonMinimapRoomEntry currentRoom)) // 현재 방 데이터 반환
            {
                return; // 방 데이터 없으면 표시 중단
            }

            GUIStyle badgeStyle = // 방 종류 배지 스타일 생성
                new GUIStyle(DungeonMinimapRuntimeGuiProxy.skin.label) // 기본 라벨 스타일 복사
                {
                    alignment = TextAnchor.MiddleCenter, // 가운데 정렬 사용
                    fontStyle = FontStyle.Bold, // 굵은 글자 사용
                    fontSize = 15 // 전체 지도 글자 크기 지정
                };

            badgeStyle.normal.textColor = // 배지 글자색 지정
                Color.white; // 흰색 사용

            string shortLabel = // 방 종류 짧은 문자 조회
                RoomTypeRules.GetShortLabel( // 방 종류 단축 규칙 호출
                    currentRoom.RoomType); // 현재 방 종류 전달

            string displayName = // 방 종류 표시 이름 조회
                RoomTypeRules.GetDisplayName( // 방 종류 이름 규칙 호출
                    currentRoom.RoomType); // 현재 방 종류 전달

            Rect badgeRect = // 배지 영역 계산
                new Rect( // 배지 사각형 생성
                    Mathf.Max(0f, (width * 0.5f) - 90f), // 중앙 기준 왼쪽 위치 계산
                    4f, // 지도 상단 여백 지정
                    Mathf.Min(180f, width), // 지도 너비를 넘지 않는 배지 너비
                    26f); // 배지 높이 지정

            Color previousColor = // 기존 GUI 색상 저장
                DungeonMinimapRuntimeGuiProxy.color; // 현재 GUI 색상 읽기

            DungeonMinimapRuntimeGuiProxy.color = // 배지 배경색 지정
                new Color(0f, 0f, 0f, 0.68f); // 반투명 검정 배경 사용

            DungeonMinimapRuntimeGuiProxy.DrawTexture( // 배지 배경 그리기
                badgeRect, // 배지 영역 전달
                Texture2D.whiteTexture); // 흰 텍스처에 색상 적용

            DungeonMinimapRuntimeGuiProxy.color = // 라벨 색상 복원
                Color.white; // 흰색 사용

            DungeonMinimapRuntimeGuiProxy.Label( // 현재 방 종류 텍스트 표시
                badgeRect, // 배지 영역 전달
                $"[{shortLabel}] {displayName}", // 단축 문자와 이름 표시
                badgeStyle); // 배지 스타일 전달

            DungeonMinimapRuntimeGuiProxy.color = // 기존 GUI 색상 복원
                previousColor; // 저장 색상 사용
        }

        private void DrawFullMapInfo(
            Rect panelRect,
            DungeonMinimapSnapshot snapshot,
            GeneratedDungeon dungeon,
            DungeonRunState runState)
        {
            DungeonMapProgress progress =
                DungeonMapAnalytics.CalculateProgress(
                    snapshot);

            int currentFloor =
                runState != null
                    ? runState.CurrentFloor
                    : 1;

            int displayTotalFloorCount =
                Mathf.Max(
                    currentFloor,
                    Mathf.Max(1, totalFloorCount));

            string stairsDistanceText = "?";

            if (dungeon.StairsRoom != null
                && revealTracker.IsRevealed(
                    dungeon.StairsRoom.RoomId)
                && DungeonMapAnalytics.TryGetShortestDistance(
                    dungeon,
                    snapshot.CurrentRoomId,
                    dungeon.StairsRoom.RoomId,
                    out int stairsDistance))
            {
                stairsDistanceText =
                    stairsDistance + "방";
            }

            GUIStyle infoStyle =
                new GUIStyle(DungeonMinimapRuntimeGuiProxy.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleLeft
                };

            infoStyle.normal.textColor =
                Color.white;

            float left =
                panelRect.x + fullMapPadding;

            float width =
                panelRect.width
                - (fullMapPadding * 2f);

            DungeonMinimapRuntimeGuiProxy.Label(
                new Rect(
                    left,
                    panelRect.y + 8f,
                    width,
                    22f),
                $"층 진행도 : {currentFloor} / {displayTotalFloorCount}",
                infoStyle);

            DungeonMinimapRuntimeGuiProxy.Label(
                new Rect(
                    left,
                    panelRect.y + 30f,
                    width,
                    22f),
                $"탐험률 : {progress.ExploredRoomCount} / {progress.TotalRoomCount}  ({progress.ExplorationPercent:0}%)",
                infoStyle);

            DungeonMinimapRuntimeGuiProxy.Label(
                new Rect(
                    left,
                    panelRect.y + 52f,
                    width,
                    22f),
                $"계단 거리 : {stairsDistanceText}",
                infoStyle);
        }

        private void DrawFullMapHint(
            Rect panelRect)
        {
            GUIStyle hintStyle =
                new GUIStyle(DungeonMinimapRuntimeGuiProxy.skin.label)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter
                };

            hintStyle.normal.textColor =
                new Color(
                    0.82f,
                    0.82f,
                    0.82f,
                    1f);

            DungeonMinimapRuntimeGuiProxy.Label(
                new Rect(
                    panelRect.x + fullMapPadding,
                    panelRect.yMax
                    - fullMapBottomHeight,
                    panelRect.width
                    - (fullMapPadding * 2f),
                    fullMapBottomHeight - 4f),
                $"마우스 휠 : 확대/축소   |   줌 {fullMapZoom:0.0}x   |   M / Esc : 닫기",
                hintStyle);
        }

        // 114일차: 플레이어는 중앙에 고정하고 발견한 타일과 기본 지형만 월드 상대 좌표로 그린다.
        private bool TryDrawPlayerCenteredGrid(
            Rect panelRect,
            DungeonMinimapSnapshot snapshot)
        {
            RoomView currentRoomView = // 현재 방 뷰 조회
                movementController != null // 이동 컨트롤러 존재 여부 확인
                    ? movementController.CurrentRoomView // 현재 방 뷰 사용
                    : null; // 방 뷰 없음 처리

            if ((currentRoomView == null // 현재 방 뷰 누락 확인
                    || currentRoomView.PassageController == null // 통로 컨트롤러 누락 확인
                    || currentRoomView.PassageController.RoomId != snapshot.CurrentRoomId) // 현재 방 ID 불일치 확인
                && floorController != null // 층 컨트롤러 존재 확인
                && floorController.SpawnedRooms.TryGetValue( // 생성 방 목록 조회
                    snapshot.CurrentRoomId, // 현재 방 ID 전달
                    out RoomView spawnedRoom)) // 생성된 현재 방 반환
            {
                currentRoomView = // 현재 방 뷰 교체
                    spawnedRoom; // 생성된 방 사용
            }

            if (currentRoomView == null // 현재 방 뷰 확인
                || currentRoomView.PassageController == null) // 통로 컨트롤러 확인
            {
                return false; // 상세 미니맵 사용 불가 반환
            }

            ProjectDelta.Data.RoomDefinition currentDefinition = // 현재 방 정의 조회
                currentRoomView.PassageController.RoomDefinition; // 통로 컨트롤러의 방 정의 사용

            if (currentDefinition == null) // 방 정의 존재 여부 확인
            {
                return false; // 상세 미니맵 사용 불가 반환
            }

            EnsureLocalRevealTracking(); // 현재 던전 공개 기록 준비

            RevealCurrentNeighborhood( // 현재 위치 주변 타일 공개
                snapshot.CurrentRoomId, // 현재 방 ID 전달
                currentDefinition); // 현재 방 범위 전달

            const float headerHeight = 26f; // 방 종류 헤더 높이
            const float mapPadding = 8f; // 미니맵 내부 여백

            Rect localMapRect = // 실제 타일 지도 영역 계산
                new Rect( // 지도 사각형 생성
                    mapPadding, // 왼쪽 여백 적용
                    headerHeight, // 헤더 아래 시작
                    panelRect.width - (mapPadding * 2f), // 지도 너비 계산
                    panelRect.height - headerHeight - mapPadding); // 지도 높이 계산

            DungeonMinimapRuntimeGuiProxy.BeginGroup(panelRect); // 미니맵 패널 클리핑 시작

            DrawCurrentRoomTypeHeader( // 현재 방 종류 표시
                snapshot, // 현재 미니맵 스냅샷 전달
                panelRect.width, // 헤더 너비 전달
                headerHeight); // 헤더 높이 전달

            DungeonMinimapRuntimeGuiProxy.BeginGroup(localMapRect); // 타일 지도 영역 클리핑 시작

            Vector2 mapCenter = // 플레이어 고정 중심점 계산
                new Vector2( // 중심 좌표 생성
                    localMapRect.width * 0.5f, // 중앙 X 계산
                    localMapRect.height * 0.5f); // 중앙 Y 계산

            DrawRevealedLocalMap( // 발견된 지형 지도 그리기
                mapCenter, // 플레이어 중심점 전달
                localMapRect.width, // 지도 영역 너비 전달
                localMapRect.height, // 지도 영역 높이 전달
                Mathf.Max(4f, localMapCellPixelSize)); // 미니맵 타일 크기 전달

            DrawPlayerDirection( // 플레이어 방향 아이콘 그리기
                mapCenter, // 항상 지도 중앙 사용
                Mathf.Max( // 최소 아이콘 크기 보장
                    12f, // 최소 크기 지정
                    localMapCellPixelSize * 0.48f)); // 타일 크기 비례 아이콘 계산

            DungeonMinimapRuntimeGuiProxy.EndGroup(); // 타일 지도 영역 클리핑 종료
            DungeonMinimapRuntimeGuiProxy.EndGroup(); // 미니맵 패널 클리핑 종료

            return true; // 상세 미니맵 표시 성공 반환
        }

        private void EnsureLocalRevealTracking() // 현재 던전별 공개 기록 준비
        {
            GeneratedDungeon currentDungeon = // 현재 생성 던전 조회
                floorController != null // 층 컨트롤러 존재 여부 확인
                    ? floorController.CurrentDungeon // 현재 던전 사용
                    : null; // 던전 없음 처리

            if (object.ReferenceEquals( // 같은 던전 인스턴스 여부 확인
                    localRevealDungeon, // 기존 추적 던전 전달
                    currentDungeon)) // 현재 던전 전달
            {
                return; // 기존 공개 기록 유지
            }

            localRevealDungeon = // 추적 던전 교체
                currentDungeon; // 현재 던전 저장

            revealedLocalTiles.Clear(); // 층 변경 시 이전 타일 공개 기록 초기화
        }

        private void RevealCurrentNeighborhood( // 현재 칸과 주변 8칸 공개
            string roomId, // 현재 방 ID
            ProjectDelta.Data.RoomDefinition definition) // 현재 방 정의
        {
            if (movementController == null // 이동 컨트롤러 확인
                || movementController.PlayerState == null // 플레이어 상태 확인
                || string.IsNullOrEmpty(roomId) // 방 ID 확인
                || definition == null) // 방 정의 확인
            {
                return; // 공개 처리 중단
            }

            if (!revealedLocalTiles.TryGetValue( // 방별 공개 집합 조회
                    roomId, // 현재 방 ID 전달
                    out HashSet<GridPosition> revealedTiles)) // 기존 공개 집합 반환
            {
                revealedTiles = // 새 공개 집합 생성
                    new HashSet<GridPosition>(); // 중복 없는 타일 집합 초기화

                revealedLocalTiles.Add( // 방별 공개 집합 등록
                    roomId, // 현재 방 ID 저장
                    revealedTiles); // 새 공개 집합 저장
            }

            IReadOnlyList<GridPosition> nearbyTiles = // 현재 칸 주변 공개 대상 계산
                DungeonMinimapTileRevealService.CollectAround( // 공개 규칙 서비스 호출
                    movementController.PlayerState.CurrentGridPosition, // 현재 플레이어 칸 전달
                    localRevealRadius, // 공개 반경 전달
                    definition.MinX, // 방 최소 X 전달
                    definition.MaxX, // 방 최대 X 전달
                    definition.MinZ, // 방 최소 Z 전달
                    definition.MaxZ); // 방 최대 Z 전달

            for (int index = 0; // 첫 공개 대상부터 반복
                 index < nearbyTiles.Count; // 모든 공개 대상까지 반복
                 index++) // 공개 대상 인덱스 증가
            {
                revealedTiles.Add( // 공개 타일 기록 추가
                    nearbyTiles[index]); // 주변 타일 좌표 저장
            }
        }

        private void DrawRevealedLocalMap( // 발견된 모든 방 타일을 플레이어 기준으로 그리기
            Vector2 mapCenter, // 플레이어 고정 중심점
            float mapWidth, // 지도 영역 너비
            float mapHeight, // 지도 영역 높이
            float cellPixelSize) // 호출 지도에 맞춘 타일 픽셀 크기
        {
            if (floorController == null // 층 컨트롤러 확인
                || movementController == null) // 이동 컨트롤러 확인
            {
                return; // 지도 그리기 중단
            }

            float worldCellSize = // 실제 월드 칸 크기 계산
                Mathf.Max( // 0 나눗셈 방지
                    0.01f, // 최소 월드 칸 크기
                    movementController.CellSize); // 이동 시스템 칸 크기 사용

            Vector3 playerWorldPosition = // 부드러운 스크롤 기준 플레이어 위치 조회
                movementController.transform.position; // 실제 이동 중 Transform 위치 사용

            foreach (KeyValuePair<string, RoomView> pair // 생성된 모든 방 반복
                     in floorController.SpawnedRooms) // 현재 층 방 목록 사용
            {
                if (pair.Value == null // 방 뷰 누락 확인
                    || pair.Value.PassageController == null // 통로 컨트롤러 누락 확인
                    || !revealedLocalTiles.TryGetValue( // 공개 기록 존재 여부 확인
                        pair.Key, // 방 ID 전달
                        out HashSet<GridPosition> revealedTiles) // 공개 타일 집합 반환
                    || revealedTiles.Count == 0) // 공개 타일 존재 여부 확인
                {
                    continue; // 미공개 방 건너뛰기
                }

                DrawRevealedRoomCells( // 방의 공개 바닥 타일 그리기
                    pair.Value, // 대상 방 뷰 전달
                    revealedTiles, // 공개 타일 집합 전달
                    playerWorldPosition, // 플레이어 월드 위치 전달
                    worldCellSize, // 월드 칸 크기 전달
                    cellPixelSize, // 미니맵 칸 크기 전달
                    mapCenter, // 지도 중심 전달
                    mapWidth, // 지도 너비 전달
                    mapHeight); // 지도 높이 전달
            }

            foreach (KeyValuePair<string, RoomView> pair // 생성된 모든 방 재반복
                     in floorController.SpawnedRooms) // 현재 층 방 목록 사용
            {
                if (pair.Value == null // 방 뷰 누락 확인
                    || pair.Value.PassageController == null // 통로 컨트롤러 누락 확인
                    || !revealedLocalTiles.TryGetValue( // 공개 기록 존재 여부 확인
                        pair.Key, // 방 ID 전달
                        out HashSet<GridPosition> revealedTiles) // 공개 타일 집합 반환
                    || revealedTiles.Count == 0) // 공개 타일 존재 여부 확인
                {
                    continue; // 미공개 방 건너뛰기
                }

                DrawRevealedRoomTerrain( // 공개 타일의 기본 벽 구조 그리기
                    pair.Value, // 대상 방 뷰 전달
                    revealedTiles, // 공개 타일 집합 전달
                    playerWorldPosition, // 플레이어 월드 위치 전달
                    worldCellSize, // 월드 칸 크기 전달
                    cellPixelSize, // 미니맵 칸 크기 전달
                    mapCenter, // 지도 중심 전달
                    mapWidth, // 지도 너비 전달
                    mapHeight); // 지도 높이 전달
            }

            foreach (KeyValuePair<string, RoomView> pair // 생성된 모든 방 구성요소 반복
                     in floorController.SpawnedRooms) // 현재 층 방 목록 사용
            {
                if (pair.Value == null // 방 뷰 누락 확인
                    || pair.Value.PassageController == null // 통로 컨트롤러 누락 확인
                    || !revealedLocalTiles.TryGetValue( // 공개 기록 존재 여부 확인
                        pair.Key, // 방 ID 전달
                        out HashSet<GridPosition> revealedTiles) // 공개 타일 집합 반환
                    || revealedTiles.Count == 0) // 공개 타일 존재 여부 확인
                {
                    continue; // 미공개 방 건너뛰기
                }

                DrawRevealedRoomContents( // 발견된 타일의 활성 구성요소 그리기
                    pair.Value, // 대상 방 뷰 전달
                    revealedTiles, // 공개 타일 집합 전달
                    playerWorldPosition, // 플레이어 월드 위치 전달
                    worldCellSize, // 월드 칸 크기 전달
                    cellPixelSize, // 지도 칸 크기 전달
                    mapCenter, // 지도 중심 전달
                    mapWidth, // 지도 너비 전달
                    mapHeight); // 지도 높이 전달
            }
        }

        private static void DrawRevealedRoomCells( // 한 방의 공개 바닥 타일 그리기
            RoomView roomView, // 대상 방 뷰
            IEnumerable<GridPosition> revealedTiles, // 공개 타일 목록
            Vector3 playerWorldPosition, // 플레이어 월드 위치
            float worldCellSize, // 월드 칸 크기
            float cellPixelSize, // 미니맵 칸 크기
            Vector2 mapCenter, // 지도 중심
            float mapWidth, // 지도 너비
            float mapHeight) // 지도 높이
        {
            Color previousColor = // 기존 GUI 색상 저장
                DungeonMinimapRuntimeGuiProxy.color; // 현재 GUI 색상 읽기

            foreach (GridPosition position // 공개 타일 반복
                     in revealedTiles) // 공개 타일 집합 사용
            {
                Rect cellRect = // 플레이어 기준 타일 사각형 계산
                    GetPlayerCenteredCellRect( // 타일 사각형 변환 호출
                        roomView, // 대상 방 전달
                        position, // 타일 좌표 전달
                        playerWorldPosition, // 플레이어 월드 위치 전달
                        worldCellSize, // 월드 칸 크기 전달
                        cellPixelSize, // 미니맵 칸 크기 전달
                        mapCenter); // 지도 중심 전달

                if (!IsLocalRectVisible( // 지도 영역 표시 여부 확인
                        cellRect, // 타일 사각형 전달
                        mapWidth, // 지도 너비 전달
                        mapHeight)) // 지도 높이 전달
                {
                    continue; // 화면 밖 타일 건너뛰기
                }

                DungeonMinimapRuntimeGuiProxy.color = // 바닥 타일 색상 지정
                    LocalCellColor; // 기본 바닥 색상 사용

                DungeonMinimapRuntimeGuiProxy.DrawTexture( // 바닥 타일 채우기
                    cellRect, // 타일 사각형 전달
                    Texture2D.whiteTexture); // 흰 텍스처 색상 적용

                DungeonMinimapRuntimeGuiProxy.color = // 타일 격자 색상 지정
                    LocalGridColor; // 격자 색상 사용

                DrawLocalCellOutline( // 타일 테두리 그리기
                    cellRect); // 타일 사각형 전달
            }

            DungeonMinimapRuntimeGuiProxy.color = // 기존 GUI 색상 복원
                previousColor; // 저장된 색상 사용
        }

        private static void DrawRevealedRoomTerrain( // 공개 타일의 기본 지형 벽 그리기
            RoomView roomView, // 대상 방 뷰
            IEnumerable<GridPosition> revealedTiles, // 공개 타일 목록
            Vector3 playerWorldPosition, // 플레이어 월드 위치
            float worldCellSize, // 월드 칸 크기
            float cellPixelSize, // 미니맵 칸 크기
            Vector2 mapCenter, // 지도 중심
            float mapWidth, // 지도 너비
            float mapHeight) // 지도 높이
        {
            ProjectDelta.Data.RoomDefinition definition = // 방 정의 조회
                roomView.PassageController.RoomDefinition; // 통로 컨트롤러 방 정의 사용

            if (definition == null) // 방 정의 확인
            {
                return; // 지형 그리기 중단
            }

            foreach (GridPosition position // 공개 타일 반복
                     in revealedTiles) // 공개 타일 집합 사용
            {
                Rect cellRect = // 플레이어 기준 타일 사각형 계산
                    GetPlayerCenteredCellRect( // 타일 사각형 변환 호출
                        roomView, // 대상 방 전달
                        position, // 타일 좌표 전달
                        playerWorldPosition, // 플레이어 월드 위치 전달
                        worldCellSize, // 월드 칸 크기 전달
                        cellPixelSize, // 미니맵 칸 크기 전달
                        mapCenter); // 지도 중심 전달

                if (!IsLocalRectVisible( // 지도 영역 표시 여부 확인
                        cellRect, // 타일 사각형 전달
                        mapWidth, // 지도 너비 전달
                        mapHeight)) // 지도 높이 전달
                {
                    continue; // 화면 밖 타일 건너뛰기
                }

                DrawLocalTerrainEdge( // 북쪽 지형 경계 그리기
                    roomView.PassageController, // 통로 컨트롤러 전달
                    definition, // 방 정의 전달
                    position, // 현재 타일 전달
                    CardinalDirection.North, // 북쪽 방향 전달
                    cellRect, // 타일 사각형 전달
                    cellPixelSize); // 타일 픽셀 크기 전달

                DrawLocalTerrainEdge( // 동쪽 지형 경계 그리기
                    roomView.PassageController, // 통로 컨트롤러 전달
                    definition, // 방 정의 전달
                    position, // 현재 타일 전달
                    CardinalDirection.East, // 동쪽 방향 전달
                    cellRect, // 타일 사각형 전달
                    cellPixelSize); // 타일 픽셀 크기 전달

                DrawLocalTerrainEdge( // 남쪽 지형 경계 그리기
                    roomView.PassageController, // 통로 컨트롤러 전달
                    definition, // 방 정의 전달
                    position, // 현재 타일 전달
                    CardinalDirection.South, // 남쪽 방향 전달
                    cellRect, // 타일 사각형 전달
                    cellPixelSize); // 타일 픽셀 크기 전달

                DrawLocalTerrainEdge( // 서쪽 지형 경계 그리기
                    roomView.PassageController, // 통로 컨트롤러 전달
                    definition, // 방 정의 전달
                    position, // 현재 타일 전달
                    CardinalDirection.West, // 서쪽 방향 전달
                    cellRect, // 타일 사각형 전달
                    cellPixelSize); // 타일 픽셀 크기 전달
            }
        }

        private static void DrawRevealedRoomContents( // 발견된 타일의 활성 구성요소 문자 그리기
            RoomView roomView, // 대상 방 뷰
            HashSet<GridPosition> revealedTiles, // 공개 타일 집합
            Vector3 playerWorldPosition, // 플레이어 월드 위치
            float worldCellSize, // 월드 칸 크기
            float cellPixelSize, // 지도 칸 크기
            Vector2 mapCenter, // 지도 중심
            float mapWidth, // 지도 너비
            float mapHeight) // 지도 높이
        {
            if (roomView == null // 방 뷰 확인
                || revealedTiles == null // 공개 타일 집합 확인
                || revealedTiles.Count == 0) // 공개 타일 존재 확인
            {
                return; // 구성요소 그리기 중단
            }

            RoomContentMarker[] markers = // 방의 모든 구성요소 마커 조회
                roomView.GetComponentsInChildren<RoomContentMarker>(true); // 비활성 마커까지 조회 후 직접 필터링

            int fontSize = // 지도 크기에 맞춘 글자 크기 계산
                Mathf.Clamp( // 최소·최대 크기 제한
                    Mathf.RoundToInt(cellPixelSize * 0.56f), // 타일 크기 비례 글자 크기 계산
                    10, // 최소 글자 크기
                    30); // 최대 글자 크기

            GUIStyle contentStyle = // 구성요소 문자 스타일 생성
                new GUIStyle(DungeonMinimapRuntimeGuiProxy.skin.label) // 기본 라벨 스타일 복사
                {
                    alignment = TextAnchor.MiddleCenter, // 타일 중앙 정렬 사용
                    fontStyle = FontStyle.Bold, // 굵은 문자 사용
                    fontSize = fontSize // 계산된 글자 크기 적용
                };

            for (int index = 0; // 첫 구성요소부터 반복
                 index < markers.Length; // 모든 구성요소까지 반복
                 index++) // 구성요소 인덱스 증가
            {
                RoomContentMarker marker = // 현재 구성요소 마커 조회
                    markers[index]; // 배열에서 현재 마커 사용

                if (marker == null // 마커 누락 확인
                    || !marker.gameObject.activeInHierarchy // 제거·비활성 구성요소 제외
                    || !revealedTiles.Contains(marker.GridPosition)) // 아직 발견하지 않은 타일 구성요소 제외
                {
                    continue; // 표시 대상이 아니면 건너뛰기
                }

                Rect cellRect = // 구성요소가 속한 타일 사각형 계산
                    GetPlayerCenteredCellRect( // 플레이어 중심 좌표 변환 호출
                        roomView, // 대상 방 전달
                        marker.GridPosition, // 구성요소 타일 좌표 전달
                        playerWorldPosition, // 플레이어 월드 위치 전달
                        worldCellSize, // 월드 칸 크기 전달
                        cellPixelSize, // 지도 칸 크기 전달
                        mapCenter); // 지도 중심 전달

                if (!IsLocalRectVisible( // 지도 영역 표시 여부 확인
                        cellRect, // 타일 사각형 전달
                        mapWidth, // 지도 너비 전달
                        mapHeight)) // 지도 높이 전달
                {
                    continue; // 화면 밖 구성요소 건너뛰기
                }

                string glyph = // 구성요소 문자 조회
                    DungeonMinimapContentGlyphRules.GetGlyph( // 공통 문자 규칙 호출
                        marker.ContentType); // 구성요소 종류 전달

                Rect shadowRect = // 글자 그림자 영역 계산
                    new Rect( // 그림자 사각형 생성
                        cellRect.x + 1f, // 오른쪽 한 픽셀 이동
                        cellRect.y + 1f, // 아래쪽 한 픽셀 이동
                        cellRect.width, // 타일 너비 사용
                        cellRect.height); // 타일 높이 사용

                contentStyle.normal.textColor = // 그림자 글자색 지정
                    new Color(0f, 0f, 0f, 0.92f); // 검정 그림자 사용

                DungeonMinimapRuntimeGuiProxy.Label( // 구성요소 그림자 문자 표시
                    shadowRect, // 그림자 영역 전달
                    glyph, // 구성요소 문자 전달
                    contentStyle); // 구성요소 스타일 전달

                contentStyle.normal.textColor = // 실제 구성요소 글자색 지정
                    GetLocalContentColor( // 구성요소별 색상 조회
                        marker.ContentType); // 구성요소 종류 전달

                DungeonMinimapRuntimeGuiProxy.Label( // 실제 구성요소 문자 표시
                    cellRect, // 구성요소 타일 영역 전달
                    glyph, // 구성요소 문자 전달
                    contentStyle); // 구성요소 스타일 전달
            }
        }

        private static Color GetLocalContentColor( // 구성요소별 지도 문자 색상 반환
            RoomContentType contentType) // 구성요소 종류
        {
            switch (contentType) // 구성요소 종류 분기
            {
                case RoomContentType.Stairs: // 계단 종류 확인
                    return LocalStairsColor; // 계단 색상 반환

                case RoomContentType.Chest: // 상자 종류 확인
                    return LocalChestColor; // 상자 색상 반환

                case RoomContentType.SecretWall: // 비밀벽 종류 확인
                    return LocalSecretWallColor; // 비밀벽 색상 반환

                case RoomContentType.NpcPoint: // NPC 종류 확인
                    return LocalNpcColor; // NPC 색상 반환

                case RoomContentType.AmbientProp: // 환경 요소 종류 확인
                    return LocalAmbientColor; // 환경 요소 색상 반환

                case RoomContentType.Monster: // 몬스터 종류 확인
                    return LocalMonsterColor; // 몬스터 색상 반환

                default: // 알 수 없는 종류 처리
                    return Color.white; // 기본 흰색 반환
            }
        }

        private static void DrawLocalTerrainEdge( // 콘텐츠 정보 없는 기본 지형 경계 그리기
            RoomPassageController passageController, // 통로 컨트롤러
            ProjectDelta.Data.RoomDefinition definition, // 방 정의
            GridPosition position, // 타일 좌표
            CardinalDirection direction, // 경계 방향
            Rect cellRect, // 타일 사각형
            float cellPixelSize) // 타일 픽셀 크기
        {
            bool isDoor = // 문 존재 여부 확인
                passageController.TryGetDoor( // 문 통로 조회
                    position, // 타일 좌표 전달
                    direction, // 방향 전달
                    out _); // 문 상태 정보는 의도적으로 사용하지 않음

            if (isDoor) // 문 위치 확인
            {
                return; // 문 잠금/열림 상태를 숨기고 통로 틈만 유지
            }

            bool isBoundary = // 방 외곽 경계 여부 확인
                IsLocalBoundaryEdge( // 경계 판정 호출
                    definition, // 방 정의 전달
                    position, // 타일 좌표 전달
                    direction); // 방향 전달

            if (!isBoundary // 방 외곽이 아닌지 확인
                && passageController.CanPass( // 내부 이동 가능 여부 확인
                    position, // 타일 좌표 전달
                    direction)) // 방향 전달
            {
                return; // 열린 내부 경계 미표시
            }

            float wallThickness = // 벽 두께 계산
                Mathf.Max( // 최소 두께 보장
                    2f, // 최소 두께 지정
                    cellPixelSize * 0.08f); // 타일 크기 비례 두께 계산

            Color previousColor = // 기존 GUI 색상 저장
                DungeonMinimapRuntimeGuiProxy.color; // 현재 GUI 색상 읽기

            DungeonMinimapRuntimeGuiProxy.color = // 벽 색상 지정
                LocalWallColor; // 단일 지형 벽 색상 사용

            DungeonMinimapRuntimeGuiProxy.DrawTexture( // 벽 선 그리기
                GetLocalEdgeRect( // 벽 사각형 계산
                    cellRect, // 타일 사각형 전달
                    direction, // 벽 방향 전달
                    wallThickness, // 벽 두께 전달
                    false), // 문 축약 사용 안 함
                Texture2D.whiteTexture); // 흰 텍스처 색상 적용

            DungeonMinimapRuntimeGuiProxy.color = // 기존 GUI 색상 복원
                previousColor; // 저장된 색상 사용
        }

        private static Rect GetPlayerCenteredCellRect( // 월드 타일을 플레이어 중심 미니맵 사각형으로 변환
            RoomView roomView, // 타일 소속 방
            GridPosition position, // 방 내부 타일 좌표
            Vector3 playerWorldPosition, // 실제 플레이어 월드 위치
            float worldCellSize, // 월드 한 칸 크기
            float cellPixelSize, // 미니맵 한 칸 크기
            Vector2 mapCenter) // 플레이어 고정 중심점
        {
            Vector3 localCellPosition = // 방 내부 타일 월드 변환용 로컬 위치 생성
                new Vector3( // 로컬 좌표 생성
                    position.X * worldCellSize, // 로컬 X 계산
                    0f, // 높이 미사용
                    position.Z * worldCellSize); // 로컬 Z 계산

            Vector3 cellWorldPosition = // 방 Transform을 반영한 월드 위치 계산
                roomView.transform.TransformPoint( // 로컬 좌표 월드 변환
                    localCellPosition); // 타일 로컬 좌표 전달

            float relativeX = // 플레이어 기준 가로 상대 칸 계산
                (cellWorldPosition.x - playerWorldPosition.x) // 월드 X 차이 계산
                / worldCellSize; // 칸 단위로 변환

            float relativeZ = // 플레이어 기준 세로 상대 칸 계산
                (cellWorldPosition.z - playerWorldPosition.z) // 월드 Z 차이 계산
                / worldCellSize; // 칸 단위로 변환

            Vector2 cellCenter = // 미니맵 타일 중심점 계산
                new Vector2( // 화면 좌표 생성
                    mapCenter.x + (relativeX * cellPixelSize), // 플레이어 이동 반대 방향 X 스크롤
                    mapCenter.y - (relativeZ * cellPixelSize)); // 플레이어 이동 반대 방향 Z 스크롤

            return new Rect( // 타일 사각형 반환
                cellCenter.x - (cellPixelSize * 0.5f), // 왼쪽 좌표 계산
                cellCenter.y - (cellPixelSize * 0.5f), // 위쪽 좌표 계산
                cellPixelSize, // 타일 너비 지정
                cellPixelSize); // 타일 높이 지정
        }

        private static void DrawLocalCellOutline( // 타일 네 방향 격자선 그리기
            Rect cellRect) // 타일 사각형
        {
            DungeonMinimapRuntimeGuiProxy.DrawTexture( // 위쪽 격자선 그리기
                new Rect( // 선 사각형 생성
                    cellRect.x, // 시작 X 지정
                    cellRect.y, // 시작 Y 지정
                    cellRect.width, // 선 너비 지정
                    1f), // 선 두께 지정
                Texture2D.whiteTexture); // 흰 텍스처 색상 적용

            DungeonMinimapRuntimeGuiProxy.DrawTexture( // 아래쪽 격자선 그리기
                new Rect( // 선 사각형 생성
                    cellRect.x, // 시작 X 지정
                    cellRect.yMax - 1f, // 아래쪽 Y 지정
                    cellRect.width, // 선 너비 지정
                    1f), // 선 두께 지정
                Texture2D.whiteTexture); // 흰 텍스처 색상 적용

            DungeonMinimapRuntimeGuiProxy.DrawTexture( // 왼쪽 격자선 그리기
                new Rect( // 선 사각형 생성
                    cellRect.x, // 왼쪽 X 지정
                    cellRect.y, // 시작 Y 지정
                    1f, // 선 두께 지정
                    cellRect.height), // 선 높이 지정
                Texture2D.whiteTexture); // 흰 텍스처 색상 적용

            DungeonMinimapRuntimeGuiProxy.DrawTexture( // 오른쪽 격자선 그리기
                new Rect( // 선 사각형 생성
                    cellRect.xMax - 1f, // 오른쪽 X 지정
                    cellRect.y, // 시작 Y 지정
                    1f, // 선 두께 지정
                    cellRect.height), // 선 높이 지정
                Texture2D.whiteTexture); // 흰 텍스처 색상 적용
        }

        private static bool IsLocalRectVisible( // 미니맵 영역 교차 여부 확인
            Rect rect, // 검사 사각형
            float width, // 지도 너비
            float height) // 지도 높이
        {
            return rect.xMax >= 0f // 왼쪽 화면 바깥 제외
                && rect.yMax >= 0f // 위쪽 화면 바깥 제외
                && rect.xMin <= width // 오른쪽 화면 바깥 제외
                && rect.yMin <= height; // 아래쪽 화면 바깥 제외
        }

        private static void DrawCurrentRoomTypeHeader(
            DungeonMinimapSnapshot snapshot,
            float width,
            float height)
        {
            if (!snapshot.TryGetRoom(
                    snapshot.CurrentRoomId,
                    out DungeonMinimapRoomEntry currentRoom))
            {
                return;
            }

            GUIStyle style =
                new GUIStyle(DungeonMinimapRuntimeGuiProxy.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 14
                };

            style.normal.textColor =
                Color.white;

            string shortLabel =
                RoomTypeRules.GetShortLabel(
                    currentRoom.RoomType);

            string displayName =
                RoomTypeRules.GetDisplayName(
                    currentRoom.RoomType);

            DungeonMinimapRuntimeGuiProxy.Label(
                new Rect(
                    4f,
                    2f,
                    width - 8f,
                    height - 4f),
                $"[{shortLabel}] {displayName}",
                style);
        }

        private static Rect GetLocalEdgeRect(
            Rect cellRect,
            CardinalDirection direction,
            float thickness,
            bool shortenForDoor)
        {
            float inset =
                shortenForDoor
                    ? Mathf.Min(
                        cellRect.width,
                        cellRect.height) * 0.24f
                    : 0f;

            switch (direction)
            {
                case CardinalDirection.East:
                    return new Rect(
                        cellRect.xMax - (thickness * 0.5f),
                        cellRect.y + inset,
                        thickness,
                        Mathf.Max(1f, cellRect.height - (inset * 2f)));

                case CardinalDirection.South:
                    return new Rect(
                        cellRect.x + inset,
                        cellRect.yMax - (thickness * 0.5f),
                        Mathf.Max(1f, cellRect.width - (inset * 2f)),
                        thickness);

                case CardinalDirection.West:
                    return new Rect(
                        cellRect.x - (thickness * 0.5f),
                        cellRect.y + inset,
                        thickness,
                        Mathf.Max(1f, cellRect.height - (inset * 2f)));

                default:
                    return new Rect(
                        cellRect.x + inset,
                        cellRect.y - (thickness * 0.5f),
                        Mathf.Max(1f, cellRect.width - (inset * 2f)),
                        thickness);
            }
        }

        private static bool IsLocalBoundaryEdge(
            ProjectDelta.Data.RoomDefinition definition,
            GridPosition position,
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return position.Z == definition.MaxZ;

                case CardinalDirection.East:
                    return position.X == definition.MaxX;

                case CardinalDirection.South:
                    return position.Z == definition.MinZ;

                case CardinalDirection.West:
                    return position.X == definition.MinX;

                default:
                    return false;
            }
        }

        private void DrawCurrentCenteredMap(
            Rect panelRect,
            DungeonMinimapSnapshot snapshot,
            float preferredSpacing,
            float markerSize,
            float iconSize)
        {
            float centerX =
                panelRect.width * 0.5f;

            float centerY =
                panelRect.height * 0.5f;

            float spacing =
                CalculateCurrentCenteredSpacing(
                    snapshot,
                    panelRect.width,
                    preferredSpacing,
                    markerSize);

            DungeonMinimapRuntimeGuiProxy.BeginGroup(panelRect);

            for (int i = 0;
                 i < snapshot.Rooms.Count;
                 i++)
            {
                DungeonMinimapRoomEntry room =
                    snapshot.Rooms[i];

                if (!revealTracker.IsRevealed(
                        room.RoomId))
                {
                    continue;
                }

                GridPosition relative =
                    DungeonMinimapSnapshotBuilder
                        .GetRelativeCoordinate(
                            room.MacroCoordinate,
                            snapshot.CurrentMacroCoordinate);

                Vector2 markerCenter =
                    new Vector2(
                        centerX
                        + (relative.X * spacing),
                        centerY
                        - (relative.Z * spacing));

                DrawRoomMarker(
                    markerCenter,
                    room.State,
                    room.RoomType,
                    markerSize,
                    panelRect.width,
                    panelRect.height);
            }

            DrawPlayerDirection(
                new Vector2(
                    centerX,
                    centerY),
                iconSize);

            DungeonMinimapRuntimeGuiProxy.EndGroup();
        }

        private void DrawBoundsCenteredMap(
            Rect graphRect,
            DungeonMinimapSnapshot snapshot,
            GeneratedDungeon dungeon)
        {
            DungeonMapBounds bounds =
                DungeonMapAnalytics
                    .CalculateRevealedBounds(
                        snapshot,
                        revealTracker.RevealedRoomIds);

            if (!bounds.HasRooms)
            {
                return;
            }

            float baseMarkerSize =
                roomMarkerSize
                * fullMapContentScale;

            float baseIconSize =
                playerIconSize
                * fullMapContentScale;

            float preferredSpacing =
                roomSpacing
                * fullMapContentScale;

            float baseSpacing =
                CalculateBoundsCenteredSpacing(
                    bounds,
                    graphRect.width,
                    graphRect.height,
                    preferredSpacing,
                    baseMarkerSize);

            float spacing =
                baseSpacing * fullMapZoom;

            float markerSize =
                baseMarkerSize * fullMapZoom;

            float iconSize =
                baseIconSize * fullMapZoom;

            float connectionThickness =
                Mathf.Max(
                    1f,
                    fullMapConnectionThickness
                    * fullMapZoom);

            Vector2 pixelCenter =
                new Vector2(
                    graphRect.width * 0.5f,
                    graphRect.height * 0.5f);

            DungeonMinimapRuntimeGuiProxy.BeginGroup(graphRect);

            IReadOnlyList<DungeonMapConnection> connections =
                DungeonMapAnalytics.GetVisibleConnections(
                    dungeon,
                    revealTracker.RevealedRoomIds);

            for (int i = 0;
                 i < connections.Count;
                 i++)
            {
                DungeonMapConnection connection =
                    connections[i];

                if (!snapshot.TryGetRoom(
                        connection.FromRoomId,
                        out DungeonMinimapRoomEntry fromRoom)
                    || !snapshot.TryGetRoom(
                        connection.ToRoomId,
                        out DungeonMinimapRoomEntry toRoom))
                {
                    continue;
                }

                Vector2 fromPoint =
                    GetBoundsCenteredPoint(
                        fromRoom.MacroCoordinate,
                        bounds,
                        pixelCenter,
                        spacing);

                Vector2 toPoint =
                    GetBoundsCenteredPoint(
                        toRoom.MacroCoordinate,
                        bounds,
                        pixelCenter,
                        spacing);

                DrawConnectionLine(
                    fromPoint,
                    toPoint,
                    connectionThickness);
            }

            for (int i = 0;
                 i < snapshot.Rooms.Count;
                 i++)
            {
                DungeonMinimapRoomEntry room =
                    snapshot.Rooms[i];

                if (!revealTracker.IsRevealed(
                        room.RoomId))
                {
                    continue;
                }

                Vector2 markerCenter =
                    GetBoundsCenteredPoint(
                        room.MacroCoordinate,
                        bounds,
                        pixelCenter,
                        spacing);

                DrawRoomMarker(
                    markerCenter,
                    room.State,
                    room.RoomType,
                    markerSize,
                    graphRect.width,
                    graphRect.height);
            }

            Vector2 playerPoint =
                GetBoundsCenteredPoint(
                    snapshot.CurrentMacroCoordinate,
                    bounds,
                    pixelCenter,
                    spacing);

            DrawPlayerDirection(
                playerPoint,
                iconSize);

            DungeonMinimapRuntimeGuiProxy.EndGroup();
        }

        private float CalculateCurrentCenteredSpacing(
            DungeonMinimapSnapshot snapshot,
            float panelWidth,
            float preferredSpacing,
            float markerSize)
        {
            int maxDistance = 1;

            for (int i = 0;
                 i < snapshot.Rooms.Count;
                 i++)
            {
                DungeonMinimapRoomEntry room =
                    snapshot.Rooms[i];

                if (!revealTracker.IsRevealed(
                        room.RoomId))
                {
                    continue;
                }

                GridPosition relative =
                    DungeonMinimapSnapshotBuilder
                        .GetRelativeCoordinate(
                            room.MacroCoordinate,
                            snapshot.CurrentMacroCoordinate);

                maxDistance =
                    Mathf.Max(
                        maxDistance,
                        Mathf.Abs(relative.X),
                        Mathf.Abs(relative.Z));
            }

            float usableHalfSize =
                Mathf.Max(
                    1f,
                    (panelWidth * 0.5f)
                    - markerSize);

            float fitSpacing =
                usableHalfSize / maxDistance;

            return Mathf.Min(
                preferredSpacing,
                fitSpacing);
        }

        private static float CalculateBoundsCenteredSpacing(
            DungeonMapBounds bounds,
            float panelWidth,
            float panelHeight,
            float preferredSpacing,
            float markerSize)
        {
            int spanX =
                Mathf.Max(
                    1,
                    bounds.MaxX - bounds.MinX);

            int spanZ =
                Mathf.Max(
                    1,
                    bounds.MaxZ - bounds.MinZ);

            float usableWidth =
                Mathf.Max(
                    1f,
                    panelWidth - (markerSize * 2f));

            float usableHeight =
                Mathf.Max(
                    1f,
                    panelHeight - (markerSize * 2f));

            float fitX =
                usableWidth / spanX;

            float fitZ =
                usableHeight / spanZ;

            return Mathf.Min(
                preferredSpacing,
                fitX,
                fitZ);
        }

        private static Vector2 GetBoundsCenteredPoint(
            GridPosition coordinate,
            DungeonMapBounds bounds,
            Vector2 pixelCenter,
            float spacing)
        {
            return new Vector2(
                pixelCenter.x
                + ((coordinate.X - bounds.CenterX) * spacing),
                pixelCenter.y
                - ((coordinate.Z - bounds.CenterZ) * spacing));
        }

        private static void DrawConnectionLine(
            Vector2 from,
            Vector2 to,
            float thickness)
        {
            Color previousColor =
                DungeonMinimapRuntimeGuiProxy.color;

            DungeonMinimapRuntimeGuiProxy.color =
                ConnectionColor;

            if (Mathf.Abs(from.x - to.x)
                >= Mathf.Abs(from.y - to.y))
            {
                float x =
                    Mathf.Min(
                        from.x,
                        to.x);

                float width =
                    Mathf.Abs(
                        from.x - to.x);

                DungeonMinimapRuntimeGuiProxy.DrawTexture(
                    new Rect(
                        x,
                        from.y - (thickness * 0.5f),
                        width,
                        thickness),
                    Texture2D.whiteTexture);
            }
            else
            {
                float y =
                    Mathf.Min(
                        from.y,
                        to.y);

                float height =
                    Mathf.Abs(
                        from.y - to.y);

                DungeonMinimapRuntimeGuiProxy.DrawTexture(
                    new Rect(
                        from.x - (thickness * 0.5f),
                        y,
                        thickness,
                        height),
                    Texture2D.whiteTexture);
            }

            DungeonMinimapRuntimeGuiProxy.color =
                previousColor;
        }

        private static void DrawRoomMarker(
            Vector2 center,
            DungeonMinimapRoomState state,
            RoomType roomType,
            float markerSize,
            float panelWidth,
            float panelHeight)
        {
            float half =
                markerSize * 0.5f;

            Rect markerRect =
                new Rect(
                    center.x - half,
                    center.y - half,
                    markerSize,
                    markerSize);

            if (markerRect.xMax < 0f
                || markerRect.yMax < 0f
                || markerRect.xMin > panelWidth
                || markerRect.yMin > panelHeight)
            {
                return;
            }

            Color previousColor =
                DungeonMinimapRuntimeGuiProxy.color;

            DungeonMinimapRuntimeGuiProxy.color =
                GetRoomColor(state);

            DungeonMinimapRuntimeGuiProxy.DrawTexture(
                markerRect,
                Texture2D.whiteTexture);

            if (state
                == DungeonMinimapRoomState.Current)
            {
                float innerPadding =
                    Mathf.Max(
                        3f,
                        markerSize * 0.125f);

                Rect innerRect =
                    new Rect(
                        markerRect.x + innerPadding,
                        markerRect.y + innerPadding,
                        markerRect.width
                        - (innerPadding * 2f),
                        markerRect.height
                        - (innerPadding * 2f));

                DungeonMinimapRuntimeGuiProxy.color =
                    new Color(
                        0.12f,
                        0.12f,
                        0.12f,
                        0.92f);

                DungeonMinimapRuntimeGuiProxy.DrawTexture(
                    innerRect,
                    Texture2D.whiteTexture);
            }

            if (state != DungeonMinimapRoomState.Unvisited)
            {
                GUIStyle labelStyle =
                    new GUIStyle(DungeonMinimapRuntimeGuiProxy.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = Mathf.Max(
                            10,
                            Mathf.RoundToInt(markerSize * 0.52f))
                    };

                labelStyle.normal.textColor =
                    Color.white;

                DungeonMinimapRuntimeGuiProxy.color =
                    Color.white;

                DungeonMinimapRuntimeGuiProxy.Label(
                    markerRect,
                    RoomTypeRules.GetShortLabel(roomType),
                    labelStyle);
            }

            DungeonMinimapRuntimeGuiProxy.color =
                previousColor;
        }

        private void DrawPlayerDirection(
            Vector2 center,
            float iconSize)
        {
            if (playerIconTexture == null)
            {
                return;
            }

            float yaw =
                viewTransform != null
                    ? viewTransform.eulerAngles.y
                    : transform.eulerAngles.y;

            CardinalDirection facing =
                GridMovement.GetFacingFromYaw(yaw);

            float angle =
                GetGuiRotationAngle(facing);

            float half =
                iconSize * 0.5f;

            Rect iconRect =
                new Rect(
                    center.x - half,
                    center.y - half,
                    iconSize,
                    iconSize);

            Matrix4x4 previousMatrix =
                DungeonMinimapRuntimeGuiProxy.matrix;

            Color previousColor =
                DungeonMinimapRuntimeGuiProxy.color;

            DungeonMinimapRuntimeGuiProxy.RotateAroundPivot(
                angle,
                center);

            DungeonMinimapRuntimeGuiProxy.color =
                Color.white;

            DungeonMinimapRuntimeGuiProxy.DrawTexture(
                iconRect,
                playerIconTexture);

            DungeonMinimapRuntimeGuiProxy.matrix =
                previousMatrix;

            DungeonMinimapRuntimeGuiProxy.color =
                previousColor;
        }

        private static void DrawFullScreenDim()
        {
            Color previousColor =
                DungeonMinimapRuntimeGuiProxy.color;

            DungeonMinimapRuntimeGuiProxy.color =
                new Color(
                    0f,
                    0f,
                    0f,
                    0.6f);

            DungeonMinimapRuntimeGuiProxy.DrawTexture(
                new Rect(
                    0f,
                    0f,
                    Screen.width,
                    Screen.height),
                Texture2D.whiteTexture);

            DungeonMinimapRuntimeGuiProxy.color =
                previousColor;
        }

        private static void DrawPanelBackground(
            Rect panelRect)
        {
            Color previousColor =
                DungeonMinimapRuntimeGuiProxy.color;

            DungeonMinimapRuntimeGuiProxy.color =
                PanelColor;

            DungeonMinimapRuntimeGuiProxy.DrawTexture(
                panelRect,
                Texture2D.whiteTexture);

            DungeonMinimapRuntimeGuiProxy.color =
                previousColor;
        }

        private static Color GetRoomColor(
            DungeonMinimapRoomState state)
        {
            switch (state)
            {
                case DungeonMinimapRoomState.Current:
                    return CurrentRoomColor;

                case DungeonMinimapRoomState.Visited:
                    return VisitedRoomColor;

                default:
                    return UnvisitedRoomColor;
            }
        }

        private static float GetGuiRotationAngle(
            CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.East:
                    return 90f;

                case CardinalDirection.South:
                    return 180f;

                case CardinalDirection.West:
                    return 270f;

                default:
                    return 0f;
            }
        }

        private static Texture2D CreatePlayerIconTexture(
            int size)
        {
            Texture2D texture =
                new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false);

            texture.name =
                "DungeonMinimapPlayerDirection";

            texture.filterMode =
                FilterMode.Point;

            texture.wrapMode =
                TextureWrapMode.Clamp;

            Color[] pixels =
                new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                float normalizedY =
                    y / (float)(size - 1);

                float halfWidth =
                    normalizedY
                    * (size * 0.42f);

                float centerX =
                    (size - 1) * 0.5f;

                for (int x = 0; x < size; x++)
                {
                    int textureY =
                        (size - 1) - y;

                    int index =
                        (textureY * size) + x;

                    pixels[index] =
                        Mathf.Abs(
                            x - centerX)
                        <= halfWidth
                            ? Color.white
                            : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
    }
}
