using ProjectDelta.Domain; // 그리드 좌표·통로 규칙 사용
using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.InputSystem; // M/Esc 키 직접 조회용

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    // 23일차: 화면 우측 상단에 현재 방을 표시하는 미니맵.
    // 플레이어는 항상 지도 중심에 고정되고, 방의 칸·벽·문·콘텐츠 자리가 그 주위에 그려진다.
    // M키를 누르면 같은 내용을 화면 중앙에 크게 띄우는 전체 지도로 전환되고, Esc로 닫힌다.
    // TODO: 방-방 연결 그래프가 없어서(26~35일차 던전 생성 이전) "전체 지도"도 지금은
    // 현재 방 하나만 크게 보여준다. 여러 방을 이어 보여주는 실제 던전 지도는 그 연결
    // 데이터가 생긴 뒤로 미룬다.
    public sealed class DungeonMinimapController : MonoBehaviour // 미니맵·전체 지도 표시 제어
    {
        [SerializeField] private PlayerGridMovementController movementController; // 플레이어 이동 상태 컨트롤러
        [SerializeField] private Transform viewTransform; // 바라보는 방향 기준 Transform
        [SerializeField] private float mapSize = 240f; // 미니맵 사각형 한 변 크기(px)
        [SerializeField] private float margin = 20f; // 화면 가장자리에서의 여백(px)
        [SerializeField] private float cellPixelSize = 39f; // 그리드 한 칸의 미니맵 표시 크기(px)
        [SerializeField] private float wallThickness = 4.5f; // 벽 표시 두께(px)
        [SerializeField] private float doorMarkerSize = 12f; // 문 표시 크기(px)
        [SerializeField] private float contentMarkerSize = 13.5f; // 콘텐츠 자리 표시 크기(px)
        [SerializeField] private float playerIconSize = 21f; // 플레이어 삼각형 아이콘 크기(px)
        [SerializeField] private float fullMapScale = 5f; // 전체 지도 패널이 미니맵 대비 몇 배 큰지 (화면에 맞춰 아래에서 한 번 더 제한됨)
        [SerializeField] private float fullMapMaxScreenRatio = 0.92f; // 전체 지도 패널이 화면 짧은 변을 넘지 않도록 제한하는 비율
        [SerializeField] private float fullMapContentScale = 1.5f; // 전체 지도 안 칸/벽/문/아이콘 크기를 미니맵 대비 몇 배로 키울지
        [SerializeField] private int minX = -2; // 현재 방 최소 X (PlayerGridMovementController와 동일 값)
        [SerializeField] private int maxX = 2; // 현재 방 최대 X
        [SerializeField] private int minZ = -2; // 현재 방 최소 Z
        [SerializeField] private int maxZ = 2; // 현재 방 최대 Z

        private Texture2D playerIconTexture; // 런타임 생성 삼각형 아이콘 텍스처
        private bool isFullMapOpen; // 전체 지도 패널이 열려 있는지 여부

        private void Awake() // 참조 자동 연결 및 아이콘 생성
        {
            if (movementController == null) // 이동 컨트롤러 미지정 확인
            {
                movementController = GetComponent<PlayerGridMovementController>(); // 같은 Player의 이동 컨트롤러 연결
            }

            if (viewTransform == null) // 시점 Transform 미지정 확인
            {
                Camera mainCamera = Camera.main; // 메인 카메라 검색
                viewTransform = mainCamera != null ? mainCamera.transform : transform; // 시점 기준 자동 지정
            }

            playerIconTexture = CreatePlayerIconTexture(32); // 위쪽을 가리키는 삼각형 아이콘 생성
        }

        private void Update() // M/Esc로 전체 지도 열고 닫기
        {
            if (Keyboard.current == null) // 키보드 장치 확인
            {
                return; // 입력 조회 불가, 처리 중단
            }

            if (!isFullMapOpen && Keyboard.current.mKey.wasPressedThisFrame) // 지도가 닫혀있을 때 M 입력 확인
            {
                isFullMapOpen = true; // 전체 지도 열기
            }
            else if (isFullMapOpen && Keyboard.current.escapeKey.wasPressedThisFrame) // 지도가 열려있을 때 Esc 입력 확인
            {
                isFullMapOpen = false; // 전체 지도 닫기
            }
        }

        private void OnGUI() // 미니맵 또는 전체 지도 표시
        {
            if (movementController == null || movementController.PlayerState == null) // 필요한 참조 확인
            {
                return; // 지도 표시 생략
            }

            if (isFullMapOpen) // 전체 지도 모드 확인
            {
                DrawFullScreenDim(); // 배경 화면 어둡게 처리

                float maxAllowedSize = Mathf.Min(Screen.width, Screen.height) * fullMapMaxScreenRatio; // 화면 밖으로 넘치지 않도록 짧은 변 기준 상한 계산
                float fullSize = Mathf.Min(mapSize * fullMapScale, maxAllowedSize); // 검은 패널 자체는 크게 — 배율과 화면 상한 중 더 작은 값으로 결정

                Rect panelRect = new Rect((Screen.width - fullSize) / 2f, (Screen.height - fullSize) / 2f, fullSize, fullSize); // 화면 중앙 패널 영역 계산
                DrawPanelBackground(panelRect); // 패널 배경 표시
                DrawMapPanel(
                    panelRect,
                    cellPixelSize * fullMapContentScale,
                    wallThickness * fullMapContentScale,
                    doorMarkerSize * fullMapContentScale,
                    contentMarkerSize * fullMapContentScale,
                    playerIconSize * fullMapContentScale); // 지도 내용은 미니맵보다 fullMapContentScale배 크게 표시
            }
            else // 평소 미니맵 모드
            {
                Rect mapRect = new Rect(Screen.width - margin - mapSize, margin, mapSize, mapSize); // 우측 상단 미니맵 영역 계산
                DrawPanelBackground(mapRect); // 미니맵 배경 표시
                DrawMapPanel(mapRect, cellPixelSize, wallThickness, doorMarkerSize, contentMarkerSize, playerIconSize); // 기본 크기로 지도 내용 표시
            }
        }

        private void DrawFullScreenDim() // 전체 지도가 열렸을 때 배경 화면을 어둡게 표시
        {
            Color previousColor = GUI.color; // 색상 저장
            GUI.color = new Color(0f, 0f, 0f, 0.6f); // 반투명 검은 전체 화면
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture); // 화면 전체 어둡게 표시
            GUI.color = previousColor; // 색상 복원
        }

        private static void DrawPanelBackground(Rect panelRect) // 지도 패널 배경 사각형 표시
        {
            Color previousColor = GUI.color; // 색상 저장
            GUI.color = new Color(0f, 0f, 0f, 0.55f); // 반투명 검은 배경
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture); // 패널 배경 그리기
            GUI.color = previousColor; // 색상 복원
        }

        // 미니맵과 전체 지도가 공유하는 실제 지도 내용(바닥·벽·문·콘텐츠·플레이어)을 그린다.
        private void DrawMapPanel(Rect panelRect, float cellSize, float wallThick, float doorSize, float contentSize, float iconSize)
        {
            float center = panelRect.width / 2f; // 패널 중심 좌표 (플레이어 고정 위치)
            GridPosition playerPosition = movementController.PlayerState.CurrentGridPosition; // 현재 플레이어 그리드 좌표 조회
            RoomPassageController passageController = movementController.CurrentPassageController; // 현재 방 통로 컨트롤러 조회
            RoomView roomView = movementController.CurrentRoomView; // 현재 방 표시 진입점 조회

            GUI.BeginGroup(panelRect); // 패널 영역 밖으로 넘치는 그림 자동 클리핑

            if (passageController != null) // 통로 정보가 있어야 칸·벽·문을 그릴 수 있음
            {
                DrawFloorAndWalls(passageController, playerPosition, center, cellSize, wallThick, doorSize); // 이동 가능한 칸과 벽·문 표시
            }

            if (roomView != null) // 콘텐츠 자리 조회 가능 확인
            {
                DrawContentMarkers(roomView, playerPosition, center, cellSize, contentSize); // 상호작용 가능한 오브젝트 자리 표시
            }

            DrawPlayerIcon(center, iconSize); // 플레이어는 항상 패널 중심에 고정 표시

            GUI.EndGroup(); // 패널 영역 클리핑 종료
        }

        // 방 범위 안 모든 칸의 바닥과, 각 칸 사방의 벽/문 상태를 그린다.
        private void DrawFloorAndWalls(RoomPassageController passageController, GridPosition playerPosition, float center, float cellSize, float wallThick, float doorSize)
        {
            for (int x = minX; x <= maxX; x++) // 방 범위 X 반복
            {
                for (int z = minZ; z <= maxZ; z++) // 방 범위 Z 반복
                {
                    GridPosition cell = new GridPosition(x, z); // 현재 칸 좌표
                    Vector2 cellLocal = ToLocalPosition(cell, playerPosition, center, cellSize); // 플레이어 기준 로컬 좌표 계산

                    DrawFloorTile(cellLocal, cellSize); // 이동 가능한 칸 바닥 표시

                    DrawEdge(passageController, cell, cellLocal, CardinalDirection.North, cellSize, wallThick, doorSize); // 북쪽 벽/문 표시
                    DrawEdge(passageController, cell, cellLocal, CardinalDirection.East, cellSize, wallThick, doorSize); // 동쪽 벽/문 표시
                    DrawEdge(passageController, cell, cellLocal, CardinalDirection.South, cellSize, wallThick, doorSize); // 남쪽 벽/문 표시
                    DrawEdge(passageController, cell, cellLocal, CardinalDirection.West, cellSize, wallThick, doorSize); // 서쪽 벽/문 표시
                }
            }
        }

        private static void DrawFloorTile(Vector2 cellLocal, float cellSize) // 이동 가능한 칸 바닥 사각형 표시
        {
            float half = (cellSize - 2f) / 2f; // 칸 사이 여백을 위해 살짝 줄인 반폭
            Rect floorRect = new Rect(cellLocal.x - half, cellLocal.y - half, half * 2f, half * 2f); // 바닥 사각형 계산

            Color previousColor = GUI.color; // 색상 저장
            GUI.color = new Color(1f, 1f, 1f, 0.16f); // 옅은 흰색 바닥
            GUI.DrawTexture(floorRect, Texture2D.whiteTexture); // 바닥 표시
            GUI.color = previousColor; // 색상 복원
        }

        // 한 칸의 한쪽 방향 경계를 문/벽/열린 통로 중 무엇으로 그릴지 판단해서 표시한다.
        private void DrawEdge(RoomPassageController passageController, GridPosition cell, Vector2 cellLocal, CardinalDirection direction, float cellSize, float wallThick, float doorSize)
        {
            GridPosition delta = GridMovement.GetDirectionDelta(direction); // 방향 변화량 계산
            GridPosition neighbor = new GridPosition(cell.X + delta.X, cell.Z + delta.Z); // 인접 칸 좌표 계산
            bool neighborOutOfRoom = neighbor.X < minX || neighbor.X > maxX || neighbor.Z < minZ || neighbor.Z > maxZ; // 방 범위 밖 인접 칸 확인 (외벽)

            Vector2 edgeCenter = GetEdgeCenter(cellLocal, direction, cellSize); // 경계 중앙 로컬 좌표 계산
            bool isHorizontalEdge = direction == CardinalDirection.North || direction == CardinalDirection.South; // 가로 방향 경계 여부

            if (passageController.TryGetDoor(cell, direction, out GridPassage doorPassage)) // 이 경계가 문인지 확인
            {
                DrawDoorMarker(edgeCenter, doorPassage, doorSize); // 문 상태에 따른 색으로 표시
                return; // 문 표시 완료, 벽 표시 생략
            }

            if (neighborOutOfRoom || !passageController.CanPass(cell, direction)) // 방 바깥이거나 막힌 통로 확인
            {
                DrawWallSegment(edgeCenter, isHorizontalEdge, cellSize, wallThick); // 벽 선 표시
            }
        }

        private static Vector2 GetEdgeCenter(Vector2 cellLocal, CardinalDirection direction, float cellSize) // 칸 중심에서 방향별 경계 중앙 좌표 계산
        {
            float half = cellSize / 2f; // 칸 절반 크기

            switch (direction) // 방향 분기
            {
                case CardinalDirection.North: // 북쪽 (지도 위쪽)
                    return new Vector2(cellLocal.x, cellLocal.y - half); // 위쪽 경계 중앙 반환
                case CardinalDirection.South: // 남쪽 (지도 아래쪽)
                    return new Vector2(cellLocal.x, cellLocal.y + half); // 아래쪽 경계 중앙 반환
                case CardinalDirection.East: // 동쪽 (지도 오른쪽)
                    return new Vector2(cellLocal.x + half, cellLocal.y); // 오른쪽 경계 중앙 반환
                default: // 서쪽 (지도 왼쪽)
                    return new Vector2(cellLocal.x - half, cellLocal.y); // 왼쪽 경계 중앙 반환
            }
        }

        private static void DrawWallSegment(Vector2 edgeCenter, bool isHorizontalEdge, float cellSize, float wallThick) // 경계에 벽 선 표시
        {
            Rect wallRect = isHorizontalEdge
                ? new Rect(edgeCenter.x - (cellSize / 2f), edgeCenter.y - (wallThick / 2f), cellSize, wallThick) // 가로 벽 (북/남)
                : new Rect(edgeCenter.x - (wallThick / 2f), edgeCenter.y - (cellSize / 2f), wallThick, cellSize); // 세로 벽 (동/서)

            Color previousColor = GUI.color; // 색상 저장
            GUI.color = new Color(0.9f, 0.9f, 0.9f, 0.9f); // 밝은 회색 벽
            GUI.DrawTexture(wallRect, Texture2D.whiteTexture); // 벽 선 표시
            GUI.color = previousColor; // 색상 복원
        }

        private static void DrawDoorMarker(Vector2 edgeCenter, GridPassage doorPassage, float doorSize) // 경계에 문 상태 표시
        {
            Rect doorRect = new Rect(edgeCenter.x - (doorSize / 2f), edgeCenter.y - (doorSize / 2f), doorSize, doorSize); // 문 사각형 계산

            Color previousColor = GUI.color; // 색상 저장
            GUI.color = GetDoorColor(doorPassage); // 잠김/열림 상태에 따른 색 적용
            GUI.DrawTexture(doorRect, Texture2D.whiteTexture); // 문 표시
            GUI.color = previousColor; // 색상 복원
        }

        private static Color GetDoorColor(GridPassage doorPassage) // 문 상태별 표시 색 계산
        {
            if (doorPassage.IsLocked) // 잠긴 문 확인
            {
                return new Color(0.9f, 0.2f, 0.2f, 1f); // 빨강 (잠김)
            }

            return doorPassage.IsOpen ? new Color(0.3f, 0.85f, 0.3f, 1f) : new Color(0.9f, 0.85f, 0.3f, 1f); // 초록(열림) / 노랑(닫힘)
        }

        // 현재 방의 모든 콘텐츠 자리(계단·상자 등)를 종류별 색 점으로 표시한다.
        private void DrawContentMarkers(RoomView roomView, GridPosition playerPosition, float center, float cellSize, float contentSize)
        {
            foreach (RoomContentType contentType in System.Enum.GetValues(typeof(RoomContentType))) // 모든 콘텐츠 종류 반복
            {
                foreach (RoomContentMarker marker in roomView.GetMarkers(contentType)) // 해당 종류의 자리 전체 반복
                {
                    Vector2 markerLocal = ToLocalPosition(marker.GridPosition, playerPosition, center, cellSize); // 플레이어 기준 로컬 좌표 계산
                    Rect markerRect = new Rect(markerLocal.x - (contentSize / 2f), markerLocal.y - (contentSize / 2f), contentSize, contentSize); // 표시 사각형 계산

                    Color previousColor = GUI.color; // 색상 저장
                    GUI.color = GetContentColor(contentType); // 콘텐츠 종류별 색 적용
                    GUI.DrawTexture(markerRect, Texture2D.whiteTexture); // 콘텐츠 자리 표시
                    GUI.color = previousColor; // 색상 복원
                }
            }
        }

        private static Color GetContentColor(RoomContentType contentType) // 콘텐츠 종류별 표시 색 계산
        {
            switch (contentType) // 종류 분기
            {
                case RoomContentType.Stairs: // 계단
                    return new Color(1f, 0.55f, 0.15f, 1f); // 주황
                case RoomContentType.Chest: // 상자
                    return new Color(0.95f, 0.85f, 0.25f, 1f); // 금색
                case RoomContentType.NpcPoint: // NPC 지점
                    return new Color(0.3f, 0.85f, 1f, 1f); // 하늘색
                case RoomContentType.SecretWall: // 비밀 벽
                    return new Color(0.7f, 0.4f, 0.9f, 1f); // 보라
                default: // 환경 소품 등 나머지
                    return new Color(0.8f, 0.8f, 0.8f, 1f); // 회색
            }
        }

        // 플레이어를 기준으로 한 칸의 지도 로컬 좌표를 계산한다. 플레이어 자신은 항상 중심(center, center)이 된다.
        private static Vector2 ToLocalPosition(GridPosition cell, GridPosition playerPosition, float center, float cellSize)
        {
            float relativeX = cell.X - playerPosition.X; // 플레이어 기준 상대 X
            float relativeZ = cell.Z - playerPosition.Z; // 플레이어 기준 상대 Z
            return new Vector2(center + (relativeX * cellSize), center - (relativeZ * cellSize)); // 북쪽 +Z가 위로 가도록 반전한 로컬 좌표 반환
        }

        private void DrawPlayerIcon(float center, float iconSize) // 지도 중심에 고정된 플레이어 방향 아이콘 표시
        {
            Rect iconRect = new Rect(center - (iconSize / 2f), center - (iconSize / 2f), iconSize, iconSize); // 중심 고정 아이콘 사각형 계산
            float yaw = viewTransform != null ? viewTransform.eulerAngles.y : 0f; // 현재 수평 시선 각도 조회

            Matrix4x4 savedMatrix = GUI.matrix; // 회전 적용 전 GUI 행렬 저장
            GUIUtility.RotateAroundPivot(yaw, new Vector2(center, center)); // 바라보는 방향만큼 아이콘 회전 (기본 위쪽 = 북쪽)
            GUI.DrawTexture(iconRect, playerIconTexture); // 회전된 플레이어 아이콘 그리기
            GUI.matrix = savedMatrix; // GUI 행렬 원복 (이후 다른 GUI 요소에 영향 없도록)
        }

        // 위쪽을 가리키는 삼각형 모양 텍스처를 코드로 직접 생성한다. 별도 이미지 에셋이 필요 없다.
        private static Texture2D CreatePlayerIconTexture(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false); // 정사각형 텍스처 생성
            Color32 fillColor = new Color32(255, 255, 255, 255); // 흰색 아이콘 (칸/벽/콘텐츠 색과 구분)
            Color32 clearColor = new Color32(0, 0, 0, 0); // 투명 배경
            float center = size / 2f; // 가로 중심 좌표

            for (int y = 0; y < size; y++) // 세로 픽셀 반복
            {
                // Texture2D는 (0,0)이 좌하단 기준이라, y가 커질수록 화면에서는 위쪽에 그려진다.
                // 바라보는 방향(위쪽)에 뾰족한 꼭짓점이 오도록 y가 클수록 좁아지게 뒤집었다.
                float t = 1f - (y / (float)(size - 1)); // 꼭짓점(위쪽, t=0)에서 밑변(아래쪽, t=1)까지 진행률
                float halfWidth = t * center; // 현재 행의 삼각형 절반 폭

                for (int x = 0; x < size; x++) // 가로 픽셀 반복
                {
                    bool inside = Mathf.Abs(x - center) <= halfWidth; // 삼각형 내부 여부 판정
                    texture.SetPixel(x, y, inside ? fillColor : clearColor); // 내부는 채우고 바깥은 투명 처리
                }
            }

            texture.Apply(); // 픽셀 변경 사항 적용
            return texture; // 완성된 삼각형 텍스처 반환
        }
    }
}
