using ProjectDelta.Domain;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectDelta.Presentation
{
    // 37일차: GeneratedDungeon의 방 그래프를 화면 우측 상단 미니맵으로 표시한다.
    // 현재 방 주변 8칸에서 한 번 발견한 방만 지도에 남긴다.
    // M키로 같은 그래프를 화면 중앙의 큰 지도 패널로 열고 닫을 수 있다.
    // 38일차 연결선/확대축소/탐험률 기능과 39일차 저장 기능은 여기서 다루지 않는다.
    public sealed class DungeonMinimapController : MonoBehaviour
    {
        [SerializeField] private PlayerGridMovementController movementController;
        [SerializeField] private Transform viewTransform;
        [SerializeField] private float mapSize = 240f;
        [SerializeField] private float margin = 20f;
        [SerializeField] private float roomSpacing = 34f;
        [SerializeField] private float roomMarkerSize = 24f;
        [SerializeField] private float playerIconSize = 15f;

        // 이전 미니맵 컨트롤러에서 사용하던 필드명을 유지해 기존 Scene 직렬화 값도 재사용한다.
        [SerializeField] private float fullMapScale = 5f;
        [SerializeField] private float fullMapMaxScreenRatio = 0.92f;
        [SerializeField] private float fullMapContentScale = 1.5f;

        private readonly DungeonMinimapRevealTracker revealTracker =
            new DungeonMinimapRevealTracker();

        private DungeonFloorController floorController;
        private Texture2D playerIconTexture;
        private bool isFullMapOpen;

        private static readonly Color PanelColor =
            new Color(0f, 0f, 0f, 0.58f);

        private static readonly Color UnvisitedRoomColor =
            new Color(0.18f, 0.18f, 0.18f, 0.82f);

        private static readonly Color VisitedRoomColor =
            new Color(0.72f, 0.72f, 0.72f, 0.94f);

        private static readonly Color CurrentRoomColor =
            new Color(1f, 0.78f, 0.25f, 1f);

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

            if (isFullMapOpen
                && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isFullMapOpen = false;
            }
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

            // 현재 방 주변 8칸에서 실제로 존재하는 방을 발견 상태로 누적한다.
            // 다른 방으로 이동해도 이미 발견한 방은 현재 층이 끝날 때까지 유지된다.
            revealTracker.Update(
                dungeon,
                currentRoomId);

            if (isFullMapOpen)
            {
                DrawFullMap(snapshot);
                return;
            }

            Rect panelRect = new Rect(
                Screen.width - margin - mapSize,
                margin,
                mapSize,
                mapSize);

            DrawPanelBackground(panelRect);

            DrawGraphMap(
                panelRect,
                snapshot,
                roomSpacing,
                roomMarkerSize,
                playerIconSize);
        }

        private void DrawFullMap(
            DungeonMinimapSnapshot snapshot)
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

            DrawGraphMap(
                panelRect,
                snapshot,
                roomSpacing * fullMapContentScale,
                roomMarkerSize * fullMapContentScale,
                playerIconSize * fullMapContentScale);
        }

        private void DrawGraphMap(
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
                CalculateFittedSpacing(
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

        private float CalculateFittedSpacing(
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

                maxDistance = Mathf.Max(
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

            GUI.color = Color.white;

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
