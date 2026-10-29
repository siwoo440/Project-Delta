using ProjectDelta.Core; // 방 구조와 발견 상태
using UnityEngine; // 화면 객체와 위치
using UnityEngine.InputSystem; // 미니맵 클릭 입력
using UnityEngine.UI; // 미니맵 사각형 표시
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonMiniMap : MonoBehaviour // 방문 정보의 화면 표시
    { // 클래스 시작
        private RoomTravelController travel; // 실제 방 이동과 발견 기록
        private Transform content; // 방 칸과 연결선 표시 영역
        private Text directionLabel; // 현재 위치의 방향 표시
        private float spacing; // 공개된 방에 맞춘 표시 간격
        private Vector2 mapCenter; // 공개된 방의 좌표 중심
        private InputAction click; // 미니맵 클릭 입력
        private RectTransform clickRect; // 클릭 가능한 지도 영역
        private int createdFrame; // 생성 직후 클릭 차단
        public static void Create(Transform parent, RoomTravelController controller) // 화면에 미니맵 생성
        { // 생성 시작
            if (controller == null || !controller.IsReady) // 지도 데이터와 실제 방 상태 준비 검사
            { // 데이터 누락 조건 시작
                return; // 잘못된 미니맵 생성 방지
            } // 데이터 누락 조건 끝
            var mapObject = new GameObject("DungeonMiniMap", typeof(RectTransform), typeof(Image), typeof(DungeonMiniMap)); // 지도 배경 객체 생성
            mapObject.transform.SetParent(parent, false); // 던전 화면 아래 배치
            var rect = mapObject.GetComponent<RectTransform>(); // 지도 표시 위치 조회
            rect.anchorMin = new Vector2(1f, 1f); // 화면 오른쪽 위 기준
            rect.anchorMax = new Vector2(1f, 1f); // 화면 오른쪽 위 고정
            rect.pivot = new Vector2(1f, 1f); // 지도 오른쪽 위 기준점
            rect.anchoredPosition = new Vector2(-35f, -140f); // 화면 모서리 여백
            rect.sizeDelta = new Vector2(220f, 180f); // 지도 표시 크기
            var background = mapObject.GetComponent<Image>(); // 지도 배경 조회
            background.color = new Color(0.025f, 0.055f, 0.07f, 0.9f); // 어두운 지도 배경
            background.raycastTarget = false; // 게임 입력 가림 방지
            var title = UiFactory.CreateText(mapObject.transform, "Label", "지도", 20, new Vector2(0f, 66f), new Vector2(120f, 30f), Color.white); // 지도 제목 표시
            title.raycastTarget = false; // 제목 입력 가림 방지
            var layer = new GameObject("MapContent", typeof(RectTransform)); // 방과 연결선 표시 영역
            layer.transform.SetParent(mapObject.transform, false); // 지도 배경 아래 배치
            var layerRect = layer.GetComponent<RectTransform>(); // 표시 영역 조회
            layerRect.anchorMin = Vector2.zero; // 배경 전체 기준 시작
            layerRect.anchorMax = Vector2.one; // 배경 전체 기준 끝
            layerRect.offsetMin = Vector2.zero; // 왼쪽 아래 여백 없음
            layerRect.offsetMax = Vector2.zero; // 오른쪽 위 여백 없음
            var view = mapObject.GetComponent<DungeonMiniMap>(); // 새 지도 표시기 조회
            view.Initialize(controller, layer.transform); // 데이터 연결과 초기 표시
            view.clickRect = rect; // 실제 미니맵 클릭 영역
            view.createdFrame = Time.frameCount; // 생성 프레임 보관
            view.click = new InputAction("MiniMapClick", InputActionType.Button, "<Mouse>/leftButton"); // 미니맵 클릭 감지
            view.click.performed += view.OnClick; // 클릭 동작 연결
            view.click.Enable(); // 미니맵 클릭 활성화
        } // 생성 끝
        private void Initialize(RoomTravelController controller, Transform layer) // 방 데이터와 이벤트 연결
        { // 연결 시작
            travel = controller; // 실제 방 이동 연결
            content = layer; // 지도 표시 영역 연결
            travel.RoomChanged += Rebuild; // 성공한 방 이동의 지도 갱신 연결
            travel.DiscoveryChanged += Rebuild; // 비밀 통로 발견의 지도 갱신 연결
            Rebuild(); // 시작 방의 지도 표시
        } // 연결 끝
        private void OnDestroy() // 지도 이벤트 연결 해제
        { // 정리 시작
            if (click != null) // 클릭 입력 존재 검사
            { // 입력 정리 시작
                click.performed -= OnClick; // 클릭 동작 연결 해제
                click.Dispose(); // 클릭 입력 정리
            } // 입력 정리 끝
            if (travel != null) // 방 이동 객체 존재 검사
            { // 연결 해제 시작
                travel.RoomChanged -= Rebuild; // 지도 갱신 구독 해제
                travel.DiscoveryChanged -= Rebuild; // 비밀 발견 구독 해제
            } // 연결 해제 끝
        } // 정리 끝
        private void OnClick(InputAction.CallbackContext context) // 실제 미니맵 클릭 검사
        { // 클릭 검사 시작
            var mouse = context.control.device as Mouse; // 클릭한 마우스 조회
            if (Time.frameCount <= createdFrame || mouse == null || AppRoot.Instance == null || !AppRoot.Instance.Input.IsExplorationEnabled) // 생성 직후와 탐험 상태 검사
            { // 클릭 차단 시작
                return; // 잘못된 지도 열기 차단
            } // 클릭 차단 끝
            if (RectTransformUtility.RectangleContainsScreenPoint(clickRect, mouse.position.ReadValue(), null)) // 미니맵 영역 검사
            { // 지도 열기 시작
                AppRoot.Instance.OpenFullMap(); // 실제 전체 지도 열기
            } // 지도 열기 끝
        } // 클릭 검사 끝
        private void Update() // 플레이어 방향의 지도 표시 갱신
        { // 갱신 시작
            if (travel == null || directionLabel == null) // 지도와 방향 표시 검사
            { // 누락 조건 시작
                return; // 갱신할 방향 표시 없음
            } // 누락 조건 끝
            switch (travel.Facing) // 마지막 이동 방향 검사
            { // 방향 선택 시작
                case MoveDirection.Up: // 위쪽 방향
                    directionLabel.text = "▲"; // 위쪽 방향 표시
                    break; // 선택 종료
                case MoveDirection.Down: // 아래쪽 방향
                    directionLabel.text = "▼"; // 아래쪽 방향 표시
                    break; // 선택 종료
                case MoveDirection.Left: // 왼쪽 방향
                    directionLabel.text = "◀"; // 왼쪽 방향 표시
                    break; // 선택 종료
                case MoveDirection.Right: // 오른쪽 방향
                    directionLabel.text = "▶"; // 오른쪽 방향 표시
                    break; // 선택 종료
            } // 방향 선택 끝
        } // 갱신 끝
        private void Rebuild() // 공개된 방과 연결선만 표시
        { // 갱신 시작
            directionLabel = null; // 기존 방향 표시 연결 제거
            foreach (Transform child in content) // 기존 지도 표시 순회
            { // 기존 표시 정리 시작
                child.gameObject.SetActive(false); // 즉시 이전 표시 가림
                Destroy(child.gameObject); // 이전 표시 객체 제거
            } // 기존 표시 정리 끝
            mapCenter = new Vector2(travel.RoomX, travel.RoomY); // 현재 방을 미니맵 중앙에 배치
            spacing = 22.4f; // 다섯 칸이 화면 안에 들어가는 간격
            foreach (var door in travel.Layout.Doors) // 실제 연결 정보 순회
            { // 연결 순회 시작
                if (door.RoomX > door.TargetRoomX || (door.RoomX == door.TargetRoomX && door.RoomY > door.TargetRoomY)) // 양방향 연결의 중복 검사
                { // 중복 조건 시작
                    continue; // 반대 방향 연결선 생략
                } // 중복 조건 끝
                if (!MiniMapWindow.Contains(door.RoomX, door.RoomY, travel.RoomX, travel.RoomY) || !MiniMapWindow.Contains(door.TargetRoomX, door.TargetRoomY, travel.RoomX, travel.RoomY)) // 연결 양끝의 표시 범위 검사
                { // 범위 밖 조건 시작
                    continue; // 표시 범위 밖 연결 제외
                } // 범위 밖 조건 끝
                if (!travel.Discovery.IsConnectionVisible(door.RoomX, door.RoomY, door.TargetRoomX, door.TargetRoomY)) // 공개 가능한 연결 검사
                { // 비공개 조건 시작
                    continue; // 미발견과 비밀 연결 표시 제외
                } // 비공개 조건 끝
                var from = Position(door.RoomX, door.RoomY); // 출발 방의 지도 위치
                var to = Position(door.TargetRoomX, door.TargetRoomY); // 도착 방의 지도 위치
                var horizontal = door.RoomX != door.TargetRoomX; // 가로 연결 여부
                var thickness = Mathf.Max(2f, spacing * 0.1f); // 지도 크기에 맞춘 연결선 두께
                CreateImage("Link_" + door.RoomX + "_" + door.RoomY + "_" + door.TargetRoomX + "_" + door.TargetRoomY, (from + to) * 0.5f, horizontal ? new Vector2(spacing, thickness) : new Vector2(thickness, spacing), new Color(0.45f, 0.65f, 0.62f)); // 확인된 연결선 생성
            } // 연결 순회 끝
            foreach (var room in travel.Layout.Rooms) // 실제 방 정의 순회
            { // 방 순회 시작
                if (!MiniMapWindow.Contains(room.X, room.Y, travel.RoomX, travel.RoomY)) // 현재 방 주변 다섯 칸 범위 검사
                { // 범위 밖 조건 시작
                    continue; // 화면 범위 밖의 방 표시 제외
                } // 범위 밖 조건 끝
                var visibility = travel.Discovery.GetVisibility(room.X, room.Y); // 플레이어가 아는 방 상태
                if (visibility == RoomVisibility.Hidden) // 미발견 또는 비밀 방 검사
                { // 숨김 조건 시작
                    continue; // 비공개 방의 화면 객체 생성 방지
                } // 숨김 조건 끝
                var current = room.X == travel.RoomX && room.Y == travel.RoomY; // 현재 방 좌표 검사
                var color = current ? new Color(0.35f, 0.9f, 0.72f) : visibility == RoomVisibility.Visited ? new Color(0.69f, 0.77f, 0.76f) : new Color(0.10f, 0.16f, 0.18f); // 현재와 방문과 미방문 색상
                var roomSize = spacing * 0.7f; // 표시 간격에 맞춘 방 칸 크기
                var image = CreateImage("Room_" + room.X + "_" + room.Y, Position(room.X, room.Y), new Vector2(roomSize, roomSize), color); // 공개된 방 칸 생성
                var outline = image.gameObject.AddComponent<Outline>(); // 방 테두리 생성
                outline.effectColor = visibility == RoomVisibility.Connected ? new Color(0.38f, 0.48f, 0.49f) : new Color(0.85f, 0.94f, 0.91f); // 발견 상태별 테두리 색상
                var border = Mathf.Max(1f, spacing / 24f); // 지도 크기에 맞춘 테두리
                outline.effectDistance = new Vector2(border, -border); // 테두리 두께
                if (visibility == RoomVisibility.Visited && room.Kind == RoomKind.Stairs) // 실제 방문한 계단 방 검사
                { // 계단 표시 시작
                    UiFactory.CreateText(image.transform, "StairIndicator", "▲", Mathf.Max(8, Mathf.RoundToInt(spacing * 0.42f)), new Vector2(roomSize * 0.32f, roomSize * 0.32f), new Vector2(roomSize * 0.65f, roomSize * 0.65f), new Color(0.25f, 0.7f, 1f)); // 방문한 계단의 파란 표시
                } // 계단 표시 끝
                if (visibility == RoomVisibility.Visited && room.Kind == RoomKind.Boss) // 실제 방문한 최종 방 검사
                { // 최종 방 표시 시작
                    UiFactory.CreateText(image.transform, "BossIndicator", "◆", Mathf.Max(8, Mathf.RoundToInt(spacing * 0.42f)), new Vector2(roomSize * 0.32f, roomSize * 0.32f), new Vector2(roomSize * 0.65f, roomSize * 0.65f), new Color(1f, 0.35f, 0.35f)); // 방문한 마왕 방의 붉은 표시
                } // 최종 방 표시 끝
                if (current) // 현재 플레이어 방 검사
                { // 현재 방 조건 시작
                    directionLabel = UiFactory.CreateText(image.transform, "CurrentDirection", "▼", Mathf.Max(8, Mathf.RoundToInt(spacing * 0.46f)), Vector2.zero, new Vector2(roomSize, roomSize), new Color(0.03f, 0.15f, 0.13f)); // 현재 방의 이동 방향 표시
                } // 현재 방 조건 끝
            } // 방 순회 끝
        } // 갱신 끝
        private Image CreateImage(string name, Vector2 position, Vector2 size, Color color) // 지도 요소 생성
        { // 생성 시작
            var item = new GameObject(name, typeof(RectTransform), typeof(Image)); // 방 또는 연결선 객체
            item.transform.SetParent(content, false); // 표시 영역에 배치
            var rect = item.GetComponent<RectTransform>(); // 요소 위치 조회
            rect.anchoredPosition = position; // 지도 내 위치 설정
            rect.sizeDelta = size; // 요소 크기 설정
            var image = item.GetComponent<Image>(); // 요소 이미지 조회
            image.color = color; // 요소 색상 설정
            image.raycastTarget = false; // 게임 입력 가림 방지
            return image; // 구성된 이미지 전달
        } // 생성 끝
        private Vector2 Position(int x, int y) // 공개된 방 좌표를 지도 위치로 변환
        { // 변환 시작
            return new Vector2((x - mapCenter.x) * spacing, (y - mapCenter.y) * spacing - 15f); // 공개된 범위 기준 방 표시 좌표
        } // 변환 끝
    } // 클래스 끝
} // 이름 공간 끝
