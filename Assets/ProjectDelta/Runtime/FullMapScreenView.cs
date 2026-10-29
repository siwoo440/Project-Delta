using ProjectDelta.Core; // 방 역할과 발견 상태
using UnityEngine; // 지도 화면 객체
using UnityEngine.UI; // 지도 방과 연결 표시
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class FullMapScreenView : MonoBehaviour // 발견한 방 전체 지도
    { // 클래스 시작
        private RectTransform mapContent; // 확대와 이동 대상
        private Vector2 currentPosition; // 현재 방의 지도 좌표
        private GameInputRouter input; // 지도 전용 입력 연결
        public float MapZoom => mapContent == null ? 1f : mapContent.localScale.x; // 현재 지도 배율
        public Vector2 MapOffset => mapContent == null ? Vector2.zero : mapContent.anchoredPosition; // 현재 지도 이동량
        public Vector2 CurrentRoomScreenOffset => MapOffset + currentPosition * MapZoom; // 현재 방의 지도 중심 거리
        public static FullMapScreenView Create(RoomTravelController travel) // 현재 층의 전체 지도 생성
        { // 생성 시작
            var screen = new GameObject("FullMapScreen", typeof(FullMapScreenView)); // 지도 화면 객체
            screen.transform.SetParent(travel.transform, false); // 던전 씬 수명에 연결
            try // 실제 지도 화면 구성
            { // 구성 시작
                var canvas = UiFactory.CreateCanvas("FullMapCanvas"); // 전체 지도 전용 화면
                canvas.transform.SetParent(screen.transform, false); // 화면 상위 객체 연결
                canvas.sortingOrder = 200; // 기존 던전 안내보다 앞에 표시
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 지도 배경 객체
                background.transform.SetParent(canvas.transform, false); // 전체 화면 아래 배치
                var rect = background.GetComponent<RectTransform>(); // 배경 위치와 크기
                rect.anchorMin = Vector2.zero; // 전체 화면 왼쪽 아래
                rect.anchorMax = Vector2.one; // 전체 화면 오른쪽 위
                rect.offsetMin = Vector2.zero; // 왼쪽 아래 여백 제거
                rect.offsetMax = Vector2.zero; // 오른쪽 위 여백 제거
                var backImage = background.GetComponent<Image>(); // 배경 색상 구성
                backImage.color = new Color(0.025f, 0.045f, 0.06f, 0.97f); // 어두운 지도 바탕
                backImage.raycastTarget = false; // 다른 입력 방해 방지
                UiFactory.CreateText(canvas.transform, "Title", "발견한 전체 지도", 42, new Vector2(0f, 450f), new Vector2(1000f, 80f), Color.white); // 지도 제목
                UiFactory.CreateText(canvas.transform, "CloseHint", "방향키 / 패드 방향: 이동  ·  + / - / 어깨 버튼: 확대·축소  ·  Home / 패드 X: 현재 위치  ·  M / Esc: 닫기", 21, new Vector2(0f, -465f), new Vector2(1800f, 60f), Color.white); // 지도 조작 안내
                MapPointerButton.Create(canvas.transform, "MapCloseButton", "지도 닫기", new Vector2(750f, 400f), AppRoot.Instance.CloseFullMap); // 마우스 지도 닫기 버튼
                var viewport = new GameObject("MapViewport", typeof(RectTransform), typeof(RectMask2D)); // 지도 영역의 화면 잘림
                viewport.transform.SetParent(canvas.transform, false); // 지도 화면 아래 배치
                var viewportRect = viewport.GetComponent<RectTransform>(); // 지도 표시 영역
                viewportRect.sizeDelta = new Vector2(1400f, 680f); // 지도와 조작 버튼의 분리
                var content = new GameObject("MapContent", typeof(RectTransform)); // 이동과 확대 대상
                content.transform.SetParent(viewport.transform, false); // 잘림 영역 아래 배치
                var view = screen.GetComponent<FullMapScreenView>(); // 지도 화면 제어기
                view.mapContent = content.GetComponent<RectTransform>(); // 실제 지도 이동 대상 연결
                view.mapContent.sizeDelta = viewportRect.sizeDelta; // 지도 중심 기준 크기
                view.currentPosition = DrawMap(content.transform, travel); // 공개된 방과 현재 방 좌표
                view.input = AppRoot.Instance.Input; // 실제 지도 입력 조회
                view.input.FullMapPanRequested += view.Pan; // 지도 이동 입력 연결
                view.input.FullMapZoomRequested += view.Zoom; // 지도 확대 입력 연결
                view.input.FullMapCenterRequested += view.Center; // 현재 방 복귀 입력 연결
                MapPointerButton.Create(canvas.transform, "MapPanLeft", "←", new Vector2(-720f, -390f), () => view.Pan(Vector2Int.left)); // 마우스 왼쪽 이동
                MapPointerButton.Create(canvas.transform, "MapPanUp", "↑", new Vector2(-480f, -390f), () => view.Pan(Vector2Int.up)); // 마우스 위쪽 이동
                MapPointerButton.Create(canvas.transform, "MapPanDown", "↓", new Vector2(-240f, -390f), () => view.Pan(Vector2Int.down)); // 마우스 아래쪽 이동
                MapPointerButton.Create(canvas.transform, "MapPanRight", "→", new Vector2(0f, -390f), () => view.Pan(Vector2Int.right)); // 마우스 오른쪽 이동
                MapPointerButton.Create(canvas.transform, "MapZoomOut", "축소 −", new Vector2(240f, -390f), () => view.Zoom(-1)); // 마우스 축소
                MapPointerButton.Create(canvas.transform, "MapZoomIn", "확대 +", new Vector2(480f, -390f), () => view.Zoom(1)); // 마우스 확대
                MapPointerButton.Create(canvas.transform, "MapCenter", "현재 위치", new Vector2(720f, -390f), view.Center); // 마우스 현재 위치 복귀
                return view; // 완성한 지도 화면 전달
            } // 구성 끝
            catch // 화면 구성 실패 검사
            { // 실패 정리 시작
                Destroy(screen); // 불완전한 지도 객체 제거
                throw; // 원래 실패 전달
            } // 실패 정리 끝
        } // 생성 끝
        public void Pan(Vector2Int direction) // 지정 방향으로 지도 이동
        { // 이동 시작
            if (mapContent == null) // 지도 연결 검사
            { // 누락 조건 시작
                return; // 잘못된 이동 차단
            } // 누락 조건 끝
            var next = mapContent.anchoredPosition + (Vector2)direction * 80f; // 한 번의 화면 이동량
            mapContent.anchoredPosition = new Vector2(Mathf.Clamp(next.x, -1200f, 1200f), Mathf.Clamp(next.y, -900f, 900f)); // 화면 이동 범위 제한
        } // 이동 끝
        public void Zoom(int direction) // 지도 배율 변경
        { // 확대 시작
            if (mapContent == null || direction == 0) // 지도와 요청 검사
            { // 잘못된 요청 시작
                return; // 배율 변경 차단
            } // 잘못된 요청 끝
            var zoom = Mathf.Clamp(MapZoom + Mathf.Sign(direction) * 0.25f, 0.5f, 2f); // 허용 범위의 배율
            mapContent.localScale = new Vector3(zoom, zoom, 1f); // 지도 요소 확대 적용
        } // 확대 끝
        public void Center() // 현재 방을 표시 영역 중심으로 이동
        { // 복귀 시작
            if (mapContent != null) // 지도 연결 검사
            { // 연결 조건 시작
                mapContent.anchoredPosition = -currentPosition * MapZoom; // 현재 방의 화면 중심 배치
            } // 연결 조건 끝
        } // 복귀 끝
        private void OnDestroy() // 지도 입력 연결 정리
        { // 정리 시작
            if (input != null) // 입력 연결 검사
            { // 연결 조건 시작
                input.FullMapPanRequested -= Pan; // 지도 이동 입력 해제
                input.FullMapZoomRequested -= Zoom; // 지도 배율 입력 해제
                input.FullMapCenterRequested -= Center; // 중심 복귀 입력 해제
            } // 연결 조건 끝
        } // 정리 끝
        private static Vector2 DrawMap(Transform parent, RoomTravelController travel) // 발견한 방과 연결 그리기
        { // 그리기 시작
            var minX = int.MaxValue; // 공개된 방의 최소 가로 좌표
            var maxX = int.MinValue; // 공개된 방의 최대 가로 좌표
            var minY = int.MaxValue; // 공개된 방의 최소 세로 좌표
            var maxY = int.MinValue; // 공개된 방의 최대 세로 좌표
            foreach (var room in travel.Layout.Rooms) // 현재 층 방 순회
            { // 범위 순회 시작
                if (travel.Discovery.GetVisibility(room.X, room.Y) == RoomVisibility.Hidden) // 비공개 방 검사
                { // 숨김 조건 시작
                    continue; // 미발견과 비밀 방 제외
                } // 숨김 조건 끝
                minX = Mathf.Min(minX, room.X); // 공개된 최소 가로 좌표
                maxX = Mathf.Max(maxX, room.X); // 공개된 최대 가로 좌표
                minY = Mathf.Min(minY, room.Y); // 공개된 최소 세로 좌표
                maxY = Mathf.Max(maxY, room.Y); // 공개된 최대 세로 좌표
            } // 범위 순회 끝
            if (minX == int.MaxValue) // 공개된 방 존재 검사
            { // 빈 지도 조건 시작
                return Vector2.zero; // 비어 있는 지도 표시
            } // 빈 지도 조건 끝
            var center = new Vector2(((float)minX + maxX) * 0.5f, ((float)minY + maxY) * 0.5f); // 공개된 방의 지도 중심
            var spacing = Mathf.Min(110f, 1200f / ((float)maxX - minX + 1f), 700f / ((float)maxY - minY + 1f)); // 공개된 방에 맞춘 간격
            var currentPosition = Position(travel.RoomX, travel.RoomY, center, spacing); // 현재 방의 지도 좌표
            foreach (var door in travel.Layout.Doors) // 실제 양방향 연결 순회
            { // 연결 순회 시작
                if (door.RoomX > door.TargetRoomX || (door.RoomX == door.TargetRoomX && door.RoomY > door.TargetRoomY)) // 중복 연결 검사
                { // 중복 조건 시작
                    continue; // 반대 방향의 중복 표시 제외
                } // 중복 조건 끝
                if (!travel.Discovery.IsConnectionVisible(door.RoomX, door.RoomY, door.TargetRoomX, door.TargetRoomY)) // 공개된 연결 검사
                { // 숨김 조건 시작
                    continue; // 비밀과 미발견 연결 제외
                } // 숨김 조건 끝
                var from = Position(door.RoomX, door.RoomY, center, spacing); // 출발 방 위치
                var to = Position(door.TargetRoomX, door.TargetRoomY, center, spacing); // 도착 방 위치
                var horizontal = door.RoomX != door.TargetRoomX; // 연결 방향 검사
                CreateImage(parent, "Link_" + door.RoomX + "_" + door.RoomY + "_" + door.TargetRoomX + "_" + door.TargetRoomY, (from + to) * 0.5f, horizontal ? new Vector2(spacing, 5f) : new Vector2(5f, spacing), new Color(0.45f, 0.65f, 0.62f)); // 공개된 연결선
            } // 연결 순회 끝
            foreach (var room in travel.Layout.Rooms) // 현재 층의 방 순회
            { // 방 순회 시작
                var visibility = travel.Discovery.GetVisibility(room.X, room.Y); // 실제 방 공개 상태
                if (visibility == RoomVisibility.Hidden) // 숨겨진 방 검사
                { // 숨김 조건 시작
                    continue; // 미발견과 비밀 방 제외
                } // 숨김 조건 끝
                var current = room.X == travel.RoomX && room.Y == travel.RoomY; // 현재 방 검사
                var color = current ? new Color(0.35f, 0.9f, 0.72f) : visibility == RoomVisibility.Visited ? new Color(0.69f, 0.77f, 0.76f) : new Color(0.10f, 0.16f, 0.18f); // 방문 상태별 색상
                var size = spacing * 0.7f; // 방 칸 크기
                var image = CreateImage(parent, "Room_" + room.X + "_" + room.Y, Position(room.X, room.Y, center, spacing), new Vector2(size, size), color); // 공개된 방 칸
                var outline = image.gameObject.AddComponent<Outline>(); // 방 구분 테두리
                outline.effectColor = new Color(0.65f, 0.8f, 0.77f); // 공개된 방 테두리 색상
                outline.effectDistance = new Vector2(2f, -2f); // 테두리 크기
                if (visibility == RoomVisibility.Visited && room.Kind == RoomKind.Stairs) // 방문한 계단 방 검사
                { // 계단 표시 시작
                    UiFactory.CreateText(image.transform, "StairIndicator", "▲", Mathf.Max(12, Mathf.RoundToInt(spacing * 0.3f)), new Vector2(size * 0.3f, size * 0.3f), new Vector2(size * 0.5f, size * 0.5f), new Color(0.2f, 0.6f, 1f)); // 방문한 계단의 파란 표시
                } // 계단 표시 끝
                if (current) // 현재 플레이어 방 검사
                { // 현재 방 표시 시작
                    UiFactory.CreateText(image.transform, "CurrentLocation", "현재", Mathf.Max(12, Mathf.RoundToInt(spacing * 0.2f)), Vector2.zero, new Vector2(size, size), new Color(0.03f, 0.15f, 0.13f)); // 현재 방 안내
                } // 현재 방 표시 끝
            } // 방 순회 끝
            return currentPosition; // 현재 방의 지도 좌표 전달
        } // 그리기 끝
        private static Vector2 Position(int x, int y, Vector2 center, float spacing) // 방 좌표의 지도 화면 위치
        { // 변환 시작
            return new Vector2((x - center.x) * spacing, (y - center.y) * spacing); // 공개된 범위 기준 위치
        } // 변환 끝
        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size, Color color) // 지도 선과 방 생성
        { // 생성 시작
            var item = new GameObject(name, typeof(RectTransform), typeof(Image)); // 지도 표시 객체
            item.transform.SetParent(parent, false); // 지도 화면에 배치
            var rect = item.GetComponent<RectTransform>(); // 요소 위치와 크기
            rect.anchoredPosition = position; // 지도 안의 위치
            rect.sizeDelta = size; // 방 또는 연결 크기
            var image = item.GetComponent<Image>(); // 지도 이미지 구성
            image.color = color; // 발견 상태 색상
            image.raycastTarget = false; // 불필요한 입력 차단 방지
            return image; // 완성한 지도 요소 전달
        } // 생성 끝
    } // 클래스 끝
} // 이름 공간 끝
