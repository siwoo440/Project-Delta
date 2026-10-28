using ProjectDelta.Core; // 생성된 방 역할
using UnityEngine; // 화면 배치 도구
using UnityEngine.UI; // 방 이름 표시
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class DungeonScreenView : MonoBehaviour // 던전 안내 표시
    { // 클래스 시작
        private Text status; // 현재 방 이름 표시
        private RoomTravelController travel; // 실제 방 이동과 역할 정보
        private void Start() // 기본 안내 화면 구성
        { // 구성 시작
            var canvas = UiFactory.CreateCanvas("DungeonCanvas"); // 던전 안내 화면 생성
            canvas.transform.SetParent(transform, false); // 씬 화면 아래 배치
            var mint = new Color(0.62f, 0.87f, 0.79f); // 안내 강조 색상
            UiFactory.CreateText(canvas.transform, "Header", "던전 탐험", 38, new Vector2(0f, 450f), new Vector2(1000f, 90f), Color.white); // 던전 진입 결과 표시
            status = UiFactory.CreateText(canvas.transform, "Status", "던전 준비 중", 23, new Vector2(0f, 375f), new Vector2(1000f, 60f), mint); // 현재 방 안내 표시
            UiFactory.CreateText(canvas.transform, "Scope", "흰 화살표: 방향  ·  노란 화살표: 문  ·  붉은 칸: 장애물", 23, new Vector2(0f, -380f), new Vector2(1500f, 70f), new Color(0.66f, 0.72f, 0.75f)); // 방향과 문과 장애물 표시 안내
            UiFactory.CreateText(canvas.transform, "Controls", "WASD / 방향키 / 패드: 이동  ·  E / 패드 A: 문  ·  Esc: 타이틀", 23, new Vector2(0f, -455f), new Vector2(1700f, 60f), mint); // 이동과 문과 복귀 조작 안내
            var roomObject = GameObject.Find("RoomTravel"); // 실제 방 이동 객체 조회
            travel = roomObject == null ? null : roomObject.GetComponent<RoomTravelController>(); // 방 이동 제어기 연결
            DungeonMiniMap.Create(canvas.transform, travel); // 발견한 방의 미니맵 생성
            if (travel != null && travel.IsReady) // 생성된 방 구조와 실제 상태 검사
            { // 방 정보 연결 시작
                travel.RoomChanged += UpdateRoom; // 성공한 방 이동 알림 연결
                UpdateRoom(); // 시작 방의 이름 표시
            } // 방 정보 연결 끝
        } // 구성 끝
        private void OnDestroy() // 화면 이벤트 연결 정리
        { // 정리 시작
            if (travel != null) // 방 이동 객체 존재 검사
            { // 연결 해제 시작
                travel.RoomChanged -= UpdateRoom; // 방 이름 갱신 연결 해제
            } // 연결 해제 끝
        } // 정리 끝
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
                    status.text = "현재 위치: 계단 방"; // 계단 방 이름 표시
                    break; // 역할 선택 종료
                case RoomKind.Elite: // 상위 개체 방
                    status.text = "현재 위치: 상위 개체 방"; // 상위 개체 방 이름 표시
                    break; // 역할 선택 종료
                default: // 일반 방
                    status.text = "현재 위치: 일반 방"; // 일반 방 이름 표시
                    break; // 역할 선택 종료
            } // 역할 선택 끝
        } // 갱신 끝
    } // 클래스 끝
} // 이름 공간 끝
