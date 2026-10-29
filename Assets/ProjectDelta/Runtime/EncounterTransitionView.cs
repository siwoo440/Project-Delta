using System.Collections; // 단계별 전환 흐름
using UnityEngine; // 화면 객체와 시간
using UnityEngine.UI; // 화면 배경과 초상화
namespace ProjectDelta.Runtime // 실행 이름 공간
{ // 이름 공간 시작
    public sealed class EncounterTransitionView : MonoBehaviour // 자동 전투 진입 연출
    { // 클래스 시작
        private Image backdrop; // 화면 전체의 입력 차단 배경
        private Image overlay; // 흰 섬광과 검은 전환막
        private CanvasGroup content; // 조우 이름과 초상화
        private bool covering; // 진행 중인 화면 가림
        private bool revealing; // 진행 중인 화면 복원
        public bool IsCovered { get; private set; } // 전투 생성이 가능한 완전 가림 상태
        public static EncounterTransitionView Create(string enemyName, string portraitPath) // 조우 이름과 선택적 초상화 연결
        { // 생성 시작
            var screen = new GameObject("EncounterTransition", typeof(EncounterTransitionView)); // 전환 화면 객체
            try // 화면 구성 검사
            { // 화면 구성 시작
                var view = screen.GetComponent<EncounterTransitionView>(); // 전환 제어 조회
                var canvas = UiFactory.CreateCanvas("EncounterTransitionCanvas"); // 전용 화면 생성
                canvas.transform.SetParent(screen.transform, false); // 전환 객체에 화면 연결
                canvas.sortingOrder = 300; // 전투 화면보다 앞의 전환막
                view.backdrop = CreateScreenImage(canvas.transform, "EncounterTransitionBackdrop"); // 전체 입력 차단 배경
                view.backdrop.color = new Color(0.025f, 0.035f, 0.06f, 0f); // 최초 투명 배경
                view.backdrop.raycastTarget = true; // 화면 전체의 포인터 입력 차단
                var contentObject = new GameObject("EncounterTransitionContent", typeof(RectTransform), typeof(CanvasGroup)); // 조우 정보 묶음
                contentObject.transform.SetParent(canvas.transform, false); // 전환 화면 아래 배치
                view.content = contentObject.GetComponent<CanvasGroup>(); // 조우 내용 투명도 제어
                view.content.alpha = 0f; // 최초 조우 정보 숨김
                view.content.interactable = false; // 조우 내용의 선택 입력 차단
                view.content.blocksRaycasts = false; // 배경의 입력 차단 유지
                var name = string.IsNullOrWhiteSpace(enemyName) ? "알 수 없는 상대" : enemyName; // 표시할 실제 상대 이름
                UiFactory.CreateText(contentObject.transform, "EncounterTransitionName", name + " 등장!", 48, new Vector2(0f, -280f), new Vector2(1100f, 90f), Color.white); // 조우 상대 안내
                var texture = string.IsNullOrWhiteSpace(portraitPath) ? null : Resources.Load<Texture2D>(portraitPath); // 선택적 리소스 초상화
                if (texture != null && texture.width > 0 && texture.height > 0) // 유효한 초상화 검사
                { // 초상화 생성 시작
                    var portrait = new GameObject("EncounterTransitionPortrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage)); // 조우 초상화 객체
                    portrait.transform.SetParent(contentObject.transform, false); // 조우 내용 아래 배치
                    var rect = portrait.GetComponent<RectTransform>(); // 초상화 위치와 크기
                    var ratio = (float)texture.width / texture.height; // 원본 가로세로 비율
                    rect.sizeDelta = ratio >= 1f ? new Vector2(420f, 420f / ratio) : new Vector2(420f * ratio, 420f); // 비율을 보존한 초상화 크기
                    rect.anchoredPosition = new Vector2(0f, 20f); // 조우 화면 중앙 배치
                    var image = portrait.GetComponent<RawImage>(); // 초상화 표시 구성
                    image.texture = texture; // 실제 초상화 연결
                    image.raycastTarget = false; // 전환 입력 차단 배경 유지
                } // 초상화 생성 끝
                view.overlay = CreateScreenImage(canvas.transform, "EncounterTransitionOverlay"); // 화면 최상단 전환막
                view.overlay.color = new Color(1f, 1f, 1f, 0f); // 최초 투명 섬광
                view.overlay.raycastTarget = false; // 배경의 입력 차단 유지
                return view; // 생성된 전환 화면 전달
            } // 화면 구성 끝
            catch // 화면 구성 실패
            { // 생성 실패 시작
                screen.SetActive(false); // 불완전한 화면 즉시 숨김
                Destroy(screen); // 불완전한 전환 객체 정리
                throw; // 원래 구성 오류 전달
            } // 생성 실패 끝
        } // 생성 끝
        public IEnumerator Cover() // 섬광과 조우 표시 후 완전 가림
        { // 가림 시작
            if (covering || revealing || IsCovered) // 중복 실행과 상태 검사
            { // 중복 거절 시작
                yield break; // 기존 전환 상태 보존
            } // 중복 거절 끝
            covering = true; // 화면 가림 진행 표시
            yield return Fade(overlay, Color.white, 0f, 1f, 0.09f); // 짧은 흰색 섬광
            if (this == null || !isActiveAndEnabled || backdrop == null || content == null) // 중단된 전환과 화면 검사
            { // 중단 시작
                yield break; // 파괴된 화면의 후속 변경 차단
            } // 중단 끝
            backdrop.color = new Color(0.025f, 0.035f, 0.06f, 0.96f); // 조우 배경 표시
            content.alpha = 1f; // 상대 이름과 초상화 표시
            yield return Fade(overlay, Color.white, 1f, 0f, 0.12f); // 섬광 뒤 조우 정보 노출
            yield return new WaitForSecondsRealtime(0.32f); // 상대 확인 시간
            yield return Fade(overlay, Color.black, 0f, 1f, 0.20f); // 전투 화면 생성 전 검은 가림
            if (this == null || !isActiveAndEnabled || overlay == null) // 중단된 전환과 전환막 검사
            { // 중단 시작
                yield break; // 중단된 전환의 완료 표시 차단
            } // 중단 끝
            IsCovered = true; // 완전히 가려진 전환 시점
            covering = false; // 가림 작업 종료
        } // 가림 끝
        public IEnumerator Reveal() // 준비된 전투 화면 표시
        { // 복원 시작
            if (!IsCovered || covering || revealing) // 완전 가림과 중복 실행 검사
            { // 복원 거절 시작
                yield break; // 잘못된 순서의 화면 변경 차단
            } // 복원 거절 끝
            revealing = true; // 화면 복원 진행 표시
            if (this == null || !isActiveAndEnabled || content == null || backdrop == null) // 중단된 화면 검사
            { // 중단 시작
                yield break; // 파괴된 화면의 변경 차단
            } // 중단 끝
            content.alpha = 0f; // 이전 조우 내용 숨김
            backdrop.color = new Color(0f, 0f, 0f, 0f); // 기존 배경 투명화
            yield return Fade(overlay, Color.black, 1f, 0f, 0.24f); // 준비된 화면의 부드러운 노출
            if (this == null || !isActiveAndEnabled || overlay == null) // 중단된 전환과 전환막 검사
            { // 중단 시작
                yield break; // 파괴된 화면의 상태 변경 차단
            } // 중단 끝
            IsCovered = false; // 완전 가림 상태 해제
            revealing = false; // 화면 복원 종료
        } // 복원 끝
        public void Close() // 전환 화면 정리
        { // 정리 시작
            gameObject.SetActive(false); // 화면과 입력 차단 즉시 해제
            Destroy(gameObject); // 전환 객체 제거
        } // 정리 끝
        private static Image CreateScreenImage(Transform parent, string name) // 화면 전체를 덮는 이미지 생성
        { // 생성 시작
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 화면 이미지 객체
            imageObject.transform.SetParent(parent, false); // 지정 화면 아래 배치
            var rect = imageObject.GetComponent<RectTransform>(); // 화면 크기 제어
            rect.anchorMin = Vector2.zero; // 왼쪽 아래 기준
            rect.anchorMax = Vector2.one; // 오른쪽 위 기준
            rect.offsetMin = Vector2.zero; // 왼쪽 아래 여백 제거
            rect.offsetMax = Vector2.zero; // 오른쪽 위 여백 제거
            return imageObject.GetComponent<Image>(); // 생성된 화면 이미지 전달
        } // 생성 끝
        private static IEnumerator Fade(Image image, Color color, float from, float to, float duration) // 시간 배율과 무관한 전환막 페이드
        { // 페이드 시작
            if (image == null || !image.isActiveAndEnabled) // 전환막의 파괴와 비활성 검사
            { // 중단 시작
                yield break; // 유효하지 않은 화면 변경 차단
            } // 중단 끝
            var elapsed = 0f; // 경과 시간 초기화
            color.a = from; // 최초 투명도 설정
            image.color = color; // 최초 전환막 표시
            while (elapsed < duration) // 지정된 시간 동안 반복
            { // 페이드 반복 시작
                if (image == null || !image.isActiveAndEnabled) // 대기 중 전환막의 파괴와 비활성 검사
                { // 중단 시작
                    yield break; // 제거된 화면의 후속 변경 차단
                } // 중단 끝
                elapsed += Time.unscaledDeltaTime; // 일시정지와 무관한 경과 시간
                color.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)); // 부드러운 투명도 계산
                image.color = color; // 현재 전환막 반영
                yield return null; // 다음 화면 프레임 대기
            } // 페이드 반복 끝
            if (image == null || !image.isActiveAndEnabled) // 마지막 대기 중 전환막 상태 검사
            { // 중단 시작
                yield break; // 제거된 화면의 최종 변경 차단
            } // 중단 끝
            color.a = to; // 최종 투명도 보정
            image.color = color; // 정확한 최종 화면 반영
        } // 페이드 끝
    } // 클래스 끝
} // 이름 공간 끝
