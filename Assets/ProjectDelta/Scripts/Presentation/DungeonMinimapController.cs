using System.Collections.Generic;
using ProjectDelta.Domain;
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

        private readonly DungeonMinimapRevealTracker revealTracker =
            new DungeonMinimapRevealTracker();

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

        private void OnGUI()
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

            DrawBoundsCenteredMap(
                graphRect,
                snapshot,
                dungeon);

            DrawFullMapHint(panelRect);
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
                new GUIStyle(GUI.skin.label)
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

            GUI.Label(
                new Rect(
                    left,
                    panelRect.y + 8f,
                    width,
                    22f),
                $"층 진행도 : {currentFloor} / {displayTotalFloorCount}",
                infoStyle);

            GUI.Label(
                new Rect(
                    left,
                    panelRect.y + 30f,
                    width,
                    22f),
                $"탐험률 : {progress.ExploredRoomCount} / {progress.TotalRoomCount}  ({progress.ExplorationPercent:0}%)",
                infoStyle);

            GUI.Label(
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
                new GUIStyle(GUI.skin.label)
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

            GUI.Label(
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

            GUI.BeginGroup(panelRect);

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
                    markerSize,
                    panelRect.width,
                    panelRect.height);
            }

            DrawPlayerDirection(
                new Vector2(
                    centerX,
                    centerY),
                iconSize);

            GUI.EndGroup();
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

            GUI.BeginGroup(graphRect);

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

            GUI.EndGroup();
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
                GUI.color;

            GUI.color =
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

                GUI.DrawTexture(
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

                GUI.DrawTexture(
                    new Rect(
                        from.x - (thickness * 0.5f),
                        y,
                        thickness,
                        height),
                    Texture2D.whiteTexture);
            }

            GUI.color =
                previousColor;
        }

        private static void DrawRoomMarker(
            Vector2 center,
            DungeonMinimapRoomState state,
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
                GUI.color;

            GUI.color =
                GetRoomColor(state);

            GUI.DrawTexture(
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

                GUI.color =
                    new Color(
                        0.12f,
                        0.12f,
                        0.12f,
                        0.92f);

                GUI.DrawTexture(
                    innerRect,
                    Texture2D.whiteTexture);
            }

            GUI.color =
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
                GUI.matrix;

            Color previousColor =
                GUI.color;

            GUIUtility.RotateAroundPivot(
                angle,
                center);

            GUI.color =
                Color.white;

            GUI.DrawTexture(
                iconRect,
                playerIconTexture);

            GUI.matrix =
                previousMatrix;

            GUI.color =
                previousColor;
        }

        private static void DrawFullScreenDim()
        {
            Color previousColor =
                GUI.color;

            GUI.color =
                new Color(
                    0f,
                    0f,
                    0f,
                    0.6f);

            GUI.DrawTexture(
                new Rect(
                    0f,
                    0f,
                    Screen.width,
                    Screen.height),
                Texture2D.whiteTexture);

            GUI.color =
                previousColor;
        }

        private static void DrawPanelBackground(
            Rect panelRect)
        {
            Color previousColor =
                GUI.color;

            GUI.color =
                PanelColor;

            GUI.DrawTexture(
                panelRect,
                Texture2D.whiteTexture);

            GUI.color =
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
