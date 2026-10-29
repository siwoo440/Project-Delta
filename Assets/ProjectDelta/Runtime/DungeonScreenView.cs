using System.Collections.Generic; // 최근 탐험 기록 보관
using ProjectDelta.Core; // 생성된 방 역할
using UnityEngine; // 화면 배치 도구
using UnityEngine.UI; // 방 이름 표시
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonScreenView : MonoBehaviour // 던전 안내 표시
    { // 클래스 시작
        private Text status; // 현재 방 이름 표시
        private Text playerHealth; // 현재 플레이어 체력 표시
        private Text playerMana; // 현재 플레이어 마나 표시
        private Text explorationLog; // 최근 탐험 기록 표시
        private readonly Queue<string> recentEvents = new Queue<string>(); // 최근 탐험 기록 목록
        private Text doorNotice; // 잠긴 문과 열쇠 안내
        private RoomTravelController travel; // 실제 방 이동과 역할 정보
        private void Start() // 기본 안내 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("DungeonCanvas"); // 던전 안내 화면 생성
            canvas.transform.SetParent(transform, false); // 씬 화면 아래 배치
            CreatePlayerPortrait(canvas.transform); // 오른쪽 하단 주인공 일러스트
            CreateBackground(canvas.transform, "DungeonPlayerBackground", new Vector2(-750f, -465f), new Vector2(300f, 85f)); // 하단 플레이어 상태 배경
            CreateBackground(canvas.transform, "DungeonLogBackground", new Vector2(750f, -40f), new Vector2(420f, 240f)); // 오른쪽 최근 기록 배경
            CreateBackground(canvas.transform, "DungeonHeaderBackground", new Vector2(0f, 485f), new Vector2(510f, 85f)); // 상단의 좁은 던전 안내 배경
            var mint = new Color(0.62f, 0.87f, 0.79f); // 안내 강조 색상
            UiFactory.CreateText(canvas.transform, "LogTitle", "최근 기록", 24, new Vector2(750f, 45f), new Vector2(380f, 45f), Color.white); // 탐험 기록 제목
            explorationLog = UiFactory.CreateText(canvas.transform, "ExplorationLog", "", 20, new Vector2(750f, -65f), new Vector2(380f, 150f), new Color(0.88f, 0.93f, 0.92f)); // 최근 탐험 기록 내용
            explorationLog.alignment = TextAnchor.UpperLeft; // 최근 기록의 왼쪽 정렬
            playerHealth = UiFactory.CreateText(canvas.transform, "PlayerHealthStatus", "체력 100 / 100", 25, new Vector2(-750f, -447f), new Vector2(280f, 38f), Color.white); // 하단 플레이어 체력 안내
            playerMana = UiFactory.CreateText(canvas.transform, "PlayerManaStatus", "마나 50 / 50", 23, new Vector2(-750f, -485f), new Vector2(280f, 38f), new Color(0.58f, 0.76f, 1f)); // 하단 플레이어 마나 안내
            UiFactory.CreateText(canvas.transform, "Header", "던전 탐험", 27, new Vector2(0f, 505f), new Vector2(510f, 36f), Color.white); // 상단 던전 제목
            status = UiFactory.CreateText(canvas.transform, "Status", "던전 준비 중", 20, new Vector2(0f, 470f), new Vector2(510f, 36f), mint); // 상단 현재 방 안내
            doorNotice = UiFactory.CreateText(canvas.transform, "DoorNotice", "", 23, new Vector2(0f, -340f), new Vector2(1200f, 60f), mint); // 문 상호작용 안내
            var roomObject = GameObject.Find("RoomTravel"); // 실제 방 이동 객체 조회
            travel = roomObject == null ? null : roomObject.GetComponent<RoomTravelController>(); // 방 이동 제어기 연결
            DungeonMiniMap.Create(canvas.transform, travel); // 발견한 방의 미니맵 생성
            if (AppRoot.Instance != null) // 공통 게임 객체 존재 검사
            { // 인벤토리 연결 시작
                AppRoot.Instance.Input.SaveRequested += SaveRun; // 키보드와 게임패드 저장 요청 연결
                AppRoot.Instance.InventoryClosed += ClearDoorNotice; // 인벤토리 복귀 시 안내 초기화
                AppRoot.Instance.ExplorationEvent += AppendLog; // 성공한 탐험 행동 기록 연결
                foreach (var entry in AppRoot.Instance.ExplorationHistory.Entries) // 현재 회차의 기존 기록 순회
                { // 기존 기록 복원 시작
                    AppendLog(entry); // 최근 기록 화면 복원
                } // 기존 기록 복원 끝
            } // 인벤토리 연결 끝
            if (travel != null && travel.IsReady) // 생성된 방 구조와 실제 상태 검사
            { // 방 정보 연결 시작
                travel.RoomChanged += UpdateRoom; // 성공한 방 이동 알림 연결
                travel.DoorNoticeChanged += UpdateDoorNotice; // 문 안내 변경 연결
                UpdateRoom(); // 시작 방의 이름 표시
            } // 방 정보 연결 끝
        } // 구성 끝
        private void SaveRun() // 실제 회차 저장과 안내 표시
        { // 저장 시작
            if (AppRoot.Instance == null) // 실행 객체 존재 검사
            { // 누락 조건 시작
                return; // 저장 요청 중단
            } // 누락 조건 끝
            var saved = AppRoot.Instance.TrySaveRun(out var error); // 현재 탐험 저장
            doorNotice.text = saved ? "회차를 저장했습니다" : error; // 저장 결과 안내
        } // 저장 끝
        private static void CreatePlayerPortrait(Transform parent) // 오른쪽 아래 주인공 일러스트 생성
        { // 생성 시작
            var texture = Resources.Load<Texture2D>("BattlePortraits/PlayerPrototype"); // 기존 주인공 이미지 조회
            if (texture == null) // 선택적 이미지 존재 검사
            { // 이미지 누락 시작
                return; // 탐험 화면을 그대로 유지
            } // 이미지 누락 끝
            var portrait = new GameObject("DungeonPlayerPortrait", typeof(RectTransform), typeof(RawImage)); // 주인공 이미지 객체
            portrait.transform.SetParent(parent, false); // 탐험 화면 아래 배치
            var rect = portrait.GetComponent<RectTransform>(); // 이미지 위치와 크기 조회
            rect.anchorMin = new Vector2(1f, 0f); // 오른쪽 아래 기준점
            rect.anchorMax = new Vector2(1f, 0f); // 오른쪽 아래 고정점
            rect.pivot = new Vector2(1f, 0f); // 이미지 오른쪽 아래 기준
            rect.anchoredPosition = new Vector2(-30f, 25f); // 화면 모서리 여백
            rect.sizeDelta = new Vector2(230f * texture.width / texture.height, 230f); // 원본 비율의 이미지 크기
            var image = portrait.GetComponent<RawImage>(); // 실제 이미지 표시 조회
            image.texture = texture; // 주인공 이미지 연결
            image.raycastTarget = false; // 게임 입력 가림 방지
        } // 생성 끝
        private void Update() // 탐험 중 실제 체력 표시 갱신
        { // 갱신 시작
            if (playerHealth == null || AppRoot.Instance == null) // 체력 안내와 게임 상태 검사
            { // 누락 조건 시작
                return; // 표시할 체력이 없는 경우 유지
            } // 누락 조건 끝
            var health = AppRoot.Instance.PlayerHealth; // 현재 플레이어 체력
            playerHealth.text = "체력 " + health + " / 100" + (health <= 25 ? " ! 위험" : string.Empty); // 현재 체력과 낮은 체력 경고
            playerHealth.color = health <= 25 ? new Color(1f, 0.48f, 0.42f) : Color.white; // 낮은 체력의 경고 색상
            playerMana.text = "마나 " + AppRoot.Instance.PlayerMana + " / 50"; // 실제 회차 마나 갱신
        } // 갱신 끝
        private static void CreateBackground(Transform parent, string name, Vector2 position, Vector2 size) // 안내 문구용 입력 통과 배경
        { // 생성 시작
            var background = new GameObject(name, typeof(RectTransform), typeof(Image)); // 화면 배경 객체
            background.transform.SetParent(parent, false); // 던전 화면 아래 배치
            var rect = background.GetComponent<RectTransform>(); // 배경 위치와 크기
            rect.anchoredPosition = position; // 안내 문구 뒤의 위치
            rect.sizeDelta = size; // 안내 문구를 감싸는 크기
            var image = background.GetComponent<Image>(); // 배경 이미지 구성
            image.color = new Color(0.025f, 0.045f, 0.055f, 0.86f); // 던전 바닥과 구분되는 어두운 색
            image.raycastTarget = false; // 마우스와 게임 입력 통과
        } // 생성 끝
        private void OnDestroy() // 화면 이벤트 연결 정리
        { // 정리 시작
            if (AppRoot.Instance != null) // 공통 게임 객체 존재 검사
            { // 인벤토리 연결 해제 시작
                AppRoot.Instance.InventoryClosed -= ClearDoorNotice; // 인벤토리 복귀 연결 해제
                AppRoot.Instance.Input.SaveRequested -= SaveRun; // 저장 요청 연결 해제
                AppRoot.Instance.ExplorationEvent -= AppendLog; // 탐험 행동 기록 연결 해제
            } // 인벤토리 연결 해제 끝
            if (travel != null) // 방 이동 객체 존재 검사
            { // 연결 해제 시작
                travel.RoomChanged -= UpdateRoom; // 방 이름 갱신 연결 해제
                travel.DoorNoticeChanged -= UpdateDoorNotice; // 문 안내 연결 해제
            } // 연결 해제 끝
        } // 정리 끝
        private void ClearDoorNotice() // 인벤토리 복귀 시 이전 문 안내 제거
        { // 제거 시작
            doorNotice.text = string.Empty; // 오래된 열쇠 수량 안내 제거
        } // 제거 끝
        private void UpdateDoorNotice(string message) // 문 상호작용 안내 갱신
        { // 갱신 시작
            doorNotice.text = message; // 실제 안내 문구 표시
            AppRoot.Instance.RecordExplorationEvent(message); // 실제 문 상호작용 보관
        } // 갱신 끝
        private void AppendLog(string message) // 최근 탐험 기록 추가
        { // 기록 시작
            if (explorationLog == null || string.IsNullOrWhiteSpace(message)) // 화면과 실제 기록 검사
            { // 빈 기록 조건 시작
                return; // 빈 안내의 기록 제외
            } // 빈 기록 조건 끝
            recentEvents.Enqueue(message); // 실제 탐험 사건 보관
            while (recentEvents.Count > 4) // 최근 기록 수 제한 검사
            { // 오래된 기록 정리 시작
                recentEvents.Dequeue(); // 가장 오래된 기록 제거
            } // 오래된 기록 정리 끝
            explorationLog.text = string.Join("\n", recentEvents.ToArray()); // 최근 기록을 순서대로 표시
        } // 기록 끝
        private void UpdateRoom() // 현재 방의 역할 표시
        { // 갱신 시작
            var room = travel.Layout.GetRoom(travel.RoomX, travel.RoomY); // 실제 현재 방 정보 조회
            if (room == null) // 방 정보 존재 검사
            { // 누락 조건 시작
                return; // 잘못된 방 이름 표시 방지
            } // 누락 조건 끝
            switch (room.Kind) // 현재 방의 역할 선택
            { // 역할 선택 시작
                case RoomKind.Start: // 시작 방
                    status.text = "현재 위치: 시작 방"; // 시작 방 이름 표시
                    break; // 역할 선택 종료
                case RoomKind.Stairs: // 계단 방
                    status.text = travel.Floor >= 2 ? "현재 위치: 계단 방 · 이후 층 준비 중" : "현재 위치: 계단 방"; // 계단 방과 추가 층 안내
                    break; // 역할 선택 종료
                case RoomKind.Elite: // 상위 개체 방
                    status.text = "현재 위치: 상위 개체 방"; // 상위 개체 방 이름 표시
                    break; // 역할 선택 종료
                default: // 일반 방
                    status.text = "현재 위치: 일반 방"; // 일반 방 이름 표시
                    break; // 역할 선택 종료
            } // 역할 선택 끝
            status.text = travel.Floor + "층 · " + status.text; // 현재 층과 방 함께 표시
            AppRoot.Instance.RecordExplorationEvent(status.text.Replace("현재 위치: ", "") + " 진입"); // 실제 도착 방의 회차 기록
        } // 갱신 끝
    } // 클래스 끝
} // 이름 공간 끝
