using System.Collections.Generic; // 출구 마커 사전 사용
using ProjectDelta.Application; // 110일차: RoomTypeRollService 사용
using ProjectDelta.Data; // RoomDefinition 사용
using ProjectDelta.Domain; // 통로 규칙 사용
using UnityEngine; // Unity 기본 기능 사용

namespace ProjectDelta.Presentation
{
    public enum TestRoomLayoutKind
    {
        Primary,
        Secondary
    }

    public sealed class RoomPassageController : MonoBehaviour
    {
        [SerializeField] private string roomId = "TestRoom_A";
        [SerializeField] private RoomDefinition roomDefinition;
        [SerializeField] private TestRoomLayoutKind layoutKind = TestRoomLayoutKind.Primary;
        [SerializeField] private Transform unlockedDoorVisual;
        [SerializeField] private Transform lockedDoorVisual;
        [SerializeField] private Transform boundaryDoorVisual;

        private RoomInstance roomInstance;
        private RoomGridLayout layout;
        private GridPassage unlockedDoorPassage;
        private GridPassage lockedDoorPassage;
        private GridPassage boundaryDoorPassage;

        private readonly Dictionary<RoomExit, RoomExitMarker> generatedExitMarkers =
            new Dictionary<RoomExit, RoomExitMarker>();

        private const float GeneratedWallHeight = 2.5f;
        private const float GeneratedWallThickness = 0.2f;
        private const float GeneratedDoorWidth = 1.8f;
        private const float GeneratedDoorHeight = 2.2f;
        private const string GeneratedWallBlockerName = "GeneratedWallBlocker";
        private const string GeneratedDoorLintelName = "GeneratedDoorLintel";

        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private static readonly Color GeneratedDoorColor = new Color(0.60f, 0.60f, 0.60f, 1f);
        private MaterialPropertyBlock doorColorPropertyBlock;

        public string RoomId => roomId;
        public RoomDefinition RoomDefinition => roomDefinition;
        public TestRoomLayoutKind LayoutKind => layoutKind;
        public GridPosition BoundaryPosition =>
            layoutKind == TestRoomLayoutKind.Primary ? new GridPosition(0, 2) : new GridPosition(0, -2);
        public CardinalDirection BoundaryDirection =>
            layoutKind == TestRoomLayoutKind.Primary ? CardinalDirection.North : CardinalDirection.South;
        public GridPassage BoundaryDoorPassage => boundaryDoorPassage;
        public RoomInstance CurrentInstance => roomInstance;

        private void Awake()
        {
            InitializeRoom(roomId, roomDefinition, false);
        }

        private void Update()
        {
            RefreshDoorVisuals();
        }

        // 36일차: 같은 프리팹을 여러 RoomNode로 사용할 수 있도록 런타임 ID와 정의로 다시 초기화한다.
        // prepareGeneratedExits=true이면 실제 그래프 연결이 확정되기 전 모든 경계 출구를 벽으로 막아둔다.
        public void ConfigureRuntime(string runtimeRoomId, RoomDefinition definition, bool prepareGeneratedExits)
        {
            if (string.IsNullOrEmpty(runtimeRoomId))
            {
                Debug.LogError("[Project Delta] 생성 방 Runtime RoomId가 비어있습니다.", this);
                return;
            }

            roomId = runtimeRoomId;
            roomDefinition = definition;
            InitializeRoom(roomId, roomDefinition, prepareGeneratedExits);
        }

        private void InitializeRoom(string targetRoomId, RoomDefinition definition, bool blockGeneratedExits)
        {
            if (definition == null)
            {
                Debug.LogError($"[Project Delta] {targetRoomId}에 RoomDefinition이 지정되지 않았습니다.", this);
            }

            roomInstance = RoomInstance.Create(
                targetRoomId,
                definition != null ? definition.Id : targetRoomId,
                definition != null ? definition.Passages : null);

            layout = roomInstance.Layout;

            if (RunContext.Current != null)
            {
                RunContext.Current.Dungeon.Register(roomInstance);

                if (DungeonSaveMapper.TryGetRoomState(targetRoomId, out RoomRunState savedState))
                {
                    roomInstance.ApplySavedState(
                        savedState.Visited,
                        savedState.Completed,
                        savedState.ChestOpened,
                        savedState.RoomType,
                        savedState.TrapTriggered,
                        savedState.EventTriggered);
                }
                else
                {
                    // 110일차: 저장된 상태가 없는 새 방이면 종류를 한 번 굴려서 확정한다.
                    // 무작위 판정은 Application 계층(RoomTypeRollService)이 담당한다.
                    roomInstance.SetRoomType(
                        RoomTypeRollService.Roll());
                }
            }

            unlockedDoorPassage = null;
            lockedDoorPassage = null;
            boundaryDoorPassage = null;

            if (layoutKind == TestRoomLayoutKind.Primary && layout != null)
            {
                unlockedDoorPassage = layout.GetPassage(new GridPosition(0, 0), CardinalDirection.North);
                lockedDoorPassage = layout.GetPassage(new GridPosition(1, 0), CardinalDirection.North);
            }

            if (layout != null)
            {
                boundaryDoorPassage = layout.GetPassage(BoundaryPosition, BoundaryDirection);
            }

            CollectGeneratedExitMarkers();

            if (blockGeneratedExits && definition != null && layout != null)
            {
                foreach (PassageEntry entry in definition.GetExits())
                {
                    RoomExit exit = new RoomExit(
                        new GridPosition(entry.X, entry.Z),
                        entry.Direction);

                    layout.SetPassage(exit.LocalPosition, exit.Direction, GridPassage.CreateWall());
                }
            }

            RefreshDoorVisuals();
        }

        private void CollectGeneratedExitMarkers()
        {
            generatedExitMarkers.Clear();

            foreach (RoomExitMarker marker in GetComponentsInChildren<RoomExitMarker>(true))
            {
                generatedExitMarkers[marker.Exit] = marker;
                ApplyGeneratedDoorColor(marker);
            }
        }

        private void ApplyGeneratedDoorColor(RoomExitMarker marker)
        {
            if (marker == null)
            {
                return;
            }

            Transform visual = marker.transform.Find("DoorVisual");

            if (visual == null)
            {
                return;
            }

            if (doorColorPropertyBlock == null)
            {
                doorColorPropertyBlock = new MaterialPropertyBlock();
            }

            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                doorColorPropertyBlock.Clear();
                renderer.GetPropertyBlock(doorColorPropertyBlock);
                doorColorPropertyBlock.SetColor(BaseColorProperty, GeneratedDoorColor);
                doorColorPropertyBlock.SetColor(ColorProperty, GeneratedDoorColor);
                renderer.SetPropertyBlock(doorColorPropertyBlock);
            }
        }

        public bool CanPass(GridPosition position, CardinalDirection direction)
        {
            return layout != null && layout.CanPass(position, direction);
        }

        public bool TryGetDoor(
            GridPosition position,
            CardinalDirection direction,
            out GridPassage doorPassage)
        {
            doorPassage = layout != null ? layout.GetPassage(position, direction) : null;

            if (doorPassage == null || doorPassage.Type != PassageType.Door)
            {
                doorPassage = null;
                return false;
            }

            return true;
        }

        public DoorOpenResult TryOpenDoor(
            GridPosition position,
            CardinalDirection direction,
            PlayerRunState playerState)
        {
            if (!TryGetDoor(position, direction, out GridPassage doorPassage))
            {
                return DoorOpenResult.NotDoor;
            }

            DoorOpenResult result = doorPassage.TryOpenDoor(playerState);

            if (result == DoorOpenResult.Opened)
            {
                RefreshDoorVisuals();
            }

            return result;
        }

        // 기존 2방 테스트 연결 호환 API
        public void SetBoundaryDoorPassage(GridPassage sharedPassage)
        {
            if (sharedPassage == null || layout == null)
            {
                return;
            }

            boundaryDoorPassage = sharedPassage;
            layout.SetPassage(BoundaryPosition, BoundaryDirection, boundaryDoorPassage);
            RefreshDoorVisuals();
        }

        // 36일차: 정확한 RoomExit 위치에 그래프의 공유 문을 연결한다.
        public bool SetGeneratedDoorPassage(RoomExit exit, GridPassage sharedPassage)
        {
            if (layout == null || sharedPassage == null)
            {
                return false;
            }

            if (!generatedExitMarkers.ContainsKey(exit))
            {
                Debug.LogError(
                    $"[Project Delta] {roomId} 프리팹에서 그래프 출구 {exit}에 대응하는 RoomExitMarker를 찾을 수 없습니다.",
                    this);
                return false;
            }

            layout.SetPassage(exit.LocalPosition, exit.Direction, sharedPassage);
            RefreshDoorVisuals();
            return true;
        }

        public bool TryGetGeneratedPassage(RoomExit exit, out GridPassage passage)
        {
            passage = layout != null
                ? layout.GetPassage(exit.LocalPosition, exit.Direction)
                : null;

            return passage != null;
        }

        private void RefreshDoorVisuals()
        {
            if (unlockedDoorVisual != null)
            {
                unlockedDoorVisual.gameObject.SetActive(
                    unlockedDoorPassage == null || !unlockedDoorPassage.IsOpen);
            }

            if (lockedDoorVisual != null)
            {
                lockedDoorVisual.gameObject.SetActive(
                    lockedDoorPassage == null || !lockedDoorPassage.IsOpen);
            }

            if (boundaryDoorVisual != null)
            {
                boundaryDoorVisual.gameObject.SetActive(
                    boundaryDoorPassage == null || !boundaryDoorPassage.IsOpen);
            }

            if (layout == null)
            {
                return;
            }

            foreach (KeyValuePair<RoomExit, RoomExitMarker> pair in generatedExitMarkers)
            {
                RoomExit exit = pair.Key;
                RoomExitMarker marker = pair.Value;

                if (marker == null)
                {
                    continue;
                }

                GridPassage passage = layout.GetPassage(exit.LocalPosition, exit.Direction);
                RefreshGeneratedExitVisual(marker, exit, passage);
            }
        }

        private void RefreshGeneratedExitVisual(
            RoomExitMarker marker,
            RoomExit exit,
            GridPassage passage)
        {
            Transform doorVisual = marker.transform.Find("DoorVisual");
            Transform wallBlocker = GetOrCreateWallBlocker(marker, exit);
            Transform lintel = GetOrCreateDoorLintel(marker, exit);

            bool isDoor = passage != null && passage.Type == PassageType.Door;

            if (doorVisual != null)
            {
                // 실제 Door일 때만 회색 문을 사용한다.
                // 열린 문은 문 판만 사라지고 위쪽 고정 벽은 남는다.
                doorVisual.gameObject.SetActive(isDoor && !passage.CanPass());
            }

            if (wallBlocker != null)
            {
                // 연결되지 않은 출구는 문처럼 보이지 않도록 실제 고정 벽으로 완전히 막는다.
                wallBlocker.gameObject.SetActive(!isDoor);
            }

            if (lintel != null)
            {
                // DoorHeight 2.2와 WallHeight 2.5 사이의 0.3 공간을 고정 벽으로 채운다.
                lintel.gameObject.SetActive(isDoor);
            }
        }

        private Transform GetOrCreateWallBlocker(RoomExitMarker marker, RoomExit exit)
        {
            Transform existing = marker.transform.Find(GeneratedWallBlockerName);

            if (existing != null)
            {
                return existing;
            }

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = GeneratedWallBlockerName;
            wall.transform.SetParent(marker.transform, false);

            // RoomExitMarker 중심 Y는 DoorHeight * 0.5 = 1.1이다.
            // 전체 벽 중심 Y 1.25에 맞추기 위해 로컬 Y를 0.15 올린다.
            wall.transform.localPosition = new Vector3(
                0f,
                (GeneratedWallHeight - GeneratedDoorHeight) * 0.5f,
                0f);

            wall.transform.localRotation = Quaternion.identity;

            if (exit.Direction == CardinalDirection.North
                || exit.Direction == CardinalDirection.South)
            {
                wall.transform.localScale = new Vector3(
                    GeneratedDoorWidth,
                    GeneratedWallHeight,
                    GeneratedWallThickness);
            }
            else
            {
                wall.transform.localScale = new Vector3(
                    GeneratedWallThickness,
                    GeneratedWallHeight,
                    GeneratedDoorWidth);
            }

            ApplyFixedWallMaterial(wall);

            Collider collider = wall.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            return wall.transform;
        }

        private Transform GetOrCreateDoorLintel(RoomExitMarker marker, RoomExit exit)
        {
            Transform existing = marker.transform.Find(GeneratedDoorLintelName);

            if (existing != null)
            {
                return existing;
            }

            float lintelHeight = GeneratedWallHeight - GeneratedDoorHeight;

            GameObject lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lintel.name = GeneratedDoorLintelName;
            lintel.transform.SetParent(marker.transform, false);

            // 문 상단 2.2부터 벽 상단 2.5까지 정확히 채운다.
            lintel.transform.localPosition = new Vector3(
                0f,
                (GeneratedDoorHeight * 0.5f) + (lintelHeight * 0.5f),
                0f);

            lintel.transform.localRotation = Quaternion.identity;

            if (exit.Direction == CardinalDirection.North
                || exit.Direction == CardinalDirection.South)
            {
                lintel.transform.localScale = new Vector3(
                    GeneratedDoorWidth,
                    lintelHeight,
                    GeneratedWallThickness);
            }
            else
            {
                lintel.transform.localScale = new Vector3(
                    GeneratedWallThickness,
                    lintelHeight,
                    GeneratedDoorWidth);
            }

            ApplyFixedWallMaterial(lintel);

            Collider collider = lintel.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            return lintel.transform;
        }

        private void ApplyFixedWallMaterial(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Renderer targetRenderer = target.GetComponent<Renderer>();

            if (targetRenderer == null)
            {
                return;
            }

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null
                    || renderer == targetRenderer
                    || !renderer.gameObject.name.StartsWith("Wall_"))
                {
                    continue;
                }

                targetRenderer.sharedMaterial = renderer.sharedMaterial;
                return;
            }
        }
    }
}
