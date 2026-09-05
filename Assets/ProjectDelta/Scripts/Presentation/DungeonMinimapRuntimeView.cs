using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // UGUI 기능

namespace ProjectDelta.Presentation // 프레젠테이션 네임스페이스
{
    public sealed class DungeonMinimapRuntimeView : MonoBehaviour // 미니맵 런타임 UGUI 화면
    {
        private GameObject canvasObject; // 전용 캔버스
        private RectTransform contentRoot; // 동적 콘텐츠 루트
        private RectTransform canvasRect; // 캔버스 좌표
        private Font runtimeFont; // 런타임 기본 글꼴

        public bool IsVisible =>
            canvasObject != null
            && canvasObject.activeSelf; // 화면 표시 여부

        public void Initialize() // 미니맵 UGUI 초기화
        {
            if (canvasObject != null) // 기존 화면 확인
            {
                return; // 중복 생성 방지
            }

            RuntimeUiFactory.EnsureEventSystem(); // 공용 EventSystem 준비

            canvasObject = new GameObject(
                "DungeonMinimapRuntimeCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)); // 전용 Canvas 생성

            canvasObject.transform.SetParent(
                transform,
                false); // 어댑터 하위 배치

            DontDestroyOnLoad(
                canvasObject); // 장면 전환 유지

            Canvas canvas =
                canvasObject.GetComponent<Canvas>(); // Canvas 참조 조회

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay; // 화면 오버레이 사용

            canvas.sortingOrder =
                3000; // 일반 탐험 HUD 위 표시

            CanvasScaler scaler =
                canvasObject.GetComponent<CanvasScaler>(); // 스케일러 조회

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 스케일 사용

            UiScaleSettings.Refresh(); // 현재 UI 배율 갱신
            UiScaleSettings.ApplyToCanvasScaler(
                scaler,
                new Vector2(1920f, 1080f)); // 프로젝트 공용 배율 적용

            GraphicRaycaster raycaster =
                canvasObject.GetComponent<GraphicRaycaster>(); // 레이캐스터 조회

            raycaster.enabled =
                false; // 미니맵 입력 가로채기 방지

            canvasRect =
                canvasObject.GetComponent<RectTransform>(); // Canvas 좌표 저장

            contentRoot =
                RuntimeUiFactory.CreateStretchedRect(
                    "Content",
                    canvasObject.transform); // 전체 콘텐츠 루트 생성

            runtimeFont =
                Resources.GetBuiltinResource<Font>(
                    "LegacyRuntime.ttf"); // 기본 글꼴 로드

            canvasObject.SetActive(
                false); // 초기 화면 숨김
        }

        public void Show(
            DungeonMinimapRuntimeFrame frame) // 현재 프레임 표시
        {
            if (canvasObject == null) // 초기화 여부 확인
            {
                Initialize(); // 화면 자동 생성
            }

            canvasObject.SetActive(
                true); // 미니맵 화면 표시

            Render(
                frame); // 프레임 렌더링
        }

        public void Hide() // 미니맵 화면 숨김
        {
            if (canvasObject == null) // 화면 존재 확인
            {
                return; // 미생성 종료
            }

            canvasObject.SetActive(
                false); // 화면 숨김
        }

        private void Render(
            DungeonMinimapRuntimeFrame frame) // 전체 프레임 UGUI 변환
        {
            ClearChildren(
                contentRoot); // 이전 지도 오브젝트 제거

            if (frame == null
                || frame.Root == null) // 프레임 유효성 확인
            {
                return; // 빈 프레임 종료
            }

            for (int index = 0;
                 index < frame.Root.Children.Count;
                 index++) // 최상위 노드 순회
            {
                RenderNode(
                    contentRoot,
                    frame.Root.Children[index]); // 개별 노드 생성
            }
        }

        private void RenderNode(
            RectTransform parent,
            DungeonMinimapRuntimeNode node) // 노드 종류별 생성
        {
            if (node == null) // 빈 노드 확인
            {
                return; // 빈 노드 건너뛰기
            }

            switch (node.Kind) // 노드 종류 분기
            {
                case DungeonMinimapRuntimeNodeKind.Group:
                {
                    RenderGroup(
                        parent,
                        node); // 그룹 생성
                    break; // 그룹 분기 종료
                }

                case DungeonMinimapRuntimeNodeKind.Texture:
                {
                    RenderTexture(
                        parent,
                        node); // 텍스처 생성
                    break; // 텍스처 분기 종료
                }

                case DungeonMinimapRuntimeNodeKind.Label:
                {
                    RenderLabel(
                        parent,
                        node); // 라벨 생성
                    break; // 라벨 분기 종료
                }
            }
        }

        private void RenderGroup(
            RectTransform parent,
            DungeonMinimapRuntimeNode node) // 클리핑 그룹 생성
        {
            RectTransform group =
                RuntimeUiFactory.CreateUiObject(
                    "MapGroup",
                    parent); // 그룹 오브젝트 생성

            ApplyTopLeftRect(
                group,
                node.Rect); // 기존 GUI 좌표 적용

            group.gameObject.AddComponent<RectMask2D>(); // 그룹 범위 클리핑 적용

            for (int index = 0;
                 index < node.Children.Count;
                 index++) // 그룹 자식 순회
            {
                RenderNode(
                    group,
                    node.Children[index]); // 자식 노드 생성
            }
        }

        private void RenderTexture(
            RectTransform parent,
            DungeonMinimapRuntimeNode node) // 텍스처 UGUI 생성
        {
            RectTransform rect =
                RuntimeUiFactory.CreateUiObject(
                    "MapTexture",
                    parent); // 텍스처 오브젝트 생성

            bool rotated =
                Mathf.Abs(node.RotationAngle) > 0.001f; // 회전 여부 확인

            if (rotated) // 회전 텍스처 확인
            {
                ApplyCenteredRect(
                    rect,
                    node.Rect); // 중심 피벗 좌표 적용

                rect.localEulerAngles =
                    new Vector3(
                        0f,
                        0f,
                        -node.RotationAngle); // IMGUI 좌표계 회전 보정
            }
            else
            {
                ApplyTopLeftRect(
                    rect,
                    node.Rect); // 일반 좌표 적용
            }

            RawImage image =
                rect.gameObject.AddComponent<RawImage>(); // 원본 Texture 표시 추가

            image.texture =
                node.Texture != null
                    ? node.Texture
                    : Texture2D.whiteTexture; // 표시 텍스처 지정

            image.color =
                node.Color; // 기존 GUI.color 적용

            image.raycastTarget =
                false; // 탐험 입력 방해 방지
        }

        private void RenderLabel(
            RectTransform parent,
            DungeonMinimapRuntimeNode node) // 텍스트 UGUI 생성
        {
            RectTransform rect =
                RuntimeUiFactory.CreateUiObject(
                    "MapLabel",
                    parent); // 라벨 오브젝트 생성

            ApplyTopLeftRect(
                rect,
                node.Rect); // 기존 GUI 좌표 적용

            Text text =
                rect.gameObject.AddComponent<Text>(); // UGUI Text 추가

            RuntimeUiFactory.ConfigureText(
                text,
                node.Text,
                ScaleFontSize(node.FontSize),
                node.FontStyle,
                node.Alignment); // 공용 텍스트 설정 적용

            text.color =
                node.Color; // 기존 GUIStyle 색상 적용

            text.horizontalOverflow =
                HorizontalWrapMode.Wrap; // 긴 문구 줄바꿈 허용

            text.verticalOverflow =
                VerticalWrapMode.Overflow; // 세로 잘림 완화

            text.raycastTarget =
                false; // 탐험 입력 방해 방지

            if (runtimeFont != null) // 기본 글꼴 확인
            {
                text.font =
                    runtimeFont; // 런타임 글꼴 적용
            }
        }

        private void ApplyTopLeftRect(
            RectTransform target,
            Rect sourceRect) // IMGUI 좌표를 UGUI 좌표로 변환
        {
            Rect converted =
                ConvertScreenRect(
                    sourceRect); // 현재 Canvas 좌표 계산

            target.anchorMin =
                new Vector2(0f, 1f); // 왼쪽 위 기준 설정

            target.anchorMax =
                new Vector2(0f, 1f); // 왼쪽 위 기준 설정

            target.pivot =
                new Vector2(0f, 1f); // 왼쪽 위 피벗 설정

            target.anchoredPosition =
                new Vector2(
                    converted.x,
                    -converted.y); // 위쪽 기준 좌표 적용

            target.sizeDelta =
                new Vector2(
                    converted.width,
                    converted.height); // 크기 적용
        }

        private void ApplyCenteredRect(
            RectTransform target,
            Rect sourceRect) // 회전용 중심 피벗 좌표 변환
        {
            Rect converted =
                ConvertScreenRect(
                    sourceRect); // 현재 Canvas 좌표 계산

            target.anchorMin =
                new Vector2(0f, 1f); // 왼쪽 위 기준 설정

            target.anchorMax =
                new Vector2(0f, 1f); // 왼쪽 위 기준 설정

            target.pivot =
                new Vector2(0.5f, 0.5f); // 중심 회전 피벗 설정

            target.anchoredPosition =
                new Vector2(
                    converted.x + (converted.width * 0.5f),
                    -(converted.y + (converted.height * 0.5f))); // 중심 좌표 적용

            target.sizeDelta =
                new Vector2(
                    converted.width,
                    converted.height); // 크기 적용
        }

        private Rect ConvertScreenRect(
            Rect sourceRect) // 실제 화면 픽셀을 Canvas 기준 좌표로 변환
        {
            float canvasWidth =
                canvasRect != null
                    ? canvasRect.rect.width
                    : Screen.width; // Canvas 너비 조회

            float canvasHeight =
                canvasRect != null
                    ? canvasRect.rect.height
                    : Screen.height; // Canvas 높이 조회

            float scaleX =
                Screen.width > 0
                    ? canvasWidth / Screen.width
                    : 1f; // 가로 좌표 변환 비율

            float scaleY =
                Screen.height > 0
                    ? canvasHeight / Screen.height
                    : 1f; // 세로 좌표 변환 비율

            return new Rect(
                sourceRect.x * scaleX,
                sourceRect.y * scaleY,
                sourceRect.width * scaleX,
                sourceRect.height * scaleY); // 변환 좌표 반환
        }

        private int ScaleFontSize(
            int sourceSize) // IMGUI 글자 크기 Canvas 보정
        {
            float canvasWidth =
                canvasRect != null
                    ? canvasRect.rect.width
                    : Screen.width; // Canvas 너비 조회

            float canvasHeight =
                canvasRect != null
                    ? canvasRect.rect.height
                    : Screen.height; // Canvas 높이 조회

            float scaleX =
                Screen.width > 0
                    ? canvasWidth / Screen.width
                    : 1f; // 가로 글자 비율

            float scaleY =
                Screen.height > 0
                    ? canvasHeight / Screen.height
                    : 1f; // 세로 글자 비율

            float scale =
                Mathf.Min(
                    scaleX,
                    scaleY); // 작은 축 기준 보정

            return Mathf.Max(
                1,
                Mathf.RoundToInt(sourceSize * scale)); // 보정 글자 크기 반환
        }

        private static void ClearChildren(
            Transform parent) // 동적 UI 자식 제거
        {
            if (parent == null) // 부모 존재 확인
            {
                return; // 제거 대상 없음
            }

            for (int index = parent.childCount - 1;
                 index >= 0;
                 index--) // 자식 역순 순회
            {
                Destroy(
                    parent.GetChild(index).gameObject); // 이전 UI 제거
            }
        }
    }
}
