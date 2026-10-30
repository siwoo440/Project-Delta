using System; // 페이지 범위 계산
using System.Collections.Generic; // 표시할 기록 목록
using UnityEngine; // 기록 화면 객체
using UnityEngine.UI; // 기록 화면 구성
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class HistoryScreenView : MonoBehaviour // 현재 회차의 전체 탐험 기록 화면
    { // 클래스 시작
        private const int PageSize = 8; // 한 페이지의 기록 수
        private AppRoot owner; // 현재 회차 기록 관리자
        private Text entries; // 현재 페이지의 기록 내용
        private Text pageLabel; // 현재 페이지 번호
        private int page; // 현재 페이지의 순서
        public static HistoryScreenView Create(AppRoot owner, Transform parent) // 현재 회차 기록 화면 생성
        { // 생성 시작
            var screen = new GameObject("HistoryScreen", typeof(HistoryScreenView)); // 기록 화면 객체
            screen.transform.SetParent(parent, false); // 던전 씬 수명에 연결
            try // 실제 기록 화면 구성
            { // 구성 시작
                var view = screen.GetComponent<HistoryScreenView>(); // 기록 화면 제어
                view.owner = owner; // 실제 회차 기록 연결
                var canvas = UiFactory.CreateCanvas("HistoryCanvas"); // 기록 전용 화면
                canvas.transform.SetParent(screen.transform, false); // 기록 화면 객체에 연결
                canvas.sortingOrder = 200; // 탐험 화면보다 앞에 표시
                var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); // 기록 배경 객체
                background.transform.SetParent(canvas.transform, false); // 전체 화면 아래 배치
                var rect = background.GetComponent<RectTransform>(); // 배경 위치와 크기
                rect.anchorMin = Vector2.zero; // 전체 화면 왼쪽 아래
                rect.anchorMax = Vector2.one; // 전체 화면 오른쪽 위
                rect.offsetMin = Vector2.zero; // 왼쪽 아래 여백 제거
                rect.offsetMax = Vector2.zero; // 오른쪽 위 여백 제거
                var image = background.GetComponent<Image>(); // 배경 색상 구성
                image.color = new Color(0.025f, 0.045f, 0.06f, 0.97f); // 어두운 기록 바탕
                UiFactory.Decorate(image, "MenuPanel"); // 화면 용도별 그림 장식
                image.raycastTarget = false; // 다른 입력 방해 방지
                UiFactory.CreateText(canvas.transform, "Title", "현재 회차 탐험 기록", 42, new Vector2(0f, 450f), new Vector2(1100f, 80f), Color.white); // 기록 제목
                view.entries = UiFactory.CreateText(canvas.transform, "HistoryEntries", "", 28, new Vector2(0f, 40f), new Vector2(1200f, 640f), Color.white); // 시간순 기록 내용
                view.entries.alignment = TextAnchor.UpperLeft; // 기록의 왼쪽 위 정렬
                view.pageLabel = UiFactory.CreateText(canvas.transform, "HistoryPage", "", 25, new Vector2(0f, -305f), new Vector2(300f, 50f), Color.white); // 현재 페이지 안내
                UiFactory.CreateText(canvas.transform, "CloseHint", "H / 스틱 버튼 / Esc / 패드 B: 닫기 · 방향키: 페이지", 22, new Vector2(0f, -470f), new Vector2(1500f, 55f), Color.white); // 기록 화면 조작 안내
                MapPointerButton.Create(canvas.transform, "PreviousHistoryButton", "이전", new Vector2(-320f, -390f), () => view.MovePage(-1)); // 마우스 이전 페이지
                MapPointerButton.Create(canvas.transform, "NextHistoryButton", "다음", new Vector2(0f, -390f), () => view.MovePage(1)); // 마우스 다음 페이지
                MapPointerButton.Create(canvas.transform, "CloseHistoryButton", "닫기", new Vector2(320f, -390f), owner.CloseHistory); // 마우스 기록 닫기
                view.Refresh(); // 최초 기록 페이지 표시
                return view; // 완성한 기록 화면 전달
            } // 구성 끝
            catch // 화면 구성 실패 검사
            { // 실패 정리 시작
                Destroy(screen); // 불완전한 화면 객체 제거
                throw; // 원래 실패 전달
            } // 실패 정리 끝
        } // 생성 끝
        public void MovePage(int direction) // 요청한 페이지로 이동
        { // 이동 시작
            var total = Math.Max(1, (owner.ExplorationHistory.Entries.Count + PageSize - 1) / PageSize); // 전체 페이지 수
            page = Mathf.Clamp(page + direction, 0, total - 1); // 페이지 범위 안에서 이동
            Refresh(); // 선택한 페이지 갱신
        } // 이동 끝
        private void Refresh() // 현재 페이지의 기록 표시
        { // 갱신 시작
            var history = owner.ExplorationHistory.Entries; // 현재 회차의 시간순 기록
            var total = Math.Max(1, (history.Count + PageSize - 1) / PageSize); // 전체 페이지 수
            page = Mathf.Clamp(page, 0, total - 1); // 현재 페이지 범위 보호
            var lines = new List<string>(); // 현재 페이지 기록 목록
            var first = page * PageSize; // 현재 페이지의 첫 기록 번호
            var end = Math.Min(first + PageSize, history.Count); // 현재 페이지의 마지막 범위
            for (var index = first; index < end; index++) // 현재 페이지 기록 순회
            { // 순회 시작
                lines.Add((index + 1) + ". " + history[index]); // 실제 기록의 순서와 내용
            } // 순회 끝
            entries.text = lines.Count == 0 ? "아직 탐험 기록이 없습니다." : string.Join("\n", lines); // 현재 페이지 기록 표시
            pageLabel.text = (page + 1) + " / " + total; // 현재와 전체 페이지 표시
        } // 갱신 끝
    } // 클래스 끝
} // 이름 공간 끝
